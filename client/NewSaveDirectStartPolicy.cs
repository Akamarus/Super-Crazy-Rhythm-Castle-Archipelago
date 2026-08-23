namespace RhythmCastleAP;

internal enum NewSaveDirectStartDecision
{
    Ignore,
    Redirect,
}

internal static class NewSaveDirectStartPolicy
{
    internal static NewSaveDirectStartDecision Decide(
        bool enabled,
        bool freshSavePending,
        bool alreadyRedirected) =>
        enabled && freshSavePending && !alreadyRedirected
            ? NewSaveDirectStartDecision.Redirect
            : NewSaveDirectStartDecision.Ignore;
}
