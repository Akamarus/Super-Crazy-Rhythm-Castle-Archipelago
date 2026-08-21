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

var initialDelivery = RootsBucketRandomizationPolicy.Reconcile(
    true,
    true,
    new RootsBucketLifecycleState(1, 1, false, false, false, false));
Equal(RootsBucketGrantDecision.Apply, initialDelivery.HipGlasses, "initial Hip Glasses delivery");
Equal(RootsBucketGrantDecision.Apply, initialDelivery.ChickenBucket, "initial Chicken Bucket delivery");

var heldReplay = RootsBucketRandomizationPolicy.Reconcile(
    true,
    true,
    new RootsBucketLifecycleState(1, 1, true, false, true, false));
Equal(RootsBucketGrantDecision.AlreadyHeld, heldReplay.HipGlasses, "held Hip Glasses replay");
Equal(RootsBucketGrantDecision.AlreadyHeld, heldReplay.ChickenBucket, "held Chicken Bucket replay");

var consumedReplay = RootsBucketRandomizationPolicy.Reconcile(
    true,
    true,
    new RootsBucketLifecycleState(1, 1, true, true, true, true));
Equal(RootsBucketGrantDecision.Consumed, consumedReplay.HipGlasses, "consumed Hip Glasses replay");
Equal(RootsBucketGrantDecision.Consumed, consumedReplay.ChickenBucket, "consumed Chicken Bucket replay");

var failClosed = RootsBucketRandomizationPolicy.Reconcile(
    false,
    true,
    new RootsBucketLifecycleState(1, 1, false, false, false, false));
Equal(RootsBucketGrantDecision.Ignore, failClosed.HipGlasses, "pre-sync Hip Glasses reconciliation");
Equal(RootsBucketGrantDecision.Ignore, failClosed.ChickenBucket, "pre-sync Chicken Bucket reconciliation");

var incompatible = RootsBucketRandomizationPolicy.Reconcile(
    true,
    false,
    new RootsBucketLifecycleState(1, 1, false, false, false, false));
Equal(RootsBucketGrantDecision.Ignore, incompatible.HipGlasses, "incompatible Hip Glasses reconciliation");
Equal(RootsBucketGrantDecision.Ignore, incompatible.ChickenBucket, "incompatible Chicken Bucket reconciliation");

Console.WriteLine("Roots bucket policy tests passed.");
