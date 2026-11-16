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

foreach (var cell in new[] { "", "-cell-tower-path-1" })
foreach (var lobby in new[] { "", "-lobby-doors-1" })
foreach (var bunker in new[] { "", "-bunker-1" })
{
    var marked = RepairVersion.Replace("-generation-foundation", cell + lobby + bunker + "-generation-foundation");
    Equal(true, RepairCompatibilityPolicy.IsCompatible(marked, true), "Plant Pipes in " + marked);
    Equal(true, RootsBucketRandomizationPolicy.IsCompatible(marked, true), "bucket in " + marked);
    Equal(false, RootsBucketRandomizationPolicy.IsCompatible(marked, false), "bucket disabled");
}
foreach (var invalid in new[] { "-unknown", "-bunker-1-lobby-doors-1", "-cell-tower-path-1-cell-tower-path-1" })
{
    var marked = RepairVersion.Replace("-generation-foundation", invalid + "-generation-foundation");
    Equal(false, RepairCompatibilityPolicy.IsCompatible(marked, true), "unexpected marker sequence");
    Equal(false, RootsBucketRandomizationPolicy.IsCompatible(marked, true), "unexpected bucket marker sequence");
}

Console.WriteLine("Repair compatibility policy tests passed.");
