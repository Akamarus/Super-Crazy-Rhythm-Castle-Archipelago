using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RhythmCastleAP;

internal static class CassetteSaveTransactionAdapter
{
    private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static readonly Dictionary<string, Type?> TypeCache = new(StringComparer.Ordinal);

    internal static bool TryGetLoadedSave(out int slot)
    {
        slot = default;
        try
        {
            Type? enquiries = FindType("SaveManagementEnquiries");
            MethodInfo? getSlot = enquiries?.GetMethod(
                "GetSelectedSaveFileSlotNumber", AllStatic, binder: null, types: Type.EmptyTypes, modifiers: null);
            MethodInfo? getState = enquiries?.GetMethod(
                "TryGetSelectedSlotSaveFileState", AllStatic, binder: null, types: Type.EmptyTypes, modifiers: null);
            object? rawSlot = UnwrapNullable(getSlot?.Invoke(null, null));
            object? currentState = UnwrapNullable(getState?.Invoke(null, null));
            if (rawSlot == null || currentState == null) return false;
            slot = Convert.ToInt32(rawSlot);
            return true;
        }
        catch
        {
            slot = default;
            return false;
        }
    }

    internal static bool TryReadCassetteStatus(
        object processor,
        string nativeSong,
        out string? nativeStatus)
    {
        nativeStatus = null;
        try
        {
            MethodInfo? obtainState = processor.GetType().GetMethods(AllInstance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "ObtainState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            object? state = obtainState?.Invoke(processor, null);
            if (state == null) return false;

            Type? songType = FindType("ePlayableSong", processor.GetType().Assembly);
            MethodInfo? readStatus = songType == null ? null : state.GetType().GetMethod(
                "GetCassetteStatusForSong", AllInstance, binder: null, types: new[] { songType }, modifiers: null);
            if (readStatus == null || songType?.IsEnum != true) return false;

            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            nativeStatus = UnwrapNullable(readStatus.Invoke(state, new[] { song }))?.ToString();
            return !string.IsNullOrWhiteSpace(nativeStatus);
        }
        catch
        {
            nativeStatus = null;
            return false;
        }
    }

    internal static bool TryGetPersistBundle(
        object request,
        out object? nativeBundle,
        out string bundleName)
    {
        nativeBundle = null;
        bundleName = string.Empty;
        try
        {
            Type requestType = request.GetType();
            if (string.Equals(requestType.Name, "PersistSaveChangeBundleRequest", StringComparison.Ordinal))
            {
                PropertyInfo? property = requestType.GetProperty("Bundle", AllInstance);
                FieldInfo? field = property == null ? requestType.GetField("Bundle", AllInstance) : null;
                nativeBundle = UnwrapNullable(property?.GetValue(request) ?? field?.GetValue(request));
            }
            else if (string.Equals(requestType.Name, "PersistAllSaveChangeBundlesRequest", StringComparison.Ordinal))
            {
                Type? bundleType = FindType("ePlayerSaveChangeBundleKey", requestType.Assembly);
                if (bundleType?.IsEnum == true)
                    nativeBundle = Enum.Parse(bundleType, CassetteNativeRequestFactory.DefaultBundle, ignoreCase: false);
            }

            bundleName = nativeBundle?.ToString() ?? string.Empty;
            return nativeBundle != null && !string.IsNullOrWhiteSpace(bundleName);
        }
        catch
        {
            nativeBundle = null;
            bundleName = string.Empty;
            return false;
        }
    }

    internal static bool TryStageHaveInBag(
        object processor,
        string nativeSong,
        object nativeBundle,
        out string detail)
    {
        detail = string.Empty;
        try
        {
            Assembly assembly = processor.GetType().Assembly;
            Type? requestType = FindType("RecordSongCassetteStatusInSaveDataRequest", assembly);
            Type? songType = FindType("ePlayableSong", assembly);
            Type? statusType = FindType("eSongCassetteStatus", assembly);
            Type bundleType = nativeBundle.GetType();
            if (requestType == null || songType == null || statusType == null)
            {
                detail = "cassette request semantic types unavailable";
                return false;
            }
            MethodInfo? process = processor.GetType().GetMethod(
                "ProcessRequest", AllInstance, binder: null, types: new[] { requestType }, modifiers: null);
            if (process == null)
            {
                detail = "matching cassette ProcessRequest overload unavailable";
                return false;
            }

            object cassetteRequest = CassetteNativeRequestFactory.Create(
                requestType,
                songType,
                statusType,
                bundleType,
                nativeSong,
                CassetteNativeRequestFactory.HaveInBag,
                nativeBundle);
            process.Invoke(processor, new[] { cassetteRequest });
            detail = $"song='{nativeSong}' status='{CassetteNativeRequestFactory.HaveInBag}' bundle='{nativeBundle}'";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
    }

    private static Type? FindType(string exactName, Assembly? preferredAssembly = null)
    {
        if (preferredAssembly != null)
        {
            Type? preferred = GetLoadableTypes(preferredAssembly)
                .FirstOrDefault(type => string.Equals(type.Name, exactName, StringComparison.Ordinal));
            if (preferred != null) return preferred;
        }

        lock (TypeCache)
        {
            if (TypeCache.TryGetValue(exactName, out Type? cached)) return cached;
            Type? found = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(GetLoadableTypes)
                .FirstOrDefault(type => string.Equals(type.Name, exactName, StringComparison.Ordinal));
            if (found != null) TypeCache[exactName] = found;
            return found;
        }
    }

    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.OfType<Type>(); }
        catch { return Array.Empty<Type>(); }
    }

    private static object? UnwrapNullable(object? value)
    {
        if (value == null) return null;
        Type type = value.GetType();
        if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(Nullable<>)) return value;
        return type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);
    }
}
