using System;
using System.Reflection;

namespace RhythmCastleAP;

internal static class CassetteNativeRequestFactory
{
    internal const string HaveInBag = "HAVE_IN_BAG";
    internal const string DefaultBundle = "DEFAULT";

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

        try
        {
            if (!songType.IsEnum || !statusType.IsEnum || !bundleType.IsEnum)
            {
                detail = "cassette request semantic types are not enums";
                return false;
            }

            object song = Enum.Parse(songType, nativeSong, ignoreCase: false);
            object status = Enum.Parse(statusType, HaveInBag, ignoreCase: false);
            object bundle = Enum.Parse(bundleType, DefaultBundle, ignoreCase: false);
            ConstructorInfo? constructor = requestType.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { songType, statusType, bundleType },
                modifiers: null);
            if (constructor == null)
            {
                detail = "exact cassette request constructor (song, status, bundle) unavailable";
                return false;
            }

            request = constructor.Invoke(new[] { song, status, bundle });
            detail = $"song='{song}' status='{status}' bundle='{bundle}'";
            return request != null;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().Message;
            return false;
        }
    }
}
