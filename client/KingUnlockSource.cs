using System.Reflection;
namespace RhythmCastleAP;

// Only the audited native victory sequence can arm this source. AP grants submit
// save requests directly, and character ownership polling never completes a check.
internal static class KingUnlockSource
{
    internal const long LocationId = 187256335;
    internal const string SourcePath = "Root/GameRoom_28B_Logic/GameRoom_28B_Script/MiscSequences/End_Win/EarnKingSequence/AwardKing";
    private static Assembly? _assembly;
    private static MethodInfo? _getter;
    private static object? _king;
    internal static bool? ReadCharacter(string character)
    {
        if (character != "KING") return null;
        try {
            var assembly = ReflectionUtil.GameAssembly;
            if (assembly == null) return null;
            if (!ReferenceEquals(assembly, _assembly)) {
                _assembly = assembly; _getter = null; _king = null;
                Type? type = assembly.GetType("ePlayableCharacter");
                if (type?.IsEnum != true || !Enum.TryParse(type, "KING", false, out _king) || Convert.ToInt32(_king) != 6) return null;
                _getter = assembly.GetType("CurrentPlayerSaveEnquiries")?.GetMethod("IsCharacterUnlocked",
                    BindingFlags.Public | BindingFlags.Static, null, new[] { type }, null);
                if (_getter?.ReturnType != typeof(bool)) _getter = null;
            }
            return _getter?.Invoke(null, new[] { _king }) as bool?;
        } catch { return null; }
    }
    internal static void RunNative(string path, Action original)
    {
        string? context = path == SourcePath ? ExpandedChecks.BeforeKingUnlock() : null;
        original(); // Never suppress, replace, or repeat the native character reward.
        if (context != null) ExpandedChecks.AfterKingUnlock(context);
    }
}
