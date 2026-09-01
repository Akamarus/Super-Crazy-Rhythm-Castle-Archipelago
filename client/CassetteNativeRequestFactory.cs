using System;
using System.Reflection;

namespace RhythmCastleAP;

internal static class CassetteNativeRequestFactory
{
    private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
    internal const string HaveInBag = "HAVE_IN_BAG";
    internal const string DefaultBundle = "DEFAULT";

    internal static object Create(
        Type requestType,
        Type songType,
        Type statusType,
        Type bundleType,
        string nativeSong,
        string nativeStatus,
        object nativeBundle)
    {
        if (!TryCreateVerified(
                requestType,
                songType,
                statusType,
                bundleType,
                nativeSong,
                nativeStatus,
                nativeBundle,
                out object? request,
                out string detail))
            throw new InvalidOperationException(detail);
        return request!;
    }

    internal static bool TryCreateHaveInBagRequest(
        Type requestType,
        Type songType,
        Type statusType,
        Type bundleType,
        string nativeSong,
        out object? request,
        out string detail)
    {
        request = null;
        detail = string.Empty;

        if (!songType.IsEnum || !statusType.IsEnum || !bundleType.IsEnum)
        {
            detail = "cassette request semantic types are not enums";
            return false;
        }
        object song;
        object status;
        object bundle;
        try
        {
            song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            status = Enum.Parse(statusType, HaveInBag, ignoreCase: false);
            bundle = Enum.Parse(bundleType, DefaultBundle, ignoreCase: false);
        }
        catch (Exception ex)
        {
            detail = $"cassette request semantic-value-parse-invocation:{Summarize(ex)}";
            return false;
        }
        return TryCreateVerified(
            requestType,
            songType,
            statusType,
            bundleType,
            nativeSong,
            HaveInBag,
            bundle,
            out request,
            out detail);
    }

