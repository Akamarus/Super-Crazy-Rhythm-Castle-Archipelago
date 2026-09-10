using System.Reflection;

namespace RhythmCastleAP;

internal static class NativeOverrideTests
{
    internal static void Run(Func<Dictionary<string, object>> contract)
    {
        var failures = new List<string>();
        MusicLabPointRandomization.Reset();
        MusicLabPointRandomization.ApplySlotData(contract(), "game", "native-read", "slot", 300);
        MusicLabPointRandomization.SynchronizeHistory(300, new[] { new MusicLabPointReceipt(0, 187256155) });
        DeveloperHarness.CurrentRoomId = "GameRoom_Hub6";
        DeveloperHarness.Enabled = true;
        ScoreFixture.SetOverride(null);
        MusicLabPointOverride.BindNativeGetter(typeof(ScoreFixture).GetMethod(nameof(ScoreFixture.NativeGetter))!);
        var nativeRead = typeof(MusicLabPointOverride).GetMethod("ReadNativeScore", BindingFlags.Static | BindingFlags.NonPublic)!;
        MusicLabPointOverridePatches.SuppressOverride = false;
        int actualNative = (int)nativeRead.Invoke(null, null)!;
        if (actualNative != 37) failures.Add($"deliberate production native read: expected 37, got {actualNative}");
        if (MusicLabPointOverridePatches.SuppressOverride) failures.Add("native read did not restore suppression");
        ScoreFixture.Reads = 0;
        MusicLabPointOverride.CycleToNextRewardThreshold();
        if (MusicLabPointOverride.OverrideScore != null)
            failures.Add($"production Shift+F4 cycle while AP owns score: expected null, got {MusicLabPointOverride.OverrideScore}");
        if (ScoreFixture.Reads != 0) failures.Add($"AP cycle must no-op before native access: expected 0 reads, got {ScoreFixture.Reads}");
        if (failures.Count != 0) throw new InvalidOperationException(string.Join("; ", failures));

        foreach (string mode in new[] { "awaiting", "synchronized", "retained", "incompatible" })
        foreach (string room in new[] { "GameRoom_Hub6", "GameRoom_Hub2" })
        {
            MusicLabPointRandomization.Reset();
            var data = contract();
            if (mode == "incompatible") data.Remove("music_lab_point_total_value");
            MusicLabPointRandomization.ApplySlotData(data, "game", "cycle", "slot", 301);
            if (mode is "synchronized" or "retained")
                MusicLabPointRandomization.SynchronizeHistory(301, new[] { new MusicLabPointReceipt(0, 187256155) });
            if (mode == "retained") MusicLabPointRandomization.OnDisconnected(301);
            DeveloperHarness.CurrentRoomId = room;
            ScoreFixture.SetOverride(64);
            ScoreFixture.Reads = 0;
            MusicLabPointOverride.CycleToNextRewardThreshold();
            if (MusicLabPointOverride.OverrideScore != 64 || ScoreFixture.Reads != 0)
                throw new InvalidOperationException($"AP-owned cycle mutated developer state in {mode}/{room}");
        }
        MusicLabPointRandomization.Reset();
        DeveloperHarness.CurrentRoomId = "GameRoom_Hub6";
        ScoreFixture.SetOverride(null);
        MusicLabPointOverride.CycleToNextRewardThreshold();
        if (MusicLabPointOverride.OverrideScore != 46)
            throw new InvalidOperationException("native developer cycle must still advance from 37 to 46");
        ScoreFixture.SetOverride(null);
        Console.WriteLine("PASS: compiled_native_read_bypass_and_developer_cycle_noop");
    }
}
