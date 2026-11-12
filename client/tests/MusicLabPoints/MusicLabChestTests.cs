using RhythmCastleAP;

internal static class MusicLabChestTests
{
    internal static void Run()
    {
        var map = new MusicLabChestMap();
        (int Threshold, string ObjectName)[] expected =
        {
            (5, "ManiacMemoryCardChest"), (10, "GarageCartridgeChest_Gradius"),
            (20, "CatBatteryChest"), (32, "SongChest_QuickSand"),
            (46, "GarageCartridgeChest_BloodyTears"), (64, "SongChest_Flamenco"),
            (89, "SongChest_TenFourGoodBuddy"), (111, "SongChest_Zen"), (140, "SongChest_Wiggle")
        };
        Require(map.Chests.Length == expected.Length, "Nine mapped chests");
        foreach (var (threshold, name) in expected)
        {
            var chest = map.At(threshold);
            Require(chest.ObjectName == name && chest.InteractionPath ==
                $"Root/GameRoom_Hub6_Logic/Objects/RewardChests/{name}/Interaction", "Native chest path");
            Require(chest.ConfigKey == $"MusicLab{threshold}PointChestLocationName" &&
                chest.LocationName == $"Music Lab - {threshold} Point Chest", "Existing configuration");
        }
        map.At(46).ConfiguredLocationName = "Customized Bloody Tears chest";
        Require(map.At(46).LocationName == "Customized Bloody Tears chest", "Configured location override");

        map.BeginVisit();
        var reads = new List<int>();
        bool complete = map.Discover(chest =>
        {
            reads.Add(chest.Threshold);
            return chest.Threshold == 46 ? null : 1000 + chest.Threshold;
        });
        Require(!complete && reads.SequenceEqual(expected.Select(row => row.Threshold)), "Partial room load");
        reads.Clear();
        Require(map.Discover(chest => { reads.Add(chest.Threshold); return 1046; }) &&
            reads.SequenceEqual(new[] { 46 }), "Retry only the missing chest");
        for (int i = 0; i < 100; i++)
            Require(map.Discover(_ => throw new Exception("Repeated scene read")), "Fully discovered room");

        map.BeginVisit();
        Require(map.ThresholdFor(1046) == 46, "Previous ID available to early collection events");
        Require(map.Discover(chest => 2000 + chest.Threshold), "Recheck metadata on return");
        Require(map.ThresholdFor(1046) == 0 && map.ThresholdFor(2046) == 46 &&
            map.TryGetFlag(46, out int flag) && flag == 2046, "Both lookups use the new flag");
        map.BeginVisit();
        Require(!map.Discover(_ => { map.BeginVisit(); return 999; }) &&
            map.ThresholdFor(999) == 0, "Discard an interrupted visit's result");
    }

    private static void Require(bool condition, string scenario)
    {
        if (!condition) throw new InvalidOperationException(scenario);
    }
}
