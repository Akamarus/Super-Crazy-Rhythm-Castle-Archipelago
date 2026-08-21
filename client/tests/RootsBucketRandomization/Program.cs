using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

const string CompatibleVersion =
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17";

Equal(true, RootsBucketRandomizationPolicy.IsCompatible(CompatibleVersion, true), "compatible feature");
Equal(true, RootsBucketRandomizationPolicy.IsCompatible(CompatibleVersion + "-patch1", true), "compatible additive patch");
Equal(false, RootsBucketRandomizationPolicy.IsCompatible("area-routing-plant-pipes-0.15-generation-foundation-0.16", true), "old seed");
Equal(false, RootsBucketRandomizationPolicy.IsCompatible("wrong-prefix-0.17", true), "wrong prefix");
Equal(false, RootsBucketRandomizationPolicy.IsCompatible(null, true), "missing version");
Equal(false, RootsBucketRandomizationPolicy.IsCompatible(CompatibleVersion, false), "feature disabled");

Equal(true, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_08", "HIP_GLASSES_BAG_ITEM", true), "Level 4 glasses grant");
Equal(true, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_Hub2", "CHICKEN_BUCKET_BAG_ITEM", true), "Bucket Minion chicken grant");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(false, true, "GameRoom_08", "HIP_GLASSES_BAG_ITEM", true), "pre-sync fail closed");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, false, "GameRoom_08", "HIP_GLASSES_BAG_ITEM", true), "incompatible fail closed");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_080", "HIP_GLASSES_BAG_ITEM", true), "near-match room");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_08", "CHICKEN_BUCKET_BAG_ITEM", true), "wrong room and item pair");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_08", "HIP_GLASSES_BAG_ITEM", false), "item consumption");
Equal(false, RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(true, true, "GameRoom_08", "LEVEL_08_GLASSES_COLLECTED", true), "source marker");

Equal(RootsBucketGrantDecision.Ignore, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(true, true, 0, false, false), "not received");
Equal(RootsBucketGrantDecision.Apply, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(true, true, 1, false, false), "new delivery");
Equal(RootsBucketGrantDecision.AlreadyHeld, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(true, true, 1, true, false), "already held");
Equal(RootsBucketGrantDecision.Consumed, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(true, true, 1, true, true), "consumed wins over stale held state");
Equal(RootsBucketGrantDecision.Ignore, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(false, true, 1, false, false), "grant before sync");
Equal(RootsBucketGrantDecision.Ignore, RootsBucketRandomizationPolicy.DecideReceivedItemGrant(true, false, 1, false, false), "grant on incompatible seed");

Console.WriteLine("Roots bucket policy tests passed.");
