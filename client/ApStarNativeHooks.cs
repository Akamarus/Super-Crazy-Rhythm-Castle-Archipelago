using System.Reflection;
using HarmonyLib;

namespace RhythmCastleAP;

// These are exact installed-interop methods. Harmony provides the IL2CPP boundary;
// do not replace them with unmanaged callbacks for by-value request payloads.
internal static class ApStarNativeHooks
{
    private static Func<int, int> _resolveTotal = value => value;
    private static Func<string, string, bool> _canEnter = (_, _) => true;
    private static Action<string, string> _reportBlocked = (_, _) => { };
    private static Action<string, string>? _admittedStart;
    private static Action<string, string>? _previewObserved;
    private static MethodInfo? _obtainPreviewState;
    internal static bool Ready { get; private set; }

    internal static int Install(Harmony harmony, Func<int, int> resolveTotal,
        Func<string, string, bool> canEnter, Action<string, string> reportBlocked,
        Action<string, string>? admittedStart = null, Action<string, string>? previewObserved = null)
    {
        _resolveTotal = resolveTotal;
        _canEnter = canEnter;
        _reportBlocked = reportBlocked;
        _admittedStart = admittedStart;
        _previewObserved = previewObserved;
        int count = 0;
        count += Patch(harmony, "CurrentPlayerSaveEnquiries", "GetTotalNumStars", typeof(int),
            Array.Empty<string>(), nameof(TotalPostfix), false, true);
        count += Patch(harmony, "GameFlowRequestProcessor", "ProcessRequest", typeof(void),
            new[] { "StartLevelRequest" }, nameof(StartPrefix), true, false);
        count += Patch(harmony, "LevelPreviewUI", "CanEnterDisplayedLevelVariant", typeof(bool),
            new[] { "Boolean" }, nameof(PreviewPostfix), false, false);
        Ready = count == 3 && _obtainPreviewState != null;
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] AP STAR NATIVE HOOKS installed={count}/3 ready={Ready}; live gate and display acceptance remains required.");
        return count;
    }

    private static int Patch(Harmony harmony, string ownerName, string methodName,
        Type returnType, string[] parameters, string callback, bool prefix, bool isStatic)
    {
        try
        {
            Assembly game = ReflectionUtil.GameAssembly ?? throw new InvalidOperationException("Game assembly unavailable");
            Type owner = ReflectionUtil.SafeGetTypes(game).Single(t => t.Name == ownerName);
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            MethodInfo target = owner.GetMethods(flags).Single(m => m.Name == methodName &&
                m.ReturnType == returnType && m.GetParameters().Select(p => p.ParameterType.Name).SequenceEqual(parameters));
            if (ownerName == "LevelPreviewUI")
                _obtainPreviewState = owner.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Single(m => m.Name == "ObtainState" && m.GetParameters().Length == 0);
            var patch = new HarmonyMethod(typeof(ApStarNativeHooks).GetMethod(callback, BindingFlags.Static | BindingFlags.NonPublic)!);
            harmony.Patch(target, prefix: prefix ? patch : null, postfix: prefix ? null : patch);
            return 1;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError($"[SCRC-AP] AP STAR HOOK unavailable target='{ownerName}.{methodName}' reason='{ex.GetBaseException().Message}'.");
            return 0;
        }
    }

    private static void TotalPostfix(ref int __result) => __result = _resolveTotal(__result);

    private static bool StartPrefix(object[]? __args)
    {
        object? request = ReflectionUtil.FindArg(__args, "StartLevelRequest");
        return Admit(request, "LevelIdentifier", "Variant", report: true);
    }

    private static void PreviewPostfix(object __instance, ref bool __result)
    {
        bool nativeAllowed = __result; // Retain native restrictions, including versus player count.
        try
        {
            object? state = _obtainPreviewState?.Invoke(__instance, null);
            bool apAllowed = Admit(state, "LevelIdentifier", "LevelVariantIdentifier", report: false);
            __result = nativeAllowed && apAllowed;
        }
        catch (Exception ex)
        {
            // Policy decides whether unknown identities fail closed for the active contract.
            __result = nativeAllowed && _canEnter(string.Empty, string.Empty);
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] AP STAR preview identity unavailable: {ex.GetBaseException().Message}");
        }
    }

    private static bool Admit(object? source, string levelMember, string variantMember, bool report)
    {
        string level = ReflectionUtil.ExtractIdentifier(ReflectionUtil.ReadMember(source, levelMember)) ?? string.Empty;
        string variant = ReflectionUtil.ExtractIdentifier(ReflectionUtil.ReadMember(source, variantMember)) ?? string.Empty;
        bool allowed = _canEnter(level, variant);
        if (!allowed && report) _reportBlocked(level, variant);
        if (allowed && report) _admittedStart?.Invoke(level, variant);
        if (!report) _previewObserved?.Invoke(level, variant);
        return allowed;
    }
}
