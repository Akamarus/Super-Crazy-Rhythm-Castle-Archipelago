using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static void True(bool actual, string scenario) => Equal(true, actual, scenario);

static void False(bool actual, string scenario) => Equal(false, actual, scenario);

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "client", "Plugin.cs")))
            return directory.FullName;
        directory = directory.Parent;
    }

    throw new InvalidOperationException("repository root containing client/Plugin.cs was not found");
}

static string Slice(string source, string startMarker, string endMarker)
{
    int start = source.IndexOf(startMarker, StringComparison.Ordinal);
    if (start < 0)
        throw new InvalidOperationException($"production wiring marker missing: {startMarker}");
    int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
    if (end < 0)
        throw new InvalidOperationException($"production wiring end marker missing: {endMarker}");
    return source[start..end];
}

static (bool Intercepted, bool EffectiveOwned) ResolveEntrancePreview(
    bool enabled,
    bool compatible,
    string nativeCartridgeType,
    bool nativeOwned,
    params string[] apOwnedSongs)
{
    bool intercepted = GarageAvailabilityPolicy.TryResolveEntrancePreviewOwned(
        enabled,
        compatible,
        nativeCartridgeType,
        nativeOwned,
        apOwnedSongs,
        out var ownership);
    return (intercepted, intercepted ? ownership.EffectiveOwned : nativeOwned);
}

static (bool ContractMatched, int ChangedCount) ApplyEntrancePreview(
    object previewData,
    bool enabled,
    bool compatible,
    params string[] apOwnedSongs)
{
    bool contractMatched = GarageAvailabilityPolicy.TryApplyEntrancePreviewOwnership(
        previewData,
        enabled,
        compatible,
        apOwnedSongs,
        out int changedCount);
    return (contractMatched, changedCount);
}

foreach (bool owns in new[] { false, true })
{
    Equal(GarageObjectDecision.Active,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.Initialization),
        "holder and play initialization always remain active");
    Equal(owns ? GarageObjectDecision.Active : GarageObjectDecision.Inactive,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.SongCartridge),
        "only matching song cartridge follows AP ownership");
}

foreach (GarageObjectRole role in Enum.GetValues<GarageObjectRole>())
{
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(false, true, false, role),
        "disabled AP preserves vanilla");
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(true, false, false, role),
        "incompatible slot preserves vanilla");
}

var entrancePreviewCases = new[]
{
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, true),
        Scenario: "AP-owned Superstar changes native false to effective true"),
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, false),
        Scenario: "unowned Superstar remains false"),
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: true,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, false),
        Scenario: "native source flag cannot expose AP-unowned Superstar"),
    (Enabled: true, Compatible: true, NativeType: "VAMPIRE_KILLER", NativeOwned: false,
        ApOwnedSongs: new[] { "Vampire Killer" }, Expected: (false, false),
        Scenario: "physical Vampire Killer remains fully native"),
    (Enabled: false, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, false),
        Scenario: "disabled routing preserves native false"),
    (Enabled: true, Compatible: false, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, false),
        Scenario: "incompatible routing preserves native false"),
    (Enabled: false, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: true,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, true),
        Scenario: "disabled routing preserves native true"),
};
foreach (var testCase in entrancePreviewCases)
{
    Equal(
        testCase.Expected,
        ResolveEntrancePreview(
            testCase.Enabled,
            testCase.Compatible,
            testCase.NativeType,
            testCase.NativeOwned,
            testCase.ApOwnedSongs),
        testCase.Scenario);
}

True(
    GarageAvailabilityPolicy.IsExactEntrancePreviewViewSignature(
        "LevelPreviewUIView",
        "ReflectGarageVisuals",
        "Void",
        new[] { "LevelPreviewUIData" }),
    "preview override targets the exact LevelPreviewUIView Garage signature");
False(
    GarageAvailabilityPolicy.IsExactEntrancePreviewViewSignature(
        "LevelPreviewUIData",
        "ReflectGarageVisuals",
        "Void",
        new[] { "LevelPreviewUIData" }),
    "preview override rejects the Garage visuals method on any other owner");

var liveShape = new FakeGaragePreviewData(
    new FakeGarageResult("BLOODY_TEARS", false),
    new FakeGarageResult("VAMPIRE_KILLER", true),
    new FakeGarageResult("SMOOCH", false),
    new FakeGarageResult("GRADIUS_REMIX", false),
    new FakeGarageResult("WAG_THE_DOG", false),
    new FakeGarageResult("SUPERSTAR", false));
Equal(
    (true, 1),
    ApplyEntrancePreview(liveShape, true, true, "Superstar"),
    "the six-entry live Garage DTO promotes only AP-owned Superstar");
Equal(false, liveShape.PreviousGarageResults[0].cartridgeOwned,
    "unowned Bloody Tears remains hidden");
Equal(true, liveShape.PreviousGarageResults[1].cartridgeOwned,
    "physical Vampire Killer preserves native ownership");
