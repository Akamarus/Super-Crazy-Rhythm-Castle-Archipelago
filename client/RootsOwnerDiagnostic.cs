using System.Collections;
using System.Reflection;
using UnityEngine;

namespace RhythmCastleAP;

internal static class RootsOwnerDiagnostic
{
    internal static void Postfix(object? __instance)
    {
        if (__instance is not Component component || component.transform == null)
            return;

        string path = BuildPath(component.transform);
        if (!RootsOwnerDiagnosticPolicy.ShouldInspect(DeveloperHarness.CurrentRoomId, path))
            return;

        object? resolved = ReflectionUtil.ReadMember(__instance, "resolved");
        object? roomStart = ReflectionUtil.ReadMember(__instance, "roomStartCondition");
        object? occur = ReflectionUtil.ReadMember(__instance, "occurCondition");
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS OWNER READ path='{path}' resolved={resolved ?? "<null>"} " +
            $"roomStartCondition='{Describe(roomStart)}' occurCondition='{Describe(occur)}' " +
            $"readOnly=True mutationRequested={RootsOwnerDiagnosticPolicy.RequestsMutation}.");
        DumpList(__instance, "triggerOnSkip");
        DumpList(__instance, "triggerOnOccur");
    }

    private static void DumpList(object owner, string member)
    {
        object? raw = ReflectionUtil.ReadMember(owner, member);
        int index = 0;
        int nativeCount = ReadCount(raw);
        try
        {
            if (raw is IEnumerable enumerable)
            {
                foreach (object? entry in enumerable)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] ROOTS OWNER LIST member='{member}' index={index++} entry='{Describe(entry)}' " +
                        $"members='{DescribeMembers(entry)}' readOnly=True.");
                }
            }

            if (index == 0 && raw != null && nativeCount > 0)
            {
                MethodInfo? getter = raw.GetType().GetMethod(
                    "get_Item",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    new[] { typeof(int) },
                    null);
                for (int i = 0; i < nativeCount && i < 32; i++)
                {
                    object? entry = getter?.Invoke(raw, new object[] { i });
                    object? hasValue = entry == null ? null : ReflectionUtil.ReadMember(entry, "HasValue");
                    object? target = entry == null ? null : ReflectionUtil.ReadMember(entry, "Value");
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] ROOTS OWNER LIST member='{member}' index={i} entry='{Describe(entry)}' " +
                        $"hasValue={hasValue ?? "<null>"} target='{Describe(target)}' " +
                        $"targetMembers='{DescribeMembers(target)}' access='get_Item+Value' readOnly=True.");
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS OWNER LIST member='{member}' enumeration failed: {ex.GetBaseException().Message}");
        }
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS OWNER LIST member='{member}' enumerated={index} nativeCount={nativeCount} rawType='{raw?.GetType().FullName ?? "<null>"}' readOnly=True.");
    }

    private static int ReadCount(object? list)
    {
        object? raw = list == null ? null : ReflectionUtil.ReadMember(list, "Count");
        if (raw is int count) return count;
        return int.TryParse(raw?.ToString(), out int parsed) ? parsed : -1;
    }

    private static string DescribeMembers(object? value)
    {
        if (value == null) return "<null>";
        var parts = new List<string>();
        for (Type? type = value.GetType(); type != null && parts.Count < 24; type = type.BaseType)
        {
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (parts.Count >= 24) break;
                try { parts.Add($"{field.Name}={Describe(field.GetValue(value))}"); } catch { }
            }
        }
        return string.Join("|", parts);
    }

    private static string Describe(object? value)
    {
        if (value == null) return "<null>";
        if (value is Component component && component.transform != null)
            return $"{value.GetType().FullName}@{BuildPath(component.transform)}";
        if (value is GameObject go && go.transform != null)
            return $"GameObject@{BuildPath(go.transform)}";
        return $"{value.GetType().FullName}:{value}";
    }

    private static string BuildPath(Transform transform)
    {
        var names = new List<string>();
        for (Transform? current = transform; current != null && names.Count < 40; current = current.parent)
            names.Add(current.name ?? "<unnamed>");
        names.Reverse();
        return string.Join("/", names);
    }
}
