namespace RhythmCastleAP;
internal static class CellTowerPathPolicy
{
    internal const string RaisedPath = "Root/GameRoom_Hub5B_Logic/Objects/Cell_OldTom/HangingCell_Raised";
    internal static bool ShouldOpen(bool enabled, bool compatible, bool ownsCellTower, string path) =>
        enabled && compatible && ownsCellTower && path == RaisedPath;
}
