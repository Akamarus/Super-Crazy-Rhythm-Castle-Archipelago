using System.Reflection;

namespace RhythmCastleAP;

internal static class NativeProgressionFlags
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, object[]> Arguments = new(StringComparer.Ordinal);
    private static Assembly? _assembly;
    private static Type? _flagType;
    private static MethodInfo? _getter;

    internal static bool? Read(string flag)
    {
        lock (Sync)
        {
            try
            {
                Assembly? assembly = ReflectionUtil.GameAssembly;
                if (assembly == null) return null;
                if (assembly != _assembly)
                {
                    Type? type = assembly.GetType("eGameProgressionFlag");
                    if (type?.IsEnum != true) return null;
                    MethodInfo? getter = assembly.GetType("CurrentPlayerSaveEnquiries")?.GetMethod(
                        "GetProgressionFlagValue", BindingFlags.Public | BindingFlags.Static, null, new[] { type }, null);
                    if (getter?.ReturnType != typeof(bool)) return null;
                    Arguments.Clear();
                    _flagType = type;
                    _getter = getter;
                    _assembly = assembly;
                }
                // Only the enum argument is reusable. Ownership belongs to the current native save.
                if (!Arguments.TryGetValue(flag, out object[]? arguments))
                {
                    if (!Enum.TryParse(_flagType!, flag, false, out object? value) || value == null) return null;
                    Arguments.Add(flag, arguments = new[] { value });
                }
                return _getter!.Invoke(null, arguments) as bool?;
            }
            catch { return null; }
        }
    }
}
