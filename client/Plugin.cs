using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using UnityEngine;

namespace RhythmCastleAP;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class Plugin : BasePlugin
{
    public const string PluginGuid = "jack.rhythmcastle.archipelago";
    public const string PluginName = "Super Crazy Rhythm Castle Archipelago";
    public const string PluginVersion = "0.75.4";
    public const string GameName = "Super Crazy Rhythm Castle";

    internal static ManualLogSource? LoggerInstance;
    internal static ArchipelagoClient? AP;

    private Harmony? _harmony;

    public override void Load()
    {
        LoggerInstance = Log;
        ClientPerformance.Configure(Config.Bind("Developer", "EnablePerformanceDiagnostics", false,
            "Aggregate main-thread component timing and frame intervals every 30 seconds; no per-frame logging.").Value,
            message => Log.LogInfo(message));

        var enabled = Config.Bind("Archipelago", "Enabled", false,
            "Enable the Archipelago network connection.");
        var server = Config.Bind("Archipelago", "Server", "localhost:38281",
            "Archipelago server hostname and port.");
        var slot = Config.Bind("Archipelago", "Slot", "Player1",
            "Archipelago slot/player name.");
        var password = Config.Bind("Archipelago", "Password", "",
            "Archipelago room password, if any.");
        var applyReceivedProgression = Config.Bind("Archipelago", "ApplyReceivedProgression", false,
            "EXPERIMENTAL: apply selected received AP items to native Rhythm Castle progression flags.");
        var directStartAtLevelOne = Config.Bind("QualityOfLife", "DirectStartAtLevelOne", false,
            "Legacy option: skip the fresh-save intro/tutorial and start in Roots outside Level 1.");
        var directStartAtPhoneHub = Config.Bind("QualityOfLife", "DirectStartAtPhoneHub", false,
            "Area-routing start: skip the fresh-save intro/tutorial and start in the Music Lab / six-phone hub (GameRoom_Hub6). If enabled, this takes precedence over DirectStartAtLevelOne.");
        var bunkerStarRequirement = Config.Bind("QualityOfLife", "BunkerStarRequirement", 50,
            "Star requirement for the final Secret Bunker Star Eater. Vanilla is 66. This changes only that Star Eater threshold and does not alter saved level star ratings.");
        var royalStarRequirement = Config.Bind("Developer", "RoyalStarRequirement", 40,
            "TEST ONLY: Royal Corridor Star Eater threshold. Vanilla is 40. Does not change earned stars or saved scores. Set back to 40 after testing.");
        var developerHarness = Config.Bind("Developer", "EnableTestHarness", true,
            "Enable maintainer-directed F4/F5 discovery and Area Access status/test hotkeys.");
        var cassettePointerBoundPersistenceAcceptance = Config.Bind(
            "Developer",
            "EnableCassettePointerBoundPersistenceAcceptance", false,
            "DIAGNOSTICS ONLY: observe uncorrelated native save-write completion hints for cassette persistence. Production durability is independent of this setting. Disabled by default; restart after enabling.");
        var areaAccessPrototype = Config.Bind("Developer", "EnableAreaAccessPrototype", false,
            "EXPERIMENTAL: gate the six Hub6 destination phones using randomizer Area Access permissions. With APWorld v0.13, use PrototypeStartingArea=AP so the seed's precollected Area Access item determines the starter. Music Lab / Hub6 and Game Garage remain available.");
        var prototypeStartingArea = Config.Bind("Developer", "PrototypeStartingArea", "AP",
            "Area Access start source. Use AP with APWorld v0.13 so slot data selects the starter. Developer fixed values: Roots, Lobby, Meat Dimension, Cell Tower, Tower of Fear, Royal Corridor.");
        var musicLab5ChestLocationName = Config.Bind("Archipelago", "MusicLab5PointChestLocationName",
            "Music Lab - 5 Point Chest",
            "Archipelago location name to complete when the native 5-point Music Lab reward chest has already been collected or is collected now.");
        var musicLab10ChestLocationName = Config.Bind("Archipelago", "MusicLab10PointChestLocationName",
            "Music Lab - 10 Point Chest",
            "Archipelago location name to complete when the native 10-point Music Lab reward chest has already been collected or is collected now.");
        var musicLab20ChestLocationName = Config.Bind("Archipelago", "MusicLab20PointChestLocationName",
            "Music Lab - 20 Point Chest",
            "Archipelago location name to complete when the native 20-point Music Lab reward chest has already been collected or is collected now.");
        var musicLab32ChestLocationName = Config.Bind("Archipelago", "MusicLab32PointChestLocationName",
            "Music Lab - 32 Point Chest",
            "Archipelago location name to complete when the native 32-point Music Lab reward chest has already been collected or is collected now.");
        var musicLab46ChestLocationName = Config.Bind("Archipelago", "MusicLab46PointChestLocationName",
            "Music Lab - 46 Point Chest",
            "Archipelago location name to complete when the native 46-point Music Lab reward chest has already been collected or is collected now.");
        var musicLab64ChestLocationName = Config.Bind("Archipelago", "MusicLab64PointChestLocationName",
            "Music Lab - 64 Point Chest",
            "Archipelago location name to complete when the native 64-point Flamenco reward chest collection flag is set.");
        var musicLab89ChestLocationName = Config.Bind("Archipelago", "MusicLab89PointChestLocationName",
            "Music Lab - 89 Point Chest",
            "Archipelago location name to complete when the native 89-point Ten-Four Good Buddy reward chest collection flag is set.");
        var musicLab111ChestLocationName = Config.Bind("Archipelago", "MusicLab111PointChestLocationName",
            "Music Lab - 111 Point Chest",
            "Archipelago location name to complete when the native 111-point Zen reward chest collection flag is set.");
        var musicLab140ChestLocationName = Config.Bind("Archipelago", "MusicLab140PointChestLocationName",
            "Music Lab - 140 Point Chest",
            "Archipelago location name to complete when the native 140-point Wiggle reward chest collection flag is set.");
        var garageMedalLocationFormat = Config.Bind("Archipelago", "GarageMedalLocationFormat",
            "Game Garage - {song} - {tier}",
            "Archipelago location-name format for Game Garage sticker checks. Supported placeholders: {song} and {tier}. Default tiers are Bronze, Silver, Gold, Platinum.");
        var cassetteMedalLocationFormat = Config.Bind("Archipelago", "CassetteMedalLocationFormat",
            "Music Lab Cassette - {song} - {tier}",
            "Archipelago location-name format for verified Music Lab cassette medal checks. Supported placeholders: {song} and {tier}.");

        LocationMap.MusicLab5PointChestLocationName = musicLab5ChestLocationName.Value;
        LocationMap.MusicLab10PointChestLocationName = musicLab10ChestLocationName.Value;
        LocationMap.MusicLab20PointChestLocationName = musicLab20ChestLocationName.Value;
        LocationMap.MusicLab32PointChestLocationName = musicLab32ChestLocationName.Value;
        LocationMap.MusicLab46PointChestLocationName = musicLab46ChestLocationName.Value;
        LocationMap.MusicLab64PointChestLocationName = musicLab64ChestLocationName.Value;
        LocationMap.MusicLab89PointChestLocationName = musicLab89ChestLocationName.Value;
        LocationMap.MusicLab111PointChestLocationName = musicLab111ChestLocationName.Value;
        LocationMap.MusicLab140PointChestLocationName = musicLab140ChestLocationName.Value;
        LocationMap.GarageMedalLocationFormat = garageMedalLocationFormat.Value;
        LocationMap.CassetteMedalLocationFormat = cassetteMedalLocationFormat.Value;

        Log.LogInfo($"[SCRC-AP] v{PluginVersion} loading.");

        _harmony = new Harmony(PluginGuid);
        int patched = 0;

        patched += PatchMethodsByParameterWithPrefixAndPostfix("ProcessRequest", "PersistLevelResultRequest",
            nameof(GamePatches.PersistResultPrefix), nameof(GamePatches.PersistResultPostfix));
        ApStars.Configure(identity => AP?.SendStarGoal(identity) == true);
        patched += ApStarNativeHooks.Install(_harmony, ApStars.ResolveTotal, ApStars.CanEnter,
            ApStars.ReportBlocked, ApStars.OnAdmittedStart, ApStars.OnPreviewObserved);
        patched += PatchMethodsByParameterWithPrefixAndPostfix(
            "ProcessRequest",
            "ApplyLevelResultToSaveDataRequest",
            nameof(GamePatches.ApplyResultPrefix),
            nameof(GamePatches.ApplyResultPostfix));
        patched += PatchCassetteEvaluation();
        patched += PatchCassetteStatusRequest();
        patched += PatchExactMethod("SaveDataRequestProcessor", "ChangeSelectedPlayerSaveSlot", "Int32", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix));
        patched += PatchExactMethod("SaveDataRequestProcessor", "CreateNewPlayerSaveFileInEmptySlot", "Int32", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix));
        patched += PatchExactMethod("SaveDataRequestProcessor", "ProcessRequest", "BuildPlayerSaveStateFromFileRequest", nameof(CassetteSaveTransactionPatches.BuiltPlayerSaveStatePostfix));
        patched += PatchExactMethodPrefix("SaveDataRequestProcessor", "ProcessRequest", "SelectMostRecentlyUsedRegularPlayerSaveSlotRequest", nameof(CassetteSaveTransactionPatches.MostRecentSelectionPrefix));
        if (cassettePointerBoundPersistenceAcceptance.Value)
            patched += PatchMethodsByParameter("HandleEvent", "PlayerSaveWriteCompletedEvent", nameof(CassetteSaveTransactionPatches.PlayerSaveWriteCompletedEventPostfix));
        patched += PatchMethodsByParameter("HandleEvent", "LevelResultWasPersistedEvent", nameof(GamePatches.ResultPersistedEventPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "SetScoredSongInCurrentLevelRequest", nameof(GamePatches.SetScoredSongRequestPostfix));
        patched += GarageNativeDoorPatches.Install();
        patched += GarageNativeHolderPatches.Install();
        patched += GarageRoomInitialization.Install(_harmony);
        patched += PatchMethodsByParameterWithPrefixAndPostfix(
            "ProcessRequest",
            "RecordGameProgressionInSaveDataRequest",
            nameof(ProgressionPatches.ProgressionRequestPrefix),
            nameof(ProgressionPatches.ProgressionRequestPostfix));
        patched += PatchMethodsByParameter("HandleEvent", "GameProgressionFlagUpdatedEvent", nameof(ProgressionPatches.ProgressionFlagEventPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "ObtainBagItemRequest", nameof(ProgressionPatches.BagItemRequestPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "EarnAbilityItemRequest", nameof(ProgressionPatches.AbilityItemRequestPostfix));
        patched += PatchGarageEntrancePreview();
        patched += PatchMethodsByParameter(
            "ProcessRequest",
            "CreateNewPlayerSaveFileInSlotRequest",
            nameof(IntroRoomToHubRedirectPatches.NewSaveCreatedPostfix));
        patched += PatchIntroRoomToHubRedirect();
        patched += PatchRootsPostLevelOnePresentation();
        int musicLabScorePatches = PatchMusicLabMedalScoreOverride();
        patched += musicLabScorePatches;
        MusicLabPointRandomization.ReportGetterAvailability(musicLabScorePatches > 0);
        patched += PatchHub6AreaPhoneProgressionConditions();
        patched += QuestCheckHooks.Install(_harmony);
        patched += PatchRootsOwnerDiagnostic();
        patched += PatchRootsComputerNormalization();

        Log.LogInfo($"[SCRC-AP] Game hooks installed: {patched}.");

        IntroHubSkip.Configure(directStartAtPhoneHub.Value, directStartAtLevelOne.Value);
        AP = new ArchipelagoClient(server.Value, slot.Value, password.Value, applyReceivedProgression.Value);

        GarageCartridgeAccess.Configure();
        WeedKillerRandomization.Configure();
        PlantPipesRandomization.Configure();
        MusicLabPointRandomization.Configure();
        CassetteReceiptRandomization.Configure(
            acceptanceDiagnosticsEnabled: cassettePointerBoundPersistenceAcceptance.Value);
        cassettePointerBoundPersistenceAcceptance.SettingChanged += (_, _) =>
            CassetteReceiptRandomization.SetPersistenceAcceptanceDiagnosticsEnabled(cassettePointerBoundPersistenceAcceptance.Value);
        CassetteSourceRandomization.Configure();
        PreviewAbilityRandomization.Configure();
        BottomHudDiagnostic.Configure();
        RootsBucketRandomization.Configure();
        RootsIntroCutsceneBypass.Configure();
        RootsStartupBootstrap.Configure();
        if (enabled.Value)
        {
            AddComponent<GarageCartridgeAccessKeeper>();
            AddComponent<MusicLabBarrierKeeper>();
            Log.LogWarning(
                "[SCRC-AP] RANDOMIZED GAME GARAGE CARTRIDGE ROUTING READY: waits for APWorld slot data. APWorld v0.13+ randomizes cartridge sources as AP checks: vanilla cartridge collected markers are retained, vanilla bag grants are suppressed outside Game Garage, and only AP-owned cartridges are released in GameRoom_27.");
        }

        Log.LogWarning(
            "[SCRC-AP] ROOTS WEED KILLER RANDOMIZATION READY: waits for APWorld v0.14+ slot data. Gecko's WEED_KILLER_BAG_ITEM grant is suppressed, ROOTS_HUB_WEED_KILLER_COLLECTED sends the AP source check, and receiving Weed Killer writes the real native bag-item progression flag so vanilla can consume it for Level 3 access.");
        Log.LogWarning(
            "[SCRC-AP] ROOTS PLANT PIPES RANDOMIZATION READY: waits for APWorld v0.15+ slot data. In Level 3, Frog/Hippo's WEED_KILLER_ABILITY grant is suppressed while LEVEL_07_WK_ABILITY_EARNED remains vanilla and sends the AP source check; receiving Plant Pipes grants the real native ability.");
        Log.LogWarning(
            "[SCRC-AP] LEVEL 2 MONEY CASSETTE RANDOMIZATION READY: waits for slot-data opt-in. A successful default Level_06 run with an unowned I_GOT_MONEY cassette suppresses the native evaluator and sends 'Level 2 - Money Cassette'; AP receipt reconciliation grants HAVE_IN_BAG through the native cassette save path.");
        if (enabled.Value)
        {
            ItemNotificationOverlay.PopupsEnabled = Config.Bind("Notifications", "ShowItemPopups", true,
                "Show six-second item sent/received popups. F6 opens the latest 100 notifications.").Value;
            AddComponent<ItemNotificationOverlay>();
            AddComponent<PlantPipesReconciliationKeeper>();
        }
        if (enabled.Value)
            AddComponent<CassetteReceiptReconciliationKeeper>();
        if (enabled.Value)
            AddComponent<PreviewAbilityReconciliationKeeper>();
        if (enabled.Value)
            AddComponent<BottomHudDiagnosticKeeper>();

        AreaAccessPrototype.Configure(areaAccessPrototype.Value, prototypeStartingArea.Value);
        if (areaAccessPrototype.Value)
        {
            AddComponent<AreaPhoneAccessKeeper>();
            AddComponent<RootsAreaBaselineKeeper>();
            AddComponent<MeatAreaBaselineKeeper>();
            AddComponent<MeatMouseEscortRecoveryKeeper>();
            Log.LogWarning(
                $"[SCRC-AP] AREA ACCESS ROUTING ENABLED: startingSource='{AreaAccessPrototype.StartingArea}'. Hub6/Music Lab + Game Garage remain available. With PrototypeStartingArea=AP, all six major-area phones begin locked until the AP server supplies the seed starter. Locked PhoneBox interactions are disabled; known Hub6 cloth covers follow Area Access; unlocked late-area phones bypass only their local vanilla visited/open condition without changing its save flag; Hub6->area TransitionToGameRoomRequest calls remain safety-gated. PAGE UP remains a developer grant; PAGE DOWN is disabled in AP-driven mode; END prints status.");
            Log.LogWarning(
                "[SCRC-AP] AREA ARRIVAL PRESENTATION READY: AP-phone entry to Roots or Lobby temporarily satisfies only the exact first-arrival/HUD conditions during destination-scene initialization; native save flags and vanilla story-route arrivals remain unchanged. Roots Access also suppresses FirstAreaGate and the dedicated StarEaterBlockade/Blockade collider. Meat Dimension Access suppresses only the exact Hub4 FirstAreaGate root. Interrupted Act 4 mouse escorts may reset and trigger only Hub4's exact native SpawnMouseLeader component when its native flags and Character state prove recovery is needed. HOME in Hub2 prints compact status only.");
        }

        Log.LogWarning(
            $"[SCRC-AP] MUSIC LAB REWARD CHEST AP CHECKS READY: thresholds=5/10/20/32/46/64/89/111/140. v0.67.60 reads each live Hub6 chest's native unlock requirement + chestUnlockedProgressionFlag only after Hub6 has loaded, then reconciles already-collected rewards against the save. No startup-time chest component is created; vanilla rewards are unchanged.");
        Log.LogWarning(
            $"[SCRC-AP] GAME GARAGE AP CHECKS READY: songs=6 cumulativeStickerTiers=Bronze/Silver/Gold/Platinum locationFormat='{LocationMap.GarageMedalLocationFormat}'. Cartridge insertion supplies song identity; SongStickerEvaluation supplies the earned sticker.");
        Log.LogWarning(
            $"[SCRC-AP] MUSIC LAB CASSETTE AP CHECKS READY: verifiedVariants=30 cumulativeMedalTiers=Bronze/Silver/Gold/Platinum locationFormat='{LocationMap.CassetteMedalLocationFormat}'. Verified mappings include 30 currently discovered cassette variants; result-time clean medal evaluation supplies Bronze/Silver/Gold/Platinum.");

        BunkerStarRequirementKeeper.RequiredStars =
            Math.Clamp(bunkerStarRequirement.Value, 0, 66);

        BunkerStarRequirementKeeper.RoyalRequiredStars = Math.Clamp(royalStarRequirement.Value, 0, 40);
        if (BunkerStarRequirementKeeper.RequiredStars != 66 || BunkerStarRequirementKeeper.RoyalRequiredStars != 40)
        {
            AddComponent<BunkerStarRequirementKeeper>();
            Log.LogWarning(
                $"[SCRC-AP] STAR EATER TEST OVERRIDES: Royal={BunkerStarRequirementKeeper.RoyalRequiredStars} (vanilla40), Bunker={BunkerStarRequirementKeeper.RequiredStars} (vanilla66). Exact scene-root and proximity guards apply; earned stars and saved scores are unchanged.");
        }

        DeveloperHarness.Enabled = developerHarness.Value;
        if (developerHarness.Value)
        {
            AddComponent<DeveloperHotkeys>();
            AddComponent<MusicLabDiagnosticKeeper>();
            Log.LogWarning(
                "[SCRC-AP] MUSIC LAB TRACKING ENABLED: Garage cartridge-state identity, cumulative Garage sticker AP checks on real results, and all nine Music Lab reward chests (5/10/20/32/46/64/89/111/140) from native Hub6 chest metadata. Hub6 F5 forces reconciliation and prints mapping/status.");
            Log.LogWarning(
                "[SCRC-AP] DEVELOPER TEST HARNESS ENABLED. Release builds do not bind the cassette-catalog INSERT diagnostic; retained gameplay diagnostics are documented for maintainer-directed testing only.");
        }

        if (IntroHubSkip.Enabled)
        {
            Log.LogInfo($"[SCRC-AP] Intro-to-hub skip enabled: GameRoom_04A -> {IntroHubSkip.TargetRoomId}.");
        }

        if (enabled.Value)
        {
            Log.LogInfo($"[SCRC-AP] Archipelago enabled; connecting to {server.Value} as '{slot.Value}'.");
            Log.LogInfo("[SCRC-AP] NET stage=before-background-task");

            _ = Task.Run(() =>
            {
                try
                {
                    LoggerInstance?.LogInfo("[SCRC-AP] NET stage=background-task-started");
                    AP.Connect();
                    LoggerInstance?.LogInfo("[SCRC-AP] NET stage=background-task-returned");
                }
                catch (Exception ex)
                {
                    LoggerInstance?.LogError(
                        $"[SCRC-AP] NET outer exception type={ex.GetType().FullName}: {ex}");
                }
            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    LoggerInstance?.LogError(
                        $"[SCRC-AP] NET background task faulted: {t.Exception}");
                }
                else if (t.IsCanceled)
                {
                    LoggerInstance?.LogWarning("[SCRC-AP] NET background task canceled.");
                }
            });
        }
        else
        {
            Log.LogInfo("[SCRC-AP] Archipelago networking is disabled in config. Game check detection remains active.");
        }
    }


    private int PatchMethodsByParameterWithPrefixAndPostfix(
        string methodName, string parameterTypeName, string prefixName, string postfixName)
    {
        int count = 0;
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        if (gameAssembly == null)
        {
            Log.LogError("[SCRC-AP] Assembly-CSharp is not loaded; cannot install game hooks.");
            return 0;
        }

        foreach (Type type in ReflectionUtil.SafeGetTypes(gameAssembly))
        {
            MethodInfo[] methods;
            try { methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); }
            catch { continue; }

            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName) continue;

                ParameterInfo[] parameters;
                try { parameters = method.GetParameters(); }
                catch { continue; }

                if (!parameters.Any(p => p.ParameterType.Name == parameterTypeName)) continue;

                try
                {
                    MethodInfo? prefix =
                        FindPatchMethod(typeof(GamePatches), prefixName)
                        ?? FindPatchMethod(typeof(ProgressionPatches), prefixName)
                        ?? FindPatchMethod(typeof(CassetteSaveTransactionPatches), prefixName);

                    MethodInfo? postfix =
                        FindPatchMethod(typeof(GamePatches), postfixName)
                        ?? FindPatchMethod(typeof(ProgressionPatches), postfixName)
                        ?? FindPatchMethod(typeof(IntroRoomToHubRedirectPatches), postfixName)
                        ?? FindPatchMethod(typeof(CassetteSaveTransactionPatches), postfixName);

                    if (prefix == null || postfix == null)
                    {
                        Log.LogError(
                            $"[SCRC-AP] Could not find prefix/postfix '{prefixName}'/'{postfixName}'.");
                        continue;
                    }

                    _harmony!.Patch(
                        method,
                        prefix: new HarmonyMethod(prefix),
                        postfix: new HarmonyMethod(postfix));

                    Log.LogInfo(
                        $"[SCRC-AP] Hooked {type.FullName}.{method.Name}({parameterTypeName}) with progression prefix/postfix");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogError($"[SCRC-AP] Failed to hook {type.FullName}.{method.Name}: {ex}");
                }
            }
        }

        return count;
    }

    private int PatchCassetteEvaluation()
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? type = gameAssembly == null
            ? null
            : ReflectionUtil.SafeGetTypes(gameAssembly).FirstOrDefault(t =>
                string.Equals(t.Name, "LevelLogic", StringComparison.Ordinal));
        MethodInfo? target = type?.GetMethod(
            "EvaluatePlayerLevelSongCassettes",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
        MethodInfo? prefix = FindPatchMethod(
            typeof(GamePatches),
            nameof(GamePatches.LevelCassetteEvaluationPrefix));
        if (target == null || prefix == null || _harmony == null)
        {
            Log.LogWarning(
                "[SCRC-AP] CASSETTE SOURCE hook unavailable.");
            return 0;
        }

        try
        {
            _harmony.Patch(
                target,
                prefix: new HarmonyMethod(prefix));
            Log.LogInfo(
                "[SCRC-AP] CASSETTE SOURCE HOOKED LevelLogic.EvaluatePlayerLevelSongCassettes(bool).");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogWarning(
                $"[SCRC-AP] CASSETTE SOURCE hook failed: {ex.GetBaseException().Message}");
            return 0;
        }
    }

    private int PatchCassetteStatusRequest()
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? processor = gameAssembly == null ? null : ReflectionUtil.SafeGetTypes(gameAssembly)
            .FirstOrDefault(t => string.Equals(t.Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal));
        Type? requestType = gameAssembly?.GetType(
            "RecordSongCassetteStatusInSaveDataRequest", throwOnError: false, ignoreCase: false);
        MethodInfo? target = processor?.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "ProcessRequest" &&
                m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == requestType);
        MethodInfo? prefix = FindPatchMethod(
            typeof(GamePatches), nameof(GamePatches.CassetteStatusRequestPrefix));
        if (target == null || prefix == null || _harmony == null)
        {
            Log.LogWarning("[SCRC-AP] CASSETTE STATUS SOURCE hook unavailable.");
            return 0;
        }

        try
        {
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            Log.LogInfo("[SCRC-AP] CASSETTE STATUS SOURCE HOOKED PlayerSaveRequestProcessor.ProcessRequest(RecordSongCassetteStatusInSaveDataRequest).");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogWarning($"[SCRC-AP] CASSETTE STATUS SOURCE hook failed: {ex.GetBaseException().Message}");
            return 0;
        }
    }

    private int PatchMusicLabMedalScoreOverride()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        Type? type = ReflectionUtil.SafeGetTypes(asm)
            .FirstOrDefault(t => t.Name == "CurrentPlayerSaveEnquiries");

        if (type == null)
        {
            Log.LogWarning("[SCRC-AP] MUSIC LAB POINT OVERRIDE target type not found: CurrentPlayerSaveEnquiries");
            return 0;
        }

        MethodInfo? method = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
            {
                if (m.Name != "GetMedalScore" || m.ReturnType != typeof(int))
                    return false;
                try { return m.GetParameters().Length == 0; }
                catch { return false; }
            });

        if (method == null)
        {
            Log.LogWarning("[SCRC-AP] MUSIC LAB POINT OVERRIDE target method not found: CurrentPlayerSaveEnquiries.GetMedalScore()");
            return 0;
        }

        MethodInfo? postfix = FindPatchMethod(
            typeof(MusicLabPointOverridePatches),
            nameof(MusicLabPointOverridePatches.GetMedalScorePostfix));

        if (postfix == null)
        {
            Log.LogError("[SCRC-AP] MUSIC LAB POINT OVERRIDE postfix lookup failed.");
            return 0;
        }

        try
        {
            _harmony!.Patch(method, postfix: new HarmonyMethod(postfix));
            MusicLabPointOverride.BindNativeGetter(method);
            Log.LogWarning(
                "[SCRC-AP] MUSIC LAB POINT OVERRIDE READY: patched CurrentPlayerSaveEnquiries.GetMedalScore(). Shift+F4 cycles the temporary effective total through native-next-threshold -> 111 -> 140 -> OFF without changing saved medals.");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogError(
                $"[SCRC-AP] Failed to hook Music Lab medal-score getter: {ex.GetBaseException().Message}");
            return 0;
        }
    }

    private int PatchRootsPostLevelOnePresentation()
    {
        Assembly? assembly = ReflectionUtil.GameAssembly;
        if (assembly == null)
            return 0;
        Type? type = ReflectionUtil.SafeGetTypes(assembly)
            .FirstOrDefault(candidate => candidate.Name == RootsPresentationPolicy.DifficultySequenceType);
        MethodInfo? prefix = FindPatchMethod(typeof(GamePatches), nameof(GamePatches.RootsPostLevelOneSequencePrefix));
        if (type == null || prefix == null)
        {
            Log.LogWarning("[SCRC-AP] Roots post-Level-1 presentation hook unavailable.");
            return 0;
        }
        int count = 0;
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(candidate => candidate.Name == "Begin"))
        {
            if (method.IsAbstract || method.ContainsGenericParameters)
                continue;
            try
            {
                _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                count++;
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[SCRC-AP] Could not hook Roots post-Level-1 presentation: {ex.GetBaseException().Message}");
            }
        }
        return count;
    }

    private int PatchIntroRoomToHubRedirect()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        MethodInfo? prefix = AccessTools.Method(
            typeof(IntroRoomToHubRedirectPatches),
            nameof(IntroRoomToHubRedirectPatches.Prefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] Intro-room redirect prefix lookup failed.");
            return 0;
        }

        int count = 0;

        foreach (Type type in ReflectionUtil.SafeGetTypes(asm))
        {
            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.Static))
            {
                if (method.Name != "ProcessRequest")
                    continue;

                ParameterInfo[] ps;
                try { ps = method.GetParameters(); }
                catch { continue; }

                if (!ps.Any(p => p.ParameterType.Name == "TransitionToGameRoomRequest"))
                    continue;

                try
                {
                    _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                    Log.LogInfo(
                        $"[SCRC-AP] INTRO->HUB REDIRECT HOOKED {type.FullName}.ProcessRequest(TransitionToGameRoomRequest)");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogError(
                        $"[SCRC-AP] Failed to hook intro->hub redirect on {type.FullName}: {ex.GetBaseException().Message}");
                }
            }
        }

        return count;
    }

    private int PatchRootsOwnerDiagnostic()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        Type? type = asm == null
            ? null
            : ReflectionUtil.SafeGetTypes(asm).FirstOrDefault(t =>
                string.Equals(t.Name, "OneTimeGameProgressionLogic", StringComparison.Ordinal));
        MethodInfo? target = type?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => string.Equals(m.Name, "HandleEvent", StringComparison.Ordinal) &&
                                 m.GetParameters().Length == 1 &&
                                 string.Equals(m.GetParameters()[0].ParameterType.Name, "GameRoomReadyToRunEvent", StringComparison.Ordinal));
        MethodInfo? postfix = AccessTools.Method(typeof(RootsOwnerDiagnostic), nameof(RootsOwnerDiagnostic.Postfix));
        if (target == null || postfix == null || _harmony == null)
        {
            Log.LogWarning("[SCRC-AP] ROOTS OWNER READ hook unavailable.");
            return 0;
        }
        _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
        Log.LogInfo("[SCRC-AP] ROOTS OWNER READ hook installed (read-only).");
        return 1;
    }

    private int PatchRootsComputerNormalization()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        MethodInfo? postfixFactory = AccessTools.Method(
            typeof(RootsComputerNormalization),
            nameof(RootsComputerNormalization.CurrentPhasePostfixFactory));
        if (asm == null || postfixFactory == null || _harmony == null)
        {
            Log.LogWarning($"[SCRC-AP] ROOTS COMPUTER STATE normalization hook unavailable: assembly={asm != null} factory={postfixFactory != null} harmony={_harmony != null}.");
            return 0;
        }

        int count = 0;
        foreach (Type type in ReflectionUtil.SafeGetTypes(asm).Where(t =>
                     string.Equals(t.Name, "DifficultyTogglerState", StringComparison.Ordinal)))
        {
            foreach (MethodInfo target in type.GetMethods(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(m => string.Equals(m.Name, "get_CurrentPhase", StringComparison.Ordinal) &&
                                 m.GetParameters().Length == 0))
            {
                try
                {
                    Type phaseType = target.ReturnType;
                    if (!phaseType.IsEnum || Enum.GetUnderlyingType(phaseType) != typeof(int))
                    {
                        Log.LogWarning($"[SCRC-AP] ROOTS COMPUTER STATE normalization skipped unexpected return type {phaseType.FullName}.");
                        continue;
                    }

                    _harmony.Patch(target, postfix: new HarmonyMethod(postfixFactory));
                    Log.LogInfo($"[SCRC-AP] ROOTS COMPUTER STATE normalization hooked {type.FullName}.{target.Name} return={target.ReturnType.FullName}.");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogError($"[SCRC-AP] ROOTS COMPUTER STATE normalization hook failed safely for {type.FullName}.{target.Name}: {ex.GetBaseException().Message}");
                }
            }
        }
        if (count == 0)
            Log.LogWarning("[SCRC-AP] ROOTS COMPUTER STATE normalization found no declared DifficultyTogglerState.get_CurrentPhase candidate.");
        return count;
    }

    private int PatchHub6AreaPhoneProgressionConditions()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null || _harmony == null)
            return 0;

        Type? conditionType = ReflectionUtil.SafeGetTypes(asm)
            .FirstOrDefault(t => string.Equals(
                t.Name, "IsGameProgressionFlagTrueCondition", StringComparison.Ordinal));
        MethodInfo? prefix = AccessTools.Method(
            typeof(AreaPhoneConditionPatches),
            nameof(AreaPhoneConditionPatches.BoolConditionPrefix));

        if (conditionType == null || prefix == null)
        {
            Log.LogWarning(
                "[SCRC-AP] AREA PHONE CONDITION BYPASS unavailable: IsGameProgressionFlagTrueCondition or prefix was not found.");
            return 0;
        }

        var methods = new HashSet<MethodInfo>();
        try
        {
            for (Type? current = conditionType;
                 current != null && current.Assembly == asm;
                 current = current.BaseType)
            {
                foreach (MethodInfo method in current.GetMethods(
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.DeclaredOnly))
                {
                    if (method.IsAbstract || method.ContainsGenericParameters || method.ReturnType != typeof(bool))
                        continue;
                    methods.Add(method);
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"[SCRC-AP] AREA PHONE CONDITION BYPASS method discovery failed: {ex.GetBaseException().Message}");
            return 0;
        }

        QuestSharedConditionHook.VerifyAlias();
        int count = 0;
        foreach (MethodInfo method in methods)
        {
            try
            {
                _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
                QuestSharedConditionHook.RecordInstalled(method);
                Log.LogInfo(
                    $"[SCRC-AP] AREA PHONE CONDITION HOOKED {method.DeclaringType?.Name}.{method.Name} -> bool.");
                count++;
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    $"[SCRC-AP] AREA PHONE CONDITION hook skipped {method.DeclaringType?.Name}.{method.Name}: {ex.GetBaseException().Message}");
            }
        }

        if (count == 0)
        {
            Log.LogWarning(
                "[SCRC-AP] AREA PHONE CONDITION BYPASS found no patchable bool-return condition methods; late-area phone conditions may remain vanilla-gated.");
        }
        else
        {
            Log.LogWarning(
                $"[SCRC-AP] AREA PHONE VANILLA CONDITION BYPASS READY: boolMethods={count}. Only IsGameProgressionFlagTrueCondition objects under an AP-unlocked Hub6 phone are forced true; save flags are not changed.");
        }

        return count;
    }

    private static MethodInfo? FindPatchMethod(Type type, string name)
    {
        return type.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
    }

    private int PatchGarageEntrancePreview()
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? owner = gameAssembly == null ? null : ReflectionUtil.SafeGetTypes(gameAssembly)
            .FirstOrDefault(type => string.Equals(type.Name, "LevelPreviewUIView", StringComparison.Ordinal));
        var matchingTargets = new List<MethodInfo>();
        if (owner != null)
        {
            MethodInfo[] methods;
            try
            {
                methods = owner.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                    BindingFlags.Static | BindingFlags.DeclaredOnly);
            }
            catch
            {
                methods = Array.Empty<MethodInfo>();
            }

            foreach (MethodInfo method in methods)
            {
                string[] parameterTypeNames;
                try
                {
                    parameterTypeNames = method.GetParameters()
                        .Select(parameter => parameter.ParameterType.Name)
                        .ToArray();
                }
                catch
                {
                    continue;
                }

                if (GarageAvailabilityPolicy.IsExactEntrancePreviewViewSignature(
                        owner.Name,
                        method.Name,
                        method.ReturnType.Name,
                        parameterTypeNames))
                {
                    matchingTargets.Add(method);
                }
            }
        }

        MethodInfo? prefix = FindPatchMethod(
            typeof(GarageEntrancePreviewPatches),
            nameof(GarageEntrancePreviewPatches.Prefix));
        if (matchingTargets.Count != 1 || prefix == null || _harmony == null)
        {
            Log.LogWarning(
                $"[SCRC-AP] GAME GARAGE ENTRANCE PREVIEW hook unavailable: expected exact " +
                "LevelPreviewUIView.ReflectGarageVisuals(LevelPreviewUIData) signature " +
                $"(matches={matchingTargets.Count}). Native preview ownership remains unchanged.");
            return 0;
        }

        try
        {
            _harmony.Patch(matchingTargets[0], prefix: new HarmonyMethod(prefix));
            Log.LogInfo(
                "[SCRC-AP] Hooked exact Game Garage entrance preview ownership boundary.");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogWarning(
                $"[SCRC-AP] GAME GARAGE ENTRANCE PREVIEW hook skipped: {ex.GetBaseException().Message}. " +
                "Native preview ownership remains unchanged.");
            return 0;
        }
    }

    private int PatchMethodsByParameter(string methodName, string parameterTypeName, string postfixName)
    {
        int count = 0;
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        if (gameAssembly == null)
        {
            Log.LogError("[SCRC-AP] Assembly-CSharp is not loaded; cannot install game hooks.");
            return 0;
        }

        foreach (Type type in ReflectionUtil.SafeGetTypes(gameAssembly))
        {
            MethodInfo[] methods;
            try { methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); }
            catch { continue; }

            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName) continue;

                ParameterInfo[] parameters;
                try { parameters = method.GetParameters(); }
                catch { continue; }

                if (!parameters.Any(p => p.ParameterType.Name == parameterTypeName)) continue;

                try
                {
                    MethodInfo? postfix =
                        FindPatchMethod(typeof(GamePatches), postfixName)
                        ?? FindPatchMethod(typeof(ProgressionPatches), postfixName)
                        ?? FindPatchMethod(typeof(IntroRoomToHubRedirectPatches), postfixName)
                        ?? FindPatchMethod(typeof(CassetteSaveTransactionPatches), postfixName);

                    if (postfix == null)
                    {
                        Log.LogError($"[SCRC-AP] Could not find postfix method '{postfixName}'.");
                        continue;
                    }

                    _harmony!.Patch(method, postfix: new HarmonyMethod(postfix));
                    Log.LogInfo($"[SCRC-AP] Hooked {type.FullName}.{method.Name}({parameterTypeName})");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogError($"[SCRC-AP] Failed to hook {type.FullName}.{method.Name}: {ex}");
                }
            }
        }

        return count;
    }

    private int PatchExactMethod(string ownerTypeName, string methodName, string parameterTypeName, string postfixName)
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? owner = gameAssembly == null ? null : ReflectionUtil.SafeGetTypes(gameAssembly)
            .FirstOrDefault(type => string.Equals(type.Name, ownerTypeName, StringComparison.Ordinal));
        MethodInfo? target = owner?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(method => method.Name == methodName && method.GetParameters() is ParameterInfo[] parameters &&
                parameters.Length == 1 && parameters[0].ParameterType.Name == parameterTypeName);
        MethodInfo? postfix = FindPatchMethod(typeof(CassetteSaveTransactionPatches), postfixName);
        if (target == null || postfix == null || _harmony == null)
        {
            Log.LogWarning($"[SCRC-AP] Exact cassette save-boundary hook unavailable: {ownerTypeName}.{methodName}({parameterTypeName}).");
            return 0;
        }
        try
        {
            _harmony.Patch(target, postfix: new HarmonyMethod(postfix));
            Log.LogInfo($"[SCRC-AP] Hooked exact {ownerTypeName}.{methodName}({parameterTypeName})");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogError($"[SCRC-AP] Failed exact cassette save-boundary hook {ownerTypeName}.{methodName}: {ex}");
            return 0;
        }
    }

    private int PatchExactMethodPrefix(string ownerTypeName, string methodName, string parameterTypeName, string prefixName)
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? owner = gameAssembly == null ? null : ReflectionUtil.SafeGetTypes(gameAssembly)
            .FirstOrDefault(type => string.Equals(type.Name, ownerTypeName, StringComparison.Ordinal));
        MethodInfo? target = owner?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(method => method.Name == methodName && method.GetParameters() is ParameterInfo[] parameters &&
                parameters.Length == 1 && parameters[0].ParameterType.Name == parameterTypeName);
        MethodInfo? prefix = FindPatchMethod(typeof(CassetteSaveTransactionPatches), prefixName);
        if (target == null || prefix == null || _harmony == null)
        {
            Log.LogWarning($"[SCRC-AP] Exact cassette save-boundary prefix unavailable: {ownerTypeName}.{methodName}({parameterTypeName}).");
            return 0;
        }
        try
        {
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix));
            Log.LogInfo($"[SCRC-AP] Hooked exact prefix {ownerTypeName}.{methodName}({parameterTypeName})");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogError($"[SCRC-AP] Failed exact cassette save-boundary prefix {ownerTypeName}.{methodName}: {ex}");
            return 0;
        }
    }

    private int PatchExactMethodWithPrefixAndPostfix(
        string ownerTypeName,
        string methodName,
        string parameterTypeName,
        string prefixName,
        string postfixName)
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? owner = gameAssembly == null ? null : ReflectionUtil.SafeGetTypes(gameAssembly)
            .FirstOrDefault(type => string.Equals(type.Name, ownerTypeName, StringComparison.Ordinal));
        MethodInfo? target = owner?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            .SingleOrDefault(method => method.Name == methodName && method.GetParameters() is ParameterInfo[] parameters &&
                parameters.Length == 1 && parameters[0].ParameterType.Name == parameterTypeName);
        MethodInfo? prefix = FindPatchMethod(typeof(CassetteSaveTransactionPatches), prefixName);
        MethodInfo? postfix = FindPatchMethod(typeof(CassetteSaveTransactionPatches), postfixName);
        if (target == null || prefix == null || postfix == null || _harmony == null)
        {
            Log.LogWarning($"[SCRC-AP] Exact cassette save-boundary prefix/postfix unavailable: {ownerTypeName}.{methodName}({parameterTypeName}).");
            return 0;
        }
        try
        {
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
            Log.LogInfo($"[SCRC-AP] Hooked exact prefix/postfix {ownerTypeName}.{methodName}({parameterTypeName})");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogError($"[SCRC-AP] Failed exact cassette save-boundary prefix/postfix {ownerTypeName}.{methodName}: {ex}");
            return 0;
        }
    }

    public override bool Unload()
    {
        AP?.Shutdown();
        ApStarEaterThresholds.Restore();
        ApStars.OnNativeBoundary();
        _harmony?.UnpatchSelf();
        return true;
    }
}

internal static class LocationMap
{
    // Confirmed from in-game testing:
    // user-facing Level 1 -> internal Level_05
    // user-facing Level 2 -> internal Level_06
    // user-facing Level 3 -> internal Level_07
    //
    // Only verified levels are network-enabled for now.
    public static string MusicLab5PointChestLocationName { get; set; } =
        "Music Lab - 5 Point Chest";

    public static string MusicLab10PointChestLocationName { get; set; } =
        "Music Lab - 10 Point Chest";

    public static string MusicLab20PointChestLocationName { get; set; } =
        "Music Lab - 20 Point Chest";

    public static string MusicLab32PointChestLocationName { get; set; } =
        "Music Lab - 32 Point Chest";

    public static string MusicLab46PointChestLocationName { get; set; } =
        "Music Lab - 46 Point Chest";

    public static string MusicLab64PointChestLocationName { get; set; } =
        "Music Lab - 64 Point Chest";

    public static string MusicLab89PointChestLocationName { get; set; } =
        "Music Lab - 89 Point Chest";

    public static string MusicLab111PointChestLocationName { get; set; } =
        "Music Lab - 111 Point Chest";

    public static string MusicLab140PointChestLocationName { get; set; } =
        "Music Lab - 140 Point Chest";

    public static string GarageMedalLocationFormat { get; set; } =
        "Game Garage - {song} - {tier}";

    public static string CassetteMedalLocationFormat { get; set; } =
        "Music Lab Cassette - {song} - {tier}";

    public static string GetGarageMedalLocationName(string song, string tier)
    {
        string format = string.IsNullOrWhiteSpace(GarageMedalLocationFormat)
            ? "Game Garage - {song} - {tier}"
            : GarageMedalLocationFormat;

        return format
            .Replace("{song}", song, StringComparison.OrdinalIgnoreCase)
            .Replace("{tier}", tier, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetCassetteMedalLocationName(string song, string tier)
    {
        string format = string.IsNullOrWhiteSpace(CassetteMedalLocationFormat)
            ? "Music Lab Cassette - {song} - {tier}"
            : CassetteMedalLocationFormat;

        return format
            .Replace("{song}", song, StringComparison.OrdinalIgnoreCase)
            .Replace("{tier}", tier, StringComparison.OrdinalIgnoreCase);
    }

    public static readonly IReadOnlyDictionary<string, string> InternalToLocationName =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Level_05"] = "Level 1 - Completion",
            ["Level_06"] = "Level 2 - Completion",
            ["Level_07"] = "Level 3 - Completion",
            // Confirmed user-facing Level 4 boss -> internal Level_08.
            // Kept out of the current dev APWorld, so networking code will warn if the
            // connected datapackage does not yet contain this location.
            ["Level_08"] = "Level 4 - Completion",
            // Confirmed lift quest / lobby unlock level.
            ["Level_09"] = "Level 5 - Completion",
            ["Level_28"] = "Level 22 - Completion",
        };
}

internal sealed class ArchipelagoClient
{
    private readonly string _server;
    private readonly string _slot;
    private readonly string _password;
    private readonly bool _applyReceivedProgression;
    private readonly ConcurrentQueue<string> _pendingChecks = new();
    private readonly HashSet<string> _queuedOrSent = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    private readonly object _connectLock = new();
    private readonly ReconnectPolicy _reconnectPolicy = new();
    private readonly CancellationTokenSource _shutdownToken = new();

    private readonly SessionGenerationLeaseGate<ArchipelagoSession> ConnectionLifecycle = new();
    private long _connectionGeneration;
    private volatile bool _connected;
    private int _reconnectWorkerActive;
    private readonly HashSet<int> _processedReceivedItemIndexes = new();
    private (string Game, string Seed, int Team, int Slot)? _authenticatedIdentity;

    public bool Connected => _connected && MusicLabPointRandomization.Snapshot.Mode != MusicLabPointRuntimeMode.Incompatible;

    public ArchipelagoClient(string server, string slot, string password, bool applyReceivedProgression)
    {
        _server = server;
        _slot = slot;
        _password = password;
        _applyReceivedProgression = applyReceivedProgression;
    }

    public void Connect()
    {
        if (!TryConnectOnce())
            RequestReconnect("initial connection failed");
    }

    private bool TryConnectOnce()
    {
        lock (_connectLock)
        {
        Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET stage=connect-entered");
        long generation = Interlocked.Increment(ref _connectionGeneration);
        if (!ConnectionLifecycle.TryAdmit(generation))
            return false;
        ArchipelagoSession? session = null;

        try
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] NET assemblies Archipelago.MultiClient.Net loaded=" +
                $"{AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Archipelago.MultiClient.Net")}");

            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET stage=create-session");
            session = ArchipelagoSessionFactory.CreateSession(ConnectionEndpointPolicy.Normalize(_server));
            var pointHistory = new MusicLabPointSessionHistory(generation);
            var starHistory = new ApStarSessionHistory(generation, ApStars.State);
            var questHistory = new CharacterQuestItemSessionHistory(generation, CharacterQuestItems.State);
            var questChecks = new QuestChecksSession(generation);
            var notificationSession = ItemNotifications.CreateSession(session, generation, action =>
            {
                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? notificationLease)) return;
                using (notificationLease) action();
            });
            int loginEstablished = 0;
            var attemptDiagnostics = new ConnectionAttemptDiagnostics();
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET stage=session-created");

            session.Socket.SocketOpened += () =>
            {
                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? callbackLease))
                    return;
                using (callbackLease)
                    Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET socket-opened");
            };

            session.Socket.SocketClosed += reason =>
            {
                ClearCurrentSession(session, generation, () =>
                {
                    Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] NET socket-closed reason='{reason}'");
                    RequestReconnect($"socket closed: {reason}");
                });
            };

            session.Socket.ErrorReceived += (exception, message) =>
            {
                ConnectionErrorDisposition disposition = ConnectionErrorDispositionPolicy.Decide(
                    Volatile.Read(ref loginEstablished) != 0,
                    () => session.Socket.Connected);
                if (disposition == ConnectionErrorDisposition.IgnoreUntilLoginReturns)
                {
                    attemptDiagnostics.ReportFirstError(exception, message, detail =>
                        Plugin.LoggerInstance?.LogWarning(
                            $"[SCRC-AP] NET prelogin-error generation={generation} detail='{detail}'"));
                    return;
                }
                if (disposition == ConnectionErrorDisposition.EndSessionAndReconnect)
                {
                    ClearCurrentSession(session, generation, () =>
                    {
                        Plugin.LoggerInstance?.LogError(
                            $"[SCRC-AP] NET socket-error message='{message}' exception={exception}");
                        RequestReconnect($"terminal socket error: {message}");
                    });
                    return;
                }

                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? callbackLease))
                    return;
                using (callbackLease)
                    Plugin.LoggerInstance?.LogError(
                        $"[SCRC-AP] NET socket-error message='{message}' exception={exception}");
            };

            session.Items.ItemReceived += helper =>
            {
                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? callbackLease))
                    return;
                using (callbackLease)
                {
                    try
                    {
                        while (helper.Any())
                        {
                            int itemIndex = helper.Index;
                            var item = helper.DequeueItem();
                            bool firstProcessing;
                            lock (_lock)
                                firstProcessing = _processedReceivedItemIndexes.Add(itemIndex);
                            if (!firstProcessing)
                            {
                                Plugin.LoggerInstance?.LogInfo(
                                    $"[SCRC-AP] ITEM HISTORY REPLAY SKIPPED index={itemIndex} name='{item.ItemName}'.");
                                continue;
                            }
                            Plugin.LoggerInstance?.LogInfo(
                                $"[SCRC-AP] ITEM RECEIVED id={item.ItemId} name='{item.ItemName}' from='{item.Player?.Name ?? "unknown"}'");

                            try
                            {
                                // Area Access is core routing state in APWorld v0.13+, not an
                                // experimental native-save mutation. Always consume these six
                                // item names when Area Access routing is enabled.
                                if (item.ItemId == ApStarContract.StarId) continue;
                                if (CharacterQuestItems.TryHandleItemName(item.ItemName) || QuestChecks.Handles(item.ItemName)) continue;
                                ReceivedItemDispatch.TryApply(
                                    item.ItemName,
                                    _applyReceivedProgression,
                                    AreaAccessPrototype.TryApplyItem,
                                    GarageCartridgeAccess.TryApplyItem,
                                    WeedKillerRandomization.TryApplyItem,
                                    PlantPipesRandomization.TryApplyItem,
                                    PreviewAbilityRandomization.TryApplyItem,
                                    RootsBucketRandomization.TryApplyItem,
                                    CassetteReceiptRandomization.TryApplyItem,
                                    MusicLabPointRandomization.TryHandleItemName,
                                    NativeProgression.ApplyArchipelagoItem);
                            }
                            catch (Exception ex)
                            {
                                lock (_lock)
                                    _processedReceivedItemIndexes.Remove(itemIndex);
                                Plugin.LoggerInstance?.LogError(
                                    $"[SCRC-AP] Failed to apply received item '{item.ItemName}': {ex}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plugin.LoggerInstance?.LogError($"[SCRC-AP] Error while reading received item: {ex}");
                    }
                }
            };

            session.Socket.PacketReceived += packet =>
            {
                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? packetLease))
                    return;
                using (packetLease)
                {
                    pointHistory.HandlePacket(packet);
                    starHistory.HandlePacket(packet);
                    questHistory.HandlePacket(packet);
                    questChecks.HandlePacket(packet);
                    notificationSession.HandlePacket(packet);
                }
            };

            if (!ConnectionLifecycle.TryPublish(session, generation, previousSession =>
                {
                    CharacterQuestItems.State.Suspend(generation);
                    QuestChecks.State.Suspend(generation);
                    _connected = false;
                    AreaAccessPrototype.EndAuthenticatedSession();
                    CampaignLevelRandomization.OnDisconnected();
                    if (previousSession.Session != null &&
                        !ReferenceEquals(previousSession.Session, session))
                    {
                        GarageCartridgeAccess.EndServerSync(previousSession.Generation);
                        MusicLabPointRandomization.OnDisconnected(previousSession.Generation);
                    }
                }, out GenerationSession<ArchipelagoSession> replaced))
            {
                GarageCartridgeAccess.EndServerSync(generation);
                ObserveDisconnect(session);
                return false;
            }
            ArchipelagoSession? previous = replaced.Session;
            if (previous != null && !ReferenceEquals(previous, session))
                ObserveDisconnect(previous);

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] NET stage=login-begin server='{_server}' slot='{_slot}' game='{Plugin.GameName}'");

            LoginResult result = session.TryConnectAndLogin(
                Plugin.GameName,
                _slot,
                ItemsHandlingFlags.AllItems,
                new Version(0, 6, 0),
                password: string.IsNullOrWhiteSpace(_password) ? null : _password,
                requestSlotData: true);

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] NET stage=login-returned successful={result.Successful} resultType={result.GetType().FullName}");

            if (result.Successful)
                Interlocked.Exchange(ref loginEstablished, 1);

            if (!result.Successful)
            {
                ClearCurrentSession(session, generation);
                ObserveDisconnect(session);
                if (result is LoginFailure failure)
                    Plugin.LoggerInstance?.LogError(
                        $"[SCRC-AP] Archipelago login failed: {string.Join("; ", failure.Errors)}");
                else
                    Plugin.LoggerInstance?.LogError("[SCRC-AP] Archipelago login failed.");
                return false;
            }

            if (result is LoginSuccessful loginSuccess)
            {
                if (!ConnectionLifecycle.TryAcquire(session, generation, out LifecycleLease? loginLease))
                    return false;
                try
                {
                    using (loginLease)
                    {
                        ExpandedChecksMode expandedMode = ExpandedChecksPolicy.Validate(loginSuccess.SlotData);
                        if (expandedMode == ExpandedChecksMode.Invalid)
                            throw new InvalidOperationException("Expanded check contract is incompatible.");
                        QuestChecksMode checksMode = QuestChecksPolicy.Validate(loginSuccess.SlotData);
                        if (checksMode == QuestChecksMode.Invalid || (checksMode == QuestChecksMode.Enabled && !QuestCheckHooks.Ready))
                            throw new InvalidOperationException("Quest checks contract or native hooks are incompatible.");
                        CharacterQuestItemMode questMode = CharacterQuestItemPolicy.Validate(loginSuccess.SlotData);
                        if (questMode == CharacterQuestItemMode.Invalid)
                            throw new InvalidOperationException("Character quest item contract is incompatible.");
                        AreaAccessPrototype.ApplySlotDataStarter(loginSuccess.SlotData);
                        IntroHubSkip.ApplySlotData(loginSuccess.SlotData);
                        RootsStartupBootstrap.ApplySlotData(loginSuccess.SlotData);
                        GarageCartridgeAccess.ApplySlotData(loginSuccess.SlotData);
                        WeedKillerRandomization.ApplySlotData(loginSuccess.SlotData);
                        PlantPipesRandomization.ApplySlotData(loginSuccess.SlotData);
                        CassetteReceiptRandomization.ApplySlotData(loginSuccess.SlotData);
                        CassetteSourceRandomization.ApplySlotData(loginSuccess.SlotData);
                        PreviewAbilityRandomization.ApplySlotData(loginSuccess.SlotData);
                        BottomHudDiagnostic.ApplySlotData(loginSuccess.SlotData);
                        RootsBucketRandomization.ApplySlotData(loginSuccess.SlotData);
                        lock (_lock)
                        {
                            var identity = (Plugin.GameName, session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot);
                            if (_authenticatedIdentity != identity)
                            {
                                _pendingChecks.Clear();
                                _queuedOrSent.Clear();
                                _processedReceivedItemIndexes.Clear();
                                CampaignLevelRandomization.OnIdentityReplaced();
                            }
                            _authenticatedIdentity = identity;
                            CampaignLevelRandomization.ApplySlotData(loginSuccess.SlotData);
                            CampaignLevelRandomizationSnapshot campaignState = CampaignLevelRandomization.Snapshot;
                            if (campaignState.Mode == CampaignLocationCompatibilityMode.IncompatibleClaim)
                                Plugin.LoggerInstance?.LogError(
                                    $"[SCRC-AP] CAMPAIGN COMPATIBILITY ERROR: {campaignState.Detail}; campaign checks disabled.");
                        }
                        ApStarSettings starSettings = starHistory.ApplySlotData(loginSuccess.SlotData,
                            string.Join("|", Plugin.GameName, session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot));
                        if (starSettings.Mode == ApStarMode.Awaiting && !ApStarNativeHooks.Ready)
                        {
                            starSettings = new(ApStarMode.Incompatible, Detail: "Required AP Star native hooks unavailable");
                            ApStars.State.Configure(generation,
                                string.Join("|", Plugin.GameName, session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot), starSettings);
                        }
                        if (starSettings.Mode == ApStarMode.Incompatible)
                            throw new InvalidOperationException($"AP Stars incompatible: {starSettings.Detail}");
                        MusicLabPointSnapshot pointState = pointHistory.ApplySlotData(
                            loginSuccess.SlotData,
                            Plugin.GameName,
                            session.RoomState.Seed,
                            _slot);
                        if (pointState.Mode == MusicLabPointRuntimeMode.Incompatible)
                            throw new InvalidOperationException($"Music Lab Points incompatible: {pointState.Detail}");
                        questHistory.Configure(questMode == CharacterQuestItemMode.Enabled);
                        questChecks.Configure(string.Join("|", Plugin.GameName, session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot),
                            checksMode == QuestChecksMode.Enabled, session.Locations.AllLocationsChecked);
                        ExpandedChecks.Configure(generation,
                            string.Join("|", Plugin.GameName, session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot),
                            expandedMode == ExpandedChecksMode.Enabled, session.Locations.AllLocationsChecked,
                            includeStarChecks: starSettings.Mode == ApStarMode.Awaiting);
                        if (GarageCartridgeAccess.Enabled &&
                            !GarageCartridgeAccess.BeginServerSync(session, generation))
                            throw new InvalidOperationException(
                                $"Garage inserted-state synchronization generation {generation} was already ended.");
                        notificationSession.Configure(session.RoomState.Seed, loginSuccess.Team, loginSuccess.Slot);
                        _connected = true;
                        _reconnectPolicy.OnConnected();
                        Plugin.LoggerInstance?.LogInfo(
                            $"[SCRC-AP] CONNECTED server={_server} slot='{_slot}' game='{Plugin.GameName}'.");
                    }
                }
                catch (Exception ex)
                {
                    ClearCurrentSession(session, generation);
                    ObserveDisconnect(session);
                    Plugin.LoggerInstance?.LogError(
                        $"[SCRC-AP] Failed to configure AP session from slot data: {ex}");
                    return false;
                }
            }
            else
            {
                throw new InvalidOperationException(
                    $"Successful login returned unexpected result type '{result.GetType().FullName}'.");
            }

            FlushPendingChecks();
            GarageCartridgeAccess.RequestUnityReconciliation("Archipelago connected");
            PreviewAbilityRandomization.RequestUnityReconciliation("Archipelago connected");
            return true;
        }
        catch (Exception ex)
        {
            if (session != null)
            {
                if (!ClearCurrentSession(session, generation))
                {
                    ConnectionLifecycle.Abandon(generation);
                    GarageCartridgeAccess.EndServerSync(generation);
                }
                ObserveDisconnect(session);
            }
            else
            {
                ConnectionLifecycle.Abandon(generation);
                GarageCartridgeAccess.EndServerSync(generation);
            }
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] NET inner exception type={ex.GetType().FullName}: {ex}");
            return false;
        }
        }
    }

    private static void ObserveDisconnect(ArchipelagoSession session)
    {
        _ = ConnectionAttemptDiagnostics.ObserveDisconnectAsync(
            session.Socket.DisconnectAsync,
            detail => Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] NET disconnect-cleanup detail='{detail}'"));
    }

    public void Shutdown()
    {
        GenerationSession<ArchipelagoSession> current = ConnectionLifecycle.Shutdown(current =>
        {
            _connected = false;
            AreaAccessPrototype.EndAuthenticatedSession();
            _reconnectPolicy.OnDeliberateShutdown();
            MusicLabPointRandomization.Reset();
            ApStars.State.Reset();
            CharacterQuestItems.State.Reset();
            QuestChecks.State.Reset();
            ExpandedChecks.Reset();
            ItemNotifications.Feed.Reset();
            lock (_lock)
            {
                _authenticatedIdentity = null;
                _pendingChecks.Clear();
                _queuedOrSent.Clear();
                CampaignLevelRandomization.Shutdown();
            }
            _shutdownToken.Cancel();
            if (current.Session != null)
                GarageCartridgeAccess.EndServerSync(current.Generation);
        });
        ArchipelagoSession? session = current.Session;
        if (session != null)
            ObserveDisconnect(session);
    }

    private bool ClearCurrentSession(
        ArchipelagoSession session,
        long generation,
        Action? terminal = null)
    {
        return ConnectionLifecycle.TryEnd(session, generation, () =>
        {
            _connected = false;
            AreaAccessPrototype.EndAuthenticatedSession();
            GarageCartridgeAccess.EndServerSync(generation);
            MusicLabPointRandomization.OnDisconnected(generation);
            ApStars.State.Disconnect(generation);
            CampaignLevelRandomization.OnDisconnected();
            terminal?.Invoke();
        });
    }

    private void RequestReconnect(string reason)
    {
        _reconnectPolicy.OnUnexpectedDisconnect();
        if (Interlocked.CompareExchange(ref _reconnectWorkerActive, 1, 0) != 0)
            return;
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] NET reconnect requested reason='{reason}'.");
        _ = Task.Run(ReconnectLoopAsync);
    }

    private async Task ReconnectLoopAsync()
    {
        try
        {
            while (true)
            {
                ReconnectAttempt? attempt = _reconnectPolicy.NextAttempt();
                if (attempt == null)
                    return;
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] NET reconnect waiting seconds={attempt.Value.Delay.TotalSeconds:0}.");
                await Task.Delay(attempt.Value.Delay, _shutdownToken.Token).ConfigureAwait(false);
                if (!ReconnectAttemptAdmission.TryExecute(
                        _connectLock,
                        _reconnectPolicy,
                        attempt.Value,
                        TryConnectOnce,
                        out bool connected))
                {
                    Plugin.LoggerInstance?.LogInfo(
                        "[SCRC-AP] NET stale reconnect attempt canceled before session creation.");
                    return;
                }
                if (connected)
                {
                    Plugin.LoggerInstance?.LogWarning("[SCRC-AP] NET reconnect succeeded.");
                    return;
                }
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] NET reconnect attempt failed; retry remains scheduled.");
            }
        }
        catch (OperationCanceledException) when (_shutdownToken.IsCancellationRequested)
        {
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET reconnect canceled by deliberate shutdown.");
        }
        finally
        {
            Interlocked.Exchange(ref _reconnectWorkerActive, 0);
            if (_reconnectPolicy.ReconnectRequested)
                RequestReconnect("disconnect raced with reconnect worker completion");
        }
    }

    public void QueueCampaignResult(string? level, string? variant, int? starsEarned)
    {
        // Evaluation and queue admission share the login identity boundary.
        // A result cannot be evaluated for one seed and queued for its replacement.
        lock (_lock)
        {
            if (!_authenticatedIdentity.HasValue)
                return;
            IReadOnlyList<string> locations = CampaignLevelRandomization.EvaluatePersistedResult(level, variant, starsEarned);
            if (locations.Count == 0)
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] CAMPAIGN RESULT produced no active locations internal={level} variant={variant} stars={starsEarned?.ToString() ?? "<missing>"} mode={CampaignLevelRandomization.Snapshot.Mode}.");
            foreach (string location in locations)
                QueueLocation(location);
        }
    }

    internal bool SendStarGoal(string expectedIdentity)
    {
        if (!Connected || !ConnectionLifecycle.TryAcquireCurrent(out ArchipelagoSession? session,
                out long generation, out LifecycleLease? lease) || session == null) return false;
        using (lease)
        lock (_lock)
        {
            if (!Connected || !_authenticatedIdentity.HasValue) return false;
            var identity = _authenticatedIdentity.Value;
            if (string.Join("|", identity.Game, identity.Seed, identity.Team, identity.Slot) != expectedIdentity ||
                ApStars.State.Mode != ApStarMode.Ready || !ApStars.State.GoalPending) return false;
            session.Socket.SendPacket(new Archipelago.MultiClient.Net.Packets.StatusUpdatePacket {
                Status = Archipelago.MultiClient.Net.Enums.ArchipelagoClientState.ClientGoal });
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] AP STAR VICTORY sent for a qualifying persisted final-boss clear.");
            return true;
        }
    }

    internal bool QueueExpandedLocation(string locationName, string expectedIdentity)
    {
        lock (_lock)
        {
            if (!_authenticatedIdentity.HasValue) return false;
            var identity = _authenticatedIdentity.Value;
            if (string.Join("|", identity.Game, identity.Seed, identity.Team, identity.Slot) != expectedIdentity)
                return false;
            QueueLocation(locationName);
            return true;
        }
    }

    public void QueueLocation(string locationName)
    {
        lock (_lock)
        {
            if (!_queuedOrSent.Add(locationName))
            {
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Duplicate local check ignored: {locationName}");
                return;
            }
            _pendingChecks.Enqueue(locationName);
        }
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEUED CHECK '{locationName}'.");

        if (Connected)
            Task.Run(FlushPendingChecks);
    }

    private void FlushPendingChecks()
    {
        if (!Connected ||
            !ConnectionLifecycle.TryAcquireCurrent(
                out ArchipelagoSession? session,
                out long generation,
                out LifecycleLease? sessionLease) ||
            session == null)
            return;

        bool sendFailed = false;
        using (sessionLease)
        {
            // The initial Connected read can precede a transport replacement.
            // Recheck under its lease before delivering identity-bound results.
            if (!Connected)
                return;
            while (_pendingChecks.TryDequeue(out string? locationName))
            {
                try
                {
                    long id = session.Locations.GetLocationIdFromName(Plugin.GameName, locationName);
                    if (id < 0)
                    {
                        Plugin.LoggerInstance?.LogWarning(
                            $"[SCRC-AP] Location '{locationName}' is not in the connected datapackage; keeping it local only.");
                        continue;
                    }

                    if (!session.Locations.AllLocations.Contains(id))
                    {
                        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SKIPPED INACTIVE CHECK '{locationName}' ({id}); not enabled in this seed.");
                        continue;
                    }
                    session.Locations.CompleteLocationChecks(id);
                    Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SENT CHECK '{locationName}' ({id}).");
                }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogError($"[SCRC-AP] Failed to send '{locationName}': {ex.GetBaseException().Message}");
                    _pendingChecks.Enqueue(locationName);
                    sendFailed = true;
                    break;
                }
            }
        }

        if (sendFailed)
            ClearCurrentSession(
                session,
                generation,
                () => RequestReconnect("location check send failed"));
    }
}

internal static class Level6Discovery
{
    private const string Level02DoorRootPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level02";

    private const string UpperPathDoorsRootPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors";

    private const string TrainingCentreRootPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21";

    private const string TrainingLevel19RootPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19";

    private const string TrainingLevel21RootPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/ContainerWithHole-tofb/ShippingContainerWithHole/Level21Door";

    private static readonly object Sync = new();

    private static bool _active;
    private static int _sequence;

    private static readonly string[] RouteObjectTokens =
    {
        "Level_02",
        "Level02",
        "Level_03",
        "Level03",
        "Level_10",
        "Level10",
        "Level_12",
        "Level12",
        "Level_14",
        "Level14",
        "Boring",
        "BoringRoom",
        "Training",
        "Demolition",
        "Heist",
        "Menial",
        "Important",
        "Letter",
        "Bean",
        "Trumpet",
        "Hand",
        "Button",
        "ManualButton",
        "Room02",
        "Room_02",
        "Minim",
        "Tower",
        "Vault",
        "Plunger",
        "Phone",
        "Booth",
        "Entrance",
        "LevelDoor",
        "Door",
        "Blockade",
        "Barrier"
    };

    public static bool Active
    {
        get
        {
            lock (Sync)
                return _active;
        }
    }

    public static void Begin(string reason)
    {
        lock (Sync)
        {
            _active = true;
            _sequence = 0;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 7 / UPPER LOBBY DISCOVERY START ===== reason='{reason}' currentRoom='{DeveloperHarness.CurrentRoomId}'.");

        Record(
            "BASELINE",
            "Tracing vanilla progression after Boring Room / internal Level_02 toward Demolition Training and the upper-lobby branches. Important Letters, Bean Trumpet, mail delivery, hand blockers, Minim Tower, Vault, and all related story progression remain vanilla.");
    }

    public static void RecordLevelResultApplied(string level)
    {
        if (string.Equals(
                level,
                "Level_02",
                StringComparison.OrdinalIgnoreCase))
        {
            Begin("Level_02 / Boring Room result applied");
        }

        if (Active)
            Record("LEVEL RESULT APPLIED", level);
    }

    public static void RecordLevelPersisted(string level)
    {
        if (!Active &&
            string.Equals(
                level,
                "Level_02",
                StringComparison.OrdinalIgnoreCase))
        {
            Begin("Level_02 / Boring Room result persisted");
        }

        if (Active)
            Record("LEVEL PERSISTED", level);
    }

    public static void RecordTransition(string roomId)
    {
        if (!Active)
            return;

        Record("ROOM TRANSITION", roomId);
    }

    public static void RecordProgressionRequest(string flag)
    {
        if (!Active)
            return;

        Record("PROGRESSION REQUEST", flag);
    }

    public static void RecordProgressionFlagUpdated(string flag)
    {
        if (!Active)
            return;

        Record("PROGRESSION FLAG UPDATED", flag);
    }

    public static void RecordBagItem(object request)
    {
        if (!Active)
            return;

        Record(
            "BAG ITEM",
            SummarizeRequest(
                request,
                new[]
                {
                    "BagItem",
                    "BagItemIdentifier",
                    "Item",
                    "ItemIdentifier",
                    "Identifier"
                }));
    }

    public static void RecordAbilityItem(object request)
    {
        if (!Active)
            return;

        Record(
            "ABILITY ITEM",
            SummarizeRequest(
                request,
                new[]
                {
                    "Ability",
                    "AbilityIdentifier",
                    "AbilityItem",
                    "Item",
                    "Identifier"
                }));
    }

    public static void ManualBegin()
    {
        Begin("developer F4 manual/restart");
    }

    public static void ScanCurrentScene()
    {
        if (!DeveloperHarness.Enabled)
            return;

        string room = DeveloperHarness.CurrentRoomId;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 7 F5 UPPER LOBBY SCENE SCAN BEGIN ===== currentRoom='{room}'.");

        int matches = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string name = tr.name ?? string.Empty;

                bool tokenMatch = RouteObjectTokens.Any(token =>
                    name.Contains(
                        token,
                        StringComparison.OrdinalIgnoreCase));

                if (!tokenMatch)
                    continue;

                string path = BuildHierarchy(tr);

                // Focus the scan on gameplay/hub hierarchy and ignore most UI
                // matches unless they contain a particularly useful route name.
                bool usefulUi =
                    name.Contains("Boring", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Vault", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Minim", StringComparison.OrdinalIgnoreCase) ||
                    name.Contains("Plunger", StringComparison.OrdinalIgnoreCase);

                if (path.Contains("OverarchingUIRoot", StringComparison.Ordinal) &&
                    !usefulUi)
                {
                    continue;
                }

                if (!seen.Add(path))
                    continue;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 7 ROUTE OBJECT path='{path}' " +
                    $"activeSelf={SafeActiveSelf(tr)} activeInHierarchy={SafeActiveInHierarchy(tr)} " +
                    $"worldPos=({tr.position.x:0.###},{tr.position.y:0.###},{tr.position.z:0.###}).");

                matches++;

                if (matches >= 350)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] LEVEL 7 F5 UPPER LOBBY SCENE SCAN capped at 350 matching transforms.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 7 F5 UPPER LOBBY SCENE SCAN failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 7 F5 UPPER LOBBY SCENE SCAN END ===== matches={matches}.");

        DumpUpperPathDoorsSubtree();
        DumpTrainingCentreSubtrees();
    }

    private static void DumpUpperPathDoorsSubtree()
    {
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== UPPER PATH DOORS SUBTREE DUMP BEGIN ===== root='{UpperPathDoorsRootPath}'.");

        int matches = 0;
        bool rootFound = false;
        var rows = new List<(string Path, bool ActiveSelf, bool ActiveInHierarchy, Vector3 Position)>();

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                bool isRoot =
                    string.Equals(
                        path,
                        UpperPathDoorsRootPath,
                        StringComparison.Ordinal);

                bool isDescendant =
                    path.StartsWith(
                        UpperPathDoorsRootPath + "/",
                        StringComparison.Ordinal);

                if (!isRoot && !isDescendant)
                    continue;

                if (isRoot)
                    rootFound = true;

                rows.Add(
                    (
                        path,
                        SafeActiveSelf(tr),
                        SafeActiveInHierarchy(tr),
                        tr.position
                    ));
            }

            rows.Sort((a, b) =>
            {
                int depthA = CountPathDepth(a.Path);
                int depthB = CountPathDepth(b.Path);

                int byDepth = depthA.CompareTo(depthB);
                if (byDepth != 0)
                    return byDepth;

                return string.Compare(
                    a.Path,
                    b.Path,
                    StringComparison.Ordinal);
            });

            foreach (var row in rows)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] UPPER DOORS SUBTREE path='{row.Path}' " +
                    $"activeSelf={row.ActiveSelf} activeInHierarchy={row.ActiveInHierarchy} " +
                    $"worldPos=({row.Position.x:0.###},{row.Position.y:0.###},{row.Position.z:0.###}).");

                matches++;

                if (matches >= 700)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] UPPER PATH DOORS SUBTREE DUMP capped at 700 transforms.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] UPPER PATH DOORS SUBTREE DUMP failed: {ex.GetBaseException().Message}");
        }

        if (!rootFound)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] UPPER PATH DOORS SUBTREE DUMP: exact Objects/Doors root was not found in the current scene.");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== UPPER PATH DOORS SUBTREE DUMP END ===== matches={matches} rootFound={rootFound}.");
    }

    private static void DumpTrainingCentreSubtrees()
    {
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== TRAINING CENTRE SUBTREE DUMP BEGIN ===== root='{TrainingCentreRootPath}'.");

        int matches = 0;
        bool centreFound = false;
        bool level19Found = false;
        bool level21Found = false;

        var rows = new List<(string Path, bool ActiveSelf, bool ActiveInHierarchy, Vector3 Position)>();

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                bool inCentre =
                    string.Equals(path, TrainingCentreRootPath, StringComparison.Ordinal) ||
                    path.StartsWith(TrainingCentreRootPath + "/", StringComparison.Ordinal);

                if (!inCentre)
                    continue;

                if (string.Equals(path, TrainingCentreRootPath, StringComparison.Ordinal))
                    centreFound = true;

                if (string.Equals(path, TrainingLevel19RootPath, StringComparison.Ordinal))
                    level19Found = true;

                if (string.Equals(path, TrainingLevel21RootPath, StringComparison.Ordinal))
                    level21Found = true;

                rows.Add(
                    (
                        path,
                        SafeActiveSelf(tr),
                        SafeActiveInHierarchy(tr),
                        tr.position
                    ));
            }

            rows.Sort((a, b) =>
            {
                int depthA = CountPathDepth(a.Path);
                int depthB = CountPathDepth(b.Path);

                int byDepth = depthA.CompareTo(depthB);
                if (byDepth != 0)
                    return byDepth;

                return string.Compare(a.Path, b.Path, StringComparison.Ordinal);
            });

            foreach (var row in rows)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] TRAINING SUBTREE path='{row.Path}' " +
                    $"activeSelf={row.ActiveSelf} activeInHierarchy={row.ActiveInHierarchy} " +
                    $"worldPos=({row.Position.x:0.###},{row.Position.y:0.###},{row.Position.z:0.###}).");

                matches++;

                if (matches >= 450)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] TRAINING CENTRE SUBTREE DUMP capped at 450 transforms.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] TRAINING CENTRE SUBTREE DUMP failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== TRAINING CENTRE SUBTREE DUMP END ===== matches={matches} " +
            $"centreFound={centreFound} level19Found={level19Found} level21Found={level21Found}.");
    }

    private static void DumpLevel02DoorSubtree()
    {
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 02 DOOR SUBTREE DUMP BEGIN ===== root='{Level02DoorRootPath}'.");

        int matches = 0;
        bool rootFound = false;
        var rows = new List<(string Path, bool ActiveSelf, bool ActiveInHierarchy, Vector3 Position)>();

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                bool isRoot =
                    string.Equals(
                        path,
                        Level02DoorRootPath,
                        StringComparison.Ordinal);

                bool isDescendant =
                    path.StartsWith(
                        Level02DoorRootPath + "/",
                        StringComparison.Ordinal);

                if (!isRoot && !isDescendant)
                    continue;

                if (isRoot)
                    rootFound = true;

                rows.Add(
                    (
                        path,
                        SafeActiveSelf(tr),
                        SafeActiveInHierarchy(tr),
                        tr.position
                    ));
            }

            rows.Sort((a, b) =>
            {
                int depthA = CountPathDepth(a.Path);
                int depthB = CountPathDepth(b.Path);

                int byDepth = depthA.CompareTo(depthB);
                if (byDepth != 0)
                    return byDepth;

                return string.Compare(
                    a.Path,
                    b.Path,
                    StringComparison.Ordinal);
            });

            foreach (var row in rows)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL02 SUBTREE path='{row.Path}' " +
                    $"activeSelf={row.ActiveSelf} activeInHierarchy={row.ActiveInHierarchy} " +
                    $"worldPos=({row.Position.x:0.###},{row.Position.y:0.###},{row.Position.z:0.###}).");

                matches++;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 02 DOOR SUBTREE DUMP failed: {ex.GetBaseException().Message}");
        }

        if (!rootFound)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] LEVEL 02 DOOR SUBTREE DUMP: exact Level02 root was not found in the currently loaded scene.");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 02 DOOR SUBTREE DUMP END ===== matches={matches} rootFound={rootFound}.");
    }

    private static int CountPathDepth(string path)
    {
        if (string.IsNullOrEmpty(path))
            return 0;

        int depth = 1;

        foreach (char c in path)
        {
            if (c == '/')
                depth++;
        }

        return depth;
    }

    private static void Record(
        string kind,
        string detail)
    {
        int sequence;

        lock (Sync)
        {
            if (!_active)
                return;

            sequence = ++_sequence;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL7 TRACE #{sequence:000} [{kind}] {detail}");
    }

    private static string SummarizeRequest(
        object request,
        IEnumerable<string> preferredNames)
    {
        try
        {
            foreach (string name in preferredNames)
            {
                object? value = ReflectionUtil.ReadMember(
                    request,
                    name);

                value = ReflectionUtil.UnwrapNullable(value);

                if (value == null)
                    continue;

                string? identifier =
                    ReflectionUtil.ExtractIdentifier(value);

                if (!string.IsNullOrWhiteSpace(identifier))
                    return $"{name}={identifier}";

                string raw = value.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(raw))
                    return $"{name}={raw}";
            }
        }
        catch
        {
        }

        return $"requestType={request.GetType().Name}";
    }

    private static bool SafeActiveSelf(Transform tr)
    {
        try
        {
            return tr.gameObject.activeSelf;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeActiveInHierarchy(Transform tr)
    {
        try
        {
            return tr.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 36 && current != null; i++)
            {
                names.Add(current.name ?? "<unnamed>");
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return "<unavailable>";
        }
    }
}


internal sealed class BunkerStarRequirementKeeper : MonoBehaviour
{
    public static int RequiredStars = 50;

    public static int RoyalRequiredStars = 40;
    private static StarEaterTestTarget? _activeTarget;
    private static int ActiveRequiredStars => _activeTarget?.Required ?? 66;

    private const string StarEaterComponentName =
        "StarEaterInteraction";

    private const string ThresholdFieldName =
        "feedingThreshold";

    // Patch only while physically near the actual StarEaterInteraction object.
    // This keeps the generic DefinedValue<int>.GetValue() override extremely
    // short-lived and avoids changing other Hub8 numeric definitions while the
    // player is elsewhere.
    private const float PatchRadius =
        8.0f;

    private const float ReleaseRadius =
        10.0f;

    private Transform? _interactionTransform;
    private Transform? _playerTransform;

    private IntPtr _thresholdObject;
    private IntPtr _thresholdClass;
    private IntPtr _getValueMethod;

    private int _targetRetryFrame;
    private int _playerRetryFrame;
    private bool _reportedReady;
    private bool _reportedPlayerFailure;

    private static IntPtr _getValueCodePointer;
    private static byte[]? _getValueOriginalBytes;
    private static bool _getValuePatched;
    private static bool _reportedPatchFailure;

    private static readonly Dictionary<IntPtr, byte[]> StarEaterBoolOriginalBytes =
        new();

    private static bool _starEaterInteractionChecksPatched;
    private static bool _interactionSessionStarted;
    private static bool _reportedInteractionPatchActive;
    private static bool _reportedInteractionInterestActive;
    private static bool _reportedInteractionInterestFailure;

    private static bool _manualRetryRequested;
    private static float _manualHoldUntil;

    public BunkerStarRequirementKeeper(
        IntPtr pointer) : base(pointer)
    {
    }

    public static void RequestManualRetry()
    {
        _manualRetryRequested = true;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV SHIFT+F5: requested one manual configured Star Eater GetValue patch attempt. If successful, the patch will be held for 20 seconds or until leaving the configured room.");
    }

    public static void ResetForSceneChange()
    {
        RestoreStarEaterInteractionPatches(
            "scene transition");

        RestoreNativeGetValuePatch(
            "scene transition");

        _manualRetryRequested = false;
        _manualHoldUntil = 0f;
        _reportedPatchFailure = false;
        _reportedInteractionInterestFailure = false;
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("BunkerStarRequirementKeeper.Update");
        if (ApStars.State.Active)
        {
            RestoreStarEaterInteractionPatches("AP Star contract active");
            RestoreNativeGetValuePatch("AP Star contract active");
            ClearCachedTargets();
            _activeTarget = null;
            return;
        }
        StarEaterTestTarget? selected = StarEaterTestOverridePolicy.Select(
            DeveloperHarness.CurrentRoomId, RoyalRequiredStars, RequiredStars);
        if (_activeTarget != selected)
        {
            RestoreStarEaterInteractionPatches("test target changed");
            RestoreNativeGetValuePatch("test target changed");
            ClearCachedTargets();
            _activeTarget = selected;
            _manualRetryRequested = false;
            _manualHoldUntil = 0f;
        }
        if (selected == null)
        {
            RestoreStarEaterInteractionPatches(
                "outside configured test room / vanilla requirement");

            RestoreNativeGetValuePatch(
                "outside configured test room / vanilla requirement");

            ClearCachedTargets();
            return;
        }

        bool manual =
            _manualRetryRequested;

        if (manual)
        {
            _manualRetryRequested = false;
            _manualHoldUntil =
                Time.time + 20f;

            _targetRetryFrame = 0;
            _playerRetryFrame = 0;
            _reportedPatchFailure = false;
        }

        if (!TargetsReady())
        {
            if (Time.frameCount >= _targetRetryFrame)
            {
                _targetRetryFrame =
                    Time.frameCount + 30;

                ResolveThresholdTarget();
            }
        }

        if (!TargetsReady())
        {
            RestoreStarEaterInteractionPatches(
                "Star Eater threshold target unavailable");

            RestoreNativeGetValuePatch(
                "Star Eater threshold target unavailable");

            return;
        }

        if (!PlayerReady())
        {
            if (Time.frameCount >= _playerRetryFrame)
            {
                _playerRetryFrame =
                    Time.frameCount + 30;

                ResolvePlayer();
            }
        }

        bool manualHold =
            Time.time < _manualHoldUntil;

        if (!PlayerReady())
        {
            if (manualHold)
            {
                TryApplyNativeGetValuePatch(
                    "manual hold without player marker");
            }
            else
            {
                RestoreStarEaterInteractionPatches(
                    "player marker unavailable");

                RestoreNativeGetValuePatch(
                    "player marker unavailable");
            }

            return;
        }

        float distance;

        try
        {
            distance =
                Vector3.Distance(
                    _playerTransform!.position,
                    _interactionTransform!.position);
        }
        catch
        {
            _playerTransform = null;

            RestoreStarEaterInteractionPatches(
                "player/interaction transform invalid");

            RestoreNativeGetValuePatch(
                "player/interaction transform invalid");

            return;
        }

        bool shouldEnable =
            manualHold ||
            distance <= PatchRadius;

        if (!shouldEnable)
        {
            if (distance > ReleaseRadius)
            {
                RestoreStarEaterInteractionPatches(
                    $"player left Star Eater range ({distance:0.###})");

                RestoreNativeGetValuePatch(
                    $"player left Star Eater range ({distance:0.###})");
            }

            return;
        }

        if (!_getValuePatched)
        {
            TryApplyNativeGetValuePatch(
                manualHold
                    ? $"manual hold distance={distance:0.###}"
                    : $"player distance={distance:0.###}");
        }

        if (!_getValuePatched)
            return;

        if (!_starEaterInteractionChecksPatched)
        {
            TryApplyStarEaterInteractionPatches(
                manualHold
                    ? $"manual hold distance={distance:0.###}"
                    : $"player distance={distance:0.###}");
        }

        if (_starEaterInteractionChecksPatched)
        {
            TryRegisterStarEaterInteractionInterest();
        }
    }

    private void OnDisable()
    {
        RestoreStarEaterInteractionPatches(
            "keeper disabled");

        RestoreNativeGetValuePatch(
            "keeper disabled");
    }

    private void OnDestroy()
    {
        RestoreStarEaterInteractionPatches(
            "keeper destroyed");

        RestoreNativeGetValuePatch(
            "keeper destroyed");
    }

    private void ClearCachedTargets()
    {
        _interactionTransform = null;
        _playerTransform = null;

        _thresholdObject = IntPtr.Zero;
        _thresholdClass = IntPtr.Zero;
        _getValueMethod = IntPtr.Zero;

        _targetRetryFrame = 0;
        _playerRetryFrame = 0;
        _reportedReady = false;
        _reportedPlayerFailure = false;

        _interactionSessionStarted = false;
        _reportedInteractionInterestActive = false;
        _reportedInteractionInterestFailure = false;
    }

    private bool TargetsReady()
    {
        try
        {
            return _interactionTransform != null &&
                   _interactionTransform.gameObject != null &&
                   _thresholdObject != IntPtr.Zero &&
                   _thresholdClass != IntPtr.Zero &&
                   _getValueMethod != IntPtr.Zero;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerReady()
    {
        try
        {
            return _playerTransform != null &&
                   _playerTransform.gameObject != null &&
                   _playerTransform.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void ResolveThresholdTarget()
    {
        GameObject? root =
            FindKnownStarEaterRoot();

        if (root == null)
            return;

        if (!FindNativeComponentInNarrowSubtree(
                root,
                StarEaterComponentName,
                out object? rawComponent,
                out Transform? componentTransform))
        {
            return;
        }

        if (rawComponent == null ||
            componentTransform == null)
        {
            return;
        }

        IntPtr componentPointer =
            GetIl2CppPointer(
                rawComponent);

        if (componentPointer == IntPtr.Zero)
            return;

        try
        {
            IntPtr componentClass =
                il2cpp_object_get_class(
                    componentPointer);

            if (componentClass == IntPtr.Zero)
                return;

            IntPtr thresholdField =
                FindFieldInHierarchy(
                    componentClass,
                    ThresholdFieldName);

            if (thresholdField == IntPtr.Zero)
                return;

            IntPtr thresholdObject =
                il2cpp_field_get_value_object(
                    thresholdField,
                    componentPointer);

            if (thresholdObject == IntPtr.Zero)
                return;

            IntPtr thresholdClass =
                il2cpp_object_get_class(
                    thresholdObject);

            if (thresholdClass == IntPtr.Zero)
                return;

            IntPtr getValueMethod =
                FindMethodInHierarchy(
                    thresholdClass,
                    "GetValue",
                    0);

            if (getValueMethod == IntPtr.Zero)
                return;

            _interactionTransform =
                componentTransform;

            _thresholdObject =
                thresholdObject;

            _thresholdClass =
                thresholdClass;

            _getValueMethod =
                getValueMethod;

            if (!_reportedReady)
            {
                _reportedReady = true;

                int current =
                    TryInvokeGetValue(
                        thresholdObject,
                        getValueMethod,
                        out int evaluated)
                        ? evaluated
                        : int.MinValue;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH READY: interaction='{BuildHierarchy(componentTransform)}', currentGetValue={(current == int.MinValue ? "<unknown>" : current.ToString())}, patchRadius={PatchRadius:0.##}, releaseRadius={ReleaseRadius:0.##}. feedingThreshold object data will not be modified.");
            }
        }
        catch (Exception ex)
        {
            ReportPatchFailureOnce(
                $"target resolution failed: {ex.GetBaseException().Message}");
        }
    }

    private void ResolvePlayer()
    {
        string[] candidates =
        {
            "GameRoomContainer/GameRoomSystems(Clone)/SpawnedEntityContainer/PlayerCharacter(Clone)",
            "PlayerCharacter(Clone)",
            "PlayerCharacter",
        };

        foreach (string candidate in candidates)
        {
            try
            {
                GameObject? player =
                    GameObject.Find(
                        candidate);

                if (player == null ||
                    player.transform == null ||
                    !player.activeInHierarchy)
                {
                    continue;
                }

                _playerTransform =
                    player.transform;

                _reportedPlayerFailure = false;

                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] STAR EATER TEST player marker resolved directly: '{BuildHierarchy(_playerTransform)}'.");

                return;
            }
            catch
            {
            }
        }

        if (!_reportedPlayerFailure)
        {
            _reportedPlayerFailure = true;

            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] STAR EATER TEST player marker not yet available through direct GameObject.Find; no scene-wide search will be performed.");
        }
    }

    private void TryApplyStarEaterInteractionPatches(
        string reason)
    {
        if (_starEaterInteractionChecksPatched)
            return;

        if (_thresholdObject == IntPtr.Zero ||
            _interactionTransform == null ||
            _interactionTransform.gameObject == null)
        {
            return;
        }

        object? rawComponent =
            FindNativeComponentByName(
                _interactionTransform.gameObject,
                StarEaterComponentName);

        if (rawComponent == null)
        {
            ReportInteractionFailureOnce(
                "could not re-resolve StarEaterInteraction on the exact interaction transform");

            return;
        }

        IntPtr componentPointer =
            GetIl2CppPointer(
                rawComponent);

        if (componentPointer == IntPtr.Zero)
        {
            ReportInteractionFailureOnce(
                "StarEaterInteraction native object pointer was null");

            return;
        }

        IntPtr componentClass;

        try
        {
            componentClass =
                il2cpp_object_get_class(
                    componentPointer);
        }
        catch (Exception ex)
        {
            ReportInteractionFailureOnce(
                $"could not resolve StarEaterInteraction native class: {ex.GetBaseException().Message}");

            return;
        }

        if (componentClass == IntPtr.Zero)
            return;

        var targets =
            new (string Name, int Args)[]
            {
                ("DoesStarEaterWantToInteract", 0),
                ("CanFeedNewStars", 0),
                ("IsCharacterAllowedToInteract", 1),
                ("IsInteractionEnabled", 0),
            };

        var newlyPatched =
            new List<IntPtr>();

        try
        {
            foreach (var target in targets)
            {
                IntPtr method =
                    FindMethodInHierarchy(
                        componentClass,
                        target.Name,
                        target.Args);

                if (method == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"{target.Name}({target.Args}) not found");
                }

                IntPtr codePointer =
                    GetMethodCodePointer(
                        method);

                if (codePointer == IntPtr.Zero)
                {
                    throw new InvalidOperationException(
                        $"{target.Name} code pointer was null");
                }

                // Multiple inherited/generic methods can theoretically share a
                // native code pointer. Store/patch each unique pointer once.
                if (StarEaterBoolOriginalBytes.ContainsKey(
                        codePointer))
                {
                    continue;
                }

                byte[] forceTrue =
                {
                    0xB8, 0x01, 0x00, 0x00, 0x00,
                    0xC3
                };

                byte[] original =
                    new byte[forceTrue.Length];

                Marshal.Copy(
                    codePointer,
                    original,
                    0,
                    original.Length);

                if (!WriteExecutableBytes(
                        codePointer,
                        forceTrue))
                {
                    throw new InvalidOperationException(
                        $"failed to patch {target.Name} at 0x{codePointer.ToInt64():X}");
                }

                StarEaterBoolOriginalBytes[
                    codePointer] =
                    original;

                newlyPatched.Add(
                    codePointer);
            }

            _starEaterInteractionChecksPatched =
                true;

            _interactionSessionStarted =
                false;

            if (!_reportedInteractionPatchActive)
            {
                _reportedInteractionPatchActive =
                    true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] STAR EATER TEST INTERACTION PATCH ACTIVE: forced Star Eater DoesStarEaterWantToInteract, CanFeedNewStars, IsCharacterAllowedToInteract, and IsInteractionEnabled true; reason='{reason}'. Patches are proximity-scoped and restored on exit/scene transition.");
            }
        }
        catch (Exception ex)
        {
            // Restore anything applied during this partial attempt.
            foreach (IntPtr codePointer in newlyPatched)
            {
                if (!StarEaterBoolOriginalBytes.TryGetValue(
                        codePointer,
                        out byte[]? original))
                {
                    continue;
                }

                WriteExecutableBytes(
                    codePointer,
                    original);

                StarEaterBoolOriginalBytes.Remove(
                    codePointer);
            }

            _starEaterInteractionChecksPatched =
                false;

            ReportInteractionFailureOnce(
                $"interaction patch apply failed: {ex.GetBaseException().Message}");
        }
    }

    private static void RestoreStarEaterInteractionPatches(
        string reason)
    {
        if (StarEaterBoolOriginalBytes.Count == 0 &&
            !_starEaterInteractionChecksPatched)
        {
            _interactionSessionStarted = false;
            _reportedInteractionInterestActive = false;
            return;
        }

        bool allRestored =
            true;

        foreach (var pair in
            StarEaterBoolOriginalBytes.ToArray())
        {
            if (!WriteExecutableBytes(
                    pair.Key,
                    pair.Value))
            {
                allRestored = false;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] STAR EATER TEST interaction method restore failed at 0x{pair.Key.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}.");
            }
        }

        StarEaterBoolOriginalBytes.Clear();

        _starEaterInteractionChecksPatched =
            false;

        _interactionSessionStarted =
            false;

        _reportedInteractionInterestActive =
            false;

        _reportedInteractionInterestFailure =
            false;

        if (_reportedInteractionPatchActive)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] STAR EATER TEST INTERACTION PATCH RESTORED: reason='{reason}', allRestored={allRestored}.");
        }

        _reportedInteractionPatchActive =
            false;
    }

    private void TryRegisterStarEaterInteractionInterest()
    {
        if (!_starEaterInteractionChecksPatched ||
            _interactionTransform == null ||
            _interactionTransform.gameObject == null)
        {
            return;
        }

        object? rawComponent =
            FindNativeComponentByName(
                _interactionTransform.gameObject,
                StarEaterComponentName);

        if (rawComponent == null)
            return;

        IntPtr componentPointer =
            GetIl2CppPointer(
                rawComponent);

        if (componentPointer == IntPtr.Zero)
            return;

        IntPtr componentClass =
            il2cpp_object_get_class(
                componentPointer);

        if (componentClass == IntPtr.Zero)
            return;

        try
        {
            if (!_interactionSessionStarted)
            {
                IntPtr sessionMethod =
                    FindMethodInHierarchy(
                        componentClass,
                        "OnNewInteractionInterestSession",
                        0);

                if (sessionMethod != IntPtr.Zero)
                {
                    IntPtr sessionException =
                        IntPtr.Zero;

                    il2cpp_runtime_invoke(
                        sessionMethod,
                        componentPointer,
                        IntPtr.Zero,
                        ref sessionException);

                    if (sessionException != IntPtr.Zero)
                    {
                        ReportInteractionInterestFailureOnce(
                            $"OnNewInteractionInterestSession exception=0x{sessionException.ToInt64():X}");

                        return;
                    }
                }

                _interactionSessionStarted =
                    true;
            }

            IntPtr interestMethod =
                FindMethodInHierarchy(
                    componentClass,
                    "RecordInteractionInterest",
                    0);

            if (interestMethod == IntPtr.Zero)
            {
                ReportInteractionInterestFailureOnce(
                    "RecordInteractionInterest() not found");

                return;
            }

            IntPtr interestException =
                IntPtr.Zero;

            il2cpp_runtime_invoke(
                interestMethod,
                componentPointer,
                IntPtr.Zero,
                ref interestException);

            if (interestException != IntPtr.Zero)
            {
                _interactionSessionStarted =
                    false;

                ReportInteractionInterestFailureOnce(
                    $"RecordInteractionInterest exception=0x{interestException.ToInt64():X}");

                return;
            }

            _reportedInteractionInterestFailure =
                false;

            if (!_reportedInteractionInterestActive)
            {
                _reportedInteractionInterestActive =
                    true;

                Plugin.LoggerInstance?.LogInfo(
                    "[SCRC-AP] STAR EATER TEST NATIVE INTERACTION INTEREST ACTIVE: registered the real StarEaterInteraction with the game's interaction-interest pipeline.");
            }
        }
        catch (Exception ex)
        {
            ReportInteractionInterestFailureOnce(
                $"interaction-interest registration failed: {ex.GetBaseException().Message}");
        }
    }

    private static object? FindNativeComponentByName(
        GameObject gameObject,
        string componentName)
    {
        MethodInfo? getByName =
            typeof(GameObject).GetMethod(
                "GetComponent",
                BindingFlags.Public |
                BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);

        if (getByName == null)
            return null;

        return TryGetNativeComponentByName(
            getByName,
            gameObject,
            componentName);
    }

    private static void ReportInteractionFailureOnce(
        string message)
    {
        if (_reportedInteractionPatchActive)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] STAR EATER TEST INTERACTION PATCH FAILURE: {message}");
    }

    private static void ReportInteractionInterestFailureOnce(
        string message)
    {
        if (_reportedInteractionInterestFailure)
            return;

        _reportedInteractionInterestFailure =
            true;

        Plugin.LoggerInstance?.LogDebug(
            $"[SCRC-AP] STAR EATER TEST interaction-interest transient failure: {message}");
    }

    private void TryApplyNativeGetValuePatch(
        string reason)
    {
        if (_getValuePatched)
            return;

        if (_getValueMethod == IntPtr.Zero ||
            _thresholdObject == IntPtr.Zero)
        {
            return;
        }

        try
        {
            int before =
                TryInvokeGetValue(
                    _thresholdObject,
                    _getValueMethod,
                    out int evaluatedBefore)
                    ? evaluatedBefore
                    : int.MinValue;

            IntPtr codePointer =
                GetMethodCodePointer(
                    _getValueMethod);

            if (codePointer == IntPtr.Zero)
            {
                ReportPatchFailureOnce(
                    "DefinedValue<int>.GetValue() native code pointer was null");

                return;
            }

            // Windows x64:
            //   mov eax, imm32
            //   ret
            //
            // GetValue returns System.Int32, so this bypasses only the evaluator
            // while the player is near this Star Eater. It does not mutate the
            // DefinedInt or IntDefinition objects.
            byte[] forceConfiguredValue =
            {
                0xB8,
                (byte)(ActiveRequiredStars & 0xFF),
                (byte)((ActiveRequiredStars >> 8) & 0xFF),
                (byte)((ActiveRequiredStars >> 16) & 0xFF),
                (byte)((ActiveRequiredStars >> 24) & 0xFF),
                0xC3
            };

            byte[] original =
                new byte[forceConfiguredValue.Length];

            Marshal.Copy(
                codePointer,
                original,
                0,
                original.Length);

            if (!WriteExecutableBytes(
                    codePointer,
                    forceConfiguredValue))
            {
                ReportPatchFailureOnce(
                    $"VirtualProtect/write failed at 0x{codePointer.ToInt64():X}");

                return;
            }

            _getValueCodePointer =
                codePointer;

            _getValueOriginalBytes =
                original;

            _getValuePatched =
                true;

            bool verified =
                TryInvokeGetValue(
                    _thresholdObject,
                    _getValueMethod,
                    out int after);

            if (!verified ||
                after != ActiveRequiredStars)
            {
                RestoreNativeGetValuePatch(
                    "verification failed");

                ReportPatchFailureOnce(
                    $"patched GetValue verification returned {(verified ? after.ToString() : "<invoke-failed>")} instead of {ActiveRequiredStars}");

                return;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH ACTIVE: DefinedValue<int>.GetValue() {(before == int.MinValue ? "<unknown>" : before.ToString())} -> {after}; reason='{reason}'. Patch is temporary and feedingThreshold data is untouched.");
        }
        catch (Exception ex)
        {
            RestoreNativeGetValuePatch(
                "exception during apply");

            ReportPatchFailureOnce(
                $"native GetValue patch failed: {ex.GetBaseException().Message}");
        }
    }

    private static void RestoreNativeGetValuePatch(
        string reason)
    {
        if (!_getValuePatched)
            return;

        IntPtr codePointer =
            _getValueCodePointer;

        byte[]? original =
            _getValueOriginalBytes;

        try
        {
            if (codePointer != IntPtr.Zero &&
                original != null &&
                original.Length > 0)
            {
                if (WriteExecutableBytes(
                        codePointer,
                        original))
                {
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH RESTORED: reason='{reason}'.");
                }
                else
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH RESTORE FAILED at 0x{codePointer.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}.");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH RESTORE FAILED: {ex.GetBaseException().Message}");
        }
        finally
        {
            _getValuePatched = false;
            _getValueCodePointer = IntPtr.Zero;
            _getValueOriginalBytes = null;
        }
    }

    private static bool WriteExecutableBytes(
        IntPtr codePointer,
        byte[] bytes)
    {
        if (codePointer == IntPtr.Zero ||
            bytes.Length == 0)
        {
            return false;
        }

        const uint PAGE_EXECUTE_READWRITE =
            0x40;

        if (!VirtualProtect(
                codePointer,
                (UIntPtr)bytes.Length,
                PAGE_EXECUTE_READWRITE,
                out uint oldProtect))
        {
            return false;
        }

        try
        {
            Marshal.Copy(
                bytes,
                0,
                codePointer,
                bytes.Length);

            FlushInstructionCache(
                GetCurrentProcess(),
                codePointer,
                (UIntPtr)bytes.Length);

            return true;
        }
        finally
        {
            VirtualProtect(
                codePointer,
                (UIntPtr)bytes.Length,
                oldProtect,
                out _);
        }
    }

    private static IntPtr GetMethodCodePointer(
        IntPtr method)
    {
        if (method == IntPtr.Zero)
            return IntPtr.Zero;

        try
        {
            return il2cpp_method_get_pointer(
                method);
        }
        catch (EntryPointNotFoundException)
        {
            // Same Unity 2021 fallback already proven by the native
            // LevelEntranceDoor interaction bridge.
            try
            {
                return Marshal.ReadIntPtr(
                    method);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
    }

    private static bool TryInvokeGetValue(
        IntPtr thresholdObject,
        IntPtr getValueMethod,
        out int value)
    {
        value =
            int.MinValue;

        try
        {
            IntPtr exc =
                IntPtr.Zero;

            IntPtr boxed =
                il2cpp_runtime_invoke(
                    getValueMethod,
                    thresholdObject,
                    IntPtr.Zero,
                    ref exc);

            if (exc != IntPtr.Zero ||
                boxed == IntPtr.Zero)
            {
                return false;
            }

            IntPtr unboxed =
                il2cpp_object_unbox(
                    boxed);

            if (unboxed == IntPtr.Zero)
                return false;

            value =
                Marshal.ReadInt32(
                    unboxed);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static GameObject? FindKnownStarEaterRoot()
    {
        try
        {
            GameObject? exact =
                GameObject.Find(
                    _activeTarget!.RootPath);

            if (exact != null)
                return exact;
        }
        catch
        {
        }

        try
        {
            GameObject? named =
                GameObject.Find(
                    "StarEater");

            if (named == null ||
                named.transform == null)
            {
                return null;
            }

            string path =
                BuildHierarchy(
                    named.transform);

            return string.Equals(path, _activeTarget?.RootPath, StringComparison.Ordinal)
                ? named
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool FindNativeComponentInNarrowSubtree(
        GameObject root,
        string componentName,
        out object? component,
        out Transform? componentTransform)
    {
        component = null;
        componentTransform = null;

        MethodInfo? getByName =
            typeof(GameObject).GetMethod(
                "GetComponent",
                BindingFlags.Public |
                BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);

        if (getByName == null)
            return false;

        object? direct =
            TryGetNativeComponentByName(
                getByName,
                root,
                componentName);

        if (direct != null)
        {
            component = direct;
            componentTransform = root.transform;
            return true;
        }

        var queue =
            new Queue<(Transform Transform, int Depth)>();

        queue.Enqueue(
            (root.transform, 0));

        while (queue.Count > 0)
        {
            var entry =
                queue.Dequeue();

            Transform tr =
                entry.Transform;

            int depth =
                entry.Depth;

            if (tr == null ||
                tr.gameObject == null)
            {
                continue;
            }

            string name =
                tr.name ?? string.Empty;

            if (depth > 0 &&
                (name.Contains(
                     "IdlingTendrils",
                     StringComparison.OrdinalIgnoreCase) ||
                 name.Contains(
                     "Skeleton",
                     StringComparison.OrdinalIgnoreCase) ||
                 name.Contains(
                     "Geom",
                     StringComparison.OrdinalIgnoreCase) ||
                 name.Contains(
                     "Shadow",
                     StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            if (depth > 0)
            {
                object? found =
                    TryGetNativeComponentByName(
                        getByName,
                        tr.gameObject,
                        componentName);

                if (found != null)
                {
                    component = found;
                    componentTransform = tr;
                    return true;
                }
            }

            if (depth >= 6)
                continue;

            for (int i = 0;
                 i < tr.childCount;
                 i++)
            {
                Transform? child =
                    null;

                try
                {
                    child =
                        tr.GetChild(i);
                }
                catch
                {
                }

                if (child != null)
                {
                    queue.Enqueue(
                        (child, depth + 1));
                }
            }
        }

        return false;
    }

    private static object? TryGetNativeComponentByName(
        MethodInfo getByName,
        GameObject gameObject,
        string componentName)
    {
        try
        {
            return getByName.Invoke(
                gameObject,
                new object[] { componentName });
        }
        catch
        {
            return null;
        }
    }

    private const string NativeLibrary =
        "GameAssembly.dll";

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_get_class(
        IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_parent(
        IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_field_from_name(
        IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_field_get_value_object(
        IntPtr field,
        IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_method_from_name(
        IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int argsCount);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_method_get_pointer(
        IntPtr method);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_runtime_invoke(
        IntPtr method,
        IntPtr obj,
        IntPtr parameters,
        ref IntPtr exc);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_unbox(
        IntPtr obj);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtect(
        IntPtr lpAddress,
        UIntPtr dwSize,
        uint flNewProtect,
        out uint lpflOldProtect);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushInstructionCache(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        UIntPtr dwSize);

    private static IntPtr FindFieldInHierarchy(
        IntPtr klass,
        string fieldName)
    {
        IntPtr current =
            klass;

        for (int depth = 0;
             current != IntPtr.Zero &&
             depth < 10;
             depth++)
        {
            IntPtr field =
                il2cpp_class_get_field_from_name(
                    current,
                    fieldName);

            if (field != IntPtr.Zero)
                return field;

            current =
                il2cpp_class_get_parent(
                    current);
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindMethodInHierarchy(
        IntPtr klass,
        string methodName,
        int argCount)
    {
        IntPtr current =
            klass;

        for (int depth = 0;
             current != IntPtr.Zero &&
             depth < 10;
             depth++)
        {
            IntPtr method =
                il2cpp_class_get_method_from_name(
                    current,
                    methodName,
                    argCount);

            if (method != IntPtr.Zero)
                return method;

            current =
                il2cpp_class_get_parent(
                    current);
        }

        return IntPtr.Zero;
    }

    private static IntPtr GetIl2CppPointer(
        object value)
    {
        try
        {
            PropertyInfo? property =
                value.GetType().GetProperty(
                    "Pointer",
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance);

            return property?.GetValue(
                       value) is IntPtr pointer
                ? pointer
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static void ReportPatchFailureOnce(
        string message)
    {
        if (_reportedPatchFailure)
            return;

        _reportedPatchFailure = true;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] STAR EATER TEST GETVALUE PATCH FAILURE: {message}");
    }

    private static string BuildHierarchy(
        Transform tr)
    {
        try
        {
            var names =
                new List<string>();

            Transform? current =
                tr;

            for (int i = 0;
                 i < 40 &&
                 current != null;
                 i++)
            {
                names.Add(
                    current.name ??
                    "<unnamed>");

                current =
                    current.parent;
            }

            names.Reverse();

            return string.Join(
                "/",
                names);
        }
        catch
        {
            return "<unavailable>";
        }
    }
}


internal static class Level8Discovery
{
    private static readonly object Sync = new();

    private static bool _active;
    private static int _sequence;

    public static bool Active
    {
        get
        {
            lock (Sync)
                return _active;
        }
    }

    public static void Begin(string reason)
    {
        lock (Sync)
        {
            _active = true;
            _sequence = 0;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== SECRET BUNKER / STAR EATER DISCOVERY START ===== reason='{reason}' currentRoom='{DeveloperHarness.CurrentRoomId}'.");

        Record(
            "BASELINE",
            "King Ferdinand I rewards the user-facing Bunker Keycard as LIFT_KEY_BAG_ITEM. Using it in Lobby/Hub1A clears LIFT_KEY_BAG_ITEM and sets LOBBY_HUB_LIFT_KEY_USED, opening Root/GameRoom_Hub1_Logic/Hub1A_Logic_BunkerArea/BunkerDoor/BunkerEntrance and leading to GameRoom_Hub8 / The Secret Bunker. First entry sets BUNKER_HUB_FIRST_TIME_INTRO_WITNESSED. Gecko/traveler first interaction grants BUNKER_HUB_NOTE_PAD and BUNKER_HUB_DATA_STICK then BUNKER_HUB_GECKO_INITIAL_INTERACTION. Talking to the demon grants BUNKER_HUB_DEMON_KEY; Demon Key and all Devil Mode/seal/challenge progression remain vanilla. Talking to the Star Eater sets BUNKER_HUB_STAR_EATER_INTRODUCED; vanilla requires 66 stars, while the plugin lowers only this final Hub8 Star Eater threshold to the configured BunkerStarRequirement (default 50). Hub8 hierarchy exposes StarEater_Feed with GeckoAppears, CameraZoomPortalOpening, PlayZigTendralReachingIntoPortal, TurnOnCartridge, and a StarEaterFedCondition. Tracing the actual feed flags, post-feed portal/cartridge route, and next numbered level while preserving the normal feed interaction/cutscene.");
    }

    public static void ManualBegin()
    {
        Begin("developer F4 manual/restart");
    }

    public static void RecordTransition(string roomId)
    {
        if (!Active)
            return;

        Record("ROOM TRANSITION", roomId);
    }

    public static void RecordLevelResultApplied(string level)
    {
        if (!Active)
            return;

        Record("LEVEL RESULT APPLIED", level);
    }

    public static void RecordLevelPersisted(string level)
    {
        if (!Active)
            return;

        Record("LEVEL PERSISTED", level);
    }

    public static void RecordProgressionRequest(string flag)
    {
        if (!Active)
            return;

        Record("PROGRESSION REQUEST", flag);
    }

    public static void RecordProgressionFlagUpdated(string flag)
    {
        if (!Active)
            return;

        Record("PROGRESSION FLAG UPDATED", flag);
    }

    public static void RecordBagItem(object request)
    {
        if (!Active)
            return;

        Record(
            "BAG ITEM",
            SummarizeRequest(
                request,
                new[]
                {
                    "BagItem",
                    "BagItemIdentifier",
                    "Item",
                    "ItemIdentifier",
                    "Identifier"
                }));
    }

    public static void RecordAbilityItem(object request)
    {
        if (!Active)
            return;

        Record(
            "ABILITY ITEM",
            SummarizeRequest(
                request,
                new[]
                {
                    "Ability",
                    "AbilityIdentifier",
                    "AbilityItem",
                    "Item",
                    "Identifier"
                }));
    }

    public static void ScanCurrentScene()
    {
        if (!DeveloperHarness.Enabled)
            return;

        string room =
            DeveloperHarness.CurrentRoomId;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== SECRET BUNKER F5 SCAN BEGIN ===== currentRoom='{room}'.");

        Transform? player =
            FindPlayerMarker();

        Vector3 playerPosition =
            Vector3.zero;

        bool havePlayer =
            false;

        if (player != null)
        {
            try
            {
                playerPosition =
                    player.position;

                havePlayer =
                    true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] SECRET BUNKER player='{BuildHierarchy(player)}' " +
                    $"worldPos=({playerPosition.x:0.###},{playerPosition.y:0.###},{playerPosition.z:0.###}).");
            }
            catch
            {
            }
        }

        var rows = new List<(
            string Path,
            bool ActiveSelf,
            bool ActiveInHierarchy,
            Vector3 Position,
            float Distance,
            bool StrongCandidate)>();

        var candidateRoots =
            new Dictionary<string, Transform>(
                StringComparer.Ordinal);

        try
        {
            foreach (Transform tr in
                Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null ||
                    tr.gameObject == null)
                {
                    continue;
                }

                string path =
                    BuildHierarchy(tr);

                if (!BelongsToCurrentRoom(
                        room,
                        path))
                {
                    continue;
                }

                // Avoid the large visual hierarchies that made the earlier
                // boss scan noisy.
                if (path.Contains(
                        "/Abilities/Violin",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "ViolinProjectile",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "OverarchingUIRoot",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "/Skeleton/",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "/ShadowVolumes/",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Vector3 pos =
                    tr.position;

                float distance =
                    havePlayer
                        ? Vector3.Distance(
                            playerPosition,
                            pos)
                        : float.MaxValue;

                bool starEater =
                    path.Contains(
                        "StarEater",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Stardust",
                        StringComparison.OrdinalIgnoreCase);

                bool geckoPortal =
                    path.Contains(
                        "Gecko",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Portal",
                        StringComparison.OrdinalIgnoreCase);

                bool cartridge =
                    path.Contains(
                        "Cartridge",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Catridge",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "GameEnder",
                        StringComparison.OrdinalIgnoreCase);

                bool demon =
                    path.Contains(
                        "Demon",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Devil",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Seal",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Challenge",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Rune",
                        StringComparison.OrdinalIgnoreCase);

                bool bunkerItems =
                    path.Contains(
                        "NotePad",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "DataStick",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Data Stick",
                        StringComparison.OrdinalIgnoreCase);

                bool levelDoor =
                    path.Contains(
                        "LevelEntranceDoor",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "LevelDoor",
                        StringComparison.OrdinalIgnoreCase);

                bool route =
                    path.Contains(
                        "Entrance",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Exit",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Door",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Transition",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Gate",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Blocker",
                        StringComparison.OrdinalIgnoreCase);

                bool progression =
                    path.Contains(
                        "Condition",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Requirement",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Progress",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Sequence",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "Persist",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "TrueMode",
                        StringComparison.OrdinalIgnoreCase);

                bool strongCandidate =
                    starEater ||
                    geckoPortal ||
                    cartridge ||
                    demon ||
                    bunkerItems ||
                    levelDoor;

                bool interesting =
                    strongCandidate ||
                    route ||
                    progression;

                if (!interesting)
                    continue;

                // Keep all globally useful progression/Star Eater objects,
                // while limiting unrelated physical doors to the nearby area.
                if (havePlayer &&
                    distance > 34f &&
                    !strongCandidate &&
                    !progression)
                {
                    continue;
                }

                rows.Add(
                    (
                        path,
                        SafeActiveSelf(tr),
                        SafeActiveInHierarchy(tr),
                        pos,
                        distance,
                        strongCandidate
                    ));

                if (strongCandidate)
                {
                    Transform candidate =
                        ChooseCandidateRoot(tr);

                    string candidatePath =
                        BuildHierarchy(candidate);

                    if (!candidateRoots.ContainsKey(candidatePath))
                        candidateRoots[candidatePath] = candidate;
                }
            }

            rows.Sort((a, b) =>
            {
                if (a.StrongCandidate != b.StrongCandidate)
                    return a.StrongCandidate ? -1 : 1;

                if (havePlayer)
                {
                    int byDistance =
                        a.Distance.CompareTo(b.Distance);

                    if (byDistance != 0)
                        return byDistance;
                }

                return string.Compare(
                    a.Path,
                    b.Path,
                    StringComparison.Ordinal);
            });

            int count =
                0;

            foreach (var row in rows)
            {
                string distanceText =
                    havePlayer
                        ? row.Distance.ToString("0.###")
                        : "<unknown>";

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] SECRET BUNKER OBJECT path='{row.Path}' " +
                    $"strong={row.StrongCandidate} distance={distanceText} " +
                    $"activeSelf={row.ActiveSelf} " +
                    $"activeInHierarchy={row.ActiveInHierarchy} " +
                    $"worldPos=({row.Position.x:0.###},{row.Position.y:0.###},{row.Position.z:0.###}).");

                count++;

                if (count >= 420)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] SECRET BUNKER object dump capped at 420 transforms.");

                    break;
                }
            }

            int dumped =
                0;

            foreach (var pair in
                candidateRoots.OrderBy(
                    p => p.Key,
                    StringComparer.Ordinal))
            {
                if (dumped >= 18)
                    break;

                DumpCandidateSubtree(
                    pair.Key,
                    pair.Value);

                dumped++;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SECRET BUNKER scan summary objects={count} candidateRoots={candidateRoots.Count} dumped={dumped}.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SECRET BUNKER scan failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== SECRET BUNKER F5 SCAN END =====");
    }

    private static bool BelongsToCurrentRoom(
        string room,
        string path)
    {
        if (string.IsNullOrWhiteSpace(room))
            return false;

        if (path.Contains(
                room,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(
                room,
                "GameRoom_Hub1A",
                StringComparison.Ordinal))
        {
            return path.Contains(
                       "GameRoom_Hub1_Logic",
                       StringComparison.OrdinalIgnoreCase) ||
                   path.Contains(
                       "Hub1A_Logic",
                       StringComparison.OrdinalIgnoreCase) ||
                   path.Contains(
                       "Hub1a_Logic",
                       StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    private static Transform ChooseCandidateRoot(
        Transform tr)
    {
        Transform candidate =
            tr;

        Transform? current =
            tr;

        for (int i = 0;
             i < 10 && current != null;
             i++)
        {
            string name =
                current.name ?? string.Empty;

            if (name.Contains(
                    "StarEater",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Gecko",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Portal",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Cartridge",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Catridge",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Demon",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "Devil",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "LevelEntranceDoor",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains(
                    "LevelDoor",
                    StringComparison.OrdinalIgnoreCase))
            {
                candidate =
                    current;
            }

            current =
                current.parent;
        }

        return candidate;
    }

    private static void DumpCandidateSubtree(
        string rootPath,
        Transform root)
    {
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- SECRET BUNKER CANDIDATE BEGIN root='{rootPath}' -----");

        int count =
            0;

        try
        {
            foreach (Transform tr in
                root.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null ||
                    tr.gameObject == null)
                {
                    continue;
                }

                string path =
                    BuildHierarchy(tr);

                if (path.Contains(
                        "/Skeleton/",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "/ShadowVolumes/",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "/Abilities/Violin",
                        StringComparison.OrdinalIgnoreCase) ||
                    path.Contains(
                        "ViolinProjectile",
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] SECRET BUNKER CANDIDATE path='{path}' " +
                    $"activeSelf={SafeActiveSelf(tr)} " +
                    $"activeInHierarchy={SafeActiveInHierarchy(tr)} " +
                    $"worldPos=({tr.position.x:0.###},{tr.position.y:0.###},{tr.position.z:0.###}).");

                count++;

                if (count >= 180)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] SECRET BUNKER candidate '{rootPath}' capped at 180 transforms.");

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SECRET BUNKER candidate '{rootPath}' failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- SECRET BUNKER CANDIDATE END root='{rootPath}' matches={count} -----");
    }

    private static Transform? FindPlayerMarker()
    {
        Transform? exactPlayerRoot =
            null;

        Transform? fallbackPlayerChild =
            null;

        try
        {
            foreach (Transform tr in
                Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null ||
                    tr.gameObject == null ||
                    !tr.gameObject.activeInHierarchy)
                {
                    continue;
                }

                string name =
                    tr.name ?? string.Empty;

                string path =
                    BuildHierarchy(tr);

                string lowerName =
                    name.ToLowerInvariant();

                string lowerPath =
                    path.ToLowerInvariant();

                if (!lowerPath.Contains(
                        "/spawnedentitycontainer/playercharacter",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (lowerPath.Contains(
                        "/collisiondetectionslaves/",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (lowerName.StartsWith(
                        "playercharacter",
                        StringComparison.Ordinal))
                {
                    exactPlayerRoot =
                        tr;

                    break;
                }

                fallbackPlayerChild ??=
                    tr;
            }
        }
        catch
        {
            return null;
        }

        return exactPlayerRoot ??
               fallbackPlayerChild;
    }

    private static void Record(
        string kind,
        string detail)
    {
        int sequence;

        lock (Sync)
        {
            if (!_active)
                return;

            sequence =
                ++_sequence;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] SECRET BUNKER TRACE #{sequence:000} [{kind}] {detail}");
    }

    private static string SummarizeRequest(
        object request,
        IEnumerable<string> preferredNames)
    {
        try
        {
            foreach (string name in preferredNames)
            {
                object? value =
                    ReflectionUtil.ReadMember(
                        request,
                        name);

                value =
                    ReflectionUtil.UnwrapNullable(
                        value);

                if (value == null)
                    continue;

                string? identifier =
                    ReflectionUtil.ExtractIdentifier(
                        value);

                if (!string.IsNullOrWhiteSpace(identifier))
                    return $"{name}={identifier}";

                string raw =
                    value.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(raw))
                    return $"{name}={raw}";
            }
        }
        catch
        {
        }

        return $"requestType={request.GetType().Name}";
    }

    private static bool SafeActiveSelf(
        Transform tr)
    {
        try
        {
            return tr.gameObject.activeSelf;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeActiveInHierarchy(
        Transform tr)
    {
        try
        {
            return tr.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildHierarchy(
        Transform tr)
    {
        try
        {
            var names =
                new List<string>();

            Transform? current =
                tr;

            for (int i = 0;
                 i < 40 && current != null;
                 i++)
            {
                names.Add(
                    current.name ?? "<unnamed>");

                current =
                    current.parent;
            }

            names.Reverse();

            return string.Join(
                "/",
                names);
        }
        catch
        {
            return "<unavailable>";
        }
    }
}


internal static class CassetteSaveTransactionPatches
{
    private static readonly HashSet<string> ExtractionFailures = new(StringComparer.Ordinal);

    public static void SelectedSlotMutationPostfix(object[]? __args, MethodBase __originalMethod, object? __instance)
    {
        CassetteReceiptRandomization.SuspendGameplayReadinessForBoundary();
        GarageCartridgeAccess.ResetNativeBagObservations("selected save slot mutation");
        string methodIdentity = $"{__originalMethod?.DeclaringType?.Name ?? "<unknown>"}.{__originalMethod?.Name ?? "<unknown>"}(Int32)";
        if (__args == null)
        {
            LogExtractionFailureOnce(methodIdentity, "arguments were null");
            return;
        }
        if (__args.Length != 1)
        {
            LogExtractionFailureOnce(methodIdentity, $"expected one argument, received {__args.Length}");
            return;
        }
        if (__args[0] is not int slot)
        {
            LogExtractionFailureOnce(methodIdentity, $"slot argument was not boxed Int32 (actual={__args[0]?.GetType().Name ?? "<null>"})");
            return;
        }
        CassetteSaveBoundarySignalKind kind = string.Equals(
            __originalMethod?.Name, "CreateNewPlayerSaveFileInEmptySlot", StringComparison.Ordinal)
            ? CassetteSaveBoundarySignalKind.Creation
            : CassetteSaveBoundarySignalKind.Selection;
        CassetteReceiptRandomization.QueueSaveBoundarySignal(slot, kind, __instance);
    }

    public static void BuiltPlayerSaveStatePostfix(
        object[]? __args,
        MethodBase __originalMethod,
        object? __instance)
    {
        CassetteReceiptRandomization.SuspendGameplayReadinessForBoundary();
        GarageCartridgeAccess.ResetNativeBagObservations("player save state rebuilt");
        const string requestIdentity = "BuildPlayerSaveStateFromFileRequest";
        string methodIdentity = $"{__originalMethod?.DeclaringType?.Name ?? "SaveDataRequestProcessor"}.{__originalMethod?.Name ?? "ProcessRequest"}({requestIdentity})";
        object? request = ReflectionUtil.FindArg(__args, "BuildPlayerSaveStateFromFileRequest");
        if (request == null)
        {
            LogExtractionFailureOnce(methodIdentity, $"{requestIdentity} argument was missing");
            return;
        }
        int? slot = ReflectionUtil.ReadInt(request, "SlotNumber");
        if (!slot.HasValue)
        {
            LogExtractionFailureOnce(methodIdentity, $"{requestIdentity}.SlotNumber was unreadable");
            return;
        }
        CassetteReceiptRandomization.QueueSaveBoundarySignal(slot.Value, CassetteSaveBoundarySignalKind.Build, __instance);
    }

    public static void MostRecentSelectionPrefix(object? __instance)
    {
        GarageCartridgeAccess.ResetNativeBagObservations("most recent save selection");
        CassetteReceiptRandomization.BeginMostRecentSelectionBoundary(__instance);
    }

    public static void PlayerSaveWriteCompletedEventPostfix(object[]? __args)
    {
        object? nativeEvent = ReflectionUtil.FindArg(__args, "PlayerSaveWriteCompletedEvent");
        CassetteReceiptRandomization.ObservePersistenceAcceptanceWriteCompletedEvent(nativeEvent);
    }

    private static void LogExtractionFailureOnce(string identity, string reason)
    {
        string key = $"{identity}|{reason}";
        lock (ExtractionFailures)
        {
            if (!ExtractionFailures.Add(key)) return;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE BOUNDARY ARGUMENT REJECTED target='{identity}' reason='{reason}'; gameplay readiness was suspended before extraction and the callback failed closed.");
    }
}


internal static class GamePatches
{
    private static readonly Dictionary<string, PendingResult> Pending = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PersistedThisSession = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> RepeatedPersistedLogs = new(StringComparer.OrdinalIgnoreCase);
    private const int PersistedEventLogCapacity = 128;

    public static void PersistResultPrefix(object[]? __args) =>
        ApStars.BeforePersistResult(ReflectionUtil.FindArg(__args, "PersistLevelResultRequest"));

    public static void PersistResultPostfix(object[]? __args)
    {
        object? request = ReflectionUtil.FindArg(__args, "PersistLevelResultRequest");
        if (request == null) return;

        bool? success = ReflectionUtil.ReadBool(request, "DidPlayersSucceed");
        bool? shouldSave = ReflectionUtil.ReadBool(request, "ShouldSaveScore");

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] RESULT REQUEST success={success?.ToString() ?? "?"} saveScore={shouldSave?.ToString() ?? "?"}");
    }

    public static void SetScoredSongRequestPostfix(object[]? __args)
    {
        object? request = ReflectionUtil.FindArg(__args, "SetScoredSongInCurrentLevelRequest");
        if (request == null) return;

        MusicLabDiscovery.RecordScoredSongRequest(request);
    }

    public static void SelectedSaveChangedEventPostfix()
    {
        // Intentionally empty: patching SelectedPlayerSaveSlotChangedEvent.HandleEvent
        // is prohibited because its IL2CPP payload previously crashed startup.
    }

    public static void ApplyResultPrefix(object? __instance, object[]? __args)
    {
        ApStars.BeforeApplyResult(ReflectionUtil.FindArg(__args, "ApplyLevelResultToSaveDataRequest"));
        CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(
            __instance,
            reconcileNow: false);
    }

    public static bool LevelCassetteEvaluationPrefix(object[]? __args)
    {
        object? rawSucceeded = ReflectionUtil.FindArg(__args, "Boolean");
        bool? succeeded = rawSucceeded is bool value ? value : null;
        return CassetteSourceRandomization.AllowLevelCassetteEvaluation(succeeded);
    }

    public static bool CassetteStatusRequestPrefix(
        object? __instance,
        object[]? __args)
    {
        object? request = ReflectionUtil.FindArg(__args, "RecordSongCassetteStatusInSaveDataRequest");
        if (request == null)
            return true;

        string? song = (ReflectionUtil.ReadMember(request, "Song") ??
                        ReflectionUtil.ReadMember(request, "_Song_k__BackingField"))?.ToString();
        string? status = (ReflectionUtil.ReadMember(request, "CassetteStatus") ??
                          ReflectionUtil.ReadMember(request, "_CassetteStatus_k__BackingField"))?.ToString();
        return CassetteSourceRandomization.AllowCassetteStatusRequest(
            song, status, CassetteReceiptRandomization.IsApplyingNativeGrant);
    }

    public static void ApplyResultPostfix(object[]? __args)
    {
        object? request = ReflectionUtil.FindArg(__args, "ApplyLevelResultToSaveDataRequest");
        if (request == null) return;

        string level = ReflectionUtil.ExtractIdentifier(
            ReflectionUtil.ReadMember(request, "LevelIdentifier")) ?? "<unknown>";
        string variant = ReflectionUtil.ExtractIdentifier(
            ReflectionUtil.ReadMember(request, "LevelVariantIdentifier")) ?? "<none>";
        int? players = ReflectionUtil.ReadInt(request, "NumPlayers");
        int? score = ReflectionUtil.ReadInt(request, "ScoreObtained");
        string difficulty = ReflectionUtil.ReadMember(request, "DifficultyMix")?.ToString() ?? "<unknown>";

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] RESULT DATA internal={level} variant={variant} score={score?.ToString() ?? "?"} players={players?.ToString() ?? "?"} difficulty={difficulty}");

        SpecialModeDiscovery.RecordResultApplied(request, level, variant, score, difficulty);
        QuestActionDiscovery.RecordResult(level, variant, persisted: false);
        MusicLabDiscovery.RecordResultRequest(request, level, variant, score, difficulty);
        int? starsEarned = MusicLabDiscovery.ProbeNormalLevelStarRating(level, variant, score);
        Pending[level] = new PendingResult(level, variant, players, score, difficulty, starsEarned, ExpandedChecks.CaptureResultContext());

        Level6Discovery.RecordLevelResultApplied(level);
        Level8Discovery.RecordLevelResultApplied(level);
        PlantPipesRandomization.OnLevelResultApplied(level);
        CassetteReceiptRandomization.OnLifecyclePoint("level result applied");
    }

    public static bool RootsPostLevelOneSequencePrefix(MethodBase __originalMethod, object? __instance) =>
        RootsPostLevelOnePresentation.BeforeSequence(
            AreaAccessPrototype.Enabled, IntroHubSkip.Compatible, __originalMethod, __instance,
            message => Plugin.LoggerInstance?.LogWarning(message));

    public static void ResultPersistedEventPostfix(object[]? __args)
    {
        object? evt = ReflectionUtil.FindArg(__args, "LevelResultWasPersistedEvent");
        if (evt == null) return;
        ApStars.OnResultPersisted(evt);

        string level = ReflectionUtil.ExtractIdentifier(
            ReflectionUtil.ReadMember(evt, "Level")) ?? "<unknown>";

        Pending.TryGetValue(level, out PendingResult? result);

        string variant =
            result?.Variant
            ?? ReflectionUtil.ExtractIdentifier(
                ReflectionUtil.ReadMember(evt, "LevelVariant"))
            ?? "<none>";

        string persistedKey =
            $"{level}|{variant}";

        bool repeatedPersistedKey = ShouldLogRepeatedPersistedKey(persistedKey);
        if (repeatedPersistedKey)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] Repeated persisted event retained for result evaluation: {level} variant={variant}.");
        }

        MusicLabDiscovery.RecordPersistedEvent(evt, level, variant, result?.Score);
        SpecialModeDiscovery.RecordResultPersisted(evt, level, variant, result?.Score, result?.Difficulty ?? "<unknown>");
        QuestActionDiscovery.RecordResult(level, variant, persisted: true);
        ExpandedChecks.ObservePersistedResult(level, variant, result?.ExpandedContext);

        Level6Discovery.RecordLevelPersisted(level);
        Level8Discovery.RecordLevelPersisted(level);
        PlantPipesRandomization.OnLevelResultPersisted(level);
        CassetteReceiptRandomization.OnLifecyclePoint("level result persisted");
        RootsPostLevelOnePresentation.RecordLevelPersisted(level);
        BottomHudDiagnostic.OnLevelResultPersisted(level);

        if (result != null)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] COMPLETION internal={result.Level} variant={result.Variant} score={result.Score?.ToString() ?? "?"} difficulty={result.Difficulty}");
        }
        else
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] COMPLETION internal={level} variant={variant}");
        }

        bool explicitlyNonDefaultVariant =
            !string.IsNullOrWhiteSpace(variant) &&
            !string.Equals(
                variant,
                "<none>",
                StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(
                variant,
                "LevelVariant_Default",
                StringComparison.OrdinalIgnoreCase);

        if (explicitlyNonDefaultVariant)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] NON-DEFAULT VARIANT COMPLETION internal={level} variant={variant}: base-level AP location check suppressed.");
        }

        try
        {
            SaveDataProbe.ProbePersistedLevel(evt, level);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SAVE PROBE failed for {level}: {ex.GetBaseException().Message}");
        }

        if (CampaignLevelCatalog.TryGet(level, out _))
        {
            Plugin.AP?.QueueCampaignResult(level, variant, result?.StarsEarned);
        }
        else if (!explicitlyNonDefaultVariant)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] Unmapped internal level '{level}'. It will not be sent to Archipelago yet.");
        }
    }

    private static bool ShouldLogRepeatedPersistedKey(string persistedKey)
    {
        lock (PersistedThisSession)
        {
            if (!PersistedThisSession.Contains(persistedKey))
            {
                if (PersistedThisSession.Count < PersistedEventLogCapacity)
                    PersistedThisSession.Add(persistedKey);
                return false;
            }

            return RepeatedPersistedLogs.Count < PersistedEventLogCapacity &&
                   RepeatedPersistedLogs.Add(persistedKey);
        }
    }

    private sealed record PendingResult(
        string Level,
        string Variant,
        int? Players,
        int? Score,
        string Difficulty,
        int? StarsEarned,
        string? ExpandedContext);
}















internal static class QuestActionDiscovery
{
    private static readonly QuestActionDiagnosticPolicy Policy = new();

    public static void RecordProgressionRequest(object request, string flag)
    {
        Log(Policy.Observe("request", DeveloperHarness.CurrentRoomId, flag, null,
            ReflectionUtil.ReadBool(request, "Value") ?? ReflectionUtil.ReadBool(request, "_Value_k__BackingField")));
    }

    public static void RecordProgressionFlagUpdated(object evt, string flag)
    {
        Log(Policy.Observe("event", DeveloperHarness.CurrentRoomId, flag,
            ReflectionUtil.ReadBool(evt, "FlagWasSet") ?? ReflectionUtil.ReadBool(evt, "_FlagWasSet_k__BackingField"),
            ReflectionUtil.ReadBool(evt, "FlagIsSet") ?? ReflectionUtil.ReadBool(evt, "_FlagIsSet_k__BackingField")));
    }

    public static void RecordResult(string level, string variant, bool persisted)
    {
        Log(Policy.Observe(persisted ? "result-persisted" : "result-applied",
            DeveloperHarness.CurrentRoomId, level + "|" + variant));
    }

    public static void RecordSceneObject(string path, string componentTypes, bool activeSelf, bool activeInHierarchy)
    {
        Log(Policy.ObserveScene(DeveloperHarness.CurrentRoomId, path, componentTypes, activeSelf, activeInHierarchy));
    }

    public static void SnapshotKnownFlags()
    {
        string room = DeveloperHarness.CurrentRoomId;
        foreach (string flag in Policy.BeginSnapshot(room))
        {
            bool readable = RootsBucketRandomization.TryReadProgressionFlag(flag, out bool value);
            Log(Policy.Observe("snapshot", room, flag, null, readable ? value : null));
        }
    }

    private static void Log(string? message)
    {
        if (message != null)
            Plugin.LoggerInstance?.LogInfo(message);
    }
}

internal static class SpecialModeDiscovery
{
    private const int ProgressionFlagCapacity = 256;
    private const int SceneObjectCapacity = 100;

    private static readonly object Sync = new();
    private static readonly HashSet<string> ObservedProgressionFlags =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool _progressionCapacityLogged;

    public static void RecordProgressionFlagUpdated(object evt, string flag)
    {
        if (!SpecialVariantDiagnosticPolicy.IsRelevantProgressionFlag(flag))
            return;

        string room = DeveloperHarness.CurrentRoomId;
        string key = $"{room}\0{flag}";
        bool capacityReached = false;

        lock (Sync)
        {
            if (ObservedProgressionFlags.Contains(key))
                return;

            if (ObservedProgressionFlags.Count >= ProgressionFlagCapacity)
            {
                if (_progressionCapacityLogged)
                    return;

                _progressionCapacityLogged = true;
                capacityReached = true;
            }
            else
            {
                ObservedProgressionFlags.Add(key);
            }
        }

        if (capacityReached)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC FLAG UPDATED readOnly=True room='{room}' " +
                $"flag='<capacity reached>' eventType='{evt.GetType().FullName}' " +
                $"emitted={ProgressionFlagCapacity} truncated=1.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC FLAG UPDATED readOnly=True room='{room}' " +
            $"flag='{flag}' eventType='{evt.GetType().FullName}' emitted=1 truncated=0.");
    }

    public static void RecordResultApplied(
        object request,
        string level,
        string variant,
        int? score,
        string difficulty)
    {
        SpecialVariantKind identity =
            SpecialVariantDiagnosticPolicy.ClassifyVariant(level, variant);
        if (identity == SpecialVariantKind.None)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC RESULT APPLIED readOnly=True " +
            $"room='{DeveloperHarness.CurrentRoomId}' internal='{level}' variant='{variant}' " +
            $"score={score?.ToString() ?? "?"} difficulty='{difficulty}' identity={identity} " +
            $"eventType='{request.GetType().FullName}' emitted=1 truncated=0.");
    }

    public static void RecordResultPersisted(
        object evt,
        string level,
        string variant,
        int? score,
        string difficulty)
    {
        SpecialVariantKind identity =
            SpecialVariantDiagnosticPolicy.ClassifyVariant(level, variant);
        if (identity == SpecialVariantKind.None)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC RESULT PERSISTED readOnly=True " +
            $"room='{DeveloperHarness.CurrentRoomId}' internal='{level}' variant='{variant}' " +
            $"score={score?.ToString() ?? "?"} difficulty='{difficulty}' identity={identity} " +
            $"eventType='{evt.GetType().FullName}' emitted=1 truncated=0.");
    }

    public static void ScanCurrentScene()
    {
        string room = DeveloperHarness.CurrentRoomId;
        if (string.Equals(room, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(room, "GameRoom_27", StringComparison.OrdinalIgnoreCase))
            return;

        int emitted = 0;
        int truncated = 0;
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC SCENE SCAN BEGIN readOnly=True " +
            $"room='{room}' emitted=0 truncated=0.");

        try
        {
            foreach (Transform transform in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (transform == null || transform.gameObject == null)
                    continue;

                GameObject gameObject = transform.gameObject;
                bool activeSelf;
                bool activeInHierarchy;
                try
                {
                    activeSelf = gameObject.activeSelf;
                    activeInHierarchy = gameObject.activeInHierarchy;
                }
                catch
                {
                    continue;
                }

                string path = BuildHierarchy(transform);
                string[] componentTypes = ReadComponentTypes(gameObject);
                QuestActionDiscovery.RecordSceneObject(path, string.Join("|", componentTypes), activeSelf, activeInHierarchy);
                if (!activeInHierarchy)
                    continue;
                if (!SpecialVariantDiagnosticPolicy.IsRelevantSceneObject(path, componentTypes))
                    continue;

                if (emitted >= SceneObjectCapacity)
                {
                    truncated++;
                    continue;
                }

                emitted++;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC SCENE OBJECT readOnly=True " +
                    $"room='{room}' path='{path}' activeSelf={activeSelf} " +
                    $"activeInHierarchy={activeInHierarchy} " +
                    $"componentTypes='{string.Join("|", componentTypes)}'.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC SCENE SCAN ERROR readOnly=True " +
                $"room='{room}' error='{ex.GetBaseException().Message}'.");
        }
        finally
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SPECIAL MODE DIAGNOSTIC SCENE SCAN END readOnly=True " +
                $"room='{room}' emitted={emitted} truncated={truncated}.");
        }
    }

    private static string[] ReadComponentTypes(GameObject gameObject)
    {
        try
        {
            return gameObject.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().FullName ?? component.GetType().Name)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static string BuildHierarchy(Transform transform)
    {
        try
        {
            var names = new List<string>();
            Transform? current = transform;
            while (current != null)
            {
                names.Add(current.name);
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return "<unavailable>";
        }
    }
}


internal static class DeveloperHarness
{
    private const string HubRoomId = "GameRoom_Hub2";
    private const string Level4RoomId = "GameRoom_08";

    private static readonly object Sync = new();

    private static object? _gameFlowProcessor;
    private static MethodInfo? _gameFlowProcessRequest;
    private static Type? _transitionRequestType;
    private static Type? _roomIdentifierType;

    private static object? _capturedTransitionType;
    private static Type? _transitionTypeType;

    private static Type? _spawnPointType;
    private static object? _capturedSpawnPoint;

    private static string _currentRoomId = "";

    public static bool Enabled { get; set; }

    public static string CurrentRoomId
    {
        get
        {
            lock (Sync)
                return _currentRoomId;
        }
    }

    public static bool IsCurrentRoom(string roomId)
    {
        lock (Sync)
        {
            return string.Equals(
                _currentRoomId,
                roomId,
                StringComparison.Ordinal);
        }
    }

    public static bool IsHub2Current =>
        IsCurrentRoom(HubRoomId);

    public static void SetCurrentRoomForRedirect(string roomId)
    {
        lock (Sync)
            _currentRoomId = roomId;
    }

    public static void CaptureGameFlow(object? processor, object request)
    {
        // Capture is required by the production Level 4 entrance proxy as well
        // as the developer hotkeys, so it must remain active even when the
        // developer harness is disabled.
        if (processor == null)
            return;

        if (request.GetType().Name != "TransitionToGameRoomRequest")
            return;

        MethodInfo? process = processor.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "ProcessRequest")
                    return false;

                ParameterInfo[] ps;
                try { ps = m.GetParameters(); }
                catch { return false; }

                return ps.Length == 1 && ps[0].ParameterType == request.GetType();
            });

        if (process == null)
            return;

        object? room =
            ReflectionUtil.ReadMember(request, "RoomToGoTo")
            ?? ReflectionUtil.ReadMember(request, "_RoomToGoTo_k__BackingField");

        object? transitionType =
            ReflectionUtil.ReadMember(request, "TransitionType")
            ?? ReflectionUtil.ReadMember(request, "_TransitionType_k__BackingField");

        object? spawnPoint =
            ReflectionUtil.ReadMember(request, "SpecificSpawnPoint")
            ?? ReflectionUtil.ReadMember(request, "_SpecificSpawnPoint_k__BackingField");

        object? partOfLoadValue =
            ReflectionUtil.ReadMember(request, "PartOfLoadLevel")
            ?? ReflectionUtil.ReadMember(request, "_PartOfLoadLevel_k__BackingField");

        string observedRoom =
            room == null
                ? "<null>"
                : ReflectionUtil.ExtractIdentifier(room)
                  ?? SafeSimpleValue(room);

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] GAME TRANSITION OBSERVED: room={observedRoom} " +
            $"transitionType={SafeSimpleValue(transitionType)} " +
            $"spawnPoint={SafeSimpleValue(spawnPoint)} " +
            $"partOfLoadLevel={SafeSimpleValue(partOfLoadValue)}.");

        GarageRoomInitialization.State.Transition(observedRoom);
        PhoneBoothDiagnostic.RecordObservedTransition(
            observedRoom,
            SafeSimpleValue(transitionType),
            SafeSimpleValue(spawnPoint),
            SafeSimpleValue(partOfLoadValue));

        Level6Discovery.RecordTransition(observedRoom);
        Level8Discovery.RecordTransition(observedRoom);
        MusicLabDiscovery.RecordTransition(observedRoom);

        lock (Sync)
        {
            _currentRoomId = observedRoom;
        }
        BottomHudDiagnostic.OnRoomTransition(observedRoom);
        CassetteReceiptRandomization.OnLifecyclePoint(
            $"room transition to {observedRoom}");

        BunkerStarRequirementKeeper.ResetForSceneChange();

        bool firstCapture;

        lock (Sync)
        {
            firstCapture = _gameFlowProcessor == null;

            _gameFlowProcessor = processor;
            _gameFlowProcessRequest = process;
            _transitionRequestType = request.GetType();

            if (room != null)
                _roomIdentifierType = room.GetType();

            if (transitionType != null)
            {
                _capturedTransitionType = transitionType;
                _transitionTypeType = transitionType.GetType();
            }

            if (spawnPoint != null)
            {
                _capturedSpawnPoint = spawnPoint;
                _spawnPointType = spawnPoint.GetType();
            }
        }

        if (firstCapture)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] DEV HARNESS captured {processor.GetType().Name}.ProcessRequest(TransitionToGameRoomRequest).");

            LogTransitionConstructors(request.GetType());

            if (spawnPoint != null)
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] DEV HARNESS captured startup SpawnPointKey value={SafeSimpleValue(spawnPoint)}.");
            }

        }
    }

    public static void CycleMusicLabPointOverride()
    {
        if (!Enabled)
            return;

        MusicLabPointOverride.CycleToNextRewardThreshold();
    }

    public static void StartPostAct1Discovery()
    {
        if (!Enabled)
            return;

        Level8Discovery.ManualBegin();
        MusicLabDiscovery.ManualBegin();
    }

    public static void ScanPostAct1RouteObjects()
    {
        if (!Enabled)
            return;

        if (MusicLabDiscovery.IsMusicLabRoom(DeveloperHarness.CurrentRoomId))
            MusicLabDiscovery.ScanCurrentScene();
        else
            Level8Discovery.ScanCurrentScene();
    }

    public static void RetryBunkerStarRequirementOnce()
    {
        if (!Enabled)
            return;

        BunkerStarRequirementKeeper.RequestManualRetry();
    }

    private static string SafeSimpleValue(object? value)
    {
        if (value == null)
            return "<null>";

        try
        {
            Type t = value.GetType();

            if (value is string || t.IsPrimitive || t.IsEnum)
            {
                string raw = value.ToString() ?? "<unknown>";
                return raw.Length > 120 ? raw.Substring(0, 120) : raw;
            }

            object? id =
                ReflectionUtil.ReadMember(value, "id")
                ?? ReflectionUtil.ReadMember(value, "ID")
                ?? ReflectionUtil.ReadMember(value, "Key")
                ?? ReflectionUtil.ReadMember(value, "key");

            if (id != null)
                return $"{t.Name}:{id}";

            return t.Name;
        }
        catch
        {
            return "<unprintable>";
        }
    }

    private static object? BuildTransitionRequest(
        Type requestType,
        object hubId,
        object? transitionType,
        Type? transitionTypeType,
        Type? spawnPointType,
        object? capturedSpawnPoint)
    {
        ConstructorInfo[] ctors;

        try
        {
            ctors = requestType.GetConstructors(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch
        {
            return null;
        }

        foreach (ConstructorInfo ctor in ctors
                     .OrderByDescending(c => c.GetParameters().Length))
        {
            ParameterInfo[] ps;

            try { ps = ctor.GetParameters(); }
            catch { continue; }

            // IntPtr-only constructors are wrapper constructors, not native request factories.
            if (ps.Length == 1 && ps[0].ParameterType == typeof(IntPtr))
                continue;

            var args = new object?[ps.Length];
            bool usable = true;

            for (int i = 0; i < ps.Length; i++)
            {
                Type pt = ps[i].ParameterType;
                string pn = ps[i].Name ?? "";

                if (pt.IsInstanceOfType(hubId) ||
                    pt.Name == "GameRoomIdentifier")
                {
                    args[i] = hubId;
                }
                else if (transitionType != null &&
                         (pt.IsInstanceOfType(transitionType) ||
                          (transitionTypeType != null && pt == transitionTypeType)))
                {
                    args[i] = transitionType;
                }
                else if (spawnPointType != null && pt == spawnPointType)
                {
                    args[i] =
                        capturedSpawnPoint != null && pt.IsInstanceOfType(capturedSpawnPoint)
                            ? capturedSpawnPoint
                            : TryCreateDefault(pt);
                }
                else if (pt == typeof(bool))
                {
                    args[i] = false;
                }
                else if (pt.IsEnum)
                {
                    args[i] = TryGetEnumValue(pt, "FADE_LOAD") ?? Enum.GetValues(pt).GetValue(0);
                }
                else if (pt == typeof(string))
                {
                    args[i] = "";
                }
                else if (!pt.IsValueType)
                {
                    args[i] = null;
                }
                else
                {
                    object? def = TryCreateDefault(pt);
                    if (def == null)
                    {
                        usable = false;
                        break;
                    }

                    args[i] = def;
                }
            }

            if (!usable)
                continue;

            try
            {
                object? built = ctor.Invoke(args!);
                if (built != null)
                {
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] DEV HARNESS constructed TransitionToGameRoomRequest using ctor({string.Join(", ", ps.Select(p => p.ParameterType.Name))}).");
                    return built;
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] DEV HARNESS ctor failed ({string.Join(", ", ps.Select(p => p.ParameterType.Name))}): {ex.GetBaseException().Message}");
            }
        }

        return null;
    }

    private static object? TryCreateDefault(Type type)
    {
        try
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);

            ConstructorInfo? empty = type.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                Type.EmptyTypes,
                modifiers: null);

            return empty?.Invoke(Array.Empty<object>());
        }
        catch
        {
            return null;
        }
    }

    private static object? TryGetEnumValue(Type enumType, string name)
    {
        try
        {
            if (!enumType.IsEnum)
                return null;

            foreach (string value in Enum.GetNames(enumType))
            {
                if (string.Equals(value, name, StringComparison.OrdinalIgnoreCase))
                    return Enum.Parse(enumType, value);
            }
        }
        catch { }

        return null;
    }

    private static object? BuildIdentifier(Type type, string value)
    {
        foreach (ConstructorInfo ctor in type.GetConstructors(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            ParameterInfo[] ps;
            try { ps = ctor.GetParameters(); }
            catch { continue; }

            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                try { return ctor.Invoke(new object[] { value }); }
                catch { }
            }
        }

        foreach (MethodInfo m in type.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            ParameterInfo[] ps;
            try { ps = m.GetParameters(); }
            catch { continue; }

            if (m.ReturnType == type &&
                ps.Length == 1 &&
                ps[0].ParameterType == typeof(string))
            {
                try { return m.Invoke(null, new object[] { value }); }
                catch { }
            }
        }

        return null;
    }

    private static bool WriteMember(object obj, string name, object value)
    {
        Type t = obj.GetType();

        PropertyInfo? p = t.GetProperty(
            name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (p != null && p.CanWrite)
        {
            try
            {
                p.SetValue(obj, value);
                return true;
            }
            catch { }
        }

        FieldInfo? f = t.GetField(
            name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (f != null)
        {
            try
            {
                if (f.FieldType.IsInstanceOfType(value) ||
                    (f.FieldType == typeof(bool) && value is bool))
                {
                    f.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
        }

        return false;
    }

    private static void LogTransitionConstructors(Type requestType)
    {
        try
        {
            foreach (ConstructorInfo ctor in requestType.GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                string signature = string.Join(
                    ", ",
                    ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));

                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] DEV HARNESS TransitionToGameRoomRequest ctor({signature})");
            }
        }
        catch { }
    }
}

internal static class MusicLabPointOverride
{
    private static readonly int[] RewardThresholds = { 5, 10, 20, 32, 46, 64, 89, 111, 140 };
    private static MethodInfo? _nativeGetter;
    private static int? _overrideScore;
    private static int _lastNativeScore = -1;

    public static int? OverrideScore => _overrideScore;

    public static void BindNativeGetter(MethodInfo method)
    {
        _nativeGetter = method;
    }

    public static void RecordNativeScore(int score)
    {
        _lastNativeScore = score;
    }

    public static void CycleToNextRewardThreshold()
    {
        if (MusicLabPointRandomization.Snapshot.Mode != MusicLabPointRuntimeMode.Native)
            return;
        int native = ReadNativeScore();
        int current = Math.Max(native, _overrideScore ?? native);

        if (_overrideScore.HasValue && _overrideScore.Value >= RewardThresholds[^1])
        {
            int previous = _overrideScore.Value;
            _overrideScore = null;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB POINT CHEAT OFF: previousEffective={previous} native={native}. GetMedalScore() is back to the real saved medal total.");
            return;
        }

        int next = RewardThresholds.FirstOrDefault(t => t > current);
        if (next <= 0)
        {
            _overrideScore = null;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB POINT CHEAT NOT NEEDED: native={native} already meets/exceeds the final 140-point threshold. Override is OFF.");
            return;
        }

        _overrideScore = next;
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB POINT CHEAT ENABLED: native={native} effective={next}. This is a runtime GetMedalScore() override only; saved clean medals and the real point total are unchanged. Press Shift+F4 again for the next threshold.");
    }

    private static int ReadNativeScore()
    {
        MethodInfo? getter = _nativeGetter;
        if (getter == null)
            return _lastNativeScore >= 0 ? _lastNativeScore : 0;

        bool previousSuppress = MusicLabPointOverridePatches.SuppressOverride;
        try
        {
            MusicLabPointOverridePatches.SuppressOverride = true;
            object? value = getter.Invoke(null, null);
            if (value is int score)
            {
                _lastNativeScore = score;
                return score;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB POINT CHEAT native score read failed: {ex.GetBaseException().Message}");
        }
        finally
        {
            MusicLabPointOverridePatches.SuppressOverride = previousSuppress;
        }

        return _lastNativeScore >= 0 ? _lastNativeScore : 0;
    }
}

internal static class MusicLabPointOverridePatches
{
    public static bool SuppressOverride { get; set; }

    public static void GetMedalScorePostfix(ref int __result)
    {
        MusicLabPointOverride.RecordNativeScore(__result);
        if (SuppressOverride)
            return;

        int nativeScore = __result;
        int? developerScore = DeveloperHarness.Enabled
            ? MusicLabPointOverride.OverrideScore
            : null;
        __result = DeveloperHarness.CurrentRoomId == "GameRoom_Hub6"
            ? MusicLabPointRandomization.ResolveEffectiveScore(nativeScore, developerScore)
            : developerScore ?? nativeScore;
    }
}

internal static class MusicLabPointDiagnostic
{
    private static readonly string[] MemberKeywords =
    {
        "Medal", "MusicLab", "Music Lab", "Hub06", "Hub6", "Reward", "Point"
    };

    private static readonly string[] TypeKeywords =
    {
        "MedalScore", "MusicLab", "Hub06", "Hub6", "RewardChest", "CleanMedal"
    };

    public static void Probe()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] MUSIC LAB POINT PROBE failed: Assembly-CSharp is unavailable.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] MUSIC LAB POINT PROBE BEGIN: read-only scan for the native total that drives Hub6 reward thresholds 5/10/20/32/46/64/89/111/140, plus any save/request path that can safely change it later.");

        List<Type> types = ReflectionUtil.SafeGetTypes(asm).ToList();

        Type? currentSaveEnquiries = types.FirstOrDefault(t =>
            string.Equals(t.Name, "CurrentPlayerSaveEnquiries", StringComparison.Ordinal));

        if (currentSaveEnquiries == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] MUSIC LAB POINT PROBE: CurrentPlayerSaveEnquiries type not found.");
        }
        else
        {
            DumpRelevantMembers(currentSaveEnquiries, null, staticMembers: true, "CurrentPlayerSaveEnquiries");

            object? save = TryGetSelectedSave(currentSaveEnquiries);
            if (save == null)
            {
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] MUSIC LAB POINT PROBE: selected player save was unavailable.");
            }
            else
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE SELECTED SAVE type='{save.GetType().FullName}'.");
                DumpRelevantMembers(save.GetType(), save, staticMembers: false, "SelectedSave");
            }
        }

        foreach (string exactTypeName in new[]
                 {
                     "LevelScoringEnquiries",
                     "GameProgressionEnquiries",
                     "Hub06MedalScoreRewardChest"
                 })
        {
            Type? exact = types.FirstOrDefault(t =>
                string.Equals(t.Name, exactTypeName, StringComparison.Ordinal));
            if (exact == null)
                continue;

            bool staticMembers = exactTypeName.EndsWith("Enquiries", StringComparison.Ordinal);
            DumpRelevantMembers(exact, null, staticMembers, exactTypeName);
        }

        int relatedTypes = 0;
        foreach (Type t in types
                     .Where(IsRelevantType)
                     .OrderBy(t => t.FullName)
                     .Take(120))
        {
            relatedTypes++;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB POINT PROBE TYPE name='{t.FullName}'.");

            if (t.Name.Contains("Request", StringComparison.OrdinalIgnoreCase))
                DumpConstructors(t);
        }

        int processorOverloads = 0;
        foreach (Type processorType in types.Where(t =>
                     string.Equals(t.Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal)))
        {
            foreach (MethodInfo m in SafeMethods(
                         processorType,
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!string.Equals(m.Name, "ProcessRequest", StringComparison.Ordinal))
                    continue;

                ParameterInfo[] ps;
                try { ps = m.GetParameters(); }
                catch { continue; }

                if (ps.Length != 1 || !IsRelevantName(ps[0].ParameterType.Name))
                    continue;

                processorOverloads++;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE SAVE REQUEST PROCESSOR overload='ProcessRequest({ps[0].ParameterType.FullName})'.");
            }
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB POINT PROBE COMPLETE: relatedTypes={relatedTypes} relevantSaveProcessorOverloads={processorOverloads}. Send this log before we add a write/set-points hotkey.");
    }

    private static bool IsRelevantType(Type t)
    {
        string name = t.Name ?? string.Empty;
        return TypeKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)) &&
               (name.Contains("Request", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Save", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Enquir", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Progress", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Reward", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Medal", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("Score", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsRelevantName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;
        return MemberKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase)) ||
               TypeKeywords.Any(k => name.Contains(k, StringComparison.OrdinalIgnoreCase));
    }

    private static void DumpRelevantMembers(
        Type type,
        object? target,
        bool staticMembers,
        string label)
    {
        BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic |
                             (staticMembers ? BindingFlags.Static : BindingFlags.Instance);

        foreach (MethodInfo m in SafeMethods(type, flags)
                     .Where(m => IsRelevantName(m.Name))
                     .OrderBy(m => m.Name)
                     .Take(120))
        {
            ParameterInfo[] ps;
            try { ps = m.GetParameters(); }
            catch { continue; }

            string signature = string.Join(", ", ps.Select(p => p.ParameterType.Name));
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB POINT PROBE METHOD owner='{label}' name='{m.Name}' returns='{m.ReturnType.FullName}' params='({signature})'.");

            if (target == null && !m.IsStatic)
                continue;
            if (ps.Length != 0 || m.ReturnType == typeof(void) || m.ContainsGenericParameters)
                continue;
            if (!IsSafeSimpleReturn(m.ReturnType))
                continue;

            try
            {
                object? value = m.Invoke(target, null);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE VALUE owner='{label}' source='{m.Name}()' value='{SafeValue(value)}'.");
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE INVOKE SKIPPED owner='{label}' method='{m.Name}' reason='{ex.GetBaseException().Message}'.");
            }
        }

        foreach (PropertyInfo p in SafeProperties(type, flags)
                     .Where(p => IsRelevantName(p.Name))
                     .OrderBy(p => p.Name)
                     .Take(120))
        {
            if (p.GetIndexParameters().Length != 0)
                continue;

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB POINT PROBE PROPERTY owner='{label}' name='{p.Name}' type='{p.PropertyType.FullName}' canWrite={p.CanWrite}.");

            MethodInfo? getter = p.GetGetMethod(true);
            if (target == null && getter != null && !getter.IsStatic)
                continue;
            if (!p.CanRead || !IsSafeSimpleReturn(p.PropertyType))
                continue;

            try
            {
                object? value = p.GetValue(target);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE VALUE owner='{label}' source='property {p.Name}' value='{SafeValue(value)}'.");
            }
            catch { }
        }

        foreach (FieldInfo f in SafeFields(type, flags)
                     .Where(f => IsRelevantName(f.Name))
                     .OrderBy(f => f.Name)
                     .Take(120))
        {
            if (target == null && !f.IsStatic)
                continue;

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB POINT PROBE FIELD owner='{label}' name='{f.Name}' type='{f.FieldType.FullName}' writable={!f.IsInitOnly && !f.IsLiteral}.");

            if (!IsSafeSimpleReturn(f.FieldType))
                continue;

            try
            {
                object? value = f.GetValue(target);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE VALUE owner='{label}' source='field {f.Name}' value='{SafeValue(value)}'.");
            }
            catch { }
        }
    }

    private static object? TryGetSelectedSave(Type enquiriesType)
    {
        foreach (string methodName in new[]
                 {
                     "GetSelectedSlotSaveFileState",
                     "TryGetSelectedSlotSaveFileState"
                 })
        {
            MethodInfo? method = SafeMethods(
                    enquiriesType,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m =>
                    string.Equals(m.Name, methodName, StringComparison.Ordinal) &&
                    SafeParameterCount(m) == 0);

            if (method == null)
                continue;

            try
            {
                object? result = method.Invoke(null, null);
                if (result != null)
                {
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] MUSIC LAB POINT PROBE selected-save source='CurrentPlayerSaveEnquiries.{methodName}()'.");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE selected-save getter '{methodName}' failed: {ex.GetBaseException().Message}");
            }
        }

        return null;
    }

    private static void DumpConstructors(Type type)
    {
        try
        {
            foreach (ConstructorInfo ctor in type.GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     .Take(24))
            {
                ParameterInfo[] ps;
                try { ps = ctor.GetParameters(); }
                catch { continue; }

                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] MUSIC LAB POINT PROBE CTOR type='{type.FullName}' params='({string.Join(", ", ps.Select(p => p.ParameterType.FullName + " " + p.Name))})'.");
            }
        }
        catch { }
    }

    private static IEnumerable<MethodInfo> SafeMethods(Type type, BindingFlags flags)
    {
        try { return type.GetMethods(flags); }
        catch { return Array.Empty<MethodInfo>(); }
    }

    private static IEnumerable<PropertyInfo> SafeProperties(Type type, BindingFlags flags)
    {
        try { return type.GetProperties(flags); }
        catch { return Array.Empty<PropertyInfo>(); }
    }

    private static IEnumerable<FieldInfo> SafeFields(Type type, BindingFlags flags)
    {
        try { return type.GetFields(flags); }
        catch { return Array.Empty<FieldInfo>(); }
    }

    private static int SafeParameterCount(MethodInfo method)
    {
        try { return method.GetParameters().Length; }
        catch { return -1; }
    }

    private static bool IsSafeSimpleReturn(Type type)
    {
        Type? nullable = Nullable.GetUnderlyingType(type);
        if (nullable != null)
            type = nullable;

        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }

    private static string SafeValue(object? value)
    {
        if (value == null)
            return "<null>";

        try
        {
            object? unwrapped = ReflectionUtil.UnwrapNullable(value);
            return unwrapped?.ToString() ?? "<nullable-null>";
        }
        catch
        {
            return "<unprintable>";
        }
    }
}


internal static class PhoneBoothDiagnostic
{
    private const string NativeLibrary = "GameAssembly.dll";
    private static readonly object Sync = new();
    private static readonly string[] CandidateTokens =
    {
        "phone", "booth", "telephone", "fasttravel", "fast_travel", "fast travel", "travel", "teleport", "transition"
    };

    private static float _armedUntil;
    private static int _scanSequence;
    private static bool _typeScanDone;
    private static string _armedSourceRoom = "<unknown>";

    public static void ScanCurrentSceneAndArm()
    {
        int sequence;
        lock (Sync)
        {
            sequence = ++_scanSequence;
            _armedUntil = Time.realtimeSinceStartup + 30f;
            _armedSourceRoom = DeveloperHarness.CurrentRoomId;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== PHONE BOOTH SCAN #{sequence:00} BEGIN ===== currentRoom='{DeveloperHarness.CurrentRoomId}'. Next game-room transition is tagged for 30 seconds.");

        if (!_typeScanDone)
        {
            _typeScanDone = true;
            DumpRelevantGameTypes();
        }

        if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase))
            DumpHub6FocusedCandidates();
        else if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase))
            DumpRootsFocusedCandidates();
        else
            DumpSceneCandidates();

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== PHONE BOOTH SCAN #{sequence:00} END ===== Interact with a Phone Booth and choose any destination within 30 seconds.");
    }

    public static void RecordObservedTransition(
        string room,
        string transitionType,
        string spawnPoint,
        string partOfLoadLevel)
    {
        bool armed;
        lock (Sync)
            armed = Time.realtimeSinceStartup <= _armedUntil;

        if (!armed)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] PHONE BOOTH ARMED TRANSITION sourceRoom='{_armedSourceRoom}' room='{room}' transitionType='{transitionType}' spawnPoint='{spawnPoint}' partOfLoadLevel='{partOfLoadLevel}'.");

        lock (Sync)
            _armedUntil = 0f;
    }

    private static void DumpRelevantGameTypes()
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        if (gameAssembly == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] PHONE BOOTH TYPE SCAN skipped: Assembly-CSharp unavailable.");
            return;
        }

        int emittedTypes = 0;
        foreach (Type type in ReflectionUtil.SafeGetTypes(gameAssembly)
                     .Where(t => IsRelevantText(t.FullName ?? t.Name))
                     .OrderBy(t => t.FullName, StringComparer.OrdinalIgnoreCase))
        {
            if (emittedTypes++ >= 80)
            {
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] PHONE BOOTH TYPE SCAN capped at 80 matching types.");
                break;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] PHONE BOOTH TYPE type='{type.FullName}'.");

            int members = 0;
            try
            {
                foreach (FieldInfo field in type.GetFields(
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static))
                {
                    if (members++ >= 40) break;
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] PHONE BOOTH TYPE FIELD owner='{type.FullName}' name='{field.Name}' type='{field.FieldType.FullName}'.");
                }
            }
            catch { }

            try
            {
                foreach (PropertyInfo property in type.GetProperties(
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static))
                {
                    if (members++ >= 40) break;
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] PHONE BOOTH TYPE PROPERTY owner='{type.FullName}' name='{property.Name}' type='{property.PropertyType.FullName}'.");
                }
            }
            catch { }

            try
            {
                foreach (MethodInfo method in type.GetMethods(
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    if (members++ >= 40) break;
                    ParameterInfo[] ps;
                    try { ps = method.GetParameters(); }
                    catch { continue; }
                    string signature = string.Join(", ", ps.Select(p => $"{p.ParameterType.Name} {p.Name}"));
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] PHONE BOOTH TYPE METHOD owner='{type.FullName}' signature='{method.ReturnType.Name} {method.Name}({signature})'.");
                }
            }
            catch { }
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] PHONE BOOTH TYPE SCAN COMPLETE matches={Math.Min(emittedTypes, 80)}.");
    }

    private static void DumpHub6FocusedCandidates()
    {
        const string phoneRootPath = "Root/GameRoom_Hub6_Logic/Objects/Phones";
        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== HUB6 PHONE BANK FOCUSED SCAN BEGIN ===== root='Root/GameRoom_Hub6_Logic/Objects/Phones'.");

        var all = new List<Transform>();
        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null)
                    continue;

                string path = BuildPath(tr);
                if (path.Equals(phoneRootPath, StringComparison.Ordinal) ||
                    path.StartsWith(phoneRootPath + "/", StringComparison.Ordinal))
                    all.Add(tr);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] HUB6 PHONE BANK enumeration failed: {ex.GetBaseException().Message}");
            return;
        }

        foreach (Transform tr in all.OrderBy(t => BuildPath(t), StringComparer.OrdinalIgnoreCase))
        {
            string path = BuildPath(tr);
            string relative = path.Length <= phoneRootPath.Length
                ? string.Empty
                : path.Substring(phoneRootPath.Length + 1);
            int depth = string.IsNullOrEmpty(relative) ? 0 : relative.Count(c => c == '/') + 1;

            GameObject go;
            try { go = tr.gameObject; }
            catch { continue; }
            if (go == null)
                continue;

            bool directPhoneRoot = depth == 1;
            bool interesting = directPhoneRoot || HasNonVisualNativeComponent(go) || HasTargetBearingNativeComponent(go);
            if (!interesting)
                continue;

            bool active = false;
            try { active = go.activeInHierarchy; } catch { }
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] HUB6 PHONE BANK OBJECT depth={depth} path='{path}' active={active} directPhoneRoot={directPhoneRoot}.");
            DumpNativeComponents(go, path);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] HUB6 PHONE BANK FOCUSED SCAN COMPLETE matchedTransforms={all.Count}.");

        DumpHub6TargetBearingComponents();
        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== HUB6 PHONE BANK FOCUSED SCAN END =====");
    }

    private static bool HasNonVisualNativeComponent(GameObject go)
    {
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch { return false; }

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            IntPtr obj = GetNativePointer(component);
            if (obj == IntPtr.Zero)
                continue;

            IntPtr klass = IntPtr.Zero;
            try { klass = pb_il2cpp_object_get_class(obj); } catch { }
            string name = NativeClassFullName(klass);
            if (string.IsNullOrWhiteSpace(name))
                continue;

            if (name == "UnityEngine.Transform" ||
                name == "UnityEngine.RectTransform" ||
                name == "UnityEngine.MeshFilter" ||
                name == "UnityEngine.MeshRenderer" ||
                name == "UnityEngine.SkinnedMeshRenderer" ||
                name == "UnityEngine.Animator" ||
                name == "UnityEngine.BoxCollider" ||
                name == "UnityEngine.SphereCollider" ||
                name == "UnityEngine.CapsuleCollider" ||
                name == "UnityEngine.MeshCollider" ||
                name == "UnityEngine.AudioSource" ||
                name == "UnityEngine.Light")
                continue;

            return true;
        }

        return false;
    }

    private static bool HasTargetBearingNativeComponent(GameObject go)
    {
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch { return false; }

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            IntPtr obj = GetNativePointer(component);
            if (obj == IntPtr.Zero)
                continue;

            IntPtr klass = IntPtr.Zero;
            try { klass = pb_il2cpp_object_get_class(obj); } catch { }
            if (NativeClassHasTargetField(klass))
                return true;
        }

        return false;
    }

    private static bool NativeClassHasTargetField(IntPtr klass)
    {
        IntPtr current = klass;
        for (int depth = 0; current != IntPtr.Zero && depth < 7; depth++)
        {
            IntPtr iter = IntPtr.Zero;
            while (true)
            {
                IntPtr field;
                try { field = pb_il2cpp_class_get_fields(current, ref iter); }
                catch { field = IntPtr.Zero; }
                if (field == IntPtr.Zero)
                    break;

                string fieldName = NativeAnsi(() => pb_il2cpp_field_get_name(field));
                IntPtr fieldType = IntPtr.Zero;
                try { fieldType = pb_il2cpp_field_get_type(field); } catch { }
                string fieldTypeName = NativeTypeName(fieldType);
                string combined = (fieldName + " " + fieldTypeName).ToLowerInvariant();
                if (combined.Contains("roomtogo") ||
                    combined.Contains("gameroom") ||
                    combined.Contains("destination") ||
                    combined.Contains("specificspawn") ||
                    combined.Contains("spawnpoint"))
                    return true;
            }

            try { current = pb_il2cpp_class_get_parent(current); }
            catch { current = IntPtr.Zero; }
        }

        return false;
    }

    private static void DumpHub6TargetBearingComponents()
    {
        const string hub6LogicPrefix = "Root/GameRoom_Hub6_Logic/";
        int emitted = 0;
        var seen = new HashSet<int>();

        try
        {
            var hub6Transforms = new List<Transform>();
            foreach (Transform candidate in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (candidate != null)
                    hub6Transforms.Add(candidate);
            }

            foreach (Transform tr in hub6Transforms.OrderBy(t => BuildPath(t), StringComparer.OrdinalIgnoreCase))
            {
                if (tr == null)
                    continue;

                string path = BuildPath(tr);
                if (!path.StartsWith(hub6LogicPrefix, StringComparison.Ordinal))
                    continue;

                GameObject go;
                try { go = tr.gameObject; }
                catch { continue; }
                if (go == null)
                    continue;

                int id;
                try { id = go.GetInstanceID(); }
                catch { id = 0; }
                if (id != 0 && !seen.Add(id))
                    continue;

                if (!HasTargetBearingNativeComponent(go))
                    continue;

                if (emitted++ >= 160)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] HUB6 TARGET COMPONENT SCAN capped at 160 objects.");
                    break;
                }

                bool active = false;
                try { active = go.activeInHierarchy; } catch { }
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] HUB6 TARGET OBJECT path='{path}' active={active}.");
                DumpNativeComponents(go, path);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] HUB6 TARGET COMPONENT SCAN failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] HUB6 TARGET COMPONENT SCAN COMPLETE objects={Math.Min(emitted, 160)}.");
    }

    private static void DumpRootsFocusedCandidates()
    {
        const string rootsRootPath = "Root/GameRoom_Hub2_Logic";
        string[] rootsTokens =
        {
            "phone", "booth", "trevor", "weed", "killer", "bagpipe", "star", "eater", "firstareagate", "gate"
        };

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== ROOTS ROUTING FOCUSED SCAN BEGIN ===== Looking for the Roots Phone Booth Weed Killer reward sequence, Trevor blockade, FirstAreaGate, and related progression conditions.");

        var matches = new List<Transform>();
        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null)
                    continue;

                string path = BuildPath(tr);
                if (!path.StartsWith(rootsRootPath, StringComparison.Ordinal))
                    continue;

                string lower = path.ToLowerInvariant();
                if (!rootsTokens.Any(token => lower.Contains(token, StringComparison.Ordinal)))
                    continue;

                matches.Add(tr);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS ROUTING scan enumeration failed: {ex.GetBaseException().Message}");
            return;
        }

        var seen = new HashSet<int>();
        int emitted = 0;
        foreach (Transform tr in matches.OrderBy(t => BuildPath(t), StringComparer.OrdinalIgnoreCase))
        {
            GameObject go;
            try { go = tr.gameObject; }
            catch { continue; }
            if (go == null)
                continue;

            int id;
            try { id = go.GetInstanceID(); }
            catch { id = 0; }
            if (id != 0 && !seen.Add(id))
                continue;

            if (emitted++ >= 180)
            {
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] ROOTS ROUTING focused scan capped at 180 matching objects.");
                break;
            }

            string path = BuildPath(tr);
            bool active = false;
            try { active = go.activeInHierarchy; } catch { }
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS ROUTING OBJECT path='{path}' active={active}.");
            DumpNativeComponents(go, path);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== ROOTS ROUTING FOCUSED SCAN END ===== objects={Math.Min(emitted, 180)}. Use the Roots world Phone Booth once after the scan so its next transition is tagged.");
    }

    private static void DumpSceneCandidates()
    {
        var candidates = new List<Transform>();
        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null)
                    continue;

                string path = BuildPath(tr);
                if (!IsRelevantText(path))
                    continue;

                candidates.Add(tr);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] PHONE BOOTH SCENE SCAN enumeration failed: {ex.GetBaseException().Message}");
            return;
        }

        var seen = new HashSet<int>();
        int emitted = 0;
        foreach (Transform tr in candidates.OrderBy(t => BuildPath(t), StringComparer.OrdinalIgnoreCase))
        {
            GameObject go;
            try { go = tr.gameObject; }
            catch { continue; }
            if (go == null)
                continue;

            int id;
            try { id = go.GetInstanceID(); }
            catch { id = 0; }
            if (id != 0 && !seen.Add(id))
                continue;

            if (emitted++ >= 100)
            {
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] PHONE BOOTH SCENE SCAN capped at 100 matching objects.");
                break;
            }

            string path = BuildPath(tr);
            bool active = false;
            try { active = go.activeInHierarchy; } catch { }
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] PHONE BOOTH OBJECT path='{path}' active={active}.");

            DumpNativeComponents(go, path);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] PHONE BOOTH SCENE SCAN COMPLETE objects={Math.Min(emitted, 100)}.");
    }

    private static void DumpNativeComponents(GameObject go, string path)
    {
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] PHONE BOOTH COMPONENTS failed path='{path}': {ex.GetBaseException().Message}");
            return;
        }

        int index = 0;
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            index++;
            IntPtr obj = GetNativePointer(component);
            IntPtr klass = IntPtr.Zero;
            try
            {
                if (obj != IntPtr.Zero)
                    klass = pb_il2cpp_object_get_class(obj);
            }
            catch { }

            string className = NativeClassFullName(klass);
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] PHONE BOOTH COMPONENT path='{path}' index={index} managed='{component.GetType().FullName}' native='{className}'.");

            // The booth hierarchy often uses generic interaction/sequence components,
            // so inspect all components on matching scene objects but emit only
            // destination/room/travel/progression-related fields.
            DumpRelevantNativeFields(obj, klass, path, className);
        }
    }

    private static void DumpRelevantNativeFields(
        IntPtr obj,
        IntPtr klass,
        string path,
        string rootClassName)
    {
        if (obj == IntPtr.Zero || klass == IntPtr.Zero)
            return;

        int emitted = 0;
        IntPtr current = klass;
        for (int depth = 0; current != IntPtr.Zero && depth < 7 && emitted < 60; depth++)
        {
            IntPtr iter = IntPtr.Zero;
            while (emitted < 60)
            {
                IntPtr field;
                try { field = pb_il2cpp_class_get_fields(current, ref iter); }
                catch { field = IntPtr.Zero; }
                if (field == IntPtr.Zero)
                    break;

                string fieldName = NativeAnsi(() => pb_il2cpp_field_get_name(field));
                IntPtr fieldType = IntPtr.Zero;
                try { fieldType = pb_il2cpp_field_get_type(field); }
                catch { }
                string fieldTypeName = NativeTypeName(fieldType);

                if (!IsInterestingField(fieldName, fieldTypeName))
                    continue;

                string value = NativeFieldValue(field, obj, fieldTypeName);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] PHONE BOOTH NATIVE FIELD path='{path}' class='{rootClassName}' declaring='{NativeClassFullName(current)}' name='{fieldName}' type='{fieldTypeName}' value='{value}'.");
                emitted++;
            }

            try { current = pb_il2cpp_class_get_parent(current); }
            catch { current = IntPtr.Zero; }
        }
    }

    private static bool IsInterestingField(string fieldName, string fieldTypeName)
    {
        string combined = (fieldName + " " + fieldTypeName).ToLowerInvariant();
        return combined.Contains("destination") ||
               combined.Contains("room") ||
               combined.Contains("spawn") ||
               combined.Contains("travel") ||
               combined.Contains("teleport") ||
               combined.Contains("phone") ||
               combined.Contains("booth") ||
               combined.Contains("unlock") ||
               combined.Contains("progress") ||
               combined.Contains("available") ||
               combined.Contains("visited") ||
               combined.Contains("identifier") ||
               combined.Contains("location") ||
               combined.Contains("level") ||
               combined.Contains("item") ||
               combined.Contains("ability") ||
               combined.Contains("bag") ||
               combined.Contains("reward") ||
               combined.Contains("flag") ||
               combined.Contains("condition") ||
               combined.Contains("sequence") ||
               combined.Contains("quest");
    }

    private static bool IsRelevantText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string value = text.ToLowerInvariant();
        return CandidateTokens.Any(token => value.Contains(token, StringComparison.Ordinal));
    }

    private static string BuildPath(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;
            for (int i = 0; i < 32 && current != null; i++)
            {
                names.Add(current.name ?? "<unnamed>");
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return tr.name ?? "<unavailable>";
        }
    }

    private static IntPtr GetNativePointer(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(value) is IntPtr pointer ? pointer : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static string NativeClassFullName(IntPtr klass)
    {
        if (klass == IntPtr.Zero)
            return "<no-class>";

        string ns = NativeAnsi(() => pb_il2cpp_class_get_namespace(klass));
        string name = NativeAnsi(() => pb_il2cpp_class_get_name(klass));
        if (string.IsNullOrWhiteSpace(name))
            name = "<unnamed-class>";
        return string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
    }

    private static string NativeTypeName(IntPtr type)
    {
        if (type == IntPtr.Zero)
            return "<unknown-type>";

        try
        {
            IntPtr klass = pb_il2cpp_class_from_type(type);
            if (klass != IntPtr.Zero)
                return NativeClassFullName(klass);
        }
        catch { }
        return "<unknown-type>";
    }

    private static string NativeAnsi(Func<IntPtr> getter)
    {
        try
        {
            IntPtr ptr = getter();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NativeFieldValue(IntPtr field, IntPtr obj, string typeName)
    {
        IntPtr boxed;
        try { boxed = pb_il2cpp_field_get_value_object(field, obj); }
        catch { return "<read-failed>"; }
        if (boxed == IntPtr.Zero)
            return "<null>";

        string simple = typeName.ToLowerInvariant();
        try
        {
            IntPtr unboxed = pb_il2cpp_object_unbox(boxed);
            if (unboxed != IntPtr.Zero)
            {
                if (simple == "system.boolean")
                    return Marshal.ReadByte(unboxed) != 0 ? "True" : "False";
                if (simple == "system.byte")
                    return Marshal.ReadByte(unboxed).ToString();
                if (simple.Contains("int32") || simple.Contains("uint32") ||
                    simple.Contains("int16") || simple.Contains("uint16") ||
                    simple.Contains("progressionflag"))
                    return Marshal.ReadInt32(unboxed).ToString();
            }
        }
        catch { }

        try
        {
            IntPtr valueClass = pb_il2cpp_object_get_class(boxed);
            return $"<{NativeClassFullName(valueClass)}>";
        }
        catch
        {
            return "<object>";
        }
    }

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_object_get_class")]
    private static extern IntPtr pb_il2cpp_object_get_class(IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_parent")]
    private static extern IntPtr pb_il2cpp_class_get_parent(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_name")]
    private static extern IntPtr pb_il2cpp_class_get_name(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_namespace")]
    private static extern IntPtr pb_il2cpp_class_get_namespace(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_fields")]
    private static extern IntPtr pb_il2cpp_class_get_fields(IntPtr klass, ref IntPtr iter);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_name")]
    private static extern IntPtr pb_il2cpp_field_get_name(IntPtr field);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_type")]
    private static extern IntPtr pb_il2cpp_field_get_type(IntPtr field);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_from_type")]
    private static extern IntPtr pb_il2cpp_class_from_type(IntPtr type);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_value_object")]
    private static extern IntPtr pb_il2cpp_field_get_value_object(IntPtr field, IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_object_unbox")]
    private static extern IntPtr pb_il2cpp_object_unbox(IntPtr obj);
}

internal static class WeedKillerRandomization
{
    public const string ItemName = "Weed Killer";
    public const string SourceLocationName = "Roots - Gecko's Weed Killer";
    public const string NativeBagFlag = "WEED_KILLER_BAG_ITEM";
    public const string NativeCollectedFlag = "ROOTS_HUB_WEED_KILLER_COLLECTED";

    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static bool _owned;
    private static bool _pendingNativeGrant;
    private static bool _nativeGrantApplied;

    [ThreadStatic]
    private static bool _applyingNativeGrant;

    public static bool Enabled { get; private set; }
    public static string ImplementationVersion { get; private set; } = string.Empty;

    public static void Configure()
    {
        lock (Sync)
        {
            Enabled = false;
            ImplementationVersion = string.Empty;
            _playerSaveRequestProcessor = null;
            _owned = false;
            _pendingNativeGrant = false;
            _nativeGrantApplied = false;
        }
    }

    public static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        bool requested = false;
        if (slotData != null &&
            slotData.TryGetValue("randomize_weed_killer", out object? rawEnabled) &&
            rawEnabled != null)
        {
            if (rawEnabled is bool b)
                requested = b;
            else
                bool.TryParse(rawEnabled.ToString(), out requested);
        }

        lock (Sync)
        {
            ImplementationVersion = implementation;
            Enabled = requested &&
                      (implementation.StartsWith("area-routing-weed-killer-0.14", StringComparison.OrdinalIgnoreCase) ||
                       implementation.StartsWith("area-routing-plant-pipes-0.15", StringComparison.OrdinalIgnoreCase));
        }

        Plugin.LoggerInstance?.LogWarning(
            Enabled
                ? $"[SCRC-AP] ROOTS WEED KILLER RANDOMIZATION ENABLED implementation='{implementation}' item='{ItemName}' source='{SourceLocationName}'. Gecko source check is live; vanilla Weed Killer grant is suppressed and AP delivery grants the native consumable."
                : $"[SCRC-AP] ROOTS WEED KILLER RANDOMIZATION disabled implementation='{implementation}' requested={requested}; Gecko/Weed Killer remain vanilla for this seed.");

        TryFlushPendingNativeGrant();
    }

    public static bool TryApplyItem(string itemName)
    {
        if (!string.Equals(itemName, ItemName, StringComparison.OrdinalIgnoreCase))
            return false;

        lock (Sync)
        {
            _owned = true;
            _pendingNativeGrant = true;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS WEED KILLER RECEIVED item='{ItemName}' routingEnabled={Enabled}. Native flag '{NativeBagFlag}' will be applied immediately when PlayerSaveRequestProcessor is available.");

        TryFlushPendingNativeGrant();
        return true;
    }

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null || !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;

        lock (Sync)
            _playerSaveRequestProcessor = instance;
    }

    public static void TryFlushPendingNativeGrant()
    {
        object? processor;
        bool shouldGrant;
        lock (Sync)
        {
            shouldGrant = Enabled && _owned && _pendingNativeGrant && !_nativeGrantApplied;
            processor = _playerSaveRequestProcessor;
        }

        if (_applyingNativeGrant || !shouldGrant || processor == null)
            return;

        if (!TrySubmitProgressionFlag(processor, NativeBagFlag, true, out string detail))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS WEED KILLER native grant pending: could not submit '{NativeBagFlag}' yet. {detail}");
            return;
        }

        lock (Sync)
        {
            _pendingNativeGrant = false;
            _nativeGrantApplied = true;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS WEED KILLER NATIVE GRANT APPLIED flag='{NativeBagFlag}'. Vanilla may consume this bag item normally when opening Level 3. {detail}");
    }

    public static bool ShouldSuppressGeckoVanillaGrant(object request, string flag)
    {
        if (_applyingNativeGrant ||
            !Enabled ||
            !string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.Ordinal) ||
            !string.Equals(flag, NativeBagFlag, StringComparison.OrdinalIgnoreCase))
            return false;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        if (!value)
            return false;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS WEED KILLER VANILLA GRANT SUPPRESSED flag='{flag}' room='{DeveloperHarness.CurrentRoomId}'. Gecko's story sequence continues; the actual Weed Killer must come from Archipelago.");
        return true;
    }

    public static void RecordGeckoSourceCollected(object request, string flag)
    {
        if (!Enabled ||
            !string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.Ordinal) ||
            !string.Equals(flag, NativeCollectedFlag, StringComparison.OrdinalIgnoreCase))
            return;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        if (!value)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS WEED KILLER SOURCE AP CHECK flag='{flag}' location='{SourceLocationName}'. Native collected/story marker retained; native Weed Killer bag grant is randomized.");
        Plugin.AP?.QueueLocation(SourceLocationName);
    }

    internal static bool TrySubmitProgressionFlag(object processor, string flagName, bool value, out string detail)
    {
        detail = string.Empty;
        try
        {
            Assembly? gameAssembly = ReflectionUtil.GameAssembly;
            Type? requestType = gameAssembly?.GetType("RecordGameProgressionInSaveDataRequest", throwOnError: false, ignoreCase: false);
            if (requestType == null)
            {
                detail = "RecordGameProgressionInSaveDataRequest type not found.";
                return false;
            }

            Type? flagType = requestType.GetProperty(
                    "Flag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.PropertyType
                ?? requestType.GetField(
                    "_Flag_k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType;
            if (flagType == null || !flagType.IsEnum)
            {
                detail = "progression flag enum type not found.";
                return false;
            }

            object flagValue;
            try { flagValue = Enum.Parse(flagType, flagName, ignoreCase: false); }
            catch (Exception ex)
            {
                detail = $"enum value '{flagName}' unavailable: {ex.GetBaseException().Message}";
                return false;
            }

            object? request = null;
            string ctorDescription = string.Empty;
            foreach (ConstructorInfo ctor in requestType.GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     .Where(c => !c.GetParameters().Any(p => p.ParameterType == typeof(IntPtr)))
                     .OrderBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ps;
                try { ps = ctor.GetParameters(); }
                catch { continue; }

                var args = new object?[ps.Length];
                bool supported = true;
                for (int i = 0; i < ps.Length; i++)
                {
                    Type pt = ps[i].ParameterType;
                    if (pt == flagType)
                        args[i] = flagValue;
                    else if (pt == typeof(bool))
                        args[i] = value;
                    else if (pt.IsEnum)
                    {
                        try { args[i] = Enum.Parse(pt, "INVALID", ignoreCase: true); }
                        catch { args[i] = Activator.CreateInstance(pt); }
                    }
                    else if (pt.IsValueType)
                        args[i] = Activator.CreateInstance(pt);
                    else
                        args[i] = null;
                }

                if (!supported)
                    continue;

                try
                {
                    request = ctor.Invoke(args);
                    ctorDescription = $"ctor({string.Join(",", ps.Select(p => p.ParameterType.Name))})";
                    if (request != null)
                        break;
                }
                catch { request = null; }
            }

            if (request == null)
            {
                try
                {
                    request = Activator.CreateInstance(requestType, nonPublic: true);
                    ctorDescription = "Activator.CreateInstance";
                }
                catch (Exception ex)
                {
                    detail = $"could not construct request: {ex.GetBaseException().Message}";
                    return false;
                }
            }

            // Ensure the semantic values are correct even if a permissive/default
            // constructor was selected. Generated IL2CPP wrappers expose these
            // backing fields in the current game build.
            TryWriteMember(request, "Flag", flagValue);
            TryWriteMember(request, "_Flag_k__BackingField", flagValue);
            TryWriteMember(request, "Value", value);
            TryWriteMember(request, "_Value_k__BackingField", value);

            MethodInfo? process = processor.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "ProcessRequest", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] ps;
                    try { ps = m.GetParameters(); }
                    catch { return false; }
                    return ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(request);
                });

            if (process == null)
            {
                detail = "matching PlayerSaveRequestProcessor.ProcessRequest overload not found.";
                return false;
            }

            try
            {
                _applyingNativeGrant = true;
                process.Invoke(processor, new[] { request });
            }
            finally
            {
                _applyingNativeGrant = false;
            }

            detail = $"via {ctorDescription}";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().ToString();
            return false;
        }
    }

    private static bool TryWriteMember(object obj, string name, object value)
    {
        Type t = obj.GetType();
        try
        {
            PropertyInfo? p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        try
        {
            FieldInfo? f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        return false;
    }
}


internal static class RootsIntroCutsceneBypass
{
    public const string NativeIntroWitnessedFlag = "ROOTS_HUB_INTRO_WITNESSED";
    public const string NativeGateOpenedFlag = "ROOTS_HUB_GATE_OPENED";
    public const string NativeDifficultyCompleteFlag = "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE";
    public const string RootsRoomId = "GameRoom_Hub2";

    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static bool _missingProcessorLogged;
    private static PendingTransition? _pendingTransition;
    private static int _retryCount;
    private static bool _submissionOutstanding;
    private static DateTime _nextAttemptUtc;

    [ThreadStatic]
    private static bool _replaying;

    private sealed class PendingTransition
    {
        public object Instance = null!;
        public MethodBase Method = null!;
        public object[] Args = Array.Empty<object>();
    }

    public static void Configure()
    {
        lock (Sync)
        {
            _playerSaveRequestProcessor = null;
            _missingProcessorLogged = false;
            _pendingTransition = null;
            _retryCount = 0;
            _submissionOutstanding = false;
            _nextAttemptUtc = DateTime.MinValue;
        }
    }

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null ||
            !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;

        lock (Sync)
            _playerSaveRequestProcessor = instance;
    }

    public static bool ShouldAllowTransition(
        object instance,
        MethodBase method,
        object[]? args,
        string? roomId)
    {
        if (_replaying)
            return true;

        bool enabled = AreaAccessPrototype.Enabled && AreaAccessPrototype.HasArea("Roots");
        bool enteringRoots = string.Equals(roomId, RootsRoomId, StringComparison.Ordinal);
        bool bootstrapOwned = TryReadBootstrapState(
            out _, out _, out _);

        object? processor;
        int retryCount;
        bool submissionOutstanding;
        lock (Sync)
        {
            processor = _playerSaveRequestProcessor;
            retryCount = _retryCount;
            submissionOutstanding = _submissionOutstanding;
        }
        RootsIntroDecision decision = RootsPresentationPolicy.DecideIntro(
            new RootsIntroSnapshot(
                enabled,
                IntroHubSkip.Compatible,
                enteringRoots,
                bootstrapOwned,
                processor != null,
                submissionOutstanding,
                retryCount));

        if (decision is RootsIntroDecision.Vanilla or RootsIntroDecision.FallbackVanilla)
            return true;
        if (decision == RootsIntroDecision.AllowTransition)
        {
            lock (Sync)
            {
                _pendingTransition = null;
            }
            return true;
        }

        lock (Sync)
        {
            _pendingTransition ??= new PendingTransition
            {
                Instance = instance,
                Method = method,
                Args = args?.ToArray() ?? Array.Empty<object>(),
            };
        }

        TryAdvancePending();
        return false;
    }

    public static void TickPending() => TryAdvancePending();

    private static void TryAdvancePending()
    {
        PendingTransition? pending;
        object? processor;
        int retryCount;
        bool outstanding;
        DateTime nextAttempt;
        lock (Sync)
        {
            pending = _pendingTransition;
            processor = _playerSaveRequestProcessor;
            retryCount = _retryCount;
            outstanding = _submissionOutstanding;
            nextAttempt = _nextAttemptUtc;
        }
        if (pending == null)
            return;

        bool bootstrapOwned = TryReadBootstrapState(
            out bool introOwned,
            out bool gateOwned,
            out bool difficultyOwned);
        if (bootstrapOwned)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS PRE-ENTRY BOOTSTRAP VERIFIED timing='before {RootsRoomId} transition' intro=True gate=True difficulty=True; native PlayGateAndDifficultyScene will take its skip path without granting Level 1 completion, Stars, or AP checks.");
            ReplayPending();
            return;
        }

        if (DateTime.UtcNow < nextAttempt)
            return;

        TimeSpan? retryDelay = RootsPresentationPolicy.RetryDelay(retryCount);
        if (retryDelay == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] ROOTS PRE-ENTRY BOOTSTRAP FALLBACK: exact native flags could not be verified within the bounded retry schedule; replaying vanilla transition.");
            ReplayPending();
            return;
        }

        if (processor != null && !outstanding)
        {
            bool submitted = true;
            var details = new List<string>();
            if (!introOwned)
            {
                submitted &= WeedKillerRandomization.TrySubmitProgressionFlag(
                    processor, NativeIntroWitnessedFlag, true, out string detail);
                details.Add(detail);
            }
            if (!gateOwned)
            {
                submitted &= WeedKillerRandomization.TrySubmitProgressionFlag(
                    processor, NativeGateOpenedFlag, true, out string detail);
                details.Add(detail);
            }
            if (!difficultyOwned)
            {
                submitted &= WeedKillerRandomization.TrySubmitProgressionFlag(
                    processor, NativeDifficultyCompleteFlag, true, out string detail);
                details.Add(detail);
            }
            lock (Sync)
                _submissionOutstanding = submitted;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] ROOTS PRE-ENTRY BOOTSTRAP submission submitted={submitted} retry={retryCount} introOwned={introOwned} gateOwned={gateOwned} difficultyOwned={difficultyOwned}. {string.Join(" | ", details)}");
        }
        else if (processor == null && !_missingProcessorLogged)
        {
            _missingProcessorLogged = true;
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] ROOTS INTRO CUTSCENE BYPASS waiting for PlayerSaveRequestProcessor before entry.");
        }

        lock (Sync)
        {
            _retryCount++;
            _submissionOutstanding = false;
            _nextAttemptUtc = DateTime.UtcNow + retryDelay.Value;
        }
    }

    private static bool TryReadBootstrapState(
        out bool introOwned,
        out bool gateOwned,
        out bool difficultyOwned)
    {
        bool introReadable = RootsBucketRandomization.TryReadProgressionFlag(
            NativeIntroWitnessedFlag, out introOwned);
        bool gateReadable = RootsBucketRandomization.TryReadProgressionFlag(
            NativeGateOpenedFlag, out gateOwned);
        bool difficultyReadable = RootsBucketRandomization.TryReadProgressionFlag(
            NativeDifficultyCompleteFlag, out difficultyOwned);
        return introReadable && gateReadable && difficultyReadable &&
               introOwned && gateOwned && difficultyOwned;
    }

    private static void ReplayPending()
    {
        PendingTransition? pending;
        lock (Sync)
        {
            pending = _pendingTransition;
            _pendingTransition = null;
            _submissionOutstanding = false;
        }
        if (pending == null)
            return;

        try
        {
            _replaying = true;
            pending.Method.Invoke(pending.Instance, pending.Args);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] ROOTS deferred transition replay failed: {ex.GetBaseException().Message}");
        }
        finally
        {
            _replaying = false;
        }
    }
}

internal static class RootsStartupBootstrap
{
    internal const string GateOpenedFlag = "ROOTS_HUB_GATE_OPENED";
    internal const string DifficultyCompleteFlag = "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE";

    private static readonly object Sync = new();
    private static object? _processor;
    private static bool _compatible;
    private static bool _synchronized;
    private static bool _complete;
    private static int _attempts;
    private static int _cooldown;

    public static void Configure()
    {
        lock (Sync)
        {
            _processor = null;
            _compatible = false;
            _synchronized = false;
            _complete = false;
            _attempts = 0;
            _cooldown = 0;
        }
    }

    public static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? raw)
            ? raw?.ToString() ?? string.Empty
            : string.Empty;
        lock (Sync)
        {
            _compatible = AreaAccessPrototype.Enabled &&
                          implementation.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase);
            _synchronized = true;
            _complete = false;
            _attempts = 0;
            _cooldown = 0;
        }
    }

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null ||
            !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;
        lock (Sync)
            _processor = instance;
    }

    public static void Tick()
    {
        object? processor;
        int attempts;
        lock (Sync)
        {
            if (!_compatible || _complete || _cooldown-- > 0)
                return;
            _cooldown = 60;
            processor = _processor;
            attempts = _attempts;
        }

        bool gateReadable = RootsBucketRandomization.TryReadProgressionFlag(GateOpenedFlag, out bool gateOwned);
        bool difficultyReadable = RootsBucketRandomization.TryReadProgressionFlag(DifficultyCompleteFlag, out bool difficultyOwned);
        if (!gateReadable || !difficultyReadable || processor == null)
            return;

        StarHudDecision hudDecision = StarHudPolicy.Decide(
            enabled: true,
            compatible: _compatible,
            synchronized: _synchronized,
            roomId: DeveloperHarness.CurrentRoomId);

        if (hudDecision != StarHudDecision.ShowCampaignStars)
            return;

        if (gateOwned && difficultyOwned)
        {
            lock (Sync)
                _complete = true;
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] ROOTS STARTUP BOOTSTRAP VERIFIED: native campaign HUD and difficulty bookkeeping are ready.");
            return;
        }

        if (attempts >= 4)
        {
            lock (Sync)
                _complete = true;
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] ROOTS STARTUP BOOTSTRAP FALLBACK: exact flags could not be verified after four submissions; preserving current native state.");
            return;
        }

        bool submitted = true;
        if (!gateOwned)
            submitted &= WeedKillerRandomization.TrySubmitProgressionFlag(
                processor, GateOpenedFlag, true, out _);
        if (!difficultyOwned)
            submitted &= WeedKillerRandomization.TrySubmitProgressionFlag(
                processor, DifficultyCompleteFlag, true, out _);

        lock (Sync)
            _attempts++;
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] ROOTS STARTUP BOOTSTRAP SUBMITTED attempt={attempts + 1} success={submitted} gateOwned={gateOwned} difficultyOwned={difficultyOwned}; Music Lab Points HUD remains untouched and no difficulty was selected.");
    }
}

internal sealed class MusicLabBarrierKeeper : MonoBehaviour
{
    private readonly Dictionary<string, (GameObject Object, bool ActiveSelf)> _baseline =
        new(StringComparer.Ordinal);
    private int _cooldown;

    public MusicLabBarrierKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("MusicLabBarrierKeeper.Update");
        if (_cooldown-- > 0)
            return;
        _cooldown = 30;

        string roomId = DeveloperHarness.CurrentRoomId;
        bool active = AreaAccessPrototype.Enabled && IntroHubSkip.Compatible &&
                      string.Equals(roomId, "GameRoom_Hub6", StringComparison.OrdinalIgnoreCase);
        if (!active)
        {
            RestoreVanillaState();
            return;
        }

        foreach (string exactPath in MusicLabBarrierPolicy.KnownBarrierPaths)
        {
            if (!MusicLabBarrierPolicy.ShouldDisable(true, true, roomId, exactPath))
                continue;

            if (!_baseline.TryGetValue(exactPath, out var state))
            {
                GameObject? obj = GameObject.Find(exactPath);
                if (obj == null)
                    continue;
                state = (obj, obj.activeSelf);
                _baseline[exactPath] = state;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB BARRIER BYPASS bound exactPath='{exactPath}' vanillaActive={state.ActiveSelf}.");
            }

            if (state.Object != null && state.Object.activeSelf)
                state.Object.SetActive(false);
        }
    }

    private void OnDestroy() => RestoreVanillaState();

    private void RestoreVanillaState()
    {
        foreach (var state in _baseline.Values)
        {
            if (state.Object != null && state.Object.activeSelf != state.ActiveSelf)
                state.Object.SetActive(state.ActiveSelf);
        }
        _baseline.Clear();
    }
}


internal static class CassetteSourceRandomization
{
    private static bool _enabled;

    internal static void Configure() => _enabled=false;
    internal static void ApplySlotData(Dictionary<string,object>? slotData)
    {
        CassetteSlotCompatibilityResult compatibility = CassetteSlotDataCompatibility.Validate(slotData);
        _enabled=compatibility.Compatible;
        Plugin.LoggerInstance?.LogWarning(_enabled
            ? $"[SCRC-AP] CASSETTE SOURCE RANDOMIZATION ENABLED entries={CassetteCatalog.All.Count} levelSources={CassetteCatalog.All.Count(x=>x.SourceType==CassetteSourceType.LevelEarnedReward)} chestSources={CassetteCatalog.All.Count(x=>x.SourceType==CassetteSourceType.MusicLabPointChest)}."
            : $"[SCRC-AP] CASSETTE SEED INCOMPATIBLE detail=\"{compatibility.Detail}\". Full cassette routing is disabled and native cassette behavior remains enabled. Generate and host a fresh APWorld v0.22 seed.");
    }

    internal static bool AllowLevelCassetteEvaluation(bool? succeeded)
    {
        if(!_enabled)return true;
        string level=InvokeStaticIdentifier("LevelEnquiries","GetCurrentLevelIdentifier");
        string variant=InvokeStaticIdentifier("LevelEnquiries","GetCurrentLevelVariantIdentifier");
        IReadOnlyList<CassetteDefinition> mapped=CassetteCatalog.ForLevelSource(level,variant);
        if(mapped.Count==0)return true;
        var statuses=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        foreach(CassetteDefinition cassette in mapped)
        {
            if(!TryReadNativeStatus(cassette.NativeSong,out string? status,out string detail))
            {
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SOURCE SAFE FALLBACK level='{level}' variant='{variant}' song='{cassette.NativeSong}' detail='{detail}'. Native evaluator allowed.");
                return true;
            }
            statuses[cassette.NativeSong]=status!;
        }
        CassetteSourceDecision decision=CassetteRandomizationPolicy.DecideLevelEvaluation(level,variant,succeeded==true,statuses);
        if(decision.AllowNative)return true;
        foreach(string location in decision.SourceLocationsToQueue)
            QueueCatalogSourceByLocation(location,$"level='{level}' variant='{variant}'");
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE NATIVE EVALUATOR SUPPRESSED level='{level}' variant='{variant}' songs='{string.Join(",",decision.NativeSongsToSuppress)}' detail='{decision.Detail}'.");
        return false;
    }

    internal static bool AllowCassetteStatusRequest(string? nativeSong, string? status, bool fromArchipelago)
    {
        if (!_enabled || !CassetteRandomizationPolicy.ShouldSuppressNativePointChestGrant(
                nativeSong, status, fromArchipelago))
            return true;

        CassetteDefinition entry = CassetteCatalog.ByNativeSong[nativeSong!];
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE POINT-CHEST NATIVE BAG GRANT SUPPRESSED nativeSong='{entry.NativeSong}' location='{entry.SourceName}'. Other chest rewards and collected-state progression remain native.");
        return false;
    }

    internal static bool QueueCatalogSourceByLocation(string location,string sourceIdentity)
    {
        CassetteDefinition? entry=CassetteCatalog.All.FirstOrDefault(x=>string.Equals(x.SourceName,location,StringComparison.OrdinalIgnoreCase));
        if(entry==null)return false;
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE SOURCE AP CHECK nativeSong='{entry.NativeSong}' location='{entry.SourceName}' source='{sourceIdentity}'.");
        Plugin.AP?.QueueLocation(entry.SourceName);
        return true;
    }

    private static string InvokeStaticIdentifier(string typeName,string methodName)
    {
        try
        {
            Type? type=ReflectionUtil.GameAssembly?.GetType(typeName,false,false);
            MethodInfo? method=type?.GetMethod(methodName,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static);
            object? raw=method?.Invoke(null,null); object? value=ReflectionUtil.UnwrapNullable(raw);
            return ReflectionUtil.ExtractIdentifier(value)??value?.ToString()??"<null>";
        }
        catch(Exception ex){return $"<error:{ex.GetBaseException().Message}>";}
    }

    private static bool TryReadNativeStatus(string nativeSong,out string? status,out string detail)
    {
        status=null;detail=string.Empty;
        try
        {
            Assembly? asm=ReflectionUtil.GameAssembly;
            Type? enquiries=asm?.GetType("SongCassetteEnquiries",false,false);
            Type? songType=asm?.GetType("ePlayableSong",false,false);
            if(enquiries==null||songType==null||!songType.IsEnum){detail="cassette enquiries or song enum unavailable";return false;}
            object song=Enum.Parse(songType,nativeSong,false);
            MethodInfo? method=enquiries.GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).FirstOrDefault(m=>m.Name=="GetSongCassetteStatus"&&m.GetParameters().Length==1&&m.GetParameters()[0].ParameterType==songType);
            if(method==null){detail="GetSongCassetteStatus unavailable";return false;}
            object? raw=ReflectionUtil.UnwrapNullable(method.Invoke(null,new[]{song})); status=raw?.ToString();
            detail=$"selected save returned {status??"<null>"}";return !string.IsNullOrWhiteSpace(status);
        }
        catch(Exception ex){detail=ex.GetBaseException().Message;return false;}
    }

}

internal static class CassetteSlotDataCompatibility
{
    internal static CassetteSlotCompatibilityResult Validate(Dictionary<string,object>? slotData)
    {
        int schema=ReadInt(slotData,"cassette_schema");
        int count=ReadInt(slotData,"cassette_count");
        bool enabled=ReadBool(slotData,"full_cassette_randomization");
        return CassetteSlotCompatibility.Validate(
            schema, enabled, count,
            ReadMap(slotData,"cassette_items"),
            ReadMap(slotData,"cassette_sources"),
            ReadMap(slotData,"cassette_reused_locations"));
    }

    private static bool ReadBool(Dictionary<string,object>? data,string key)
    {
        if(data==null||!data.TryGetValue(key,out object? raw)||raw==null)return false;
        if(raw is bool b)return b; if(raw is long l)return l!=0; if(raw is int i)return i!=0;
        return bool.TryParse(raw.ToString(),out bool parsed)&&parsed;
    }
    private static int ReadInt(Dictionary<string,object>? data,string key)
    {
        if(data==null||!data.TryGetValue(key,out object? raw)||raw==null)return -1;
        if(raw is int i)return i; if(raw is long l&&l>=int.MinValue&&l<=int.MaxValue)return (int)l;
        return int.TryParse(raw.ToString(),out int parsed)?parsed:-1;
    }
    private static IReadOnlyDictionary<string,string> ReadMap(Dictionary<string,object>? data,string key)
    {
        if(data==null||!data.TryGetValue(key,out object? raw))return new Dictionary<string,string>(StringComparer.Ordinal);
        return CassetteSlotMapReader.Read(raw);
    }
}



internal static class CassetteReceiptRandomization
{
    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static object? _joinedSaveDataRequestProcessor;
    private static bool _slotDataSynchronized;
    private static CassetteSaveEpochRuntime _runtime = new();
    private static CassetteProcessorSaveIdentityStabilizer _saveIdentity = new();
    private static CassetteGameplayReadyGate _gameplayReady = new();
    private static CassetteRegularSavePointerJoinProbe _regularSavePointerJoinProbe = new();
    private static long _activeSaveGeneration;
    private static long _activeSavePointer;
    private static CassetteDiagnosticSignatureDeduplicator _mostRecentQueueLogDeduper = new();
    private static CassetteDiagnosticSignatureDeduplicator _mostRecentResultLogDeduper = new();
    private static bool _unityReconciliationRequested;
    private static string _unityReconciliationReason = string.Empty;
    private static string _lastIdentityDiagnostic = string.Empty;
    private static bool _saveSynchronizationReady;
    private static bool _saveSynchronizationDeferredLogged;
    private static bool _saveSynchronizationReadyLogged;
    private static bool _acceptanceDiagnosticsEnabled;
    private static CassetteProductionPersistenceCoordinator _productionPersistence =
        new(() => RequestUnityReconciliation("cassette persistence verified"));
    private static readonly CassettePersistenceAcceptanceMarkerEmitter _persistenceMarkerEmitter = new();
    private static CassettePersistencePollScheduler _persistencePoll = new();
    [ThreadStatic] private static bool _applyingNativeGrant;

    internal static bool Enabled { get; private set; }
    internal static bool IsApplyingNativeGrant => _applyingNativeGrant;
    internal static bool IsPersistenceWriteActive
    {
        get { lock (Sync) return _productionPersistence.Active; }
    }

    internal static void Configure(bool acceptanceDiagnosticsEnabled = false)
    {
        lock (Sync)
        {
            Enabled = false; _slotDataSynchronized = false;
            _playerSaveRequestProcessor = null; _joinedSaveDataRequestProcessor = null; _runtime = new CassetteSaveEpochRuntime(); _saveIdentity = new CassetteProcessorSaveIdentityStabilizer();
            _gameplayReady = new CassetteGameplayReadyGate();
            _regularSavePointerJoinProbe = new CassetteRegularSavePointerJoinProbe();
            _activeSaveGeneration = 0; _activeSavePointer = 0;
            _mostRecentQueueLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
            _mostRecentResultLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
            _unityReconciliationRequested = false; _unityReconciliationReason = string.Empty; _lastIdentityDiagnostic = string.Empty;
            _saveSynchronizationReady = false; _saveSynchronizationDeferredLogged = false; _saveSynchronizationReadyLogged = false;
            _acceptanceDiagnosticsEnabled = acceptanceDiagnosticsEnabled;
            _productionPersistence = new CassetteProductionPersistenceCoordinator(
                () => RequestUnityReconciliation("cassette persistence verified"));
            _persistencePoll = new CassettePersistencePollScheduler();
        }
    }

    internal static void SetPersistenceAcceptanceDiagnosticsEnabled(bool enabled)
    {
        if (enabled)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] CASSETTE PERSISTENCE DIAGNOSTICS enable deferred; restart is required so the opt-in write-completed event observer is installed at startup.");
            return;
        }
        lock (Sync) _acceptanceDiagnosticsEnabled = false;
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        CassetteSlotCompatibilityResult compatibility = CassetteSlotDataCompatibility.Validate(slotData);
        bool enabled = compatibility.Compatible;
        if (!enabled) CancelPointerBoundPersistence("cassette-routing-disabled");
        lock (Sync)
        {
            Enabled = enabled;
            _slotDataSynchronized = true;
            if (!enabled)
            {
                _runtime.DeactivateSave();
                _gameplayReady.BeginBoundary();
            }
        }
        Plugin.LoggerInstance?.LogWarning(enabled
            ? $"[SCRC-AP] CASSETTE RECEIPT RECONCILIATION ENABLED entries={CassetteCatalog.All.Count}. AP-owned cassettes will be reconciled as HAVE_IN_BAG through the native save lifecycle; deposited cassettes remain deposited."
            : $"[SCRC-AP] CASSETTE RECEIPT RECONCILIATION disabled detail=\"{compatibility.Detail}\"; native cassette inventory remains vanilla.");
        if (enabled) RequestUnityReconciliation("slot data synchronized");
    }

    internal static bool TryApplyItem(string itemName)
    {
        bool recognized;
        recognized = CassetteCatalog.ByItemName.TryGetValue(itemName, out CassetteDefinition? entry);
        if (!recognized) return false;
        lock (Sync) _runtime.Receive(entry!.NativeSong);
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE RECEIVED item='{entry.ItemName}' nativeSong='{entry.NativeSong}' routingEnabled={Enabled}; Unity-thread reconciliation requested.");
        RequestUnityReconciliation("AP cassette receipt");
        return true;
    }

    internal static void CapturePlayerSaveRequestProcessor(object? instance, bool reconcileNow = true)
    {
        if (instance == null || !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal)) return;
        bool changed;
        bool boundaryBound;
        lock (Sync)
        {
            changed = !ReferenceEquals(_playerSaveRequestProcessor, instance);
            _playerSaveRequestProcessor = instance;
            boundaryBound = _saveIdentity.CaptureProcessor(instance);
            _regularSavePointerJoinProbe.CapturePlayerProcessor(instance);
        }
        if (changed)
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE PLAYER PROCESSOR CAPTURED type='{instance.GetType().FullName}'.");
        if (boundaryBound)
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CASSETTE SAVE IDENTITY bound to post-selection player processor.");
        if (reconcileNow) OnLifecyclePoint("player save request processor activity");
    }

    internal static void OnLifecyclePoint(string reason)
    {
        RequestUnityReconciliation(reason);
    }

    internal static void LogPostLoadSnapshot(string currentRoom, string reason)
    {
        object? processor;
        long epoch;
        int? slot;
        long expectedPointer;
        lock (Sync)
        {
            processor = _playerSaveRequestProcessor;
            epoch = _runtime.Epoch;
            slot = _runtime.ActiveSlot;
            expectedPointer = _activeSavePointer;
        }
        string[] songs = { "BADASS", "HEAVY_METAL", "KEEP_ON_HUSTLIN", "ON_THE_WAY" };
        CassettePostLoadDiagnosticState snapshot =
            CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(processor, songs);
        string epochText = epoch > 0 ? epoch.ToString() : "<unavailable>";
        string slotText = slot?.ToString() ?? "<unavailable>";
        string expectedPointerText = expectedPointer != 0 ? $"0x{expectedPointer:X}" : "<unavailable>";
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE POST-LOAD SNAPSHOT room='{currentRoom}' reason='{reason}' " +
            $"epoch={epochText} slot={slotText} expectedPointer={expectedPointerText} " +
            CassetteSaveTransactionAdapter.FormatCassettePostLoadDiagnostic(snapshot));
    }

    private static void RequestUnityReconciliation(string reason)
    {
        lock (Sync)
        {
            if (!_slotDataSynchronized || !Enabled) return;
            _unityReconciliationRequested = true;
            _unityReconciliationReason = reason;
        }
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE Unity-thread observation queued reason='{reason}'; loaded-save epoch unchanged.");
    }

    internal static void QueueSaveBoundarySignal(int expectedSlot, CassetteSaveBoundarySignalKind kind, object? saveDataProcessor)
    {
        lock (Sync)
        {
            _regularSavePointerJoinProbe.Cancel();
            // Retain the exact callback owner for later pointer-bound proof. Never
            // substitute the old save's owner or invoke native work in this callback.
            _joinedSaveDataRequestProcessor = saveDataProcessor != null &&
                string.Equals(saveDataProcessor.GetType().Name, "SaveDataRequestProcessor", StringComparison.Ordinal)
                ? saveDataProcessor : null;
            object? processor = CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(_playerSaveRequestProcessor)
                ? _playerSaveRequestProcessor
                : null;
            _saveIdentity.Signal(expectedSlot, kind, processor);
            _unityReconciliationRequested = false;
            _unityReconciliationReason = string.Empty;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE BOUNDARY PENDING slot={expectedSlot} kind='{kind}'; prior gameplay readiness was suspended before new-load identity processing, pending two stable processor observations.");
    }

    internal static void SuspendGameplayReadinessForBoundary()
    {
        CancelPointerBoundPersistence("save-boundary");
        lock (Sync) _gameplayReady.BeginBoundary();
    }

    internal static void BeginMostRecentSelectionBoundary(object? saveDataProcessor)
    {
        CancelPointerBoundPersistence("most-recent-save-boundary");
        string stage;
        bool log;
        lock (Sync)
        {
            // Suspension is deliberately first: a malformed callback can never leave the old epoch eligible for work.
            _gameplayReady.BeginBoundary();
            _saveIdentity.SuspendUnresolved();
            _regularSavePointerJoinProbe.Cancel();
            _joinedSaveDataRequestProcessor = null;
            _unityReconciliationRequested = false;
            _unityReconciliationReason = string.Empty;
            if (saveDataProcessor == null)
            {
                stage = "processor-null";
            }
            else if (!string.Equals(saveDataProcessor.GetType().Name, "SaveDataRequestProcessor", StringComparison.Ordinal))
            {
                stage = $"processor-wrong-type:{saveDataProcessor.GetType().Name}";
            }
            else
            {
                _regularSavePointerJoinProbe.Queue(saveDataProcessor);
                if (CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(_playerSaveRequestProcessor))
                    _regularSavePointerJoinProbe.CapturePlayerProcessor(_playerSaveRequestProcessor);
                stage = "queued";
            }
            log = _mostRecentQueueLogDeduper.ShouldLog(stage);
        }
        if (log)
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE MOST-RECENT SAVE BOUNDARY stage='{stage}'; prior gameplay readiness suspended before new-load identity processing, pending unique regular-save pointer join.");
    }

    internal static void ActivateLoadedSave(long generation, int slot, long pointer, string reason)
    {
        CancelPointerBoundPersistence("new-save-epoch");
        long epoch;
        lock (Sync)
        {
            _runtime.ActivateSave(slot);
            _activeSaveGeneration = generation;
            _activeSavePointer = pointer;
            _gameplayReady.Activate(new(generation, _runtime.Epoch, slot, pointer));
            _saveSynchronizationReady = false;
            _saveSynchronizationDeferredLogged = false;
            _saveSynchronizationReadyLogged = false;
            epoch = _runtime.Epoch;
            var identity = new CassettePersistenceAcceptanceIdentity(generation, epoch, slot, pointer);
            _productionPersistence.BeginEpoch(identity);
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE EPOCH ACTIVATED epoch={epoch} slot={slot} reason='{reason}'.");
        RequestUnityReconciliation("save selection completed");
    }

    internal static void DeactivateLoadedSave(string reason)
    {
        CancelPointerBoundPersistence("save-deactivated");
        lock (Sync)
        {
            _runtime.DeactivateSave();
            _activeSaveGeneration = 0;
            _activeSavePointer = 0;
            _gameplayReady.BeginBoundary();
            _joinedSaveDataRequestProcessor = null;
            _saveSynchronizationReady = false;
            _saveSynchronizationDeferredLogged = false;
            _saveSynchronizationReadyLogged = false;
        }
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE SAVE EPOCH INACTIVE reason='{reason}'.");
    }

    private static void CancelPointerBoundPersistence(string reason)
    {
        CassettePersistenceAcceptanceIdentity identity;
        lock (Sync)
        {
            identity = new CassettePersistenceAcceptanceIdentity(
                _activeSaveGeneration,
                _runtime.Epoch,
                _runtime.ActiveSlot ?? -1,
                _activeSavePointer);
        }
        _productionPersistence.CancelEpoch(identity, reason);
        DrainPersistenceMarkers();
    }

    internal static void TryReconcile(string reason)
    {
        string[] songs;
        long generation;
        long epoch;
        int slot;
        long pointer;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_slotDataSynchronized || !Enabled || !_runtime.HasActiveSave) return;
            generation = _activeSaveGeneration;
            epoch = _runtime.Epoch;
            slot = _runtime.ActiveSlot!.Value;
            pointer = _activeSavePointer;
            if (!_gameplayReady.IsOpen(new(generation, epoch, slot, pointer))) return;
            songs = _runtime.PendingSongs.ToArray();
        }
        if (!TryConfirmSaveSynchronizationReady(
                generation, epoch, slot, pointer,
                allowObservationProbe: true, out _))
            return;
        EnsureCassetteProcessorAvailable();
        foreach (string song in songs) TryReconcileSong(song, reason, verificationDue: false);
    }

    private static bool TryConfirmSaveSynchronizationReady(
        long generation,
        long epoch,
        int slot,
        long pointer,
        bool allowObservationProbe,
        out string stage)
    {
        lock (Sync)
        {
            bool current = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                _activeSaveGeneration == generation && _runtime.Epoch == epoch &&
                _runtime.ActiveSlot == slot && _activeSavePointer == pointer;
            if (!current)
            {
                stage = "readiness-save-identity-changed";
                return false;
            }
            if (!allowObservationProbe)
            {
                stage = _saveSynchronizationReady ? "success" : "readiness-awaiting-observation";
                return _saveSynchronizationReady;
            }
        }

        bool ready = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(pointer, out stage);
        bool logDeferred = false;
        bool logReady = false;
        lock (Sync)
        {
            bool current = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                _activeSaveGeneration == generation && _runtime.Epoch == epoch &&
                _runtime.ActiveSlot == slot && _activeSavePointer == pointer;
            if (!current)
            {
                stage = "readiness-save-identity-changed";
                return false;
            }
            bool wasReady = _saveSynchronizationReady;
            _saveSynchronizationReady = ready;
            if (!ready && !_saveSynchronizationDeferredLogged)
            {
                _saveSynchronizationDeferredLogged = true;
                logDeferred = true;
            }
            else if (ready && !wasReady && _saveSynchronizationDeferredLogged && !_saveSynchronizationReadyLogged)
            {
                _saveSynchronizationReadyLogged = true;
                logReady = true;
            }
        }
        if (logDeferred)
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] CASSETTE SAVE SYNCHRONIZATION DEFERRED epoch={epoch} slot={slot} pointer=0x{pointer:X} stage='{stage}'; pending AP ownership retained until a lifecycle observation proves native selection readiness.");
        if (logReady)
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] CASSETTE SAVE SYNCHRONIZATION READY epoch={epoch} slot={slot} pointer=0x{pointer:X}; pending reconciliation resumed by lifecycle observation.");
        return ready;
    }

    private static bool EnsureCassetteProcessorAvailable()
    {
        lock (Sync)
        {
            if (_playerSaveRequestProcessor != null) return true;
            if (!_runtime.HasActiveSave) return false;
        }
        if (!CassetteSaveTransactionAdapter.TryGetLoadedSave(out _)) return false;
        try
        {
            Type? processorType = ReflectionUtil.GameAssembly == null
                ? null
                : ReflectionUtil.SafeGetTypes(ReflectionUtil.GameAssembly)
                    .FirstOrDefault(type => string.Equals(type.Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal));
            ConstructorInfo? constructor = processorType?.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(candidate => candidate.GetParameters().Length == 0);
            object? processor = constructor?.Invoke(Array.Empty<object>());
            if (!CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(processor)) return false;
            CapturePlayerSaveRequestProcessor(processor, reconcileNow: false);
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] CASSETTE constructed stateless PlayerSaveRequestProcessor after selected save became readable.");
            return true;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug($"[SCRC-AP] CASSETTE stateless processor unavailable: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static void TryReconcileSong(string nativeSong, string reason, bool verificationDue)
    {
        object? processor;
        long generation;
        long epoch;
        int slot;
        long pointer;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_slotDataSynchronized || !Enabled || !_runtime.HasActiveSave || !_runtime.IsPending(nativeSong)) return;
            processor = _playerSaveRequestProcessor;
            generation = _activeSaveGeneration;
            epoch = _runtime.Epoch;
            slot = _runtime.ActiveSlot!.Value;
            pointer = _activeSavePointer;
            if (!_gameplayReady.IsOpen(new(generation, epoch, slot, pointer))) return;
        }
        if (!TryConfirmSaveSynchronizationReady(
                generation, epoch, slot, pointer,
                allowObservationProbe: true, out _))
            return;
        if (!CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(processor) ||
            !CassetteSaveTransactionAdapter.TryReadCassetteStatus(processor!, nativeSong, out string? status))
        {
            lock (Sync)
            {
                if (verificationDue && _runtime.HasActiveSave && _runtime.Epoch == epoch)
                    _runtime.RecordVerification(nativeSong, null);
            }
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE DEFERRED nativeSong='{nativeSong}' reason='{reason}' detail='selected save or compatible processor unavailable'.");
            return;
        }
        bool submit;
        lock (Sync)
        {
            if (!_runtime.HasActiveSave || _runtime.Epoch != epoch) return;
            if (verificationDue) _runtime.RecordVerification(nativeSong, status);
            else _runtime.Observe(nativeSong, status);
            if (!_runtime.IsPending(nativeSong))
            {
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE VERIFIED nativeSong='{nativeSong}' status='{status}' epoch={epoch} reason='{reason}'.");
                return;
            }
            submit = _productionPersistence.CanSubmitNativeCassetteGrant(
                _runtime.CanSubmit(nativeSong, status, processorAvailable: true));
        }
        if (!submit) return;
        bool submitted;
        string detail;
        _applyingNativeGrant = true;
        try { submitted = CassetteSaveTransactionAdapter.TrySubmitHaveInBag(processor!, nativeSong, out detail); }
        finally { _applyingNativeGrant = false; }
        lock (Sync)
        {
            if (!_runtime.HasActiveSave || _runtime.Epoch != epoch) return;
            if (submitted) _runtime.RecordSubmission(nativeSong);
            else _runtime.RecordSubmissionFailure(nativeSong);
        }
        Plugin.LoggerInstance?.LogWarning(submitted
            ? $"[SCRC-AP] CASSETTE GRANT SUBMITTED epoch={epoch} {detail}; authoritative verification pending."
            : $"[SCRC-AP] CASSETTE GRANT FAILED epoch={epoch} nativeSong='{nativeSong}' detail='{detail}'.");
    }

    internal static void WithStableQuestItemSave(Action<object, string> action)
    {
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave || !_saveSynchronizationReady ||
                _playerSaveRequestProcessor == null ||
                !_gameplayReady.IsOpen(new(_activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot!.Value, _activeSavePointer)))
                return;
            if (!CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(_playerSaveRequestProcessor,
                out long pointer, out _) || pointer != _activeSavePointer) return;
            action(_playerSaveRequestProcessor, $"{_activeSaveGeneration}:{_runtime.Epoch}:{_runtime.ActiveSlot}:{pointer:X}");
        }
    }

    internal static void WithStableSelectedQuestSave(Action<object, string, int> action)
    {
        WithStableQuestItemSave((processor, identity) => action(processor, identity, _runtime.ActiveSlot!.Value));
    }

    internal static bool TryCaptureGameplayReadyObservation(out CassetteGameplayReadyObservation observation)
    {
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave)
            {
                observation = default;
                return false;
            }

            var identity = new CassetteGameplayReadyIdentity(
                _activeSaveGeneration,
                _runtime.Epoch,
                _runtime.ActiveSlot!.Value,
                _activeSavePointer);
            return _gameplayReady.TryCapture(out observation) && observation.Identity == identity;
        }
    }

    internal static void ObserveGameplayReady(CassetteGameplayReadyObservation observation)
    {
        bool opened;
        lock (Sync)
        {
            opened = _gameplayReady.TryOpen(observation);
        }
        if (!opened) return;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] CASSETTE GAMEPLAY READY epoch={observation.Identity.Epoch} slot={observation.Identity.Slot} pointer=0x{observation.Identity.Pointer:X}; live Hub6 phone bank observed and pending AP ownership retained for reconciliation.");
        RequestUnityReconciliation("live Hub6 gameplay ready");
    }

    internal static void ObserveGameplayReadyMarker(bool phoneBankLive, int markerIdentity)
    {
        lock (Sync) _gameplayReady.ObserveMarker(phoneBankLive, markerIdentity);
    }

    internal static void TickUnity(TimeSpan elapsed)
    {
        CassetteRegularSavePointerJoinSnapshot joinSnapshot;
        lock (Sync) joinSnapshot = _regularSavePointerJoinProbe.Capture();
        if (joinSnapshot.Ready)
        {
            bool joined = CassetteSaveTransactionAdapter.TryMatchRegularSaveSlot(
                joinSnapshot.SaveDataProcessor,
                joinSnapshot.PlayerSaveProcessor,
                out int joinedSlot,
                out IReadOnlyList<CassetteRegularSavePointerEntry> regularEntries,
                out string joinStage);
            bool consume;
            lock (Sync)
            {
                consume = _regularSavePointerJoinProbe.TryConsume(joinSnapshot);
                if (consume && joined)
                {
                    _joinedSaveDataRequestProcessor = joinSnapshot.SaveDataProcessor;
                    _saveIdentity.Signal(joinedSlot, CassetteSaveBoundarySignalKind.Selection, joinSnapshot.PlayerSaveProcessor);
                }
            }
            if (consume)
            {
                string fingerprints = string.Join(",", regularEntries.Select(entry =>
                    $"slot={entry.Slot}:pointer=0x{entry.Pointer:X}:lastPlayUtcTicks={entry.LastPlayDateTimeUtcTicks}"));
                string resultSignature = joined
                    ? $"resolved|slot={joinedSlot}|entries={fingerprints}"
                    : $"unresolved|stage={joinStage}|entries={fingerprints}";
                bool shouldLog;
                lock (Sync) shouldLog = _mostRecentResultLogDeduper.ShouldLog(resultSignature);
                if (shouldLog)
                    Plugin.LoggerInstance?.LogWarning(joined
                        ? $"[SCRC-AP] CASSETTE MOST-RECENT SAVE BOUNDARY RESOLVED uiSlot={joinedSlot} entries='[{fingerprints}]'; awaiting two stable processor observations."
                        : $"[SCRC-AP] CASSETTE MOST-RECENT SAVE BOUNDARY UNRESOLVED stage='{joinStage}' entries='[{fingerprints}]'; epoch remains suspended.");
            }
        }

        CassetteSaveActivation? activation = null;
        CassetteProcessorSaveIdentitySnapshot identitySnapshot;
        lock (Sync) identitySnapshot = _saveIdentity.Capture();
        if (identitySnapshot.Pending)
        {
            bool identityReadable = CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(
                identitySnapshot.Processor, out long selectedStatePointer, out string identityStage);
            lock (Sync)
            {
                if (identitySnapshot.IsCurrent(_saveIdentity.Capture()))
                    activation = _saveIdentity.Observe(
                        identitySnapshot.Generation,
                        identitySnapshot.Processor,
                        identityReadable,
                        selectedStatePointer,
                        identityStage);
            }
            LogIdentityDiagnosticOnChange(identityReadable
                ? $"expectedSlot={identitySnapshot.ExpectedSlot?.ToString() ?? "<none>"} processorPointer=0x{selectedStatePointer:X} outcome='{(activation.HasValue ? "Activated" : "PendingConfirmation")}'"
                : $"expectedSlot={identitySnapshot.ExpectedSlot?.ToString() ?? "<none>"} processorPointer=<unavailable> outcome='{identityStage}'");
        }
        if (activation.HasValue)
        {
            CassetteSaveActivation loaded = activation.Value;
            ActivateLoadedSave(loaded.Generation, loaded.Slot, loaded.Pointer, $"{loaded.Reason} generation={loaded.Generation} pointer=0x{loaded.Pointer:X} build={loaded.IncludesBuild}");
        }

        PollPointerBoundPersistence(elapsed, "UPDATE");

        string? reason = null;
        lock (Sync)
        {
            if (_saveIdentity.Pending) return;
            if (_unityReconciliationRequested)
            {
                reason = _unityReconciliationReason;
                _unityReconciliationRequested = false;
                _unityReconciliationReason = string.Empty;
            }
        }
        if (reason != null) TryReconcile(reason);

        string[] ready;
        lock (Sync)
        {
            if (!_runtime.HasActiveSave ||
                !_gameplayReady.IsOpen(new(
                    _activeSaveGeneration,
                    _runtime.Epoch,
                    _runtime.ActiveSlot!.Value,
                    _activeSavePointer)))
                return;
            if (!_saveSynchronizationReady) return;
            ready = _runtime.Tick(elapsed).ToArray();
        }
        foreach (string song in ready) TryReconcileSong(song, "bounded delayed verification", verificationDue: true);
        CassettePersistenceTargetDiagnosticWave diagnosticWave;
        bool hasDiagnosticWave;
        bool queuedPersistenceWave = false;
        lock (Sync)
        {
            hasDiagnosticWave = _runtime.TryConsumeNewlyVerifiedGrantDiagnosticWave(
                _activeSaveGeneration, _activeSavePointer, out diagnosticWave);
            if (hasDiagnosticWave)
            {
                var identity = new CassettePersistenceAcceptanceIdentity(
                    diagnosticWave.Identity.Generation,
                    diagnosticWave.Identity.Epoch,
                    diagnosticWave.Identity.Slot,
                    diagnosticWave.Identity.StatePointer);
                queuedPersistenceWave = _productionPersistence.TryEnqueue(identity, diagnosticWave.Songs);
            }
        }
        if (queuedPersistenceWave)
        {
            LogPersistenceTargetOwnershipDiagnostic(diagnosticWave);
        }
        TryStartPointerBoundPersistence(elapsed);
    }

    private static void LogPersistenceTargetOwnershipDiagnostic(CassettePersistenceTargetDiagnosticWave wave)
    {
        object? playerProcessor;
        object? retainedSaveDataProcessor;
        long generation;
        long epoch;
        int slot;
        long pointer;
        string[] ownedSongs;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave || wave.Songs.Count == 0 ||
                !wave.IsCurrent(_activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot, _activeSavePointer))
                return;
            playerProcessor = _playerSaveRequestProcessor;
            retainedSaveDataProcessor = _joinedSaveDataRequestProcessor;
            generation = wave.Identity.Generation;
            epoch = wave.Identity.Epoch;
            slot = wave.Identity.Slot;
            pointer = wave.Identity.StatePointer;
            ownedSongs = _runtime.OwnedSongs.ToArray();
        }

        int frame = Time.frameCount;
        int thread = Environment.CurrentManagedThreadId;
        CassettePostLoadDiagnosticState selectedEvidence =
            CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(playerProcessor, ownedSongs);
        bool targetReadable = CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
            playerProcessor,
            retainedSaveDataProcessor,
            slot,
            pointer,
            ownedSongs,
            out CassetteDiskCommitTargetDiagnosticState target,
            out int registryCount,
            out string targetStage);
        bool activeStateReadable = CassetteSaveTransactionAdapter.TryReadPersistenceTargetActiveState(
            playerProcessor,
            pointer,
            out CassettePersistenceTargetActiveState activeState,
            out string activeStateStage);
        bool writeStateReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(
            playerProcessor,
            pointer,
            ownedSongs,
            out CassettePublicWriteDiagnosticState writeState,
            out string writeStateStage);

        bool identityStillCurrent;
        lock (Sync)
        {
            identityStillCurrent = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                wave.IsCurrent(_activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot, _activeSavePointer);
        }

        static string Pointer(bool readable, long value) => readable ? $"0x{value:X}" : "<unavailable>";
        static string Value<T>(bool readable, T value) => readable ? value?.ToString() ?? "<null>" : "<unavailable>";
        bool expectedEntryMatches = target.HasExpectedSlotEntryPointer && target.ExpectedSlotEntryPointer == pointer;
        bool selectedEntryMatches = target.HasSelectedEntryPointer && target.SelectedEntryPointer == pointer;
        string decision = CassettePersistenceTargetDiagnosticDecision.Classify(
            identityStillCurrent, expectedEntryMatches, selectedEntryMatches);
        string activeText = activeStateReadable
            ? $"statePointer=0x{activeState.StatePointer:X} hasUnstaged={activeState.HasUnstagedChanges} " +
              $"hasChanges={activeState.HasChanges} requiresWrite={activeState.RequiresWriteToDisk} " +
              $"defaultBundlePresent={activeState.DefaultBundlePresent} defaultBundleChangeCount={activeState.DefaultBundleChangeCount}"
            : $"statePointer=<unavailable> hasUnstaged=<unavailable> hasChanges=<unavailable> " +
              $"requiresWrite=<unavailable> defaultBundlePresent=<unavailable> defaultBundleChangeCount=<unavailable> " +
              $"activeStage='{activeStateStage}'";
        string writeText = writeStateReadable
            ? $"redundancyIndex={writeState.RedundancyBundleIndex} redundancyRevision={writeState.RedundancyBundleRevision} " +
              $"lastSuccess={CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(writeState.WriteState.LastSuccessTime)} " +
              $"lastFailure={CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(writeState.WriteState.LastFailureTime)} " +
              $"failureReason='{writeState.WriteState.FailureReason ?? "<null>"}' " +
              $"currentGameTime={CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(writeState.CurrentGameTime)}"
            : $"redundancyIndex=<unavailable> redundancyRevision=<unavailable> lastSuccess=<unavailable> " +
              $"lastFailure=<unavailable> failureReason=<unavailable> currentGameTime=<unavailable> writeStage='{writeStateStage}'";

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE PERSISTENCE TARGET SNAPSHOT generation={generation} epoch={epoch} slot={slot} " +
            $"expectedPointer=0x{pointer:X} frame={frame} thread={thread} identityCurrent={identityStillCurrent} " +
            $"newlyVerified=[{string.Join(",", wave.Songs)}] ownedSongs=[{string.Join(",", ownedSongs)}] " +
            $"playerProcessorPointer={Pointer(target.HasPlayerProcessorPointer, target.PlayerProcessorPointer)} " +
            $"registeredPersistProcessorPointer={Pointer(target.HasRegisteredPersistProcessorPointer, target.RegisteredPersistProcessorPointer)} " +
            $"retainedSaveDataProcessorPointer={Pointer(target.HasRetainedSaveDataProcessorPointer, target.RetainedSaveDataProcessorPointer)} " +
            $"saveDataStatePointer={Pointer(target.HasSaveDataStatePointer, target.SaveDataStatePointer)} registryCount={registryCount} " +
            $"numericSelectedSlot={Value(target.HasSelectedPlayerSaveSlot, target.SelectedPlayerSaveSlot)} " +
            $"expectedEntryPointer={Pointer(target.HasExpectedSlotEntryPointer, target.ExpectedSlotEntryPointer)} " +
            $"selectedEntryPointer={Pointer(target.HasSelectedEntryPointer, target.SelectedEntryPointer)} " +
            $"targetReadable={targetReadable} targetStage='{targetStage}' decision='{decision}' {activeText} {writeText} " +
            CassetteSaveTransactionAdapter.FormatCassettePostLoadDiagnostic(selectedEvidence));
    }

    private static void TryStartPointerBoundPersistence(TimeSpan elapsed)
    {
        object? playerProcessor;
        object? retainedSaveDataProcessor;
        string[] ownedSongs;
        CassettePersistenceAcceptanceIdentity identity;
        CassettePersistenceQueuedWave wave;
        bool routingCompatible;
        bool gameplayReady;
        bool waveCurrent;
        lock (Sync)
        {
            identity = new(_activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot ?? -1, _activeSavePointer);
            routingCompatible = _slotDataSynchronized && Enabled;
            if (!routingCompatible || _productionPersistence.Active ||
                !_productionPersistence.TryPeek(identity, out wave))
                return;
            waveCurrent = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                wave.Identity == identity;
            gameplayReady = waveCurrent && _gameplayReady.IsOpen(
                new(identity.Generation, identity.Epoch, identity.Slot, identity.Pointer));
            playerProcessor = _playerSaveRequestProcessor;
            retainedSaveDataProcessor = _joinedSaveDataRequestProcessor;
            if (!gameplayReady || retainedSaveDataProcessor == null ||
                !_persistencePoll.TryBegin(identity, wave.WaveId, elapsed))
                return;
            ownedSongs = _runtime.OwnedSongs.ToArray();
        }

        CassettePostLoadDiagnosticState selected =
            CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(playerProcessor, ownedSongs);
        bool targetReadable = CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
            playerProcessor, retainedSaveDataProcessor, identity.Slot, identity.Pointer, ownedSongs,
            out CassetteDiskCommitTargetDiagnosticState target, out _, out string targetStage);
        bool activeReadable = CassetteSaveTransactionAdapter.TryReadPersistenceTargetActiveState(
            playerProcessor, identity.Pointer,
            out CassettePersistenceTargetActiveState activeState, out string activeStage);
        bool writeReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(
            playerProcessor, identity.Pointer, ownedSongs,
            out CassettePublicWriteDiagnosticState writeState, out string writeStage);

        bool selectionValid = selected.SelectionValid.Readable && selected.SelectionValid.Value;
        bool selectedPointerMatches = selected.SelectedStatePointer.Readable &&
            selected.SelectedStatePointer.Value == identity.Pointer;
        bool registeredRetainedMatch = CassettePersistenceAcceptanceOwnershipProof.IsExactPointerBoundOwner(
            target.HasRegisteredPersistProcessorPointer,
            target.RegisteredPersistProcessorPointer,
            target.HasRetainedSaveDataProcessorPointer,
            target.RetainedSaveDataProcessorPointer,
            target.HasExpectedSlotEntryPointer,
            target.ExpectedSlotEntryPointer,
            identity.Pointer) &&
            CassettePersistenceAcceptanceOwnershipProof.IsKnownExpectedSlotOnlyDiagnostic(
                targetReadable,
                targetStage,
                target.HasSelectedPlayerSaveSlot,
                target.HasSelectedEntryPointer,
                target.HasSelected);
        bool expectedEntryMatches = target.HasExpectedSlotEntryPointer &&
            target.ExpectedSlotEntryPointer == identity.Pointer;
        bool selectedEntryMatches = target.HasSelectedEntryPointer &&
            target.SelectedEntryPointer == identity.Pointer;
        string decision = CassettePersistenceTargetDiagnosticDecision.Classify(
            waveCurrent, expectedEntryMatches, selectedEntryMatches);
        bool statusesRetained = target.HasPlayer && wave.Songs.All(song =>
            target.Player.EffectiveStatuses.TryGetValue(song, out string? status) &&
            string.Equals(status, "HAVE_IN_BAG", StringComparison.Ordinal));

        var evidence = new CassettePersistenceAcceptanceEligibilityEvidence(
            OptInEnabled: true,
            routingCompatible,
            gameplayReady,
            waveCurrent,
            selectionValid,
            selectedPointerMatches,
            registeredRetainedMatch,
            expectedEntryMatches,
            string.Equals(decision, "pointer-bound-public-state-candidate", StringComparison.Ordinal),
            activeReadable && activeState.DefaultBundlePresent,
            activeReadable ? activeState.DefaultBundleChangeCount : 0,
            statusesRetained,
            activeReadable && activeState.HasUnstagedChanges,
            activeReadable && activeState.HasChanges,
            !activeReadable || activeState.RequiresWriteToDisk);
        if (!CassettePersistenceAcceptanceEligibility.Evaluate(evidence, out string gateStage))
        {
            LogPersistenceDeferredOnce(wave, gateStage);
            return;
        }
        if (!writeReadable)
        {
            LogPersistenceDeferredOnce(wave, writeStage);
            return;
        }
        if (!CassetteSaveTransactionAdapter.TryPreparePointerBoundPersistenceInvocation(
                playerProcessor, identity.Pointer,
                out CassettePointerBoundPersistenceInvocationPlan plan, out string planStage))
        {
            LogPersistenceDeferredOnce(wave, planStage);
            return;
        }

        var baseline = new CassettePersistenceAcceptanceBaseline(
            writeState.WriteState,
            writeState.RedundancyBundleIndex,
            writeState.RedundancyBundleRevision,
            wave.Songs.ToArray());
        string preDetail =
            $"decision='{decision}' songs=[{string.Join(",", baseline.Songs)}] " +
            $"targetStage='{targetStage}' activeStage='{activeStage}' writeStage='{writeStage}' " +
            FormatPersistenceAcceptanceState(writeState);
        bool stillCurrent;
        lock (Sync)
        {
            stillCurrent = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                identity == new CassettePersistenceAcceptanceIdentity(
                    _activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot ?? -1, _activeSavePointer) &&
                _productionPersistence.TryPeek(identity, out CassettePersistenceQueuedWave currentWave) &&
                currentWave.WaveId == wave.WaveId;
        }
        if (!stillCurrent) return;
        CassetteProductionPersistenceStartOutcome startOutcome = _productionPersistence.TryInvoke(
            identity,
            wave.WaveId,
            eligible: true,
            baseline,
            preDetail,
            onPrepared: DrainPersistenceMarkers,
            invokeAdapter: () =>
            {
                CassettePointerBoundPersistenceInvocationResult invocation =
                    CassetteSaveTransactionAdapter.InvokePointerBoundPersistence(plan, out string invokeStage);
                return new(
                    invocation == CassettePointerBoundPersistenceInvocationResult.Invoked,
                    invokeStage,
                    $"outcome='{invocation}' stage='{invokeStage}' " +
                    "order='PersistAllChangesInBundle(DEFAULT/1),RequestUrgentWriteToDisk'");
            });
        DrainPersistenceMarkers();
        if (startOutcome != CassetteProductionPersistenceStartOutcome.Invoked) return;
        PollPointerBoundPersistence(TimeSpan.Zero, "IMMEDIATE POST");
    }

    internal static void ObservePersistenceAcceptanceWriteCompletedEvent(object? nativeEvent)
    {
        if (!CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEvent(
                nativeEvent, out int slot, out bool succeeded, out string? failureReason, out string stage))
            return;
        CassettePersistenceAcceptanceAttempt attempt;
        lock (Sync)
        {
            if (!_acceptanceDiagnosticsEnabled || !_productionPersistence.Active) return;
            attempt = _productionPersistence.Attempt;
        }
        _productionPersistence.ObserveWriteCompletedEvent(
            attempt,
            slot,
            succeeded,
            $"slot={slot} succeeded={succeeded} failureReason='{failureReason ?? "<null>"}' stage='{stage}'");
        DrainPersistenceMarkers();
    }

    private static void PollPointerBoundPersistence(TimeSpan elapsed, string phase)
    {
        CassettePersistenceAcceptanceAttempt attempt;
        CassettePersistenceAcceptanceBaseline baseline;
        object? playerProcessor;
        object? retainedSaveDataProcessor;
        lock (Sync)
        {
            if (!_productionPersistence.Active || !_productionPersistence.Invoked)
                return;
            attempt = _productionPersistence.Attempt;
            baseline = _productionPersistence.Baseline;
            playerProcessor = _playerSaveRequestProcessor;
            retainedSaveDataProcessor = _joinedSaveDataRequestProcessor;
        }

        CassettePostLoadDiagnosticState selected =
            CassetteSaveTransactionAdapter.ReadCassettePostLoadDiagnostic(playerProcessor, baseline.Songs);
        bool targetReadable = CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
            playerProcessor, retainedSaveDataProcessor, attempt.Slot, attempt.Pointer, baseline.Songs,
            out CassetteDiskCommitTargetDiagnosticState target, out _, out string targetStage);
        bool writeReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(
            playerProcessor, attempt.Pointer, baseline.Songs,
            out CassettePublicWriteDiagnosticState writeState, out string writeStage);
        bool selectedMatches = selected.SelectionValid.Readable && selected.SelectionValid.Value &&
            selected.SelectedStatePointer.Readable && selected.SelectedStatePointer.Value == attempt.Pointer;
        bool ownershipMatches = CassettePersistenceAcceptanceOwnershipProof.IsExactPointerBoundOwner(
            target.HasRegisteredPersistProcessorPointer,
            target.RegisteredPersistProcessorPointer,
            target.HasRetainedSaveDataProcessorPointer,
            target.RetainedSaveDataProcessorPointer,
            target.HasExpectedSlotEntryPointer,
            target.ExpectedSlotEntryPointer,
            attempt.Pointer) &&
            CassettePersistenceAcceptanceOwnershipProof.IsKnownExpectedSlotOnlyDiagnostic(
                targetReadable,
                targetStage,
                target.HasSelectedPlayerSaveSlot,
                target.HasSelectedEntryPointer,
                target.HasSelected);
        bool statusesRetained = writeReadable && baseline.Songs.All(song =>
            writeState.Statuses.TryGetValue(song, out string? status) &&
            string.Equals(status, "HAVE_IN_BAG", StringComparison.Ordinal));
        CassettePersistenceAcceptanceIdentity currentIdentity;
        lock (Sync)
            currentIdentity = new(_activeSaveGeneration, _runtime.Epoch, _runtime.ActiveSlot ?? -1, _activeSavePointer);
        bool coherentReadable = selectedMatches && ownershipMatches && writeReadable;
        string stateDetail = writeReadable
            ? FormatPersistenceAcceptanceState(writeState)
            : "state=<unavailable>";
        _productionPersistence.Observe(
            attempt, currentIdentity, writeState, statusesRetained, coherentReadable,
            elapsed, phase, targetStage, writeStage, stateDetail);
        DrainPersistenceMarkers();
    }

    private static string FormatPersistenceAcceptanceState(CassettePublicWriteDiagnosticState state) =>
        $"statePointer=0x{state.StatePointer:X} hasUnstaged={state.HasUnstagedChanges} " +
        $"hasChanges={state.WriteState.HasChanges} requiresWrite={state.WriteState.RequiresWriteToDisk} " +
        $"redundancyIndex={state.RedundancyBundleIndex} redundancyRevision={state.RedundancyBundleRevision} " +
        $"lastSuccess={CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(state.WriteState.LastSuccessTime)} " +
        $"lastFailure={CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(state.WriteState.LastFailureTime)} " +
        $"failureReason='{state.WriteState.FailureReason ?? "<null>"}'";

    private static void LogPersistenceDeferredOnce(
        CassettePersistenceQueuedWave wave,
        string stage)
    {
        if (!_productionPersistence.ShouldLogDeferred(wave.WaveId)) return;
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] CASSETTE PERSISTENCE DEFERRED wave={wave.WaveId} " +
            $"generation={wave.Identity.Generation} epoch={wave.Identity.Epoch} slot={wave.Identity.Slot} " +
            $"pointer=0x{wave.Identity.Pointer:X} stage='{stage}'; exact verified wave remains queued.");
    }

    private static void DrainPersistenceMarkers() =>
        _persistenceMarkerEmitter.Drain(
            TryTakePersistenceMarker,
            EmitPersistenceMarker);

    private static bool TryTakePersistenceMarker(
        out CassettePersistenceAcceptanceMarkerRecord marker)
        => _productionPersistence.TryDequeueMarker(out marker);

    private static void EmitPersistenceMarker(
        CassettePersistenceAcceptanceMarkerRecord marker) =>
        LogPersistence(marker.Marker, marker.Attempt, marker.Detail, marker.Sequence);

    private static void LogPersistence(
        string marker,
        CassettePersistenceAcceptanceAttempt attempt,
        string detail,
        long sequence)
    {
        string label = marker switch
        {
            "PRE" => "CASSETTE PERSISTENCE PRE",
            "INVOKED" => "CASSETTE PERSISTENCE INVOKED",
            "IMMEDIATE POST" => "CASSETTE PERSISTENCE IMMEDIATE POST",
            "EVENT" => "CASSETTE PERSISTENCE ACCEPTANCE EVENT",
            "VERIFIED" => "CASSETTE PERSISTENCE VERIFIED",
            "FAILED" => "CASSETTE PERSISTENCE FAILED",
            "TIMEOUT" => "CASSETTE PERSISTENCE TIMEOUT",
            "CANCELLED" => "CASSETTE PERSISTENCE CANCELLED",
            _ => $"CASSETTE PERSISTENCE {marker}",
        };
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] {label} sequence={sequence} attempt={attempt.Id} " +
            $"generation={attempt.Generation} epoch={attempt.Epoch} slot={attempt.Slot} pointer=0x{attempt.Pointer:X} " +
            $"frame={Time.frameCount} thread={Environment.CurrentManagedThreadId} {detail}");
    }

    private static void LogIdentityDiagnosticOnChange(string diagnostic)
    {
        lock (Sync)
        {
            if (string.Equals(_lastIdentityDiagnostic, diagnostic, StringComparison.Ordinal)) return;
            _lastIdentityDiagnostic = diagnostic;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE IDENTITY {diagnostic}.");
    }

    private static bool TryReadNativeStatus(string nativeSong, out string? status, out string detail)
    {
        status = null; detail = string.Empty;
        try
        {
            Assembly? asm = ReflectionUtil.GameAssembly;
            Type? enquiries = asm?.GetType("SongCassetteEnquiries", false, false);
            Type? songType = asm?.GetType("ePlayableSong", false, false);
            if (enquiries == null || songType == null || !songType.IsEnum) { detail = "cassette enquiries or song enum unavailable"; return false; }
            object song = Enum.Parse(songType, nativeSong, false);
            MethodInfo? method = enquiries.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetSongCassetteStatus" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == songType);
            if (method == null) { detail = "GetSongCassetteStatus unavailable"; return false; }
            object? raw = ReflectionUtil.UnwrapNullable(method.Invoke(null, new[] { song })); status = raw?.ToString();
            detail = $"selected save returned {status ?? "<null>"}"; return !string.IsNullOrWhiteSpace(status);
        }
        catch (Exception ex) { detail = ex.GetBaseException().Message; return false; }
    }

    private static bool TryWriteMember(object obj, string name, object value)
    {
        try { PropertyInfo? p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (p?.CanWrite == true) { p.SetValue(obj, value); return true; } } catch { }
        try { FieldInfo? f = obj.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance); if (f != null) { f.SetValue(obj, value); return true; } } catch { }
        return false;
    }

    private static bool ReadSlotBool(Dictionary<string, object>? slotData, string key)
    {
        if (slotData == null || !slotData.TryGetValue(key, out object? raw) || raw == null) return false;
        if (raw is bool b) return b; if (raw is long l) return l != 0; if (raw is int i) return i != 0;
        return bool.TryParse(raw.ToString(), out bool parsed) && parsed;
    }
}


internal static class PlantPipesRandomization
{
    public const string ItemName = "Plant Pipes";
    public const string SourceLocationName = "Roots - Level 3 - Plant Pipes Pickup";
    private static string ActiveSourceLocationName = SourceLocationName;
    public const string NativeAbilityFlag = "WEED_KILLER_ABILITY";
    public const string NativeSourceMarkerFlag = "LEVEL_07_WK_ABILITY_EARNED";
    public const string SourceRoomId = "GameRoom_07";

    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static bool _slotDataSynchronized;
    private static bool _compatible;
    private static int _receivedCount;
    private static PlantPipesRuntime _runtime = new(new NativeAdapter());
    private static string _lastRuntimeOutcome = string.Empty;
    private static bool _processorConstructionFailureLogged;

    [ThreadStatic]
    private static bool _applyingNativeGrant;

    public static bool Enabled { get; private set; }
    public static string ImplementationVersion { get; private set; } = string.Empty;

    public static void Configure()
    {
        lock (Sync)
        {
            ActiveSourceLocationName = SourceLocationName;
            Enabled = false;
            ImplementationVersion = string.Empty;
            _playerSaveRequestProcessor = null;
            _slotDataSynchronized = false;
            _compatible = false;
            _receivedCount = 0;
            _runtime = new PlantPipesRuntime(new NativeAdapter());
            _lastRuntimeOutcome = string.Empty;
            _processorConstructionFailureLogged = false;
        }
    }

    public static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        bool requested = false;
        if (slotData != null &&
            slotData.TryGetValue("randomize_plant_pipes", out object? rawEnabled) &&
            rawEnabled != null)
        {
            if (rawEnabled is bool b)
                requested = b;
            else
                bool.TryParse(rawEnabled.ToString(), out requested);
        }

        bool repairClaimed = ReadSlotBool(slotData, "plant_pipes_durable_reconciliation");
        bool repairContractValid = !repairClaimed ||
                                   RepairCompatibilityPolicy.IsCompatible(implementation, repairClaimed);

        lock (Sync)
        {
            // A notification-only client update must still work with the old seed's datapackage.
            ActiveSourceLocationName = slotData != null &&
                slotData.TryGetValue("plant_pipes_source_location", out object? sourceName) &&
                string.Equals(sourceName?.ToString(), SourceLocationName, StringComparison.Ordinal)
                    ? SourceLocationName : "Roots - Level 3 - Frog and Hippo";
            ImplementationVersion = implementation;
            Enabled = requested &&
                      implementation.StartsWith("area-routing-plant-pipes-0.15", StringComparison.OrdinalIgnoreCase);
            _slotDataSynchronized = true;
            _compatible = Enabled && repairContractValid;
            _runtime.Configure(_slotDataSynchronized, _compatible);
        }

        Plugin.LoggerInstance?.LogWarning(
            Enabled
                ? $"[SCRC-AP] ROOTS PLANT PIPES RANDOMIZATION ENABLED implementation='{implementation}' item='{ItemName}' source='{ActiveSourceLocationName}' durableRepair={repairClaimed}. Level 3 Frog/Hippo source check is live; vanilla '{NativeAbilityFlag}' is suppressed there while '{NativeSourceMarkerFlag}' remains native, and AP ownership is reconciled against the selected save."
                : $"[SCRC-AP] ROOTS PLANT PIPES RANDOMIZATION disabled implementation='{implementation}' requested={requested} repairContractValid={repairContractValid}; Plant Pipes remain vanilla for this seed.");
    }

    public static bool TryApplyItem(string itemName)
    {
        if (!string.Equals(itemName, ItemName, StringComparison.OrdinalIgnoreCase))
            return false;

        int count;
        lock (Sync)
        {
            count = ++_receivedCount;
            _runtime.NoteReceivedCount(count);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS PLANT PIPES RECEIVED item='{ItemName}' receivedCount={count} routingEnabled={Enabled}. Native ability flag '{NativeAbilityFlag}' will be reconciled when the selected save is readable.");
        return true;
    }

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null || !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;

        lock (Sync)
            _playerSaveRequestProcessor = instance;
    }

    public static void TryFlushPendingNativeGrant()
    {
        if (_applyingNativeGrant)
            return;

        string previous;
        string current;
        lock (Sync)
        {
            _runtime.Configure(_slotDataSynchronized, _compatible);
            _runtime.NoteReceivedCount(_receivedCount);
            previous = _lastRuntimeOutcome;
            _runtime.OnLifecyclePoint("lifecycle");
            current = _runtime.LastOutcome;
            _lastRuntimeOutcome = current;
        }

        if (!string.Equals(previous, current, StringComparison.Ordinal))
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] ROOTS PLANT PIPES reconciliation result={current} receivedCount={_receivedCount} room='{DeveloperHarness.CurrentRoomId}'.");
        }
    }

    public static void OnLevelResultApplied(string level)
    {
        if (!string.Equals(level, "Level_08", StringComparison.OrdinalIgnoreCase))
            return;
        lock (Sync)
            _runtime.OnLifecyclePoint("level-result-applied:Level_08");
    }

    public static void OnLevelResultPersisted(string level)
    {
        if (!string.Equals(level, "Level_08", StringComparison.OrdinalIgnoreCase))
            return;
        lock (Sync)
            _runtime.OnLifecyclePoint("level-result-persisted:Level_08");
    }

    internal static void TickPending(TimeSpan elapsed)
    {
        string previous;
        string current;
        lock (Sync)
        {
            previous = _lastRuntimeOutcome;
            _runtime.TickPending(elapsed, "bounded retry");
            current = _runtime.LastOutcome;
            _lastRuntimeOutcome = current;
        }

        if (!string.Equals(previous, current, StringComparison.Ordinal))
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] ROOTS PLANT PIPES retry result={current} room='{DeveloperHarness.CurrentRoomId}'.");
        }
    }

    public static bool ShouldSuppressFrogHippoVanillaGrant(object request, string flag)
    {
        if (_applyingNativeGrant ||
            !Enabled ||
            !string.Equals(DeveloperHarness.CurrentRoomId, SourceRoomId, StringComparison.Ordinal) ||
            !string.Equals(flag, NativeAbilityFlag, StringComparison.OrdinalIgnoreCase))
            return false;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        if (!value)
            return false;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS PLANT PIPES VANILLA GRANT SUPPRESSED flag='{flag}' room='{DeveloperHarness.CurrentRoomId}'. Frog/Hippo's source/story sequence continues; the actual Plant Pipes ability must come from Archipelago.");
        return true;
    }

    public static void RecordFrogHippoSourceCollected(object request, string flag)
    {
        if (!Enabled ||
            !string.Equals(DeveloperHarness.CurrentRoomId, SourceRoomId, StringComparison.Ordinal) ||
            !string.Equals(flag, NativeSourceMarkerFlag, StringComparison.OrdinalIgnoreCase))
            return;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        if (!value)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS PLANT PIPES SOURCE AP CHECK flag='{flag}' location='{ActiveSourceLocationName}'. Native Frog/Hippo source marker retained; native Plant Pipes ability grant is randomized.");
        Plugin.AP?.QueueLocation(ActiveSourceLocationName);
    }

    internal static string ReadDiagnosticState()
    {
        bool abilityReadable = RootsBucketRandomization.TryReadProgressionFlag(NativeAbilityFlag, out bool ability);
        bool sourceReadable = RootsBucketRandomization.TryReadProgressionFlag(NativeSourceMarkerFlag, out bool source);
        bool owned;
        bool pending;
        bool applied;
        bool processor;
        lock (Sync)
        {
            owned = _receivedCount > 0;
            pending = _runtime.HasPendingRetry || _runtime.LastOutcome is "save-unavailable" or "processor-unavailable" or "verification-pending";
            applied = _runtime.LastOutcome is "grant-submitted" or "verified-owned";
            processor = _playerSaveRequestProcessor != null;
        }

        return $"room='{DeveloperHarness.CurrentRoomId}' selectedSaveReadable={abilityReadable && sourceReadable} " +
               $"{NativeAbilityFlag}={(abilityReadable ? ability.ToString() : "<unavailable>")} " +
               $"{NativeSourceMarkerFlag}={(sourceReadable ? source.ToString() : "<unavailable>")} " +
               $"apOwned={owned} pending={pending} appliedThisProcess={applied} processorAvailable={processor}";
    }

    private static bool ReadSlotBool(Dictionary<string, object>? slotData, string key)
    {
        if (slotData == null || !slotData.TryGetValue(key, out object? raw) || raw == null)
            return false;
        if (raw is bool value)
            return value;
        return bool.TryParse(raw.ToString(), out bool parsed) && parsed;
    }

    private static bool EnsureProcessorAvailable()
    {
        lock (Sync)
        {
            if (_playerSaveRequestProcessor != null)
                return true;
        }

        if (!RootsBucketRandomization.TryReadProgressionFlag(NativeAbilityFlag, out _))
            return false;

        try
        {
            Type? processorType = ReflectionUtil.GameAssembly == null
                ? null
                : ReflectionUtil.SafeGetTypes(ReflectionUtil.GameAssembly)
                    .FirstOrDefault(t => string.Equals(t.Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal));
            ConstructorInfo? constructor = processorType?.GetConstructors(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(c => c.GetParameters().Length == 0);
            object? processor = constructor?.Invoke(Array.Empty<object>());
            if (processor == null)
                throw new InvalidOperationException("parameterless PlayerSaveRequestProcessor constructor unavailable");

            lock (Sync)
                _playerSaveRequestProcessor = processor;
            RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(processor);
            CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(processor);
            GarageCartridgeAccess.CapturePlayerSaveRequestProcessor(processor);
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] ROOTS PLANT PIPES constructed stateless PlayerSaveRequestProcessor after selected-save enquiries became readable.");
            return true;
        }
        catch (Exception ex)
        {
            bool log;
            lock (Sync)
            {
                log = !_processorConstructionFailureLogged;
                _processorConstructionFailureLogged = true;
            }
            if (log)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] ROOTS PLANT PIPES processor construction unavailable: {ex.GetBaseException().Message}");
            }
            return false;
        }
    }

    private sealed class NativeAdapter : IPlantPipesNativeAdapter
    {
        public bool SaveAvailable =>
            RootsBucketRandomization.TryReadProgressionFlag(NativeAbilityFlag, out _);

        public bool ProcessorAvailable => EnsureProcessorAvailable();

        public bool TryReadOwned(out bool owned) =>
            RootsBucketRandomization.TryReadProgressionFlag(NativeAbilityFlag, out owned);

        public bool TryApply(out string detail)
        {
            object? processor;
            lock (Sync)
                processor = _playerSaveRequestProcessor;
            if (processor == null && !EnsureProcessorAvailable())
            {
                detail = "PlayerSaveRequestProcessor unavailable.";
                return false;
            }
            lock (Sync)
                processor = _playerSaveRequestProcessor;
            if (processor == null)
            {
                detail = "PlayerSaveRequestProcessor unavailable after construction.";
                return false;
            }
            return TrySubmitProgressionFlag(processor, NativeAbilityFlag, true, out detail);
        }
    }

    private static bool TrySubmitProgressionFlag(object processor, string flagName, bool value, out string detail)
    {
        detail = string.Empty;
        try
        {
            Assembly? gameAssembly = ReflectionUtil.GameAssembly;
            Type? requestType = gameAssembly?.GetType("RecordGameProgressionInSaveDataRequest", throwOnError: false, ignoreCase: false);
            if (requestType == null)
            {
                detail = "RecordGameProgressionInSaveDataRequest type not found.";
                return false;
            }

            Type? flagType = requestType.GetProperty(
                    "Flag", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.PropertyType
                ?? requestType.GetField(
                    "_Flag_k__BackingField", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType;
            if (flagType == null || !flagType.IsEnum)
            {
                detail = "progression flag enum type not found.";
                return false;
            }

            object flagValue;
            try { flagValue = Enum.Parse(flagType, flagName, ignoreCase: false); }
            catch (Exception ex)
            {
                detail = $"enum value '{flagName}' unavailable: {ex.GetBaseException().Message}";
                return false;
            }

            object? request = null;
            string ctorDescription = string.Empty;
            foreach (ConstructorInfo ctor in requestType.GetConstructors(
                         BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                     .Where(c => !c.GetParameters().Any(p => p.ParameterType == typeof(IntPtr)))
                     .OrderBy(c => c.GetParameters().Length))
            {
                ParameterInfo[] ps;
                try { ps = ctor.GetParameters(); }
                catch { continue; }

                var args = new object?[ps.Length];
                for (int i = 0; i < ps.Length; i++)
                {
                    Type pt = ps[i].ParameterType;
                    if (pt == flagType)
                        args[i] = flagValue;
                    else if (pt == typeof(bool))
                        args[i] = value;
                    else if (pt.IsEnum)
                    {
                        try { args[i] = Enum.Parse(pt, "INVALID", ignoreCase: true); }
                        catch { args[i] = Activator.CreateInstance(pt); }
                    }
                    else if (pt.IsValueType)
                        args[i] = Activator.CreateInstance(pt);
                    else
                        args[i] = null;
                }

                try
                {
                    request = ctor.Invoke(args);
                    ctorDescription = $"ctor({string.Join(",", ps.Select(p => p.ParameterType.Name))})";
                    if (request != null)
                        break;
                }
                catch { request = null; }
            }

            if (request == null)
            {
                try
                {
                    request = Activator.CreateInstance(requestType, nonPublic: true);
                    ctorDescription = "Activator.CreateInstance";
                }
                catch (Exception ex)
                {
                    detail = $"could not construct request: {ex.GetBaseException().Message}";
                    return false;
                }
            }

            TryWriteMember(request, "Flag", flagValue);
            TryWriteMember(request, "_Flag_k__BackingField", flagValue);
            TryWriteMember(request, "Value", value);
            TryWriteMember(request, "_Value_k__BackingField", value);

            MethodInfo? process = processor.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (!string.Equals(m.Name, "ProcessRequest", StringComparison.Ordinal))
                        return false;
                    ParameterInfo[] ps;
                    try { ps = m.GetParameters(); }
                    catch { return false; }
                    return ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(request);
                });

            if (process == null)
            {
                detail = "matching PlayerSaveRequestProcessor.ProcessRequest overload not found.";
                return false;
            }

            try
            {
                _applyingNativeGrant = true;
                process.Invoke(processor, new[] { request });
            }
            finally
            {
                _applyingNativeGrant = false;
            }

            detail = $"via {ctorDescription}";
            return true;
        }
        catch (Exception ex)
        {
            detail = ex.GetBaseException().ToString();
            return false;
        }
    }

    private static bool TryWriteMember(object obj, string name, object value)
    {
        Type t = obj.GetType();
        try
        {
            PropertyInfo? p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.CanWrite)
            {
                p.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        try
        {
            FieldInfo? f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (f != null)
            {
                f.SetValue(obj, value);
                return true;
            }
        }
        catch { }

        return false;
    }
}

internal static class BottomHudDiagnostic
{
    private const string RoomId = "GameRoom_Hub2";
    private const string ControlPath =
        "Root/GameRoom_Hub2_Logic/Objects/AmProContainer/AmProRobot";
    private const string NativeControlType = "DifficultyToggler";

    private static readonly object Sync = new();
    private static readonly Queue<string> PendingLifecycles = new();
    private static readonly HashSet<string> PendingKeys = new(StringComparer.OrdinalIgnoreCase);
    private static BottomHudDiagnosticPolicy _policy = new();
    private static bool _enabled;

    internal static void Configure()
    {
        lock (Sync)
        {
            _policy = new BottomHudDiagnosticPolicy();
            PendingLifecycles.Clear();
            PendingKeys.Clear();
            _enabled = false;
            EnqueueLocked("after-restart");
        }
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? raw)
            ? raw?.ToString() ?? string.Empty
            : string.Empty;
        lock (Sync)
        {
            _enabled = implementation.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase);
            if (_enabled)
                EnqueueLocked("direct-start");
        }
    }

    internal static void OnLevelResultPersisted(string level)
    {
        if (string.Equals(level, "Level_05", StringComparison.OrdinalIgnoreCase))
            Enqueue("after-level-1");
    }

    internal static void OnRoomTransition(string roomId)
    {
        if (string.Equals(roomId, RoomId, StringComparison.OrdinalIgnoreCase))
            Enqueue("after-reload");
    }

    internal static void Tick()
    {
        lock (Sync)
        {
            if (!_enabled || PendingLifecycles.Count == 0)
                return;
        }
        if (!string.Equals(DeveloperHarness.CurrentRoomId, RoomId, StringComparison.OrdinalIgnoreCase))
            return;

        GameObject? controlObject = GameObject.Find(ControlPath);
        string lifecycle;
        lock (Sync)
            lifecycle = PendingLifecycles.Peek();
        BottomHudDiagnosticDecision decision;
        lock (Sync)
            decision = _policy.Decide(RoomId, lifecycle, controlObject != null);
        if (decision == BottomHudDiagnosticDecision.SkipDuplicate)
        {
            lock (Sync)
            {
                PendingLifecycles.Dequeue();
                PendingKeys.Remove(lifecycle);
            }
            return;
        }
        if (decision != BottomHudDiagnosticDecision.Emit || controlObject == null)
            return;

        Component? control = null;
        string[] componentTypes;
        try
        {
            Component[] components = controlObject.GetComponents<Component>();
            componentTypes = components
                .Where(component => component != null)
                .Select(component => component.GetType().FullName ?? component.GetType().Name)
                .ToArray();
            control = components.FirstOrDefault(component =>
                component != null &&
                (component.GetType().Name.Contains(NativeControlType, StringComparison.OrdinalIgnoreCase) ||
                 component.GetType().FullName?.Contains(NativeControlType, StringComparison.OrdinalIgnoreCase) == true));
        }
        catch
        {
            componentTypes = Array.Empty<string>();
        }

        string interaction = ReadBoolResult(control, "IsInteractionEnabled", "InteractionEnabled");
        object? required = control == null ? null : ReflectionUtil.ReadMember(control, "RequiredCondition");
        object? blocked = control == null ? null : ReflectionUtil.ReadMember(control, "BlockCondition");
        string phase = control == null
            ? "<unavailable>"
            : ReflectionUtil.ReadMember(control, "Phase")?.ToString() ?? "<unavailable>";
        bool gateReadable = RootsBucketRandomization.TryReadProgressionFlag(
            RootsStartupBootstrap.GateOpenedFlag, out bool gateOwned);
        bool difficultyReadable = RootsBucketRandomization.TryReadProgressionFlag(
            RootsStartupBootstrap.DifficultyCompleteFlag, out bool difficultyOwned);

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD SNAPSHOT lifecycle='{lifecycle}' room='{RoomId}' " +
            $"nativeControlType='{NativeControlType}' path='{ControlPath}' " +
            $"managedProxy='{control?.GetType().FullName ?? "<unresolved>"}' " +
            $"components='{string.Join("|", componentTypes)}' interactionEnabled={interaction} phase={phase} " +
            $"requiredCondition={DescribeCondition(required)} blockCondition={DescribeCondition(blocked)} " +
            $"{RootsStartupBootstrap.GateOpenedFlag}={(gateReadable ? gateOwned.ToString() : "<unavailable>")} " +
            $"{RootsStartupBootstrap.DifficultyCompleteFlag}={(difficultyReadable ? difficultyOwned.ToString() : "<unavailable>")} " +
            "readOnly=True mutationRequested=False.");

        lock (Sync)
        {
            PendingLifecycles.Dequeue();
            PendingKeys.Remove(lifecycle);
        }
    }

    private static void Enqueue(string lifecycle)
    {
        lock (Sync)
        {
            if (_enabled)
                EnqueueLocked(lifecycle);
        }
    }

    private static void EnqueueLocked(string lifecycle)
    {
        if (PendingKeys.Add(lifecycle))
            PendingLifecycles.Enqueue(lifecycle);
    }

    private static string ReadBoolResult(object? target, string methodName, string memberName)
    {
        if (target == null)
            return "<unavailable>";
        try
        {
            MethodInfo? method = target.GetType().GetMethod(
                methodName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                Type.EmptyTypes,
                null);
            if (method?.Invoke(target, null) is bool result)
                return result.ToString();
        }
        catch { }
        return ReflectionUtil.ReadMember(target, memberName)?.ToString() ?? "<unavailable>";
    }

    private static string DescribeCondition(object? condition)
    {
        if (condition == null)
            return "<null>";
        string type = condition.GetType().FullName ?? condition.GetType().Name;
        string result = ReadBoolResult(condition, "IsConditionMet", "ConditionMet");
        if (string.Equals(result, "<unavailable>", StringComparison.Ordinal))
            result = ReadBoolResult(condition, "GetValue", "Value");
        return $"'{type}:{result}'";
    }
}

internal sealed class BottomHudDiagnosticKeeper : MonoBehaviour
{
    private int _cooldown;

    public BottomHudDiagnosticKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("BottomHudDiagnosticKeeper.Update");
        if (_cooldown-- > 0)
            return;
        _cooldown = 60;
        BottomHudDiagnostic.Tick();
    }
}

internal static class PreviewAbilityRandomization
{
    private static readonly object Sync = new();
    private static readonly PreviewAbilityReconcileDispatcher UnityDispatcher = new();
    private static object? _playerSaveRequestProcessor;
    private static HypnoPanReconciler _hypnoPan = new(new NativeAdapter());
    private static ViolanceReconciler _violance = new(new NativeAdapter());
    private static int _hypnoPanReceived;
    private static int _violanceReceived;
    private static string _lastHypnoOutcome = string.Empty;
    private static string _lastViolanceOutcome = string.Empty;
    private static bool _compatible;

    [ThreadStatic]
    private static bool _applyingNativeGrant;

    internal static void Configure()
    {
        lock (Sync)
        {
            _playerSaveRequestProcessor = null;
            _hypnoPan = new HypnoPanReconciler(new NativeAdapter());
            _violance = new ViolanceReconciler(new NativeAdapter());
            _hypnoPanReceived = 0;
            _violanceReceived = 0;
            _lastHypnoOutcome = string.Empty;
            _lastViolanceOutcome = string.Empty;
            _compatible = false;
            UnityDispatcher.Clear();
        }
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? raw)
            ? raw?.ToString() ?? string.Empty
            : string.Empty;
        bool compatible = implementation.StartsWith(
            "area-routing-plant-pipes-0.15",
            StringComparison.OrdinalIgnoreCase);
        lock (Sync)
        {
            _compatible = compatible;
            _hypnoPan.Configure(compatible);
            _violance.Configure(compatible);
        }
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] PREVIEW ABILITIES configured compatible={compatible} implementation='{implementation}'. Items remain excluded from generated seeds.");
    }

    internal static bool TryApplyItem(string itemName)
    {
        bool hypno = string.Equals(itemName, HypnoPanReconciler.ItemName, StringComparison.OrdinalIgnoreCase);
        bool violance = string.Equals(itemName, ViolanceReconciler.ItemName, StringComparison.OrdinalIgnoreCase);
        if (!hypno && !violance)
            return false;

        int count;
        lock (Sync)
        {
            if (hypno)
            {
                count = ++_hypnoPanReceived;
                _hypnoPan.NoteReceivedCount(count);
            }
            else
            {
                count = ++_violanceReceived;
                _violance.NoteReceivedCount(count);
            }
        }
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] PREVIEW ABILITY RECEIVED item='{itemName}' receivedCount={count}; native reconciliation queued for the Unity thread.");
        return true;
    }

    internal static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null ||
            !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;
        lock (Sync)
            _playerSaveRequestProcessor = instance;
    }

    internal static void OnLifecyclePoint(string reason)
    {
        if (_applyingNativeGrant)
            return;
        lock (Sync)
        {
            _hypnoPan.Configure(_compatible);
            _violance.Configure(_compatible);
            _hypnoPan.NoteReceivedCount(_hypnoPanReceived);
            _violance.NoteReceivedCount(_violanceReceived);
            _hypnoPan.OnLifecyclePoint(reason);
            _violance.OnLifecyclePoint(reason);
            LogOutcome(HypnoPanReconciler.ItemName, _hypnoPan.LastOutcome, ref _lastHypnoOutcome, reason);
            LogOutcome(ViolanceReconciler.ItemName, _violance.LastOutcome, ref _lastViolanceOutcome, reason);
        }
    }

    internal static void RequestUnityReconciliation(string reason)
    {
        UnityDispatcher.Request(reason);
    }

    internal static void OnUnityLifecycle()
    {
        if (!UnityDispatcher.Drain(OnLifecyclePoint))
            OnLifecyclePoint("Unity lifecycle");
    }

    internal static void TickPending(TimeSpan elapsed)
    {
        if (_applyingNativeGrant)
            return;
        lock (Sync)
        {
            _hypnoPan.TickPending(elapsed);
            _violance.TickPending(elapsed);
            LogOutcome(HypnoPanReconciler.ItemName, _hypnoPan.LastOutcome, ref _lastHypnoOutcome, "pending tick");
            LogOutcome(ViolanceReconciler.ItemName, _violance.LastOutcome, ref _lastViolanceOutcome, "pending tick");
        }
    }

    private static void LogOutcome(string itemName, string outcome, ref string previous, string reason)
    {
        if (string.Equals(previous, outcome, StringComparison.Ordinal))
            return;
        previous = outcome;
        if (string.Equals(outcome, "verified-owned", StringComparison.Ordinal))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] PREVIEW ABILITY VERIFIED item='{itemName}' reason='{reason}'.");
        }
        else if (!string.Equals(outcome, "not-owned", StringComparison.Ordinal))
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] PREVIEW ABILITY state item='{itemName}' outcome={outcome} reason='{reason}'.");
        }
    }

    private sealed class NativeAdapter : IPreviewAbilityNativeAdapter
    {
        public bool SaveAvailable =>
            RootsBucketRandomization.TryReadProgressionFlag(HypnoPanReconciler.AbilityFlag, out _);

        public bool TryRead(string nativeFlag, out bool owned) =>
            RootsBucketRandomization.TryReadProgressionFlag(nativeFlag, out owned);

        public bool TrySubmit(string nativeFlag, out string detail)
        {
            object? processor;
            lock (Sync)
                processor = _playerSaveRequestProcessor;
            if (processor == null)
            {
                detail = "PlayerSaveRequestProcessor unavailable.";
                return false;
            }

            _applyingNativeGrant = true;
            try
            {
                return WeedKillerRandomization.TrySubmitProgressionFlag(
                    processor,
                    nativeFlag,
                    true,
                    out detail);
            }
            finally
            {
                _applyingNativeGrant = false;
            }
        }
    }
}

internal sealed class PreviewAbilityReconciliationKeeper : MonoBehaviour
{
    private int _cooldown;

    public PreviewAbilityReconciliationKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("PreviewAbilityReconciliationKeeper.Update");
        if (_cooldown-- > 0)
            return;
        _cooldown = 60;
        PreviewAbilityRandomization.OnUnityLifecycle();
        PreviewAbilityRandomization.TickPending(TimeSpan.FromSeconds(1));
    }
}

internal sealed class PlantPipesReconciliationKeeper : MonoBehaviour
{
    private int _cooldown;
    private string _lastState = string.Empty;

    public PlantPipesReconciliationKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("PlantPipesReconciliationKeeper.Update");
        if (_cooldown-- > 0)
            return;
        _cooldown = 60;

        PlantPipesRandomization.TryFlushPendingNativeGrant();
        PlantPipesRandomization.TickPending(TimeSpan.FromSeconds(1));

        string state = PlantPipesRandomization.ReadDiagnosticState();
        if (string.Equals(state, _lastState, StringComparison.Ordinal))
            return;

        _lastState = state;
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] ROOTS PLANT PIPES STATE {state}");
    }
}


internal sealed class CassetteReceiptReconciliationKeeper : MonoBehaviour
{
    private const string Hub6PhoneBankRootPath = "Root/GameRoom_Hub6_Logic/Objects/Phones";
    private long _lastTimestamp;
    private GameObject? _phoneBank;
    private readonly HubReadinessProbePolicy _phoneProbe = new();

    public CassetteReceiptReconciliationKeeper(IntPtr pointer) : base(pointer)
    {
    }
    private void OnGUI() => ApStars.RenderOverlay();

    private void Update()
    {
        using var timing = ClientPerformance.Measure("CassetteReceiptReconciliationKeeper.Update");
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        TimeSpan elapsed = _lastTimestamp == 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds((double)(now - _lastTimestamp) / System.Diagnostics.Stopwatch.Frequency);
        _lastTimestamp = now;
        ClientPerformance.Frame(elapsed.TotalSeconds, DeveloperHarness.CurrentRoomId);
        using (ClientPerformance.Measure("Reconcile.Cassettes")) { CassetteReceiptRandomization.TickUnity(elapsed); }
        using (ClientPerformance.Measure("Reconcile.CharacterItems")) { CharacterQuestItems.TickUnity(elapsed); }
        using (ClientPerformance.Measure("Reconcile.Quests")) { QuestChecks.Tick(elapsed); }
        using (ClientPerformance.Measure("Reconcile.Expanded")) { ExpandedChecks.Tick(elapsed); }
        using (ClientPerformance.Measure("Reconcile.Stars")) { ApStars.TickUnity(); }
        using (ClientPerformance.Measure("Reconcile.Roots")) { RootsBucketRandomization.TickUnity(elapsed); }
        string room = DeveloperHarness.CurrentRoomId;
        if (room != "GameRoom_Hub6") _phoneBank = null;
        bool live = _phoneBank != null && _phoneBank.activeInHierarchy;
        if (_phoneProbe.ShouldSearch(room, Time.unscaledTime, live))
            _phoneBank = GameObject.Find(Hub6PhoneBankRootPath);
        GameObject? phoneBank = _phoneBank != null && _phoneBank.activeInHierarchy ? _phoneBank : null;
        CassetteReceiptRandomization.ObserveGameplayReadyMarker(
            phoneBank != null,
            phoneBank?.GetInstanceID() ?? 0);
        if (phoneBank != null &&
            CassetteReceiptRandomization.TryCaptureGameplayReadyObservation(out CassetteGameplayReadyObservation observation))
            CassetteReceiptRandomization.ObserveGameplayReady(observation);
    }
}

internal static class GarageEntrancePreviewPatches
{
    public static void Prefix(object[]? __args)
    {
        if (__args is not { Length: > 0 } || __args[0] == null)
            return;

        GarageCartridgeAccess.TryApplyEntrancePreviewOwnership(__args[0]);
        try {
            GaragePreviewMedals.Apply(__args[0], GarageCartridgeAccess.Enabled,
                GarageCartridgeAccess.Cartridges.Where(c => GarageCartridgeAccess.HasCartridge(c.Song)).Select(c => c.Song),
                GarageNativeSavedMedals.Read);
        } catch (Exception ex) {
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] Garage preview saved-medal read unavailable: {ex.GetBaseException().Message}");
        }
    }
}

internal static class GarageCartridgeAccess
{
    internal static readonly GarageCartridgeNativeDefinition[] Cartridges =
        GarageCartridgeNativePolicy.AllCartridges;

    private static readonly GarageDeferredDoorConsumption DeferredDoorConsumption = new();
    private static long _doorSaveBoundaryEpoch;
    private static readonly object Sync = new();
    private static readonly GarageCartridgeInsertionCoordinator InsertionCoordinator = new(
        GarageCartridgeNativePolicy.RandomizedCartridges.Select(cartridge =>
            new KeyValuePair<string, string>(cartridge.Song, cartridge.ServerInsertionKey)));
    private static readonly GarageCartridgeReconciliationAccess ServerSyncLifecycle = new();
    private static readonly PreviewAbilityReconcileDispatcher UnityDispatcher = new();
    private static readonly HashSet<string> OwnedSongs = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, GarageNativeGrantDecision> LastNativeDecisions =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly GarageCartridgeReconciliationAdapter InsertionTracker = new();
    private static readonly HashSet<string> InsertionCandidateArmedLogged =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> NativeGrantPendingLogged =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> NativeGrantAppliedLogged =
        new(StringComparer.OrdinalIgnoreCase);
    private static object? _playerSaveRequestProcessor;

    [ThreadStatic]
    private static int _applyingArchipelagoGrant;

    public static bool Enabled { get; private set; }
    public static bool SourceRandomizationEnabled { get; private set; }
    public static bool VanillaEntranceEnabled { get; private set; }
    public static string ImplementationVersion { get; private set; } = string.Empty;

    internal static long NativeBagObservationResetEpoch
    {
        get
        {
            lock (Sync)
                return InsertionTracker.ResetEpoch;
        }
    }

    static GarageCartridgeAccess()
    {
        InsertionCoordinator.DurableInsertionConfirmed += song =>
        {
            LogDiagnostic(GarageCartridgeDiagnostics.ServerConfirmedDurable(song));
            RequestUnityReconciliation("Garage insertion state confirmed durable");
        };
        InsertionCoordinator.DiagnosticEmitted += diagnostic =>
        {
            LogDiagnostic(diagnostic);
            if (diagnostic.Message.Contains(GarageCartridgeDiagnostics.SyncReadyMarker, StringComparison.Ordinal) ||
                diagnostic.Message.Contains(GarageCartridgeDiagnostics.SyncFailedMarker, StringComparison.Ordinal))
            {
                RequestUnityReconciliation("Garage inserted-state synchronization completed");
            }
        };
    }

    private static void LogDiagnostic(GarageCartridgeDiagnostic diagnostic)
    {
        if (diagnostic.Level == GarageCartridgeDiagnosticLevel.Warning)
            Plugin.LoggerInstance?.LogWarning(diagnostic.Message);
        else
            Plugin.LoggerInstance?.LogInfo(diagnostic.Message);
    }

    public static void Configure()
    {
        ServerSyncLifecycle.EndActive(generation =>
            InsertionCoordinator.EndConnection(generation));

        Enabled = false;
        SourceRandomizationEnabled = false;
        VanillaEntranceEnabled = false;
        ImplementationVersion = string.Empty;
        lock (Sync)
        {
            OwnedSongs.Clear();
            _playerSaveRequestProcessor = null;
        }
        ResetNativeBagObservations("configure");
        UnityDispatcher.Clear();
    }

    public static bool TryApplyItem(string itemName)
    {
        foreach (GarageCartridgeNativeDefinition cartridge in Cartridges)
        {
            if (!string.Equals(cartridge.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                continue;

            bool added;
            lock (Sync)
                added = OwnedSongs.Add(cartridge.Song);

            Plugin.LoggerInstance?.LogWarning(
                added
                    ? $"[SCRC-AP] GAME GARAGE CARTRIDGE RECEIVED song='{cartridge.Song}' item='{cartridge.ItemName}' routingEnabled={Enabled}; Unity-thread reconciliation requested."
                    : $"[SCRC-AP] GAME GARAGE CARTRIDGE already owned song='{cartridge.Song}' item='{cartridge.ItemName}' routingEnabled={Enabled}; Unity-thread reconciliation requested.");
            RequestUnityReconciliation("AP Garage cartridge receipt");
            return true;
        }

        return false;
    }

    public static bool BeginServerSync(ArchipelagoSession session, long generation)
    {
        if (!Enabled)
            return false;

        bool began = ServerSyncLifecycle.TryBegin(generation, () =>
            InsertionCoordinator.BeginConnection(
                generation,
                new ArchipelagoGarageInsertionDataStore(session)));
        if (!began)
            return false;
        ResetNativeBagObservations("server synchronization started");
        RequestUnityReconciliation("Garage inserted-state synchronization started");
        return true;
    }

    public static void EndServerSync(long generation)
    {
        bool ended = ServerSyncLifecycle.End(generation, () =>
            InsertionCoordinator.EndConnection(generation));
        if (!ended)
            return;

        ResetNativeBagObservations("server synchronization ended");
        UnityDispatcher.Clear();
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] GAME GARAGE CARTRIDGE inserted-state synchronization ended generation={generation}.");
    }

    public static void RequestUnityReconciliation(string reason)
    {
        if (!Enabled)
            return;
        UnityDispatcher.Request(reason);
    }

    public static void OnUnityLifecycle()
    {
        UnityDispatcher.Drain(_ => TryFlushPendingNativeGrants());
    }

    public static void ResetNativeBagObservations(
        string reason,
        bool preservePendingConsumption = false,
        bool enteringGarageFromApproachRoom = false,
        bool preserveDoorSaveBoundary = false)
    {
        if (preservePendingConsumption &&
            ServerSyncLifecycle.TryRunCurrent(current =>
                current.Observe(() => ResetNativeBagObservationsWithinLease(
                    reason,
                    current.Generation,
                    enteringGarageFromApproachRoom, preserveDoorSaveBoundary))))
        {
            return;
        }

        ResetNativeBagObservationsWithinLease(
            reason,
            generation: null,
            enteringGarageFromApproachRoom: false, preserveDoorSaveBoundary);
    }

    private static void ResetNativeBagObservationsWithinLease(
        string reason,
        long? generation,
        bool enteringGarageFromApproachRoom, bool preserveDoorSaveBoundary)
    {
        lock (Sync)
        {
            if (!generation.HasValue && !preserveDoorSaveBoundary) _doorSaveBoundaryEpoch++;
            long previousEpoch = InsertionTracker.ResetEpoch;
            if (generation.HasValue)
            {
                InsertionTracker.ResetForRoomTransition(
                    generation.Value,
                    enteringGarageFromApproachRoom);
            }
            else
                InsertionTracker.Reset();
            DeferredDoorConsumption.Transition(generation, previousEpoch, InsertionTracker.ResetEpoch, enteringGarageFromApproachRoom);
            InsertionCandidateArmedLogged.Clear();
            LastNativeDecisions.Clear();
            NativeGrantPendingLogged.Clear();
            NativeGrantAppliedLogged.Clear();
        }
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] GAME GARAGE CARTRIDGE native bag observations reset reason='{reason}'.");
    }

    private static GarageCartridgeReconciliationResult ObserveNativeBagWithinLease(
        long generation,
        GarageCartridgeNativeDefinition cartridge,
        bool readable,
        bool held,
        bool inGarage,
        Func<bool, GarageNativeGrantSubmission>? submitGrant = null)
    {
        bool enabled;
        bool apOwned;
        lock (Sync)
        {
            enabled = Enabled;
            apOwned = OwnedSongs.Contains(cartridge.Song);
        }

        GarageInsertionServerValue serverValue = InsertionCoordinator.InitialSyncReady
            ? InsertionCoordinator.GetServerValue(cartridge.Song)
            : GarageInsertionServerValue.Unknown;
        GarageCartridgeReconciliationResult result = InsertionTracker.Reconcile(
            cartridge.Song,
            enabled,
            apOwned,
            cartridge.UsesPhysicalVanillaEntrance,
            serverValue,
            readable,
            held,
            inGarage,
            false, // Native door tracking handles consumption; no mod-driven release.
            generation,
            InsertionCoordinator.NoteInserted,
            submitGrant);
        bool logCandidateArmed;
        lock (Sync)
        {
            logCandidateArmed = result.CandidateArmed &&
                InsertionCandidateArmedLogged.Add(cartridge.Song);
            if (result.RecordedInsertion)
            {
                LastNativeDecisions.Remove(cartridge.Song);
                NativeGrantPendingLogged.Remove(cartridge.Song);
            }
        }

        if (logCandidateArmed)
            LogDiagnostic(GarageCartridgeDiagnostics.CandidateArmed(cartridge.Song));
        if (result.RecordedInsertion)
        {
            LogDiagnostic(GarageCartridgeDiagnostics.NativeConsumptionObserved(cartridge.Song));
            LogDiagnostic(GarageCartridgeDiagnostics.ServerWritePending(cartridge.Song));
            RequestUnityReconciliation("Garage native cartridge consumption observed");
        }
        return result;
    }

    internal static GarageDoorConsumptionSnapshot? CaptureNativeDoorRemoval(GarageCartridgeNativeDefinition cartridge)
    {
        if (!Enabled || string.IsNullOrEmpty(cartridge.Song) || cartridge.UsesPhysicalVanillaEntrance)
            return null;
        GarageDoorConsumptionSnapshot? snapshot = null;
        ServerSyncLifecycle.TryRunCurrent(current => current.Observe(() =>
        {
            if (!HasCartridge(cartridge.Song) || !InsertionCoordinator.InitialSyncReady ||
                InsertionCoordinator.GetServerValue(cartridge.Song) != GarageInsertionServerValue.NotInserted)
                return;
            bool readable = RootsBucketRandomization.TryReadProgressionFlag(cartridge.NativeBagFlag, out bool held);
            if (readable && held)
                snapshot = new GarageDoorConsumptionSnapshot(cartridge, current.Generation, NativeBagObservationResetEpoch, _doorSaveBoundaryEpoch);
        }));
        return snapshot;
    }

    internal static void CompleteNativeDoorRemoval(GarageDoorConsumptionSnapshot? snapshot)
    {
        if (snapshot == null)
            return;
        ServerSyncLifecycle.TryRunCurrent(current => current.Observe(() =>
        {
            bool readable = RootsBucketRandomization.TryReadProgressionFlag(snapshot.Cartridge.NativeBagFlag, out bool held);
            GarageInsertionServerValue serverValue = InsertionCoordinator.InitialSyncReady
                ? InsertionCoordinator.GetServerValue(snapshot.Cartridge.Song)
                : GarageInsertionServerValue.Unknown;
            bool confirmed = GarageCartridgeInsertionPolicy.ShouldRecordDoorConsumption(
                Enabled, HasCartridge(snapshot.Cartridge.Song), snapshot.Cartridge.UsesPhysicalVanillaEntrance,
                serverValue, true, true, readable, held,
                current.Generation == snapshot.Generation && NativeBagObservationResetEpoch == snapshot.ResetEpoch);
            if (!confirmed) {
                if (Enabled && HasCartridge(snapshot.Cartridge.Song) && serverValue == GarageInsertionServerValue.NotInserted &&
                    current.Generation == snapshot.Generation && _doorSaveBoundaryEpoch == snapshot.SaveBoundaryEpoch &&
                    GarageCartridgeRoomPolicy.IsGarageApproachRoom(DeveloperHarness.CurrentRoomId))
                    lock (Sync) DeferredDoorConsumption.Arm(snapshot.Cartridge.Song, snapshot.Generation, NativeBagObservationResetEpoch);
                return;
            }
            InsertionCoordinator.NoteInserted(snapshot.Cartridge.Song);
            LogDiagnostic(GarageCartridgeDiagnostics.NativeConsumptionObserved(snapshot.Cartridge.Song));
            LogDiagnostic(GarageCartridgeDiagnostics.ServerWritePending(snapshot.Cartridge.Song));
            RequestUnityReconciliation("exact native Garage door consumption confirmed");
        }));
    }

    public static void RecordGarageEntryTransitionRequest(
        string? originRoom,
        string? destinationRoom)
    {
        if (!GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(originRoom, destinationRoom))
            return;

        bool bound = false;
        ServerSyncLifecycle.TryRunCurrent(current =>
            current.Observe(() =>
                bound = InsertionTracker.RecordGarageEntryTransitionRequest(
                    originRoom,
                    destinationRoom,
                    current.Generation)));
        if (bound)
            RequestUnityReconciliation("Garage entry transition request bound native consumption");
    }

    public static void RecordNativeBagProgressionRequest(object request, string flag)
    {
        bool? value = ReflectionUtil.ReadBool(request, "Value")
                      ?? ReflectionUtil.ReadBool(request, "_Value_k__BackingField");
        TryArmPendingNativeConsumption(
            flag,
            value,
            signalWasSet: null,
            requirePreviouslySet: false);
    }

    public static void RecordNativeBagProgressionFlagUpdated(object evt, string flag)
    {
        bool? isSet = ReflectionUtil.ReadBool(evt, "FlagIsSet")
                      ?? ReflectionUtil.ReadBool(evt, "_FlagIsSet_k__BackingField");
        bool? wasSet = ReflectionUtil.ReadBool(evt, "FlagWasSet")
                       ?? ReflectionUtil.ReadBool(evt, "_FlagWasSet_k__BackingField");
        TryArmPendingNativeConsumption(
            flag,
            isSet,
            wasSet,
            requirePreviouslySet: true);
    }

    private static void TryArmPendingNativeConsumption(
        string flag,
        bool? signalIsSet,
        bool? signalWasSet,
        bool requirePreviouslySet)
    {
        string room = DeveloperHarness.CurrentRoomId;
        bool inGarage = GarageCartridgeRoomPolicy.IsGarage(room);
        bool inGarageApproachRoom = GarageCartridgeRoomPolicy.IsGarageApproachRoom(room);
        GarageCartridgeProgressionFlag? classification =
            GarageCartridgeNativePolicy.ClassifyProgressionFlag(flag);
        if ((!inGarage && !inGarageApproachRoom) ||
            classification is not { Kind: GarageCartridgeProgressionFlagKind.BagItem } classified ||
            classified.Cartridge.UsesPhysicalVanillaEntrance)
        {
            return;
        }

        GarageCartridgeNativeDefinition cartridge = classified.Cartridge;
        bool armed = false;
        Action<GarageCartridgeReconciliationLease> record = current =>
            current.Observe(() =>
            {
                GarageInsertionServerValue serverValue = InsertionCoordinator.InitialSyncReady
                    ? InsertionCoordinator.GetServerValue(cartridge.Song)
                    : GarageInsertionServerValue.Unknown;
                lock (Sync)
                {
                    armed = requirePreviouslySet
                        ? InsertionTracker.RecordProgressionFlagUpdated(
                            flag,
                            signalIsSet,
                            signalWasSet,
                            Enabled,
                            OwnedSongs.Contains(cartridge.Song),
                            serverValue,
                            inGarage,
                            false,
                            current.Generation,
                            inGarageApproachRoom)
                        : InsertionTracker.RecordProgressionRequest(
                            flag,
                            signalIsSet,
                            Enabled,
                            OwnedSongs.Contains(cartridge.Song),
                            serverValue,
                            inGarage,
                            false,
                            current.Generation,
                            inGarageApproachRoom);
                }
            });
        if (requirePreviouslySet)
        {
            ServerSyncLifecycle.TryRunProgressionFlagUpdated(
                signalIsSet,
                signalWasSet,
                record);
        }
        else
        {
            ServerSyncLifecycle.TryRunProgressionRequest(signalIsSet, record);
        }

        if (armed)
            RequestUnityReconciliation("Garage native cartridge consumption awaiting confirmation");
    }

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null ||
            !string.Equals(instance.GetType().Name, "PlayerSaveRequestProcessor", StringComparison.Ordinal))
            return;

        bool processorReplaced;
        lock (Sync)
        {
            processorReplaced = !ReferenceEquals(_playerSaveRequestProcessor, instance);
            _playerSaveRequestProcessor = instance;
        }
        if (processorReplaced)
            ResetNativeBagObservations("PlayerSaveRequestProcessor replacement", preserveDoorSaveBoundary: true);
    }

    public static void TryFlushPendingNativeGrants()
    {
        if (!GarageRoomInitialization.CanMutate || _applyingArchipelagoGrant != 0)
            return;
        ServerSyncLifecycle.TryRunCurrent(current =>
        {
            long generation = current.Generation;
            object? processor;
            bool enabled;
            lock (Sync)
            {
                processor = _playerSaveRequestProcessor;
                enabled = Enabled;
            }

            if (!enabled)
                return;

            bool initialSyncReady = InsertionCoordinator.InitialSyncReady;
            string room = DeveloperHarness.CurrentRoomId;
            bool inGarage = GarageCartridgeRoomPolicy.IsGarage(room);
            foreach (GarageCartridgeNativeDefinition cartridge in GarageCartridgeNativePolicy.RandomizedCartridges)
            {
                bool bagReadable = RootsBucketRandomization.TryReadProgressionFlag(cartridge.NativeBagFlag, out bool bagHeld);
                GarageDoorReadback doorReadback;
                lock (Sync) doorReadback = DeferredDoorConsumption.Observe(cartridge.Song, generation, InsertionTracker.ResetEpoch, inGarage, bagReadable, bagHeld);
                if (doorReadback == GarageDoorReadback.Waiting) continue;
                if (doorReadback == GarageDoorReadback.Consumed) {
                    InsertionCoordinator.NoteInserted(cartridge.Song);
                    Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] GAME GARAGE DEFERRED DOOR CONSUMPTION confirmed song='{cartridge.Song}' bagHeld=False; no regrant.");
                }
                GarageInsertionServerValue serverValue = initialSyncReady
                    ? InsertionCoordinator.GetServerValue(cartridge.Song)
                    : GarageInsertionServerValue.Unknown;
                Func<bool, GarageNativeGrantSubmission>? submitGrant = processor == null
                    ? null
                    : value =>
                    {
                        _applyingArchipelagoGrant++;
                        try
                        {
                            bool submitted = WeedKillerRandomization.TrySubmitProgressionFlag(
                                processor,
                                cartridge.NativeBagFlag,
                                value,
                                out string detail);
                            return new GarageNativeGrantSubmission(submitted, detail);
                        }
                        finally
                        {
                            _applyingArchipelagoGrant--;
                        }
                    };
                GarageCartridgeReconciliationResult result = default;
                current.Observe(() =>
                    result = ObserveNativeBagWithinLease(
                        current.Generation,
                        cartridge,
                        bagReadable,
                        bagHeld,
                        inGarage,
                        submitGrant));
                GarageNativeGrantDecision decision = result.GrantDecision;

                bool decisionChanged;
                lock (Sync)
                {
                    decisionChanged = !LastNativeDecisions.TryGetValue(cartridge.Song, out GarageNativeGrantDecision previous) ||
                                      previous != decision;
                    LastNativeDecisions[cartridge.Song] = decision;
                }
                if (decisionChanged && decision != GarageNativeGrantDecision.ApplyBagItem)
                {
                    if (decision == GarageNativeGrantDecision.AlreadyInserted)
                    {
                        LogDiagnostic(GarageCartridgeDiagnostics.AlreadyInsertedNoRegrant(
                            generation,
                            cartridge.Song));
                    }
                    else
                    {
                        Plugin.LoggerInstance?.LogInfo(
                            $"[SCRC-AP] GAME GARAGE CARTRIDGE native reconciliation generation={generation} song='{cartridge.Song}' result='{decision}' serverValue='{serverValue}' bagReadable={bagReadable} bagHeld={bagHeld}.");
                    }
                }

                if (!result.GrantAttempted)
                    continue;

                if (!result.GrantSubmitted)
                {
                    bool log;
                    lock (Sync)
                        log = NativeGrantPendingLogged.Add(cartridge.Song);
                    if (log)
                    {
                        Plugin.LoggerInstance?.LogWarning(
                            $"[SCRC-AP] GAME GARAGE CARTRIDGE native grant pending generation={generation} song='{cartridge.Song}' flag='{cartridge.NativeBagFlag}'. {result.GrantDetail}");
                    }
                    continue;
                }

                bool appliedLog;
                lock (Sync)
                {
                    NativeGrantPendingLogged.Remove(cartridge.Song);
                    appliedLog = NativeGrantAppliedLogged.Add(cartridge.Song);
                }
                if (appliedLog)
                    LogDiagnostic(GarageCartridgeDiagnostics.NativeGrantApplied(
                        generation,
                        cartridge.Song,
                        cartridge.NativeBagFlag,
                        result.GrantDetail));
            }
        });
    }

    public static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        if (slotData == null || slotData.Count == 0)
        {
            Enabled = false;
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] GAME GARAGE CARTRIDGE SLOT DATA missing/empty; vanilla Garage cartridge availability remains untouched.");
            return;
        }

        ImplementationVersion = slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        bool requested = false;
        if (slotData.TryGetValue("randomize_game_garage_cartridges", out object? rawEnabled) && rawEnabled != null)
        {
            if (rawEnabled is bool boolValue)
                requested = boolValue;
            else
                bool.TryParse(rawEnabled.ToString(), out requested);
        }

        Enabled = requested && ImplementationVersion.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase);

        bool sourceRequested = false;
        if (slotData.TryGetValue("randomize_vanilla_cartridge_sources", out object? rawSources) && rawSources != null)
        {
            if (rawSources is bool sourceBool)
                sourceRequested = sourceBool;
            else
                bool.TryParse(rawSources.ToString(), out sourceRequested);
        }
        SourceRandomizationEnabled = Enabled && sourceRequested;

        string vanillaSong = slotData.TryGetValue("vanilla_game_garage_cartridge", out object? rawVanillaSong)
            ? rawVanillaSong?.ToString() ?? string.Empty
            : string.Empty;
        VanillaEntranceEnabled = Enabled &&
            GarageVanillaEntrancePolicy.IsEnabled(ImplementationVersion, vanillaSong);
        if (VanillaEntranceEnabled)
        {
            lock (Sync)
            {
                OwnedSongs.Add(GarageVanillaEntrancePolicy.Song);
            }
        }

        if (!Enabled)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] GAME GARAGE CARTRIDGE RANDOMIZATION disabled by slot data implementation='{ImplementationVersion}' requested={requested}; vanilla Garage cartridge availability remains untouched.");
            return;
        }

        string owned;
        lock (Sync)
            owned = OwnedSongs.Count == 0 ? "<none>" : string.Join(", ", OwnedSongs.OrderBy(x => x));

        Plugin.LoggerInstance?.LogWarning(
            VanillaEntranceEnabled
                ? $"[SCRC-AP] GAME GARAGE CARTRIDGE RANDOMIZATION ENABLED implementation='{ImplementationVersion}' receivedSoFar='{owned}' sourceChecks={SourceRandomizationEnabled}. Vampire Killer uses its physical vanilla pickup and normal Garage entrance sequence; the other five songs require AP Cartridge items."
                : $"[SCRC-AP] GAME GARAGE CARTRIDGE RANDOMIZATION ENABLED implementation='{ImplementationVersion}' receivedSoFar='{owned}' sourceChecks={SourceRandomizationEnabled}. Six Garage songs require their corresponding AP Cartridge item.");
    }

    public static bool TryApplyEntrancePreviewOwnership(object previewData)
    {
        string[] apOwnedSongs;
        lock (Sync)
            apOwnedSongs = OwnedSongs.ToArray();
        bool compatible =
            ImplementationVersion.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase);
        return GarageAvailabilityPolicy.TryApplyEntrancePreviewOwnership(
            previewData,
            Enabled,
            compatible,
            apOwnedSongs,
            out _);
    }

    public static bool ShouldSuppressVanillaSourceGrant(object request, string flag)
    {
        if (_applyingArchipelagoGrant != 0 ||
            !SourceRandomizationEnabled ||
            string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_27", StringComparison.Ordinal))
            return false;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        GarageCartridgeProgressionFlag? classification =
            GarageCartridgeNativePolicy.ClassifyProgressionFlag(flag);
        if (!value || classification is not { Kind: GarageCartridgeProgressionFlagKind.BagItem })
            return false;

        string song = classification.Value.Cartridge.Song;
        if (!GarageCartridgeNativePolicy.ShouldSuppressVanillaSourceGrant(flag, VanillaEntranceEnabled))
            return false;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CARTRIDGE VANILLA GRANT SUPPRESSED song='{song}' flag='{flag}' room='{DeveloperHarness.CurrentRoomId}'. Source remains collectible; the actual cartridge must come from Archipelago.");
        return true;
    }

    public static void RecordVanillaSourceCollected(object request, string flag)
    {
        if (!SourceRandomizationEnabled || string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_27", StringComparison.Ordinal))
            return;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        GarageCartridgeProgressionFlag? classification =
            GarageCartridgeNativePolicy.ClassifyProgressionFlag(flag);
        if (!value || classification is not { Kind: GarageCartridgeProgressionFlagKind.Collected })
            return;

        string song = classification.Value.Cartridge.Song;
        string? location = GetVanillaSourceLocation(song);
        if (!GarageVanillaEntrancePolicy.ShouldRandomizeSong(VanillaEntranceEnabled, song))
            return;
        if (string.IsNullOrWhiteSpace(location))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CARTRIDGE SOURCE COLLECTED song='{song}' flag='{flag}' but no AP location mapping is registered yet.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CARTRIDGE SOURCE AP CHECK song='{song}' flag='{flag}' room='{DeveloperHarness.CurrentRoomId}' location='{location}'. Native collected marker retained; native bag grant suppressed outside Game Garage.");
        Plugin.AP?.QueueLocation(location);
    }

    private static string? GetVanillaSourceLocation(string song) => song switch
    {
        // These two vanilla cartridge sources are already represented by the
        // existing Music Lab reward-chest checks, so do not add duplicate locations.
        "Gradius Remix" => LocationMap.MusicLab10PointChestLocationName,
        "Bloody Tears" => LocationMap.MusicLab46PointChestLocationName,
        "Smooch" => "Cartridge Pickup - Smooch",
        "Superstar" => "Cartridge Pickup - Superstar",
        "Vampire Killer" => "Cartridge Pickup - Vampire Killer",
        "Wag the Dog" => "Cartridge Pickup - Wag the Dog",
        _ => null,
    };

    public static bool HasCartridge(string song)
    {
        if (!Enabled)
            return true;

        lock (Sync)
            return OwnedSongs.Contains(song);
    }

}

internal sealed class GarageCartridgeAccessKeeper : MonoBehaviour
{
    private string _lastRoom = string.Empty;
    private float _nextNativeReconcile;
    public GarageCartridgeAccessKeeper(IntPtr pointer) : base(pointer) { }
    private void Update()
    {
        using var timing = ClientPerformance.Measure("GarageCartridgeAccessKeeper.Update");
        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, _lastRoom, StringComparison.Ordinal)) {
            GarageCartridgeAccess.ResetNativeBagObservations(
                $"room transition '{_lastRoom}' -> '{room}'", preservePendingConsumption: true,
                GarageCartridgeRoomPolicy.IsGarageEntryFromApproach(_lastRoom, room));
            _lastRoom = room;
        }
        GarageCartridgeAccess.OnUnityLifecycle();
        if (Time.unscaledTime >= _nextNativeReconcile) {
            _nextNativeReconcile = Time.unscaledTime + 1f;
            GarageCartridgeAccess.TryFlushPendingNativeGrants();
        }
        // Native holders now place/hide cartridges during their own initialization.
        // Never SetActive or reparent a cartridge after the room is shown.
    }
}

internal static class AreaPhoneConditionPatches
{
    private static readonly HashSet<string> Logged = new(StringComparer.OrdinalIgnoreCase);

    public static bool BoolConditionPrefix(object? __instance, ref bool __result, MethodBase __originalMethod)
    {
        try
        {
            if (__instance != null && QuestSharedConditionHook.Ready &&
                QuestConditionHookPlan.IsSharedEntry(__originalMethod.DeclaringType?.Name, __originalMethod.Name) &&
                !QuestCheckHooks.ConditionPrefix(__instance, ref __result))
                return false;
            if (!AreaAccessPrototype.Enabled || __instance == null)
                return true;

            string currentRoom = DeveloperHarness.CurrentRoomId;
            string? flag = ReadProgressionFlag(__instance);
            if (AreaArrivalPresentationOverride.ShouldBypass(currentRoom, flag))
            {
                __result = true;
                string arrivalKey = $"arrival|{currentRoom}|{flag}";
                if (Logged.Add(arrivalKey))
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] AREA ARRIVAL CONDITION BYPASSED room='{currentRoom}' flag='{flag}' method='{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}' result=True saveFlagUnchanged=True.");
                }
                return false;
            }

            if (!string.Equals(currentRoom, "GameRoom_Hub6", StringComparison.Ordinal))
                return true;

            if (__instance is not Component component || component.gameObject == null)
                return true;

            string path = BuildPath(component.transform);
            foreach (AreaAccessPrototype.AreaDefinition area in AreaAccessPrototype.Areas)
            {
                string rootToken = "/Objects/Phones/" + area.PhoneRootName + "/";
                if (!path.Contains(rootToken, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Only AP-unlocked destinations bypass their native visited/open
                // condition. Locked destinations retain their vanilla result and
                // are additionally protected by the disabled PhoneBox + transition gate.
                if (!AreaAccessPrototype.HasArea(area.AreaName))
                    return true;

                __result = true;
                string logKey = area.AreaName + "|" + (__originalMethod.DeclaringType?.Name ?? "?") + "." + __originalMethod.Name;
                if (Logged.Add(logKey))
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] AREA PHONE VANILLA CONDITION BYPASSED area='{area.AreaName}' path='{path}' method='{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}' result=True saveFlagUnchanged=True.");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA PHONE CONDITION BYPASS failed safely: {ex.GetBaseException().Message}");
        }

        return true;
    }

    private static string? ReadProgressionFlag(object condition)
    {
        object? raw = ReflectionUtil.ReadMember(condition, "ProgressionFlag")
                      ?? ReflectionUtil.ReadMember(condition, "Flag")
                      ?? ReflectionUtil.ReadMember(condition, "_ProgressionFlag")
                      ?? ReflectionUtil.ReadMember(condition, "_ProgressionFlag_k__BackingField")
                      ?? ReflectionUtil.ReadMember(condition, "_Flag_k__BackingField");
        return raw == null
            ? null
            : ReflectionUtil.ExtractIdentifier(raw) ?? raw.ToString();
    }

    private static string BuildPath(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;
            for (int i = 0; i < 32 && current != null; i++)
            {
                names.Add(current.name ?? "<unnamed>");
                current = current.parent;
            }
            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return tr.name ?? "<unavailable>";
        }
    }
}

internal static class AreaArrivalPresentationOverride
{
    private static readonly object Sync = new();
    private static AreaArrivalKind _arrival;

    internal static void ObserveTransition(string? originRoom, string? destinationRoom)
    {
        AreaArrivalKind next = AreaArrivalPresentationPolicy.DecideTransition(
            AreaAccessPrototype.Enabled,
            IntroHubSkip.Compatible,
            originRoom,
            destinationRoom,
            AreaAccessPrototype.HasArea("Roots"),
            AreaAccessPrototype.HasArea("Lobby"));
        lock (Sync)
            _arrival = next;

        if (next != AreaArrivalKind.None)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA ARRIVAL OVERRIDE ARMED kind='{next}' origin='{originRoom}' destination='{destinationRoom}'; exact presentation/HUD conditions only, save flags unchanged.");
        }
    }

    internal static bool ShouldBypass(string? currentRoom, string? progressionFlag)
    {
        AreaArrivalKind arrival;
        lock (Sync)
            arrival = _arrival;
        return AreaArrivalPresentationPolicy.ShouldBypassCondition(
            arrival, currentRoom, progressionFlag);
    }

    internal static bool IsRootsArrivalActive
    {
        get
        {
            lock (Sync)
                return _arrival == AreaArrivalKind.RootsPhone;
        }
    }
}

internal sealed class RootsAreaBaselineKeeper : MonoBehaviour
{
    private const string RootsRoomId = "GameRoom_Hub2";
    private const string FirstAreaGatePath =
        "Root/GameRoom_Hub2_Logic/Objects/FirstAreaGate";
    private const string StarEaterBlockadePath =
        "Root/GameRoom_Hub2_Logic/Objects/StarEaterBlockade/Blockade";

    private string _boundRoom = string.Empty;
    private int _initialiseDelayFrames;
    private GameObject? _firstAreaGate;
    private GameObject? _starEaterBlockade;
    private GameObject? _computerCover;
    private bool _reportedGate;
    private bool _reportedBlockade;
    private bool _reportedReady;

    internal static bool LastGateBound { get; private set; }
    internal static bool LastBlockadeBound { get; private set; }

    public RootsAreaBaselineKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        using var timing = ClientPerformance.Measure("RootsAreaBaselineKeeper.LateUpdate");
        RootsIntroCutsceneBypass.TickPending();

        bool shouldOwnRootsBaseline =
            AreaAccessPrototype.Enabled &&
            AreaAccessPrototype.HasArea("Roots") &&
            string.Equals(
                DeveloperHarness.CurrentRoomId,
                RootsRoomId,
                StringComparison.OrdinalIgnoreCase);

        if (!shouldOwnRootsBaseline)
        {
            ResetForRoomExit();
            return;
        }

        if (!string.Equals(_boundRoom, RootsRoomId, StringComparison.Ordinal))
        {
            _boundRoom = RootsRoomId;
            _initialiseDelayFrames = 12;
            _firstAreaGate = null;
            _starEaterBlockade = null;
            _computerCover = null;
            _reportedGate = false;
            _reportedBlockade = false;
            _reportedReady = false;
            LastGateBound = false;
            LastBlockadeBound = false;
            return;
        }

        // Let Hub2 finish its room-initialise scripts before touching the two
        // campaign-order physical barriers. Both bindings are exact-path only;
        // no global Resources/Transform scan is used.
        if (_initialiseDelayFrames > 0)
        {
            _initialiseDelayFrames--;
            return;
        }

        if (_firstAreaGate == null)
        {
            try { _firstAreaGate = GameObject.Find(FirstAreaGatePath); }
            catch { _firstAreaGate = null; }
            LastGateBound = _firstAreaGate != null;
        }

        if (_starEaterBlockade == null)
        {
            try { _starEaterBlockade = GameObject.Find(StarEaterBlockadePath); }
            catch { _starEaterBlockade = null; }
            LastBlockadeBound = _starEaterBlockade != null;
        }

        if (_computerCover == null)
        {
            try { _computerCover = GameObject.Find(RootsComputerPolicy.CoverPath); }
            catch { _computerCover = null; }
        }

        try
        {
            if (_computerCover != null &&
                RootsComputerPolicy.ShouldHideCover(
                    AreaAccessPrototype.Enabled,
                    IntroHubSkip.Compatible,
                    AreaAccessPrototype.HasArea("Roots"),
                    DeveloperHarness.CurrentRoomId) &&
                _computerCover.activeSelf)
            {
                _computerCover.SetActive(false);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] ROOTS SOPHISTICATED COMPUTER COVER HIDDEN path='{RootsComputerPolicy.CoverPath}' saveFlagsUnchanged=True.");
            }
        }
        catch { _computerCover = null; }

        // Roots Access owns traversal of the full area. These are campaign-order
        // scene barriers only, so disable the scene objects without setting the
        // corresponding vanilla story/quest flags.
        try
        {
            if (_firstAreaGate != null && _firstAreaGate.activeSelf)
                _firstAreaGate.SetActive(false);

            if (_firstAreaGate != null && !_reportedGate)
            {
                _reportedGate = true;
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] ROOTS FIRST AREA GATE SUPPRESSED: exact FirstAreaGate root disabled while Roots Access is owned; vanilla gate/story flags remain unchanged.");
            }
        }
        catch
        {
            _firstAreaGate = null;
            LastGateBound = false;
        }

        try
        {
            if (_starEaterBlockade != null && _starEaterBlockade.activeSelf)
                _starEaterBlockade.SetActive(false);

            if (_starEaterBlockade != null && !_reportedBlockade)
            {
                _reportedBlockade = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] ROOTS STAR EATER BLOCKADE SUPPRESSED: exact physical blocker '{StarEaterBlockadePath}' disabled while Roots Access is owned; Star Eater/Trevor quest flags remain unchanged.");
            }
        }
        catch
        {
            _starEaterBlockade = null;
            LastBlockadeBound = false;
        }

        if (!_reportedReady && LastGateBound && LastBlockadeBound)
        {
            _reportedReady = true;
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] ROOTS AREA BASELINE READY IN SCENE: gateBound=True starEaterBlockadeBound=True. No global scene scan or Trevor-story mutation is running.");
        }
    }

    internal static void LogCompactStatus()
    {
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS HOME STATUS room='{DeveloperHarness.CurrentRoomId}' rootsAccess={AreaAccessPrototype.HasArea("Roots")} gateBound={LastGateBound} starEaterBlockadeBound={LastBlockadeBound}. Exact-path baseline only; no collider probe or scene-wide scan.");
    }

    private void ResetForRoomExit()
    {
        if (string.IsNullOrEmpty(_boundRoom))
            return;

        _boundRoom = string.Empty;
        _initialiseDelayFrames = 0;
        _firstAreaGate = null;
        _starEaterBlockade = null;
        _computerCover = null;
        _reportedGate = false;
        _reportedBlockade = false;
        _reportedReady = false;
        LastGateBound = false;
        LastBlockadeBound = false;
    }
}

internal sealed class MeatAreaBaselineKeeper : MonoBehaviour
{
    private string _boundRoom = string.Empty;
    private int _initialiseDelayFrames;
    private GameObject? _firstAreaGate;
    private bool _reportedGate;

    public MeatAreaBaselineKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        using var timing = ClientPerformance.Measure("MeatAreaBaselineKeeper.LateUpdate");
        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, MeatAreaPresentationPolicy.RoomId, StringComparison.Ordinal))
        {
            ResetForRoomExit();
            return;
        }

        if (!MeatAreaPresentationPolicy.ShouldSuppressFirstAreaGate(
                AreaAccessPrototype.Enabled,
                AreaAccessPrototype.AuthenticatedCompatibleSession,
                AreaAccessPrototype.HasArea("Meat Dimension"),
                room,
                MeatAreaPresentationPolicy.FirstAreaGatePath))
        {
            return;
        }

        if (string.IsNullOrEmpty(_boundRoom))
        {
            _boundRoom = MeatAreaPresentationPolicy.RoomId;
            _initialiseDelayFrames = 12;
            _firstAreaGate = null;
            _reportedGate = false;
            return;
        }

        // Let Hub4 finish its room-initialise scripts before touching the one
        // campaign-order physical barrier. Binding is exact-path only; no
        // global Resources/Transform scan is used.
        if (_initialiseDelayFrames > 0)
        {
            _initialiseDelayFrames--;
            return;
        }

        if (_firstAreaGate == null)
        {
            try
            {
                _firstAreaGate = GameObject.Find(MeatAreaPresentationPolicy.FirstAreaGatePath);
            }
            catch
            {
                _firstAreaGate = null;
            }
        }

        try
        {
            if (_firstAreaGate != null && _firstAreaGate.activeSelf)
                _firstAreaGate.SetActive(false);

            if (_firstAreaGate != null && !_reportedGate)
            {
                _reportedGate = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT FIRST AREA GATE SUPPRESSED: exact physical blocker '{MeatAreaPresentationPolicy.FirstAreaGatePath}' disabled while Meat Dimension Access is owned; MEAT_HUB_GATE_OPENED and all native quest/story flags remain unchanged.");
            }
        }
        catch
        {
            _firstAreaGate = null;
        }
    }

    private void ResetForRoomExit()
    {
        if (string.IsNullOrEmpty(_boundRoom))
            return;

        _boundRoom = string.Empty;
        _initialiseDelayFrames = 0;
        _firstAreaGate = null;
        _reportedGate = false;
    }
}

internal sealed class MeatMouseEscortRecoveryKeeper : MonoBehaviour
{
    private readonly MeatMouseEscortRecoveryRuntime _runtime = new();
    private readonly MeatMouseEscortConditionDiagnosticRuntime _conditionDiagnosticRuntime = new();
    private string _boundRoom = string.Empty;
    private int _initialiseDelayFrames;
    private bool _reportedPending;
    private bool _bindingDiagnosticEmitted;
    private bool _conditionDiagnosticEmitted;

    public MeatMouseEscortRecoveryKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        using var timing = ClientPerformance.Measure("MeatMouseEscortRecoveryKeeper.LateUpdate");
        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, MeatMouseEscortRecoveryPolicy.RoomId, StringComparison.Ordinal))
        {
            ResetForRoomExit();
            return;
        }

        if (string.IsNullOrEmpty(_boundRoom))
        {
            _boundRoom = MeatMouseEscortRecoveryPolicy.RoomId;
            _initialiseDelayFrames = 12;
            _runtime.Reset();
            _conditionDiagnosticRuntime.Reset();
            _reportedPending = false;
            _bindingDiagnosticEmitted = false;
            _conditionDiagnosticEmitted = false;
            return;
        }

        if (_initialiseDelayFrames > 0)
        {
            _initialiseDelayFrames--;
            return;
        }

        TryEmitExactConditionDiagnostic(room);

        if (!_runtime.ShouldEvaluateFrame())
        {
            _runtime.AdvanceFrame();
            return;
        }

        GameObject? spawnerObject = null;
        object? nativeSpawner = null;
        MethodInfo? resetSpawnCount = null;
        MethodInfo? triggerSpawner = null;
        try
        {
            spawnerObject = GameObject.Find(MeatMouseEscortRecoveryPolicy.NativeSpawnerPath);
            Assembly? gameAssembly = ReflectionUtil.GameAssembly;
            Type? nativeSpawnerType = gameAssembly == null
                ? null
                : ReflectionUtil.SafeGetTypes(gameAssembly)
                    .FirstOrDefault(type => string.Equals(
                        type.FullName, "SpawnMeatAnimalCharacterOnDemand", StringComparison.Ordinal));
            object? rawSpawner = spawnerObject != null && nativeSpawnerType != null
                ? spawnerObject.GetComponent(Il2CppInterop.Runtime.Il2CppType.From(nativeSpawnerType))
                : null;
            ConstructorInfo? pointerConstructor = nativeSpawnerType?.GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(IntPtr) },
                modifiers: null);
            IntPtr rawSpawnerPointer = rawSpawner == null
                ? IntPtr.Zero
                : DiagnosticNativePointer(rawSpawner);
            nativeSpawner = pointerConstructor != null && rawSpawnerPointer != IntPtr.Zero
                ? pointerConstructor.Invoke(new object[] { rawSpawnerPointer })
                : null;
            resetSpawnCount = nativeSpawnerType?.GetMethod(
                "SetSpawnCount",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: new[] { typeof(int) },
                modifiers: null);
            triggerSpawner = nativeSpawnerType?.GetMethod(
                "OnTrigger",
                BindingFlags.Public | BindingFlags.Instance,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
        }
        catch
        {
            spawnerObject = null;
            nativeSpawner = null;
            resetSpawnCount = null;
            triggerSpawner = null;
        }

        bool requirementReadable = RootsBucketRandomization.TryReadProgressionFlag(
            MeatMouseEscortRecoveryPolicy.RequirementStatedFlag, out bool requirementStated);
        bool revolutionReadable = RootsBucketRandomization.TryReadProgressionFlag(
            MeatMouseEscortRecoveryPolicy.RevolutionTriggeredFlag, out bool revolutionTriggered);

        bool leaderReadable = false;
        bool leaderPresent = false;
        string leaderReadStage = "native-spawner-unavailable";
        if (nativeSpawner != null)
        {
            MeatMouseEscortCharacterState characterState =
                MeatMouseEscortCharacterReader.Read(nativeSpawner);
            leaderReadable = characterState.Readable;
            leaderPresent = characterState.Present;
            leaderReadStage = characterState.Stage;
        }

        MeatMouseEscortRecoverySnapshot snapshot = new(
            AreaAccessPrototype.AuthenticatedCompatibleSession,
            room,
            requirementReadable,
            requirementStated,
            revolutionReadable,
            revolutionTriggered,
            spawnerObject != null && nativeSpawner != null &&
            resetSpawnCount != null && triggerSpawner != null,
            leaderReadable,
            leaderPresent,
            _runtime.AttemptConsumed);

        MeatMouseEscortRecoveryObservation observation = _runtime.Observe(snapshot);
        if (observation == MeatMouseEscortRecoveryObservation.Pending)
        {
            if (MeatMouseEscortBindingDiagnosticPolicy.ShouldEmit(
                    room, _bindingDiagnosticEmitted, spawnerObject != null))
            {
                _bindingDiagnosticEmitted = true;
                try
                {
                    EmitNativeBindingDiagnostic();
                }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MEAT MOUSE ESCORT BINDING failed error='{ex.GetType().Name}' readOnly=True. Existing recovery evaluation remains unchanged.");
                }
            }

            if (!_reportedPending)
            {
                _reportedPending = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT RECOVERY pending room='{room}' authenticatedCompatible={snapshot.AuthenticatedCompatible} requirementReadable={requirementReadable} revolutionReadable={revolutionReadable} exactSpawnerBound={nativeSpawner != null && resetSpawnCount != null && triggerSpawner != null} leaderReadable={leaderReadable} leaderReadStage='{leaderReadStage}'. Reevaluation is bounded and rate-limited; no attempt, progression flag, or AP check was consumed.");
            }
            return;
        }

        if (observation == MeatMouseEscortRecoveryObservation.Failed)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT RECOVERY failed room='{room}' reason='pending prerequisites exhausted bounded reevaluation' evaluations={_runtime.PendingEvaluationCount}. No native attempt, progression write, or AP check was consumed.");
            return;
        }

        if (observation == MeatMouseEscortRecoveryObservation.Skipped)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT RECOVERY skipped room='{room}' requirementStated={requirementStated} revolutionTriggered={revolutionTriggered} leaderPresent={leaderPresent}. Native state is not an interrupted eligible escort; no progression flags or AP checks changed.");
            return;
        }

        if (observation != MeatMouseEscortRecoveryObservation.Ready ||
            !_runtime.TryConsumeAttempt())
            return;

        try
        {
            resetSpawnCount!.Invoke(nativeSpawner, new object[] { 0 });
            triggerSpawner!.Invoke(nativeSpawner, Array.Empty<object>());
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT RECOVERY applied room='{room}' path='{MeatMouseEscortRecoveryPolicy.NativeSpawnerPath}' boundary='SpawnMeatAnimalCharacterOnDemand.SetSpawnCount(0)+OnTrigger()'. Native flags, bouncer state, and AP checks remain unchanged; this room lifetime will not retry.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] MEAT MOUSE ESCORT RECOVERY failed room='{room}' path='{MeatMouseEscortRecoveryPolicy.NativeSpawnerPath}' error='{ex.GetType().Name}'. No fallback spawn, progression write, or AP check was attempted.");
        }
    }

    private void TryEmitExactConditionDiagnostic(string room)
    {
        if (!_conditionDiagnosticRuntime.ShouldEvaluateFrame())
        {
            _conditionDiagnosticRuntime.AdvanceFrame();
            return;
        }

        GameObject? spawnCondition = null;
        GameObject? revolutionCondition = null;
        try
        {
            spawnCondition = GameObject.Find(
                MeatMouseEscortConditionDiagnosticPolicy.SpawnConditionPath);
            revolutionCondition = GameObject.Find(
                MeatMouseEscortConditionDiagnosticPolicy.RevolutionConditionPath);
        }
        catch { }

        bool exactSubtreeAvailable = spawnCondition != null && revolutionCondition != null &&
            string.Equals(
                BuildDiagnosticHierarchyPath(spawnCondition.transform),
                MeatMouseEscortConditionDiagnosticPolicy.SpawnConditionPath,
                StringComparison.Ordinal) &&
            string.Equals(
                BuildDiagnosticHierarchyPath(revolutionCondition.transform),
                MeatMouseEscortConditionDiagnosticPolicy.RevolutionConditionPath,
                StringComparison.Ordinal);
        if (_conditionDiagnosticRuntime.Observe(room, exactSubtreeAvailable) &&
            MeatMouseEscortConditionDiagnosticPolicy.ShouldEmit(
                room, _conditionDiagnosticEmitted, exactSubtreeAvailable))
        {
            _conditionDiagnosticEmitted = true;
            try
            {
                EmitExactConditionDiagnostic();
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT CONDITION DIAGNOSTIC failed error='{ex.GetType().Name}' readOnly=True.");
            }
        }
        else if (_conditionDiagnosticRuntime.Finished && !_conditionDiagnosticEmitted)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] MEAT MOUSE ESCORT CONDITION DIAGNOSTIC skipped reason='exact SpawnCondition/RevolutionNotTriggeredCondition subtree not available during bounded polls' readOnly=True.");
        }
    }

    private static void EmitExactConditionDiagnostic()
    {
        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? generalConditionType = gameAssembly?.GetType(
            "GeneralCondition", throwOnError: false, ignoreCase: false);

        foreach (string path in new[]
        {
            MeatMouseEscortConditionDiagnosticPolicy.SpawnConditionPath,
            MeatMouseEscortConditionDiagnosticPolicy.RevolutionConditionPath,
        })
        {
            if (!MeatMouseEscortConditionDiagnosticPolicy.ShouldInspectPath(path))
                continue;

            GameObject? scopedObject = null;
            try { scopedObject = GameObject.Find(path); }
            catch { }
            string actualPath = scopedObject == null
                ? "<missing>"
                : BuildDiagnosticHierarchyPath(scopedObject.transform);
            if (scopedObject == null ||
                !string.Equals(actualPath, path, StringComparison.Ordinal))
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT CONDITION OBJECT expected='{path}' actual='{actualPath}' exactMatch=False readOnly=True.");
                continue;
            }

            var rawComponents = new List<Component>();
            bool componentsReadable = true;
            try
            {
                foreach (Component component in scopedObject.GetComponents<Component>())
                {
                    if (component != null)
                        rawComponents.Add(component);
                    if (rawComponents.Count >= 16)
                        break;
                }
            }
            catch { componentsReadable = false; }
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT CONDITION OBJECT expected='{path}' actual='{actualPath}' exactMatch=True componentsReadable={componentsReadable} inspectedComponentCount={rawComponents.Count} cap=16 readOnly=True.");

            for (int index = 0; index < rawComponents.Count; index++)
            {
                Component component = rawComponents[index];
                IntPtr pointer = DiagnosticNativePointer(component);
                IntPtr klass = IntPtr.Zero;
                if (pointer != IntPtr.Zero)
                {
                    try { klass = mm_il2cpp_object_get_class(pointer); }
                    catch { }
                }
                string nativeClass = DiagnosticNativeClassFullName(klass);
                Type? nativeType = null;
                try
                {
                    nativeType = gameAssembly?.GetType(
                        nativeClass, throwOnError: false, ignoreCase: false);
                }
                catch { }
                bool exactGeneralCondition = generalConditionType != null &&
                    nativeType != null && generalConditionType.IsAssignableFrom(nativeType);
                object? wrapped = null;
                if (exactGeneralCondition && pointer != IntPtr.Zero)
                {
                    try
                    {
                        ConstructorInfo? constructor = nativeType!.GetConstructor(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            binder: null,
                            types: new[] { typeof(IntPtr) },
                            modifiers: null);
                        wrapped = constructor?.Invoke(new object[] { pointer });
                    }
                    catch { }
                }
                MeatMouseEscortConditionState state =
                    MeatMouseEscortConditionReader.Read(wrapped);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT CONDITION COMPONENT path='{path}' index={index} managed='{component.GetType().FullName ?? "<unknown>"}' native='{nativeClass}' exactGeneralCondition={exactGeneralCondition} wrapperCreated={wrapped != null} checkIfMetReadable={state.Readable} checkIfMet={(state.Readable ? state.Met.ToString() : "<unreadable>")} readStage='{state.Stage}' readOnly=True.");
            }
        }
    }

    private static void EmitNativeBindingDiagnostic()
    {
        const string nativeSpawnerTypeName = "SpawnMeatAnimalCharacterOnDemand";
        GameObject? container = null;
        try
        {
            container = GameObject.Find(MeatMouseEscortRecoveryPolicy.NativeSpawnerPath);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT BINDING path lookup failed path='{MeatMouseEscortRecoveryPolicy.NativeSpawnerPath}' error='{ex.GetType().Name}'. readOnly=True.");
        }

        string containerActualPath = container == null
            ? "<missing>"
            : BuildDiagnosticHierarchyPath(container.transform);
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MEAT MOUSE ESCORT BINDING PATH expected='{MeatMouseEscortRecoveryPolicy.NativeSpawnerPath}' objectFound={container != null} actual='{containerActualPath}' exactHierarchyMatch={string.Equals(containerActualPath, MeatMouseEscortRecoveryPolicy.NativeSpawnerPath, StringComparison.Ordinal)} readOnly=True.");

        Assembly? gameAssembly = ReflectionUtil.GameAssembly;
        Type? nativeSpawnerType = null;
        try
        {
            nativeSpawnerType = gameAssembly?.GetType(
                nativeSpawnerTypeName, throwOnError: false, ignoreCase: false);
        }
        catch { }

        object? il2CppType = null;
        bool il2CppTypeConverted = false;
        if (nativeSpawnerType != null)
        {
            try
            {
                il2CppType = Il2CppInterop.Runtime.Il2CppType.From(nativeSpawnerType);
                il2CppTypeConverted = il2CppType != null;
            }
            catch { }
        }

        ConstructorInfo? pointerConstructor = nativeSpawnerType?.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(IntPtr) },
            modifiers: null);
        MethodInfo? resetSpawnCount = nativeSpawnerType?.GetMethod(
            "SetSpawnCount",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(int) },
            modifiers: null);
        MethodInfo? triggerSpawner = nativeSpawnerType?.GetMethod(
            "OnTrigger",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MEAT MOUSE ESCORT BINDING TYPE gameAssemblyFound={gameAssembly != null} typeFound={nativeSpawnerType != null} il2CppTypeConverted={il2CppTypeConverted} pointerConstructorFound={pointerConstructor != null} setSpawnCountFound={resetSpawnCount != null} onTriggerFound={triggerSpawner != null} readOnly=True.");

        var scopedObjects = new List<(GameObject Object, string ExpectedPath, string Scope)>();
        if (container != null)
        {
            if (MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(
                    MeatMouseEscortRecoveryPolicy.NativeSpawnerPath))
            {
                scopedObjects.Add((
                    container,
                    MeatMouseEscortRecoveryPolicy.NativeSpawnerPath,
                    "container"));
            }

            try
            {
                for (int childIndex = 0; childIndex < container.transform.childCount; childIndex++)
                {
                    Transform child = container.transform.GetChild(childIndex);
                    if (child == null || child.gameObject == null)
                        continue;

                    string expectedChildPath =
                        MeatMouseEscortRecoveryPolicy.NativeSpawnerPath + "/" + child.name;
                    if (MeatMouseEscortBindingDiagnosticPolicy.ShouldInspectPath(expectedChildPath))
                    {
                        scopedObjects.Add((
                            child.gameObject,
                            expectedChildPath,
                            "direct-child"));
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT BINDING direct-child enumeration failed error='{ex.GetType().Name}'. readOnly=True.");
            }
        }

        var exactNativeMatches = new List<(Component Component, IntPtr Pointer, string Path)>();
        foreach ((GameObject scopedObject, string expectedPath, string scope) in scopedObjects)
        {
            string actualPath = BuildDiagnosticHierarchyPath(scopedObject.transform);
            GameObject? exactLookup = null;
            try { exactLookup = GameObject.Find(expectedPath); }
            catch { }
            bool exactObjectMatch = SameDiagnosticNativeObject(scopedObject, exactLookup);

            var rawComponents = new List<Component>();
            try
            {
                foreach (Component component in scopedObject.GetComponents<Component>())
                {
                    if (component != null)
                        rawComponents.Add(component);
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT BINDING OBJECT scope='{scope}' expected='{expectedPath}' actual='{actualPath}' exactObjectMatch={exactObjectMatch} componentsReadable=False error='{ex.GetType().Name}' readOnly=True.");
            }

            object? typedComponent = null;
            if (il2CppTypeConverted)
            {
                try
                {
                    typedComponent = scopedObject.GetComponent(
                        (Il2CppSystem.Type)il2CppType!);
                }
                catch { }
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT BINDING OBJECT scope='{scope}' expected='{expectedPath}' actual='{actualPath}' exactObjectMatch={exactObjectMatch} rawComponentCount={rawComponents.Count} typedGetComponentFound={typedComponent != null} readOnly=True.");

            for (int componentIndex = 0; componentIndex < rawComponents.Count; componentIndex++)
            {
                Component component = rawComponents[componentIndex];
                IntPtr pointer = DiagnosticNativePointer(component);
                IntPtr klass = IntPtr.Zero;
                if (pointer != IntPtr.Zero)
                {
                    try { klass = mm_il2cpp_object_get_class(pointer); }
                    catch { }
                }

                string nativeClass = DiagnosticNativeClassFullName(klass);
                bool exactNativeMatch = string.Equals(
                    nativeClass, nativeSpawnerTypeName, StringComparison.Ordinal);
                if (exactNativeMatch)
                    exactNativeMatches.Add((component, pointer, actualPath));

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MEAT MOUSE ESCORT BINDING COMPONENT path='{actualPath}' index={componentIndex} managed='{component.GetType().FullName ?? "<unknown>"}' native='{nativeClass}' exactNativeMatch={exactNativeMatch} pointerNonzero={pointer != IntPtr.Zero} readOnly=True.");
            }
        }

        string exactMatchLocations = exactNativeMatches.Count == 0
            ? "<none>"
            : string.Join("|", exactNativeMatches.Select(match => match.Path));
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MEAT MOUSE ESCORT BINDING MATCH exactType='{nativeSpawnerTypeName}' count={exactNativeMatches.Count} locations='{exactMatchLocations}' readOnly=True.");

        int wrapperCreatedCount = 0;
        int characterReadableCount = 0;
        int hasValueReadableCount = 0;
        for (int matchIndex = 0; matchIndex < exactNativeMatches.Count; matchIndex++)
        {
            (Component _, IntPtr pointer, string path) = exactNativeMatches[matchIndex];
            object? wrapped = null;
            if (pointerConstructor != null && pointer != IntPtr.Zero)
            {
                try { wrapped = pointerConstructor.Invoke(new object[] { pointer }); }
                catch { }
            }
            if (wrapped != null)
                wrapperCreatedCount++;

            MeatMouseEscortCharacterState characterState =
                MeatMouseEscortCharacterReader.Read(wrapped);
            bool characterReadable = characterState.Readable;
            bool hasValueReadable = characterState.Readable;
            bool? hasValue = characterState.Readable
                ? characterState.Present
                : null;
            if (characterReadable)
                characterReadableCount++;
            if (hasValueReadable)
                hasValueReadableCount++;

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MEAT MOUSE ESCORT BINDING WRAPPER index={matchIndex} path='{path}' pointerConstructorFound={pointerConstructor != null} wrapperCreated={wrapped != null} setSpawnCountFound={resetSpawnCount != null} onTriggerFound={triggerSpawner != null} characterReadable={characterReadable} hasValueReadable={hasValueReadable} hasValue={hasValue?.ToString() ?? "<unreadable>"} characterReadStage='{characterState.Stage}' readOnly=True.");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MEAT MOUSE ESCORT BINDING WRAPPER SUMMARY exactMatchCount={exactNativeMatches.Count} pointerConstructorFound={pointerConstructor != null} wrapperCreatedCount={wrapperCreatedCount} setSpawnCountFound={resetSpawnCount != null} onTriggerFound={triggerSpawner != null} characterReadableCount={characterReadableCount} hasValueReadableCount={hasValueReadableCount} readOnly=True.");
    }

    private static string BuildDiagnosticHierarchyPath(Transform transform)
    {
        try
        {
            var names = new List<string>();
            for (Transform? current = transform;
                 current != null && names.Count < 32;
                 current = current.parent)
                names.Add(current.name ?? "<unnamed>");
            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return "<unreadable>";
        }
    }

    private static bool SameDiagnosticNativeObject(GameObject expected, GameObject? actual)
    {
        if (actual == null)
            return false;
        IntPtr expectedPointer = DiagnosticNativePointer(expected);
        IntPtr actualPointer = DiagnosticNativePointer(actual);
        return expectedPointer != IntPtr.Zero && expectedPointer == actualPointer;
    }

    private static IntPtr DiagnosticNativePointer(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(value) is IntPtr pointer ? pointer : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static string DiagnosticNativeClassFullName(IntPtr klass)
    {
        if (klass == IntPtr.Zero)
            return "<no-class>";

        string ns = DiagnosticNativeAnsi(() => mm_il2cpp_class_get_namespace(klass));
        string name = DiagnosticNativeAnsi(() => mm_il2cpp_class_get_name(klass));
        if (string.IsNullOrWhiteSpace(name))
            name = "<unnamed-class>";
        return string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
    }

    private static string DiagnosticNativeAnsi(Func<IntPtr> getter)
    {
        try
        {
            IntPtr value = getter();
            return value == IntPtr.Zero
                ? string.Empty
                : Marshal.PtrToStringAnsi(value) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    [DllImport("GameAssembly.dll", CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "il2cpp_object_get_class")]
    private static extern IntPtr mm_il2cpp_object_get_class(IntPtr obj);

    [DllImport("GameAssembly.dll", CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "il2cpp_class_get_name")]
    private static extern IntPtr mm_il2cpp_class_get_name(IntPtr klass);

    [DllImport("GameAssembly.dll", CallingConvention = CallingConvention.Cdecl,
        EntryPoint = "il2cpp_class_get_namespace")]
    private static extern IntPtr mm_il2cpp_class_get_namespace(IntPtr klass);

    private void ResetForRoomExit()
    {
        if (string.IsNullOrEmpty(_boundRoom))
            return;

        _boundRoom = string.Empty;
        _initialiseDelayFrames = 0;
        _runtime.Reset();
        _conditionDiagnosticRuntime.Reset();
        _reportedPending = false;
        _bindingDiagnosticEmitted = false;
        _conditionDiagnosticEmitted = false;
    }
}

internal static class AreaAccessPrototype
{
    internal readonly record struct AreaDefinition(
        string AreaName,
        string ItemName,
        string PhoneRootName,
        string? ClothRelativePath,
        string[] HubRoomIds);

    internal static readonly AreaDefinition[] Areas =
    {
        new("Roots", "Roots Access", "BoxDoor_Roots", null, new[] { "GameRoom_Hub2" }),
        new("Lobby", "Lobby Access", "BoxDoor_Lobby", "Geom/HR06_PhoneTableCloth", new[] { "GameRoom_Hub1A", "GameRoom_Hub1B" }),
        new("Meat Dimension", "Meat Dimension Access", "BoxDoor_Meat", "Geom/HR06_PhoneTableCloth.01-tofb", new[] { "GameRoom_Hub4" }),
        // Hub6's internal "Prison" phone is the Cell Tower route seen in the travel scan.
        new("Cell Tower", "Cell Tower Access", "BoxDoor_Prison", "Geom/HR06_PhoneTableCloth.02-tofb", new[] { "GameRoom_Hub5A", "GameRoom_Hub5B" }),
        // Hub6's internal "Madness" phone is the Tower of Fear route seen in the travel scan.
        new("Tower of Fear", "Tower of Fear Access", "BoxDoor_Madness", "Geom/HR06_PhoneTableCloth.03-tofb", new[] { "GameRoom_Hub3" }),
        new("Royal Corridor", "Royal Corridor Access", "BoxDoor_Royal", "Geom/HR06_PhoneTableCloth.03-tofb", new[] { "GameRoom_Hub7" }),
    };

    private static readonly object Sync = new();
    private static readonly HashSet<string> UnlockedAreas =
        new(StringComparer.OrdinalIgnoreCase);
    private static bool _authenticatedCompatibleSession;

    public static bool Enabled { get; private set; }
    public static bool APDrivenStartingArea { get; private set; }
    public static string StartingArea { get; private set; } = "AP precollected item";
    public static bool AuthenticatedCompatibleSession
    {
        get
        {
            lock (Sync)
                return _authenticatedCompatibleSession;
        }
    }

    public static void Configure(bool enabled, string requestedStartingArea)
    {
        Enabled = enabled;

        string requested = requestedStartingArea?.Trim() ?? string.Empty;
        APDrivenStartingArea =
            requested.Equals("AP", StringComparison.OrdinalIgnoreCase) ||
            requested.Equals("Seed", StringComparison.OrdinalIgnoreCase) ||
            requested.Equals("Precollected", StringComparison.OrdinalIgnoreCase);

        StartingArea = APDrivenStartingArea
            ? "AP precollected item"
            : NormalizeAreaName(requestedStartingArea) ?? "Roots";

        lock (Sync)
        {
            _authenticatedCompatibleSession = false;
            UnlockedAreas.Clear();
            if (Enabled && !APDrivenStartingArea)
                UnlockedAreas.Add(StartingArea);
        }

        if (Enabled)
            LogStatus("configured");
    }

    public static bool TryApplyItem(string itemName)
    {
        if (!Enabled)
            return false;

        foreach (AreaDefinition area in Areas)
        {
            if (!string.Equals(area.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                continue;

            GrantArea(area.AreaName, $"AP item '{itemName}'");
            return true;
        }

        return false;
    }

    public static void ApplySlotDataStarter(Dictionary<string, object>? slotData)
    {
        if (!Enabled)
            return;

        if (slotData == null || slotData.Count == 0)
        {
            EndAuthenticatedSession();
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] AREA ACCESS SLOT DATA missing/empty; all major-area phones remain locked until a normal Area Access item is received.");
            return;
        }

        string implementationVersion = slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        bool compatible = AreaAccessSessionPolicy.IsAuthenticatedCompatible(
            Enabled,
            implementationVersion);
        lock (Sync)
            _authenticatedCompatibleSession = compatible;

        string starterItem = slotData.TryGetValue("starting_area_item", out object? rawStarter)
            ? rawStarter?.ToString() ?? string.Empty
            : string.Empty;

        string starterArea = slotData.TryGetValue("starting_area", out object? rawArea)
            ? rawArea?.ToString() ?? string.Empty
            : string.Empty;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] AREA ACCESS SLOT DATA implementation='{implementationVersion}' startingAreaItem='{starterItem}' startingArea='{starterArea}'.");

        if (!compatible)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] AREA ACCESS SEED INCOMPATIBLE: server slot data implementation='{implementationVersion}'. Generate AND HOST a NEW seed with APWorld v0.13; replacing the installed .apworld does not change an already-generated seed. All major-area phones remain locked for safety.");
            return;
        }

        if (!APDrivenStartingArea)
            return;

        if (!string.IsNullOrWhiteSpace(starterItem) && TryApplyItem(starterItem))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA ACCESS STARTER APPLIED FROM SLOT DATA item='{starterItem}'.");
            LogStatus("slot-data starter");
            return;
        }

        // Backward/fallback path: if a compatible APWorld supplies only the area
        // display name, still allow it to select the initial phone.
        string? normalized = NormalizeAreaName(starterArea);
        if (normalized != null)
        {
            GrantArea(normalized, $"AP slot data starting_area '{starterArea}'");
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA ACCESS STARTER APPLIED FROM SLOT DATA area='{normalized}' fallback=True.");
            LogStatus("slot-data starter fallback");
            return;
        }

        Plugin.LoggerInstance?.LogError(
            $"[SCRC-AP] AREA ACCESS SLOT DATA did not contain a recognized starter: starting_area_item='{starterItem}' starting_area='{starterArea}'.");
    }

    public static void EndAuthenticatedSession()
    {
        lock (Sync)
            _authenticatedCompatibleSession = false;
    }

    public static bool HasArea(string areaName)
    {
        if (!Enabled)
            return true;

        lock (Sync)
            return UnlockedAreas.Contains(areaName);
    }

    public static void GrantArea(string areaName, string source = "developer")
    {
        string? normalized = NormalizeAreaName(areaName);
        if (normalized == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA ACCESS ignored unknown area '{areaName}'.");
            return;
        }

        bool added;
        lock (Sync)
            added = UnlockedAreas.Add(normalized);

        Plugin.LoggerInstance?.LogWarning(
            added
                ? $"[SCRC-AP] AREA ACCESS UNLOCKED area='{normalized}' source='{source}'."
                : $"[SCRC-AP] AREA ACCESS already unlocked area='{normalized}' source='{source}'.");
    }

    public static void GrantNextLockedArea()
    {
        if (!Enabled)
            return;

        foreach (AreaDefinition area in Areas)
        {
            if (!HasArea(area.AreaName))
            {
                GrantArea(area.AreaName, "PAGE UP");
                LogStatus("after PAGE UP");
                return;
            }
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] AREA ACCESS PAGE UP: all six major areas are already unlocked.");
    }

    public static void ResetToStartingArea()
    {
        if (!Enabled)
            return;

        if (APDrivenStartingArea)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] AREA ACCESS PAGE DOWN ignored in AP-driven mode; received Area Access items are authoritative for this session.");
            LogStatus("PAGE DOWN ignored");
            return;
        }

        lock (Sync)
        {
            UnlockedAreas.Clear();
            UnlockedAreas.Add(StartingArea);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] AREA ACCESS RESET: only starting area '{StartingArea}' is unlocked.");
        LogStatus("after PAGE DOWN");
    }

    public static void LogStatus(string source = "END")
    {
        if (!Enabled)
            return;

        string status = string.Join(", ", Areas.Select(a =>
            $"{a.AreaName}={(HasArea(a.AreaName) ? "OPEN" : "LOCKED")}"));

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] AREA ACCESS STATUS source='{source}' startingArea='{StartingArea}' {status}.");
    }

    public static bool TryGetAreaForHubRoom(string roomId, out AreaDefinition area)
    {
        foreach (AreaDefinition candidate in Areas)
        {
            if (candidate.HubRoomIds.Any(id =>
                    string.Equals(id, roomId, StringComparison.Ordinal)))
            {
                area = candidate;
                return true;
            }
        }

        area = default;
        return false;
    }

    private static string? NormalizeAreaName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string candidate = value.Trim();
        foreach (AreaDefinition area in Areas)
        {
            if (string.Equals(area.AreaName, candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(area.ItemName, candidate, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(area.PhoneRootName, candidate, StringComparison.OrdinalIgnoreCase))
            {
                return area.AreaName;
            }
        }

        if (candidate.Equals("Meat", StringComparison.OrdinalIgnoreCase)) return "Meat Dimension";
        if (candidate.Equals("Cell", StringComparison.OrdinalIgnoreCase)) return "Cell Tower";
        if (candidate.Equals("Tower", StringComparison.OrdinalIgnoreCase) ||
            candidate.Equals("Madness", StringComparison.OrdinalIgnoreCase)) return "Tower of Fear";
        if (candidate.Equals("Royal", StringComparison.OrdinalIgnoreCase)) return "Royal Corridor";

        return null;
    }
}

internal sealed class AreaPhoneAccessKeeper : MonoBehaviour
{
    private const string Hub6RoomId = "GameRoom_Hub6";
    private const string PhoneBankRootPath = "Root/GameRoom_Hub6_Logic/Objects/Phones";

    private readonly Dictionary<string, GameObject> _phoneRoots =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GameObject> _phoneBoxes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GameObject> _onTriggers =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GameObject> _cloths =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _lastAppliedUnlockState =
        new(StringComparer.OrdinalIgnoreCase);

    private string _lastRoom = string.Empty;
    private float _nextPoll;
    private bool _missingRootLogged;

    public AreaPhoneAccessKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("AreaPhoneAccessKeeper.Update");
        if (!AreaAccessPrototype.Enabled)
            return;

        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, _lastRoom, StringComparison.Ordinal))
        {
            _lastRoom = room;
            ClearBindings();
            _nextPoll = 0f;
        }

        if (!string.Equals(room, Hub6RoomId, StringComparison.Ordinal))
            return;

        if (Time.unscaledTime < _nextPoll)
            return;

        _nextPoll = Time.unscaledTime + 0.20f;

        if (!EnsureBindings())
            return;

        foreach (AreaAccessPrototype.AreaDefinition area in AreaAccessPrototype.Areas)
        {
            bool unlocked = AreaAccessPrototype.HasArea(area.AreaName);

            if (_phoneRoots.TryGetValue(area.AreaName, out GameObject? root) && root != null && !root.activeSelf)
                root.SetActive(true);

            // PhoneBox owns the native Switch that registers the green interaction
            // prompt and starts the phone sequence. Disabling only OnTrigger is too
            // late: Switch can still accept the interaction, consume the prompt, and
            // only then emit a TransitionToGameRoomRequest. Keep the visible phone
            // root alive, but disable the whole invisible PhoneBox interaction object
            // while locked. Re-enabling PhoneBox gives vanilla a fresh Switch/OnTrigger
            // lifecycle immediately when Area Access is granted.
            if (_phoneBoxes.TryGetValue(area.AreaName, out GameObject? phoneBox) && phoneBox != null)
            {
                if (phoneBox.activeSelf != unlocked)
                    phoneBox.SetActive(unlocked);
            }

            if (_onTriggers.TryGetValue(area.AreaName, out GameObject? trigger) && trigger != null)
            {
                // Keep the child's own activeSelf consistent as a secondary guard.
                // When PhoneBox is locked this object is inactive-in-hierarchy anyway.
                if (trigger.activeSelf != unlocked)
                    trigger.SetActive(unlocked);
            }

            // Fresh saves leave the later Hub6 phones under vanilla cloth covers.
            // Area Access owns the randomizer-facing visual state, but does not
            // spoof the vanilla visited/open progression flags behind those covers.
            if (_cloths.TryGetValue(area.AreaName, out GameObject? cloth) && cloth != null)
            {
                bool shouldShowCloth = !unlocked;
                if (cloth.activeSelf != shouldShowCloth)
                    cloth.SetActive(shouldShowCloth);
            }

            if (!_lastAppliedUnlockState.TryGetValue(area.AreaName, out bool previous) || previous != unlocked)
            {
                _lastAppliedUnlockState[area.AreaName] = unlocked;
                string phoneBoxState = _phoneBoxes.TryGetValue(area.AreaName, out GameObject? pb) && pb != null
                    ? pb.activeSelf.ToString()
                    : "<missing>";
                string triggerState = _onTriggers.TryGetValue(area.AreaName, out GameObject? t) && t != null
                    ? t.activeSelf.ToString()
                    : "<missing>";
                string clothState = string.IsNullOrWhiteSpace(area.ClothRelativePath)
                    ? "<none>"
                    : (_cloths.TryGetValue(area.AreaName, out GameObject? c) && c != null
                        ? c.activeSelf.ToString()
                        : "<missing>");
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] AREA PHONE STATE area='{area.AreaName}' phoneRoot='{area.PhoneRootName}' unlocked={unlocked} phoneBoxActive={phoneBoxState} onTriggerActive={triggerState} clothActive={clothState}.");
            }
        }
    }

    private bool EnsureBindings()
    {
        if (_onTriggers.Count == AreaAccessPrototype.Areas.Length)
            return true;

        GameObject? bank = GameObject.Find(PhoneBankRootPath);
        if (bank == null)
        {
            if (!_missingRootLogged)
            {
                _missingRootLogged = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] AREA PHONE ROUTING waiting for Hub6 phone bank '{PhoneBankRootPath}'.");
            }
            return false;
        }

        _missingRootLogged = false;

        foreach (AreaAccessPrototype.AreaDefinition area in AreaAccessPrototype.Areas)
        {
            Transform? root = bank.transform.Find(area.PhoneRootName);
            Transform? phoneBox = root?.Find("PhoneBox");
            Transform? trigger = phoneBox?.Find("OnTrigger");
            Transform? cloth = null;
            if (root != null && !string.IsNullOrWhiteSpace(area.ClothRelativePath))
            {
                try { cloth = root.Find(area.ClothRelativePath); }
                catch { }
            }

            if (root != null) _phoneRoots[area.AreaName] = root.gameObject;
            if (phoneBox != null) _phoneBoxes[area.AreaName] = phoneBox.gameObject;
            if (trigger != null) _onTriggers[area.AreaName] = trigger.gameObject;
            if (cloth != null) _cloths[area.AreaName] = cloth.gameObject;

            string missingKey = "missing:" + area.AreaName;
            if (trigger == null && !_lastAppliedUnlockState.ContainsKey(missingKey))
            {
                _lastAppliedUnlockState[missingKey] = false;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] AREA PHONE BINDING MISSING area='{area.AreaName}' expected='{PhoneBankRootPath}/{area.PhoneRootName}/PhoneBox/OnTrigger'.");
            }
        }

        if (_onTriggers.Count == AreaAccessPrototype.Areas.Length)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] AREA PHONE BINDINGS READY: Lobby/Roots/Meat/Madness(=Tower of Fear)/Prison(=Cell Tower)/Royal OnTrigger reactors resolved.");
            return true;
        }

        return false;
    }

    private void ClearBindings()
    {
        _phoneRoots.Clear();
        _phoneBoxes.Clear();
        _onTriggers.Clear();
        _cloths.Clear();
        _lastAppliedUnlockState.Clear();
        _missingRootLogged = false;
    }
}

internal sealed class DeveloperHotkeys : MonoBehaviour
{
    public DeveloperHotkeys(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("DeveloperHotkeys.Update");
        if (!DeveloperHarness.Enabled)
            return;

        if (AreaAccessPrototype.Enabled)
        {
            if (Input.GetKeyDown(KeyCode.PageUp))
                AreaAccessPrototype.GrantNextLockedArea();

            if (Input.GetKeyDown(KeyCode.PageDown))
                AreaAccessPrototype.ResetToStartingArea();

            if (Input.GetKeyDown(KeyCode.End))
                AreaAccessPrototype.LogStatus();
        }

        if (Input.GetKeyDown(KeyCode.Home))
        {
            if (AreaAccessPrototype.Enabled &&
                string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.OrdinalIgnoreCase))
            {
                RootsAreaBaselineKeeper.LogCompactStatus();
            }
            else
            {
                PhoneBoothDiagnostic.ScanCurrentSceneAndArm();
            }
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            bool shift =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            if (shift)
                DeveloperHarness.CycleMusicLabPointOverride();
            else
                DeveloperHarness.StartPostAct1Discovery();
        }

        if (Input.GetKeyDown(KeyCode.F5))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            bool shift =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            if (!control && !alt && !shift)
            {
                QuestActionDiscovery.SnapshotKnownFlags();
                MusicLabDiscovery.ScanNativeIdentityCandidates();
                SpecialModeDiscovery.ScanCurrentScene();
            }

            if (shift)
                DeveloperHarness.RetryBunkerStarRequirementOnce();
            else
                DeveloperHarness.ScanPostAct1RouteObjects();
        }

    }
}

internal static class IntroHubSkip
{
    public static bool Enabled { get; private set; }
    public static bool Compatible { get; private set; }
    public static string TargetRoomId { get; private set; } = "GameRoom_Hub2";

    public static void Configure(bool directStartAtPhoneHub, bool legacyDirectStartAtLevelOne)
    {
        Compatible = false;

        if (directStartAtPhoneHub)
        {
            Enabled = true;
            TargetRoomId = "GameRoom_Hub6";
            return;
        }

        Enabled = legacyDirectStartAtLevelOne;
        TargetRoomId = "GameRoom_Hub2";
    }

    public static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = slotData != null &&
                                slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        Compatible = Enabled &&
                     implementation.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase);

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] INTRO DIRECT START SLOT CONTRACT synchronized=True enabled={Enabled} compatible={Compatible} implementation='{implementation}'.");
    }
}

internal static class IntroRoomToHubRedirectPatches
{
    private static bool _freshSavePending;
    private static bool _freshSaveRedirected;

    public static void NewSaveCreatedPostfix()
    {
        RootsPostLevelOnePresentation.NewSaveCreated();
        CassetteReceiptRandomization.OnLifecyclePoint("new player save created");
        _freshSavePending = IntroHubSkip.Enabled && IntroHubSkip.Compatible;
        _freshSaveRedirected = false;
        if (_freshSavePending)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] NEW SAVE DIRECT START ARMED: compatible slot data is synchronized; the exact intro-room transition will be redirected to the configured start hub.");
        }
    }

    public static bool Prefix(MethodBase __originalMethod, object? __instance, object[]? __args)
    {
        object? request = __args?.FirstOrDefault(a =>
            a != null && a.GetType().Name == "TransitionToGameRoomRequest");

        if (request == null)
            return true;

        object? room = ReflectionUtil.ReadMember(request, "RoomToGoTo")
                       ?? ReflectionUtil.ReadMember(request, "_RoomToGoTo_k__BackingField");

        string? roomId = room == null
            ? null
            : ReflectionUtil.ExtractIdentifier(room) ?? room.ToString();

        string originRoom = DeveloperHarness.CurrentRoomId;
        AreaAccessPrototype.AreaDefinition destinationArea = default;
        bool isMajorAreaHub = roomId != null &&
                              AreaAccessPrototype.TryGetAreaForHubRoom(
                                  roomId, out destinationArea);
        bool ownsDestination = !isMajorAreaHub || AreaAccessPrototype.HasArea(destinationArea.AreaName);
        AreaAccessDestinationDecision destinationDecision = AreaAccessDestinationPolicy.Decide(
            AreaAccessPrototype.Enabled,
            AreaAccessPrototype.AuthenticatedCompatibleSession,
            roomId,
            isMajorAreaHub,
            ownsDestination);
        if (destinationDecision == AreaAccessDestinationDecision.RedirectToMusicLab)
        {
            string lockedDestination = roomId!;
            object? musicLabRoom = BuildIdentifier(room!.GetType(), AreaAccessDestinationPolicy.MusicLabRoomId);
            bool rewrote = musicLabRoom != null &&
                           (WriteMember(request, "RoomToGoTo", musicLabRoom) ||
                            WriteMember(request, "_RoomToGoTo_k__BackingField", musicLabRoom));
            if (!rewrote)
            {
                Plugin.LoggerInstance?.LogError(
                    $"[SCRC-AP] AREA DESTINATION GUARD FAILED CLOSED area='{destinationArea.AreaName}' destination='{lockedDestination}' origin='{originRoom}' reason='could not rewrite locked destination to Music Lab'.");
                return false;
            }

            WriteMember(request, "PartOfLoadLevel", false);
            WriteMember(request, "_PartOfLoadLevel_k__BackingField", false);
            roomId = AreaAccessDestinationPolicy.MusicLabRoomId;

            // Preserve the existing Hub6 phone hard guard. World exits and
            // save restoration instead continue safely into Hub6.
            if (string.Equals(originRoom, AreaAccessDestinationPolicy.MusicLabRoomId, StringComparison.Ordinal))
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] AREA PHONE TRANSITION BLOCKED area='{destinationArea.AreaName}' destination='{lockedDestination}' origin='{originRoom}' reason='Area Access locked'; request target rewritten to '{roomId}'.");
                return false;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA DESTINATION REDIRECTED area='{destinationArea.AreaName}' destination='{lockedDestination}' origin='{originRoom}' redirect='{roomId}' reason='Area Access locked'. Native save, progression, visited, and story flags remain unchanged.");
        }

        if (__instance != null &&
            !RootsIntroCutsceneBypass.ShouldAllowTransition(
                __instance, __originalMethod, __args, roomId))
            return false;

        GarageCartridgeAccess.RecordGarageEntryTransitionRequest(originRoom, roomId);
        AreaArrivalPresentationOverride.ObserveTransition(originRoom, roomId);

        DeveloperHarness.CaptureGameFlow(__instance, request);

        if (!IntroHubSkip.Enabled)
            return true;

        bool redirectFreshSave = NewSaveDirectStartPolicy.Decide(
            IntroHubSkip.Enabled,
            IntroHubSkip.Compatible,
            _freshSavePending,
            _freshSaveRedirected,
            roomId) == NewSaveDirectStartDecision.Redirect;
        bool redirectLegacyIntro = NewSaveDirectStartPolicy.ShouldUseLegacyFallback(
            IntroHubSkip.Enabled,
            IntroHubSkip.Compatible,
            roomId);

        if (!redirectFreshSave && !redirectLegacyIntro)
            return true;

        Type roomType = room!.GetType();
        object? hubRoom = BuildIdentifier(roomType, IntroHubSkip.TargetRoomId);

        if (hubRoom == null)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Could not construct GameRoomIdentifier('{IntroHubSkip.TargetRoomId}') for intro skip.");
            return true;
        }

        bool wrote = WriteMember(request, "RoomToGoTo", hubRoom)
                     || WriteMember(request, "_RoomToGoTo_k__BackingField", hubRoom);

        if (!wrote)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Could not replace intro RoomToGoTo with {IntroHubSkip.TargetRoomId}.");
            return true;
        }

        // This is now a direct hub transition rather than a level-load room.
        WriteMember(request, "PartOfLoadLevel", false);
        WriteMember(request, "_PartOfLoadLevel_k__BackingField", false);

        DeveloperHarness.SetCurrentRoomForRedirect(IntroHubSkip.TargetRoomId);

        if (redirectFreshSave)
        {
            _freshSaveRedirected = true;
            _freshSavePending = false;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] INTRO SKIP: redirected room {roomId ?? "<unknown>"} -> {IntroHubSkip.TargetRoomId} source='{(redirectFreshSave ? "new-save" : "legacy-intro")}'.");

        return true;
    }

    private static object? BuildIdentifier(Type type, string value)
    {
        foreach (ConstructorInfo ctor in type.GetConstructors(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            ParameterInfo[] ps;
            try { ps = ctor.GetParameters(); }
            catch { continue; }

            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                try { return ctor.Invoke(new object[] { value }); }
                catch { }
            }
        }

        foreach (MethodInfo m in type.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            ParameterInfo[] ps;
            try { ps = m.GetParameters(); }
            catch { continue; }

            if (m.ReturnType == type &&
                ps.Length == 1 &&
                ps[0].ParameterType == typeof(string))
            {
                try { return m.Invoke(null, new object[] { value }); }
                catch { }
            }
        }

        return null;
    }

    private static bool WriteMember(object obj, string name, object value)
    {
        Type t = obj.GetType();

        PropertyInfo? p = t.GetProperty(
            name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (p != null && p.CanWrite)
        {
            try
            {
                p.SetValue(obj, value);
                return true;
            }
            catch { }
        }

        FieldInfo? f = t.GetField(
            name,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (f != null)
        {
            try
            {
                if (f.FieldType == typeof(bool) && value is bool b)
                {
                    f.SetValue(obj, b);
                    return true;
                }

                if (f.FieldType.IsInstanceOfType(value))
                {
                    f.SetValue(obj, value);
                    return true;
                }
            }
            catch { }
        }

        return false;
    }
}












internal sealed class MusicLabDiagnosticKeeper : MonoBehaviour
{
    private string _lastRoom = string.Empty;
    private float _nextMusicLabPoll;
    private float _nextRewardChestReconcile;
    private int _rewardChestReconcileAttempts;

    public MusicLabDiagnosticKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        using var timing = ClientPerformance.Measure("MusicLabDiagnosticKeeper.Update");
        if (!DeveloperHarness.Enabled)
            return;

        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, _lastRoom, StringComparison.Ordinal))
        {
            _lastRoom = room;
            _nextMusicLabPoll = 0f;
            _rewardChestReconcileAttempts = 0;
            _nextRewardChestReconcile = Time.unscaledTime + 1.0f;
        }

        // v0.67.39: do not create a new startup-time keeper. Reuse this already-stable
        // diagnostic component and only inspect native reward-chest objects after Hub6
        // is actually live. Retry briefly because the transition request can precede
        // scene object creation/save readiness.
        if (string.Equals(room, MusicLabDiscovery.Hub6RoomId, StringComparison.Ordinal) &&
            _rewardChestReconcileAttempts < 6 &&
            Time.unscaledTime >= _nextRewardChestReconcile)
        {
            _rewardChestReconcileAttempts++;
            _nextRewardChestReconcile = Time.unscaledTime + 1.0f;
            if (MusicLabDiscovery.ReconcileCollectedRewardChests())
                _rewardChestReconcileAttempts = 6;
        }

        if (string.Equals(room, MusicLabDiscovery.GarageRoomId, StringComparison.Ordinal))
        {
            // v0.67.21: the Garage selection is visible directly in the six known
            // cartridge holders. Poll only those six transforms at low frequency;
            // log only state transitions. This avoids reflection/current-song spam.
            if (Time.unscaledTime < _nextMusicLabPoll)
                return;

            _nextMusicLabPoll = Time.unscaledTime + 0.10f;
            MusicLabDiscovery.PollGarageCartridgeSelection();
            return;
        }

    }
}

internal static class MusicLabDiscovery
{
    public const string Hub6RoomId = "GameRoom_Hub6";
    public const string GarageRoomId = "GameRoom_27";
    private const string Hub6PhoneBankRootPath = "Root/GameRoom_Hub6_Logic/Objects/Phones";

    private static readonly object Sync = new();
    private static readonly CassettePostLoadSnapshotLiveGate PostLoadSnapshotLiveGate = new();
    private static readonly string[] GarageSequenceNames =
    {
        "PlayBloodyTears",
        "PlayGradiusRemix",
        "PlaySmooch",
        "PlaySuperstar",
        "PlayVampireKiller",
        "PlayWagTheDog",
    };

    private static readonly (string Song, string RelativePath)[] GarageCartridgeObjects =
    {
        ("Bloody Tears", "CartridgeHolder_BloodyTears/GR27_GameCartridge_BloodyTears"),
        ("Gradius Remix", "CartridgeHolder_LoveShine/GR27_GameCartridge_Gradius"),
        ("Smooch", "CartridgeHolder_Smooch/GR27_GameCartridge_Smooch"),
        ("Superstar", "CartridgeHolder_StarEater/GR27_GameCartridge_Superstar"),
        ("Vampire Killer", "CartridgeHolder_VampireKiller/GR27_GameCartridge_VampireKiller"),
        ("Wag the Dog", "CartridgeHolder_SuperCrazyRhythmCastle/GR27_GameCartridge_WagTheDog"),
    };

    private static bool _active;
    private static int _sequence;
    private static string _currentGarageSong = string.Empty;
    private static bool _musicLabStageInProgress;
    private static int _garageInteractionSnapshot;
    private static bool _garageCartridgeBaselineReady;
    private static readonly Dictionary<string, bool> GarageCartridgeBaselineActive = new(StringComparer.Ordinal);
    private static string _garagePickupCandidate = string.Empty;

    // v0.67.21: score/medal enquiry entry points remain cached and read-only.
    // Garage identity is captured separately from the six cartridge-holder
    // transforms, avoiding the unusable managed Component wrappers.
    private static bool _scoringProbeResolved;
    private static MethodInfo? _getCurrentSongMethod;
    private static MethodInfo? _getMostRecentGarageStickerMethod;
    private static MethodInfo? _getMostRecentCleanMedalMethod;
    private static MethodInfo? _getMostRecentStarRatingMethod;


    public static bool IsMusicLabRoom(string? room) =>
        string.Equals(room, Hub6RoomId, StringComparison.Ordinal) ||
        string.Equals(room, GarageRoomId, StringComparison.Ordinal);

    public static void ManualBegin()
    {
        lock (Sync)
        {
            _active = true;
            _sequence = 0;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== MUSIC LAB / GAME GARAGE DISCOVERY START ===== currentRoom='{DeveloperHarness.CurrentRoomId}'.");

        Record(
            "BASELINE",
            "Hub6 contains the phone hub + Music Lab. Cassette songs use non-default variants and award Bronze/Silver/Gold/Platinum worth 1/2/3/4 Music Lab points. GameRoom_27 is the Game Garage and its six cartridge songs share the same medal-point economy. Known chest thresholds: 5,10,20,32,46,64,89,111,140. Known rewards now mapped: 5=Old Game Data/SCGMD memory card; 10=Space Cartridge/Gradius Remix; 20=Car Battery/Meoo route; 32=Quicksand Cassette; 46=Bloodstained/Bloody Tears Cartridge. Focus: exact Garage identity from cartridge pickup/insertion state inside GameRoom_27, direct medal/star evaluations, native point source/threshold conditions, and later chest reward state without modifying progression.");
    }

    public static void RecordTransition(string roomId)
    {
        string originRoom = DeveloperHarness.CurrentRoomId;
        bool fromHub6 = string.Equals(originRoom, Hub6RoomId, StringComparison.Ordinal);
        bool toHub6 = string.Equals(roomId, Hub6RoomId, StringComparison.Ordinal);
        bool toGarage = string.Equals(roomId, GarageRoomId, StringComparison.Ordinal);

        lock (Sync)
        {
            if (fromHub6 &&
                !toHub6 &&
                !toGarage &&
                roomId.StartsWith("GameRoom_", StringComparison.Ordinal))
            {
                _musicLabStageInProgress = true;
            }

            if (toHub6)
            {
                _musicLabStageInProgress = false;
            }

            if (toGarage)
            {
                _currentGarageSong = string.Empty;
                _garageCartridgeBaselineReady = false;
                GarageCartridgeBaselineActive.Clear();
                _garagePickupCandidate = string.Empty;
            }
        }

        if (fromHub6 || toHub6 || toGarage)
            Record("ROOM TRANSITION", $"{originRoom} -> {roomId}");
    }

    private const int Flamenco64CollectionFlag = 150002;
    private const string Flamenco64CollectionFlagName = "SONG_CASSETTE_COLLECTED_FLAMENCO";
    private const string TenFour89CollectionFlagName = "SONG_CASSETTE_COLLECTED_TEN_FOUR_GOOD_BUDDY";
    private const string Zen111CollectionFlagName = "SONG_CASSETTE_COLLECTED_ZEN";
    private const string Wiggle140CollectionFlagName = "SONG_CASSETTE_COLLECTED_WIGGLE";

    private static readonly (int Threshold, string ChestName)[] MusicLabRewardChests =
    {
        (5, "ManiacMemoryCardChest"),
        (10, "GarageCartridgeChest_Gradius"),
        (20, "CatBatteryChest"),
        (32, "SongChest_QuickSand"),
        (46, "GarageCartridgeChest_BloodyTears"),
        (64, "SongChest_Flamenco"),
        (89, "SongChest_TenFourGoodBuddy"),
        (111, "SongChest_Zen"),
        (140, "SongChest_Wiggle"),
    };

    private static readonly Dictionary<int, int> MusicLabRewardThresholdByFlag = new();
    private static readonly Dictionary<int, int> MusicLabRewardFlagByThreshold = new();

    public static void RecordProgressionRequest(object request, string flag)
    {
        string room = DeveloperHarness.CurrentRoomId;
        int? numericFlag = ReadNumericProgressionFlag(
            ReflectionUtil.ReadMember(request, "Flag")
            ?? ReflectionUtil.ReadMember(request, "_Flag_k__BackingField"));
        bool? value = ReflectionUtil.ReadBool(request, "Value")
                      ?? ReflectionUtil.ReadBool(request, "_Value_k__BackingField");

        if (string.Equals(room, Hub6RoomId, StringComparison.Ordinal) && value != false)
            TryQueueMusicLabRewardChest(flag, numericFlag, "progression request");

        if (_active && IsMusicLabRoom(room))
            Record("PROGRESSION REQUEST", $"flag='{flag}' numeric={numericFlag?.ToString() ?? "?"} value={value?.ToString() ?? "?"}");
    }

    public static void RecordProgressionFlagUpdated(object evt, string flag)
    {
        string room = DeveloperHarness.CurrentRoomId;
        int? numericFlag = ReadNumericProgressionFlag(
            ReflectionUtil.ReadMember(evt, "ProgressionFlag")
            ?? ReflectionUtil.ReadMember(evt, "_ProgressionFlag_k__BackingField"));
        bool? isSet = ReflectionUtil.ReadBool(evt, "FlagIsSet")
                      ?? ReflectionUtil.ReadBool(evt, "_FlagIsSet_k__BackingField");
        bool? wasSet = ReflectionUtil.ReadBool(evt, "FlagWasSet")
                       ?? ReflectionUtil.ReadBool(evt, "_FlagWasSet_k__BackingField");

        if (string.Equals(room, Hub6RoomId, StringComparison.Ordinal) && isSet != false)
            TryQueueMusicLabRewardChest(flag, numericFlag, "progression flag event");

        if (_active && IsMusicLabRoom(room))
            Record("PROGRESSION FLAG UPDATED", $"flag='{flag}' numeric={numericFlag?.ToString() ?? "?"} isSet={isSet?.ToString() ?? "?"} wasSet={wasSet?.ToString() ?? "?"}");
    }

    private static string MusicLabRewardLocationName(int threshold)
    {
        return threshold switch
        {
            5 => LocationMap.MusicLab5PointChestLocationName,
            10 => LocationMap.MusicLab10PointChestLocationName,
            20 => LocationMap.MusicLab20PointChestLocationName,
            32 => LocationMap.MusicLab32PointChestLocationName,
            46 => LocationMap.MusicLab46PointChestLocationName,
            64 => LocationMap.MusicLab64PointChestLocationName,
            89 => LocationMap.MusicLab89PointChestLocationName,
            111 => LocationMap.MusicLab111PointChestLocationName,
            140 => LocationMap.MusicLab140PointChestLocationName,
            _ => $"Music Lab - {threshold} Point Chest",
        };
    }

    private static string MusicLabRewardChestName(int threshold)
    {
        foreach ((int candidateThreshold, string chestName) in MusicLabRewardChests)
        {
            if (candidateThreshold == threshold)
                return chestName;
        }
        return "<unknown>";
    }

    private static void TryQueueMusicLabRewardChest(string flag, int? numericFlag, string source)
    {
        int threshold = 0;

        if (numericFlag.HasValue)
        {
            lock (Sync)
                MusicLabRewardThresholdByFlag.TryGetValue(numericFlag.Value, out threshold);
        }

        // The four late song rewards were already observed directly in earlier logs;
        // retain them as a fallback if an event arrives before the live Hub6 scan.
        if (threshold == 0 &&
            (numericFlag == Flamenco64CollectionFlag ||
             string.Equals(flag, Flamenco64CollectionFlagName, StringComparison.OrdinalIgnoreCase)))
            threshold = 64;
        else if (threshold == 0 && string.Equals(flag, TenFour89CollectionFlagName, StringComparison.OrdinalIgnoreCase))
            threshold = 89;
        else if (threshold == 0 && string.Equals(flag, Zen111CollectionFlagName, StringComparison.OrdinalIgnoreCase))
            threshold = 111;
        else if (threshold == 0 && string.Equals(flag, Wiggle140CollectionFlagName, StringComparison.OrdinalIgnoreCase))
            threshold = 140;

        if (threshold == 0 && string.Equals(DeveloperHarness.CurrentRoomId, Hub6RoomId, StringComparison.Ordinal))
        {
            TryDiscoverMusicLabRewardChestFlags();
            if (numericFlag.HasValue)
            {
                lock (Sync)
                    MusicLabRewardThresholdByFlag.TryGetValue(numericFlag.Value, out threshold);
            }
        }

        if (threshold == 0)
            return;

        string chest = MusicLabRewardChestName(threshold);
        string locationName = MusicLabRewardLocationName(threshold);

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB {threshold}-POINT CHEST COLLECTED source='{source}' flag='{flag}' numeric={numericFlag?.ToString() ?? "?"} chest='{chest}'.");
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] AP LOCATION '{locationName}' source='Music Lab {threshold}-point reward chest'.");
        if (!CassetteSourceRandomization.QueueCatalogSourceByLocation(
                locationName,
                $"Music Lab {threshold}-point reward chest flag='{flag}' numeric={numericFlag?.ToString() ?? "?"}"))
        {
            Plugin.AP?.QueueLocation(locationName);
        }
    }

    private static bool TryDiscoverMusicLabRewardChestFlags()
    {
        if (!string.Equals(DeveloperHarness.CurrentRoomId, Hub6RoomId, StringComparison.Ordinal))
            return false;

        int found = 0;
        foreach ((int expectedThreshold, string chestName) in MusicLabRewardChests)
        {
            string interactionPath =
                $"Root/GameRoom_Hub6_Logic/Objects/RewardChests/{chestName}/Interaction";
            GameObject? interaction = GameObject.Find(interactionPath);
            if (interaction == null)
                continue;

            if (!TryReadNativeMusicLabRewardChestMetadata(interaction, out int threshold, out int progressionFlag))
                continue;

            // The native unlockRequirement is authoritative. If a future build moves a
            // chest, log it rather than silently assigning the flag to the wrong AP check.
            if (threshold != expectedThreshold)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB REWARD CHEST metadata mismatch chest='{chestName}' expectedThreshold={expectedThreshold} nativeThreshold={threshold}; skipped.");
                continue;
            }

            lock (Sync)
            {
                MusicLabRewardFlagByThreshold[threshold] = progressionFlag;
                MusicLabRewardThresholdByFlag[progressionFlag] = threshold;
            }

            found++;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB REWARD CHEST MAP threshold={threshold} chest='{chestName}' nativeCollectionFlag={progressionFlag}.");
        }

        return found == MusicLabRewardChests.Length;
    }

    private static bool TryReadNativeMusicLabRewardChestMetadata(
        GameObject interaction,
        out int threshold,
        out int progressionFlag)
    {
        threshold = 0;
        progressionFlag = 0;

        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = interaction.GetComponents<Component>(); }
        catch { return false; }

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            IntPtr obj = GetNativePointer(component);
            if (obj == IntPtr.Zero)
                continue;

            IntPtr klass = IntPtr.Zero;
            try { klass = ml_il2cpp_object_get_class(obj); }
            catch { }
            if (klass == IntPtr.Zero)
                continue;

            string className = NativeClassFullName(klass);
            if (!className.EndsWith("Hub06MedalScoreRewardChest", StringComparison.Ordinal))
                continue;

            string thresholdText = string.Empty;
            string flagText = string.Empty;
            IntPtr current = klass;
            for (int depth = 0; current != IntPtr.Zero && depth < 5; depth++)
            {
                IntPtr iter = IntPtr.Zero;
                while (true)
                {
                    IntPtr field;
                    try { field = ml_il2cpp_class_get_fields(current, ref iter); }
                    catch { field = IntPtr.Zero; }
                    if (field == IntPtr.Zero)
                        break;

                    string fieldName = NativeAnsi(() => ml_il2cpp_field_get_name(field));
                    if (!string.Equals(fieldName, "unlockRequirement", StringComparison.Ordinal) &&
                        !string.Equals(fieldName, "chestUnlockedProgressionFlag", StringComparison.Ordinal))
                        continue;

                    IntPtr fieldType = IntPtr.Zero;
                    try { fieldType = ml_il2cpp_field_get_type(field); }
                    catch { }
                    string value = NativeFieldValue(field, obj, NativeTypeName(fieldType));

                    if (string.Equals(fieldName, "unlockRequirement", StringComparison.Ordinal))
                        thresholdText = value;
                    else
                        flagText = value;
                }

                try { current = ml_il2cpp_class_get_parent(current); }
                catch { current = IntPtr.Zero; }
            }

            threshold = ParseTrailingNativeInt(thresholdText);
            progressionFlag = ParseTrailingNativeInt(flagText);
            return threshold > 0 && progressionFlag > 0;
        }

        return false;
    }

    private static int ParseTrailingNativeInt(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        int equals = text.LastIndexOf('=');
        string candidate = equals >= 0 ? text[(equals + 1)..] : text;
        if (int.TryParse(candidate.Trim(), out int direct))
            return direct;

        int end = text.Length - 1;
        while (end >= 0 && !char.IsDigit(text[end]) && text[end] != '-')
            end--;
        if (end < 0)
            return 0;

        int start = end;
        while (start > 0 && (char.IsDigit(text[start - 1]) || text[start - 1] == '-'))
            start--;

        return int.TryParse(text[start..(end + 1)], out int parsed) ? parsed : 0;
    }

    public static bool ReconcileCollectedRewardChests()
    {
        if (!string.Equals(DeveloperHarness.CurrentRoomId, Hub6RoomId, StringComparison.Ordinal))
            return false;

        if (!TryDiscoverMusicLabRewardChestFlags())
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE deferred: not all nine live Hub6 reward-chest metadata mappings are available yet.");
            return false;
        }

        Type? enquiries = FindExactGameType("CurrentPlayerSaveEnquiries");
        if (enquiries == null)
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE deferred: CurrentPlayerSaveEnquiries is not available yet.");
            return false;
        }

        MethodInfo? flagGetter = null;
        try
        {
            flagGetter = enquiries.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m =>
                {
                    if (m.ReturnType != typeof(bool))
                        return false;

                    ParameterInfo[] ps;
                    try { ps = m.GetParameters(); }
                    catch { return false; }

                    if (ps.Length != 1)
                        return false;

                    string parameterTypeName = ps[0].ParameterType.Name;
                    if (!parameterTypeName.Contains("GameProgressionFlag", StringComparison.OrdinalIgnoreCase))
                        return false;

                    return m.Name.Contains("ProgressionFlag", StringComparison.OrdinalIgnoreCase)
                           || m.Name.Contains("GameProgression", StringComparison.OrdinalIgnoreCase)
                           || m.Name.Contains("FlagSet", StringComparison.OrdinalIgnoreCase);
                });
        }
        catch { }

        if (flagGetter == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE could not find a static bool CurrentPlayerSaveEnquiries progression-flag getter.");
            return false;
        }

        ParameterInfo[] parameters;
        try { parameters = flagGetter.GetParameters(); }
        catch { return false; }
        Type flagType = parameters[0].ParameterType;

        int collected = 0;
        foreach ((int threshold, string chestName) in MusicLabRewardChests)
        {
            int numericFlag;
            lock (Sync)
            {
                if (!MusicLabRewardFlagByThreshold.TryGetValue(threshold, out numericFlag))
                    return false;
            }

            try
            {
                object flagValue;
                if (flagType.IsEnum)
                    flagValue = Enum.ToObject(flagType, numericFlag);
                else
                    flagValue = Activator.CreateInstance(flagType, numericFlag)
                                ?? throw new InvalidOperationException($"Could not construct {flagType.FullName} from {numericFlag}.");

                object? raw = flagGetter.Invoke(null, new[] { flagValue });
                if (raw is bool isSet && isSet)
                {
                    collected++;
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE collected threshold={threshold} chest='{chestName}' nativeCollectionFlag={numericFlag} via='{enquiries.Name}.{flagGetter.Name}'.");
                    TryQueueMusicLabRewardChest(flagValue.ToString() ?? numericFlag.ToString(), numericFlag, "save reconciliation");
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE failed threshold={threshold} flag={numericFlag} via='{enquiries.Name}.{flagGetter.Name}': {ex.GetBaseException().Message}");
                return false;
            }
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] MUSIC LAB REWARD CHEST RECONCILE COMPLETE getter='{enquiries.Name}.{flagGetter.Name}' collected={collected}/9.");
        return true;
    }

    private static int? ReadNumericProgressionFlag(object? value)
    {
        value = ReflectionUtil.UnwrapNullable(value);
        if (value == null)
            return null;

        try
        {
            if (value is IConvertible)
                return Convert.ToInt32(value);
        }
        catch { }

        string text = value.ToString() ?? string.Empty;
        if (int.TryParse(text, out int parsed))
            return parsed;

        return null;
    }

    public static void RecordBagItem(object request)
    {
        if (!_active || !IsMusicLabRoom(DeveloperHarness.CurrentRoomId))
            return;

        DumpRelevantMembers(
            request,
            "BAG ITEM REQUEST",
            new[] { "bag", "item", "identifier", "cassette", "cartridge", "memory" });
    }

    public static void RecordAbilityItem(object request)
    {
        if (!_active || !IsMusicLabRoom(DeveloperHarness.CurrentRoomId))
            return;

        DumpRelevantMembers(
            request,
            "ABILITY ITEM REQUEST",
            new[] { "ability", "item", "identifier" });
    }

    public static void PollGarageCartridgeSelection()
    {
        if (!string.Equals(DeveloperHarness.CurrentRoomId, GarageRoomId, StringComparison.Ordinal))
            return;

        const string rootPath = "Root/GameRoom_27_Logic/Objects/Cartridges";
        GameObject? root = GameObject.Find(rootPath);
        if (root == null)
            return;

        // Establish a per-room baseline only after all six known cartridge
        // objects resolve. In randomized progression some may already be
        // inactive; only cartridges that were active on entry can become a
        // pickup/insert candidate during this visit.
        lock (Sync)
        {
            if (!_garageCartridgeBaselineReady)
            {
                var resolved = new List<(string Song, bool Active)>();
                foreach (var cartridge in GarageCartridgeObjects)
                {
                    Transform? tr = null;
                    try { tr = root.transform.Find(cartridge.RelativePath); }
                    catch { }

                    if (tr == null || tr.gameObject == null)
                        return;

                    bool active = false;
                    try { active = tr.gameObject.activeSelf; }
                    catch { }
                    if (GarageCartridgeAccess.Enabled && !GarageCartridgeAccess.HasCartridge(cartridge.Song))
                        active = false;
                    resolved.Add((cartridge.Song, active));
                }

                GarageCartridgeBaselineActive.Clear();
                foreach (var entry in resolved)
                    GarageCartridgeBaselineActive[entry.Song] = entry.Active;

                _garageCartridgeBaselineReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] MUSIC LAB GARAGE CARTRIDGE BASELINE READY: " +
                    string.Join(", ", resolved.Select(x => $"{x.Song}={(x.Active ? "active" : "inactive")}")) + ".");
                return;
            }
        }

        var missing = new List<string>();
        var inactive = new List<string>();

        foreach (var cartridge in GarageCartridgeObjects)
        {
            if (GarageCartridgeAccess.Enabled && !GarageCartridgeAccess.HasCartridge(cartridge.Song))
                continue;

            bool baselineActive;
            lock (Sync)
            {
                if (!GarageCartridgeBaselineActive.TryGetValue(cartridge.Song, out baselineActive) || !baselineActive)
                    continue;
            }

            Transform? tr = null;
            try { tr = root.transform.Find(cartridge.RelativePath); }
            catch { }

            if (tr == null || tr.gameObject == null)
            {
                missing.Add(cartridge.Song);
                continue;
            }

            bool active = true;
            try { active = tr.gameObject.activeSelf; }
            catch { }
            if (!active)
                inactive.Add(cartridge.Song);
        }

        // v0.67.20 proved the inserted Bloody Tears holder copy is present but
        // activeSelf=false while the other floor cartridges remain active.
        // Treat exactly one such transition as authoritative.
        if (inactive.Count == 1)
        {
            string song = inactive[0];
            bool changed;
            lock (Sync)
            {
                changed = !string.Equals(_currentGarageSong, song, StringComparison.Ordinal);
                _currentGarageSong = song;
                _garagePickupCandidate = song;
            }

            if (changed)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE SONG CAPTURE source='cartridge holder inactive after insert' song='{song}'.");
                if (_active)
                    Record("GARAGE CARTRIDGE INSERTED", $"song='{song}'");
            }
            return;
        }

        // While carried, the selected cartridge is temporarily reparented out
        // of the Cartridges subtree. This is only a candidate; insertion above
        // is the confirmation used for result identity.
        if (inactive.Count == 0 && missing.Count == 1)
        {
            string song = missing[0];
            bool changed;
            lock (Sync)
            {
                changed = !string.Equals(_garagePickupCandidate, song, StringComparison.Ordinal);
                _garagePickupCandidate = song;
            }

            if (changed)
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE CARTRIDGE PICKUP CANDIDATE song='{song}'.");
            return;
        }

        // If the held cartridge is returned to its floor holder without being
        // inserted, clear only the candidate. Keep any confirmed song cached
        // until another insertion or the room changes.
        if (inactive.Count == 0 && missing.Count == 0)
        {
            lock (Sync)
                _garagePickupCandidate = string.Empty;
        }
    }

    public static void RecordScoredSongRequest(object request)
    {
        string currentRoom = DeveloperHarness.CurrentRoomId;
        bool isGarage = string.Equals(currentRoom, GarageRoomId, StringComparison.Ordinal);
        bool isCassetteStage;
        lock (Sync)
            isCassetteStage = _musicLabStageInProgress && !isGarage;

        // v0.67.28 attempted the scored-song request as an early identity source; some cassette starts do not issue it.
        // Garage already used it as a fallback; cassette discovery now observes
        // it too so unknown cassettes can be identified without finishing them.
        if (!isGarage && !isCassetteStage)
            return;

        object? songValue = null;
        string sourceMember = string.Empty;

        foreach (string name in new[]
        {
            "ScoredSong", "Song", "PlayableSong", "SongToScore", "CurrentlyScoredSong",
            "_ScoredSong_k__BackingField", "_Song_k__BackingField", "zScoredSong"
        })
        {
            object? candidate = ReflectionUtil.ReadMember(request, name);
            candidate = ReflectionUtil.UnwrapNullable(candidate);
            if (candidate == null)
                continue;

            string? text = ReflectionUtil.ExtractIdentifier(candidate);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            songValue = candidate;
            sourceMember = name;
            break;
        }

        // If the exact generated member name differs, narrowly inspect only this
        // request type for fields/properties whose name or type mentions Song.
        if (songValue == null)
        {
            Type type = request.GetType();

            try
            {
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    string fieldName = field.Name ?? string.Empty;
                    string fieldType = field.FieldType?.Name ?? string.Empty;
                    if (!fieldName.Contains("song", StringComparison.OrdinalIgnoreCase) &&
                        !fieldType.Contains("song", StringComparison.OrdinalIgnoreCase))
                        continue;

                    object? candidate = null;
                    try { candidate = ReflectionUtil.UnwrapNullable(field.GetValue(request)); } catch { }
                    string? text = ReflectionUtil.ExtractIdentifier(candidate);
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MUSIC LAB GARAGE SCORED SONG REQUEST FIELD name='{fieldName}' type='{field.FieldType.FullName}' value='{text ?? "<null/unreadable>"}'.");

                    if (songValue == null && candidate != null && !string.IsNullOrWhiteSpace(text))
                    {
                        songValue = candidate;
                        sourceMember = fieldName;
                    }
                }
            }
            catch { }

            try
            {
                foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    if (property.GetIndexParameters().Length != 0)
                        continue;

                    string propertyName = property.Name ?? string.Empty;
                    string propertyType = property.PropertyType?.Name ?? string.Empty;
                    if (!propertyName.Contains("song", StringComparison.OrdinalIgnoreCase) &&
                        !propertyType.Contains("song", StringComparison.OrdinalIgnoreCase))
                        continue;

                    object? candidate = null;
                    try { candidate = ReflectionUtil.UnwrapNullable(property.GetValue(request)); } catch { }
                    string? text = ReflectionUtil.ExtractIdentifier(candidate);
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MUSIC LAB GARAGE SCORED SONG REQUEST PROPERTY name='{propertyName}' type='{property.PropertyType.FullName}' value='{text ?? "<null/unreadable>"}'.");

                    if (songValue == null && candidate != null && !string.IsNullOrWhiteSpace(text))
                    {
                        songValue = candidate;
                        sourceMember = propertyName;
                    }
                }
            }
            catch { }
        }

        string rawSong = ReflectionUtil.ExtractIdentifier(songValue) ?? string.Empty;

        if (isGarage)
        {
            string friendly = FriendlyGarageSong(rawSong);
            if (!string.IsNullOrWhiteSpace(friendly))
            {
                lock (Sync)
                    _currentGarageSong = friendly;

                Record(
                    "GARAGE SONG SELECTED",
                    $"song='{friendly}' raw='{rawSong}' source='SetScoredSongInCurrentLevelRequest.{sourceMember}'.");
            }
            else
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE scored-song request observed but song identity could not be decoded; requestType='{request.GetType().FullName}'.");
            }

            return;
        }

        // Cassette quick-discovery path. We intentionally do not queue an AP
        // location here: a song start is identity-only and awards no medal.
        // The normal result path remains authoritative for Bronze/Silver/Gold/Platinum.
        string displaySong = FriendlyCassetteSongFromRawSong(rawSong);
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB CASSETTE START IDENTITY room='{currentRoom}' rawSong='{(string.IsNullOrWhiteSpace(rawSong) ? "<unavailable>" : rawSong)}' display='{displaySong}' source='SetScoredSongInCurrentLevelRequest.{(string.IsNullOrWhiteSpace(sourceMember) ? "<unknown>" : sourceMember)}'. Start-only discovery: safe to quit back to Hub6 after this line appears.");

        DumpRelevantMembers(
            request,
            "CASSETTE START SCORED SONG REQUEST",
            new[] { "song", "track", "level", "variant" });
    }

    private static string FriendlyCassetteSongFromRawSong(string rawSong)
    {
        if (string.IsNullOrWhiteSpace(rawSong))
            return "<unavailable>";

        string raw = rawSong.Trim();
        foreach (string prefix in new[] { "ePlayableSong_", "PlayableSong_", "Song_" })
        {
            if (raw.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                raw = raw[prefix.Length..];
                break;
            }
        }

        // Preserve the exact raw identifier in the log; this display form is
        // only for readability and is not used as an AP key.
        string[] words = raw.Replace('-', '_')
            .Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return rawSong;

        return string.Join(" ", words.Select(word =>
            word.Length <= 2
                ? word.ToUpperInvariant()
                : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));
    }

    private static string FriendlyGarageSong(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string compact = new string(raw.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        if (compact.Contains("bloodytears")) return "Bloody Tears";
        if (compact.Contains("gradius")) return "Gradius Remix";
        if (compact.Contains("smooch")) return "Smooch";
        if (compact.Contains("superstar")) return "Superstar";
        if (compact.Contains("vampirekiller")) return "Vampire Killer";
        if (compact.Contains("wagthedog")) return "Wag the Dog";

        return string.Empty;
    }

    private static int GarageStickerRank(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return 0;

        string compact = new string(raw.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

        if (compact.Contains("platinum") || compact.Contains("perfect")) return 4;
        if (compact.Contains("gold")) return 3;
        if (compact.Contains("silver")) return 2;
        if (compact.Contains("bronze")) return 1;

        return 0;
    }

    private static readonly string[] GarageStickerTiers =
    {
        "Bronze",
        "Silver",
        "Gold",
        "Platinum",
    };

    private static void QueueGarageStickerLocations(
        string reason,
        string song,
        string sticker,
        string stickerPro)
    {
        // F5 and other diagnostics may call the same scoring enquiry. AP checks
        // are only generated from the real result path.
        if (!string.Equals(reason, "result", StringComparison.OrdinalIgnoreCase))
            return;

        if (string.IsNullOrWhiteSpace(song))
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] GAME GARAGE AP CHECK SUPPRESSED: result had no captured cartridge identity.");
            return;
        }

        int rank = GarageStickerRank(sticker);
        string sourceSticker = sticker;
        if (rank <= 0)
        {
            rank = GarageStickerRank(stickerPro);
            sourceSticker = stickerPro;
        }

        if (rank <= 0)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] GAME GARAGE AP CHECK SUPPRESSED: song='{song}' sticker='{sticker}' stickerPro='{stickerPro}' was not a recognized Bronze/Silver/Gold/Platinum result.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] GAME GARAGE RESULT CHECKS song='{song}' earned='{GarageStickerTiers[rank - 1]}' rawSticker='{sourceSticker}' cumulativeCount={rank}.");

        for (int i = 0; i < rank; i++)
        {
            string tier = GarageStickerTiers[i];
            string locationName = LocationMap.GetGarageMedalLocationName(song, tier);
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] AP LOCATION '{locationName}' source='Game Garage {song} {tier} sticker threshold'.");
            Plugin.AP?.QueueLocation(locationName);
        }
    }

    private static string FriendlyCassetteSong(string variant)
    {
        if (string.Equals(variant, "LevelVariant_10_THE_LITTLE_THINGS", StringComparison.OrdinalIgnoreCase))
            return "The Little Things";
        if (string.Equals(variant, "LevelVariant_10_NO_PLAN_B", StringComparison.OrdinalIgnoreCase))
            return "No Plan B";
        if (string.Equals(variant, "LevelVariant_10_JOLT_CITY", StringComparison.OrdinalIgnoreCase))
            return "Jolt City";
        if (string.Equals(variant, "LevelVariant_10_QUIERES_BAILAR", StringComparison.OrdinalIgnoreCase))
            return "Quieres Bailar";
        if (string.Equals(variant, "LevelVariant_10_QUICKSAND", StringComparison.OrdinalIgnoreCase))
            return "Quicksand";
        if (string.Equals(variant, "LevelVariant_10_GOLD", StringComparison.OrdinalIgnoreCase))
            return "Gold";
        if (string.Equals(variant, "LevelVariant_10_I_GOT_MONEY", StringComparison.OrdinalIgnoreCase))
            return "I Got Money";
        if (string.Equals(variant, "LevelVariant_10_HIPPO_AND_FROG", StringComparison.OrdinalIgnoreCase))
            return "Hippo and Frog";
        if (string.Equals(variant, "LevelVariant_10_ON_THE_WAY", StringComparison.OrdinalIgnoreCase))
            return "On the Way";
        if (string.Equals(variant, "LevelVariant_10_BADASS", StringComparison.OrdinalIgnoreCase))
            return "Badass";
        if (string.Equals(variant, "LevelVariant_10_HEAVY_METAL", StringComparison.OrdinalIgnoreCase))
            return "Heavy Metal";
        if (string.Equals(variant, "LevelVariant_10_AOK", StringComparison.OrdinalIgnoreCase))
            return "AOK";
        if (string.Equals(variant, "LevelVariant_10_RAINBOW_MELODIES", StringComparison.OrdinalIgnoreCase))
            return "Rainbow Melodies";
        if (string.Equals(variant, "LevelVariant_10_SNEAKING", StringComparison.OrdinalIgnoreCase))
            return "Sneaking";
        if (string.Equals(variant, "LevelVariant_10_THE_HEIST", StringComparison.OrdinalIgnoreCase))
            return "The Heist";
        if (string.Equals(variant, "LevelVariant_10_MONEY_DUB", StringComparison.OrdinalIgnoreCase))
            return "Money";
        if (string.Equals(variant, "LevelVariant_10_LETS_GO", StringComparison.OrdinalIgnoreCase))
            return "Lets Go";
        if (string.Equals(variant, "LevelVariant_10_BOUNCE", StringComparison.OrdinalIgnoreCase))
            return "Bounce";
        if (string.Equals(variant, "LevelVariant_10_EPICAL", StringComparison.OrdinalIgnoreCase))
            return "Epical";
        if (string.Equals(variant, "LevelVariant_10_HOLLYWOOD_TRAILER", StringComparison.OrdinalIgnoreCase))
            return "Hollywood Trailer";
        if (string.Equals(variant, "LevelVariant_10_FALSE_DATA", StringComparison.OrdinalIgnoreCase))
            return "False Data";
        if (string.Equals(variant, "LevelVariant_10_GOTTA_GET_UP", StringComparison.OrdinalIgnoreCase))
            return "Gotta Get Up";
        if (string.Equals(variant, "LevelVariant_10_FUMBLIN_AROUND", StringComparison.OrdinalIgnoreCase))
            return "Fumblin Around";
        if (string.Equals(variant, "LevelVariant_10_PARTY_NON_STOP", StringComparison.OrdinalIgnoreCase))
            return "Party Non Stop";
        if (string.Equals(variant, "LevelVariant_10_KEEP_ON_HUSTLIN", StringComparison.OrdinalIgnoreCase))
            return "Keep On Hustlin";
        if (string.Equals(variant, "LevelVariant_10_ANOTHER_DAY_IN_PARADISE", StringComparison.OrdinalIgnoreCase))
            return "Another Day In Paradise";
        if (string.Equals(variant, "LevelVariant_10_FLAMENCO", StringComparison.OrdinalIgnoreCase))
            return "Flamenco";
        if (string.Equals(variant, "LevelVariant_10_TEN_FOUR_GOOD_BUDDY", StringComparison.OrdinalIgnoreCase))
            return "Ten-Four Good Buddy";
        if (string.Equals(variant, "LevelVariant_10_ZEN", StringComparison.OrdinalIgnoreCase))
            return "Zen";
        if (string.Equals(variant, "LevelVariant_10_WIGGLE", StringComparison.OrdinalIgnoreCase))
            return "Wiggle";

        return string.Empty;
    }

    private static readonly string[] CassetteMedalTiers =
    {
        "Bronze",
        "Silver",
        "Gold",
        "Platinum",
    };

    private static void QueueCassetteMedalLocations(
        string level,
        string variant,
        string medal,
        string medalPro)
    {
        string song = FriendlyCassetteSong(variant);
        if (string.IsNullOrWhiteSpace(song))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE AP CHECK SUPPRESSED: unverified cassette variant internal='{level}' variant='{variant}'.");
            return;
        }

        int rank = GarageStickerRank(medal);
        string sourceMedal = medal;
        if (rank <= 0)
        {
            rank = GarageStickerRank(medalPro);
            sourceMedal = medalPro;
        }

        if (rank <= 0)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE AP CHECK SUPPRESSED: song='{song}' medal='{medal}' medalPro='{medalPro}' was not a recognized Bronze/Silver/Gold/Platinum result.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB CASSETTE RESULT CHECKS song='{song}' internal='{level}' variant='{variant}' earned='{CassetteMedalTiers[rank - 1]}' rawMedal='{sourceMedal}' cumulativeCount={rank}.");

        for (int i = 0; i < rank; i++)
        {
            string tier = CassetteMedalTiers[i];
            string locationName = LocationMap.GetCassetteMedalLocationName(song, tier);
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] AP LOCATION '{locationName}' source='Music Lab cassette {song} {tier} medal threshold'.");
            Plugin.AP?.QueueLocation(locationName);
        }
    }

    private static Type? FindExactGameType(string fullName)
    {
        // AccessTools.TypeByName() scans every loaded assembly when the type is
        // not already cached. With IL2CPP wrappers that produces thousands of
        // harmless ReflectionTypeLoadException warnings. We know both enquiry
        // types live in Assembly-CSharp, so resolve that assembly by name and
        // ask it for the exact type without enumerating unrelated types.
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            string? assemblyName = null;
            try { assemblyName = assembly.GetName().Name; } catch { }
            if (!string.Equals(assemblyName, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase))
                continue;

            try { return assembly.GetType(fullName, throwOnError: false, ignoreCase: false); }
            catch { return null; }
        }

        return null;
    }

    private static void ResolveScoringProbeMethods()
    {
        lock (Sync)
        {
            if (_scoringProbeResolved)
                return;

            _scoringProbeResolved = true;
        }

        try
        {
            Type? rhythmType = FindExactGameType("RhythmGameEnquiries");
            if (rhythmType != null)
            {
                _getCurrentSongMethod = rhythmType.GetMethod(
                    "GetCurrentSong",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
            }

            Type? scoringType = FindExactGameType("LevelScoringEnquiries");
            if (scoringType != null)
            {
                _getMostRecentGarageStickerMethod = scoringType.GetMethod(
                    "GetMostRecentStickerEvaluationForCurrentlyScoredSong",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);

                _getMostRecentCleanMedalMethod = scoringType.GetMethod(
                    "GetMostRecentCleanMedalEvaluation",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);

                _getMostRecentStarRatingMethod = scoringType.GetMethod(
                    "GetMostRecentStarRatingEvaluation",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB SCORING PROBE READY: exactAssembly=True GetCurrentSong={_getCurrentSongMethod != null} GarageSticker={_getMostRecentGarageStickerMethod != null} CleanMedal={_getMostRecentCleanMedalMethod != null} StarRating={_getMostRecentStarRatingMethod != null}. Read-only calls only.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB SCORING PROBE resolution failed: {ex.GetBaseException().Message}");
        }
    }

    private static string ReadDiagnosticMember(object? obj, params string[] names)
    {
        if (obj == null)
            return "<null>";

        foreach (string name in names)
        {
            object? value = null;
            try { value = ReflectionUtil.UnwrapNullable(ReflectionUtil.ReadMember(obj, name)); }
            catch { }

            if (value == null)
                continue;

            string text = ReflectionUtil.ExtractIdentifier(value) ?? value.ToString() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(text))
                return text;
        }

        return "<unavailable>";
    }

    private static void ProbeGarageScoringState(string reason)
    {
        ResolveScoringProbeMethods();

        string rawSong = string.Empty;
        string friendly;
        lock (Sync)
            friendly = _currentGarageSong;

        // Secondary fallback only. The cartridge-holder insertion capture is
        // preferred. At result time the rhythm system commonly clears CurrentSong
        // to INVALID, so never overwrite a cartridge-captured identity.
        if (_getCurrentSongMethod != null && string.IsNullOrWhiteSpace(friendly))
        {
            try
            {
                object? value = ReflectionUtil.UnwrapNullable(_getCurrentSongMethod.Invoke(null, null));
                rawSong = ReflectionUtil.ExtractIdentifier(value) ?? value?.ToString() ?? string.Empty;
                string fallbackFriendly = FriendlyGarageSong(rawSong);

                if (!string.IsNullOrWhiteSpace(fallbackFriendly))
                {
                    friendly = fallbackFriendly;
                    lock (Sync)
                        _currentGarageSong = fallbackFriendly;
                }

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE CURRENT SONG fallback reason='{reason}' raw='{(string.IsNullOrWhiteSpace(rawSong) ? "<none>" : rawSong)}' mapped='{(string.IsNullOrWhiteSpace(fallbackFriendly) ? "<unknown>" : fallbackFriendly)}'.");
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE GetCurrentSong fallback failed reason='{reason}': {ex.GetBaseException().Message}");
            }
        }

        if (_getMostRecentGarageStickerMethod != null)
        {
            try
            {
                object? evaluation = _getMostRecentGarageStickerMethod.Invoke(null, null);
                string evaluationRawSong = ReadDiagnosticMember(
                    evaluation,
                    "PlayableSong",
                    "_PlayableSong_k__BackingField",
                    "Song",
                    "ScoredSong");
                string evaluationFriendly = FriendlyGarageSong(evaluationRawSong);

                // Some builds expose a song member on SongStickerEvaluation and
                // some do not. Treat it only as a fallback; the cartridge-state detector captures the
                // Garage identity from the inserted cartridge holder state.
                if (!string.IsNullOrWhiteSpace(evaluationFriendly))
                {
                    rawSong = evaluationRawSong;
                    friendly = evaluationFriendly;
                    lock (Sync)
                        _currentGarageSong = evaluationFriendly;
                }

                string sticker = ReadDiagnosticMember(evaluation, "StickerQualityEarned");
                string stickerPro = ReadDiagnosticMember(evaluation, "StickerQualityEarnedByPro");
                string scoreUsed = ReadDiagnosticMember(evaluation, "ScoreUsed");

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE STICKER EVALUATION reason='{reason}' song='{(string.IsNullOrWhiteSpace(friendly) ? "<unknown>" : friendly)}' rawSong='{evaluationRawSong}' sticker='{sticker}' stickerPro='{stickerPro}' scoreUsed='{scoreUsed}'.");

                QueueGarageStickerLocations(reason, friendly, sticker, stickerPro);
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE sticker-evaluation probe failed reason='{reason}': {ex.GetBaseException().Message}");
            }
        }
    }

    private static void ProbeCleanMedalState(string level, string variant, int? score)
    {
        ResolveScoringProbeMethods();
        if (_getMostRecentCleanMedalMethod == null)
            return;

        try
        {
            object? evaluation = _getMostRecentCleanMedalMethod.Invoke(null, null);
            string medal = ReadDiagnosticMember(evaluation, "MedalEarned");
            string medalPro = ReadDiagnosticMember(evaluation, "MedalEarnedByPro");
            string scoreUsed = ReadDiagnosticMember(evaluation, "ScoreUsed");

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE MEDAL internal={level} variant={variant} resultScore={score?.ToString() ?? "?"} medal='{medal}' medalPro='{medalPro}' scoreUsed='{scoreUsed}'.");

            QueueCassetteMedalLocations(level, variant, medal, medalPro);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB cassette medal probe failed internal={level} variant={variant}: {ex.GetBaseException().Message}");
        }
    }

    public static int? ProbeNormalLevelStarRating(string level, string variant, int? score)
    {
        if (string.Equals(level, "Level_27", StringComparison.OrdinalIgnoreCase))
            return null;

        if (!string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase))
            return null;

        ResolveScoringProbeMethods();
        if (_getMostRecentStarRatingMethod == null)
            return null;

        try
        {
            object? evaluation = _getMostRecentStarRatingMethod.Invoke(null, null);
            if (evaluation == null)
                return null;
            string stars = ReadDiagnosticMember(evaluation, "StarsEarned");
            string scoreUsed = ReadDiagnosticMember(evaluation, "ScoreUsed");
            int? starsEarned = ReflectionUtil.ReadInt(evaluation, "StarsEarned");
            if (starsEarned == null)
            {
                string digits = new(stars.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out int parsed))
                    starsEarned = parsed;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] PERFORMANCE STAR EVALUATION internal={level} variant={variant} resultScore={score?.ToString() ?? "?"} stars='{stars}' scoreUsed='{scoreUsed}'.");
            return starsEarned.HasValue ? Math.Clamp(starsEarned.Value, 0, 3) : null;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] performance star probe failed internal={level}: {ex.GetBaseException().Message}");
            return null;
        }
    }

    public static void RecordResultRequest(
        object request,
        string level,
        string variant,
        int? score,
        string difficulty)
    {
        if (!IsMusicLabResult(level, variant))
            return;


        if (string.Equals(level, "Level_27", StringComparison.OrdinalIgnoreCase))
            ProbeGarageScoringState("result");
        else
            ProbeCleanMedalState(level, variant, score);

        string identity = DescribeSongIdentity(level, variant);
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB RESULT identity='{identity}' internal={level} variant={variant} score={score?.ToString() ?? "?"} difficulty={difficulty}.");

        DumpRelevantMembers(
            request,
            "RESULT REQUEST",
            new[] { "medal", "rank", "grade", "point", "score", "star", "track", "song", "level", "variant" });
    }

    public static void RecordPersistedEvent(
        object evt,
        string level,
        string variant,
        int? score)
    {
        if (!IsMusicLabResult(level, variant))
            return;


        Record(
            "RESULT PERSISTED",
            $"identity='{DescribeSongIdentity(level, variant)}' internal={level} variant={variant} score={score?.ToString() ?? "?"}");

        DumpRelevantMembers(
            evt,
            "PERSISTED EVENT",
            new[] { "medal", "rank", "grade", "point", "score", "star", "track", "song", "level", "variant" });
    }

    private static bool IsMusicLabResult(string level, string variant)
    {
        if (string.Equals(level, "Level_27", StringComparison.OrdinalIgnoreCase))
            return true;

        bool musicLabStage;
        lock (Sync)
            musicLabStage = _musicLabStageInProgress;

        return musicLabStage &&
               !string.IsNullOrWhiteSpace(variant) &&
               !string.Equals(variant, "LevelVariant_Default", StringComparison.OrdinalIgnoreCase) &&
               variant.StartsWith("LevelVariant_", StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeSongIdentity(string level, string variant)
    {
        if (string.Equals(level, "Level_27", StringComparison.OrdinalIgnoreCase))
        {
            lock (Sync)
            {
                return string.IsNullOrWhiteSpace(_currentGarageSong)
                    ? "Game Garage - <selected cartridge unknown>"
                    : $"Game Garage - {_currentGarageSong}";
            }
        }

        string friendlyCassette = FriendlyCassetteSong(variant);
        if (!string.IsNullOrWhiteSpace(friendlyCassette))
            return $"Music Lab Cassette - {friendlyCassette}";

        string cleaned = variant;
        int marker = cleaned.IndexOf("_", "LevelVariant_".Length, StringComparison.Ordinal);
        if (marker >= 0 && marker + 1 < cleaned.Length)
            cleaned = cleaned[(marker + 1)..];

        return $"Music Lab Cassette - {cleaned}";
    }

    public static void ScanCurrentScene()
    {
        if (!DeveloperHarness.Enabled)
            return;

        string room = DeveloperHarness.CurrentRoomId;
        if (!IsMusicLabRoom(room))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB F5 ignored outside Hub6/GameRoom_27; currentRoom='{room}'.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== MUSIC LAB F5 FOCUSED SCAN BEGIN ===== currentRoom='{room}'.");

        if (string.Equals(room, Hub6RoomId, StringComparison.Ordinal))
            ScanHub6();
        else
            ScanGarage();

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== MUSIC LAB F5 FOCUSED SCAN END =====");
    }

    private static void ScanHub6()
    {
        GameObject? phoneBank = GameObject.Find(Hub6PhoneBankRootPath);
        CassettePostLoadSnapshotGateDecision snapshotDecision =
            PostLoadSnapshotLiveGate.Observe(phoneBank != null);
        if (snapshotDecision.LogDeferred)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE POST-LOAD SNAPSHOT DEFERRED room='{DeveloperHarness.CurrentRoomId}' " +
                $"reason='Hub6 scene is not live' missingMarker='{Hub6PhoneBankRootPath}'.");
        }
        if (snapshotDecision.ReadSnapshot)
        {
            CassetteReceiptRandomization.LogPostLoadSnapshot(
                DeveloperHarness.CurrentRoomId,
                "plain F5 after Hub6 became live");
        }
        bool reconciled = ReconcileCollectedRewardChests();
        int mapped;
        lock (Sync)
            mapped = MusicLabRewardFlagByThreshold.Count;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB REWARD CHEST STATUS thresholds=5/10/20/32/46/64/89/111/140 mappedNativeFlags={mapped}/9 reconcileComplete={reconciled}. F5 is read-only except for queuing AP checks for rewards the native save already says are collected.");

        ScanNativeIdentityCandidates();
    }

    public static void ScanNativeIdentityCandidates()
    {
        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] ===== V06763 NATIVE IDENTITY DIAGNOSTIC BEGIN ===== readOnly=True.");

        int hubCandidates = 0;
        foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (tr == null)
                continue;

            string path = BuildHierarchy(tr);
            GameObject go;
            try { go = tr.gameObject; }
            catch { continue; }
            if (go == null)
                continue;

            string[] componentTypes;
            try
            {
                componentTypes = go.GetComponents<Component>()
                    .Where(component => component != null)
                    .Select(component => component.GetType().FullName ?? component.GetType().Name)
                    .ToArray();
            }
            catch
            {
                componentTypes = Array.Empty<string>();
            }

            if (!NativeIdentityDiagnosticPolicy.IsHub6Candidate(path, componentTypes))
                continue;

            bool activeSelf = false;
            bool activeInHierarchy = false;
            try
            {
                activeSelf = go.activeSelf;
                activeInHierarchy = go.activeInHierarchy;
            }
            catch { }

            hubCandidates++;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] V06763 HUB6 CANDIDATE path='{path}' activeSelf={activeSelf} activeInHierarchy={activeInHierarchy} managedComponents='{string.Join("|", componentTypes)}'.");
            DumpExactObjectComponents(path, "V06763 HUB6 CANDIDATE");
        }

        int difficultyTypes = 0;
        Type[] gameTypes;
        try { gameTypes = ReflectionUtil.GameAssembly?.GetTypes() ?? Array.Empty<Type>(); }
        catch { gameTypes = Array.Empty<Type>(); }

        foreach (Type type in gameTypes
                     .Where(type => NativeIdentityDiagnosticPolicy.IsDifficultyType(type.FullName ?? type.Name))
                     .OrderBy(type => type.FullName, StringComparer.Ordinal))
        {
            string[] members;
            try
            {
                members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .Select(member => $"{member.MemberType}:{member.Name}")
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .ToArray();
            }
            catch
            {
                members = Array.Empty<string>();
            }

            if (members.Length == 0)
                continue;

            difficultyTypes++;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] V06763 DIFFICULTY TYPE fullName='{type.FullName}' members='{string.Join("|", members)}'.");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== V06763 NATIVE IDENTITY DIAGNOSTIC END ===== hubCandidates={hubCandidates} difficultyTypes={difficultyTypes} readOnly=True.");
    }

    private static void ScanGarage()
    {
        DumpGarageInteractionSnapshot();
        ProbeGarageScoringState("F5");

        foreach (string sequenceName in GarageSequenceNames)
        {
            string sequenceRoot =
                $"Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/{sequenceName}";
            string scoredSong = sequenceRoot + "/SetScoredSong";

            DumpExactObjectComponents(scoredSong, $"GARAGE {sequenceName} SetScoredSong");
        }

        DumpExactSubtreeComponents(
            "Root/GameRoom_27_Logic/Objects/Cartridges",
            "GARAGE CARTRIDGES",
            100,
            false);

        DumpExactSubtreeComponents(
            "Root/GameRoom_27_Logic/Objects/Medals",
            "GARAGE MEDALS",
            100,
            true);

        string current;
        lock (Sync)
            current = _currentGarageSong;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB GARAGE selectedSong='{(string.IsNullOrWhiteSpace(current) ? "<unknown/not started>" : current)}'.");
    }

    // v0.67.23 diagnostic helper retained: managed GetComponents<Component>() often reports game scripts as
    // UnityEngine.Component. The wrapped Pointer still addresses the real IL2CPP
    // object, so query its native class/field metadata directly. This is read-only.
    private static void DumpNativeIl2CppComponents(string path)
    {
        GameObject? go = GameObject.Find(path);
        if (go == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB 64 NATIVE OBJECT missing path='{path}'.");
            return;
        }

        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB 64 NATIVE OBJECT components failed path='{path}': {ex.GetBaseException().Message}");
            return;
        }

        int index = 0;
        foreach (Component component in components)
        {
            if (component == null)
                continue;

            index++;
            IntPtr obj = GetNativePointer(component);
            if (obj == IntPtr.Zero)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB 64 NATIVE COMPONENT path='{path}' index={index} managed='{component.GetType().FullName}' pointer=<zero>.");
                continue;
            }

            IntPtr klass = IntPtr.Zero;
            try { klass = ml_il2cpp_object_get_class(obj); }
            catch { }

            string className = NativeClassFullName(klass);
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB 64 NATIVE COMPONENT path='{path}' index={index} managed='{component.GetType().FullName}' class='{className}'.");

            DumpNativeFields(obj, klass, path, className);
        }
    }

    private static void DumpNativeFields(IntPtr obj, IntPtr klass, string path, string rootClassName)
    {
        if (obj == IntPtr.Zero || klass == IntPtr.Zero)
            return;

        int emitted = 0;
        IntPtr current = klass;
        for (int depth = 0; current != IntPtr.Zero && depth < 7 && emitted < 48; depth++)
        {
            IntPtr iter = IntPtr.Zero;
            while (emitted < 48)
            {
                IntPtr field;
                try { field = ml_il2cpp_class_get_fields(current, ref iter); }
                catch { field = IntPtr.Zero; }

                if (field == IntPtr.Zero)
                    break;

                string fieldName = NativeAnsi(() => ml_il2cpp_field_get_name(field));
                if (string.IsNullOrWhiteSpace(fieldName))
                    fieldName = "<unnamed>";

                IntPtr fieldType = IntPtr.Zero;
                try { fieldType = ml_il2cpp_field_get_type(field); } catch { }
                string fieldTypeName = NativeTypeName(fieldType);

                if (fieldName is "m_CachedPtr" or "m_InstanceID")
                    continue;

                string value = NativeFieldValue(field, obj, fieldTypeName);
                string declaring = NativeClassFullName(current);
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB 64 NATIVE FIELD path='{path}' rootClass='{rootClassName}' declaring='{declaring}' name='{fieldName}' type='{fieldTypeName}' value='{value}'.");
                emitted++;
            }

            try { current = ml_il2cpp_class_get_parent(current); }
            catch { current = IntPtr.Zero; }
        }
    }

    private static IntPtr GetNativePointer(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(value) is IntPtr pointer ? pointer : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static string NativeClassFullName(IntPtr klass)
    {
        if (klass == IntPtr.Zero)
            return "<no-class>";

        string ns = NativeAnsi(() => ml_il2cpp_class_get_namespace(klass));
        string name = NativeAnsi(() => ml_il2cpp_class_get_name(klass));
        if (string.IsNullOrWhiteSpace(name))
            name = "<unnamed-class>";
        return string.IsNullOrWhiteSpace(ns) ? name : ns + "." + name;
    }

    private static string NativeTypeName(IntPtr type)
    {
        if (type == IntPtr.Zero)
            return "<unknown-type>";

        try
        {
            IntPtr klass = ml_il2cpp_class_from_type(type);
            if (klass != IntPtr.Zero)
                return NativeClassFullName(klass);
        }
        catch { }

        return "<unknown-type>";
    }

    private static string NativeAnsi(Func<IntPtr> getter)
    {
        try
        {
            IntPtr ptr = getter();
            return ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NativeFieldValue(IntPtr field, IntPtr obj, string typeName)
    {
        IntPtr boxed = IntPtr.Zero;
        try { boxed = ml_il2cpp_field_get_value_object(field, obj); }
        catch { return "<read-failed>"; }

        if (boxed == IntPtr.Zero)
            return "<null>";

        string simple = typeName.ToLowerInvariant();
        try
        {
            IntPtr unboxed = ml_il2cpp_object_unbox(boxed);
            if (unboxed != IntPtr.Zero)
            {
                if (simple == "system.boolean")
                    return Marshal.ReadByte(unboxed) != 0 ? "True" : "False";

                if (simple == "system.byte")
                    return Marshal.ReadByte(unboxed).ToString();

                if (simple.Contains("int32") || simple.Contains("uint32") ||
                    simple.Contains("int16") || simple.Contains("uint16") ||
                    simple.Contains("progressionflag"))
                    return Marshal.ReadInt32(unboxed).ToString();
            }
        }
        catch { }

        try
        {
            IntPtr valueClass = ml_il2cpp_object_get_class(boxed);
            string valueClassName = NativeClassFullName(valueClass);
            if (valueClassName.Contains("DefinedInt", StringComparison.OrdinalIgnoreCase) ||
                valueClassName.Contains("IntReference", StringComparison.OrdinalIgnoreCase) ||
                valueClassName.Contains("IntVariable", StringComparison.OrdinalIgnoreCase))
            {
                IntPtr method = FindNativeMethod(valueClass, "GetValue", 0);
                if (method != IntPtr.Zero)
                {
                    IntPtr exc = IntPtr.Zero;
                    IntPtr result = ml_il2cpp_runtime_invoke(method, boxed, IntPtr.Zero, ref exc);
                    if (exc == IntPtr.Zero && result != IntPtr.Zero)
                    {
                        IntPtr raw = ml_il2cpp_object_unbox(result);
                        if (raw != IntPtr.Zero)
                            return $"{valueClassName}.GetValue()={Marshal.ReadInt32(raw)}";
                    }
                }
            }

            return $"<{valueClassName}>";
        }
        catch
        {
            return "<object>";
        }
    }

    private static IntPtr FindNativeMethod(IntPtr klass, string name, int argc)
    {
        IntPtr current = klass;
        for (int depth = 0; current != IntPtr.Zero && depth < 8; depth++)
        {
            try
            {
                IntPtr method = ml_il2cpp_class_get_method_from_name(current, name, argc);
                if (method != IntPtr.Zero)
                    return method;
                current = ml_il2cpp_class_get_parent(current);
            }
            catch
            {
                return IntPtr.Zero;
            }
        }
        return IntPtr.Zero;
    }

    private const string MusicLabNativeLibrary = "GameAssembly.dll";

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_object_get_class")]
    private static extern IntPtr ml_il2cpp_object_get_class(IntPtr obj);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_parent")]
    private static extern IntPtr ml_il2cpp_class_get_parent(IntPtr klass);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_name")]
    private static extern IntPtr ml_il2cpp_class_get_name(IntPtr klass);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_namespace")]
    private static extern IntPtr ml_il2cpp_class_get_namespace(IntPtr klass);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_fields")]
    private static extern IntPtr ml_il2cpp_class_get_fields(IntPtr klass, ref IntPtr iter);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_name")]
    private static extern IntPtr ml_il2cpp_field_get_name(IntPtr field);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_type")]
    private static extern IntPtr ml_il2cpp_field_get_type(IntPtr field);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_from_type")]
    private static extern IntPtr ml_il2cpp_class_from_type(IntPtr type);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_field_get_value_object")]
    private static extern IntPtr ml_il2cpp_field_get_value_object(IntPtr field, IntPtr obj);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_object_unbox")]
    private static extern IntPtr ml_il2cpp_object_unbox(IntPtr obj);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_class_get_method_from_name")]
    private static extern IntPtr ml_il2cpp_class_get_method_from_name(IntPtr klass, [MarshalAs(UnmanagedType.LPStr)] string name, int argsCount);

    [DllImport(MusicLabNativeLibrary, CallingConvention = CallingConvention.Cdecl, EntryPoint = "il2cpp_runtime_invoke")]
    private static extern IntPtr ml_il2cpp_runtime_invoke(IntPtr method, IntPtr obj, IntPtr parameters, ref IntPtr exc);

    private static void DumpGarageInteractionSnapshot()
    {
        int snapshot = ++_garageInteractionSnapshot;
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== MUSIC LAB GARAGE INTERACTION SNAPSHOT #{snapshot:00} BEGIN =====");

        const string cartridgeRootPath = "Root/GameRoom_27_Logic/Objects/Cartridges";
        GameObject? cartridgeRoot = GameObject.Find(cartridgeRootPath);
        if (cartridgeRoot == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} cartridge root not found path='{cartridgeRootPath}'.");
        }
        else
        {
            try
            {
                int count = 0;
                foreach (Transform tr in cartridgeRoot.transform.GetComponentsInChildren<Transform>(true))
                {
                    if (tr == null || tr.gameObject == null)
                        continue;

                    string path = BuildHierarchy(tr);
                    string parent = tr.parent != null ? BuildHierarchy(tr.parent) : "<none>";
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} CARTRIDGE path='{path}' parent='{parent}' activeSelf={SafeActiveSelf(tr)} activeInHierarchy={SafeActiveInHierarchy(tr)}.");

                    // Only the exact cartridge subtree is inspected. Relevant component
                    // state is useful for detecting held/inserted/selected transitions.
                    DumpRelevantGarageInteractionComponents(tr.gameObject, path, snapshot);

                    if (++count >= 120)
                        break;
                }
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} cartridge traversal failed: {ex.GetBaseException().Message}");
            }
        }

        foreach (string sequenceName in GarageSequenceNames)
        {
            string rootPath = $"Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/{sequenceName}";
            GameObject? sequenceRoot = GameObject.Find(rootPath);
            if (sequenceRoot == null)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} SEQUENCE name='{sequenceName}' found=False.");
                continue;
            }

            Transform tr = sequenceRoot.transform;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} SEQUENCE name='{sequenceName}' found=True activeSelf={SafeActiveSelf(tr)} activeInHierarchy={SafeActiveInHierarchy(tr)} childCount={tr.childCount}.");

            DumpRelevantGarageInteractionComponents(sequenceRoot, rootPath, snapshot);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== MUSIC LAB GARAGE INTERACTION SNAPSHOT #{snapshot:00} END =====");
    }

    private static void DumpRelevantGarageInteractionComponents(GameObject go, string path, int snapshot)
    {
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch { return; }

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type type = component.GetType();
            string fullTypeName = type.FullName ?? type.Name ?? string.Empty;
            if (fullTypeName.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase))
                continue;

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] GARAGE SNAPSHOT #{snapshot:00} COMPONENT path='{path}' type='{fullTypeName}'.");

            DumpRelevantMembers(
                component,
                $"GARAGE SNAPSHOT #{snapshot:00} {type.Name}",
                new[]
                {
                    "cartridge", "song", "track", "selected", "insert", "slot",
                    "held", "hold", "carry", "pickup", "interact", "state", "active",
                    "playing", "play", "item", "identifier"
                });
        }
    }

    private static void DumpExactSubtreeHierarchy(
        string rootPath,
        string label,
        int transformCap)
    {
        GameObject? root = GameObject.Find(rootPath);
        if (root == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label}: exact root not found '{rootPath}'.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- MUSIC LAB {label} BEGIN root='{rootPath}' -----");

        int visited = 0;

        try
        {
            foreach (Transform tr in root.transform.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                visited++;
                if (visited > transformCap)
                    break;

                string path = BuildHierarchy(tr);
                string parent = tr.parent != null ? BuildHierarchy(tr.parent) : "<none>";
                int sibling = -1;
                int childCount = -1;
                try { sibling = tr.GetSiblingIndex(); } catch { }
                try { childCount = tr.childCount; } catch { }

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB {label} NODE #{visited:000} path='{path}' parent='{parent}' sibling={sibling} childCount={childCount} activeSelf={SafeActiveSelf(tr)} activeInHierarchy={SafeActiveInHierarchy(tr)}.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label} hierarchy scan failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- MUSIC LAB {label} END visited={visited} -----");
    }

    private static void DumpExactSubtreeComponents(
        string rootPath,
        string label,
        int transformCap,
        bool onlyInterestingNames)
    {
        GameObject? root = GameObject.Find(rootPath);
        if (root == null)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB {label}: exact root not found '{rootPath}'.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- MUSIC LAB {label} BEGIN root='{rootPath}' -----");

        int visited = 0;
        int dumped = 0;

        try
        {
            foreach (Transform tr in root.transform.GetComponentsInChildren<Transform>(true))
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                visited++;
                if (visited > transformCap)
                    break;

                string name = tr.name ?? string.Empty;
                string lower = name.ToLowerInvariant();

                if (onlyInterestingNames &&
                    !lower.Contains("condition") &&
                    !lower.Contains("interaction") &&
                    !lower.Contains("point") &&
                    !lower.Contains("medal") &&
                    !lower.Contains("score") &&
                    !lower.Contains("chest") &&
                    !lower.Contains("reward") &&
                    !lower.Contains("memory") &&
                    !lower.Contains("cartridge") &&
                    !lower.Contains("cassette"))
                {
                    continue;
                }

                string path = BuildHierarchy(tr);
                DumpComponents(tr.gameObject, path);
                dumped++;

                if (dumped >= 80)
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label} subtree scan failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ----- MUSIC LAB {label} END visited={visited} componentObjects={dumped} -----");
    }

    private static void DumpExactObjectComponents(string path, string label)
    {
        GameObject? go = GameObject.Find(path);
        if (go == null)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB {label}: object not found '{path}'.");
            return;
        }

        // v0.67.13 only reflected components whose managed assembly name was
        // exactly Assembly-CSharp. IL2CPP interop wrappers do not reliably report
        // that assembly name, which hid the useful SetScoredSong component.
        // This is intentionally limited to the six exact SetScoredSong objects.
        try
        {
            var components = go.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                Type type = component.GetType();
                string assemblyName;
                try { assemblyName = type.Assembly.GetName().Name ?? string.Empty; }
                catch { assemblyName = string.Empty; }

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB EXACT COMPONENT path='{path}' type='{type.FullName}' assembly='{assemblyName}'.");

                MethodInfo[] methods;
                try
                {
                    methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                }
                catch
                {
                    methods = Array.Empty<MethodInfo>();
                }

                foreach (MethodInfo method in methods.Take(32))
                {
                    string signature;
                    try { signature = string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name)); }
                    catch { signature = "?"; }

                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] MUSIC LAB EXACT METHOD path='{path}' type='{type.FullName}' method='{method.Name}({signature})' returns='{method.ReturnType.Name}'.");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label}: exact component dump failed: {ex.GetBaseException().Message}");
        }

        DumpComponents(go, path);
    }

    private static void DumpComponents(GameObject go, string path)
    {
        Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
        try { components = go.GetComponents<Component>(); }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB COMPONENTS path='{path}' failed: {ex.GetBaseException().Message}");
            return;
        }

        foreach (Component component in components)
        {
            if (component == null)
                continue;

            Type type = component.GetType();
            string assemblyName;
            try { assemblyName = type.Assembly.GetName().Name ?? string.Empty; }
            catch { assemblyName = string.Empty; }

            string fullTypeName = type.FullName ?? type.Name ?? string.Empty;

            // Stay narrow to the already-selected Hub6 chest / Garage objects,
            // but do not assume IL2CPP wrapper types report Assembly-CSharp as
            // their managed assembly. v0.67.13 filtered the useful components out.
            if (fullTypeName.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase))
                continue;

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB COMPONENT path='{path}' type='{type.FullName}' assembly='{assemblyName}'.");

            DumpRelevantMembers(
                component,
                $"COMPONENT {type.Name}",
                new[]
                {
                    "point", "medal", "score", "threshold", "require", "count", "total",
                    "value", "condition", "cassette", "cartridge", "memory", "item", "song", "track"
                });

            DumpRelevantMethods(component, path);
        }
    }

    private static void DumpRelevantMembers(
        object obj,
        string label,
        IEnumerable<string> keywords)
    {
        string[] loweredKeywords = keywords.Select(k => k.ToLowerInvariant()).ToArray();
        Type type = obj.GetType();
        int emitted = 0;

        FieldInfo[] fields;
        try
        {
            fields = type.GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch
        {
            fields = Array.Empty<FieldInfo>();
        }

        foreach (FieldInfo field in fields)
        {
            string lowerName = field.Name.ToLowerInvariant();
            if (!loweredKeywords.Any(lowerName.Contains))
                continue;

            object? value = null;
            try { value = field.GetValue(obj); }
            catch { }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label} FIELD name='{field.Name}' type='{field.FieldType.FullName}' value='{SafeDiagnosticValue(value)}'.");

            emitted++;
            if (emitted >= 32)
                return;
        }

        PropertyInfo[] properties;
        try
        {
            properties = type.GetProperties(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
        catch
        {
            properties = Array.Empty<PropertyInfo>();
        }

        foreach (PropertyInfo property in properties)
        {
            if (property.GetIndexParameters().Length != 0)
                continue;

            string lowerName = property.Name.ToLowerInvariant();
            if (!loweredKeywords.Any(lowerName.Contains))
                continue;

            Type pt = property.PropertyType;
            bool simpleType =
                pt == typeof(string) ||
                pt == typeof(bool) ||
                pt == typeof(int) ||
                pt == typeof(float) ||
                pt == typeof(double) ||
                pt.IsEnum ||
                pt.Name.Contains("Identifier", StringComparison.OrdinalIgnoreCase) ||
                pt.Name.Contains("DefinedInt", StringComparison.OrdinalIgnoreCase);

            if (!simpleType)
                continue;

            object? value = null;
            try { value = property.GetValue(obj); }
            catch { }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB {label} PROPERTY name='{property.Name}' type='{pt.FullName}' value='{SafeDiagnosticValue(value)}'.");

            emitted++;
            if (emitted >= 32)
                return;
        }
    }

    private static void DumpRelevantMethods(Component component, string path)
    {
        Type type = component.GetType();
        MethodInfo[] methods;
        try
        {
            methods = type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        }
        catch
        {
            return;
        }

        int emitted = 0;
        foreach (MethodInfo method in methods)
        {
            string lower = method.Name.ToLowerInvariant();
            if (!lower.Contains("begin") &&
                !lower.Contains("trigger") &&
                !lower.Contains("interact") &&
                !lower.Contains("condition") &&
                !lower.Contains("evaluate") &&
                !lower.Contains("require") &&
                !lower.Contains("point") &&
                !lower.Contains("medal") &&
                !lower.Contains("score") &&
                !lower.Contains("value") &&
                !lower.StartsWith("is") &&
                !lower.StartsWith("can") &&
                !lower.StartsWith("get"))
            {
                continue;
            }

            string signature;
            try
            {
                signature = string.Join(",", method.GetParameters().Select(p => p.ParameterType.Name));
            }
            catch
            {
                signature = "?";
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] MUSIC LAB METHOD path='{path}' type='{type.Name}' method='{method.Name}({signature})' returns='{method.ReturnType.Name}'.");

            emitted++;
            if (emitted >= 18)
                break;
        }
    }

    private static string SafeDiagnosticValue(object? value)
    {
        if (value == null)
            return "<null>";

        try
        {
            Type type = value.GetType();
            if (value is string ||
                value is bool ||
                value is byte ||
                value is sbyte ||
                value is short ||
                value is ushort ||
                value is int ||
                value is uint ||
                value is long ||
                value is ulong ||
                value is float ||
                value is double ||
                value is decimal ||
                type.IsEnum)
            {
                return value.ToString() ?? "<null>";
            }

            if (type.Name.Contains("DefinedInt", StringComparison.OrdinalIgnoreCase))
            {
                MethodInfo? getValue = type.GetMethod(
                    "GetValue",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    Type.EmptyTypes,
                    null);

                if (getValue != null)
                {
                    try
                    {
                        object? result = getValue.Invoke(value, Array.Empty<object>());
                        return $"{type.Name}.GetValue()={result?.ToString() ?? "<null>"}";
                    }
                    catch
                    {
                        return $"<{type.Name}: GetValue failed>";
                    }
                }
            }

            string? identifier = ReflectionUtil.ExtractIdentifier(value);
            if (!string.IsNullOrWhiteSpace(identifier))
                return identifier;

            return $"<{type.FullName}>";
        }
        catch
        {
            return "<unavailable>";
        }
    }

    private static bool SafeActiveSelf(Transform tr)
    {
        try { return tr.gameObject.activeSelf; }
        catch { return false; }
    }

    private static bool SafeActiveInHierarchy(Transform tr)
    {
        try { return tr.gameObject.activeInHierarchy; }
        catch { return false; }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            while (current != null)
            {
                names.Add(current.name ?? "<unnamed>");
                current = current.parent;
            }

            names.Reverse();
            return string.Join("/", names);
        }
        catch
        {
            return "<unavailable>";
        }
    }

    private static void Record(string kind, string detail)
    {
        int sequence;
        lock (Sync)
        {
            if (!_active)
                return;

            sequence = ++_sequence;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB TRACE #{sequence:000} [{kind}] {detail}");
    }
}


internal static class NativeProgression
{
    private static object? _playerSaveRequestProcessor;
    private static readonly object ProcessorLock = new();

    public static void CapturePlayerSaveRequestProcessor(object? instance)
    {
        if (instance == null) return;

        Type t = instance.GetType();
        if (t.Name != "PlayerSaveRequestProcessor") return;

        bool changed;
        lock (ProcessorLock)
        {
            changed = !ReferenceEquals(_playerSaveRequestProcessor, instance);
            _playerSaveRequestProcessor = instance;
        }

        if (changed)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] Captured live PlayerSaveRequestProcessor instance ({t.FullName}).");
        }
    }

    public static void ApplyArchipelagoItem(string itemName)
    {
        if (AreaAccessPrototype.TryApplyItem(itemName))
            return;

        if (CassetteReceiptRandomization.TryApplyItem(itemName))
            return;

    }
}

internal static class ProgressionPatches
{
    public static bool ProgressionRequestPrefix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);
        GarageCartridgeAccess.CapturePlayerSaveRequestProcessor(__instance);
        RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.TryFlushPendingNativeGrant();
        PlantPipesRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PlantPipesRandomization.TryFlushPendingNativeGrant();
        PreviewAbilityRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.OnLifecyclePoint("progression request prefix");
        RootsBucketRandomization.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "RecordGameProgressionInSaveDataRequest");
        if (req == null) return true;

        string flag = ReflectionUtil.ReadMember(req, "Flag")?.ToString()
                      ?? ReflectionUtil.ReadMember(req, "_Flag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        GarageCartridgeAccess.RecordNativeBagProgressionRequest(req, flag);

        ExpandedChecks.BeforeProgressionRequest(req, flag);
        if (!QuestChecks.BeforeProgressionRequest(req, flag)) return false;
        if (QuestChecks.SuppressPlunger(req, flag)) return false;
        if (CharacterQuestItems.ShouldSuppress(req, flag))
            return false;

        if (GarageCartridgeAccess.ShouldSuppressVanillaSourceGrant(req, flag))
            return false;

        if (WeedKillerRandomization.ShouldSuppressGeckoVanillaGrant(req, flag))
            return false;

        if (PlantPipesRandomization.ShouldSuppressFrogHippoVanillaGrant(req, flag))
            return false;

        if (RootsBucketRandomization.ShouldSuppressVanillaGrant(req, flag))
            return false;

        return true;
    }

    public static void ProgressionRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);
        GarageCartridgeAccess.CapturePlayerSaveRequestProcessor(__instance);
        RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.TryFlushPendingNativeGrant();
        PlantPipesRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PlantPipesRandomization.TryFlushPendingNativeGrant();
        CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.OnLifecyclePoint("progression request postfix");
        RootsBucketRandomization.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "RecordGameProgressionInSaveDataRequest");
        if (req == null) return;

        string flag = ReflectionUtil.ReadMember(req, "Flag")?.ToString()
                      ?? ReflectionUtil.ReadMember(req, "_Flag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        Level6Discovery.RecordProgressionRequest(flag);
        Level8Discovery.RecordProgressionRequest(flag);
        MusicLabDiscovery.RecordProgressionRequest(req, flag);
        QuestActionDiscovery.RecordProgressionRequest(req, flag);
        GarageCartridgeAccess.RecordVanillaSourceCollected(req, flag);
        WeedKillerRandomization.RecordGeckoSourceCollected(req, flag);
        PlantPipesRandomization.RecordFrogHippoSourceCollected(req, flag);
        RootsBucketRandomization.RecordSourceCollected(req, flag);

    }

    public static void ProgressionFlagEventPostfix(object[]? __args)
    {
        object? evt = ReflectionUtil.FindArg(__args, "GameProgressionFlagUpdatedEvent");
        if (evt == null) return;

        string flag = ReflectionUtil.ReadMember(evt, "ProgressionFlag")?.ToString()
                      ?? ReflectionUtil.ReadMember(evt, "_ProgressionFlag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        GarageCartridgeAccess.RecordNativeBagProgressionFlagUpdated(evt, flag);

        Level6Discovery.RecordProgressionFlagUpdated(flag);
        Level8Discovery.RecordProgressionFlagUpdated(flag);
        MusicLabDiscovery.RecordProgressionFlagUpdated(evt, flag);
        SpecialModeDiscovery.RecordProgressionFlagUpdated(evt, flag);
        CharacterQuestItems.RecordConsumption(evt, flag);
        QuestChecks.Observe(evt, flag);
        ExpandedChecks.Observe(evt, flag);
        QuestActionDiscovery.RecordProgressionFlagUpdated(evt, flag);

    }

    public static void BagItemRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);
        GarageCartridgeAccess.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "ObtainBagItemRequest");
        if (req == null) return;

        Level6Discovery.RecordBagItem(req);
        Level8Discovery.RecordBagItem(req);
        MusicLabDiscovery.RecordBagItem(req);

    }

    public static void AbilityItemRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "EarnAbilityItemRequest");
        if (req == null) return;

        Level6Discovery.RecordAbilityItem(req);
        Level8Discovery.RecordAbilityItem(req);
        MusicLabDiscovery.RecordAbilityItem(req);

    }

}

internal static class SaveDataProbe
{
    public static void ProbePersistedLevel(object persistedEvent, string fallbackLevel)
    {
        object? levelObj = ReflectionUtil.UnwrapNullable(
            ReflectionUtil.ReadMember(persistedEvent, "Level"));

        if (levelObj == null)
        {
            Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: persisted event did not expose a LevelIdentifier.");
            return;
        }

        string variantText = "LevelVariant_Default";
        object? variantObj = ReflectionUtil.UnwrapNullable(
            ReflectionUtil.ReadMember(persistedEvent, "LevelVariant"));

        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null) return;

        try
        {
            Type? enquiriesType = ReflectionUtil.SafeGetTypes(asm)
                .FirstOrDefault(t => t.Name == "CurrentPlayerSaveEnquiries");

            if (enquiriesType == null)
            {
                Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: CurrentPlayerSaveEnquiries type not found.");
                return;
            }

            object? save = TryGetSelectedSave(enquiriesType);
            if (save == null)
            {
                Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: selected player save was unavailable.");
                return;
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] SAVE PROBE selectedSaveType={save.GetType().FullName}");

            MethodInfo? getLevelVariant = save.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m =>
                {
                    if (m.Name != "GetLevelVariant") return false;
                    var ps = m.GetParameters();
                    return ps.Length == 2
                        && ps[0].ParameterType.Name == "LevelIdentifier"
                        && ps[1].ParameterType.Name == "LevelVariantIdentifier";
                });

            if (getLevelVariant == null)
            {
                Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: selected save has no usable GetLevelVariant method.");
                return;
            }

            var parameters = getLevelVariant.GetParameters();

            if (variantObj == null || !parameters[1].ParameterType.IsInstanceOfType(variantObj))
                variantObj = TryBuildIdentifier(parameters[1].ParameterType, variantText);

            if (variantObj == null)
            {
                Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: could not construct LevelVariant_Default identifier.");
                return;
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] SAVE PROBE begin internal={fallbackLevel} variant={ReflectionUtil.ExtractIdentifier(variantObj) ?? variantText}");

            object? levelState = getLevelVariant.Invoke(save, new[] { levelObj, variantObj });

            if (levelState == null)
            {
                Plugin.LoggerInstance?.LogWarning("[SCRC-AP] SAVE PROBE: GetLevelVariant returned null.");
                return;
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] SAVE PROBE levelStateType={levelState.GetType().FullName}");

            DumpInterestingMembers(levelState, "LevelState", depth: 0);
            ProbeCharacterRatings(levelState);
            ProbeStarDictionary(levelState);
        }
        catch (TargetInvocationException tie)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SAVE PROBE invocation failed: {tie.InnerException?.Message ?? tie.Message}");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SAVE PROBE failed: {ex.GetBaseException().Message}");
        }
    }

    private static object? TryGetSelectedSave(Type enquiriesType)
    {
        foreach (string methodName in new[]
        {
            "GetSelectedSlotSaveFileState",
            "TryGetSelectedSlotSaveFileState"
        })
        {
            MethodInfo? m = enquiriesType.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == methodName && x.GetParameters().Length == 0);

            if (m == null) continue;

            try
            {
                object? result = m.Invoke(null, null);
                if (result != null)
                {
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] SAVE PROBE source=CurrentPlayerSaveEnquiries.{methodName}()");
                    return result;
                }
            }
            catch (TargetInvocationException tie)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] SAVE PROBE {methodName} threw: {tie.InnerException?.Message ?? tie.Message}");
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] SAVE PROBE {methodName} failed: {ex.Message}");
            }
        }

        return null;
    }

    private static object? TryBuildIdentifier(Type identifierType, string text)
    {
        foreach (ConstructorInfo ctor in identifierType.GetConstructors(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            ParameterInfo[] ps;
            try { ps = ctor.GetParameters(); }
            catch { continue; }

            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                try { return ctor.Invoke(new object[] { text }); }
                catch { }
            }
        }

        foreach (MethodInfo m in identifierType.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (m.ReturnType != identifierType) continue;
            ParameterInfo[] ps;
            try { ps = m.GetParameters(); }
            catch { continue; }

            if (ps.Length == 1 && ps[0].ParameterType == typeof(string))
            {
                try { return m.Invoke(null, new object[] { text }); }
                catch { }
            }
        }

        return null;
    }

    private static void ProbeCharacterRatings(object state)
    {
        MethodInfo? getter = state.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "GetStarRatingForCharacter" &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType.Name == "ePlayableCharacter");

        if (getter == null)
        {
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] SAVE PROBE: GetStarRatingForCharacter was not found.");
            return;
        }

        Type enumType = getter.GetParameters()[0].ParameterType;
        if (!enumType.IsEnum) return;

        foreach (object character in Enum.GetValues(enumType))
        {
            try
            {
                object? rating = getter.Invoke(state, new[] { character });
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] SAVE CharacterStarRating[{character}]={rating}");
            }
            catch { }
        }
    }

    private static void ProbeStarDictionary(object state)
    {
        object? dict = ReflectionUtil.ReadMember(state, "StarRatingPerCharacter");
        if (dict == null)
        {
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] SAVE StarRatingPerCharacter=<null/unavailable>");
            return;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] SAVE StarRatingPerCharacter type={dict.GetType().FullName}");

        try
        {
            if (dict is System.Collections.IEnumerable enumerable)
            {
                int count = 0;
                foreach (object? entry in enumerable)
                {
                    if (entry == null) continue;

                    object? key = ReflectionUtil.ReadMember(entry, "Key");
                    object? value = ReflectionUtil.ReadMember(entry, "Value");

                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] SAVE StarRatingPerCharacter[{key ?? "?"}]={value ?? entry}");
                    count++;

                    if (count >= 32) break;
                }

                if (count == 0)
                    Plugin.LoggerInstance?.LogInfo("[SCRC-AP] SAVE StarRatingPerCharacter was enumerable but yielded no entries.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] SAVE StarRatingPerCharacter enumeration failed: {ex.Message}");
        }
    }

    private static void DumpInterestingMembers(object obj, string prefix, int depth)
    {
        if (depth > 2 || obj == null) return;

        Type t = obj.GetType();
        string[] keywords =
        {
            "Star", "Score", "Medal", "Result", "Best", "Character",
            "Player", "Completed", "Success", "Variant", "Level"
        };

        var seen = new HashSet<string>();

        foreach (PropertyInfo p in t.GetProperties(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (p.GetIndexParameters().Length != 0) continue;
            if (!keywords.Any(k => p.Name.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;
            if (!seen.Add(p.Name)) continue;

            object? value;
            try { value = p.GetValue(obj); }
            catch { continue; }

            LogValue($"{prefix}.{p.Name}", value, depth);
        }

        foreach (FieldInfo f in t.GetFields(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!keywords.Any(k => f.Name.Contains(k, StringComparison.OrdinalIgnoreCase))) continue;
            if (!seen.Add(f.Name)) continue;

            object? value;
            try { value = f.GetValue(obj); }
            catch { continue; }

            LogValue($"{prefix}.{f.Name}", value, depth);
        }
    }

    private static void LogValue(string name, object? value, int depth)
    {
        if (value == null)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SAVE {name}=<null>");
            return;
        }

        object? unwrapped = ReflectionUtil.UnwrapNullable(value);
        if (unwrapped == null)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SAVE {name}=<nullable-null>");
            return;
        }

        Type t = unwrapped.GetType();

        if (t.IsPrimitive || t.IsEnum || unwrapped is string || unwrapped is decimal)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SAVE {name}={unwrapped}");
            return;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] SAVE {name}=<{t.FullName}> {unwrapped}");

        DumpInterestingMembers(unwrapped, name, depth + 1);
    }
}
