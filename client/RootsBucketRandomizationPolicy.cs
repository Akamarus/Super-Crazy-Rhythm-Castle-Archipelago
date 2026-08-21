namespace RhythmCastleAP;

internal enum RootsBucketItemKind
{
    HipGlasses,
    ChickenBucket,
}

internal enum RootsBucketGrantDecision
{
    Ignore,
    Apply,
    AlreadyHeld,
    Consumed,
}

internal static class RootsBucketRandomizationPolicy
{
    internal const string CompatibleVersionPrefix =
        "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17";

    internal static bool IsCompatible(string? implementationVersion, bool featureFlag) =>
        featureFlag &&
        implementationVersion?.StartsWith(CompatibleVersionPrefix, StringComparison.Ordinal) == true;

    internal static bool ShouldSuppressVanillaGrant(
        bool synchronized,
        bool compatible,
        string? roomId,
        string flagName,
        bool value)
    {
        if (!synchronized || !compatible || !value)
            return false;

        return
            (string.Equals(roomId, "GameRoom_08", StringComparison.Ordinal) &&
             string.Equals(flagName, "HIP_GLASSES_BAG_ITEM", StringComparison.Ordinal)) ||
            (string.Equals(roomId, "GameRoom_Hub2", StringComparison.Ordinal) &&
             string.Equals(flagName, "CHICKEN_BUCKET_BAG_ITEM", StringComparison.Ordinal));
    }

    internal static RootsBucketGrantDecision DecideReceivedItemGrant(
        bool synchronized,
        bool compatible,
        int receivedCount,
        bool nativeHeld,
        bool consumed)
    {
        if (!synchronized || !compatible || receivedCount <= 0)
            return RootsBucketGrantDecision.Ignore;
        if (consumed)
            return RootsBucketGrantDecision.Consumed;
        if (nativeHeld)
            return RootsBucketGrantDecision.AlreadyHeld;
        return RootsBucketGrantDecision.Apply;
    }
}
