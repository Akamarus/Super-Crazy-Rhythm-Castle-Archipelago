namespace RhythmCastleAP;
internal static class LobbyDoorPolicy
{
    internal const string Training="Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19/Level19Door";
    internal const string School="Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level20/Level20Door";
    internal static string? Cover(bool enabled,bool authenticated,bool owned,string path) =>
        !enabled || !authenticated || !owned ? null : path==Training ? "LevelLockedCover-tofb" : path==School ? "LevelLockedCover" : null;
    internal static bool MatchesCondition(string path,string type,int? flag) =>
        path==Training && type=="IsGameProgressionFlagSetCondition" && flag==649 ||
        path==School && type=="LevelCompleteCondition";
}
