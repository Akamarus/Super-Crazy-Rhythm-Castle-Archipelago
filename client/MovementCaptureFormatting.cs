using System.Globalization;
using System.Reflection;
using System.Text;
namespace RhythmCastleAP;

// Reflection only over a capped list at capture cadence; never executes restriction votes.
internal static class MovementCaptureFormatting
{
    public static int ActorPriority(string path)
    {
        if (path.Contains("Detector", StringComparison.OrdinalIgnoreCase)) return 3;
        if (path.Contains("PlayerCharacter", StringComparison.OrdinalIgnoreCase)) return 0;
        if (path.Contains("Character", StringComparison.OrdinalIgnoreCase)) return 1;
        return 2;
    }

    public static string ActorLabel(string path, int bodyId) => path + "#body:" + bodyId.ToString(CultureInfo.InvariantCulture);

    private static readonly Dictionary<Type, PropertyInfo[]> DetailProperties = new();
    private static readonly string[] DetailNames = { "HasValue", "Value", "MovementSinceLastFrameMagnitude", "SmoothedMeasuredVelocityMagnitude", "MovementSinceLastFrame", "SmoothedMeasuredVelocity", "Position", "Time", "WorldPosition", "Identifier", "WorldEntityIdentifier" };
    public static string Details(object? value) => DetailsAtDepth(value, 0);
    private static string DetailsAtDepth(object? value, int depth)
    {
        if (value == null) return "null";
        if (value is IFormattable scalar) return scalar.ToString(null, CultureInfo.InvariantCulture) ?? "unavailable";
        if (value is bool || value is string || value.GetType().IsEnum) return value.ToString()!;
        if (depth >= 2) return "unavailable:depth-limit";
        var type = value.GetType();
        if (!DetailProperties.TryGetValue(type, out var properties))
        {
            properties = DetailNames.Select(name => type.GetProperty(name)).Where(p => p != null && p.GetIndexParameters().Length == 0).Cast<PropertyInfo>().ToArray();
            DetailProperties[type] = properties;
        }
        var parts = new List<string>();
        foreach (var property in properties)
        {
            try
            {
                object? field = property.GetValue(value);
                parts.Add(property.Name + "=" + DetailsAtDepth(field, depth + 1));
                if (property.Name == "HasValue" && field is false) break;
            }
            catch { parts.Add(property.Name + "=unavailable"); }
        }
        return parts.Count == 0 ? "unavailable:" + type.Name : "{" + string.Join(";", parts) + "}";
    }

    public static string NullableInt(object? value)
    {
        if (value == null) return "null";
        if (value is int number) return number.ToString(CultureInfo.InvariantCulture);
        try
        {
            var type = value.GetType();
            var present = type.GetProperty("HasValue")?.GetValue(value);
            if (present is false) return "null";
            if (present is true && type.GetProperty("Value")?.GetValue(value) is int nativeNumber)
                return nativeNumber.ToString(CultureInfo.InvariantCulture);
        }
        catch { }
        return "unavailable";
    }

    public static string Restrictors(object? list)
    {
        if (list == null) return "unavailable";
        try
        {
            var type = list.GetType();
            if (type.GetProperty("Count")?.GetValue(list) is not int count || count < 0) return "unavailable";
            var item = type.GetProperty("Item", new[] { typeof(int) });
            if (count > 0 && item == null) return "unavailable";
            var result = new StringBuilder("count:").Append(count).Append(" [");
            for (int index = 0; index < Math.Min(count, 16); index++)
            {
                if (index > 0) result.Append(", ");
                try { result.Append(Identity(item!.GetValue(list, new object[] { index }))); }
                catch { result.Append("unavailable"); }
            }
            if (count > 16) result.Append(", truncated");
            return result.Append(']').ToString();
        }
        catch { return "unavailable"; }
    }

    private static string Identity(object? value)
    {
        if (value == null) return "null";
        var type = value.GetType();
        // IL2CPP lists may expose interface/base wrappers. CLR GetType is not the native runtime type.
        object? nativeType = type.GetMethod("GetIl2CppType", Type.EmptyTypes)?.Invoke(value, null);
        string name = nativeType?.GetType().GetProperty("FullName")?.GetValue(nativeType)?.ToString()
            ?? "runtime-type-unavailable(wrapper:" + type.FullName + ")";
        // Restrictors may be ordinary IL2CPP objects, with no Unity instance ID.
        if (type.GetMethod("GetInstanceID", Type.EmptyTypes)?.Invoke(value, null) is int id)
            return name + "#unity:" + id.ToString(CultureInfo.InvariantCulture);
        if (type.GetProperty("Pointer")?.GetValue(value) is IntPtr pointer && pointer != IntPtr.Zero)
            return name + "#ptr:0x" + pointer.ToInt64().ToString("x", CultureInfo.InvariantCulture);
        return name + "#identity-unavailable";
    }
}
