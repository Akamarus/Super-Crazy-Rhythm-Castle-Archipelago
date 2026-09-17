using RhythmCastleAP;

int assertions = 0;

void Equal<T>(T expected, T actual, string name)
{
    assertions++;
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new InvalidOperationException($"{name}: expected {expected}, got {actual}");
}

void Contains(string expected, string actual, string name)
{
    assertions++;
    if (!actual.Contains(expected, StringComparison.Ordinal))
        throw new InvalidOperationException($"{name}: missing '{expected}'");
}

string SourceRegion(string source, string start, string end)
{
    int startIndex = source.IndexOf(start, StringComparison.Ordinal);
    if (startIndex < 0)
        throw new InvalidOperationException($"Source start marker missing: {start}");

    int endIndex = source.IndexOf(end, startIndex + start.Length, StringComparison.Ordinal);
    if (endIndex < 0)
        throw new InvalidOperationException($"Source end marker missing: {end}");

    return source[startIndex..endIndex];
}

Equal(SpecialVariantKind.BeeNectarParty,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_06", "LevelVariant_BeeMode"),
    "Nectar Party exact identity");
Equal(SpecialVariantKind.BeeAct1BNectar,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_12", "LevelVariant_BeeMode"),
    "Act 1B Nectar exact identity");
Equal(SpecialVariantKind.DevilDemonicRoom,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_02", "LevelVariant_DevilMode"),
    "Demonic Room exact identity");
Equal(SpecialVariantKind.DevilDemonicTower,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_11", "LevelVariant_DevilMode"),
    "Demonic Tower exact identity");
Equal(SpecialVariantKind.DevilDemonicEscape,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_13", "LevelVariant_DevilMode"),
    "Demonic Escape exact identity");
Equal(SpecialVariantKind.DevilDemonicLockers,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_14", "LevelVariant_DevilMode"),
    "Demonic Lockers exact identity");
Equal(SpecialVariantKind.None,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_06", "LevelVariant_Default"),
    "default variant is not special");
Equal(SpecialVariantKind.None,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_11", "LevelVariant_BeeMode"),
    "known level with the wrong special variant is not classified");
Equal(SpecialVariantKind.None,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("Level_99", "LevelVariant_DevilMode"),
    "unknown level with a known special variant is not classified");
Equal(SpecialVariantKind.None,
    SpecialVariantDiagnosticPolicy.ClassifyVariant(null, "LevelVariant_BeeMode"),
    "missing level is not classified");
Equal(SpecialVariantKind.BeeNectarParty,
    SpecialVariantDiagnosticPolicy.ClassifyVariant("level_06", "levelvariant_beemode"),
    "exact identity comparison is case-insensitive");

foreach (string token in new[] { "BIZZLE", "CLIVE", "BEE", "NECTAR", "DEMON", "DEVIL", "SEAL", "BUNKER" })
{
    Equal(true,
        SpecialVariantDiagnosticPolicy.IsRelevantProgressionFlag($"story_{token.ToLowerInvariant()}_updated"),
        $"lower-case {token} progression flag is relevant");
    Equal(true,
        SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(
            $"Root/Objects/{token.ToLowerInvariant()}Candidate",
            Array.Empty<string>()),
        $"lower-case {token} hierarchy path is relevant");
    Equal(true,
        SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(
            "Root/Objects/Unrelated",
            new[] { $"Game.{token.ToLowerInvariant()}CandidateComponent" }),
        $"lower-case {token} component type is relevant");
}

foreach (string unrelated in new[] { "STAR", "SCORE", "CASSETTE", "KEY" })
{
    Equal(false,
        SpecialVariantDiagnosticPolicy.IsRelevantProgressionFlag($"PLAYER_{unrelated}_UPDATED"),
        $"generic {unrelated} progression flag is excluded");
    Equal(false,
        SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(
            $"Root/UI/{unrelated}",
            new[] { $"Game.{unrelated}Display" }),
        $"generic {unrelated} scene object is excluded");
}

Equal(true,
    SpecialVariantDiagnosticPolicy.IsRelevantProgressionFlag("SECRET_DEMON_KEY_ACQUIRED"),
    "Demon Key is relevant because it contains DEMON");
Equal(true,
    SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(
        "Root/SecretBunker/BunkerKeyInteraction",
        new[] { "Game.KeyInteraction" }),
    "Bunker Key is relevant because it contains BUNKER");
Equal(false, SpecialVariantDiagnosticPolicy.IsRelevantProgressionFlag(null),
    "null progression flag is excluded");
Equal(false, SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(null, null),
    "missing scene identity is excluded");

string pluginSource = File.ReadAllText(
    Path.Combine(Directory.GetCurrentDirectory(), "client", "Plugin.cs"));
string discoverySource = SourceRegion(
    pluginSource,
    "internal static class SpecialModeDiscovery",
    "internal static class DeveloperHarness");

foreach (string required in new[]
{
    "SPECIAL MODE DIAGNOSTIC",
    "FLAG UPDATED",
    "RESULT APPLIED",
    "RESULT PERSISTED",
    "readOnly=True",
    "emitted=",
    "truncated=",
})
{
    Contains(required, discoverySource, $"diagnostic source records {required}");
}

foreach (string forbidden in new[]
{
    "TrySubmitProgressionFlag",
    "QueueLocation",
    "SetActive(",
    "ReceivedItemDispatch",
    "AllowNative",
    "ProcessRequest",
    ".Invoke(",
    "ShouldSuppress",
    "187256",
})
{
    Equal(false, discoverySource.Contains(forbidden, StringComparison.Ordinal),
        $"diagnostic source excludes mutation/ID token {forbidden}");
}

string progressionWiring = SourceRegion(
    pluginSource,
    "public static void ProgressionFlagEventPostfix",
    "public static void BagItemRequestPostfix");
Contains("SpecialModeDiscovery.RecordProgressionFlagUpdated(evt, flag);", progressionWiring,
    "progression patch forwards the exact event and flag");

string resultWiring = SourceRegion(
    pluginSource,
    "internal static class GamePatches",
    "private sealed record PendingResult");
Contains("SpecialModeDiscovery.RecordResultApplied(request, level, variant, score, difficulty);", resultWiring,
    "result apply patch forwards the exact request and extracted identity");
Contains("SpecialModeDiscovery.RecordResultPersisted(evt, level, variant, result?.Score, result?.Difficulty ?? \"<unknown>\");", resultWiring,
    "result persisted patch forwards the exact event and retained result evidence");

string f5Wiring = SourceRegion(
    pluginSource,
    "if (Input.GetKeyDown(KeyCode.F5))",
    "internal static class IntroRoomToHubRedirectPatches");
Contains("SpecialModeDiscovery.ScanCurrentScene();", f5Wiring,
    "plain F5 forwards to the special-mode scene scan");

Console.WriteLine($"Special variant diagnostic policy tests passed. Assertions: {assertions}.");
