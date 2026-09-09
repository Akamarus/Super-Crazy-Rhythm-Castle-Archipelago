using RhythmCastleAP;

static void Equal<T>(T expected, T actual, string scenario)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{scenario}: expected {expected}, got {actual}");
}

static void True(bool actual, string scenario) => Equal(true, actual, scenario);

static void False(bool actual, string scenario) => Equal(false, actual, scenario);

static string FindRepositoryRoot()
{
    DirectoryInfo? directory = new(AppContext.BaseDirectory);
    while (directory != null)
    {
        if (File.Exists(Path.Combine(directory.FullName, "client", "Plugin.cs")))
            return directory.FullName;
        directory = directory.Parent;
    }

    throw new InvalidOperationException("repository root containing client/Plugin.cs was not found");
}

static string Slice(string source, string startMarker, string endMarker)
{
    int start = source.IndexOf(startMarker, StringComparison.Ordinal);
    if (start < 0)
        throw new InvalidOperationException($"production wiring marker missing: {startMarker}");
    int end = source.IndexOf(endMarker, start + startMarker.Length, StringComparison.Ordinal);
    if (end < 0)
        throw new InvalidOperationException($"production wiring end marker missing: {endMarker}");
    return source[start..end];
}

static (bool Intercepted, bool EffectiveOwned) ResolveEntrancePreview(
    bool enabled,
    bool compatible,
    string nativeCartridgeType,
    bool nativeOwned,
    params string[] apOwnedSongs)
{
    bool intercepted = GarageAvailabilityPolicy.TryResolveEntrancePreviewOwned(
        enabled,
        compatible,
        nativeCartridgeType,
        nativeOwned,
        apOwnedSongs,
        out var ownership);
    return (intercepted, intercepted ? ownership.EffectiveOwned : nativeOwned);
}

foreach (bool owns in new[] { false, true })
{
    Equal(GarageObjectDecision.Active,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.Initialization),
        "holder and play initialization always remain active");
    Equal(owns ? GarageObjectDecision.Active : GarageObjectDecision.Inactive,
        GarageAvailabilityPolicy.Decide(true, true, owns, GarageObjectRole.SongCartridge),
        "only matching song cartridge follows AP ownership");
}

foreach (GarageObjectRole role in Enum.GetValues<GarageObjectRole>())
{
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(false, true, false, role),
        "disabled AP preserves vanilla");
    Equal(GarageObjectDecision.Vanilla,
        GarageAvailabilityPolicy.Decide(true, false, false, role),
        "incompatible slot preserves vanilla");
}

var entrancePreviewCases = new[]
{
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, true),
        Scenario: "AP-owned Superstar changes native false to effective true"),
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, false),
        Scenario: "unowned Superstar remains false"),
    (Enabled: true, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: true,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, true),
        Scenario: "native-owned Superstar remains true"),
    (Enabled: true, Compatible: true, NativeType: "VAMPIRE_KILLER", NativeOwned: false,
        ApOwnedSongs: new[] { "Vampire Killer" }, Expected: (false, false),
        Scenario: "physical Vampire Killer remains fully native"),
    (Enabled: false, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, false),
        Scenario: "disabled routing preserves native false"),
    (Enabled: true, Compatible: false, NativeType: "SUPERSTAR", NativeOwned: false,
        ApOwnedSongs: new[] { "Superstar" }, Expected: (true, false),
        Scenario: "incompatible routing preserves native false"),
    (Enabled: false, Compatible: true, NativeType: "SUPERSTAR", NativeOwned: true,
        ApOwnedSongs: Array.Empty<string>(), Expected: (true, true),
        Scenario: "disabled routing preserves native true"),
};
foreach (var testCase in entrancePreviewCases)
{
    Equal(
        testCase.Expected,
        ResolveEntrancePreview(
            testCase.Enabled,
            testCase.Compatible,
            testCase.NativeType,
            testCase.NativeOwned,
            testCase.ApOwnedSongs),
        testCase.Scenario);
}

string[] exactEntrancePreviewParameterTypes =
{
    "eRoom27GameCartridgeType",
    "Boolean",
    "eCleanMedal",
    "eCleanMedal",
};
True(
    GarageAvailabilityPolicy.IsExactEntrancePreviewRefreshSignature(
        "LevelPreviewUIData",
        "RefreshDataForGarageCartridge",
        "Void",
        exactEntrancePreviewParameterTypes),
    "the production target policy accepts only the verified Garage entrance refresh signature");
False(
    GarageAvailabilityPolicy.IsExactEntrancePreviewRefreshSignature(
        "LevelPreviewUIData",
        "RefreshDataForGarageCartridge",
        "Void",
        new[] { "eRoom27GameCartridgeType", "Boolean", "eCleanMedal" }),
    "the production target policy rejects an incomplete native signature");
False(
    GarageAvailabilityPolicy.IsExactEntrancePreviewRefreshSignature(
        "GameProgressionEnquiries",
        "HasGarageCartridgeBeenCollected",
        "Boolean",
        new[] { "eRoom27GameCartridgeType" }),
    "the production target policy rejects the global collected enquiry");

string pluginSource = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "client", "Plugin.cs"));
True(
    pluginSource.Contains("patched += PatchGarageEntrancePreview();", StringComparison.Ordinal),
    "plugin load installs the bounded Garage entrance preview hook");
