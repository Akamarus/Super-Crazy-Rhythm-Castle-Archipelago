using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(
    NewSaveDirectStartDecision.Redirect,
    NewSaveDirectStartPolicy.Decide(enabled: true, compatible: true, freshSavePending: true, alreadyRedirected: false, roomId: "GameRoom_04A"),
    "fresh compatible AP save redirects");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: false, compatible: true, freshSavePending: true, alreadyRedirected: false, roomId: "GameRoom_04A"),
    "disabled setting leaves new save alone");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, compatible: false, freshSavePending: true, alreadyRedirected: false, roomId: "GameRoom_04A"),
    "incompatible slot leaves new save alone");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, compatible: true, freshSavePending: false, alreadyRedirected: false, roomId: "GameRoom_04A"),
    "established save does not redirect");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, compatible: true, freshSavePending: true, alreadyRedirected: true, roomId: "GameRoom_04A"),
    "redirect is one shot");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, compatible: true, freshSavePending: true, alreadyRedirected: false, roomId: "GameRoom_Hub2"),
    "unrelated transition does not redirect");

Equal(true, NewSaveDirectStartPolicy.ShouldUseLegacyFallback(true, true, "GameRoom_04A"), "exact legacy rescue");
Equal(false, NewSaveDirectStartPolicy.ShouldUseLegacyFallback(true, false, "GameRoom_04A"), "legacy rescue requires compatible slot");
Equal(false, NewSaveDirectStartPolicy.ShouldUseLegacyFallback(true, true, "GameRoom_Hub2"), "legacy rescue requires intro room");

Console.WriteLine("New-save direct-start policy tests passed.");
