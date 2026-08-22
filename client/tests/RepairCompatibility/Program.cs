using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario) where T : notnull
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

const string RepairVersion =
    "area-routing-plant-pipes-0.15-generation-foundation-0.16-hip-glasses-chicken-bucket-0.17-next-release-repair-0.18";

Equal(true, RepairCompatibilityPolicy.IsCompatible(RepairVersion, true), "v0.18 contract");
Equal(true, RepairCompatibilityPolicy.IsCompatible(RepairVersion + "-patch1", true), "additive patch");
Equal(false, RepairCompatibilityPolicy.IsCompatible(RepairVersion, false), "feature flag off");
Equal(false, RepairCompatibilityPolicy.IsCompatible("next-release-repair-0.18", true), "missing preserved prefix");
Equal(false, RepairCompatibilityPolicy.IsCompatible(null, true), "missing version");

Console.WriteLine("Repair compatibility policy tests passed.");
