using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(PreviewAbilityReconcileDecision.NoOwnership,
    PreviewAbilityReconcilePolicy.Decide(0, compatible: true, saveAvailable: true, nativeFlag: false),
    "item not received");
Equal(PreviewAbilityReconcileDecision.Incompatible,
    PreviewAbilityReconcilePolicy.Decide(1, compatible: false, saveAvailable: true, nativeFlag: false),
    "incompatible session");
Equal(PreviewAbilityReconcileDecision.SaveUnavailable,
    PreviewAbilityReconcilePolicy.Decide(1, compatible: true, saveAvailable: false, nativeFlag: null),
    "selected save unavailable");
Equal(PreviewAbilityReconcileDecision.AlreadyGranted,
    PreviewAbilityReconcilePolicy.Decide(1, compatible: true, saveAvailable: true, nativeFlag: true),
    "native ability already present");
Equal(PreviewAbilityReconcileDecision.SubmitGrant,
    PreviewAbilityReconcilePolicy.Decide(1, compatible: true, saveAvailable: true, nativeFlag: false),
    "missing native ability");

VerifyRuntime(new HypnoPanReconciler(new FakePreviewAbilityNativeAdapter()), "PIED_PIPER_ABILITY", "Hypno Pan");
VerifyRuntime(new ViolanceReconciler(new FakePreviewAbilityNativeAdapter()), "VIOLIN_ABILITY", "Violance");
VerifyNetworkRequestsWaitForUnityDrain();

string pluginSource = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
Equal(true, pluginSource.Contains("PreviewAbilityRandomization.ApplySlotData(loginSuccess.SlotData)", StringComparison.Ordinal),
    "login configures preview compatibility");
Equal(true, pluginSource.Contains("PreviewAbilityRandomization.TryApplyItem(item.ItemName)", StringComparison.Ordinal),
    "received item history reaches preview reconcilers");
Equal(true, pluginSource.Contains("AddComponent<PreviewAbilityReconciliationKeeper>()", StringComparison.Ordinal),
    "Unity-thread lifecycle keeper is installed");
Equal(true, pluginSource.Contains("PreviewAbilityRandomization.CapturePlayerSaveRequestProcessor(__instance)", StringComparison.Ordinal),
    "save processor lifecycle is captured");
Equal(true, pluginSource.Contains("PreviewAbilityRandomization.RequestUnityReconciliation(\"Archipelago connected\")", StringComparison.Ordinal),
    "network connection queues preview reconciliation");
Equal(false, pluginSource.Contains("PreviewAbilityRandomization.OnLifecyclePoint(\"Archipelago connected\")", StringComparison.Ordinal),
    "network connection never performs native reconciliation directly");

Console.WriteLine("Preview ability reconciler tests passed.");

static void VerifyNetworkRequestsWaitForUnityDrain()
{
    var dispatcher = new PreviewAbilityReconcileDispatcher();
    int nativeCalls = 0;
    string drainedReason = string.Empty;

    Task.Run(() => dispatcher.Request("Archipelago connected")).GetAwaiter().GetResult();

    Equal(0, nativeCalls, "network request performs no Unity/native work");
    Equal(true, dispatcher.Drain(reason =>
    {
        nativeCalls++;
        drainedReason = reason;
    }), "Unity lifecycle drains the queued request");
    Equal(1, nativeCalls, "Unity drain performs reconciliation once");
    Equal("Archipelago connected", drainedReason, "Unity drain preserves the request reason");
    Equal(false, dispatcher.Drain(_ => nativeCalls++), "request is consumed once");
    Equal(1, nativeCalls, "empty drain performs no reconciliation");
}

static void VerifyRuntime(IPreviewAbilityReconciler reconciler, string expectedFlag, string itemName)
{
    var adapter = (FakePreviewAbilityNativeAdapter)reconciler.NativeAdapterForTests;
    Equal(expectedFlag, reconciler.NativeFlag, $"{itemName} native mapping");
    reconciler.Configure(compatible: true);
    reconciler.NoteReceivedCount(1);
    reconciler.NoteReceivedCount(1);
    reconciler.OnLifecyclePoint("history synchronized");
    Equal("save-unavailable", reconciler.LastOutcome, $"{itemName} waits for selected save");

    adapter.SaveAvailable = true;
    reconciler.OnLifecyclePoint("save available");
    Equal("grant-submitted", reconciler.LastOutcome, $"{itemName} submits grant");
    Equal(1, adapter.ApplyCount, $"{itemName} submits once");
    reconciler.OnLifecyclePoint("verify");
    Equal("verified-owned", reconciler.LastOutcome, $"{itemName} verifies native flag");
    Equal(1, adapter.ApplyCount, $"{itemName} duplicate count is idempotent");
    reconciler.TickPending(TimeSpan.FromSeconds(5));
    Equal(1, adapter.ApplyCount, $"{itemName} verified state does not retry");
}

internal sealed class FakePreviewAbilityNativeAdapter : IPreviewAbilityNativeAdapter
{
    public bool SaveAvailable { get; set; }
    public bool NativeOwned { get; set; }
    public int ApplyCount { get; private set; }

    public bool TryRead(string nativeFlag, out bool owned)
    {
        owned = NativeOwned;
        return SaveAvailable;
    }

    public bool TrySubmit(string nativeFlag, out string detail)
    {
        ApplyCount++;
        NativeOwned = true;
        detail = "fake grant submitted";
        return true;
    }
}
