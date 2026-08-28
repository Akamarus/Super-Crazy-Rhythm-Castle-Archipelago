using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(2, MusicLabBarrierPolicy.KnownBarrierPaths.Count, "exactly two verified barrier roots");
foreach (string path in MusicLabBarrierPolicy.KnownBarrierPaths)
{
    Equal(true, MusicLabBarrierPolicy.ShouldDisable(true, true, "GameRoom_Hub6", path), $"verified path {path}");
    Equal(false, MusicLabBarrierPolicy.ShouldDisable(false, true, "GameRoom_Hub6", path), "disabled AP");
    Equal(false, MusicLabBarrierPolicy.ShouldDisable(true, false, "GameRoom_Hub6", path), "incompatible slot");
    Equal(false, MusicLabBarrierPolicy.ShouldDisable(true, true, "GameRoom_Hub2", path), "wrong room");
}

Equal(false, MusicLabBarrierPolicy.ShouldDisable(true, true, "GameRoom_Hub6",
    "Root/GameRoom_Hub6_Logic/Objects/Barriers-tofb/Barriers_Lobby/Collision/Box"),
    "barrier child is not a repair target");
Equal(false, MusicLabBarrierPolicy.ShouldDisable(true, true, "GameRoom_Hub6",
    "Root/GameRoom_Hub6_Logic/Objects/RewardChests/Barrier"),
    "similar reward object is untouched");
Equal(false, MusicLabBarrierPolicy.ShouldDisable(true, true, "GameRoom_Hub6",
    "Root/GameRoom_Hub6_Logic/Objects/CassetteMachines/Barrier"),
    "cassette machine is untouched");

Console.WriteLine("Music Lab barrier policy tests passed.");
