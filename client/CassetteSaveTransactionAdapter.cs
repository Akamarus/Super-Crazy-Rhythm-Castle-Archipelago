using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RhythmCastleAP;

internal readonly record struct CassetteSaveFingerprint(
    string StateType,
    long PlayTimeInSeconds,
    long LastPlayDateTimeUtcTicks,
    string IGotMoneyStatus,
    string BadassStatus);

internal static class CassetteSaveTransactionAdapter
{
    private const BindingFlags AllStatic = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags AllInstance = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private const BindingFlags PublicStatic = BindingFlags.Public | BindingFlags.Static;
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    private static readonly Dictionary<string, Type?> TypeCache = new(StringComparer.Ordinal);

    internal static bool TryGetLoadedSave(out int slot)
    {
        slot = default;
        try
        {
            Type? enquiries = FindType("PlayerSaveManagementEnquiries");
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

    internal static bool TryGetLoadedSaveIdentity(out int slot, out long selectedStatePointer)
        => TryGetLoadedSaveIdentity(out slot, out selectedStatePointer, out _);

    internal static bool TryGetLoadedSaveIdentity(out int slot, out long selectedStatePointer, out string stage)
    {
        slot = default;
        selectedStatePointer = 0;
        stage = "start";
        try
        {
            Type? enquiries = FindType("PlayerSaveManagementEnquiries");
            if (enquiries == null) { stage = "owner-missing"; return false; }
            MethodInfo? getSlot = enquiries?.GetMethod(
                "GetSelectedSaveFileSlotNumber", AllStatic, binder: null, types: Type.EmptyTypes, modifiers: null);
            MethodInfo? getState = enquiries?.GetMethod(
                "TryGetSelectedSlotSaveFileState", AllStatic, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (getSlot == null) { stage = "slot-method-missing"; return false; }
            if (getState == null) { stage = "state-method-missing"; return false; }
            object? rawSlot = UnwrapNullable(getSlot?.Invoke(null, null));
            object? currentState = UnwrapNullable(getState?.Invoke(null, null));
            if (rawSlot == null) { stage = "slot-empty"; return false; }
            try { slot = Convert.ToInt32(rawSlot); }
            catch (Exception ex) { stage = $"slot-convert:{SummarizeException(ex)}"; return false; }
            if (currentState == null) { stage = "state-null"; return false; }
            PropertyInfo? pointerProperty = currentState.GetType().GetProperty(
                "Pointer", BindingFlags.Public | BindingFlags.Instance);
            if (pointerProperty == null) { stage = "pointer-missing"; return false; }
            object? rawPointer = pointerProperty.GetValue(currentState);
            if (rawPointer is not IntPtr pointer) { stage = $"pointer-wrong-type:{rawPointer?.GetType().Name ?? "null"}"; return false; }
            if (pointer == IntPtr.Zero) { stage = "pointer-zero"; return false; }
            selectedStatePointer = pointer.ToInt64();
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            slot = default;
            selectedStatePointer = 0;
            stage = $"invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryGetLoadedSaveFingerprint(out CassetteSaveFingerprint fingerprint, out string stage)
    {
        fingerprint = default;
        stage = "fingerprint-start";
        try
        {
            Type? enquiries = FindType("PlayerSaveManagementEnquiries");
            MethodInfo? getState = enquiries?.GetMethod(
                "TryGetSelectedSlotSaveFileState", PublicStatic, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (enquiries == null) { stage = "fingerprint-owner-missing"; return false; }
            if (getState == null) { stage = "fingerprint-state-method-missing"; return false; }
            object? currentState = UnwrapNullable(getState.Invoke(null, null));
            if (currentState == null) { stage = "fingerprint-state-null"; return false; }

            PropertyInfo? gameStatsProperty = currentState.GetType().GetProperty("GameStats", PublicInstance);
            object? gameStats = gameStatsProperty?.GetValue(currentState);
            if (gameStats == null) { stage = "fingerprint-game-stats-missing"; return false; }

            PropertyInfo? playTimeProperty = gameStats.GetType().GetProperty("PlayTimeInSeconds", PublicInstance);
            object? rawPlayTime = playTimeProperty?.GetValue(gameStats);
            if (rawPlayTime == null) { stage = "fingerprint-play-time-missing"; return false; }
            long playTime;
            try { playTime = Convert.ToInt64(rawPlayTime); }
            catch (Exception ex) { stage = $"fingerprint-play-time-convert:{SummarizeException(ex)}"; return false; }

            PropertyInfo? lastPlayProperty = gameStats.GetType().GetProperty("LastPlayDateTimeUtc", PublicInstance);
            object? lastPlay = lastPlayProperty?.GetValue(gameStats);
            PropertyInfo? ticksProperty = lastPlay?.GetType().GetProperty("Ticks", PublicInstance);
            object? rawTicks = ticksProperty?.GetValue(lastPlay);
            if (rawTicks == null) { stage = "fingerprint-last-play-ticks-missing"; return false; }
            long lastPlayTicks;
            try { lastPlayTicks = Convert.ToInt64(rawTicks); }
            catch (Exception ex) { stage = $"fingerprint-last-play-ticks-convert:{SummarizeException(ex)}"; return false; }

            Type? songType = FindType("ePlayableSong", currentState.GetType().Assembly);
            if (songType?.IsEnum != true) { stage = "fingerprint-song-enum-missing"; return false; }
            MethodInfo? getStatus = currentState.GetType().GetMethod(
                "GetCassetteStatusForSong", PublicInstance, binder: null, types: new[] { songType }, modifiers: null);
            if (getStatus == null) { stage = "fingerprint-cassette-method-missing"; return false; }
            if (!TryReadFingerprintCassetteStatus(currentState, getStatus, songType, "I_GOT_MONEY", out string iGotMoney, out stage)) return false;
            if (!TryReadFingerprintCassetteStatus(currentState, getStatus, songType, "BADASS", out string badass, out stage)) return false;

            fingerprint = new(
                currentState.GetType().Name,
                playTime,
                lastPlayTicks,
                iGotMoney,
                badass);
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            fingerprint = default;
            stage = $"fingerprint-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    private static bool TryReadFingerprintCassetteStatus(
        object state,
        MethodInfo getStatus,
        Type songType,
        string nativeSong,
        out string status,
        out string stage)
    {
        status = string.Empty;
        stage = $"fingerprint-{nativeSong}-start";
        try
        {
            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            object? rawStatus = UnwrapNullable(getStatus.Invoke(state, new[] { song }));
            status = rawStatus?.ToString() ?? string.Empty;
            if (status.Length == 0) { stage = $"fingerprint-{nativeSong}-status-empty"; return false; }
            return true;
        }
        catch (Exception ex)
        {
            stage = $"fingerprint-{nativeSong}-status:{SummarizeException(ex)}";
            return false;
        }
    }

    private static string SummarizeException(Exception exception)
    {
        Exception root = exception.GetBaseException();
        string message = (root.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (message.Length > 160) message = message[..160];
        return $"{root.GetType().Name}:{message}";
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

    internal static bool IsCompatiblePlayerSaveRequestProcessor(object? processor) =>
        processor != null && string.Equals(processor.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal);

    internal static bool TrySubmitHaveInBag(object processor, string nativeSong, out string detail)
    {
        detail = string.Empty;
        try
        {
            if (!IsCompatiblePlayerSaveRequestProcessor(processor))
            {
                detail = "compatible PlayerSaveRequestProcessor unavailable";
                return false;
            }
            Assembly assembly = processor.GetType().Assembly;
            Type? requestType = FindType("RecordSongCassetteStatusInSaveDataRequest", assembly);
            Type? songType = FindType("ePlayableSong", assembly);
            Type? statusType = FindType("eSongCassetteStatus", assembly);
            Type? bundleType = FindType("ePlayerSaveChangeBundleKey", assembly);
            if (requestType == null || songType == null || statusType == null || bundleType?.IsEnum != true)
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

            object nativeBundle = Enum.Parse(bundleType, CassetteNativeRequestFactory.DefaultBundle, ignoreCase: false);
            object cassetteRequest = CassetteNativeRequestFactory.Create(
                requestType, songType, statusType, bundleType, nativeSong,
                CassetteNativeRequestFactory.HaveInBag, nativeBundle);
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
        string typeName = type.FullName ?? type.Name;
        if (!typeName.Contains("Nullable`1", StringComparison.Ordinal)) return value;
        object? hasValue = type.GetProperty(
            "HasValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value);
        if (hasValue is bool present && !present) return null;
        return type.GetProperty(
            "Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value);
    }
}
