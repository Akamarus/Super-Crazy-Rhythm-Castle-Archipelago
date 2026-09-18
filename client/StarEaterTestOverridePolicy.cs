namespace RhythmCastleAP;

internal sealed record StarEaterTestTarget(string Room, string RootPath, int Required);

internal static class StarEaterTestOverridePolicy
{
    internal static StarEaterTestTarget? Select(string? room, int royal, int bunker)
    {
        royal = Math.Clamp(royal, 0, 40);
        bunker = Math.Clamp(bunker, 0, 66);
        return room switch
        {
            "GameRoom_Hub7" when royal != 40 => new(room,
                "Root/GameRoom_Hub7_Logic/Objects/StarEaterAndGap/StarEater", royal),
            "GameRoom_Hub8" when bunker != 66 => new(room,
                "Root/GameRoom_Hub8_Logic/Objects/NPCs/StarEater", bunker),
            _ => null,
        };
    }
}
