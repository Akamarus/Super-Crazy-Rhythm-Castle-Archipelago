using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

Equal(
    NewSaveDirectStartDecision.Redirect,
    NewSaveDirectStartPolicy.Decide(enabled: true, freshSavePending: true, alreadyRedirected: false),
    "new save redirects");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: false, freshSavePending: true, alreadyRedirected: false),
    "disabled setting leaves new save alone");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, freshSavePending: false, alreadyRedirected: false),
    "established save does not redirect");
Equal(
    NewSaveDirectStartDecision.Ignore,
    NewSaveDirectStartPolicy.Decide(enabled: true, freshSavePending: true, alreadyRedirected: true),
    "redirect is one shot");

Console.WriteLine("New-save direct-start policy tests passed.");
