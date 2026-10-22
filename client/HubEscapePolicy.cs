namespace RhythmCastleAP;

internal static class HubEscapePolicy
{
    private const string GateRoot = "Root/GameRoom_Hub5A_Logic/Objects/ExternalDoor/ExternalEntrance/";
    internal static bool? GateActive(bool enabled, bool compatible, bool ownsCellTower, string path)
    {
        if (!enabled || !compatible || !ownsCellTower) return null;
        return path switch {
            GateRoot + "DoorToHR5B_Open" => true,
            GateRoot + "DoorToHR5B_Closed" => false,
            _ => null,
        };
    }

    internal static bool ClearExitRequirement(bool enabled, bool compatible, bool ownsCellTower, string path) =>
        enabled && compatible && ownsCellTower && path == GateRoot.TrimEnd('/');

    internal static bool RedirectLobbyExit(bool enabled, bool compatible,
        string? origin, string? destination, bool? fishTearsDelivered) =>
        enabled && compatible && origin == "GameRoom_Hub5A" &&
        (destination == "GameRoom_Hub1A" || destination == "GameRoom_Hub1B") &&
        fishTearsDelivered != true;
}
