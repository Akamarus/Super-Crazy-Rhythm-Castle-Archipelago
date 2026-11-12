using System.Reflection;

namespace RhythmCastleAP;

internal static class NativeProgressionFlagRequest
{
    internal static bool TrySubmit(
        object processor, string flagName, bool value, ref bool applyingNativeGrant, out string detail)
    {
        detail = string.Empty;
        try
        {
            Assembly? gameAssembly = ReflectionUtil.GameAssembly;
            Type? requestType = gameAssembly?.GetType("RecordGameProgressionInSaveDataRequest", throwOnError: false, ignoreCase: false);
            if (requestType == null)
            {
                detail = "RecordGameProgressionInSaveDataRequest type not found.";
                return false;
            }

            Type? flagType = requestType.GetProperty(
                    "Flag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.PropertyType
                ?? requestType.GetField(
                    "_Flag_k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType;
            if (flagType == null || !flagType.IsEnum)
            {
                detail = "progression flag enum type not found.";
                return false;
            }

            object flagValue;
            try { flagValue = Enum.Parse(flagType, flagName, ignoreCase: false); }
            catch (Exception ex)
            {
                detail = $"enum value '{flagName}' unavailable: {ex.GetBaseException().Message}";
                return false;
            }

            object? request = null;
            string ctorDescription = string.Empty;
            foreach (ConstructorInfo ctor in requestType.GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     .Where(c => !c.GetParameters().Any(p => p.ParameterType == typeof(IntPtr)))
                     .OrderBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ps;
                try { ps = ctor.GetParameters(); }
                catch { continue; }

                var args = new object?[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    Type pt = ps[i].ParameterType;
                    if (pt == flagType)
                        args[i] = flagValue;
                    else if (pt == typeof(bool))
                        args[i] = value;
                    else if (pt.IsEnum)
                    {
                        try { args[i] = Enum.Parse(pt, "INVALID", ignoreCase: true); }
                        catch { args[i] = Activator.CreateInstance(pt); }
                    }
                    else if (pt.IsValueType)
                        args[i] = Activator.CreateInstance(pt);
                    else
                        args[i] = null;
                }

                try
                {
                    request = ctor.Invoke(args);
                    ctorDescription = $"ctor({string.Join(",", ps.Select(p => p.ParameterType.Name))})";
                    if (request != null)
                        break;
                }
                catch { request = null; }
            }

            if (request == null)
            {
                try
                {
                    request = Activator.CreateInstance(requestType, nonPublic: true);
                    ctorDescription = "Activator.CreateInstance";
                }
                catch (Exception ex)
                {
                    detail = $"could not construct request: {ex.GetBaseException().Message}";
                    return false;
                }
            }

            if (request == null)
            {
                detail = "native request construction returned null";
                return false;
            }
            // A wrapper can expose setters, backing fields, or constructor-initialized members.
            // These writes are best effort. A successful submission still needs
            // a separate read of native state to confirm the flag changed.
            TryWriteMember(request, "Flag", flagValue);
            TryWriteMember(request, "_Flag_k__BackingField", flagValue);
            TryWriteMember(request, "Value", value);
            TryWriteMember(request, "_Value_k__BackingField", value);

            MethodInfo? process = processor.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "ProcessRequest", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] ps;
                    try { ps = m.GetParameters(); }
                    catch { return false; }
                    return ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(request);
                });

            if (process == null)
            {
                detail = "matching PlayerSaveRequestProcessor.ProcessRequest overload not found.";
                return false;
            }

            try
            {
                // ProcessRequest can synchronously enter the caller's Harmony hook.
                // Keep each item's guard separate so other items still see their normal callbacks.
                applyingNativeGrant = true;
                process.Invoke(processor, new[] { request });
            }
            finally
            {
                applyingNativeGrant = false;
            }

            detail = $"via {ctorDescription}";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().ToString();
            return false;
        }
    }

    private static bool TryWriteMember(object obj, string name, object value)
    {
        Type t = obj.GetType();
        try
        {
            PropertyInfo? p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        try
        {
            FieldInfo? f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        return false;
    }
}
