using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

var pickup = LobbyItemRandomizationPolicy.SourceLocations(
    enabled: true, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", value: true);
Equal(2, pickup.Count, "one pickup maps to two future source locations");
Equal("Lobby - Important Letters Pickup", pickup[0], "letters check");
Equal("Lobby - Bean Trumpet Award", pickup[1], "trumpet check");
Equal(0, LobbyItemRandomizationPolicy.SourceLocations(true, "GameRoom_Hub1B", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", true).Count, "wrong room");
Equal(0, LobbyItemRandomizationPolicy.SourceLocations(true, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", false).Count, "not collected");
Equal(0, LobbyItemRandomizationPolicy.SourceLocations(false, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", true).Count, "feature disabled");

Equal(true, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM", true, false), "letters grant");
Equal(true, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1A", "BEAN_TRUMPET_ABILITY", true, false), "dash grant");
Equal(false, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED", true, false), "collected marker survives");
Equal(false, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM", false, false), "letters consumption survives");
Equal(false, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1A", "BEAN_TRUMPET_ABILITY", true, true), "AP grant survives");
Equal(false, LobbyItemRandomizationPolicy.SuppressNativeGrant(true, "GameRoom_Hub1B", "LOBBY_HUB_MENIAL_TASK_ITEM", true, false), "wrong room grant survives");

Equal(LobbyItemGrantDecision.Apply, LobbyItemRandomizationPolicy.LettersGrant(true, true, false, false), "new letters delivery");
Equal(LobbyItemGrantDecision.AlreadyHeld, LobbyItemRandomizationPolicy.LettersGrant(true, true, true, false), "held letters replay");
Equal(LobbyItemGrantDecision.Consumed, LobbyItemRandomizationPolicy.LettersGrant(true, true, false, true), "mail delivery prevents restoration");
Equal(LobbyItemGrantDecision.Ignore, LobbyItemRandomizationPolicy.LettersGrant(false, true, false, false), "not received");
Equal(LobbyItemGrantDecision.Ignore, LobbyItemRandomizationPolicy.LettersGrant(true, false, false, false), "incompatible seed");
Equal(LobbyItemGrantDecision.Apply, LobbyItemRandomizationPolicy.TrumpetGrant(true, true, false), "new dash delivery");
Equal(LobbyItemGrantDecision.AlreadyHeld, LobbyItemRandomizationPolicy.TrumpetGrant(true, true, true), "dash replay");

Console.WriteLine("Lobby item policy tests passed.");
