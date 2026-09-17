using System.Reflection;

namespace RhythmCastleAP;

// Current Area Access presentation behavior; independent of retired level gates.
internal static class RootsPostLevelOnePresentation
{
    private static bool _levelOnePersisted;

    internal static void RecordLevelPersisted(string level)
    {
        if (string.Equals(level, "Level_05", StringComparison.OrdinalIgnoreCase))
            _levelOnePersisted = true;
    }

    internal static void NewSaveCreated() => _levelOnePersisted = false;

    internal static bool BeforeSequence(
        bool enabled, bool compatible, MethodBase method, object? instance,
        Action<string>? report = null)
    {
        if (instance == null || method.Name != "Begin" ||
            !RootsPresentationPolicy.ShouldSuppressPostLevelOne(
                enabled, compatible, method.DeclaringType?.Name, _levelOnePersisted))
            return true;

        bool markedComplete = false;
        try
        {
            MethodInfo? mark = instance.GetType().GetMethod(
                "MarkAsComplete", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (mark != null && mark.GetParameters().Length == 0)
            {
                mark.Invoke(instance, Array.Empty<object>());
                markedComplete = true;
            }
        }
        catch (Exception ex)
        {
            report?.Invoke($"[SCRC-AP] Failed to MarkAsComplete difficulty sequence: {ex.GetBaseException()}");
        }
        report?.Invoke(markedComplete
            ? "[SCRC-AP] REDUNDANT POST-LEVEL-1 ROOTS DIFFICULTY PRESENTATION SUPPRESSED and marked complete."
            : "[SCRC-AP] REDUNDANT POST-LEVEL-1 ROOTS DIFFICULTY PRESENTATION SUPPRESSED but completion could not be marked.");
        return false;
    }
}
