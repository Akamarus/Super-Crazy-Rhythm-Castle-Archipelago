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
            types: new[] { songType, statusType, bundleType },
            modifiers: null);
        if (constructor == null)
        { detail = "exact public cassette request constructor (song, status, bundle) unavailable"; return false; }
        try { request = constructor.Invoke(new[] { song, status, nativeBundle }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request semantic-constructor-invocation:{Summarize(ex)}";
            return false;
        }
        if (request == null)
        { detail = "cassette request semantic constructor returned null"; return false; }

        PropertyInfo? bundleProperty = requestType.GetProperty("Bundle", PublicInstance);
        MethodInfo? bundleGetter = bundleProperty?.GetMethod;
        MethodInfo? bundleSetter = bundleProperty?.SetMethod;
        if (bundleProperty == null || bundleGetter?.IsPublic != true)
        { request = null; detail = "cassette request public Bundle getter unavailable"; return false; }
        if (bundleSetter?.IsPublic != true)
        { request = null; detail = "cassette request public Bundle setter unavailable"; return false; }
        ConstructorInfo? nullableConstructor = bundleProperty.PropertyType.GetConstructor(
            PublicInstance,
            binder: null,
            types: new[] { bundleType },
            modifiers: null);
        if (nullableConstructor == null)
        { request = null; detail = "cassette request public DEFAULT nullable constructor unavailable"; return false; }
        object nullableBundle;
        try { nullableBundle = nullableConstructor.Invoke(new[] { nativeBundle }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request nullable-build-invocation:{Summarize(ex)}";
            return false;
        }
        try { bundleSetter.Invoke(request, new[] { nullableBundle }); }
        catch (Exception ex)
        {
            request = null;
            detail = $"cassette request bundle-set-invocation:{Summarize(ex)}";
            return false;
        }

        PropertyInfo? songProperty = requestType.GetProperty("Song", PublicInstance);
        PropertyInfo? statusProperty = requestType.GetProperty("CassetteStatus", PublicInstance);
        if (songProperty?.GetMethod?.IsPublic != true)
        { request = null; detail = "cassette request public Song getter unavailable"; return false; }
        if (statusProperty?.GetMethod?.IsPublic != true)
        { request = null; detail = "cassette request public CassetteStatus getter unavailable"; return false; }
        object? actualSong;
        object? actualStatus;
        object? actualNullableBundle;
        try { actualSong = songProperty.GetValue(request); }
        catch (Exception ex)
        { request = null; detail = $"cassette request song-readback-invocation:{Summarize(ex)}"; return false; }
        try { actualStatus = statusProperty.GetValue(request); }
        catch (Exception ex)
        { request = null; detail = $"cassette request status-readback-invocation:{Summarize(ex)}"; return false; }
        try { actualNullableBundle = bundleProperty.GetValue(request); }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-readback-invocation:{Summarize(ex)}"; return false; }
        if (!Equals(actualSong, song))
        { request = null; detail = $"cassette request song readback mismatch:expected={song}:actual={actualSong ?? "<null>"}"; return false; }
        if (!Equals(actualStatus, status))
        { request = null; detail = $"cassette request status readback mismatch:expected={status}:actual={actualStatus ?? "<null>"}"; return false; }
        if (actualNullableBundle == null)
        { request = null; detail = "cassette request bundle readback was null"; return false; }
        Type nullableType = actualNullableBundle.GetType();
        PropertyInfo? hasValueProperty = nullableType.GetProperty("HasValue", PublicInstance);
        PropertyInfo? valueProperty = nullableType.GetProperty("Value", PublicInstance);
        if (hasValueProperty?.GetMethod?.IsPublic != true || valueProperty?.GetMethod?.IsPublic != true)
        { request = null; detail = "cassette request public Bundle nullable contract unavailable"; return false; }
        bool present;
        object? actualBundle;
        try
        {
            present = hasValueProperty.GetValue(actualNullableBundle) is bool hasValue && hasValue;
            actualBundle = present ? valueProperty.GetValue(actualNullableBundle) : null;
        }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-value-readback-invocation:{Summarize(ex)}"; return false; }
        if (!present || actualBundle == null)
        { request = null; detail = "cassette request bundle readback mismatch:expected=DEFAULT/1:actual=<absent>"; return false; }
        string actualBundleName = actualBundle.ToString() ?? "<null>";
        int actualBundleValue;
        try { actualBundleValue = Convert.ToInt32(actualBundle); }
        catch (Exception ex)
        { request = null; detail = $"cassette request bundle-numeric-readback-invocation:{Summarize(ex)}"; return false; }
        if (!string.Equals(actualBundleName, DefaultBundle, StringComparison.Ordinal) || actualBundleValue != 1)
        {
            request = null;
            detail = $"cassette request bundle readback mismatch:expected=DEFAULT/1:actual={actualBundleName}/{actualBundleValue}";
            return false;
        }

        detail = $"actualSong='{actualSong}' actualStatus='{actualStatus}' actualBundle='present=True value={actualBundleName} numeric={actualBundleValue}'";
        return true;
    }

    private static string Summarize(Exception exception)
    {
        Exception root = exception.GetBaseException();
        string message = (root.Message ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ').Trim();
        if (message.Length > 160) message = message[..160];
        return $"{root.GetType().Name}:{message}";
    }
}
