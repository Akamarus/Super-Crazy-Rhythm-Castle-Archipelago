using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RhythmCastleAP;

internal readonly record struct CassetteRegularSavePointerEntry(
    int Slot,
    long Pointer,
    long LastPlayDateTimeUtcTicks);

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

    internal static bool TryGetProcessorSaveIdentity(
        object? processor,
        out long statePointer,
        out string stage)
    {
        statePointer = 0;
        stage = "processor-identity-start";
        try
        {
            if (!IsCompatiblePlayerSaveRequestProcessor(processor))
            {
                stage = "processor-identity-incompatible";
                return false;
            }
            MethodInfo? obtainState = processor!.GetType().GetMethods(PublicInstance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "ObtainState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            if (obtainState == null) { stage = "processor-identity-obtain-state-missing"; return false; }
            object? currentState = obtainState.Invoke(processor, null);
            if (currentState == null) { stage = "processor-identity-state-null"; return false; }
            PropertyInfo? pointerProperty = currentState.GetType().GetProperty("Pointer", PublicInstance);
            object? rawPointer = pointerProperty?.GetValue(currentState);
            if (rawPointer is not IntPtr pointer) { stage = "processor-identity-pointer-missing"; return false; }
            if (pointer == IntPtr.Zero) { stage = "processor-identity-pointer-zero"; return false; }
            statePointer = pointer.ToInt64();
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            statePointer = 0;
            stage = $"processor-identity-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPublicWriteState(object? processor, out CassettePublicWriteState state, out string stage)
    {
        state = default; stage = "write-state-start";
        try
        {
            stage = "write-state-obtain";
            if (!TryObtainPublicState(processor, "write-state", out object? nativeState, out stage)) return false;
            Type type = nativeState!.GetType();
            PropertyInfo? hasChangesProperty = type.GetProperty("HasChanges", PublicInstance);
            PropertyInfo? requiresProperty = type.GetProperty("RequiresWriteToDisk", PublicInstance);
            PropertyInfo? successProperty = type.GetProperty("GameTimeOfLastWriteToDisk", PublicInstance);
            PropertyInfo? failureProperty = type.GetProperty("GameTimeOfLastFailedAttemptToWriteToDisk", PublicInstance);
            PropertyInfo? reasonProperty = type.GetProperty("FailureReasonOfLastFailedAttemptToWriteToDisk", PublicInstance);
            if (hasChangesProperty == null || requiresProperty == null || successProperty == null || failureProperty == null || reasonProperty == null)
            { stage = "write-state-contract-missing"; return false; }
            stage = "write-state-has-changes-get";
            bool hasChanges = Convert.ToBoolean(hasChangesProperty.GetValue(nativeState));
            stage = "write-state-requires-write-get";
            bool requires = Convert.ToBoolean(requiresProperty.GetValue(nativeState));
            stage = "write-state-success-time-get";
            object? successValue = ReadPublicNullableProperty(successProperty, nativeState);
            if (!TryReadPublicGameTime(successValue, "success-time", out double? success, out stage)) return false;
            stage = "write-state-failure-time-get";
            object? failureValue = ReadPublicNullableProperty(failureProperty, nativeState);
            if (!TryReadPublicGameTime(failureValue, "failure-time", out double? failure, out stage)) return false;
            stage = "write-state-failure-reason-get";
            object? reasonValue = ReadPublicNullableProperty(reasonProperty, nativeState);
            if (!TryUnwrapPublicWriteNullable(reasonValue, "failure-reason", out object? reason, out stage)) return false;
            stage = "write-state-failure-reason-format";
            string? failureReason = reason?.ToString();
            state = new(hasChanges, requires, success, failure, failureReason);
            stage = "success";
            return true;
        }
        catch (Exception ex) { stage = $"{stage}-invocation:{SummarizeException(ex)}"; return false; }
    }

    private static object? ReadPublicNullableProperty(PropertyInfo property, object target)
    {
        try { return property.GetValue(target); }
        catch (TargetInvocationException ex) when (IsEmptyIl2CppNullableReturn(property, ex)) { return null; }
    }

    private static bool IsEmptyIl2CppNullableReturn(PropertyInfo property, TargetInvocationException exception)
    {
        Type propertyType = property.PropertyType;
        if (!propertyType.IsGenericType ||
            !string.Equals(propertyType.GetGenericTypeDefinition().FullName, "Il2CppSystem.Nullable`1", StringComparison.Ordinal))
            return false;
        Exception? inner = exception.InnerException;
        return inner is NullReferenceException &&
            string.Equals(inner.TargetSite?.Name, "CreateGCHandle", StringComparison.Ordinal) &&
            string.Equals(inner.TargetSite?.DeclaringType?.FullName,
                "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase", StringComparison.Ordinal);
    }

    private static bool TryReadPublicGameTime(object? nullable, string label, out double? rawTime, out string stage)
    {
        rawTime = null;
        if (!TryUnwrapPublicWriteNullable(nullable, label, out object? gameTime, out stage)) return false;
        if (gameTime == null) { stage = "success"; return true; }
        PropertyInfo? rawProperty = gameTime.GetType().GetProperty("RawTime", PublicInstance);
        if (rawProperty == null) { stage = $"write-{label}-raw-missing"; return false; }
        stage = $"write-{label}-raw-get";
        object? raw = rawProperty.GetValue(gameTime);
        if (raw == null) { stage = $"write-{label}-raw-null"; return false; }
        rawTime = Convert.ToDouble(raw);
        stage = "success";
        return true;
    }

    private static bool TryUnwrapPublicWriteNullable(object? value, string label, out object? unwrapped, out string stage)
    {
        unwrapped = value;
        stage = "success";
        if (value == null) return true;
        Type type = value.GetType();
        if (!(type.FullName ?? type.Name).Contains("Nullable`1", StringComparison.Ordinal)) return true;
        PropertyInfo? hasValue = type.GetProperty("HasValue", PublicInstance);
        PropertyInfo? innerValue = type.GetProperty("Value", PublicInstance);
        if (hasValue == null || innerValue == null)
        { unwrapped = null; stage = $"write-{label}-nullable-contract-missing"; return false; }
        stage = $"write-{label}-nullable-has-value-get";
        if (hasValue.GetValue(value) is not bool present)
        { unwrapped = null; stage = $"write-{label}-nullable-has-value-invalid"; return false; }
        if (!present) { unwrapped = null; return true; }
        stage = $"write-{label}-nullable-value-get";
        unwrapped = innerValue.GetValue(value);
        if (unwrapped == null) { stage = $"write-{label}-nullable-value-null"; return false; }
        return true;
    }

    internal static bool TryMatchRegularSaveSlot(
        object? saveDataProcessor,
        object? playerSaveProcessor,
        out int matchedSlot,
        out IReadOnlyList<CassetteRegularSavePointerEntry> entries,
        out string stage)
    {
        matchedSlot = default;
        var foundEntries = new List<CassetteRegularSavePointerEntry>();
        entries = foundEntries;
        stage = "join-start";
        try
        {
            if (!TryObtainPublicState(saveDataProcessor, "save-data", out object? saveDataState, out stage)) return false;
            if (!TryObtainPublicState(playerSaveProcessor, "player-save", out object? playerState, out stage)) return false;
            if (!TryReadPublicPointer(playerState!, "player-save", out long playerPointer, out stage)) return false;

            PropertyInfo? savesProperty = saveDataState!.GetType().GetProperty("RegularPlayerSaves", PublicInstance);
            object? saves = savesProperty?.GetValue(saveDataState);
            if (saves == null) { stage = "regular-saves-null"; return false; }
            MethodInfo? getEnumerator = saves.GetType().GetMethod(
                "GetEnumerator", PublicInstance, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (getEnumerator == null) { stage = "enumerator-missing"; return false; }

            object? enumerator;
            try { enumerator = getEnumerator.Invoke(saves, null); }
            catch (Exception ex) { stage = $"enumeration:{SummarizeException(ex)}"; return false; }
            if (enumerator == null) { stage = "enumerator-null"; return false; }
            try
            {
                MethodInfo? moveNext = enumerator.GetType().GetMethod(
                    "MoveNext", PublicInstance, binder: null, types: Type.EmptyTypes, modifiers: null);
                PropertyInfo? currentProperty = enumerator.GetType().GetProperty("Current", PublicInstance);
                if (moveNext == null || currentProperty == null) { stage = "enumerator-contract-missing"; return false; }
                for (int count = 0; count < 32; count++)
                {
                    bool hasNext;
                    try { hasNext = Convert.ToBoolean(moveNext.Invoke(enumerator, null)); }
                    catch (Exception ex) { stage = $"enumeration:{SummarizeException(ex)}"; return false; }
                    if (!hasNext) break;
                    object? pair = currentProperty.GetValue(enumerator);
                    if (pair == null) { stage = "entry-null"; return false; }
                    PropertyInfo? keyProperty = pair.GetType().GetProperty("Key", PublicInstance);
                    if (keyProperty == null) { stage = "entry-key-missing"; return false; }
                    object? rawKey = keyProperty.GetValue(pair);
                    if (rawKey == null) { stage = "entry-key-null"; return false; }
                    int slot;
                    try { slot = Convert.ToInt32(rawKey); }
                    catch (Exception ex) { stage = $"entry-key-convert:{SummarizeException(ex)}"; return false; }
                    object? value = pair.GetType().GetProperty("Value", PublicInstance)?.GetValue(pair);
                    if (value == null) { stage = "entry-value-null"; return false; }
                    if (!TryReadPublicPointer(value, $"entry-{slot}", out long pointer, out stage)) return false;
                    PropertyInfo? gameStatsProperty = value.GetType().GetProperty("GameStats", PublicInstance);
                    if (gameStatsProperty == null) { stage = $"entry-{slot}-game-stats-missing"; return false; }
                    object? gameStats = gameStatsProperty.GetValue(value);
                    if (gameStats == null) { stage = $"entry-{slot}-game-stats-null"; return false; }
                    PropertyInfo? lastPlayProperty = gameStats.GetType().GetProperty("LastPlayDateTimeUtc", PublicInstance);
                    if (lastPlayProperty == null) { stage = $"entry-{slot}-last-play-missing"; return false; }
                    object? lastPlay = lastPlayProperty.GetValue(gameStats);
                    if (lastPlay == null) { stage = $"entry-{slot}-last-play-null"; return false; }
                    PropertyInfo? ticksProperty = lastPlay.GetType().GetProperty("Ticks", PublicInstance);
                    if (ticksProperty == null) { stage = $"entry-{slot}-ticks-missing"; return false; }
                    object? rawTicks = ticksProperty.GetValue(lastPlay);
                    if (rawTicks == null) { stage = $"entry-{slot}-ticks-null"; return false; }
                    long ticks;
                    try { ticks = Convert.ToInt64(rawTicks); }
                    catch (Exception ex) { stage = $"entry-{slot}-ticks-convert:{SummarizeException(ex)}"; return false; }
                    foundEntries.Add(new(slot, pointer, ticks));
                }
                if (foundEntries.Count == 32) { stage = "entry-limit"; return false; }
            }
            finally
            {
                if (enumerator is IDisposable disposable) disposable.Dispose();
                else enumerator.GetType().GetMethod("Dispose", PublicInstance, binder: null, types: Type.EmptyTypes, modifiers: null)?.Invoke(enumerator, null);
            }

            CassetteRegularSavePointerEntry[] matches = foundEntries.Where(entry => entry.Pointer == playerPointer).ToArray();
            if (matches.Length != 1) { stage = $"match-count:{matches.Length}"; return false; }
            matchedSlot = matches[0].Slot;
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            matchedSlot = default;
            stage = $"join-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    private static bool TryObtainPublicState(object? processor, string label, out object? state, out string stage)
    {
        state = null;
        stage = $"{label}-start";
        if (processor == null) { stage = $"{label}-processor-null"; return false; }
        MethodInfo? obtainState = processor.GetType().GetMethod(
            "ObtainState", PublicInstance, binder: null, types: Type.EmptyTypes, modifiers: null);
        if (obtainState == null) { stage = $"{label}-obtain-state-missing"; return false; }
        stage = $"{label}-obtain-state-invoke";
        state = obtainState.Invoke(processor, null);
        if (state == null) { stage = $"{label}-state-null"; return false; }
        return true;
    }

    private static bool TryReadPublicPointer(object state, string label, out long pointerValue, out string stage)
    {
        pointerValue = 0;
        PropertyInfo? pointerProperty = state.GetType().GetProperty("Pointer", PublicInstance);
        object? rawPointer = pointerProperty?.GetValue(state);
        if (rawPointer is not IntPtr pointer) { stage = $"{label}-pointer-missing"; return false; }
        if (pointer == IntPtr.Zero) { stage = $"{label}-pointer-zero"; return false; }
        pointerValue = pointer.ToInt64();
        stage = "success";
        return true;
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

    internal static bool TrySubmitDefaultUrgentPersist(out string detail)
    {
        detail = string.Empty;
        try
        {
            Type? requestType = FindType("PersistSaveChangeBundleRequest");
            Type? bundleType = FindType("ePlayerSaveChangeBundleKey");
            Type? writeType = FindType("eSaveFileWriteType");
            Type? requestSystemType = FindType("RequestSystem");
            if (requestType == null || bundleType?.IsEnum != true || writeType?.IsEnum != true || requestSystemType == null)
            { detail = "public persist semantic types unavailable"; return false; }
            object bundle = Enum.Parse(bundleType, "DEFAULT", ignoreCase: false);
            object urgent = Enum.Parse(writeType, "URGENT", ignoreCase: false);
            if (Convert.ToInt32(bundle) != 1 || Convert.ToInt32(urgent) != 0)
            { detail = "public persist enum values did not match DEFAULT=1/URGENT=0"; return false; }
            ConstructorInfo? constructor = requestType.GetConstructors(PublicInstance)
                .SingleOrDefault(candidate => candidate.GetParameters().Length == 2 &&
                    candidate.GetParameters()[1].ParameterType == writeType);
            if (constructor == null) { detail = "public persist semantic constructor unavailable"; return false; }
            Type bundleParameterType = constructor.GetParameters()[0].ParameterType;
            object? bundleArgument = BuildPublicNullable(bundleParameterType, bundleType, bundle);
            if (bundleArgument == null) { detail = "public DEFAULT nullable bundle unavailable"; return false; }
            object request = constructor.Invoke(new[] { bundleArgument, urgent });
            MethodInfo? submitDefinition = requestSystemType.GetMethods(PublicStatic)
                .SingleOrDefault(method => string.Equals(method.Name, "SubmitRequest", StringComparison.Ordinal) &&
                    method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1 &&
                    method.GetParameters().Length == 1);
            if (submitDefinition == null) { detail = "public RequestSystem.SubmitRequest<T> unavailable"; return false; }
            submitDefinition.MakeGenericMethod(requestType).Invoke(null, new[] { request });
            detail = "bundle='DEFAULT' writeType='URGENT' route='RequestSystem.SubmitRequest<T>'";
            return true;
        }
        catch (Exception ex)
        {
            detail = $"public persist invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    private static object? BuildPublicNullable(Type parameterType, Type valueType, object value)
    {
        if (parameterType == valueType) return value;
        ConstructorInfo? single = parameterType.GetConstructor(PublicInstance, null, new[] { valueType }, null);
        if (single != null) return single.Invoke(new[] { value });
        ConstructorInfo? pair = parameterType.GetConstructors(PublicInstance).FirstOrDefault(candidate =>
        {
            ParameterInfo[] parameters = candidate.GetParameters();
            return parameters.Length == 2 && parameters[0].ParameterType == typeof(bool) && parameters[1].ParameterType == valueType;
        });
        return pair?.Invoke(new[] { (object)true, value });
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
