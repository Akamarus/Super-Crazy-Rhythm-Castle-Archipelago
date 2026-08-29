using System.Reflection;
using Il2CppInterop.Runtime;
using UnityEngine;

namespace RhythmCastleAP;

internal static class CassetteCatalogDiagnostic
{
    internal static void ScanLoadedCatalog()
    {
        if (!string.Equals(
                DeveloperHarness.CurrentRoomId,
                MusicLabDiscovery.Hub6RoomId,
                StringComparison.Ordinal))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG DIAGNOSTIC ignored outside Hub6; " +
                $"currentRoom='{Clean(DeveloperHarness.CurrentRoomId)}' readOnly=True mutationRequested=False.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== CASSETTE CATALOG DIAGNOSTIC BEGIN ===== schema=1 readOnly=True mutationRequested=False.");

        try
        {
            var sources = new List<CassetteCatalogDiagnosticSource>();
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

                            sources.Add(new CassetteCatalogDiagnosticSource(
                                "level", level, variant, song, nativeIdentity));
                            songIndex++;
                        }

                        variantIndex++;
                    }

                    levelIndex++;
                }
            }

            foreach (object step in FindLoadedObjects(FindGameType("ObstainSongCassetteSequenceStep")))
            {
                string song = ReadIdentifier(step, "songCassette");
                string path = BuildPath((step as Component)?.transform);
                object? rawExtraFlag = ReflectionUtil.ReadMember(step, "extraFlagToSet");
                string extraFlag = ReflectionUtil.ExtractIdentifier(rawExtraFlag)
                                   ?? rawExtraFlag?.ToString()
                                   ?? "<unreadable>";
                sources.Add(new CassetteCatalogDiagnosticSource(
                    "sequence-step", string.Empty, string.Empty, song,
                    $"ObstainSongCassetteSequenceStep@{path}|extraFlag={extraFlag}"));
            }

            foreach (object trigger in FindLoadedObjects(FindGameType("ObtainSongCassetteOnTrigger")))
            {
                string song = ReadIdentifier(trigger, "song");
                string path = BuildPath((trigger as Component)?.transform);
                sources.Add(new CassetteCatalogDiagnosticSource(
                    "trigger", string.Empty, string.Empty, song,
                    $"ObtainSongCassetteOnTrigger@{path}"));
            }

            CassetteCatalogDiagnosticSnapshot snapshot =
                CassetteCatalogDiagnosticPolicy.CreateSnapshot(
                    sources,
                    levels.Count,
                    variants.Count);

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG PROVIDERS count={providers.Length} " +
                $"levels={snapshot.LevelCount} variants={snapshot.VariantCount}.");

            foreach (CassetteCatalogDiagnosticSource source in snapshot.Sources)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE CATALOG SOURCE sourceType='{Clean(source.SourceType)}' " +
                    $"level='{Clean(source.Level)}' variant='{Clean(source.Variant)}' " +
                    $"song='{Clean(source.Song)}' nativeIdentity='{Clean(source.NativeIdentity)}'.");
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE CATALOG SUMMARY sourceCount={snapshot.Sources.Count} " +
                $"uniqueSongs={snapshot.UniqueSongCount} expectedSongs={CassetteCatalogDiagnosticPolicy.ApprovedNativeSongs.Count} " +
                $"missing='{Clean(string.Join("|", snapshot.MissingSongs))}' " +
                $"unexpected='{Clean(string.Join("|", snapshot.UnexpectedSongs))}' " +
                $"duplicate='{Clean(string.Join("|", snapshot.DuplicateSongs))}' " +
                $"complete={snapshot.IsComplete} readOnly=True mutationRequested=False.");
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