True(
    pluginSource.Contains(
        "GarageAvailabilityPolicy.IsExactEntrancePreviewRefreshSignature(",
        StringComparison.Ordinal),
    "production hook discovery uses the tested exact-signature policy");
True(
    pluginSource.Contains(
        "_harmony.Patch(matchingTargets[0], prefix: new HarmonyMethod(prefix));",
        StringComparison.Ordinal),
    "the exact Garage entrance target is patched only with the bounded prefix");
False(
    pluginSource.Contains("HasGarageCartridgeBeenCollected", StringComparison.Ordinal),
    "production never patches the global Garage collected enquiry");

string entrancePatchSource = Slice(
    pluginSource,
    "internal static class GarageEntrancePreviewPatches",
    "internal static class GarageCartridgeAccess");
string entranceAccessSource = Slice(
    pluginSource,
    "public static bool TryResolveEntrancePreviewOwned(",
    "public static bool ShouldSuppressVanillaSourceGrant(");
True(
    entrancePatchSource.Contains("__1 = ownership.EffectiveOwned;", StringComparison.Ordinal),
    "production prefix supplies the tested effective ownership as the second native argument");
True(
    entranceAccessSource.Contains(
        "GarageAvailabilityPolicy.TryResolveEntrancePreviewOwned(",
        StringComparison.Ordinal),
    "production ownership access delegates to the tested catalog-aware seam");
foreach (string forbiddenWriteApi in new[]
{
    "InsertionCoordinator",
    "NoteInserted",
    "TrySubmitProgressionFlag",
    "RecordGameProgressionInSaveDataRequest",
    "ObtainBagItemRequest",
    "_COLLECTED",
})
{
    False(
        entrancePatchSource.Contains(forbiddenWriteApi, StringComparison.Ordinal) ||
        entranceAccessSource.Contains(forbiddenWriteApi, StringComparison.Ordinal),
        $"Garage entrance preview path never invokes insertion/native write API {forbiddenWriteApi}");
}

Equal(true,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-vanilla-vampire-garage-0.21", "Vampire Killer"),
    "v0.21 enables the vanilla entrance cartridge");
Equal(true,
    GarageVanillaEntrancePolicy.IsEnabled(
        "area-routing-test-vanilla-vampire-garage-0.21-full-cassettes-0.22",
        "Vampire Killer"),
    "v0.22-compatible slot data retains the vanilla entrance cartridge");
Equal(false,
    GarageVanillaEntrancePolicy.IsEnabled("area-routing-test-difficulty-filtering-0.20", null),
    "v0.20 seeds preserve six-cartridge randomization");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Vampire Killer"),
    "v0.21 does not AP-randomize Vampire Killer");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldRandomizeSong(true, "Smooch"),
    "v0.21 still randomizes the other Garage cartridges");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Vampire Killer"),
    "v0.21 preserves the native Vampire Killer grant");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(true, "Smooch"),
    "v0.21 still suppresses randomized native grants");
Equal(true,
    GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(false, "Vampire Killer"),
    "v0.20 retains its old Vampire Killer source behavior");
Equal(false,
    GarageVanillaEntrancePolicy.ShouldWriteNativeSaveFlags(true),
    "v0.21 requires the physical vanilla pickup and never writes its save flags");

string[] expectedNativeMappings =
{
    "Bloody Tears|Bloody Tears Cartridge|LEVEL_27_CARTRIDGE_BLOODYTEARS_BAG_ITEM|BLOODY_TEARS|scrc:garage_inserted:v1:bloody_tears",
    "Gradius Remix|Gradius Remix Cartridge|LEVEL_27_CARTRIDGE_GRADIUS_BAG_ITEM|GRADIUS_REMIX|scrc:garage_inserted:v1:gradius_remix",
    "Smooch|Smooch Cartridge|LEVEL_27_CARTRIDGE_SMOOCH_BAG_ITEM|SMOOCH|scrc:garage_inserted:v1:smooch",
    "Superstar|Superstar Cartridge|LEVEL_27_CARTRIDGE_STAR_EATER_BAG_ITEM|SUPERSTAR|scrc:garage_inserted:v1:superstar",
    "Wag the Dog|Wag the Dog Cartridge|LEVEL_27_CARTRIDGE_SUPER_CRAZY_RHYTHM_CASTLE_BAG_ITEM|WAG_THE_DOG|scrc:garage_inserted:v1:wag_the_dog",
};
Equal(
    string.Join("\n", expectedNativeMappings),
    string.Join("\n", GarageCartridgeNativePolicy.RandomizedCartridges.Select(cartridge =>
        $"{cartridge.Song}|{cartridge.ItemName}|{cartridge.NativeBagFlag}|{cartridge.NativeCartridgeType}|{cartridge.ServerInsertionKey}")),
    "all five randomized cartridges use exact verified native mappings");
Equal(false,
    GarageCartridgeNativePolicy.RandomizedCartridges.Any(cartridge =>
        string.Equals(cartridge.Song, "Vampire Killer", StringComparison.Ordinal)),
    "physical Vampire Killer is excluded from native AP reconciliation");

Console.WriteLine("Game Garage availability policy tests passed.");
