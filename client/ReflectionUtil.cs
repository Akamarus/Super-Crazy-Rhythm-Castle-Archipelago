using System.Reflection;

namespace RhythmCastleAP;

internal static class ReflectionUtil
{
    private static Assembly? _gameAssembly;
    private static int _assemblyRevision;
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Assembly, TypeCatalog> Types = new();
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<(Type, string), MemberReader> Members = new();
    private sealed record TypeCatalog(int Revision, bool Complete, Type[] Values);
    private sealed record MemberReader(PropertyInfo? Property, FieldInfo? Field);
    private static readonly string[] IdentifierMembers = { "id", "ID", "Id", "value", "Value" };
    static ReflectionUtil() => AppDomain.CurrentDomain.AssemblyLoad += (_, _) => Interlocked.Increment(ref _assemblyRevision);

    public static Assembly? GameAssembly => _gameAssembly ??= AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => string.Equals(a.GetName().Name, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        int revision = Volatile.Read(ref _assemblyRevision);
        if (!assembly.IsDynamic && Types.TryGetValue(assembly, out var cached) &&
            (cached.Complete || cached.Revision == revision)) return cached.Values;
        Type[] values; bool complete;
        try { values = assembly.GetTypes(); complete = true; }
        catch (ReflectionTypeLoadException ex) { values = ex.Types.OfType<Type>().ToArray(); complete = false; }
        catch { return Array.Empty<Type>(); }
        // A partial catalog is retried after another assembly loads. Dynamic assemblies
        // can add types without an AssemblyLoad event, so never cache their catalog.
        if (!assembly.IsDynamic) Types[assembly] = new(revision, complete, values);
        return values;
    }

    public static object? FindArg(object[]? args, string typeName) =>
        args?.FirstOrDefault(a => a != null && a.GetType().Name == typeName);

    public static object? ReadMember(object? obj, string name)
    {
        if (obj == null) return null;
        var reader = Members.GetOrAdd((obj.GetType(), name), key =>
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            PropertyInfo? property = null;
            FieldInfo? field = null;
            try {
                property = key.Item1.GetProperty(key.Item2, flags);
                if (property?.GetIndexParameters().Length != 0) property = null;
            } catch { }
            try { field = key.Item1.GetField(key.Item2, flags) ?? key.Item1.GetField($"<{key.Item2}>k__BackingField", flags); }
            catch { }
            return new MemberReader(property, field);
        });
        // Cache metadata only. Read the current object every time, and preserve the
        // old field fallback when a generated/native property getter throws.
        try { if (reader.Property != null) return reader.Property.GetValue(obj); } catch { }
        try { if (reader.Field != null) return reader.Field.GetValue(obj); } catch { }

        return null;
    }

    public static bool? ReadBool(object obj, string name)
    {
        object? v = ReadMember(obj, name);
        if (v is bool b) return b;
        return bool.TryParse(v?.ToString(), out bool parsed) ? parsed : null;
    }

    public static int? ReadInt(object obj, string name)
    {
        object? v = ReadMember(obj, name);
        if (v is int i) return i;
        return int.TryParse(v?.ToString(), out int parsed) ? parsed : null;
    }

    public static object? UnwrapNullable(object? value)
    {
        if (value == null) return null;
        Type t = value.GetType();
        string name = t.FullName ?? t.Name;

        if (!name.Contains("Nullable`1", StringComparison.Ordinal))
            return value;

        object? hasValue = ReadMember(value, "HasValue");
        if (hasValue is bool b && !b) return null;

        object? inner = ReadMember(value, "Value");
        return inner ?? value;
    }

    public static string? ExtractIdentifier(object? value)
    {
        value = UnwrapNullable(value);
        if (value == null) return null;
        if (value is string s) return s;

        foreach (string n in IdentifierMembers)
        {
            object? member = ReadMember(value, n);
            if (member is string ms && !string.IsNullOrWhiteSpace(ms))
                return ms;
        }

        string text = value.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(text)
            && text != value.GetType().Name
            && text != value.GetType().FullName)
            return text;

        return null;
    }
}