    internal static bool TryCreateDefaultUrgentPersistRequest(
        Type requestType,
        Type bundleType,
        Type writeType,
        out object? request,
        out string detail)
    {
        request = null;
        detail = string.Empty;
        if (!bundleType.IsEnum || !writeType.IsEnum)
        { detail = "public persist semantic types are not enums"; return false; }

        object nativeBundle;
        object nativeWriteType;
        try
        {
            nativeBundle = Enum.Parse(bundleType, DefaultBundle, ignoreCase: false);
            nativeWriteType = Enum.Parse(writeType, "URGENT", ignoreCase: false);
        }
        catch (Exception ex)
        {
            detail = $"public persist semantic-value-parse-invocation:{Summarize(ex)}";
            return false;
        }
        if (!string.Equals(nativeBundle.ToString(), DefaultBundle, StringComparison.Ordinal) ||
            Convert.ToInt32(nativeBundle) != 1 ||
            !string.Equals(nativeWriteType.ToString(), "URGENT", StringComparison.Ordinal) ||
            Convert.ToInt32(nativeWriteType) != 0)
        { detail = "public persist enum values did not match DEFAULT=1/URGENT=0"; return false; }

        ConstructorInfo? constructor = requestType.GetConstructor(
            PublicInstance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        if (constructor == null)
        { detail = "public parameterless persist request constructor unavailable"; return false; }
        try { request = constructor.Invoke(Array.Empty<object>()); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request parameterless-constructor-invocation:{Summarize(ex)}";
            return false;
        }
        if (request == null)
        { detail = "persist request parameterless constructor returned null"; return false; }

        PropertyInfo? bundleProperty = requestType.GetProperty("Bundle", PublicInstance);
        PropertyInfo? writeProperty = requestType.GetProperty("WriteTypeToRequest", PublicInstance);
        MethodInfo? bundleGetter = bundleProperty?.GetMethod;
        MethodInfo? writeGetter = writeProperty?.GetMethod;
        MethodInfo? writeSetter = writeProperty?.SetMethod;
        if (bundleProperty == null || bundleGetter?.IsPublic != true)
        { request = null; detail = "public persist Bundle getter unavailable"; return false; }
        if (writeProperty?.PropertyType != writeType ||
            writeGetter?.IsPublic != true ||
            writeSetter?.IsPublic != true)
        { request = null; detail = "public persist WriteTypeToRequest getter/setter unavailable"; return false; }

        try { writeSetter.Invoke(request, new[] { nativeWriteType }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request write-type-set-invocation:{Summarize(ex)}";
            return false;
        }

        object? actualWriteType;
        try { actualWriteType = writeProperty.GetValue(request); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request write-type-readback-invocation:{Summarize(ex)}";
            return false;
        }
        if (actualWriteType == null)
        { request = null; detail = "persist request write-type readback was null"; return false; }
        string actualWriteTypeName = actualWriteType.ToString() ?? "<null>";
        int actualWriteTypeValue;
        try { actualWriteTypeValue = Convert.ToInt32(actualWriteType); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request write-type-numeric-readback-invocation:{Summarize(ex)}";
            return false;
        }
        if (!Equals(actualWriteType, nativeWriteType) ||
            !string.Equals(actualWriteTypeName, "URGENT", StringComparison.Ordinal) ||
            actualWriteTypeValue != 0)
        {
            request = null;
            detail = $"persist request write-type readback mismatch:expected=URGENT/0:actual={actualWriteTypeName}/{actualWriteTypeValue}";
            return false;
        }

        object? actualNullableBundle;
        bool exactEmptyInteropBundle = false;
        try { actualNullableBundle = bundleProperty.GetValue(request); }
        catch (TargetInvocationException ex) when (IsEmptyIl2CppNullableReturn(bundleProperty, bundleType, ex))
        {
            actualNullableBundle = null;
            exactEmptyInteropBundle = true;
        }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request bundle-readback-invocation:{Summarize(ex)}";
            return false;
        }
        if (actualNullableBundle == null && !exactEmptyInteropBundle)
        { request = null; detail = "persist request bundle readback was null"; return false; }
        if (exactEmptyInteropBundle)
        {
            detail = "actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)' actualWriteType='URGENT/0'";
            return true;
        }

        object nonNullNullableBundle = actualNullableBundle!;
        Type nullableType = nonNullNullableBundle.GetType();
        PropertyInfo? hasValueProperty = nullableType.GetProperty("HasValue", PublicInstance);
        PropertyInfo? valueProperty = nullableType.GetProperty("Value", PublicInstance);
        if (!nullableType.IsGenericType ||
            nullableType.GetGenericArguments().Length != 1 ||
            nullableType.GetGenericArguments()[0] != bundleType ||
            hasValueProperty?.GetMethod?.IsPublic != true ||
            valueProperty?.GetMethod?.IsPublic != true)
        { request = null; detail = "public persist Bundle nullable contract unavailable"; return false; }

        bool present;
        try
        {
            object? hasValue = hasValueProperty.GetValue(nonNullNullableBundle);
            if (hasValue is not bool boolValue)
            { request = null; detail = "persist request Bundle HasValue readback was not Boolean"; return false; }
            present = boolValue;
        }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request bundle-has-value-readback-invocation:{Summarize(ex)}";
            return false;
        }
        if (!present)
        {
            detail = "actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)' actualWriteType='URGENT/0'";
            return true;
        }

        object? actualBundle;
        try { actualBundle = valueProperty.GetValue(nonNullNullableBundle); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request bundle-value-readback-invocation:{Summarize(ex)}";
            return false;
        }
        if (actualBundle == null)
        { request = null; detail = "persist request bundle readback mismatch:expected=<absent>:actual=<null>"; return false; }
        string actualBundleName = actualBundle.ToString() ?? "<null>";
        int actualBundleValue;
        try { actualBundleValue = Convert.ToInt32(actualBundle); }
        catch (Exception ex)
        {
            request = null;
            detail = $"persist request bundle-numeric-readback-invocation:{Summarize(ex)}";
            return false;
        }
        request = null;
        detail = $"persist request bundle readback mismatch:expected=<absent>:actual={actualBundleName}/{actualBundleValue}";
        return false;
    }

