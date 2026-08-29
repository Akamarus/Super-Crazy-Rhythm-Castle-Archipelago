using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

internal static class CassetteCatalogDiagnostic
{
    internal static void ScanLoadedCatalog()
    {
        string room = DeveloperHarness.CurrentRoomId;
        CassetteCatalogDiagnosticScope scope =
            CassetteCatalogDiagnosticPolicy.DecideScope(room);

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== CASSETTE CATALOG DIAGNOSTIC BEGIN ===== schema=3 " +
            $"room='{Clean(room)}' globalLevelScan={scope.ScanGlobalLevels} " +
            $"roomLocalNonLevelScan={scope.ScanRoomLocalNonLevelSources} " +
            $"readOnly=True mutationRequested=False.");

        try
        {
            var globalLevelSources = new List<CassetteCatalogDiagnosticSource>();
            var roomLocalNonLevelSources = new List<CassetteCatalogDiagnosticSource>();
            var levels = new HashSet<string>(StringComparer.Ordinal);
            var variants = new HashSet<string>(StringComparer.Ordinal);

            Type? providerType = FindGameType("LevelDataProvider");
            object[] providers = FindLoadedObjects(providerType).ToArray();
            MethodInfo? getAllLevels = providerType?.GetMethod(
                "GetAllLevelsData",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);

            for (int providerIndex = 0; providerIndex < providers.Length; providerIndex++)
            {
                object? rawLevels;
                try { rawLevels = getAllLevels?.Invoke(providers[providerIndex], null); }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] CASSETTE CATALOG PROVIDER unreadable index={providerIndex} " +
                        $"error='{Clean(ex.GetBaseException().Message)}'.");
                    continue;
                }

                int levelIndex = 0;
                foreach (object levelData in EnumerateNativeList(rawLevels))
                {
                    string level = ReadIdentifier(levelData, "LevelIdentifier");
                    if (string.IsNullOrWhiteSpace(level))
                        level = $"<provider-{providerIndex}-level-{levelIndex}>";
                    levels.Add(level);

                    object? rawVariants = ReflectionUtil.ReadMember(levelData, "Variants");
                    int variantIndex = 0;
                    foreach (object variantData in EnumerateNativeList(rawVariants))
                    {
                        string variant = ReadIdentifier(variantData, "VariantIdentifier");
                        if (string.IsNullOrWhiteSpace(variant))
                            variant = $"<variant-{variantIndex}>";
                        variants.Add($"{level}|{variant}");

                        object? rawSongs = ReflectionUtil.ReadMember(variantData, "SongCassettes");
                        int songIndex = 0;
                        foreach (object songValue in EnumerateNativeList(rawSongs))
                        {
                            string song = ReflectionUtil.ExtractIdentifier(songValue) ?? songValue.ToString() ?? string.Empty;
                            long? numericSong = TryReadInt64(songValue);
                            string nativeIdentity =
                                $"LevelDataProvider[{providerIndex}].GetAllLevelsData()[{levelIndex}]" +
                                $".Variants[{variantIndex}].SongCassettes[{songIndex}]=" +
                                $"{numericSong?.ToString() ?? "?"}:{song}";

                            globalLevelSources.Add(new CassetteCatalogDiagnosticSource(
                                "level", level, variant, song, nativeIdentity,
                                $"cassette:{song}"));
                            songIndex++;
                        }

                        variantIndex++;
                    }

                    levelIndex++;
                }
            }

            if (scope.ScanRoomLocalNonLevelSources)
            {
                foreach (object step in FindLoadedObjects(FindGameType("ObstainSongCassetteSequenceStep")))
                {
                    string song = ReadIdentifier(step, "songCassette");
                    string path = BuildPath((step as Component)?.transform);
                    object? rawExtraFlag = ReflectionUtil.ReadMember(step, "extraFlagToSet");
                    string extraFlag = ReflectionUtil.ExtractIdentifier(rawExtraFlag)
                                       ?? rawExtraFlag?.ToString()
                                       ?? "<unreadable>";
                    roomLocalNonLevelSources.Add(new CassetteCatalogDiagnosticSource(
                        "sequence-step", string.Empty, string.Empty, song,
                        $"ObstainSongCassetteSequenceStep@{path}|extraFlag={extraFlag}"));
                }

                foreach (object trigger in FindLoadedObjects(FindGameType("ObtainSongCassetteOnTrigger")))
                {
                    string song = ReadIdentifier(trigger, "song");
                    string path = BuildPath((trigger as Component)?.transform);
                    roomLocalNonLevelSources.Add(new CassetteCatalogDiagnosticSource(
                        "trigger", string.Empty, string.Empty, song,
                        $"ObtainSongCassetteOnTrigger@{path}"));
                }
            }

            CassetteCatalogDiagnosticCoverage coverage =
                CassetteCatalogDiagnosticPolicy.CreateCoverage(
                    globalLevelSources,
                    roomLocalNonLevelSources,
                    scope,
                    levels.Count,
                    variants.Count);

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG PROVIDERS count={providers.Length} " +
                $"levels={coverage.GlobalLevels.LevelCount} variants={coverage.GlobalLevels.VariantCount}.");

            foreach (CassetteCatalogDiagnosticSource source in coverage.GlobalLevels.Sources)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE CATALOG GLOBAL LEVEL SOURCE sourceType='{Clean(source.SourceType)}' " +
                    $"level='{Clean(source.Level)}' variant='{Clean(source.Variant)}' " +
                    $"song='{Clean(source.Song)}' logicalSource='{Clean(source.LogicalSource)}' " +
                    $"nativeIdentity='{Clean(source.NativeIdentity)}'.");
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG GLOBAL LEVEL SUMMARY sourceCount={coverage.GlobalLevels.Sources.Count} " +
                $"uniqueSongs={coverage.GlobalLevels.UniqueSongCount} " +
                $"expectedSongs={CassetteCatalogDiagnosticPolicy.ApprovedLevelEarnedNativeSongs.Count} " +
                $"missing='{Clean(string.Join("|", coverage.GlobalLevels.MissingSongs))}' " +
                $"unexpected='{Clean(string.Join("|", coverage.GlobalLevels.UnexpectedSongs))}' " +
                $"aliases='{Clean(string.Join("|", coverage.GlobalLevels.AliasSongs))}' " +
                $"ambiguous='{Clean(string.Join("|", coverage.GlobalLevels.AmbiguousSongs))}' " +
                $"complete={coverage.GlobalLevels.IsComplete} readOnly=True mutationRequested=False.");

            foreach (CassetteCatalogDiagnosticSource source in coverage.RoomLocalNonLevel.Sources)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE CATALOG ROOM LOCAL NONLEVEL SOURCE room='{Clean(room)}' " +
                    $"sourceType='{Clean(source.SourceType)}' song='{Clean(source.Song)}' " +
                    $"logicalSource='{Clean(source.LogicalSource)}' " +
                    $"nativeIdentity='{Clean(source.NativeIdentity)}'.");
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG ROOM LOCAL NONLEVEL SUMMARY room='{Clean(room)}' " +
                $"scanned={coverage.RoomLocalNonLevelScanned} " +
                $"sourceCount={coverage.RoomLocalNonLevel.Sources.Count} " +
                $"uniqueSongs={coverage.RoomLocalNonLevel.UniqueSongCount} " +
                $"expectedHub6Songs={CassetteCatalogDiagnosticPolicy.ApprovedHub6NonLevelNativeSongs.Count} " +
                $"missing='{Clean(string.Join("|", coverage.RoomLocalNonLevel.MissingSongs))}' " +
                $"unexpected='{Clean(string.Join("|", coverage.RoomLocalNonLevel.UnexpectedSongs))}' " +
                $"aliases='{Clean(string.Join("|", coverage.RoomLocalNonLevel.AliasSongs))}' " +
                $"ambiguous='{Clean(string.Join("|", coverage.RoomLocalNonLevel.AmbiguousSongs))}' " +
                $"complete={coverage.RoomLocalNonLevel.IsComplete} readOnly=True mutationRequested=False.");

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG OVERALL SUMMARY " +
                $"globalLevelComplete={coverage.GlobalLevels.IsComplete} " +
                $"roomLocalNonLevelScanned={coverage.RoomLocalNonLevelScanned} " +
                $"roomLocalNonLevelComplete={coverage.RoomLocalNonLevel.IsComplete} " +
                $"approvedPhysicalSources={CassetteCatalogDiagnosticPolicy.ApprovedNativeSongs.Count} " +
                $"allPhysicalSourcesProven={coverage.AllPhysicalSourcesProven} " +
                $"readOnly=True mutationRequested=False.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] CASSETTE CATALOG DIAGNOSTIC ERROR type='{ex.GetType().FullName}' " +
                $"message='{Clean(ex.GetBaseException().Message)}' readOnly=True.");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== CASSETTE CATALOG DIAGNOSTIC END ===== readOnly=True mutationRequested=False.");
    }

    private static IEnumerable<object> EnumerateNativeList(object? list)
    {
        if (list == null)
            yield break;

        int count = ReadCount(list);
        MethodInfo? getter = list.GetType().GetMethod(
            "get_Item",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        if (getter == null)
            yield break;

        for (int index = 0; index < count; index++)
        {
            object? value;
            try { value = getter.Invoke(list, new object[] { index }); }
            catch { continue; }
            if (value != null)
                yield return value;
        }
    }

    private static int ReadCount(object list)
    {
        object? count = ReflectionUtil.ReadMember(list, "Count");
        if (count is int value)
            return Math.Max(0, value);
        return int.TryParse(count?.ToString(), out int parsed)
            ? Math.Max(0, parsed)
            : 0;
    }

    private static string ReadIdentifier(object value, string memberName)
    {
        object? identifier = ReflectionUtil.ReadMember(value, memberName);
        return ReflectionUtil.ExtractIdentifier(identifier) ?? string.Empty;
    }

    private static long? TryReadInt64(object value)
    {
        try { return Convert.ToInt64(value); }
        catch { return null; }
    }

    private static string BuildPath(Transform? transform)
    {
        if (transform == null)
            return "<no-transform>";

        var names = new List<string>();
        for (Transform? current = transform; current != null && names.Count < 64; current = current.parent)
            names.Add(current.name ?? "<unnamed>");
        names.Reverse();
        return string.Join("/", names);
    }

    private static Type? FindGameType(string fullName)
    {
        try { return ReflectionUtil.GameAssembly?.GetType(fullName, throwOnError: false, ignoreCase: false); }
        catch { return null; }
    }

    private static IEnumerable<object> FindLoadedObjects(Type? type)
    {
        if (type == null)
            yield break;

        UnityEngine.Object[] loaded;
        try { loaded = Resources.FindObjectsOfTypeAll(Il2CppType.From(type)); }
        catch { yield break; }

        ConstructorInfo? pointerConstructor = type.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(IntPtr) },
            modifiers: null);

        foreach (UnityEngine.Object raw in loaded)
        {
            if (raw == null)
                continue;
            if (type.IsInstanceOfType(raw))
            {
                yield return raw;
                continue;
            }

            object? wrapped = null;
            try { wrapped = pointerConstructor?.Invoke(new object[] { raw.Pointer }); }
            catch { }
            if (wrapped != null)
                yield return wrapped;
        }
    }

    private static string Clean(string? value) =>
        (value ?? string.Empty)
        .Replace("\r", " ", StringComparison.Ordinal)
        .Replace("\n", " ", StringComparison.Ordinal)
        .Replace("'", "''", StringComparison.Ordinal);
}