Equal(true, liveShape.PreviousGarageResults[5].cartridgeOwned,
    "AP-owned Superstar becomes display-owned before visual reflection");
Equal(1, liveShape.PreviousGarageResults[5].WriteCount,
    "the ephemeral Superstar DTO is changed exactly once");
Equal(0, liveShape.PreviousGarageResults.Take(5).Sum(result => result.WriteCount),
    "no other Garage DTO entry is written");

foreach (var scenario in new[]
{
    (Enabled: true, Compatible: true, NativeOwned: false, ApOwned: Array.Empty<string>(), Expected: false, ExpectedWrites: 0,
        Name: "unowned randomized cartridge remains native false"),
    (Enabled: true, Compatible: true, NativeOwned: true, ApOwned: Array.Empty<string>(), Expected: false, ExpectedWrites: 1,
        Name: "native source flag is hidden without AP ownership"),
    (Enabled: false, Compatible: true, NativeOwned: false, ApOwned: new[] { "Superstar" }, Expected: false, ExpectedWrites: 0,
        Name: "disabled routing preserves native false"),
    (Enabled: true, Compatible: false, NativeOwned: false, ApOwned: new[] { "Superstar" }, Expected: false, ExpectedWrites: 0,
        Name: "incompatible routing preserves native false"),
})
{
    var data = new FakeGaragePreviewData(new FakeGarageResult("SUPERSTAR", scenario.NativeOwned));
    Equal((true, scenario.ExpectedWrites), ApplyEntrancePreview(data, scenario.Enabled, scenario.Compatible, scenario.ApOwned), scenario.Name);
    Equal(scenario.Expected, data.PreviousGarageResults[0].cartridgeOwned, scenario.Name);
    Equal(scenario.ExpectedWrites, data.PreviousGarageResults[0].WriteCount, scenario.Name);
}

var nativeVampire = new FakeGaragePreviewData(new FakeGarageResult("VAMPIRE_KILLER", false));
Equal(
    (true, 0),
    ApplyEntrancePreview(nativeVampire, true, true, "Vampire Killer"),
    "Vampire Killer remains outside the AP preview override");
Equal(false, nativeVampire.PreviousGarageResults[0].cartridgeOwned,
    "Vampire Killer false remains native false");
Equal(0, nativeVampire.PreviousGarageResults[0].WriteCount,
    "Vampire Killer is never written by the preview helper");

var validBeforeMismatch = new FakeGarageResult("SUPERSTAR", false);
var malformedShape = new FakeMalformedGaragePreviewData(
    validBeforeMismatch,
    new FakeGarageResultWithoutOwned("SMOOCH"));
Equal(
    (false, 0),
    ApplyEntrancePreview(malformedShape, true, true, "Superstar"),
    "a reflection contract mismatch fails closed");
Equal(false, validBeforeMismatch.cartridgeOwned,
    "reflection validation is all-or-nothing and makes no partial DTO mutation");
Equal(0, validBeforeMismatch.WriteCount,
    "reflection mismatch performs zero writes");

string pluginSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "client", "Plugin.cs"));
True(
    pluginSource.Contains("patched += PatchGarageEntrancePreview();", StringComparison.Ordinal),
    "plugin load installs the bounded Garage entrance preview hook");
True(
    pluginSource.Contains(
        "GarageAvailabilityPolicy.IsExactEntrancePreviewViewSignature(",
        StringComparison.Ordinal),
    "production hook discovery uses the tested exact ReflectGarageVisuals signature policy");
True(
    pluginSource.Contains(
        "_harmony.Patch(matchingTargets[0], prefix: new HarmonyMethod(prefix));",
        StringComparison.Ordinal),
    "the exact ReflectGarageVisuals target is patched only with the bounded prefix");
False(
    pluginSource.Contains("HasGarageCartridgeBeenCollected", StringComparison.Ordinal),
    "production never patches the global Garage collected enquiry");

string entrancePatchSource = Slice(
    pluginSource,
    "internal static class GarageEntrancePreviewPatches",
    "internal static class GarageCartridgeAccess");
string entranceAccessSource = Slice(
    pluginSource,
    "public static bool TryApplyEntrancePreviewOwnership(",
    "public static bool ShouldSuppressVanillaSourceGrant(");
True(
    entrancePatchSource.Contains(
        "GarageCartridgeAccess.TryApplyEntrancePreviewOwnership(__args[0])",
        StringComparison.Ordinal),
    "production ReflectGarageVisuals prefix applies ownership to the received ephemeral DTO");
True(
    entranceAccessSource.Contains(
        "GarageAvailabilityPolicy.TryApplyEntrancePreviewOwnership(",
        StringComparison.Ordinal),
    "production ownership access delegates the live DTO to the tested reflection seam");
