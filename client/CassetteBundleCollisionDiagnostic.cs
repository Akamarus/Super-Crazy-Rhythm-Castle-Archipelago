using System.Reflection;

namespace RhythmCastleAP;

internal sealed class CassetteBundleCollisionPatchState
{
    internal string Song { get; set; } = "<unavailable>";
    internal string RequestedStatus { get; set; } = "<unavailable>";
    internal string RawBundle { get; set; } = "<unavailable>";
    internal string EffectiveBundle { get; set; } = "<unavailable>";
    internal string ProcessorStatePointer { get; set; } = "<unavailable>";
    internal string EnquiryStatePointer { get; set; } = "<unavailable>";
    internal bool? CrossBundle { get; set; }
    internal string CrossBundleStatus { get; set; } = "<unavailable>";
    internal string BeforeStatus { get; set; } = "<unavailable>";
    internal string AfterStatus { get; set; } = "<unavailable>";
    internal string? Error { get; set; }
    internal object? NativeState { get; set; }
    internal object? NativeSong { get; set; }
    internal MethodInfo? StatusMethod { get; set; }
}

internal static class CassetteBundleCollisionDiagnostic
{
    internal static CassetteBundleCollisionPatchState? Begin(
        bool isApOrigin,
        object? processor,
        object? request)
    {
        if (!isApOrigin || processor == null || request == null)
            return null;

        var capture = new CassetteBundleCollisionPatchState();
        try
        {
            object? song = ReadMember(request, "Song") ??
                           ReadMember(request, "_Song_k__BackingField");
            object? requestedStatus = ReadMember(request, "CassetteStatus") ??
                                      ReadMember(request, "_CassetteStatus_k__BackingField");
            object? rawBundle = ReadMember(request, "Bundle") ??
                                ReadMember(request, "_Bundle_k__BackingField");
            object? rawBundleValue = UnwrapNullable(rawBundle);
            if (song == null || requestedStatus == null)
                throw new InvalidOperationException("cassette request song or status unavailable");

            capture.NativeSong = song;
            capture.Song = song.ToString() ?? "<unavailable>";
            capture.RequestedStatus = requestedStatus.ToString() ?? "<unavailable>";
            capture.RawBundle = rawBundleValue?.ToString() ?? "<null>";

            MethodInfo? obtainState = processor.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "ObtainState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            capture.NativeState = obtainState?.Invoke(processor, null) ??
                                  throw new InvalidOperationException("PlayerSaveRequestProcessor.ObtainState unavailable");
            capture.ProcessorStatePointer = Pointer(capture.NativeState);
            capture.EnquiryStatePointer = SelectedEnquiryStatePointer(request.GetType().Assembly);

            capture.StatusMethod = capture.NativeState.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "GetCassetteStatusForSong", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 1 &&
                    method.GetParameters()[0].ParameterType == song.GetType()) ??
                throw new InvalidOperationException("PlayerSaveFileState.GetCassetteStatusForSong unavailable");
            capture.BeforeStatus = capture.StatusMethod.Invoke(
                capture.NativeState, new[] { song })?.ToString() ?? "<null>";

            MethodInfo predicate = capture.NativeState.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "HasSongCassetteStatusBeenChangedByAnyBundle", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 3 &&
                    method.GetParameters()[0].ParameterType == song.GetType()) ??
                throw new InvalidOperationException("PlayerSaveFileState.HasSongCassetteStatusBeenChangedByAnyBundle unavailable");
            ParameterInfo[] predicateParameters = predicate.GetParameters();
            Type bundleType = BundleValueType(predicateParameters[2].ParameterType, rawBundleValue) ??
                              throw new InvalidOperationException("cassette bundle enum unavailable");
            object effectiveBundle = rawBundleValue ?? Enum.Parse(
                bundleType, CassetteNativeRequestFactory.DefaultBundle, ignoreCase: false);
            capture.EffectiveBundle = effectiveBundle.ToString() ?? "<unavailable>";

