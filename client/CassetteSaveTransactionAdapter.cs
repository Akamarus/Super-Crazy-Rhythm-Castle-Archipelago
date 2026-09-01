using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RhythmCastleAP;

internal readonly record struct CassetteRegularSavePointerEntry(
    int Slot,
    long Pointer,
    long LastPlayDateTimeUtcTicks);

internal readonly record struct CassetteRecordSongRoutingPayload(
    string Song,
    string Status,
    bool BundlePresent,
    string? Bundle);

internal readonly record struct CassettePersistBundleRoutingPayload(
    bool BundlePresent,
    string? Bundle,
    string WriteType);

internal readonly record struct CassetteBundleRoutingState(
    long StatePointer,
    bool HasUnstagedChanges,
    IReadOnlyDictionary<string, string> Statuses);

internal readonly record struct CassetteDiskCommitTargetSideState(
    long StatePointer,
    bool HasUnstagedChanges,
    bool HasChanges,
    bool RequiresWriteToDisk,
    bool DefaultBundlePresent,
    int DefaultBundleChangeCount,
    IReadOnlyDictionary<string, string> EffectiveStatuses,
    IReadOnlyDictionary<string, string?> CanonicalStatuses);

internal readonly record struct CassetteDiskCommitTargetDiagnosticState(
    long PlayerProcessorPointer,
    long RegisteredPersistProcessorPointer,
    long RetainedSaveDataProcessorPointer,
    long SaveDataStatePointer,
    int SelectedPlayerSaveSlot,
    long SelectedEntryPointer,
    long ExpectedSlotEntryPointer,
    CassetteDiskCommitTargetSideState Player,
    CassetteDiskCommitTargetSideState Selected,
    bool HasPlayerProcessorPointer = false,
    bool HasRegisteredPersistProcessorPointer = false,
    bool HasRetainedSaveDataProcessorPointer = false,
    bool HasSaveDataStatePointer = false,
    bool HasSelectedPlayerSaveSlot = false,
    bool HasSelectedEntryPointer = false,
    bool HasExpectedSlotEntryPointer = false,
    bool HasPlayer = false,
    bool HasSelected = false);

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

    internal static bool TryReadPublicWriteDiagnosticState(
        object? processor,
        long expectedPointer,
        IReadOnlyList<string> nativeSongs,
        out CassettePublicWriteDiagnosticState state,
        out string stage) =>
        TryReadPublicWriteDiagnosticStateCore(
            processor, expectedPointer, nativeSongs, out state, out stage);

    internal static bool TryReadPublicWriteLifecycleDiagnosticState(
        object? processor,
        IReadOnlyList<string> nativeSongs,
        out CassettePublicWriteDiagnosticState state,
        out string stage) =>
        TryReadPublicWriteDiagnosticStateCore(
            processor, expectedPointer: null, nativeSongs, out state, out stage);

    internal static bool TryReadRecordSongCassetteStatusRequest(
        object? request,
        out CassetteRecordSongRoutingPayload payload,
        out string stage)
    {
        payload = default;
        stage = "routing-record-start";
        try
        {
            if (request == null || !string.Equals(
                    request.GetType().Name,
                    "RecordSongCassetteStatusInSaveDataRequest",
                    StringComparison.Ordinal))
            { stage = "routing-record-incompatible"; return false; }
            Type requestType = request.GetType();
            PropertyInfo? songProperty = requestType.GetProperty("Song", PublicInstance);
            if (songProperty == null) { stage = "routing-record-song-missing"; return false; }
            stage = "routing-record-song-get";
            string? song = songProperty.GetValue(request)?.ToString();
            if (string.IsNullOrWhiteSpace(song)) { stage = "routing-record-song-empty"; return false; }
            PropertyInfo? statusProperty = requestType.GetProperty("CassetteStatus", PublicInstance);
            if (statusProperty == null) { stage = "routing-record-status-missing"; return false; }
            stage = "routing-record-status-get";
            string? status = statusProperty.GetValue(request)?.ToString();
            if (string.IsNullOrWhiteSpace(status)) { stage = "routing-record-status-empty"; return false; }
            PropertyInfo? bundleProperty = requestType.GetProperty("Bundle", PublicInstance);
            if (bundleProperty == null) { stage = "routing-record-bundle-missing"; return false; }
            stage = "routing-record-bundle-get";
            object? rawBundle = ReadPublicNullableProperty(bundleProperty, request);
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawBundle,
                    "routing-record-bundle",
                    out bool bundlePresent,
                    out object? bundle,
                    out stage))
                return false;
            payload = new(song, status, bundlePresent, bundle?.ToString());
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            payload = default;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPersistSaveChangeBundleRequest(
        object? request,
        out CassettePersistBundleRoutingPayload payload,
        out string stage)
    {
        payload = default;
        stage = "routing-persist-start";
        try
        {
            if (request == null || !string.Equals(
                    request.GetType().Name,
                    "PersistSaveChangeBundleRequest",
                    StringComparison.Ordinal))
            { stage = "routing-persist-incompatible"; return false; }
            Type requestType = request.GetType();
            PropertyInfo? bundleProperty = requestType.GetProperty("Bundle", PublicInstance);
            if (bundleProperty == null) { stage = "routing-persist-bundle-missing"; return false; }
            stage = "routing-persist-bundle-get";
            object? rawBundle = ReadPublicNullableProperty(bundleProperty, request);
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawBundle,
                    "routing-persist-bundle",
                    out bool bundlePresent,
                    out object? bundle,
                    out stage))
                return false;
            PropertyInfo? writeTypeProperty = requestType.GetProperty("WriteTypeToRequest", PublicInstance);
            if (writeTypeProperty == null) { stage = "routing-persist-write-type-missing"; return false; }
            stage = "routing-persist-write-type-get";
            string? writeType = writeTypeProperty.GetValue(request)?.ToString();
            if (string.IsNullOrWhiteSpace(writeType)) { stage = "routing-persist-write-type-empty"; return false; }
            payload = new(bundlePresent, bundle?.ToString(), writeType);
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            payload = default;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPersistAllSaveChangeBundlesRequest(
        object? request,
        out string? writeType,
        out string stage)
    {
        writeType = null;
        stage = "routing-persist-all-start";
        try
        {
            if (request == null || !string.Equals(
                    request.GetType().Name,
                    "PersistAllSaveChangeBundlesRequest",
                    StringComparison.Ordinal))
            { stage = "routing-persist-all-incompatible"; return false; }
            PropertyInfo? writeTypeProperty = request.GetType().GetProperty("WriteTypeToRequest", PublicInstance);
            if (writeTypeProperty == null) { stage = "routing-persist-all-write-type-missing"; return false; }
            stage = "routing-persist-all-write-type-get";
            writeType = writeTypeProperty.GetValue(request)?.ToString();
            if (string.IsNullOrWhiteSpace(writeType)) { stage = "routing-persist-all-write-type-empty"; return false; }
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            writeType = null;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPlayerSaveBundleRoutingState(
        object? playerSaveProcessor,
        long expectedPointer,
        IReadOnlyList<string> nativeSongs,
        out CassetteBundleRoutingState state,
        out string stage)
    {
        state = default;
        stage = "routing-player-start";
        try
        {
            if (!TryObtainPublicState(playerSaveProcessor, "routing-player", out object? nativeState, out stage))
                return false;
            return TryReadBundleRoutingStateCore(
                nativeState!, playerSaveProcessor!.GetType().Assembly, expectedPointer, nativeSongs, "routing-player", out state, out stage);
        }
        catch (Exception ex)
        {
            state = default;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadSaveDataBundleRoutingState(
        object? saveDataProcessor,
        long expectedPointer,
        IReadOnlyList<string> nativeSongs,
        out int selectedSlot,
        out CassetteBundleRoutingState state,
        out string stage)
    {
        selectedSlot = default;
        state = default;
        stage = "routing-save-data-start";
        try
        {
            if (!TryObtainPublicState(saveDataProcessor, "routing-save-data", out object? saveDataState, out stage))
                return false;
            Type saveDataStateType = saveDataState!.GetType();
            PropertyInfo? selectedSlotProperty = saveDataStateType.GetProperty("SelectedPlayerSaveSlot", PublicInstance);
            if (selectedSlotProperty == null) { stage = "routing-save-data-selected-slot-missing"; return false; }
            stage = "routing-save-data-selected-slot-get";
            object? rawSelectedSlot = ReadPublicNullableProperty(selectedSlotProperty, saveDataState);
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawSelectedSlot,
                    "routing-save-data-selected-slot",
                    out bool slotPresent,
                    out object? slotValue,
                    out stage))
                return false;
            if (!slotPresent || slotValue == null) { stage = "routing-save-data-selected-slot-empty"; return false; }
            stage = "routing-save-data-selected-slot-convert";
            selectedSlot = Convert.ToInt32(slotValue);
            PropertyInfo? savesProperty = saveDataStateType.GetProperty("RegularPlayerSaves", PublicInstance);
            if (savesProperty == null) { stage = "routing-save-data-regular-saves-missing"; return false; }
            stage = "routing-save-data-regular-saves-get";
            object? saves = savesProperty.GetValue(saveDataState);
            if (saves == null) { stage = "routing-save-data-regular-saves-null"; return false; }
            PropertyInfo? itemProperty = saves.GetType().GetProperty("Item", PublicInstance);
            if (itemProperty == null || itemProperty.GetIndexParameters().Length != 1)
            { stage = "routing-save-data-selected-state-indexer-missing"; return false; }
            stage = "routing-save-data-selected-state-get";
            object? selectedState = itemProperty.GetValue(saves, new object[] { selectedSlot });
            if (selectedState == null) { stage = "routing-save-data-selected-state-null"; return false; }
            return TryReadBundleRoutingStateCore(
                selectedState, saveDataProcessor!.GetType().Assembly, expectedPointer, nativeSongs, "routing-save-data", out state, out stage);
        }
        catch (Exception ex)
        {
            selectedSlot = default;
            state = default;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadDiskCommitTargetDiagnostic(
        object? playerSaveProcessor,
        object? retainedSaveDataProcessor,
        int expectedSlot,
        long expectedPointer,
        IReadOnlyList<string> nativeSongs,
        out CassetteDiskCommitTargetDiagnosticState state,
        out int registryCount,
        out string stage)
    {
        state = default;
        registryCount = -1;
        stage = "target-start";
        try
        {
            if (!TryReadPublicObjectPointer(playerSaveProcessor, "target-player-processor", out long playerProcessorPointer, out stage))
                return false;
            state = state with
            {
                PlayerProcessorPointer = playerProcessorPointer,
                HasPlayerProcessorPointer = true,
            };
            if (!TryObtainPublicState(playerSaveProcessor, "target-player", out object? playerState, out stage))
                return false;
            if (!TryReadDiskCommitTargetSide(
                    playerState!, playerSaveProcessor!.GetType().Assembly, nativeSongs, "target-player",
                    out CassetteDiskCommitTargetSideState player, out stage))
                return false;
            state = state with { Player = player, HasPlayer = true };

            if (!TryReadRegisteredPersistProcessorPointer(
                    playerSaveProcessor.GetType().Assembly,
                    out object? registeredPersistProcessor,
                    out long registeredPersistProcessorPointer,
                    out registryCount,
                    out stage))
                return false;
            state = state with
            {
                RegisteredPersistProcessorPointer = registeredPersistProcessorPointer,
                HasRegisteredPersistProcessorPointer = true,
            };
            if (!TryReadPublicObjectPointer(
                    retainedSaveDataProcessor, "target-save-data-processor", out long retainedSaveDataProcessorPointer, out stage))
                return false;
            state = state with
            {
                RetainedSaveDataProcessorPointer = retainedSaveDataProcessorPointer,
                HasRetainedSaveDataProcessorPointer = true,
            };
            if (!TryObtainPublicState(registeredPersistProcessor, "target-save-data", out object? saveDataState, out stage))
                return false;
            if (!TryReadPublicPointer(saveDataState!, "target-save-data-state", out long saveDataStatePointer, out stage))
                return false;
            state = state with { SaveDataStatePointer = saveDataStatePointer, HasSaveDataStatePointer = true };

            Type saveDataStateType = saveDataState!.GetType();
            if (!TryGetPublicReadableProperty(
                    saveDataStateType, "SelectedPlayerSaveSlot", PublicInstance,
                    "target-save-data-selected-slot", out PropertyInfo selectedSlotProperty, out stage))
                return false;
            stage = "target-save-data-selected-slot-get";
            object? rawSelectedSlot = ReadPublicNullableProperty(selectedSlotProperty, saveDataState);
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawSelectedSlot, "target-save-data-selected-slot", out bool slotPresent, out object? slotValue, out stage))
                return false;
            if (!slotPresent || slotValue == null) { stage = "target-save-data-selected-slot-empty"; return false; }
            stage = "target-save-data-selected-slot-convert";
            int selectedSlot = Convert.ToInt32(slotValue);
            state = state with { SelectedPlayerSaveSlot = selectedSlot, HasSelectedPlayerSaveSlot = true };

            if (!TryGetPublicReadableProperty(
                    saveDataStateType, "RegularPlayerSaves", PublicInstance,
                    "target-save-data-regular-saves", out PropertyInfo savesProperty, out stage))
                return false;
            stage = "target-save-data-regular-saves-get";
            object? saves = savesProperty.GetValue(saveDataState);
            if (saves == null) { stage = "target-save-data-regular-saves-null"; return false; }
            if (!TryReadPublicDictionaryValue(saves, selectedSlot, "target-selected-entry", out object? selectedState, out stage))
                return false;
            if (!TryReadPublicDictionaryValue(saves, expectedSlot, "target-expected-entry", out object? expectedState, out stage))
                return false;
            if (!TryReadPublicPointer(selectedState!, "target-selected-entry", out long selectedEntryPointer, out stage))
                return false;
            state = state with { SelectedEntryPointer = selectedEntryPointer, HasSelectedEntryPointer = true };
            if (!TryReadPublicPointer(expectedState!, "target-expected-entry", out long expectedSlotEntryPointer, out stage))
                return false;
            state = state with { ExpectedSlotEntryPointer = expectedSlotEntryPointer, HasExpectedSlotEntryPointer = true };
            if (!TryReadDiskCommitTargetSide(
                    selectedState!, registeredPersistProcessor!.GetType().Assembly, nativeSongs, "target-selected",
                    out CassetteDiskCommitTargetSideState selected, out stage))
                return false;

            state = state with { Selected = selected, HasSelected = true };
            stage = "success";
            _ = expectedPointer; // The expected identity is emitted by the caller; diagnostics never reject a mismatch.
            return true;
        }
        catch (Exception ex)
        {
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryConfirmSaveSynchronizationReady(
        object? retainedSaveDataProcessor,
        int expectedSlot,
        long expectedPointer,
        out string stage)
    {
        stage = "readiness-start";
        try
        {
            if (retainedSaveDataProcessor == null)
            {
                stage = "readiness-retained-save-data-processor-null";
                return false;
            }
            if (!TryReadRegisteredPersistProcessorPointer(
                    retainedSaveDataProcessor.GetType().Assembly,
                    out object? registeredPersistProcessor,
                    out long registeredPersistProcessorPointer,
                    out _,
                    out stage))
                return false;
            if (!TryReadPublicObjectPointer(
                    retainedSaveDataProcessor,
                    "readiness-retained-save-data-processor",
                    out long retainedSaveDataProcessorPointer,
                    out stage))
                return false;
            if (registeredPersistProcessorPointer != retainedSaveDataProcessorPointer)
            {
                stage = $"readiness-save-data-processor-pointer-mismatch:expected=0x{retainedSaveDataProcessorPointer:X}:actual=0x{registeredPersistProcessorPointer:X}";
                return false;
            }
            if (!TryObtainPublicState(
                    registeredPersistProcessor,
                    "readiness-save-data",
                    out object? saveDataState,
                    out stage))
                return false;

            Type saveDataStateType = saveDataState!.GetType();
            if (!TryGetPublicReadableProperty(
                    saveDataStateType,
                    "SelectedPlayerSaveSlot",
                    PublicInstance,
                    "readiness-selected-slot",
                    out PropertyInfo selectedSlotProperty,
                    out stage))
                return false;
            stage = "readiness-selected-slot-get";
            object? rawSelectedSlot = ReadPublicNullableProperty(selectedSlotProperty, saveDataState);
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawSelectedSlot,
                    "readiness-selected-slot",
                    out bool slotPresent,
                    out object? slotValue,
                    out stage))
                return false;
            if (!slotPresent || slotValue == null)
            {
                stage = "readiness-selected-slot-empty";
                return false;
            }
            stage = "readiness-selected-slot-convert";
            int selectedSlot = Convert.ToInt32(slotValue);
            if (selectedSlot != expectedSlot)
            {
                stage = $"readiness-selected-slot-mismatch:expected={expectedSlot}:actual={selectedSlot}";
                return false;
            }

            if (!TryGetPublicReadableProperty(
                    saveDataStateType,
                    "RegularPlayerSaves",
                    PublicInstance,
                    "readiness-regular-saves",
                    out PropertyInfo savesProperty,
                    out stage))
                return false;
            stage = "readiness-regular-saves-get";
            object? saves = savesProperty.GetValue(saveDataState);
            if (saves == null)
            {
                stage = "readiness-regular-saves-null";
                return false;
            }
            if (!TryReadPublicDictionaryValue(
                    saves,
                    selectedSlot,
                    "readiness-selected-entry",
                    out object? selectedState,
                    out stage))
                return false;
            if (!TryReadPublicPointer(
                    selectedState!,
                    "readiness-selected-entry",
                    out long selectedEntryPointer,
                    out stage))
                return false;
            if (selectedEntryPointer != expectedPointer)
            {
                stage = $"readiness-selected-entry-pointer-mismatch:expected=0x{expectedPointer:X}:actual=0x{selectedEntryPointer:X}";
                return false;
            }

            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    private static bool TryReadRegisteredPersistProcessorPointer(
        Assembly preferredAssembly,
        out object? processor,
        out long processorPointer,
        out int registryCount,
        out string stage)
    {
        processor = null;
        processorPointer = 0;
        registryCount = -1;
        stage = "target-registry-start";
        Type? requestSystemType = FindType("RequestSystem", preferredAssembly);
        if (requestSystemType == null) { stage = "target-registry-owner-missing"; return false; }
        if (!TryGetPublicReadableProperty(
                requestSystemType, "registeredProcessorsWrapped", PublicStatic,
                "target-registry-property", out PropertyInfo registryProperty, out stage))
            return false;
        stage = "target-registry-get";
        object? registry = registryProperty.GetValue(null);
        if (registry == null) { stage = "target-registry-null"; return false; }
        if (!TryGetPublicReadableProperty(
                registry.GetType(), "Count", PublicInstance, "target-registry-count",
                out PropertyInfo countProperty, out stage))
            return false;
        stage = "target-registry-count-get";
        object? rawCount = countProperty.GetValue(registry);
        if (rawCount == null) { stage = "target-registry-count-null"; return false; }
        stage = "target-registry-count-convert";
        registryCount = Convert.ToInt32(rawCount);
        if (registryCount < 0) { stage = "target-registry-count-invalid"; return false; }

        Type? requestType = FindType("PersistSaveChangeBundleRequest", preferredAssembly);
        if (requestType == null) { stage = "target-registry-request-type-missing"; return false; }
        Type? saveDataProcessorType = requestType.Assembly.GetType("SaveDataRequestProcessor", throwOnError: false, ignoreCase: false);
        if (saveDataProcessorType == null) { stage = "target-registry-save-data-type-missing"; return false; }
        if (!saveDataProcessorType.IsPublic && !saveDataProcessorType.IsNestedPublic)
        {
            stage = "target-registry-save-data-type-non-public";
            return false;
        }
        Type? il2CppType = FindType("Il2CppType", preferredAssembly);
        if (il2CppType == null) { stage = "target-registry-key-converter-missing"; return false; }
        MethodInfo? convertType = il2CppType.GetMethods(PublicStatic).SingleOrDefault(method =>
            string.Equals(method.Name, "From", StringComparison.Ordinal) &&
            method.GetParameters().Length == 1 &&
            method.GetParameters()[0].ParameterType == typeof(Type));
        if (convertType == null) { stage = "target-registry-key-convert-method-missing"; return false; }
        Type keyType = convertType.ReturnType;
        if (keyType == typeof(void)) { stage = "target-registry-key-convert-return-invalid"; return false; }
        stage = "target-registry-key-convert";
        object? key = convertType.Invoke(null, new object[] { requestType });
        if (key == null) { stage = "target-registry-key-null"; return false; }
        if (!keyType.IsInstanceOfType(key)) { stage = $"target-registry-key-type-mismatch:expected={keyType.FullName}:actual={key.GetType().FullName}"; return false; }
        if (!TryGetPublicReadableProperty(
                key.GetType(), "Name", PublicInstance, "target-registry-key-name",
                out PropertyInfo keyNameProperty, out stage))
            return false;
        stage = "target-registry-key-name-get";
        string? keyName = keyNameProperty.GetValue(key)?.ToString();
        if (!string.Equals(keyName, requestType.Name, StringComparison.Ordinal))
        {
            stage = $"target-registry-key-name-mismatch:expected={requestType.Name}:actual={keyName ?? "<null>"}";
            return false;
        }

        Type? registrationType = FindType("RegisteredRequestProcessor", preferredAssembly);
        if (registrationType == null) { stage = "target-registry-registration-type-missing"; return false; }
        if (!registrationType.IsPublic && !registrationType.IsNestedPublic)
        {
            stage = "target-registry-registration-type-non-public";
            return false;
        }
        MethodInfo[] lookupMethods = registry.GetType().GetMethods(PublicInstance).Where(method =>
        {
            ParameterInfo[] parameters = method.GetParameters();
            return string.Equals(method.Name, "TryGetValue", StringComparison.Ordinal) &&
                   method.ReturnType == typeof(bool) &&
                   parameters.Length == 2 &&
                   parameters[0].ParameterType == keyType &&
                   parameters[1].IsOut &&
                   parameters[1].ParameterType.IsByRef &&
                   parameters[1].ParameterType.GetElementType() == registrationType;
        }).ToArray();
        if (lookupMethods.Length == 0) { stage = "target-registry-try-get-missing"; return false; }
        if (lookupMethods.Length != 1) { stage = $"target-registry-try-get-ambiguous:{lookupMethods.Length}"; return false; }
        object?[] lookupArguments = { key, null };
        stage = "target-registry-try-get-invoke";
        object? rawFound = lookupMethods[0].Invoke(registry, lookupArguments);
        if (rawFound is not bool found) { stage = "target-registry-try-get-result-invalid"; return false; }
        if (!found) { stage = "target-registry-persist-not-found"; return false; }
        object? registration = lookupArguments[1];
        if (registration == null) { stage = "target-registry-registration-null"; return false; }
        if (!registrationType.IsInstanceOfType(registration))
        {
            stage = $"target-registry-registration-type-mismatch:expected={registrationType.FullName}:actual={registration.GetType().FullName}";
            return false;
        }
        if (!TryGetPublicReadableProperty(
                registrationType, "Processor", PublicInstance, "target-registry-processor",
                out PropertyInfo processorProperty, out stage))
            return false;
        stage = "target-registry-processor-get";
        object? matchedProcessor = processorProperty.GetValue(registration);
        if (matchedProcessor == null) { stage = "target-registry-processor-null"; return false; }
        Type? il2CppObjectBaseType = matchedProcessor.GetType();
        while (il2CppObjectBaseType != null && !string.Equals(
                   il2CppObjectBaseType.FullName,
                   "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase",
                   StringComparison.Ordinal))
            il2CppObjectBaseType = il2CppObjectBaseType.BaseType;
        if (il2CppObjectBaseType == null) { stage = "target-registry-persist-processor-il2cpp-object-base-missing"; return false; }
        if (!il2CppObjectBaseType.IsPublic && !il2CppObjectBaseType.IsNestedPublic)
        {
            stage = "target-registry-persist-processor-il2cpp-object-base-non-public";
            return false;
        }
        if (!TryRewrapPublicSaveDataProcessorForDiagnostic(
                matchedProcessor,
                saveDataProcessorType,
                il2CppObjectBaseType,
                out object? saveDataProcessor,
                out processorPointer,
                out stage))
            return false;
        processor = saveDataProcessor;
        return true;
    }

    internal static bool TryRewrapPublicSaveDataProcessorForDiagnostic(
        object? processor,
        Type saveDataProcessorType,
        Type il2CppObjectBaseType,
        out object? saveDataProcessor,
        out long processorPointer,
        out string stage)
    {
        saveDataProcessor = null;
        processorPointer = 0;
        stage = "target-registry-persist-processor-rewrap-start";
        try
        {
            if (processor == null) { stage = "target-registry-persist-processor-null"; return false; }
            if (!il2CppObjectBaseType.IsPublic && !il2CppObjectBaseType.IsNestedPublic)
            { stage = "target-registry-persist-processor-il2cpp-object-base-non-public"; return false; }
            if (!il2CppObjectBaseType.IsInstanceOfType(processor))
            { stage = "target-registry-persist-processor-il2cpp-object-base-mismatch"; return false; }
            if ((!saveDataProcessorType.IsPublic && !saveDataProcessorType.IsNestedPublic) ||
                !il2CppObjectBaseType.IsAssignableFrom(saveDataProcessorType))
            { stage = "target-registry-save-data-type-invalid"; return false; }
            if (!TryReadPublicObjectPointer(
                    processor, "target-registry-persist-processor", out long originalPointer, out stage))
                return false;

            static bool IsTryCastContract(MethodInfo method)
            {
                Type[] genericArguments = method.IsGenericMethodDefinition ? method.GetGenericArguments() : Type.EmptyTypes;
                return string.Equals(method.Name, "TryCast", StringComparison.Ordinal) &&
                       method.IsGenericMethodDefinition &&
                       genericArguments.Length == 1 &&
                       method.GetParameters().Length == 0 &&
                       method.ReturnType == genericArguments[0];
            }

            MethodInfo[] publicTryCastMethods = il2CppObjectBaseType.GetMethods(PublicInstance)
                .Where(IsTryCastContract)
                .ToArray();
            if (publicTryCastMethods.Length == 0)
            {
                bool nonPublicContractExists = il2CppObjectBaseType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
                    .Any(IsTryCastContract);
                stage = nonPublicContractExists
                    ? "target-registry-persist-processor-try-cast-non-public"
                    : "target-registry-persist-processor-try-cast-missing";
                return false;
            }
            if (publicTryCastMethods.Length != 1)
            {
                stage = $"target-registry-persist-processor-try-cast-ambiguous:{publicTryCastMethods.Length}";
                return false;
            }

            stage = "target-registry-persist-processor-try-cast-close";
            MethodInfo closedTryCast = publicTryCastMethods[0].MakeGenericMethod(saveDataProcessorType);
            stage = "target-registry-persist-processor-try-cast-invoke";
            object? castProcessor = closedTryCast.Invoke(processor, null);
            if (castProcessor == null)
            {
                stage = "target-registry-persist-processor-native-class-mismatch";
                return false;
            }
            if (castProcessor.GetType() != saveDataProcessorType)
            {
                stage = $"target-registry-persist-processor-cast-type-mismatch:expected={saveDataProcessorType.FullName}:actual={castProcessor.GetType().FullName}";
                return false;
            }
            if (!TryReadPublicObjectPointer(
                    castProcessor, "target-registry-persist-processor-cast", out long castPointer, out stage))
                return false;
            if (castPointer != originalPointer)
            {
                stage = $"target-registry-persist-processor-cast-pointer-mismatch:expected=0x{originalPointer:X}:actual=0x{castPointer:X}";
                return false;
            }

            saveDataProcessor = castProcessor;
            processorPointer = originalPointer;
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            saveDataProcessor = null;
            processorPointer = 0;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    private static bool TryReadDiskCommitTargetSide(
        object nativeState,
        Assembly preferredAssembly,
        IReadOnlyList<string> nativeSongs,
        string label,
        out CassetteDiskCommitTargetSideState state,
        out string stage)
    {
        state = default;
        stage = $"{label}-start";
        if (!TryReadPublicPointer(nativeState, label, out long statePointer, out stage)) return false;
        Type stateType = nativeState.GetType();
        if (!TryReadPublicBoolean(nativeState, "HasUnstagedChanges", $"{label}-has-unstaged", out bool hasUnstaged, out stage) ||
            !TryReadPublicBoolean(nativeState, "HasChanges", $"{label}-has-changes", out bool hasChanges, out stage) ||
            !TryReadPublicBoolean(nativeState, "RequiresWriteToDisk", $"{label}-requires-write", out bool requires, out stage))
            return false;

        Type? bundleType = FindType("ePlayerSaveChangeBundleKey", preferredAssembly);
        if (bundleType?.IsEnum != true) { stage = $"{label}-bundle-type-missing"; return false; }
        stage = $"{label}-default-bundle-parse";
        object defaultBundle = Enum.Parse(bundleType, "DEFAULT", ignoreCase: false);
        if (!TryGetPublicReadableProperty(
                stateType, "saveChangeBundles", PublicInstance, $"{label}-bundles",
                out PropertyInfo bundlesProperty, out stage))
            return false;
        stage = $"{label}-bundles-get";
        object? bundles = bundlesProperty.GetValue(nativeState);
        if (bundles == null) { stage = $"{label}-bundles-null"; return false; }
        if (!TryReadPublicDictionaryPresenceAndValue(
                bundles, defaultBundle, $"{label}-default-bundle", out bool bundlePresent, out object? bundle, out stage))
            return false;
        int bundleChangeCount = 0;
        if (bundlePresent)
        {
            if (!TryGetPublicReadableProperty(
                    bundle!.GetType(), "Changes", PublicInstance, $"{label}-default-bundle-changes",
                    out PropertyInfo changesProperty, out stage))
                return false;
            stage = $"{label}-default-bundle-changes-get";
            object? changes = changesProperty.GetValue(bundle);
            if (changes == null) { stage = $"{label}-default-bundle-changes-null"; return false; }
            if (!TryGetPublicReadableProperty(
                    changes.GetType(), "Count", PublicInstance, $"{label}-default-bundle-change-count",
                    out PropertyInfo countProperty, out stage))
                return false;
            stage = $"{label}-default-bundle-change-count-get";
            bundleChangeCount = Convert.ToInt32(countProperty.GetValue(changes));
        }

        Type? songType = FindType("ePlayableSong", preferredAssembly);
        if (songType?.IsEnum != true) { stage = $"{label}-song-type-missing"; return false; }
        MethodInfo? effectiveMethod = stateType.GetMethod(
            "GetCassetteStatusForSong", PublicInstance, binder: null, types: new[] { songType }, modifiers: null);
        if (effectiveMethod == null) { stage = $"{label}-effective-status-method-missing"; return false; }
        PropertyInfo? progressionProperty = stateType.GetProperty("GameProgression", PublicInstance);
        if (progressionProperty != null && progressionProperty.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-game-progression-getter-non-public"; return false; }
        if (progressionProperty == null)
        {
            PropertyInfo[] progressionProperties = stateType.GetProperties(PublicInstance)
                .Where(property => property.Name.EndsWith(".GameProgression", StringComparison.Ordinal) &&
                    property.GetGetMethod(nonPublic: false)?.IsPublic == true).ToArray();
            if (progressionProperties.Length > 1) { stage = $"{label}-game-progression-ambiguous"; return false; }
            progressionProperty = progressionProperties.SingleOrDefault();
        }
        if (progressionProperty == null) { stage = $"{label}-game-progression-missing"; return false; }
        stage = $"{label}-game-progression-get";
        object? progression = progressionProperty.GetValue(nativeState);
        if (progression == null) { stage = $"{label}-game-progression-null"; return false; }
        MethodInfo? canonicalMethod = progression.GetType().GetMethod(
            "GetSongCassetteStatus", PublicInstance, binder: null, types: new[] { songType }, modifiers: null);
        if (canonicalMethod == null) { stage = $"{label}-canonical-status-method-missing"; return false; }
        var effectiveStatuses = new Dictionary<string, string>(StringComparer.Ordinal);
        var canonicalStatuses = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (string nativeSong in nativeSongs.Distinct(StringComparer.Ordinal).OrderBy(song => song, StringComparer.Ordinal))
        {
            stage = $"{label}-status-{nativeSong}-song-parse";
            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            stage = $"{label}-status-{nativeSong}-effective-invoke";
            string? effective = effectiveMethod.Invoke(nativeState, new[] { song })?.ToString();
            if (string.IsNullOrWhiteSpace(effective)) { stage = $"{label}-status-{nativeSong}-effective-empty"; return false; }
            stage = $"{label}-status-{nativeSong}-canonical-invoke";
            object? rawCanonical = ReadPublicNullableMethod(canonicalMethod, progression, new[] { song });
            if (!TryUnwrapPublicDiagnosticNullable(
                    rawCanonical, $"{label}-status-{nativeSong}-canonical", out bool canonicalPresent, out object? canonical, out stage))
                return false;
            effectiveStatuses[nativeSong] = effective;
            canonicalStatuses[nativeSong] = canonicalPresent ? canonical?.ToString() : null;
        }
        state = new(
            statePointer, hasUnstaged, hasChanges, requires, bundlePresent, bundleChangeCount,
            effectiveStatuses, canonicalStatuses);
        stage = "success";
        return true;
    }

    private static object? ReadPublicNullableMethod(MethodInfo method, object target, object?[] arguments)
    {
        try { return method.Invoke(target, arguments); }
        catch (TargetInvocationException ex) when (IsEmptyIl2CppNullableReturn(method.ReturnType, ex)) { return null; }
    }

    private static bool TryReadPublicBoolean(
        object target, string propertyName, string label, out bool value, out string stage)
    {
        value = false;
        PropertyInfo? property = target.GetType().GetProperty(propertyName, PublicInstance);
        if (property == null) { stage = $"{label}-missing"; return false; }
        if (property.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-getter-non-public"; return false; }
        stage = $"{label}-get";
        value = Convert.ToBoolean(property.GetValue(target));
        return true;
    }

    private static bool TryReadPublicDictionaryValue(
        object dictionary, object key, string label, out object? value, out string stage)
    {
        value = null;
        if (!TryReadPublicDictionaryPresenceAndValue(dictionary, key, label, out bool present, out value, out stage))
            return false;
        if (!present || value == null) { stage = $"{label}-missing"; return false; }
        return true;
    }

    private static bool TryReadPublicDictionaryPresenceAndValue(
        object dictionary, object key, string label, out bool present, out object? value, out string stage)
    {
        present = false;
        value = null;
        Type dictionaryType = dictionary.GetType();
        MethodInfo? containsKey = dictionaryType.GetMethods(PublicInstance)
            .SingleOrDefault(method => string.Equals(method.Name, "ContainsKey", StringComparison.Ordinal) && method.GetParameters().Length == 1);
        PropertyInfo? itemProperty = dictionaryType.GetProperty("Item", PublicInstance);
        if (containsKey == null) { stage = $"{label}-contains-key-missing"; return false; }
        if (itemProperty == null || itemProperty.GetIndexParameters().Length != 1)
        { stage = $"{label}-indexer-missing"; return false; }
        if (itemProperty.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-indexer-getter-non-public"; return false; }
        stage = $"{label}-contains-key-invoke";
        present = Convert.ToBoolean(containsKey.Invoke(dictionary, new[] { key }));
        if (!present) { stage = "success"; return true; }
        stage = $"{label}-get";
        value = itemProperty.GetValue(dictionary, new[] { key });
        if (value == null) { stage = $"{label}-null"; return false; }
        stage = "success";
        return true;
    }

    private static bool TryReadPublicObjectPointer(object? value, string label, out long pointer, out string stage)
    {
        pointer = 0;
        if (value == null) { stage = $"{label}-null"; return false; }
        return TryReadPublicPointer(value, label, out pointer, out stage);
    }

    private static bool TryGetPublicReadableProperty(
        Type type,
        string propertyName,
        BindingFlags flags,
        string label,
        out PropertyInfo property,
        out string stage)
    {
        property = type.GetProperty(propertyName, flags)!;
        if (property == null) { stage = $"{label}-missing"; return false; }
        if (property.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-getter-non-public"; property = null!; return false; }
        stage = "success";
        return true;
    }

    private static bool TryReadPublicWriteDiagnosticStateCore(
        object? processor,
        long? expectedPointer,
        IReadOnlyList<string> nativeSongs,
        out CassettePublicWriteDiagnosticState state,
        out string stage)
    {
        state = default;
        stage = "write-diagnostic-start";
        try
        {
            if (!TryObtainPublicState(processor, "write-diagnostic", out object? nativeState, out stage))
                return false;
            Type stateType = nativeState!.GetType();
            PropertyInfo? pointerProperty = stateType.GetProperty("Pointer", PublicInstance);
            if (pointerProperty == null) { stage = "write-diagnostic-pointer-missing"; return false; }
            stage = "write-diagnostic-pointer-get";
            object? rawPointer = pointerProperty.GetValue(nativeState);
            if (rawPointer is not IntPtr pointer) { stage = "write-diagnostic-pointer-invalid"; return false; }
            if (pointer == IntPtr.Zero) { stage = "write-diagnostic-pointer-zero"; return false; }
            long statePointer = pointer.ToInt64();
            if (expectedPointer.HasValue && statePointer != expectedPointer.Value)
            {
                stage = $"write-diagnostic-pointer-mismatch:expected=0x{expectedPointer.Value:X}:actual=0x{statePointer:X}";
                return false;
            }
            PropertyInfo? hasChangesProperty = stateType.GetProperty("HasChanges", PublicInstance);
            PropertyInfo? requiresProperty = stateType.GetProperty("RequiresWriteToDisk", PublicInstance);
            PropertyInfo? successProperty = stateType.GetProperty("GameTimeOfLastWriteToDisk", PublicInstance);
            PropertyInfo? failureProperty = stateType.GetProperty("GameTimeOfLastFailedAttemptToWriteToDisk", PublicInstance);
            PropertyInfo? reasonProperty = stateType.GetProperty("FailureReasonOfLastFailedAttemptToWriteToDisk", PublicInstance);
            if (hasChangesProperty == null) { stage = "write-diagnostic-has-changes-missing"; return false; }
            if (requiresProperty == null) { stage = "write-diagnostic-requires-write-missing"; return false; }
            if (successProperty == null) { stage = "write-diagnostic-success-time-missing"; return false; }
            if (failureProperty == null) { stage = "write-diagnostic-failure-time-missing"; return false; }
            if (reasonProperty == null) { stage = "write-diagnostic-failure-reason-missing"; return false; }

            PropertyInfo? unstagedProperty = stateType.GetProperty("HasUnstagedChanges", PublicInstance);
            if (unstagedProperty == null) { stage = "write-diagnostic-has-unstaged-missing"; return false; }
            PropertyInfo? redundancyIndexProperty = stateType.GetProperty("SaveFileRedundancyBundleIndex", PublicInstance);
            if (redundancyIndexProperty == null) { stage = "write-diagnostic-redundancy-index-missing"; return false; }
            PropertyInfo? redundancyRevisionProperty = stateType.GetProperty("SaveFileRedundancyBundleRevision", PublicInstance);
            if (redundancyRevisionProperty == null) { stage = "write-diagnostic-redundancy-revision-missing"; return false; }

            stage = "write-diagnostic-has-changes-get";
            bool hasChanges = Convert.ToBoolean(hasChangesProperty.GetValue(nativeState));
            stage = "write-diagnostic-requires-write-get";
            bool requires = Convert.ToBoolean(requiresProperty.GetValue(nativeState));
            stage = "write-diagnostic-success-time-get";
            object? successValue = ReadPublicNullableProperty(successProperty, nativeState);
            if (!TryReadPublicGameTime(successValue, "diagnostic-success-time", out double? success, out stage)) return false;
            stage = "write-diagnostic-failure-time-get";
            object? failureValue = ReadPublicNullableProperty(failureProperty, nativeState);
            if (!TryReadPublicGameTime(failureValue, "diagnostic-failure-time", out double? failure, out stage)) return false;
            stage = "write-diagnostic-failure-reason-get";
            object? reasonValue = ReadPublicNullableProperty(reasonProperty, nativeState);
            if (!TryUnwrapPublicWriteNullable(reasonValue, "diagnostic-failure-reason", out object? reason, out stage)) return false;
            stage = "write-diagnostic-failure-reason-format";
            string? failureReason = reason?.ToString();

            stage = "write-diagnostic-has-unstaged-get";
            bool hasUnstagedChanges = Convert.ToBoolean(unstagedProperty.GetValue(nativeState));
            stage = "write-diagnostic-redundancy-index-get";
            int redundancyIndex = Convert.ToInt32(redundancyIndexProperty.GetValue(nativeState));
            stage = "write-diagnostic-redundancy-revision-get";
            int redundancyRevision = Convert.ToInt32(redundancyRevisionProperty.GetValue(nativeState));

            Type? enquiriesType = FindType("GameTimeEnquiries", processor?.GetType().Assembly);
            if (enquiriesType == null) { stage = "write-diagnostic-current-game-time-owner-missing"; return false; }
            PropertyInfo? currentProperty = enquiriesType.GetProperty("CurrentGameTime", PublicStatic);
            if (currentProperty == null) { stage = "write-diagnostic-current-game-time-missing"; return false; }
            stage = "write-diagnostic-current-game-time-get";
            object? currentGameTime = currentProperty.GetValue(null);
            if (currentGameTime == null) { stage = "write-diagnostic-current-game-time-null"; return false; }
            PropertyInfo? rawProperty = currentGameTime.GetType().GetProperty("RawTime", PublicInstance);
            if (rawProperty == null) { stage = "write-diagnostic-current-game-time-raw-missing"; return false; }
            stage = "write-diagnostic-current-game-time-raw-get";
            object? rawCurrentGameTime = rawProperty.GetValue(currentGameTime);
            if (rawCurrentGameTime == null) { stage = "write-diagnostic-current-game-time-raw-null"; return false; }
            stage = "write-diagnostic-current-game-time-convert";
            double currentRawTime = Convert.ToDouble(rawCurrentGameTime);

            var statuses = new Dictionary<string, string>(StringComparer.Ordinal);
            string[] songs = nativeSongs.Distinct(StringComparer.Ordinal)
                .OrderBy(song => song, StringComparer.Ordinal).ToArray();
            if (songs.Length > 0)
            {
                Type? songType = FindType("ePlayableSong", processor!.GetType().Assembly);
                if (songType?.IsEnum != true) { stage = "write-diagnostic-status-song-type-missing"; return false; }
                MethodInfo? readStatus = stateType.GetMethod(
                    "GetCassetteStatusForSong", PublicInstance, binder: null, types: new[] { songType }, modifiers: null);
                if (readStatus == null) { stage = "write-diagnostic-status-method-missing"; return false; }
                foreach (string nativeSong in songs)
                {
                    stage = $"write-diagnostic-status-{nativeSong}-song-parse";
                    object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
                    stage = $"write-diagnostic-status-{nativeSong}-invoke";
                    string? nativeStatus = readStatus.Invoke(nativeState, new[] { song })?.ToString();
                    if (string.IsNullOrWhiteSpace(nativeStatus))
                    { stage = $"write-diagnostic-status-{nativeSong}-empty"; return false; }
                    statuses[nativeSong] = nativeStatus;
                }
            }

            CassettePublicWriteState writeState = new(
                hasChanges, requires, success, failure, failureReason);
            state = new(
                writeState, currentRawTime, hasUnstagedChanges,
                redundancyIndex, redundancyRevision, statePointer, statuses);
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            state = default;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPlayerSaveWriteCompletedEvent(
        object? nativeEvent,
        out int slot,
        out bool succeeded,
        out string? failureReason,
        out string stage)
    {
        failureReason = null;
        if (!TryReadPlayerSaveWriteCompletedEventHeader(nativeEvent, out slot, out succeeded, out stage))
            return false;
        TryReadPlayerSaveWriteCompletedEventFailureReason(nativeEvent, out failureReason);
        return true;
    }

    internal static bool TryReadPlayerSaveWriteCompletedEventHeader(
        object? nativeEvent,
        out int slot,
        out bool succeeded,
        out string stage)
    {
        slot = default;
        succeeded = false;
        stage = "write-event-start";
        try
        {
            if (nativeEvent == null ||
                !string.Equals(nativeEvent.GetType().Name, "PlayerSaveWriteCompletedEvent", StringComparison.Ordinal))
            {
                stage = "write-event-incompatible";
                return false;
            }
            Type type = nativeEvent.GetType();
            PropertyInfo? slotProperty = type.GetProperty("SlotNumber", PublicInstance);
            PropertyInfo? succeededProperty = type.GetProperty("Succeeded", PublicInstance);
            if (slotProperty == null || succeededProperty == null)
            {
                stage = "write-event-contract-missing";
                return false;
            }
            stage = "write-event-slot-get";
            slot = Convert.ToInt32(slotProperty.GetValue(nativeEvent));
            stage = "write-event-succeeded-get";
            succeeded = Convert.ToBoolean(succeededProperty.GetValue(nativeEvent));
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            slot = default;
            succeeded = false;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
            return false;
        }
    }

    internal static bool TryReadPlayerSaveWriteCompletedEventFailureReason(
        object? nativeEvent,
        out string? failureReason)
    {
        failureReason = null;
        try
        {
            if (nativeEvent == null ||
                !string.Equals(nativeEvent.GetType().Name, "PlayerSaveWriteCompletedEvent", StringComparison.Ordinal))
                return false;
            PropertyInfo? failureProperty = nativeEvent.GetType().GetProperty("FailureReason", PublicInstance);
            if (failureProperty == null) return true;
            object? failureValue = ReadPublicNullableProperty(failureProperty, nativeEvent);
            if (TryUnwrapPublicWriteNullable(
                    failureValue, "event-failure-reason", out object? reason, out _))
                failureReason = reason?.ToString();
            return true;
        }
        catch
        {
            failureReason = null;
            return true;
        }
    }

    private static object? ReadPublicNullableProperty(PropertyInfo property, object target)
    {
        try { return property.GetValue(target); }
        catch (TargetInvocationException ex) when (IsEmptyIl2CppNullableReturn(property, ex)) { return null; }
    }

    private static bool IsEmptyIl2CppNullableReturn(PropertyInfo property, TargetInvocationException exception)
        => IsEmptyIl2CppNullableReturn(property.PropertyType, exception);

    private static bool IsEmptyIl2CppNullableReturn(Type returnType, TargetInvocationException exception)
    {
        if (!returnType.IsGenericType ||
            !string.Equals(returnType.GetGenericTypeDefinition().FullName, "Il2CppSystem.Nullable`1", StringComparison.Ordinal))
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

    private static bool TryReadBundleRoutingStateCore(
        object nativeState,
        Assembly preferredAssembly,
        long expectedPointer,
        IReadOnlyList<string> nativeSongs,
        string label,
        out CassetteBundleRoutingState state,
        out string stage)
    {
        state = default;
        stage = $"{label}-state-start";
        Type stateType = nativeState.GetType();
        PropertyInfo? pointerProperty = stateType.GetProperty("Pointer", PublicInstance);
        if (pointerProperty == null) { stage = $"{label}-pointer-missing"; return false; }
        stage = $"{label}-pointer-get";
        object? rawPointer = pointerProperty.GetValue(nativeState);
        if (rawPointer is not IntPtr pointer) { stage = $"{label}-pointer-invalid"; return false; }
        if (pointer == IntPtr.Zero) { stage = $"{label}-pointer-zero"; return false; }
        long statePointer = pointer.ToInt64();
        if (statePointer != expectedPointer)
        {
            stage = $"{label}-pointer-mismatch:expected=0x{expectedPointer:X}:actual=0x{statePointer:X}";
            return false;
        }
        PropertyInfo? unstagedProperty = stateType.GetProperty("HasUnstagedChanges", PublicInstance);
        if (unstagedProperty == null) { stage = $"{label}-has-unstaged-missing"; return false; }
        stage = $"{label}-has-unstaged-get";
        bool hasUnstagedChanges = Convert.ToBoolean(unstagedProperty.GetValue(nativeState));
        Type? songType = FindType("ePlayableSong", preferredAssembly);
        if (songType?.IsEnum != true) { stage = $"{label}-song-type-missing"; return false; }
        MethodInfo? readStatus = stateType.GetMethod(
            "GetCassetteStatusForSong",
            PublicInstance,
            binder: null,
            types: new[] { songType },
            modifiers: null);
        if (readStatus == null) { stage = $"{label}-status-method-missing"; return false; }
        var statuses = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string nativeSong in nativeSongs.Distinct(StringComparer.Ordinal).OrderBy(song => song, StringComparer.Ordinal))
        {
            stage = $"{label}-status-{nativeSong}-song-parse";
            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            stage = $"{label}-status-{nativeSong}-invoke";
            string? status = readStatus.Invoke(nativeState, new[] { song })?.ToString();
            if (string.IsNullOrWhiteSpace(status)) { stage = $"{label}-status-{nativeSong}-empty"; return false; }
            statuses[nativeSong] = status;
        }
        state = new(statePointer, hasUnstagedChanges, statuses);
        stage = "success";
        return true;
    }

    private static bool TryUnwrapPublicDiagnosticNullable(
        object? value,
        string label,
        out bool present,
        out object? unwrapped,
        out string stage)
    {
        present = false;
        unwrapped = null;
        stage = $"{label}-start";
        if (value == null) { stage = "success"; return true; }
        Type type = value.GetType();
        string typeName = type.FullName ?? type.Name;
        if (!typeName.Contains("Nullable`1", StringComparison.Ordinal))
        {
            present = true;
            unwrapped = value;
            stage = "success";
            return true;
        }
        PropertyInfo? hasValueProperty = type.GetProperty("HasValue", PublicInstance);
        PropertyInfo? valueProperty = type.GetProperty("Value", PublicInstance);
        if (hasValueProperty == null || valueProperty == null)
        { stage = $"{label}-contract-missing"; return false; }
        if (hasValueProperty.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-has-value-getter-non-public"; return false; }
        if (valueProperty.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-value-getter-non-public"; return false; }
        stage = $"{label}-has-value-get";
        object? rawPresent = hasValueProperty.GetValue(value);
        if (rawPresent is not bool hasValue) { stage = $"{label}-has-value-invalid"; return false; }
        present = hasValue;
        if (!present) { stage = "success"; return true; }
        stage = $"{label}-value-get";
        unwrapped = valueProperty.GetValue(value);
        if (unwrapped == null) { stage = $"{label}-value-null"; return false; }
        stage = "success";
        return true;
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
        if (pointerProperty == null) { stage = $"{label}-pointer-missing"; return false; }
        if (pointerProperty.GetGetMethod(nonPublic: false)?.IsPublic != true)
        { stage = $"{label}-pointer-getter-non-public"; return false; }
        stage = $"{label}-pointer-get";
        object? rawPointer = pointerProperty.GetValue(state);
        if (rawPointer is not IntPtr pointer) { stage = $"{label}-pointer-invalid"; return false; }
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
        out string? nativeStatus) =>
        TryReadCassetteStatus(processor, nativeSong, out nativeStatus, out _);

    internal static bool TryReadCassetteStatus(
        object processor,
        string nativeSong,
        out string? nativeStatus,
        out string stage)
    {
        nativeStatus = null;
        stage = "cassette-status-start";
        try
        {
            stage = "cassette-status-obtain-state-find";
            MethodInfo? obtainState = processor.GetType().GetMethods(AllInstance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "ObtainState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            if (obtainState == null) { stage = "cassette-status-obtain-state-missing"; return false; }
            stage = "cassette-status-obtain-state-invoke";
            object? state = obtainState.Invoke(processor, null);
            if (state == null) { stage = "cassette-status-state-null"; return false; }

            stage = "cassette-status-contract-find";
            Type? songType = FindType("ePlayableSong", processor.GetType().Assembly);
            MethodInfo? readStatus = songType == null ? null : state.GetType().GetMethod(
                "GetCassetteStatusForSong", AllInstance, binder: null, types: new[] { songType }, modifiers: null);
            if (readStatus == null || songType?.IsEnum != true)
            { stage = "cassette-status-contract-missing"; return false; }

            stage = $"cassette-status-{nativeSong}-song-parse";
            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            stage = $"cassette-status-{nativeSong}-invoke";
            nativeStatus = UnwrapNullable(readStatus.Invoke(state, new[] { song }))?.ToString();
            if (string.IsNullOrWhiteSpace(nativeStatus))
            { stage = $"cassette-status-{nativeSong}-empty"; return false; }
            stage = "success";
            return true;
        }
        catch (Exception ex)
        {
            nativeStatus = null;
            stage = $"{stage}-invocation:{SummarizeException(ex)}";
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

            if (!CassetteNativeRequestFactory.TryCreateHaveInBagRequest(
                    requestType,
                    songType,
                    statusType,
                    bundleType,
                    nativeSong,
                    out object? cassetteRequest,
                    out detail) || cassetteRequest == null)
                return false;
            process.Invoke(processor, new[] { cassetteRequest });
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
            if (!CassetteNativeRequestFactory.TryCreateDefaultUrgentPersistRequest(
                    requestType,
                    bundleType,
                    writeType,
                    out object? request,
                    out detail) || request == null)
                return false;
            MethodInfo? submitDefinition = requestSystemType.GetMethods(PublicStatic)
                .SingleOrDefault(method => string.Equals(method.Name, "SubmitRequest", StringComparison.Ordinal) &&
                    method.IsGenericMethodDefinition && method.GetGenericArguments().Length == 1 &&
                    method.GetParameters().Length == 1);
            if (submitDefinition == null) { detail = "public RequestSystem.SubmitRequest<T> unavailable"; return false; }
            submitDefinition.MakeGenericMethod(requestType).Invoke(null, new[] { request });
            detail += " route='RequestSystem.SubmitRequest<T>'";
            return true;
        }
        catch (Exception ex)
        {
            detail = $"public persist invocation:{SummarizeException(ex)}";
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