    private static bool TryCreateVerified(
        Type requestType,
        Type songType,
        Type statusType,
        Type bundleType,
        string nativeSong,
        string nativeStatus,
        object nativeBundle,
        out object? request,
        out string detail)
    {
        request = null;
        detail = string.Empty;
        if (!songType.IsEnum || !statusType.IsEnum || !bundleType.IsEnum)
        { detail = "cassette request semantic types are not enums"; return false; }
        if (!bundleType.IsInstanceOfType(nativeBundle))
        { detail = "native bundle does not match cassette request bundle type"; return false; }

        object song;
        object status;
        try
        {
            song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            status = Enum.Parse(statusType, nativeStatus, ignoreCase: false);
        }
        catch (Exception ex)
        {
            detail = $"cassette request semantic-value-parse-invocation:{Summarize(ex)}";
            return false;
        }
        if (!string.Equals(nativeBundle.ToString(), DefaultBundle, StringComparison.Ordinal) ||
            Convert.ToInt32(nativeBundle) != 1)
        { detail = "cassette request DEFAULT enum did not match name/value DEFAULT/1"; return false; }

        ConstructorInfo? constructor = requestType.GetConstructor(
            PublicInstance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        if (constructor == null)
        { detail = "public parameterless cassette request constructor unavailable"; return false; }
        try { request = constructor.Invoke(Array.Empty<object>()); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request parameterless-constructor-invocation:{Summarize(ex)}";
            return false;
        }
        if (request == null)
        { detail = "cassette request parameterless constructor returned null"; return false; }

        PropertyInfo? songProperty = requestType.GetProperty("Song", PublicInstance);
        PropertyInfo? statusProperty = requestType.GetProperty("CassetteStatus", PublicInstance);
        PropertyInfo? bundleProperty = requestType.GetProperty("Bundle", PublicInstance);
        MethodInfo? songGetter = songProperty?.GetMethod;
        MethodInfo? songSetter = songProperty?.SetMethod;
        MethodInfo? statusGetter = statusProperty?.GetMethod;
        MethodInfo? statusSetter = statusProperty?.SetMethod;
        MethodInfo? bundleGetter = bundleProperty?.GetMethod;
        if (songGetter?.IsPublic != true || songSetter?.IsPublic != true)
        { request = null; detail = "cassette request public Song getter/setter unavailable"; return false; }
        if (statusGetter?.IsPublic != true || statusSetter?.IsPublic != true)
        { request = null; detail = "cassette request public CassetteStatus getter/setter unavailable"; return false; }
        if (bundleProperty == null || bundleGetter?.IsPublic != true)
        { request = null; detail = "cassette request public Bundle getter unavailable"; return false; }
        try { songSetter.Invoke(request, new[] { song }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request song-set-invocation:{Summarize(ex)}";
            return false;
        }
        try { statusSetter.Invoke(request, new[] { status }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request status-set-invocation:{Summarize(ex)}";
            return false;
        }
        object? actualSong;
        object? actualStatus;
        object? actualNullableBundle;
        try { actualSong = songProperty!.GetValue(request); }
        catch (Exception ex)
        { request = null; detail = $"cassette request song-readback-invocation:{Summarize(ex)}"; return false; }
        try { actualStatus = statusProperty!.GetValue(request); }
        catch (Exception ex)
        { request = null; detail = $"cassette request status-readback-invocation:{Summarize(ex)}"; return false; }
        bool exactEmptyInteropBundle = false;
        try { actualNullableBundle = bundleProperty.GetValue(request); }
        catch (TargetInvocationException ex) when (IsEmptyIl2CppNullableReturn(bundleProperty, bundleType, ex))
        {
            actualNullableBundle = null;
            exactEmptyInteropBundle = true;
        }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-readback-invocation:{Summarize(ex)}"; return false; }
        if (!Equals(actualSong, song))
        { request = null; detail = $"cassette request song readback mismatch:expected={song}:actual={actualSong ?? "<null>"}"; return false; }
        if (!Equals(actualStatus, status))
        { request = null; detail = $"cassette request status readback mismatch:expected={status}:actual={actualStatus ?? "<null>"}"; return false; }
        if (actualNullableBundle == null && !exactEmptyInteropBundle)
        { request = null; detail = "cassette request bundle readback was null"; return false; }
        if (exactEmptyInteropBundle)
        {
            detail = $"actualSong='{actualSong}' actualStatus='{actualStatus}' actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)'";
            return true;
        }

        object nonNullNullableBundle = actualNullableBundle!;
        Type nullableType = nonNullNullableBundle.GetType();
        PropertyInfo? hasValueProperty = nullableType.GetProperty("HasValue", PublicInstance);
        PropertyInfo? valueProperty = nullableType.GetProperty("Value", PublicInstance);
        if (!nullableType.IsGenericType ||
            nullableType.GetGenericArguments().Length != 1 ||
            nullableType.GetGenericArguments()[0] != bundleType ||
            hasValueProperty?.GetMethod?.IsPublic != true ||
            valueProperty?.GetMethod?.IsPublic != true)
        { request = null; detail = "cassette request public Bundle nullable contract unavailable"; return false; }
        bool present;
        try
        {
            object? hasValue = hasValueProperty.GetValue(nonNullNullableBundle);
            if (hasValue is not bool boolValue)
            { request = null; detail = "cassette request Bundle HasValue readback was not Boolean"; return false; }
            present = boolValue;
        }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-has-value-readback-invocation:{Summarize(ex)}"; return false; }
        if (!present)
        {
            detail = $"actualSong='{actualSong}' actualStatus='{actualStatus}' actualBundle='present=False value=<null>' effectiveBundle='DEFAULT/1(native absent fallback)'";
            return true;
        }

        object? actualBundle;
        try { actualBundle = valueProperty.GetValue(nonNullNullableBundle); }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-value-readback-invocation:{Summarize(ex)}"; return false; }
        if (actualBundle == null)
        { request = null; detail = "cassette request bundle readback mismatch:expected=<absent>:actual=<null>"; return false; }
        string actualBundleName = actualBundle.ToString() ?? "<null>";
        int actualBundleValue;
        try { actualBundleValue = Convert.ToInt32(actualBundle); }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-numeric-readback-invocation:{Summarize(ex)}"; return false; }
        request = null;
        detail = $"cassette request bundle readback mismatch:expected=<absent>:actual={actualBundleName}/{actualBundleValue}";
        return false;
    }

    private static bool IsEmptyIl2CppNullableReturn(
        PropertyInfo property,
        Type bundleType,
        TargetInvocationException exception)
    {
        Type propertyType = property.PropertyType;
        if (!propertyType.IsGenericType ||
            propertyType.GetGenericArguments().Length != 1 ||
            propertyType.GetGenericArguments()[0] != bundleType ||
            !string.Equals(propertyType.GetGenericTypeDefinition().FullName,
                "Il2CppSystem.Nullable`1", StringComparison.Ordinal))
            return false;
        Exception? inner = exception.InnerException;
        return inner is NullReferenceException &&
            string.Equals(inner.TargetSite?.Name, "CreateGCHandle", StringComparison.Ordinal) &&
            string.Equals(inner.TargetSite?.DeclaringType?.FullName,
                "Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase", StringComparison.Ordinal);
    }

    private static string Summarize(Exception exception)
    {
        Exception root = exception.GetBaseException();
        string message = (root.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (message.Length > 160) message = message[..160];
        return $"{root.GetType().Name}:{message}";
    }
}