foreach (string removedProbe in new[]
{
    "PatchGarageEntrancePipelineDiagnostics",
    "GarageEntrancePipelineDiagnosticPatches",
    "GAME GARAGE ENTRANCE PIPELINE",
    "RefreshDataForGarageCartridge",
})
{
    False(pluginSource.Contains(removedProbe, StringComparison.Ordinal),
        $"superseded diagnostic/ineffective hook is removed: {removedProbe}");
}
foreach (string forbiddenWriteApi in new[]
{
    "InsertionCoordinator",
    "NoteInserted",
    "TrySubmitProgressionFlag",
    "RecordGameProgressionInSaveDataRequest",
    "ObtainBagItemRequest",
    "_COLLECTED",
})
{
    False(
        entrancePatchSource.Contains(forbiddenWriteApi, StringComparison.Ordinal) ||
        entranceAccessSource.Contains(forbiddenWriteApi, StringComparison.Ordinal),
        $"Garage entrance preview path never invokes insertion/native write API {forbiddenWriteApi}");
}
foreach (string forbiddenMutation in new[] { "SetActive(", "TryWrite", "WriteMember", "_COLLECTED", "DataStorage" })
{
    False(
        entrancePatchSource.Contains(forbiddenMutation, StringComparison.Ordinal) ||
        entranceAccessSource.Contains(forbiddenMutation, StringComparison.Ordinal),
        $"Garage entrance preview changes no save/bag/visual state through {forbiddenMutation}");
}

Equal(true,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-vanilla-vampire-garage-0.21", "Vampire Killer"),
    "v0.21 enables the vanilla entrance cartridge");
Equal(true,
    GarageVanillaEntrancePolicy.IsEnabled(
        "area-routing-test-vanilla-vampire-garage-0.21-full-cassettes-0.22",
        "Vampire Killer"),
    "v0.22-compatible slot data retains the vanilla entrance cartridge");
Equal(false,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-difficulty-filtering-0.20", null),
    "v0.20 seeds preserve six-cartridge randomization");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Vampire Killer"),
    "v0.21 does not AP-randomize Vampire Killer");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Smooch"),
    "v0.21 still randomizes the other Garage cartridges");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Vampire Killer"),
    "v0.21 preserves the native Vampire Killer grant");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Smooch"),
    "v0.21 still suppresses randomized native grants");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(false, "Vampire Killer"),
    "v0.20 retains its old Vampire Killer source behavior");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldWriteNativeSaveFlags(true),
    "v0.21 requires the physical vanilla pickup and never writes its save flags");

string[] expectedNativeMappings =
{
    "Bloody Tears|Bloody Tears Cartridge|LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM|BLOODY_TEARS|scrc:garage_inserted:v1:bloody_tears",
    "Gradius Remix|Gradius Remix Cartridge|LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM|GRADIUS_REMIX|scrc:garage_inserted:v1:gradius_remix",
    "Smooch|Smooch Cartridge|LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM|SMOOCH|scrc:garage_inserted:v1:smooch",
    "Superstar|Superstar Cartridge|LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM|SUPERSTAR|scrc:garage_inserted:v1:superstar",
    "Wag the Dog|Wag the Dog Cartridge|LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM|WAG_THE_DOG|scrc:garage_inserted:v1:wag_the_dog",
};
Equal(
    string.Join("\n", expectedNativeMappings),
    string.Join("\n", GarageCartridgeNativePolicy.RandomizedCartridges.Select(cartridge =>
        $"{cartridge.Song}|{cartridge.ItemName}|{cartridge.NativeBagFlag}|{cartridge.NativeCartridgeType}|{cartridge.ServerInsertionKey}")),
    "all five randomized cartridges use exact verified native mappings");
Equal(false,
    GarageCartridgeNativePolicy.RandomizedCartridges.Any(cartridge =>
        string.Equals(cartridge.Song, "Vampire Killer", StringComparison.Ordinal)),
    "physical Vampire Killer is excluded from native AP reconciliation");

Console.WriteLine("Game Garage availability policy tests passed.");

internal sealed class FakeGaragePreviewData
{
    internal FakeGaragePreviewData(params FakeGarageResult[] results)
    {
        PreviousGarageResults = results.ToList();
    }

    public List<FakeGarageResult> PreviousGarageResults { get; }
}

internal sealed class FakeGarageResult
{
    private bool _owned;

    internal FakeGarageResult(string nativeType, bool owned)
    {
        cartridgeType = nativeType;
        _owned = owned;
    }

    public string cartridgeType { get; }

    public bool cartridgeOwned
    {
        get => _owned;
        set
        {
            _owned = value;
            WriteCount++;
        }
    }

    internal int WriteCount { get; private set; }
}

internal sealed class FakeMalformedGaragePreviewData
{
    internal FakeMalformedGaragePreviewData(params object[] results)
    {
        PreviousGarageResults = results.ToList();
    }

    public List<object> PreviousGarageResults { get; }
}

internal sealed class FakeGarageResultWithoutOwned
{
    internal FakeGarageResultWithoutOwned(string nativeType)
    {
        cartridgeType = nativeType;
    }

    public string cartridgeType { get; }
}