            Type outStatusType = predicateParameters[1].ParameterType.GetElementType() ??
                                 throw new InvalidOperationException("cross-bundle out-status type unavailable");
            object? bundleExclusion = rawBundleValue != null && rawBundle != null &&
                                      predicateParameters[2].ParameterType.IsInstanceOfType(rawBundle)
                ? rawBundle
                : Activator.CreateInstance(
                    predicateParameters[2].ParameterType,
                    new[] { effectiveBundle });
            object?[] predicateArgs =
            {
                song,
                Activator.CreateInstance(outStatusType),
                bundleExclusion
            };
            object? predicateResult = predicate.Invoke(capture.NativeState, predicateArgs);
            capture.CrossBundle = predicateResult is bool value ? value : null;
            capture.CrossBundleStatus = UnwrapNullable(predicateArgs[1])?.ToString() ?? "<null>";
        }
        catch (Exception ex)
        {
            capture.Error = ex.GetBaseException().Message;
        }

        return capture;
    }

    internal static string? Complete(CassetteBundleCollisionPatchState? capture)
    {
        if (capture == null)
            return null;

        try
        {
            if (capture.NativeState != null && capture.NativeSong != null && capture.StatusMethod != null)
            {
                capture.AfterStatus = capture.StatusMethod.Invoke(
                    capture.NativeState, new[] { capture.NativeSong })?.ToString() ?? "<null>";
            }
        }
        catch (Exception ex)
        {
            capture.Error = capture.Error == null
                ? ex.GetBaseException().Message
                : capture.Error + "; after-status: " + ex.GetBaseException().Message;
        }

        return CassetteBundleCollisionDiagnosticPolicy.Format(
            new CassetteBundleCollisionObservation(
                capture.Song,
                capture.RequestedStatus,
                capture.RawBundle,
                capture.EffectiveBundle,
                capture.ProcessorStatePointer,
                capture.EnquiryStatePointer,
                capture.CrossBundle,
                capture.CrossBundleStatus,
                capture.BeforeStatus,
                capture.AfterStatus,
                capture.Error));
    }

    private static Type? BundleValueType(Type nullableBundleType, object? rawBundleValue)
    {
        if (rawBundleValue?.GetType().IsEnum == true)
            return rawBundleValue.GetType();
        Type? managedNullable = Nullable.GetUnderlyingType(nullableBundleType);
        if (managedNullable?.IsEnum == true)
            return managedNullable;
        Type? genericValue = nullableBundleType.IsGenericType
            ? nullableBundleType.GetGenericArguments().FirstOrDefault()
            : null;
        return genericValue?.IsEnum == true ? genericValue : null;
    }

    private static string SelectedEnquiryStatePointer(Assembly gameAssembly)
    {
        try
        {
            Type? enquiries = gameAssembly.GetType(
                "CurrentPlayerSaveEnquiries", throwOnError: false, ignoreCase: false);
            MethodInfo? selectedState = enquiries?.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(method =>
                    string.Equals(method.Name, "GetSelectedSaveFileState", StringComparison.Ordinal) &&
                    method.GetParameters().Length == 0);
            object? state = selectedState?.Invoke(null, null);
            return state == null ? "<unavailable>" : Pointer(state);
        }
        catch
        {
            return "<unavailable>";
        }
    }

    private static object? ReadMember(object? obj, string name)
    {
        if (obj == null)
            return null;
        Type type = obj.GetType();
        try
        {
            PropertyInfo? property = type.GetProperty(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property != null && property.GetIndexParameters().Length == 0)
                return property.GetValue(obj);
        }
        catch { }
        try
        {
            FieldInfo? field = type.GetField(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(obj);
        }
        catch
        {
            return null;
        }
    }

    private static object? UnwrapNullable(object? value)
    {
        if (value == null)
            return null;
        Type type = value.GetType();
        if (!(type.FullName ?? type.Name).Contains("Nullable`1", StringComparison.Ordinal))
            return value;
        object? hasValue = ReadMember(value, "HasValue");
        if (hasValue is bool present && !present)
            return null;
        return ReadMember(value, "Value") ?? value;
    }

    private static string Pointer(object value)
    {
        try
        {
            object? raw = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value);
            return raw is IntPtr pointer && pointer != IntPtr.Zero
                ? $"0x{pointer.ToInt64():X}"
                : "<unavailable>";
        }
        catch
        {
            return "<unavailable>";
        }
    }
}
