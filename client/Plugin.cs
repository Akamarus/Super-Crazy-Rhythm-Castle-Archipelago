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
    public const string PluginVersion = "0.68.0";
    public const string GameName = "Super Crazy Rhythm Castle";

    internal static ManualLogSource? LoggerInstance;
    internal static ArchipelagoClient? AP;

    private Harmony? _harmony;

    public override void Load()
    {
        LoggerInstance = Log;

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
        var randomizeEarlyProgression = Config.Bind("Archipelago", "RandomizeEarlyProgression", false,
            "EXPERIMENTAL: suppress selected vanilla early-route unlock flags until the corresponding AP item is received.");
        var directStartAtLevelOne = Config.Bind("QualityOfLife", "DirectStartAtLevelOne", false,
            "Legacy option: skip the fresh-save intro/tutorial and start in Roots outside Level 1.");
        var directStartAtPhoneHub = Config.Bind("QualityOfLife", "DirectStartAtPhoneHub", false,
            "Area-routing start: skip the fresh-save intro/tutorial and start in the Music Lab / six-phone hub (GameRoom_Hub6). If enabled, this takes precedence over DirectStartAtLevelOne.");
        var bunkerStarRequirement = Config.Bind("QualityOfLife", "BunkerStarRequirement", 50,
            "Star requirement for the final Secret Bunker Star Eater. Vanilla is 66. This changes only that Star Eater threshold and does not alter saved level star ratings.");
        var developerHarness = Config.Bind("Developer", "EnableTestHarness", true,
            "Enable F7 Level 4 scene discovery plus F8/F9/F10/F11 progression test hotkeys.");
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

        patched += PatchMethodsByParameter("ProcessRequest", "PersistLevelResultRequest", nameof(GamePatches.PersistResultPostfix));
        patched += PatchMethodsByParameterWithPrefixAndPostfix(
            "ProcessRequest",
            "ApplyLevelResultToSaveDataRequest",
            nameof(GamePatches.ApplyResultPrefix),
            nameof(GamePatches.ApplyResultPostfix));
        patched += PatchCassetteEvaluation();
        patched += PatchCassetteStatusRequest();
        patched += PatchExactMethod("SaveDataRequestProcessor", "ChangeSelectedPlayerSaveSlot", "Int32", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix));
        patched += PatchExactMethod("SaveDataRequestProcessor", "CreateNewPlayerSaveFileInEmptySlot", "Int32", nameof(CassetteSaveTransactionPatches.SelectedSlotMutationPostfix));
        patched += PatchExactMethodWithPrefixAndPostfix("SaveDataRequestProcessor", "ProcessRequest", "BuildPlayerSaveStateFromFileRequest", nameof(CassetteSaveTransactionPatches.BuildPlayerSaveStatePrefix), nameof(CassetteSaveTransactionPatches.BuiltPlayerSaveStatePostfix));
        patched += PatchExactMethodWithPrefixAndPostfix("SaveDataRequestProcessor", "ProcessRequest", "PersistSaveChangeBundleRequest", nameof(CassetteSaveTransactionPatches.PersistBundleRoutingPrefix), nameof(CassetteSaveTransactionPatches.PersistBundleRoutingPostfix));
        patched += PatchExactMethodWithPrefixAndPostfix("SaveDataRequestProcessor", "ProcessRequest", "PersistAllSaveChangeBundlesRequest", nameof(CassetteSaveTransactionPatches.PersistAllRoutingPrefix), nameof(CassetteSaveTransactionPatches.PersistAllRoutingPostfix));
        patched += PatchExactMethodWithPrefixAndPostfix("SaveDataRequestProcessor", "ProcessRequest", "DiscardAllUnstagedSaveStateChangesRequest", nameof(CassetteSaveTransactionPatches.DiscardAllRoutingPrefix), nameof(CassetteSaveTransactionPatches.DiscardAllRoutingPostfix));
        patched += PatchExactMethodPrefix("SaveDataRequestProcessor", "ProcessRequest", "SelectMostRecentlyUsedRegularPlayerSaveSlotRequest", nameof(CassetteSaveTransactionPatches.MostRecentSelectionPrefix));
        patched += PatchMethodsByParameter("HandleEvent", "PlayerSaveWriteCompletedEvent", nameof(CassetteSaveTransactionPatches.PlayerSaveWriteCompletedEventPostfix));
        patched += PatchMethodsByParameter("HandleEvent", "LevelResultWasPersistedEvent", nameof(GamePatches.ResultPersistedEventPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "SetScoredSongInCurrentLevelRequest", nameof(GamePatches.SetScoredSongRequestPostfix));
        patched += PatchGarageScoredSongSequenceStep();
        patched += PatchMethodsByParameterWithPrefixAndPostfix(
            "ProcessRequest",
            "RecordGameProgressionInSaveDataRequest",
            nameof(ProgressionPatches.ProgressionRequestPrefix),
            nameof(ProgressionPatches.ProgressionRequestPostfix));
        patched += PatchMethodsByParameter("HandleEvent", "GameProgressionFlagUpdatedEvent", nameof(ProgressionPatches.ProgressionFlagEventPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "ObtainBagItemRequest", nameof(ProgressionPatches.BagItemRequestPostfix));
        patched += PatchMethodsByParameter("ProcessRequest", "EarnAbilityItemRequest", nameof(ProgressionPatches.AbilityItemRequestPostfix));
        patched += PatchMethodsByParameter(
            "ProcessRequest",
            "CreateNewPlayerSaveFileInSlotRequest",
            nameof(IntroRoomToHubRedirectPatches.NewSaveCreatedPostfix));
        patched += PatchIntroRoomToHubRedirect();
        patched += PatchDifficultyAssignmentRequest();
        patched += PatchEarlyUnlockSequenceSteps();
        patched += PatchMusicLabMedalScoreOverride();
        patched += PatchHub6AreaPhoneProgressionConditions();
        patched += PatchRootsOwnerDiagnostic();
        patched += PatchRootsComputerNormalization();

        Log.LogInfo($"[SCRC-AP] Game hooks installed: {patched}.");

        NativeProgression.RandomizeEarlyProgression = randomizeEarlyProgression.Value && !areaAccessPrototype.Value;
        if (randomizeEarlyProgression.Value && areaAccessPrototype.Value)
        {
            Log.LogWarning(
                "[SCRC-AP] LEGACY LEVEL ACCESS BLOCKERS SUSPENDED: Area Access routing is enabled, so Level 2-22 Access keepers will not enforce the obsolete per-level permission model. Vanilla level/item prerequisites remain authoritative until AP-Star gates replace them.");
        }
        IntroHubSkip.Configure(directStartAtPhoneHub.Value, directStartAtLevelOne.Value);
        AP = new ArchipelagoClient(server.Value, slot.Value, password.Value, applyReceivedProgression.Value);

        GarageCartridgeAccess.Configure();
        WeedKillerRandomization.Configure();
        PlantPipesRandomization.Configure();
        CassetteReceiptRandomization.Configure();
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
            AddComponent<PlantPipesReconciliationKeeper>();
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
            Log.LogWarning(
                $"[SCRC-AP] AREA ACCESS ROUTING ENABLED: startingSource='{AreaAccessPrototype.StartingArea}'. Hub6/Music Lab + Game Garage remain available. With PrototypeStartingArea=AP, all six major-area phones begin locked until the AP server supplies the seed starter. Locked PhoneBox interactions are disabled; known Hub6 cloth covers follow Area Access; unlocked late-area phones bypass only their local vanilla visited/open condition without changing its save flag; Hub6->area TransitionToGameRoomRequest calls remain safety-gated. PAGE UP remains a developer grant; PAGE DOWN is disabled in AP-driven mode; END prints status.");
            Log.LogWarning(
                "[SCRC-AP] AREA ARRIVAL PRESENTATION READY: AP-phone entry to Roots or Lobby temporarily satisfies only the exact first-arrival/HUD conditions during destination-scene initialization; native save flags and vanilla story-route arrivals remain unchanged. Roots Access also suppresses FirstAreaGate and the dedicated StarEaterBlockade/Blockade collider. HOME in Hub2 prints compact status only.");
        }

        Log.LogWarning(
            $"[SCRC-AP] MUSIC LAB REWARD CHEST AP CHECKS READY: thresholds=5/10/20/32/46/64/89/111/140. v0.67.60 reads each live Hub6 chest's native unlock requirement + chestUnlockedProgressionFlag only after Hub6 has loaded, then reconciles already-collected rewards against the save. No startup-time chest component is created; vanilla rewards are unchanged.");
        Log.LogWarning(
            $"[SCRC-AP] GAME GARAGE AP CHECKS READY: songs=6 cumulativeStickerTiers=Bronze/Silver/Gold/Platinum locationFormat='{LocationMap.GarageMedalLocationFormat}'. Cartridge insertion supplies song identity; SongStickerEvaluation supplies the earned sticker.");
        Log.LogWarning(
            $"[SCRC-AP] MUSIC LAB CASSETTE AP CHECKS READY: verifiedVariants=30 cumulativeMedalTiers=Bronze/Silver/Gold/Platinum locationFormat='{LocationMap.CassetteMedalLocationFormat}'. Verified mappings include 30 currently discovered cassette variants; result-time clean medal evaluation supplies Bronze/Silver/Gold/Platinum.");

        BunkerStarRequirementKeeper.RequiredStars =
            Math.Clamp(bunkerStarRequirement.Value, 0, 66);

        if (BunkerStarRequirementKeeper.RequiredStars != 66)
        {
            AddComponent<BunkerStarRequirementKeeper>();
            Log.LogWarning(
                $"[SCRC-AP] SECRET BUNKER STAR REQUIREMENT OVERRIDE ENABLED: final Hub8 Star Eater target={BunkerStarRequirementKeeper.RequiredStars} stars. v0.67.60 keeps the temporary GetValue=target patch and, only near this exact Hub8 Star Eater, temporarily enables its native interaction checks and registers the real NPC with the game's interaction-interest pipeline.");
        }

        if (NativeProgression.RandomizeEarlyProgression)
        {
            AddComponent<FirstAreaGateKeeper>();
            Log.LogWarning(
                "[SCRC-AP] FIRST AREA GATE KEEPER ENABLED: FirstAreaGate will be held closed until Level 2 Access.");

            AddComponent<Level3DoorKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 3 HARD BLOCKER ENABLED: Level_07_Entrance disabled; MegafierCover animation rewound/frozen into its default closed pose until Level 3 Access.");

            AddComponent<Level4VineKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 4 VINE BLOCKER ENABLED: Level_08_Entrance disabled and thick vine pose enforced until Level 4 Access; AP release disables BossWeeds-tofb.");

            AddComponent<Level4EntranceProxy>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 4 VANILLA ENTRANCE BRIDGE ENABLED: Hub2-only; Level 4 Access releases the vines, the native indicator/interaction is enabled only at the Level 4 mat, and the native patch is restored immediately when leaving Hub2.");

            AddComponent<Level5EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 5 HARD BLOCKER ENABLED: Placeholder_Level_09_Entrance is disabled until Level 5 Access; Hip Glasses, Chicken Bucket, bucket-minion trade/blockade, and King lift chat remain vanilla.");

            AddComponent<Level6EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 6 NATIVE INTERACTION BLOCKER ENABLED: Boring Room remains completely vanilla visually; while locked and the player is near the normal Level02Door, native LevelEntranceDoor.IsInteractionEnabled() is temporarily forced false. Manual button traversal, missing-button/phone-booth story, Room02 blocker logic, and Level02Door_DevilMode remain vanilla.");

            AddComponent<Level7EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 7 NATIVE INTERACTION BLOCKER ENABLED: Demolition Training Level19Door remains completely vanilla visually; while locked and the player is near it, shared Hub1 LevelEntranceDoor.IsInteractionEnabled() is forced false. Demolition Certificate, Level 21, training story, Minim Tower, and hand barriers remain vanilla.");

            AddComponent<Level8EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 8 NATIVE INTERACTION BLOCKER ENABLED: normal Minim Tower LevelEntranceDoor_11 remains completely vanilla visually; while locked and the player is near it in Hub1B, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Level11Door_DevilMode is intentionally untouched.");

            AddComponent<Level9EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 9 NATIVE INTERACTION BLOCKER ENABLED: normal School Trip Level20Door remains visually vanilla; while locked and the player is near it in Hub1A, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Minim Tower's vanilla Level20 unlock/cover logic remains authoritative.");

            AddComponent<Level10EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 10 NATIVE INTERACTION BLOCKER ENABLED: normal Vault LevelEntranceDoor_01 remains visually vanilla; while locked and the player is near it in Hub1B, shared LevelEntranceDoor.IsInteractionEnabled() is forced false.");

            AddComponent<Level11EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 11 NATIVE INTERACTION BLOCKER ENABLED: normal Act 1: Flavor LevelEntranceDoor_01 in Hub4 remains visually vanilla; while locked and the player is near it, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Wooden Spoon, Saw Disc, disc trader/music progression, MEAT_HUB_ACT_ONE_MUSIC_DONE, MEAT_HUB_GATE_OPENED, and the separate Bee Mode entrance remain vanilla.");

            AddComponent<Level12EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 12 NATIVE INTERACTION BLOCKER ENABLED: normal Act 2: Sauce and Spice LevelEntranceDoor_02 in Hub4 remains visually vanilla; while locked and the player is near it, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Smooch Cartridge, Hypno Pan/Pied Piper ability, meat-dog/crowd interactions, Act 2 music, cat escort, bouncer progression, and other Hub4 doors remain vanilla.");

            AddComponent<Level13EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 13 NATIVE INTERACTION BLOCKER ENABLED: normal Act 3: Montage LevelEntranceDoor_03 in Hub4 remains visually vanilla; while locked and the player is near it, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Act 3 music, Scruffy escort, bouncer progression, Hypno Pan/crowd behavior, and PlaceholderLevelDoor_03 remain vanilla.");

            AddComponent<Level14EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 14 NATIVE INTERACTION BLOCKER ENABLED: normal Act 4: Habanero LevelEntranceDoor_04 in Hub4 remains visually vanilla; while locked and the player is near it, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Act 4 music, open-window/mouse-revolution quest, Hypno Pan mouse escort, bouncer progression, chilli crowd, and PlaceholderLevelDoor_04 remain vanilla.");

            AddComponent<Level15EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 15 NATIVE INTERACTION BLOCKER ENABLED: Central Mainframe Level16Door in GameRoom_Hub5A remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Cell Tower intro, ExternalDoor/HR5B forward progression, Level16AttemptedCondition, EntranceDoor, and FutureLevelEntranceDoor remain vanilla.");

            AddComponent<Level16EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 16 NATIVE INTERACTION BLOCKER ENABLED: Thief Prince LevelEntranceDoor in GameRoom_Hub5B remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Bizzle, Clive, Bee Modes, Super Nectar, Prince notes/conditions, and all other Cell Tower progression remain vanilla.");

            AddComponent<Level17EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 17 NATIVE INTERACTION BLOCKER ENABLED: Cold Storage Level21Door in GameRoom_Hub1A remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Violance/VIOLIN_ABILITY, Hypno Pan usage, Thief Prince/Old Tom NPCs, heist story, and all other Lobby progression remain vanilla.");

            AddComponent<Level18EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 18 NATIVE INTERACTION BLOCKER ENABLED: The Darkness normal LevelEntranceDoor in Tower of Fear remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Darkness shield/totem logic, Plant Pipes, Minim's Eye, and all Tower story progression remain vanilla.");

            AddComponent<Level19EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 19 NATIVE INTERACTION BLOCKER ENABLED: Escape normal LevelEntranceDoor in Tower of Fear remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Escape's separate LevelEntranceDoor_DevilMode is explicitly untouched; Violance, Minim's Mind/Brain, shield/totem logic, and all Tower story progression remain vanilla.");

            AddComponent<Level20EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 20 NATIVE INTERACTION BLOCKER ENABLED: Loneliness normal LevelEntranceDoor in Tower of Fear remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Hypno Pan route/totem logic, Minim's Heart, and all Tower story progression remain vanilla.");

            AddComponent<Level21EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 21 NATIVE INTERACTION BLOCKER ENABLED: Locker Room normal LevelEntranceDoor_14 in Royal Corridor/GameRoom_Hub7 remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Royal Corridor intro/progression and the separate LevelEntranceDoor_14_DevilMode remain completely vanilla.");

            AddComponent<Level22EntranceKeeper>();
            Log.LogWarning(
                "[SCRC-AP] LEVEL 22 NATIVE INTERACTION BLOCKER ENABLED: King Ferdinand I normal LevelEntranceDoor_28_default in Royal Corridor/GameRoom_Hub7 remains visually vanilla; while locked and the player is near its normal mat, shared LevelEntranceDoor.IsInteractionEnabled() is forced false. Star Eater bridge, fridge/ice sequence, king cutscene, and all Royal Corridor story progression remain vanilla.");
        }

        DeveloperHarness.Enabled = developerHarness.Value;
        if (developerHarness.Value)
        {
            AddComponent<DeveloperHotkeys>();
            AddComponent<MusicLabDiagnosticKeeper>();
            Log.LogWarning(
                "[SCRC-AP] MUSIC LAB DIAGNOSTIC ENABLED: Hub6/GameRoom_27 plus live cassette-start enquiry probing. v0.67.60 keeps the solved Garage cartridge-state identity path, sends cumulative Garage sticker AP checks on real results, and tracks all nine Music Lab reward chests (5/10/20/32/46/64/89/111/140) from the live native Hub6 chest metadata; Hub6 F5 forces reconciliation and prints the discovered mapping/status.");
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
        MethodInfo? postfix = FindPatchMethod(
            typeof(GamePatches), nameof(GamePatches.CassetteStatusRequestPostfix));
        if (target == null || prefix == null || postfix == null || _harmony == null)
        {
            Log.LogWarning("[SCRC-AP] CASSETTE STATUS SOURCE hook unavailable.");
            return 0;
        }

        try
        {
            _harmony.Patch(target, prefix: new HarmonyMethod(prefix), postfix: new HarmonyMethod(postfix));
            Log.LogInfo("[SCRC-AP] CASSETTE STATUS SOURCE/ROUTING HOOKED PlayerSaveRequestProcessor.ProcessRequest(RecordSongCassetteStatusInSaveDataRequest).");
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

    private int PatchGarageScoredSongSequenceStep()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        Type? type = ReflectionUtil.SafeGetTypes(asm)
            .FirstOrDefault(t => t.Name == "SetScoredSongInCurrentLevelSequenceStep");

        if (type == null)
        {
            Log.LogWarning("[SCRC-AP] MUSIC LAB GARAGE SONG SEQUENCE target not found: SetScoredSongInCurrentLevelSequenceStep");
            return 0;
        }

        MethodInfo? prefix = FindPatchMethod(
            typeof(MusicLabSequencePatches),
            nameof(MusicLabSequencePatches.GarageScoredSongBeginPrefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] MUSIC LAB GARAGE SONG SEQUENCE prefix lookup failed.");
            return 0;
        }

        int count = 0;
        foreach (MethodInfo method in type.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.DeclaredOnly)
                 .Where(m => m.Name == "Begin"))
        {
            if (method.IsAbstract || method.ContainsGenericParameters)
                continue;

            try
            {
                _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                Log.LogInfo(
                    $"[SCRC-AP] MUSIC LAB GARAGE SONG SEQUENCE HOOKED {type.FullName}.{method.Name}");
                count++;
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    $"[SCRC-AP] Could not hook exact Garage scored-song sequence {type.FullName}.{method.Name}: {ex.GetBaseException().Message}");
            }
        }

        return count;
    }

    private int PatchEarlyUnlockSequenceSteps()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        MethodInfo? prefix = FindPatchMethod(
            typeof(EarlySequenceBlockerPatches),
            nameof(EarlySequenceBlockerPatches.Prefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] Early sequence blocker prefix lookup failed.");
            return 0;
        }

        int count = 0;

        var targets = new (string TypeName, string MethodName)[]
        {
            ("AssignAndCommentOnMusicDifficultySequenceStep", "Begin"),
        };

        foreach (var target in targets)
        {
            Type? type = ReflectionUtil.SafeGetTypes(asm)
                .FirstOrDefault(t => t.Name == target.TypeName);

            if (type == null)
            {
                Log.LogWarning(
                    $"[SCRC-AP] Early sequence target not found: {target.TypeName}");
                continue;
            }

            foreach (MethodInfo method in type.GetMethods(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.DeclaredOnly)
                     .Where(m => m.Name == target.MethodName))
            {
                if (method.IsAbstract || method.ContainsGenericParameters)
                    continue;

                try
                {
                    _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                    Log.LogInfo(
                        $"[SCRC-AP] EARLY SEQUENCE BLOCKER HOOKED {type.FullName}.{method.Name}");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogWarning(
                        $"[SCRC-AP] Could not hook early sequence {type.FullName}.{method.Name}: {ex.GetBaseException().Message}");
                }
            }
        }

        return count;
    }

    private int PatchHub02MegafierDoorView()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        Type? type = ReflectionUtil.SafeGetTypes(asm)
            .FirstOrDefault(t => t.Name == "Hub02MegafierDoorView");

        if (type == null)
        {
            Log.LogWarning("[SCRC-AP] Hub02MegafierDoorView not found.");
            return 0;
        }

        MethodInfo? prefix = FindPatchMethod(
            typeof(Hub02MegafierDoorViewPatches),
            nameof(Hub02MegafierDoorViewPatches.Prefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] Hub02 Megafier door view prefix lookup failed.");
            return 0;
        }

        int count = 0;

        foreach (MethodInfo method in type.GetMethods(
                     BindingFlags.Public | BindingFlags.NonPublic |
                     BindingFlags.Instance | BindingFlags.DeclaredOnly)
                 .Where(m => m.Name == "RefreshToMatchState"))
        {
            try
            {
                _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                Log.LogInfo(
                    "[SCRC-AP] HUB02 DOOR VIEW BLOCKER HOOKED Hub02MegafierDoorView.RefreshToMatchState");
                count++;
            }
            catch (Exception ex)
            {
                Log.LogWarning(
                    $"[SCRC-AP] Could not hook Hub02MegafierDoorView.RefreshToMatchState: {ex.GetBaseException().Message}");
            }
        }

        return count;
    }

    private int PatchDifficultyAssignmentRequest()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        MethodInfo? prefix = FindPatchMethod(
            typeof(EarlyRuntimeUnlockPatches),
            nameof(EarlyRuntimeUnlockPatches.DifficultyRequestPrefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] DifficultyRequestPrefix method lookup failed.");
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

                if (!ps.Any(p => p.ParameterType.Name == "SetPlayerTrackingDifficultyRequest"))
                    continue;

                try
                {
                    _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
                    Log.LogInfo(
                        $"[SCRC-AP] RUNTIME DIFFICULTY BLOCKER HOOKED {type.FullName}.ProcessRequest(SetPlayerTrackingDifficultyRequest)");
                    count++;
                }
                catch (Exception ex)
                {
                    Log.LogError(
                        $"[SCRC-AP] Failed to hook runtime difficulty request: {ex.GetBaseException().Message}");
                }
            }
        }

        return count;
    }

    private int PatchHub02ChickenBlockade()
    {
        Assembly? asm = ReflectionUtil.GameAssembly;
        if (asm == null)
            return 0;

        Type? type = ReflectionUtil.SafeGetTypes(asm)
            .FirstOrDefault(t => t.Name == "Hub02ChickenBlockadeView");

        if (type == null)
        {
            Log.LogWarning("[SCRC-AP] Hub02ChickenBlockadeView not found.");
            return 0;
        }

        MethodInfo? prefix = FindPatchMethod(
            typeof(EarlyRuntimeUnlockPatches),
            nameof(EarlyRuntimeUnlockPatches.ChickenBlockadePrefix));

        if (prefix == null)
        {
            Log.LogError("[SCRC-AP] Chicken blockade prefix was not found.");
            return 0;
        }

        MethodInfo? method = type.GetMethod(
            "TriggerLeaveAnimation",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            Log.LogWarning("[SCRC-AP] Hub02ChickenBlockadeView.TriggerLeaveAnimation not found.");
            return 0;
        }

        try
        {
            _harmony!.Patch(method, prefix: new HarmonyMethod(prefix));
            Log.LogInfo(
                "[SCRC-AP] EARLY GATE BLOCKER HOOKED Hub02ChickenBlockadeView.TriggerLeaveAnimation");
            return 1;
        }
        catch (Exception ex)
        {
            Log.LogError(
                $"[SCRC-AP] Failed to hook Hub02 chicken blockade: {ex.GetBaseException().Message}");
            return 0;
        }
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

        int count = 0;
        foreach (MethodInfo method in methods)
        {
            try
            {
                _harmony.Patch(method, prefix: new HarmonyMethod(prefix));
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

    private ArchipelagoSession? _session;
    private volatile bool _connected;
    private int _reconnectWorkerActive;
    private readonly HashSet<int> _processedReceivedItemIndexes = new();

    public bool Connected => _connected;

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

        try
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] NET assemblies Archipelago.MultiClient.Net loaded=" +
                $"{AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Archipelago.MultiClient.Net")}");

            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET stage=create-session");
            ArchipelagoSession session = ArchipelagoSessionFactory.CreateSession(_server);
            Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET stage=session-created");

            session.Socket.SocketOpened += () =>
                Plugin.LoggerInstance?.LogInfo("[SCRC-AP] NET socket-opened");

            session.Socket.SocketClosed += reason =>
            {
                if (!IsCurrentSession(session))
                    return;
                _connected = false;
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] NET socket-closed reason='{reason}'");
                RequestReconnect($"socket closed: {reason}");
            };

            session.Socket.ErrorReceived += (exception, message) =>
            {
                Plugin.LoggerInstance?.LogError(
                    $"[SCRC-AP] NET socket-error message='{message}' exception={exception}");
                if (IsCurrentSession(session) && !session.Socket.Connected)
                {
                    _connected = false;
                    RequestReconnect($"terminal socket error: {message}");
                }
            };

            session.Items.ItemReceived += helper =>
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
                            bool handledAreaAccess = AreaAccessPrototype.TryApplyItem(item.ItemName);
                            bool handledGarageCartridge = GarageCartridgeAccess.TryApplyItem(item.ItemName);
                            bool handledWeedKiller = WeedKillerRandomization.TryApplyItem(item.ItemName);
                            bool handledPlantPipes = PlantPipesRandomization.TryApplyItem(item.ItemName);
                            bool handledPreviewAbility = PreviewAbilityRandomization.TryApplyItem(item.ItemName);
                            bool handledRootsBucket = RootsBucketRandomization.TryApplyItem(item.ItemName);

                            if (!handledAreaAccess && !handledGarageCartridge && !handledWeedKiller && !handledPlantPipes && !handledPreviewAbility && !handledRootsBucket && _applyReceivedProgression)
                                NativeProgression.ApplyArchipelagoItem(item.ItemName);
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
            };

            ArchipelagoSession? previous;
            lock (_lock)
            {
                previous = _session;
                _session = session;
            }
            if (previous != null && !ReferenceEquals(previous, session))
                _ = previous.Socket.DisconnectAsync();

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

            if (!result.Successful)
            {
                if (result is LoginFailure failure)
                    Plugin.LoggerInstance?.LogError(
                        $"[SCRC-AP] Archipelago login failed: {string.Join("; ", failure.Errors)}");
                else
                    Plugin.LoggerInstance?.LogError("[SCRC-AP] Archipelago login failed.");
                return false;
            }

            if (result is LoginSuccessful loginSuccess)
            {
                try
                {
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
                }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogError(
                        $"[SCRC-AP] Failed to apply Area Access starter from slot data: {ex}");
                }
            }

            _connected = true;
            _reconnectPolicy.OnConnected();
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] CONNECTED server={_server} slot='{_slot}' game='{Plugin.GameName}'.");

            FlushPendingChecks();
            PreviewAbilityRandomization.RequestUnityReconciliation("Archipelago connected");
            return true;
        }
        catch (Exception ex)
        {
            _connected = false;
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] NET inner exception type={ex.GetType().FullName}: {ex}");
            return false;
        }
        }
    }

    public void Shutdown()
    {
        _connected = false;
        _reconnectPolicy.OnDeliberateShutdown();
        _shutdownToken.Cancel();
        ArchipelagoSession? session;
        lock (_lock)
        {
            session = _session;
            _session = null;
        }
        if (session != null)
            _ = session.Socket.DisconnectAsync();
    }

    private bool IsCurrentSession(ArchipelagoSession session)
    {
        lock (_lock)
            return ReferenceEquals(_session, session);
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
                TimeSpan? delay = _reconnectPolicy.NextDelay();
                if (delay == null)
                    return;
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] NET reconnect waiting seconds={delay.Value.TotalSeconds:0}.");
                await Task.Delay(delay.Value, _shutdownToken.Token).ConfigureAwait(false);
                if (TryConnectOnce())
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

    public void QueueLocation(string locationName)
    {
        lock (_lock)
        {
            if (!_queuedOrSent.Add(locationName))
            {
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] Duplicate local check ignored: {locationName}");
                return;
            }
        }

        _pendingChecks.Enqueue(locationName);
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] QUEUED CHECK '{locationName}'.");

        if (_connected)
            Task.Run(FlushPendingChecks);
    }

    private void FlushPendingChecks()
    {
        ArchipelagoSession? session;
        lock (_lock)
            session = _session;
        if (!_connected || session == null)
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

                session.Locations.CompleteLocationChecks(id);
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] SENT CHECK '{locationName}' ({id}).");
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogError($"[SCRC-AP] Failed to send '{locationName}': {ex.GetBaseException().Message}");
                _pendingChecks.Enqueue(locationName);
                if (IsCurrentSession(session))
                {
                    _connected = false;
                    RequestReconnect("location check send failed");
                }
                break;
            }
        }
    }
}

internal sealed class Level5EntranceKeeper : MonoBehaviour
{
    private const string EntranceName = "Placeholder_Level_09_Entrance";
    private const string EntrancePath =
        "Root/GameRoom_Hub2_Logic/Objects/LevelDoors/Placeholder_Level_09_Entrance";

    private Transform? _entrance;
    private bool _baselineActive = true;
    private bool _baselineCaptured;

    private int _searchCooldown;
    private bool _reportedLocked;
    private bool _reportedReleased;

    public Level5EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsHub2Current)
        {
            return;
        }

        if (!EntranceValid())
        {
            if (_searchCooldown > 0)
            {
                _searchCooldown--;
                return;
            }

            _searchCooldown = 20;
            FindEntrance();
        }

        if (!EntranceValid())
            return;

        if (NativeProgression.HasLevel5Access)
        {
            ReleaseBlock();
            return;
        }

        ApplyBlock();
    }

    private bool EntranceValid()
    {
        try
        {
            return _entrance != null &&
                   _entrance.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private void FindEntrance()
    {
        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                if (!string.Equals(
                        tr.name,
                        EntranceName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string path = BuildHierarchy(tr);

                if (!path.Contains(
                        EntrancePath,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                _entrance = tr;
                _baselineActive = tr.gameObject.activeSelf;
                _baselineCaptured = true;
                _reportedLocked = false;
                _reportedReleased = false;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 5 ENTRANCE CAPTURED: '{path}' " +
                    $"baselineActive={_baselineActive}; " +
                    $"child KingLiftChatWitnessed_Condition remains vanilla.");

                return;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 5 entrance search failed: {ex.GetBaseException().Message}");
        }
    }

    private void ApplyBlock()
    {
        if (!_baselineCaptured || _entrance == null)
            return;

        try
        {
            if (_entrance.gameObject.activeSelf)
                _entrance.gameObject.SetActive(false);

            _reportedReleased = false;

            if (!_reportedLocked)
            {
                _reportedLocked = true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] LEVEL 5 HARD BLOCK ACTIVE: Placeholder_Level_09_Entrance disabled until Level 5 Access. Vanilla bucket/lift quest progression is untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] Level 5 hard block failed: {ex.GetBaseException().Message}");
            _entrance = null;
            _baselineCaptured = false;
        }
    }

    private void ReleaseBlock()
    {
        if (!_baselineCaptured || _entrance == null)
            return;

        try
        {
            if (_entrance.gameObject.activeSelf != _baselineActive)
                _entrance.gameObject.SetActive(_baselineActive);

            _reportedLocked = false;

            if (!_reportedReleased)
            {
                _reportedReleased = true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] LEVEL 5 HARD BLOCK RELEASED: Level 5 Access present; Placeholder_Level_09_Entrance restored. Its vanilla KingLiftChatWitnessed_Condition still decides when Lift Quest is actually usable.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] Level 5 release failed: {ex.GetBaseException().Message}");
            _entrance = null;
            _baselineCaptured = false;
        }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 28 && current != null; i++)
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


internal static class Level5Discovery
{
    private static readonly object Sync = new();

    private static bool _active;
    private static int _sequence;

    private static readonly string[] RouteObjectTokens =
    {
        "Level_09",
        "Level09",
        "Lift",
        "Elevator",
        "Lobby",
        "Chicken",
        "Bucket",
        "Combo",
        "Blockade",
        "LevelDoor",
        "Entrance"
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
            $"[SCRC-AP] ===== LEVEL 5 DISCOVERY START ===== reason='{reason}' currentRoom='{DeveloperHarness.CurrentRoomId}'.");

        Record(
            "BASELINE",
            "Tracing vanilla progression from Level_08 completion toward user-facing Level 5 / internal Level_09. No Level 5 progression is blocked in this build.");
    }

    public static void RecordLevelResultApplied(string level)
    {
        if (string.Equals(
                level,
                "Level_08",
                StringComparison.OrdinalIgnoreCase))
        {
            Begin("Level_08 result applied");
        }

        if (Active)
            Record("LEVEL RESULT APPLIED", level);
    }

    public static void RecordLevelPersisted(string level)
    {
        if (!Active &&
            string.Equals(
                level,
                "Level_08",
                StringComparison.OrdinalIgnoreCase))
        {
            Begin("Level_08 result persisted");
        }

        if (Active)
            Record("LEVEL PERSISTED", level);
    }

    public static void RecordTransition(string roomId)
    {
        if (!Active)
            return;

        Record("ROOM TRANSITION", roomId);

        if (roomId.Contains(
                "GameRoom_09",
                StringComparison.OrdinalIgnoreCase))
        {
            Record(
                "MILESTONE",
                "GameRoom_09 transition observed; Level 5 room appears reachable.");
        }
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
            $"[SCRC-AP] ===== LEVEL 5 F5 SCENE SCAN BEGIN ===== currentRoom='{room}'.");

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

                if (!seen.Add(path))
                    continue;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 5 ROUTE OBJECT path='{path}' " +
                    $"activeSelf={SafeActiveSelf(tr)} activeInHierarchy={SafeActiveInHierarchy(tr)} " +
                    $"worldPos=({tr.position.x:0.###},{tr.position.y:0.###},{tr.position.z:0.###}).");

                matches++;

                if (matches >= 250)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        "[SCRC-AP] LEVEL 5 F5 SCENE SCAN capped at 250 matching transforms.");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 5 F5 SCENE SCAN failed: {ex.GetBaseException().Message}");
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ===== LEVEL 5 F5 SCENE SCAN END ===== matches={matches}.");
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
            $"[SCRC-AP] LEVEL5 TRACE #{sequence:000} [{kind}] {detail}");
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
            return "<unavailable>";
        }
    }
}


internal static class NativeLevel6DoorBridge
{
    private const string NativeLibrary = "GameAssembly.dll";
    private const string DoorComponentName = "LevelEntranceDoor";

    // Level 6 and Level 7 are both in Hub1A and both need to temporarily force
    // the shared LevelEntranceDoor.IsInteractionEnabled() implementation false.
    // Owners prevent one keeper from restoring the native bytes while another
    // keeper still legitimately needs the patch.
    private static readonly HashSet<string> ActiveOwners =
        new(StringComparer.Ordinal);

    private static IntPtr _doorObjectPointer;
    private static IntPtr _doorClass;

    private static IntPtr _interactionEnabledCodePointer;
    private static byte[]? _interactionEnabledOriginalBytes;
    private static bool _interactionEnabledNativePatched;

    private static bool _reportedPatchActive;
    private static bool _reportedFailure;

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_get_class(IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_method_from_name(
        IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int argsCount);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_parent(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_method_get_pointer(IntPtr method);

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

    public static void ResetForSceneChange()
    {
        ActiveOwners.Clear();
        RestoreInteractionEnabledNativePatch();

        _doorObjectPointer = IntPtr.Zero;
        _doorClass = IntPtr.Zero;
        _reportedPatchActive = false;
        _reportedFailure = false;
    }

    public static bool RequestBlock(
        string owner,
        GameObject normalDoorRoot,
        string label)
    {
        if (string.IsNullOrWhiteSpace(owner))
            return false;

        ActiveOwners.Add(owner);

        if (_interactionEnabledNativePatched)
            return true;

        try
        {
            if (!EnsureDoorPointer(normalDoorRoot))
            {
                ActiveOwners.Remove(owner);
                ReportFailureOnce(
                    $"could not resolve the native LevelEntranceDoor instance for {label}");
                return false;
            }

            IntPtr method = FindMethodInHierarchy(
                _doorClass,
                "IsInteractionEnabled",
                0);

            if (method == IntPtr.Zero)
            {
                ActiveOwners.Remove(owner);
                ReportFailureOnce(
                    "native LevelEntranceDoor.IsInteractionEnabled() was not found");
                return false;
            }

            IntPtr codePointer;

            try
            {
                codePointer = il2cpp_method_get_pointer(method);
            }
            catch (EntryPointNotFoundException)
            {
                // Verified Unity 2021 IL2CPP fallback used by the successful
                // Level 4 and Level 6 native interaction bridges.
                codePointer = Marshal.ReadIntPtr(method);
            }

            if (codePointer == IntPtr.Zero)
            {
                ActiveOwners.Remove(owner);
                ReportFailureOnce(
                    "native IsInteractionEnabled() code pointer was null");
                return false;
            }

            // Windows x64: mov eax,0 ; ret
            byte[] forceFalse =
            {
                0xB8, 0x00, 0x00, 0x00, 0x00,
                0xC3
            };

            byte[] original = new byte[forceFalse.Length];
            Marshal.Copy(
                codePointer,
                original,
                0,
                original.Length);

            const uint PAGE_EXECUTE_READWRITE = 0x40;

            if (!VirtualProtect(
                    codePointer,
                    (UIntPtr)forceFalse.Length,
                    PAGE_EXECUTE_READWRITE,
                    out uint oldProtect))
            {
                ActiveOwners.Remove(owner);
                ReportFailureOnce(
                    $"VirtualProtect failed at 0x{codePointer.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                Marshal.Copy(
                    forceFalse,
                    0,
                    codePointer,
                    forceFalse.Length);

                FlushInstructionCache(
                    GetCurrentProcess(),
                    codePointer,
                    (UIntPtr)forceFalse.Length);
            }
            finally
            {
                VirtualProtect(
                    codePointer,
                    (UIntPtr)forceFalse.Length,
                    oldProtect,
                    out _);
            }

            _interactionEnabledCodePointer = codePointer;
            _interactionEnabledOriginalBytes = original;
            _interactionEnabledNativePatched = true;

            if (!_reportedPatchActive)
            {
                _reportedPatchActive = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] HUB1 NATIVE INTERACTION BLOCK ACTIVE: forcing shared LevelEntranceDoor.IsInteractionEnabled() false. firstOwner='{owner}' target='{label}'.");
            }

            return true;
        }
        catch (Exception ex)
        {
            ActiveOwners.Remove(owner);
            ReportFailureOnce(
                $"native interaction disable failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    public static void ReleaseBlock(string owner)
    {
        if (string.IsNullOrWhiteSpace(owner))
            return;

        ActiveOwners.Remove(owner);

        if (ActiveOwners.Count > 0)
            return;

        RestoreInteractionEnabledNativePatch();
    }

    public static void RestoreInteractionEnabledNativePatch()
    {
        if (!_interactionEnabledNativePatched)
            return;

        IntPtr codePointer = _interactionEnabledCodePointer;
        byte[]? original = _interactionEnabledOriginalBytes;

        try
        {
            if (codePointer != IntPtr.Zero &&
                original != null &&
                original.Length > 0)
            {
                const uint PAGE_EXECUTE_READWRITE = 0x40;

                if (!VirtualProtect(
                        codePointer,
                        (UIntPtr)original.Length,
                        PAGE_EXECUTE_READWRITE,
                        out uint oldProtect))
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] HUB1 native interaction restore VirtualProtect failed; Win32={Marshal.GetLastWin32Error()}.");
                    return;
                }

                try
                {
                    Marshal.Copy(
                        original,
                        0,
                        codePointer,
                        original.Length);

                    FlushInstructionCache(
                        GetCurrentProcess(),
                        codePointer,
                        (UIntPtr)original.Length);
                }
                finally
                {
                    VirtualProtect(
                        codePointer,
                        (UIntPtr)original.Length,
                        oldProtect,
                        out _);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] HUB1 native interaction restore failed: {ex.GetBaseException().Message}");
        }
        finally
        {
            _interactionEnabledNativePatched = false;
            _interactionEnabledCodePointer = IntPtr.Zero;
            _interactionEnabledOriginalBytes = null;
            _reportedPatchActive = false;
        }
    }

    private static bool EnsureDoorPointer(GameObject normalDoorRoot)
    {
        if (_doorObjectPointer != IntPtr.Zero &&
            _doorClass != IntPtr.Zero)
        {
            return true;
        }

        object? rawComponent = FindComponentByNativeName(
            normalDoorRoot,
            DoorComponentName);

        if (rawComponent == null)
            return false;

        IntPtr pointer = GetIl2CppPointer(rawComponent);
        if (pointer == IntPtr.Zero)
            return false;

        IntPtr klass = il2cpp_object_get_class(pointer);
        if (klass == IntPtr.Zero)
            return false;

        _doorObjectPointer = pointer;
        _doorClass = klass;
        return true;
    }

    private static IntPtr FindMethodInHierarchy(
        IntPtr klass,
        string methodName,
        int argCount)
    {
        IntPtr current = klass;

        for (int depth = 0;
             current != IntPtr.Zero && depth < 12;
             depth++)
        {
            IntPtr method = il2cpp_class_get_method_from_name(
                current,
                methodName,
                argCount);

            if (method != IntPtr.Zero)
                return method;

            current = il2cpp_class_get_parent(current);
        }

        return IntPtr.Zero;
    }

    private static object? FindComponentByNativeName(
        GameObject root,
        string componentName)
    {
        MethodInfo? getByName = typeof(GameObject).GetMethod(
            "GetComponent",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        if (getByName == null)
            return null;

        object? direct = getByName.Invoke(
            root,
            new object[] { componentName });

        if (direct != null)
            return direct;

        foreach (Transform tr in root.transform.GetComponentsInChildren<Transform>(true))
        {
            if (tr == null || tr.gameObject == null)
                continue;

            object? found = getByName.Invoke(
                tr.gameObject,
                new object[] { componentName });

            if (found != null)
                return found;
        }

        return null;
    }

    private static IntPtr GetIl2CppPointer(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance);

            return property?.GetValue(value) is IntPtr pointer
                ? pointer
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static void ReportFailureOnce(string message)
    {
        if (_reportedFailure)
            return;

        _reportedFailure = true;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] HUB1 NATIVE INTERACTION BLOCK FAILED: {message}.");
    }
}


internal static class Hub1EntranceLookup
{
    private const string Door6Path =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level02/Level02Door";
    private const string Trigger6Path =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level02/Level02Door/Mat/MatCollision";

    private const string Door7Path =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19/Level19Door";
    private const string Trigger7Path =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19/Level19Door/Mat/MatCollision";

    private const string Door9Path =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level20/Level20Door";
    private const string Trigger9Path =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level20/Level20Door/Mat/MatCollision";

    private const string Door17Path =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/ContainerWithHole-tofb/ShippingContainerWithHole/Level21Door";
    private const string Trigger17Path =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/ContainerWithHole-tofb/ShippingContainerWithHole/Level21Door/Mat/MatCollision";

    private static Transform? _door6;
    private static Transform? _trigger6;
    private static Transform? _door7;
    private static Transform? _trigger7;
    private static Transform? _door9;
    private static Transform? _trigger9;
    private static Transform? _door17;
    private static Transform? _trigger17;
    private static Transform? _playerMarker;

    private static int _earliestScanFrame;
    private static int _nextRetryFrame;
    private static bool _reportedReady;

    public static void ResetForSceneChange()
    {
        _door6 = null;
        _trigger6 = null;
        _door7 = null;
        _trigger7 = null;
        _door9 = null;
        _trigger9 = null;
        _door17 = null;
        _trigger17 = null;
        _playerMarker = null;

        _earliestScanFrame = Time.frameCount + 10;
        _nextRetryFrame = _earliestScanFrame;
        _reportedReady = false;
    }

    public static bool TryGetDoor(
        int level,
        out Transform? door,
        out Transform? trigger,
        out Transform? player)
    {
        door = null;
        trigger = null;
        player = null;

        EnsureScanned();

        switch (level)
        {
            case 6:
                door = Valid(_door6) ? _door6 : null;
                trigger = Valid(_trigger6) ? _trigger6 : null;
                break;

            case 7:
                door = Valid(_door7) ? _door7 : null;
                trigger = Valid(_trigger7) ? _trigger7 : null;
                break;

            case 9:
                door = Valid(_door9) ? _door9 : null;
                trigger = Valid(_trigger9) ? _trigger9 : null;
                break;

            case 17:
                door = Valid(_door17) ? _door17 : null;
                trigger = Valid(_trigger17) ? _trigger17 : null;
                break;
        }

        player =
            Valid(_playerMarker) &&
            SafeActive(_playerMarker)
                ? _playerMarker
                : null;

        return door != null &&
               trigger != null;
    }

    public static Transform? TryGetPlayer()
    {
        EnsureScanned();

        if (Valid(_playerMarker) &&
            SafeActive(_playerMarker))
        {
            return _playerMarker;
        }

        return null;
    }

    private static void EnsureScanned()
    {
        if (!DeveloperHarness.IsCurrentRoom("GameRoom_Hub1A"))
            return;

        if (AllCoreReferencesValid() &&
            Valid(_playerMarker) &&
            SafeActive(_playerMarker))
        {
            return;
        }

        int frame = Time.frameCount;

        if (frame < _earliestScanFrame ||
            frame < _nextRetryFrame)
        {
            return;
        }

        _nextRetryFrame = frame + 30;

        FindTargets();
    }

    private static void FindTargets()
    {
        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        try
        {
            foreach (Transform tr in
                Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (_door6 == null &&
                    string.Equals(path, Door6Path, StringComparison.Ordinal))
                    _door6 = tr;
                else if (_trigger6 == null &&
                         string.Equals(path, Trigger6Path, StringComparison.Ordinal))
                    _trigger6 = tr;
                else if (_door7 == null &&
                         string.Equals(path, Door7Path, StringComparison.Ordinal))
                    _door7 = tr;
                else if (_trigger7 == null &&
                         string.Equals(path, Trigger7Path, StringComparison.Ordinal))
                    _trigger7 = tr;
                else if (_door9 == null &&
                         string.Equals(path, Door9Path, StringComparison.Ordinal))
                    _door9 = tr;
                else if (_trigger9 == null &&
                         string.Equals(path, Trigger9Path, StringComparison.Ordinal))
                    _trigger9 = tr;
                else if (_door17 == null &&
                         string.Equals(path, Door17Path, StringComparison.Ordinal))
                    _door17 = tr;
                else if (_trigger17 == null &&
                         string.Equals(path, Trigger17Path, StringComparison.Ordinal))
                    _trigger17 = tr;

                if (tr.gameObject.activeInHierarchy)
                {
                    string name = tr.name ?? string.Empty;
                    string lowerName = name.ToLowerInvariant();
                    string lowerPath = path.ToLowerInvariant();

                    if (lowerPath.Contains(
                            "/spawnedentitycontainer/playercharacter",
                            StringComparison.Ordinal) &&
                        !lowerPath.Contains(
                            "/collisiondetectionslaves/",
                            StringComparison.Ordinal))
                    {
                        if (lowerName.StartsWith(
                                "playercharacter",
                                StringComparison.Ordinal))
                        {
                            exactPlayerRoot = tr;
                        }
                        else
                        {
                            fallbackPlayerChild ??= tr;
                        }
                    }
                }

                if (_door6 != null &&
                    _trigger6 != null &&
                    _door7 != null &&
                    _trigger7 != null &&
                    _door9 != null &&
                    _trigger9 != null &&
                    _door17 != null &&
                    _trigger17 != null &&
                    exactPlayerRoot != null)
                {
                    break;
                }
            }

            _playerMarker =
                exactPlayerRoot
                ?? fallbackPlayerChild
                ?? _playerMarker;

            if (!_reportedReady &&
                AllCoreReferencesValid() &&
                Valid(_playerMarker))
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] HUB1 SHARED LOOKUP READY: Level 6/7/9/17 doors, mats, and player marker resolved in one transform scan.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Hub1 shared lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private static bool AllCoreReferencesValid()
    {
        return Valid(_door6) &&
               Valid(_trigger6) &&
               Valid(_door7) &&
               Valid(_trigger7) &&
               Valid(_door9) &&
               Valid(_trigger9) &&
               Valid(_door17) &&
               Valid(_trigger17);
    }

    private static bool Valid(Transform? tr)
    {
        try
        {
            return tr != null &&
                   tr.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private static bool SafeActive(Transform tr)
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

            for (int i = 0; i < 40 && current != null; i++)
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


internal sealed class Level6EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level6";
    private const string NormalDoorPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level02/Level02Door";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level02/Level02Door/Mat/MatCollision";

    // Arm the global native false-patch before the normal interaction radius is
    // reached. It is restored immediately after leaving this small local area.
    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level6EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1A"))
        {
            SuspendOutsideHub1A();
            return;
        }

        // With access, leave the complete vanilla door/interaction pipeline
        // untouched.
        if (NativeProgression.HasLevel6Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        // Hysteresis: once armed, do not restore until a little farther away,
        // preventing rapid patch/unpatch churn at the radius edge.
        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Boring Room / Level 6");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 6 LOCK RANGE: player is {distance:0.###} from the Boring Room mat; native interaction is disabled until leaving the area or receiving Level 6 Access.");
    }

    private void SuspendOutsideHub1A()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub1EntranceLookup.TryGetDoor(
                    6,
                    out Transform? door,
                    out Transform? trigger,
                    out Transform? player))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;

            if (player != null)
                _playerMarker = player;

            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 6 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Hub1 shared lookup active; No Level02Door visual/collider objects are disabled.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 6 shared target lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub1EntranceLookup.TryGetPlayer();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 6 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level7EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level7";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19/Level19Door";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/Level19/Level19Door/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level7EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1A"))
        {
            SuspendOutsideHub1A();
            return;
        }

        if (NativeProgression.HasLevel7Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Demolition Training / Level 7 / Level_19");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 7 LOCK RANGE: player is {distance:0.###} from the Demolition Training mat; native interaction is disabled until leaving the area or receiving Level 7 Access.");
    }

    private void SuspendOutsideHub1A()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub1EntranceLookup.TryGetDoor(
                    7,
                    out Transform? door,
                    out Transform? trigger,
                    out Transform? player))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;

            if (player != null)
                _playerMarker = player;

            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 7 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Hub1 shared lookup active; Demolition Certificate and Level21/Cold Storage hierarchy remain vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 7 shared target lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub1EntranceLookup.TryGetPlayer();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 7 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
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


internal sealed class Level8EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level8";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub1B_Logic/Objects/Level11/LevelEntranceDoor_11";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1B_Logic/Objects/Level11/LevelEntranceDoor_11/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level8EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1B"))
        {
            SuspendOutsideHub1B();
            return;
        }

        if (NativeProgression.HasLevel8Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Minim Tower / Level 8 / Level_11");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 8 LOCK RANGE: player is {distance:0.###} from the Minim Tower mat; native interaction is disabled until leaving the area or receiving Level 8 Access.");
    }

    private void SuspendOutsideHub1B()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? door = null;
        Transform? trigger = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (door == null &&
                    string.Equals(path, NormalDoorPath, StringComparison.Ordinal))
                {
                    door = tr;
                }

                if (trigger == null &&
                    string.Equals(path, TriggerPointPath, StringComparison.Ordinal))
                {
                    trigger = tr;
                }

                if (door != null && trigger != null)
                    break;
            }

            if (door == null || trigger == null)
                return;

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 8 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Level11Door_DevilMode is untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 8 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                if (!tr.gameObject.activeInHierarchy)
                    continue;

                string name = tr.name ?? string.Empty;
                string path = BuildHierarchy(tr);
                string lowerName = name.ToLowerInvariant();
                string lowerPath = path.ToLowerInvariant();

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
                    exactPlayerRoot = tr;
                    break;
                }

                fallbackPlayerChild ??= tr;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 8 player-marker search failed: {ex.GetBaseException().Message}");
            return;
        }

        Transform? chosen = exactPlayerRoot ?? fallbackPlayerChild;

        if (chosen == null)
            return;

        _playerMarker = chosen;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    chosen.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 8 player marker='{BuildHierarchy(chosen)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level9EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level9";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level20/Level20Door";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1_Logic/Hub1A_Logic_UpperPath/Objects/Doors/Level20/Level20Door/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level9EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1A"))
        {
            SuspendOutsideHub1A();
            return;
        }

        if (NativeProgression.HasLevel9Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "School Trip / Level 9 / Level_20");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 9 LOCK RANGE: player is {distance:0.###} from the School Trip mat; native interaction is disabled until leaving the area or receiving Level 9 Access.");
    }

    private void SuspendOutsideHub1A()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub1EntranceLookup.TryGetDoor(
                    9,
                    out Transform? door,
                    out Transform? trigger,
                    out Transform? player))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;

            if (player != null)
                _playerMarker = player;

            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 9 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Hub1 shared lookup active; Level20/LevelLockedCover remains vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 9 shared target lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub1EntranceLookup.TryGetPlayer();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 9 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

        private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}




internal sealed class Level17EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level17";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/ContainerWithHole-tofb/ShippingContainerWithHole/Level21Door";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1_Logic/Hub1a_Logic_BottomLeft/Objects/TrainingCentre: Levels 19&21/ContainerWithHole-tofb/ShippingContainerWithHole/Level21Door/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level17EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1A"))
        {
            SuspendOutsideHub1A();
            return;
        }

        if (NativeProgression.HasLevel17Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Cold Storage / Level 17 / Level_21");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 17 LOCK RANGE: player is {distance:0.###} from the Cold Storage mat; native interaction is disabled until leaving the area or receiving Level 17 Access.");
    }

    private void SuspendOutsideHub1A()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub1EntranceLookup.TryGetDoor(
                    17,
                    out Transform? door,
                    out Transform? trigger,
                    out Transform? player))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;

            if (player != null)
                _playerMarker = player;

            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 17 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Hub1 shared lookup active; Violance/VIOLIN_ABILITY, Hypno Pan usage, Old Tom/Thief Prince NPCs, and heist progression remain vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 17 shared target lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub1EntranceLookup.TryGetPlayer();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 17 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level10EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level10";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub1B_Logic/Objects/LevelEntranceDoor_01";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub1B_Logic/Objects/LevelEntranceDoor_01/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level10EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub1B"))
        {
            SuspendOutsideHub1B();
            return;
        }

        if (NativeProgression.HasLevel10Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "The Vault / Level 10 / Level_01");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 10 LOCK RANGE: player is {distance:0.###} from The Vault mat; native interaction is disabled until leaving the area or receiving Level 11 Access.");
    }

    private void SuspendOutsideHub1B()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? door = null;
        Transform? trigger = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (door == null &&
                    string.Equals(path, NormalDoorPath, StringComparison.Ordinal))
                {
                    door = tr;
                }

                if (trigger == null &&
                    string.Equals(path, TriggerPointPath, StringComparison.Ordinal))
                {
                    trigger = tr;
                }

                if (door != null && trigger != null)
                    break;
            }

            if (door == null || trigger == null)
                return;

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 10 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 10 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 10 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (tr == null || tr.gameObject == null || !tr.gameObject.activeInHierarchy)
                continue;

            string name = tr.name ?? string.Empty;
            string path = BuildHierarchy(tr);
            string lowerName = name.ToLowerInvariant();
            string lowerPath = path.ToLowerInvariant();

            if (!lowerPath.Contains(
                    "/spawnedentitycontainer/playercharacter",
                    StringComparison.Ordinal))
                continue;

            if (lowerPath.Contains(
                    "/collisiondetectionslaves/",
                    StringComparison.Ordinal))
                continue;

            if (lowerName.StartsWith(
                    "playercharacter",
                    StringComparison.Ordinal))
            {
                exactPlayerRoot = tr;
                break;
            }

            fallbackPlayerChild ??= tr;
        }

        return exactPlayerRoot ?? fallbackPlayerChild;
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level15EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level15";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub5A_Logic/Objects/Level16Door";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub5A_Logic/Objects/Level16Door/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level15EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub5A"))
        {
            SuspendOutsideHub5A();
            return;
        }

        if (NativeProgression.HasLevel15Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Central Mainframe / Level 15 / Level_16");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 15 LOCK RANGE: player is {distance:0.###} from the Central Mainframe mat; native interaction is disabled until leaving the area or receiving Level 15 Access.");
    }

    private void SuspendOutsideHub5A()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? door = null;
        Transform? trigger = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (door == null &&
                    string.Equals(path, NormalDoorPath, StringComparison.Ordinal))
                {
                    door = tr;
                }

                if (trigger == null &&
                    string.Equals(path, TriggerPointPath, StringComparison.Ordinal))
                {
                    trigger = tr;
                }

                if (door != null && trigger != null)
                    break;
            }

            if (door == null || trigger == null)
                return;

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 15 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Cell Tower intro, ExternalDoor/HR5B gate, Level16AttemptedCondition, EntranceDoor, and FutureLevelEntranceDoor are untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 15 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 15 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (tr == null || tr.gameObject == null || !tr.gameObject.activeInHierarchy)
                continue;

            string name = tr.name ?? string.Empty;
            string path = BuildHierarchy(tr);
            string lowerName = name.ToLowerInvariant();
            string lowerPath = path.ToLowerInvariant();

            if (!lowerPath.Contains(
                    "/spawnedentitycontainer/playercharacter",
                    StringComparison.Ordinal))
                continue;

            if (lowerPath.Contains(
                    "/collisiondetectionslaves/",
                    StringComparison.Ordinal))
                continue;

            if (lowerName.StartsWith(
                    "playercharacter",
                    StringComparison.Ordinal))
            {
                exactPlayerRoot = tr;
                break;
            }

            fallbackPlayerChild ??= tr;
        }

        return exactPlayerRoot ?? fallbackPlayerChild;
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level16EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level16";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub5B_Logic/Objects/Doors/LevelEntranceDoor";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub5B_Logic/Objects/Doors/LevelEntranceDoor/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level16EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub5B"))
        {
            SuspendOutsideHub5B();
            return;
        }

        if (NativeProgression.HasLevel16Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Thief Prince / Level 16 / Level_24");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 16 LOCK RANGE: player is {distance:0.###} from the Thief Prince mat; native interaction is disabled until leaving the area or receiving Level 16 Access.");
    }

    private void SuspendOutsideHub5B()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? door = null;
        Transform? trigger = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (door == null &&
                    string.Equals(path, NormalDoorPath, StringComparison.Ordinal))
                {
                    door = tr;
                }

                if (trigger == null &&
                    string.Equals(path, TriggerPointPath, StringComparison.Ordinal))
                {
                    trigger = tr;
                }

                if (door != null && trigger != null)
                    break;
            }

            if (door == null || trigger == null)
                return;

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 16 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Bizzle, Clive, Bee Modes, Super Nectar, Prince notes/conditions, and all other Cell Tower progression are untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 16 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 16 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (tr == null || tr.gameObject == null || !tr.gameObject.activeInHierarchy)
                continue;

            string name = tr.name ?? string.Empty;
            string path = BuildHierarchy(tr);
            string lowerName = name.ToLowerInvariant();
            string lowerPath = path.ToLowerInvariant();

            if (!lowerPath.Contains(
                    "/spawnedentitycontainer/playercharacter",
                    StringComparison.Ordinal))
                continue;

            if (lowerPath.Contains(
                    "/collisiondetectionslaves/",
                    StringComparison.Ordinal))
                continue;

            if (lowerName.StartsWith(
                    "playercharacter",
                    StringComparison.Ordinal))
            {
                exactPlayerRoot = tr;
                break;
            }

            fallbackPlayerChild ??= tr;
        }

        return exactPlayerRoot ?? fallbackPlayerChild;
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal static class Hub3EntranceLookup
{
    private const string Door18Path =
        "Root/GameRoom_Hub3_Logic/Objects/Darkness/DarknessLevelDoor/LevelEntranceDoor";
    private const string Trigger18Path =
        "Root/GameRoom_Hub3_Logic/Objects/Darkness/DarknessLevelDoor/LevelEntranceDoor/Mat/MatCollision";

    private const string Door19Path =
        "Root/GameRoom_Hub3_Logic/Objects/Complexity/ComplexityLevelDoor/LevelEntranceDoor";
    private const string Trigger19Path =
        "Root/GameRoom_Hub3_Logic/Objects/Complexity/ComplexityLevelDoor/LevelEntranceDoor/Mat/MatCollision";

    private const string Door20Path =
        "Root/GameRoom_Hub3_Logic/Objects/Loneliness/LevelEntranceDoor";
    private const string Trigger20Path =
        "Root/GameRoom_Hub3_Logic/Objects/Loneliness/LevelEntranceDoor/Mat/MatCollision";

    private static Transform? _door18;
    private static Transform? _trigger18;
    private static Transform? _door19;
    private static Transform? _trigger19;
    private static Transform? _door20;
    private static Transform? _trigger20;
    private static Transform? _playerMarker;

    private static int _nextScanFrame;
    private static bool _reportedReady;

    public static void ResetForSceneChange()
    {
        _door18 = null;
        _trigger18 = null;
        _door19 = null;
        _trigger19 = null;
        _door20 = null;
        _trigger20 = null;
        _playerMarker = null;

        _nextScanFrame = Time.frameCount + 12;
        _reportedReady = false;
    }

    public static bool TryGetDoor(
        int userFacingLevel,
        out Transform? door,
        out Transform? trigger)
    {
        EnsureScan();

        door = null;
        trigger = null;

        switch (userFacingLevel)
        {
            case 18:
                door = Valid(_door18) ? _door18 : null;
                trigger = Valid(_trigger18) ? _trigger18 : null;
                break;

            case 19:
                door = Valid(_door19) ? _door19 : null;
                trigger = Valid(_trigger19) ? _trigger19 : null;
                break;

            case 20:
                door = Valid(_door20) ? _door20 : null;
                trigger = Valid(_trigger20) ? _trigger20 : null;
                break;
        }

        return door != null &&
               trigger != null;
    }

    public static Transform? GetPlayerMarker()
    {
        EnsureScan();

        return Valid(_playerMarker)
            ? _playerMarker
            : null;
    }

    private static void EnsureScan()
    {
        if (!DeveloperHarness.IsCurrentRoom("GameRoom_Hub3"))
            return;

        if (AllCoreReferencesValid())
            return;

        if (Time.frameCount < _nextScanFrame)
            return;

        _nextScanFrame =
            Time.frameCount + 30;

        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

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

                if (_door18 == null &&
                    string.Equals(
                        path,
                        Door18Path,
                        StringComparison.Ordinal))
                {
                    _door18 = tr;
                }
                else if (_trigger18 == null &&
                         string.Equals(
                             path,
                             Trigger18Path,
                             StringComparison.Ordinal))
                {
                    _trigger18 = tr;
                }
                else if (_door19 == null &&
                         string.Equals(
                             path,
                             Door19Path,
                             StringComparison.Ordinal))
                {
                    _door19 = tr;
                }
                else if (_trigger19 == null &&
                         string.Equals(
                             path,
                             Trigger19Path,
                             StringComparison.Ordinal))
                {
                    _trigger19 = tr;
                }
                else if (_door20 == null &&
                         string.Equals(
                             path,
                             Door20Path,
                             StringComparison.Ordinal))
                {
                    _door20 = tr;
                }
                else if (_trigger20 == null &&
                         string.Equals(
                             path,
                             Trigger20Path,
                             StringComparison.Ordinal))
                {
                    _trigger20 = tr;
                }

                if (tr.gameObject.activeInHierarchy)
                {
                    string lowerPath =
                        path.ToLowerInvariant();

                    if (lowerPath.Contains(
                            "/spawnedentitycontainer/playercharacter",
                            StringComparison.Ordinal) &&
                        !lowerPath.Contains(
                            "/collisiondetectionslaves/",
                            StringComparison.Ordinal))
                    {
                        string lowerName =
                            (tr.name ?? string.Empty).ToLowerInvariant();

                        if (lowerName.StartsWith(
                                "playercharacter",
                                StringComparison.Ordinal))
                        {
                            exactPlayerRoot = tr;
                        }
                        else
                        {
                            fallbackPlayerChild ??= tr;
                        }
                    }
                }

                if (_door18 != null &&
                    _trigger18 != null &&
                    _door19 != null &&
                    _trigger19 != null &&
                    _door20 != null &&
                    _trigger20 != null &&
                    exactPlayerRoot != null)
                {
                    break;
                }
            }

            _playerMarker =
                exactPlayerRoot
                ?? fallbackPlayerChild
                ?? _playerMarker;

            if (!_reportedReady &&
                AllCoreReferencesValid())
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] HUB3 SHARED LOOKUP READY: Level 18/19/20 normal doors, mats, and player marker resolved in one transform scan. Escape LevelEntranceDoor_DevilMode is untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Hub3 shared lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private static bool AllCoreReferencesValid()
    {
        return Valid(_door18) &&
               Valid(_trigger18) &&
               Valid(_door19) &&
               Valid(_trigger19) &&
               Valid(_door20) &&
               Valid(_trigger20) &&
               Valid(_playerMarker);
    }

    private static bool Valid(Transform? tr)
    {
        try
        {
            return tr != null &&
                   tr.gameObject != null;
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


internal sealed class Level18EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level18";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub3_Logic/Objects/Darkness/DarknessLevelDoor/LevelEntranceDoor";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub3_Logic/Objects/Darkness/DarknessLevelDoor/LevelEntranceDoor/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level18EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub3"))
        {
            SuspendOutsideHub3();
            return;
        }

        if (NativeProgression.HasLevel18Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "The Darkness / Level 18 / Level_03");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 18 LOCK RANGE: player is {distance:0.###} from the The Darkness mat; native interaction is disabled until leaving the area or receiving Level 18 Access.");
    }

    private void SuspendOutsideHub3()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub3EntranceLookup.TryGetDoor(
                    18,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 18 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. Weed Killer/totem/Eye progression remains authoritative and vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 18 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub3EntranceLookup.GetPlayerMarker();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 18 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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


internal sealed class Level19EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level19";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub3_Logic/Objects/Complexity/ComplexityLevelDoor/LevelEntranceDoor";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub3_Logic/Objects/Complexity/ComplexityLevelDoor/LevelEntranceDoor/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level19EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub3"))
        {
            SuspendOutsideHub3();
            return;
        }

        if (NativeProgression.HasLevel19Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Escape / Level 19 / Level_13");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 19 LOCK RANGE: player is {distance:0.###} from the Escape mat; native interaction is disabled until leaving the area or receiving Level 19 Access.");
    }

    private void SuspendOutsideHub3()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub3EntranceLookup.TryGetDoor(
                    19,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 19 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. LevelEntranceDoor_DevilMode is explicitly untouched; Violance/totem/Brain progression remains vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 19 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub3EntranceLookup.GetPlayerMarker();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 19 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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


internal sealed class Level20EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level20";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub3_Logic/Objects/Loneliness/LevelEntranceDoor";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub3_Logic/Objects/Loneliness/LevelEntranceDoor/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level20EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub3"))
        {
            SuspendOutsideHub3();
            return;
        }

        if (NativeProgression.HasLevel20Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Loneliness / Level 20 / Level_25");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 20 LOCK RANGE: player is {distance:0.###} from the Loneliness mat; native interaction is disabled until leaving the area or receiving Level 20 Access.");
    }

    private void SuspendOutsideHub3()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub3EntranceLookup.TryGetDoor(
                    20,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 20 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. Hypno Pan/totem/Heart progression remains authoritative and vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 20 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub3EntranceLookup.GetPlayerMarker();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 20 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static string BuildHierarchy(Transform tr)
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


internal static class Hub7EntranceLookup
{
    private const string Door21Path =
        "Root/GameRoom_Hub7_Logic/Objects/Level14Doors/LevelEntranceDoor_14";

    private const string Trigger21Path =
        "Root/GameRoom_Hub7_Logic/Objects/Level14Doors/LevelEntranceDoor_14/Mat/MatCollision";

    private const string Door22Path =
        "Root/GameRoom_Hub7_Logic/Objects/Doors/LevelEntranceDoor_28_default";

    private const string Trigger22Path =
        "Root/GameRoom_Hub7_Logic/Objects/Doors/LevelEntranceDoor_28_default/Mat/MatCollision";

    private static Transform? _door21;
    private static Transform? _trigger21;
    private static Transform? _door22;
    private static Transform? _trigger22;
    private static Transform? _playerMarker;

    private static int _nextScanFrame;
    private static bool _reportedReady;

    public static void ResetForSceneChange()
    {
        _door21 = null;
        _trigger21 = null;
        _door22 = null;
        _trigger22 = null;
        _playerMarker = null;

        _nextScanFrame =
            Time.frameCount + 12;

        _reportedReady = false;
    }

    public static bool TryGetLevel21(
        out Transform? door,
        out Transform? trigger)
    {
        EnsureScan();

        door =
            Valid(_door21)
                ? _door21
                : null;

        trigger =
            Valid(_trigger21)
                ? _trigger21
                : null;

        return door != null &&
               trigger != null;
    }

    public static bool TryGetLevel22(
        out Transform? door,
        out Transform? trigger)
    {
        EnsureScan();

        door =
            Valid(_door22)
                ? _door22
                : null;

        trigger =
            Valid(_trigger22)
                ? _trigger22
                : null;

        return door != null &&
               trigger != null;
    }

    public static Transform? GetPlayerMarker()
    {
        EnsureScan();

        return Valid(_playerMarker)
            ? _playerMarker
            : null;
    }

    private static void EnsureScan()
    {
        if (!DeveloperHarness.IsCurrentRoom("GameRoom_Hub7"))
            return;

        if (AllCoreReferencesValid())
            return;

        if (Time.frameCount < _nextScanFrame)
            return;

        _nextScanFrame =
            Time.frameCount + 30;

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
                    tr.gameObject == null)
                {
                    continue;
                }

                string path =
                    BuildHierarchy(tr);

                if (_door21 == null &&
                    string.Equals(
                        path,
                        Door21Path,
                        StringComparison.Ordinal))
                {
                    _door21 =
                        tr;
                }
                else if (_trigger21 == null &&
                         string.Equals(
                             path,
                             Trigger21Path,
                             StringComparison.Ordinal))
                {
                    _trigger21 =
                        tr;
                }
                else if (_door22 == null &&
                         string.Equals(
                             path,
                             Door22Path,
                             StringComparison.Ordinal))
                {
                    _door22 =
                        tr;
                }
                else if (_trigger22 == null &&
                         string.Equals(
                             path,
                             Trigger22Path,
                             StringComparison.Ordinal))
                {
                    _trigger22 =
                        tr;
                }

                if (tr.gameObject.activeInHierarchy)
                {
                    string lowerPath =
                        path.ToLowerInvariant();

                    if (lowerPath.Contains(
                            "/spawnedentitycontainer/playercharacter",
                            StringComparison.Ordinal) &&
                        !lowerPath.Contains(
                            "/collisiondetectionslaves/",
                            StringComparison.Ordinal))
                    {
                        string lowerName =
                            (tr.name ?? string.Empty).ToLowerInvariant();

                        if (lowerName.StartsWith(
                                "playercharacter",
                                StringComparison.Ordinal))
                        {
                            exactPlayerRoot =
                                tr;
                        }
                        else
                        {
                            fallbackPlayerChild ??=
                                tr;
                        }
                    }
                }

                if (_door21 != null &&
                    _trigger21 != null &&
                    _door22 != null &&
                    _trigger22 != null &&
                    exactPlayerRoot != null)
                {
                    break;
                }
            }

            _playerMarker =
                exactPlayerRoot
                ?? fallbackPlayerChild
                ?? _playerMarker;

            if (!_reportedReady &&
                AllCoreReferencesValid())
            {
                _reportedReady =
                    true;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] HUB7 SHARED LOOKUP READY: Level 21 Locker Room and Level 22 King Ferdinand I normal doors/mats plus player marker resolved in one scan. Locker Room LevelEntranceDoor_14_DevilMode remains untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Hub7 shared lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private static bool AllCoreReferencesValid()
    {
        return Valid(_door21) &&
               Valid(_trigger21) &&
               Valid(_door22) &&
               Valid(_trigger22) &&
               Valid(_playerMarker);
    }

    private static bool Valid(
        Transform? tr)
    {
        try
        {
            return tr != null &&
                   tr.gameObject != null;
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


internal sealed class Level21EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner =
        "Level21";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub7_Logic/Objects/Level14Doors/LevelEntranceDoor_14";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub7_Logic/Objects/Level14Doors/LevelEntranceDoor_14/Mat/MatCollision";

    private const float NativeBlockRadius =
        3.00f;

    private const float NativeReleaseRadius =
        3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level21EntranceKeeper(
        IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub7"))
        {
            SuspendOutsideHub7();
            return;
        }

        if (NativeProgression.HasLevel21Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            _reportedInRange =
                false;

            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown =
                20;

            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown =
                20;

            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance =
                Vector3.Distance(
                    _playerMarker!.position,
                    _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            _playerMarker =
                null;

            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(
                    NativeBlockOwner);

                _reportedInRange =
                    false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Locker Room / Level 21 / Level_14");

        if (!disabled)
            return;

        _reportedInRange =
            true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 21 LOCK RANGE: player is {distance:0.###} from the Locker Room mat; native interaction is disabled until leaving the area or receiving Level 21 Access.");
    }

    private void SuspendOutsideHub7()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);

        _door =
            null;

        _triggerPoint =
            null;

        _playerMarker =
            null;

        _targetSearchCooldown =
            0;

        _playerSearchCooldown =
            0;

        _reportedReady =
            false;

        _reportedPlayer =
            false;

        _reportedInRange =
            false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub7EntranceLookup.TryGetLevel21(
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door =
                door;

            _triggerPoint =
                trigger;

            _playerMarker =
                null;

            _reportedPlayer =
                false;

            _reportedInRange =
                false;

            if (!_reportedReady)
            {
                _reportedReady =
                    true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 21 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Royal Corridor intro remains vanilla and LevelEntranceDoor_14_DevilMode is explicitly untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 21 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub7EntranceLookup.GetPlayerMarker();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer =
                true;

            float distance =
                -1f;

            try
            {
                distance =
                    Vector3.Distance(
                        _playerMarker.position,
                        _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 21 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
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




internal sealed class Level22EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner =
        "Level22";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub7_Logic/Objects/Doors/LevelEntranceDoor_28_default";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub7_Logic/Objects/Doors/LevelEntranceDoor_28_default/Mat/MatCollision";

    private const float NativeBlockRadius =
        3.00f;

    private const float NativeReleaseRadius =
        3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level22EntranceKeeper(
        IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub7"))
        {
            SuspendOutsideHub7();
            return;
        }

        if (NativeProgression.HasLevel22Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            _reportedInRange =
                false;

            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown =
                20;

            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown =
                20;

            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance =
                Vector3.Distance(
                    _playerMarker!.position,
                    _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            _playerMarker =
                null;

            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(
                    NativeBlockOwner);

                _reportedInRange =
                    false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(
                NativeBlockOwner);

            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "King Ferdinand I / Level 22 / Level_28");

        if (!disabled)
            return;

        _reportedInRange =
            true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 22 LOCK RANGE: player is {distance:0.###} from the King Ferdinand I mat; native interaction is disabled until leaving the area or receiving Level 22 Access.");
    }

    private void SuspendOutsideHub7()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);

        _door =
            null;

        _triggerPoint =
            null;

        _playerMarker =
            null;

        _targetSearchCooldown =
            0;

        _playerSearchCooldown =
            0;

        _reportedReady =
            false;

        _reportedPlayer =
            false;

        _reportedInRange =
            false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(
            NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub7EntranceLookup.TryGetLevel22(
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door =
                door;

            _triggerPoint =
                trigger;

            _playerMarker =
                null;

            _reportedPlayer =
                false;

            _reportedInRange =
                false;

            if (!_reportedReady)
            {
                _reportedReady =
                    true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 22 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Star Eater bridge, fridge/ice sequence, king disappearance cutscene, and Royal Corridor story progression remain vanilla.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 22 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker =
            Hub7EntranceLookup.GetPlayerMarker();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer =
                true;

            float distance =
                -1f;

            try
            {
                distance =
                    Vector3.Distance(
                        _playerMarker.position,
                        _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 22 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
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


internal static class Hub4EntranceLookup
{
    private const string Door11Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_01";

    private const string Trigger11Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_01/Mat/MatCollision";

    private const string Door12Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_02";

    private const string Trigger12Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_02/Mat/MatCollision";

    private const string Door13Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_03";

    private const string Trigger13Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_03/Mat/MatCollision";

    private const string Door14Path =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_04";

    private static Transform? _door11;
    private static Transform? _trigger11;
    private static Transform? _door12;
    private static Transform? _trigger12;
    private static Transform? _door13;
    private static Transform? _trigger13;
    private static Transform? _door14;
    private static Transform? _trigger14;
    private static Transform? _playerMarker;

    private static int _nextScanFrame;
    private static bool _reportedReady;

    public static void ResetForSceneChange()
    {
        _door11 = null;
        _trigger11 = null;
        _door12 = null;
        _trigger12 = null;
        _door13 = null;
        _trigger13 = null;
        _door14 = null;
        _trigger14 = null;
        _playerMarker = null;

        // Give the destination scene a few frames to finish activating before
        // doing the one shared transform enumeration.
        _nextScanFrame = Time.frameCount + 12;
        _reportedReady = false;
    }

    public static bool TryGetDoor(
        int userFacingLevel,
        out Transform? door,
        out Transform? trigger)
    {
        EnsureScan();

        door = null;
        trigger = null;

        switch (userFacingLevel)
        {
            case 11:
                door = Valid(_door11) ? _door11 : null;
                trigger = Valid(_trigger11) ? _trigger11 : null;
                break;

            case 12:
                door = Valid(_door12) ? _door12 : null;
                trigger = Valid(_trigger12) ? _trigger12 : null;
                break;

            case 13:
                door = Valid(_door13) ? _door13 : null;
                trigger = Valid(_trigger13) ? _trigger13 : null;
                break;

            case 14:
                door = Valid(_door14) ? _door14 : null;
                trigger = Valid(_trigger14) ? _trigger14 : null;
                break;
        }

        return door != null && trigger != null;
    }

    public static Transform? GetPlayerMarker()
    {
        EnsureScan();

        return Valid(_playerMarker)
            ? _playerMarker
            : null;
    }

    private static void EnsureScan()
    {
        if (AllCoreReferencesValid())
            return;

        if (Time.frameCount < _nextScanFrame)
            return;

        // If the scene is still coming online, one keeper may ask again later.
        // All Hub4 keepers share this same retry timer so they cannot each run
        // their own expensive scan on adjacent frames.
        _nextScanFrame = Time.frameCount + 30;

        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (_door11 == null &&
                    string.Equals(path, Door11Path, StringComparison.Ordinal))
                {
                    _door11 = tr;
                }
                else if (_trigger11 == null &&
                         string.Equals(path, Trigger11Path, StringComparison.Ordinal))
                {
                    _trigger11 = tr;
                }
                else if (_door12 == null &&
                         string.Equals(path, Door12Path, StringComparison.Ordinal))
                {
                    _door12 = tr;
                }
                else if (_trigger12 == null &&
                         string.Equals(path, Trigger12Path, StringComparison.Ordinal))
                {
                    _trigger12 = tr;
                }
                else if (_door13 == null &&
                         string.Equals(path, Door13Path, StringComparison.Ordinal))
                {
                    _door13 = tr;
                }
                else if (_trigger13 == null &&
                         string.Equals(path, Trigger13Path, StringComparison.Ordinal))
                {
                    _trigger13 = tr;
                }
                else if (_door14 == null &&
                         string.Equals(path, Door14Path, StringComparison.Ordinal))
                {
                    _door14 = tr;
                }

                if (tr.gameObject.activeInHierarchy)
                {
                    string lowerPath = path.ToLowerInvariant();

                    if (lowerPath.Contains(
                            "/spawnedentitycontainer/playercharacter",
                            StringComparison.Ordinal) &&
                        !lowerPath.Contains(
                            "/collisiondetectionslaves/",
                            StringComparison.Ordinal))
                    {
                        string lowerName =
                            (tr.name ?? string.Empty).ToLowerInvariant();

                        if (lowerName.StartsWith(
                                "playercharacter",
                                StringComparison.Ordinal))
                        {
                            exactPlayerRoot = tr;
                        }
                        else
                        {
                            fallbackPlayerChild ??= tr;
                        }
                    }
                }

                if (_door11 != null &&
                    _trigger11 != null &&
                    _door12 != null &&
                    _trigger12 != null &&
                    _door13 != null &&
                    _trigger13 != null &&
                    _door14 != null &&
                    exactPlayerRoot != null)
                {
                    break;
                }
            }

            _playerMarker =
                exactPlayerRoot
                ?? fallbackPlayerChild
                ?? _playerMarker;

            if (_door14 != null && _trigger14 == null)
            {
                try
                {
                    foreach (Transform child in
                        _door14.GetComponentsInChildren<Transform>(true))
                    {
                        if (child == null || child.gameObject == null)
                            continue;

                        if (string.Equals(
                                child.name,
                                "MatCollision",
                                StringComparison.Ordinal))
                        {
                            _trigger14 = child;
                            break;
                        }
                    }
                }
                catch
                {
                }
            }

            if (!_reportedReady &&
                _door11 != null &&
                _trigger11 != null &&
                _door12 != null &&
                _trigger12 != null &&
                _door13 != null &&
                _trigger13 != null &&
                _door14 != null &&
                _trigger14 != null &&
                _playerMarker != null)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogInfo(
                    "[SCRC-AP] HUB4 SHARED LOOKUP READY: Level 11/12/13/14 doors, mats, and player marker resolved in one shared transform scan.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Hub4 shared lookup failed: {ex.GetBaseException().Message}");
        }
    }

    private static bool AllCoreReferencesValid()
    {
        return Valid(_door11) &&
               Valid(_trigger11) &&
               Valid(_door12) &&
               Valid(_trigger12) &&
               Valid(_door13) &&
               Valid(_trigger13) &&
               Valid(_door14) &&
               Valid(_trigger14) &&
               Valid(_playerMarker);
    }

    private static bool Valid(Transform? tr)
    {
        try
        {
            return tr != null &&
                   tr.gameObject != null;
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level11EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level11";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_01";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_01/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level11EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub4"))
        {
            SuspendOutsideHub4();
            return;
        }

        if (NativeProgression.HasLevel11Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Act 1: Flavor / Level 11 / Level_12");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 11 LOCK RANGE: player is {distance:0.###} from the Act 1: Flavor mat; native interaction is disabled until leaving the area or receiving Level 11 Access.");
    }

    private void SuspendOutsideHub4()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub4EntranceLookup.TryGetDoor(
                    11,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 11 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Vanilla CanEnterCondition/music progression remains authoritative; Bee Mode is untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 11 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 11 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        return Hub4EntranceLookup.GetPlayerMarker();
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level12EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level12";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_02";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_02/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level12EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub4"))
        {
            SuspendOutsideHub4();
            return;
        }

        if (NativeProgression.HasLevel12Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Act 2: Sauce and Spice / Level 12 / Level_15");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 12 LOCK RANGE: player is {distance:0.###} from the Act 2: Sauce and Spice mat; native interaction is disabled until leaving the area or receiving Level 12 Access.");
    }

    private void SuspendOutsideHub4()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub4EntranceLookup.TryGetDoor(
                    12,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 12 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Vanilla CanEnterCondition, Hypno Pan/music/cat/bouncer progression remains authoritative; other Hub4 doors are untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 12 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 12 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        return Hub4EntranceLookup.GetPlayerMarker();
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level13EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level13";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_03";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_03/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level13EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub4"))
        {
            SuspendOutsideHub4();
            return;
        }

        if (NativeProgression.HasLevel13Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Act 3: Montage / Level 13 / Level_22");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 13 LOCK RANGE: player is {distance:0.###} from the Act 3: Montage mat; native interaction is disabled until leaving the area or receiving Level 13 Access.");
    }

    private void SuspendOutsideHub4()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub4EntranceLookup.TryGetDoor(
                    13,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 13 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Vanilla Act 3 music/Scruffy/bouncer progression remains authoritative; PlaceholderLevelDoor_03 and other Hub4 doors are untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 13 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 13 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        return Hub4EntranceLookup.GetPlayerMarker();
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class Level14EntranceKeeper : MonoBehaviour
{
    private const string NativeBlockOwner = "Level14";

    private const string NormalDoorPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_04";

    private const string TriggerPointPath =
        "Root/GameRoom_Hub4_Logic/Objects/Doors/LevelDoors/LevelEntranceDoor_04/Mat/MatCollision";

    private const float NativeBlockRadius = 3.00f;
    private const float NativeReleaseRadius = 3.50f;

    private Transform? _door;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _targetSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    public Level14EntranceKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!NativeProgression.RandomizeEarlyProgression ||
            !DeveloperHarness.IsCurrentRoom("GameRoom_Hub4"))
        {
            SuspendOutsideHub4();
            return;
        }

        if (NativeProgression.HasLevel14Access)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _reportedInRange = false;
            return;
        }

        if (!TargetsValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_targetSearchCooldown > 0)
            {
                _targetSearchCooldown--;
                return;
            }

            _targetSearchCooldown = 20;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                _triggerPoint!.position);
        }
        catch
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            _playerMarker = null;
            return;
        }

        if (_reportedInRange)
        {
            if (distance > NativeReleaseRadius)
            {
                NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
                _reportedInRange = false;
            }

            return;
        }

        if (distance > NativeBlockRadius)
        {
            NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
            return;
        }

        bool disabled =
            NativeLevel6DoorBridge.RequestBlock(
                NativeBlockOwner,
                _door!.gameObject,
                "Act 4: Habanero / Level 14 / Level_23");

        if (!disabled)
            return;

        _reportedInRange = true;

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] LEVEL 14 LOCK RANGE: player is {distance:0.###} from the Act 4: Habanero mat; native interaction is disabled until leaving the area or receiving Level 14 Access.");
    }

    private void SuspendOutsideHub4()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);

        _door = null;
        _triggerPoint = null;
        _playerMarker = null;

        _targetSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;
    }

    private void OnDisable()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private void OnDestroy()
    {
        NativeLevel6DoorBridge.ReleaseBlock(NativeBlockOwner);
    }

    private bool TargetsValid()
    {
        try
        {
            return _door != null &&
                   _door.gameObject != null &&
                   _door.gameObject.activeInHierarchy &&
                   _triggerPoint != null &&
                   _triggerPoint.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        try
        {
            if (!Hub4EntranceLookup.TryGetDoor(
                    14,
                    out Transform? door,
                    out Transform? trigger))
            {
                return;
            }

            _door = door;
            _triggerPoint = trigger;
            _playerMarker = null;
            _reportedPlayer = false;
            _reportedInRange = false;

            if (!_reportedReady)
            {
                _reportedReady = true;

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 14 NATIVE BLOCK READY: door='{NormalDoorPath}', " +
                    $"trigger='{TriggerPointPath}', blockRadius={NativeBlockRadius:0.##}, releaseRadius={NativeReleaseRadius:0.##}. " +
                    $"Vanilla Act 4 music/mouse-revolution/bouncer progression remains authoritative; Hypno Pan mouse escort, chilli crowd, PlaceholderLevelDoor_04, and other Hub4 doors are untouched.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 13 target search failed: {ex.GetBaseException().Message}");
        }
    }

    private void FindPlayerMarker()
    {
        _playerMarker = FindPlayerMarkerShared();

        if (_playerMarker == null)
            return;

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;

            float distance = -1f;

            try
            {
                distance = Vector3.Distance(
                    _playerMarker.position,
                    _triggerPoint!.position);
            }
            catch
            {
            }

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 14 player marker='{BuildHierarchy(_playerMarker)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static Transform? FindPlayerMarkerShared()
    {
        return Hub4EntranceLookup.GetPlayerMarker();
    }

    private static string BuildHierarchy(Transform tr)
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
            return "<unavailable>";
        }
    }
}


internal sealed class BunkerStarRequirementKeeper : MonoBehaviour
{
    public static int RequiredStars = 50;

    private const int VanillaRequiredStars = 66;

    private const string Hub8Room =
        "GameRoom_Hub8";

    private const string StarEaterRootPath =
        "Root/GameRoom_Hub8_Logic/Objects/NPCs/StarEater";

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
            "[SCRC-AP] DEV SHIFT+F5: requested one manual Secret Bunker GetValue patch attempt. If successful, the patch will be held for 20 seconds or until leaving Hub8.");
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
        if (!DeveloperHarness.IsCurrentRoom(
                Hub8Room) ||
            RequiredStars == VanillaRequiredStars)
        {
            RestoreStarEaterInteractionPatches(
                "outside Hub8 / vanilla requirement");

            RestoreNativeGetValuePatch(
                "outside Hub8 / vanilla requirement");

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
                    $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH READY: interaction='{BuildHierarchy(componentTransform)}', currentGetValue={(current == int.MinValue ? "<unknown>" : current.ToString())}, patchRadius={PatchRadius:0.##}, releaseRadius={ReleaseRadius:0.##}. feedingThreshold object data will not be modified.");
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
                    $"[SCRC-AP] SECRET BUNKER player marker resolved directly: '{BuildHierarchy(_playerTransform)}'.");

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
                "[SCRC-AP] SECRET BUNKER player marker not yet available through direct GameObject.Find; no scene-wide search will be performed.");
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
                    $"[SCRC-AP] SECRET BUNKER INTERACTION PATCH ACTIVE: forced Star Eater DoesStarEaterWantToInteract, CanFeedNewStars, IsCharacterAllowedToInteract, and IsInteractionEnabled true; reason='{reason}'. Patches are proximity-scoped and restored on exit/scene transition.");
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
                    $"[SCRC-AP] SECRET BUNKER interaction method restore failed at 0x{pair.Key.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}.");
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
                $"[SCRC-AP] SECRET BUNKER INTERACTION PATCH RESTORED: reason='{reason}', allRestored={allRestored}.");
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
                    "[SCRC-AP] SECRET BUNKER NATIVE INTERACTION INTEREST ACTIVE: registered the real StarEaterInteraction with the game's interaction-interest pipeline.");
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
            $"[SCRC-AP] SECRET BUNKER INTERACTION PATCH FAILURE: {message}");
    }

    private static void ReportInteractionInterestFailureOnce(
        string message)
    {
        if (_reportedInteractionInterestFailure)
            return;

        _reportedInteractionInterestFailure =
            true;

        Plugin.LoggerInstance?.LogDebug(
            $"[SCRC-AP] SECRET BUNKER interaction-interest transient failure: {message}");
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
                (byte)(RequiredStars & 0xFF),
                (byte)((RequiredStars >> 8) & 0xFF),
                (byte)((RequiredStars >> 16) & 0xFF),
                (byte)((RequiredStars >> 24) & 0xFF),
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
                after != RequiredStars)
            {
                RestoreNativeGetValuePatch(
                    "verification failed");

                ReportPatchFailureOnce(
                    $"patched GetValue verification returned {(verified ? after.ToString() : "<invoke-failed>")} instead of {RequiredStars}");

                return;
            }

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH ACTIVE: DefinedValue<int>.GetValue() {(before == int.MinValue ? "<unknown>" : before.ToString())} -> {after}; reason='{reason}'. Patch is temporary and feedingThreshold data is untouched.");
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
                        $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH RESTORED: reason='{reason}'.");
                }
                else
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH RESTORE FAILED at 0x{codePointer.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}.");
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH RESTORE FAILED: {ex.GetBaseException().Message}");
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
                    StarEaterRootPath);

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

            return path.Contains(
                       "GameRoom_Hub8_Logic/Objects/NPCs/StarEater",
                       StringComparison.Ordinal)
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
            $"[SCRC-AP] SECRET BUNKER GETVALUE PATCH FAILURE: {message}");
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


internal static class MusicLabSequencePatches
{
    public static void GarageScoredSongBeginPrefix(object __instance)
    {
        try
        {
            MusicLabDiscovery.RecordGarageScoredSongSequenceBegin(__instance);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB GARAGE exact scored-song Begin prefix failed: {ex.GetBaseException().Message}");
        }
    }
}

internal static class CassetteSaveTransactionPatches
{
    private static readonly HashSet<string> ExtractionFailures = new(StringComparer.Ordinal);

    public static void SelectedSlotMutationPostfix(object[]? __args, MethodBase __originalMethod)
    {
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
        CassetteReceiptRandomization.QueueSaveBoundarySignal(slot, kind);
    }

    public static void BuildPlayerSaveStatePrefix(
        object[]? __args,
        MethodBase __originalMethod,
        out long __state)
    {
        __state = 0;
        const string requestIdentity = "BuildPlayerSaveStateFromFileRequest";
        string methodIdentity = $"{__originalMethod?.DeclaringType?.Name ?? "SaveDataRequestProcessor"}.{__originalMethod?.Name ?? "ProcessRequest"}({requestIdentity})";
        try
        {
            object? request = ReflectionUtil.FindArg(__args, requestIdentity);
            if (request == null)
            {
                LogExtractionFailureOnce(methodIdentity, $"{requestIdentity} argument was missing");
                return;
            }
            PropertyInfo? slotProperty = request.GetType().GetProperty(
                "SlotNumber", BindingFlags.Public | BindingFlags.Instance);
            object? rawSlot = slotProperty?.GetValue(request);
            if (rawSlot == null)
            {
                LogExtractionFailureOnce(methodIdentity, $"public {requestIdentity}.SlotNumber was unreadable");
                return;
            }
            int slot = Convert.ToInt32(rawSlot, CultureInfo.InvariantCulture);
            CassetteReceiptRandomization.BeginSaveStateLifecycleDiagnostic(slot, out __state);
        }
        catch (Exception ex)
        {
            try
            {
                LogExtractionFailureOnce(
                    methodIdentity,
                    $"public lifecycle prefix failed: {ex.GetBaseException().GetType().Name}");
            }
            catch
            {
                // Diagnostic extraction must never affect the native request path.
            }
            __state = 0;
        }
    }

    public static void BuiltPlayerSaveStatePostfix(
        object[]? __args,
        MethodBase __originalMethod,
        long __state)
    {
        const string requestIdentity = "BuildPlayerSaveStateFromFileRequest";
        string methodIdentity = $"{__originalMethod?.DeclaringType?.Name ?? "SaveDataRequestProcessor"}.{__originalMethod?.Name ?? "ProcessRequest"}({requestIdentity})";
        try
        {
            object? diagnosticRequest = ReflectionUtil.FindArg(__args, requestIdentity);
            PropertyInfo? slotProperty = diagnosticRequest?.GetType().GetProperty(
                "SlotNumber", BindingFlags.Public | BindingFlags.Instance);
            object? rawSlot = slotProperty?.GetValue(diagnosticRequest);
            if (rawSlot != null)
            {
                int diagnosticSlot = Convert.ToInt32(rawSlot, CultureInfo.InvariantCulture);
                CassetteReceiptRandomization.CompleteSaveStateLifecycleDiagnostic(__state, diagnosticSlot);
            }
        }
        catch (Exception ex)
        {
            try
            {
                LogExtractionFailureOnce(
                    methodIdentity,
                    $"public lifecycle postfix failed: {ex.GetBaseException().GetType().Name}");
            }
            catch
            {
                // Diagnostic extraction must never suppress the authoritative boundary signal below.
            }
        }

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
        CassetteReceiptRandomization.QueueSaveBoundarySignal(slot.Value, CassetteSaveBoundarySignalKind.Build);
    }

    public static void PersistBundleRoutingPrefix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        out CassetteBundleRoutingDiagnosticToken __state)
    {
        __state = default;
        try
        {
            object? request = ReflectionUtil.FindArg(__args, "PersistSaveChangeBundleRequest");
            CassetteReceiptRandomization.BeginBundleRoutingDiagnostic(
                CassetteBundleRoutingBoundary.PersistBundle,
                __instance,
                request,
                __originalMethod,
                originalAllowed: true,
                out __state);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(PersistSaveChangeBundleRequest)",
                "routing prefix",
                ex);
        }
    }

    public static void PersistBundleRoutingPostfix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        CassetteBundleRoutingDiagnosticToken __state)
    {
        try
        {
            CassetteReceiptRandomization.CompleteBundleRoutingDiagnostic(
                __state,
                __instance,
                ReflectionUtil.FindArg(__args, "PersistSaveChangeBundleRequest"),
                __originalMethod);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(PersistSaveChangeBundleRequest)",
                "routing postfix",
                ex);
        }
    }

    public static void PersistAllRoutingPrefix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        out CassetteBundleRoutingDiagnosticToken __state)
    {
        __state = default;
        try
        {
            object? request = ReflectionUtil.FindArg(__args, "PersistAllSaveChangeBundlesRequest");
            CassetteReceiptRandomization.BeginBundleRoutingDiagnostic(
                CassetteBundleRoutingBoundary.PersistAllBundles,
                __instance,
                request,
                __originalMethod,
                originalAllowed: true,
                out __state);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(PersistAllSaveChangeBundlesRequest)",
                "routing prefix",
                ex);
        }
    }

    public static void PersistAllRoutingPostfix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        CassetteBundleRoutingDiagnosticToken __state)
    {
        try
        {
            CassetteReceiptRandomization.CompleteBundleRoutingDiagnostic(
                __state,
                __instance,
                ReflectionUtil.FindArg(__args, "PersistAllSaveChangeBundlesRequest"),
                __originalMethod);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(PersistAllSaveChangeBundlesRequest)",
                "routing postfix",
                ex);
        }
    }

    public static void DiscardAllRoutingPrefix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        out CassetteBundleRoutingDiagnosticToken __state)
    {
        __state = default;
        try
        {
            object? request = ReflectionUtil.FindArg(__args, "DiscardAllUnstagedSaveStateChangesRequest");
            CassetteReceiptRandomization.BeginBundleRoutingDiagnostic(
                CassetteBundleRoutingBoundary.DiscardAllUnstaged,
                __instance,
                request,
                __originalMethod,
                originalAllowed: true,
                out __state);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(DiscardAllUnstagedSaveStateChangesRequest)",
                "routing prefix",
                ex);
        }
    }

    public static void DiscardAllRoutingPostfix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        CassetteBundleRoutingDiagnosticToken __state)
    {
        try
        {
            CassetteReceiptRandomization.CompleteBundleRoutingDiagnostic(
                __state,
                __instance,
                ReflectionUtil.FindArg(__args, "DiscardAllUnstagedSaveStateChangesRequest"),
                __originalMethod);
        }
        catch (Exception ex)
        {
            LogRoutingExtractionFailureSafely(
                "SaveDataRequestProcessor.ProcessRequest(DiscardAllUnstagedSaveStateChangesRequest)",
                "routing postfix",
                ex);
        }
    }

    public static void MostRecentSelectionPrefix(object? __instance)
    {
        CassetteReceiptRandomization.BeginMostRecentSelectionBoundary(__instance);
    }

    public static void PlayerSaveWriteCompletedEventPostfix(object[]? __args, MethodBase __originalMethod)
    {
        const string eventIdentity = "PlayerSaveWriteCompletedEvent";
        object? nativeEvent = ReflectionUtil.FindArg(__args, eventIdentity);
        if (nativeEvent == null)
        {
            string methodIdentity =
                $"{__originalMethod?.DeclaringType?.Name ?? "<unknown>"}.{__originalMethod?.Name ?? "HandleEvent"}({eventIdentity})";
            LogExtractionFailureOnce(methodIdentity, $"{eventIdentity} argument was missing");
            return;
        }
        CassetteReceiptRandomization.OnPlayerSaveWriteCompletedEvent(nativeEvent);
    }

    private static void LogRoutingExtractionFailureSafely(
        string identity,
        string phase,
        Exception exception)
    {
        try
        {
            string exceptionType = exception.GetBaseException().GetType().Name;
            LogExtractionFailureOnce(identity, $"{phase} failed: {exceptionType}");
        }
        catch
        {
            // Diagnostic fallback logging must never affect the patched native request.
        }
    }

    private static void LogExtractionFailureOnce(string identity, string reason)
    {
        string key = $"{identity}|{reason}";
        lock (ExtractionFailures)
        {
            if (!ExtractionFailures.Add(key)) return;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE BOUNDARY ARGUMENT REJECTED target='{identity}' reason='{reason}'; callback failed closed.");
    }
}


internal static class GamePatches
{
    private static readonly Dictionary<string, PendingResult> Pending = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> PersistedThisSession = new(StringComparer.OrdinalIgnoreCase);

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
        object[]? __args,
        MethodBase __originalMethod,
        out CassetteBundleRoutingDiagnosticToken __state)
    {
        __state = default;
        object? request = ReflectionUtil.FindArg(__args, "RecordSongCassetteStatusInSaveDataRequest");
        if (request == null)
            return true;

        string? song = (ReflectionUtil.ReadMember(request, "Song") ??
                        ReflectionUtil.ReadMember(request, "_Song_k__BackingField"))?.ToString();
        string? status = (ReflectionUtil.ReadMember(request, "CassetteStatus") ??
                          ReflectionUtil.ReadMember(request, "_CassetteStatus_k__BackingField"))?.ToString();
        bool allowed = CassetteSourceRandomization.AllowCassetteStatusRequest(
            song, status, CassetteReceiptRandomization.IsApplyingNativeGrant);
        try
        {
            CassetteReceiptRandomization.BeginBundleRoutingDiagnostic(
                CassetteBundleRoutingBoundary.RecordSong,
                __instance,
                request,
                __originalMethod,
                allowed,
                out __state);
        }
        catch
        {
            // Diagnostics must never alter native cassette request suppression.
            __state = default;
        }
        return allowed;
    }

    public static void CassetteStatusRequestPostfix(
        object? __instance,
        object[]? __args,
        MethodBase __originalMethod,
        CassetteBundleRoutingDiagnosticToken __state)
    {
        try
        {
            CassetteReceiptRandomization.CompleteBundleRoutingDiagnostic(
                __state,
                __instance,
                ReflectionUtil.FindArg(__args, "RecordSongCassetteStatusInSaveDataRequest"),
                __originalMethod);
        }
        catch
        {
            // Diagnostics must never alter native cassette request completion.
        }
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

        MusicLabDiscovery.RecordResultRequest(request, level, variant, score, difficulty);
        int? starsEarned = MusicLabDiscovery.ProbeNormalLevelStarRating(level, variant, score);
        Pending[level] = new PendingResult(level, variant, players, score, difficulty, starsEarned);

        Level5Discovery.RecordLevelResultApplied(level);
        Level6Discovery.RecordLevelResultApplied(level);
        Level8Discovery.RecordLevelResultApplied(level);
        PlantPipesRandomization.OnLevelResultApplied(level);
        CassetteReceiptRandomization.OnLifecyclePoint("level result applied");
    }

    public static void ResultPersistedEventPostfix(object[]? __args)
    {
        object? evt = ReflectionUtil.FindArg(__args, "LevelResultWasPersistedEvent");
        if (evt == null) return;

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

        bool repeatedPersistedKey = !PersistedThisSession.Add(persistedKey);
        bool garageResult = string.Equals(level, "Level_27", StringComparison.OrdinalIgnoreCase);

        if (repeatedPersistedKey && !garageResult)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] Duplicate persisted event ignored for {level} variant={variant}.");
            return;
        }

        if (repeatedPersistedKey && garageResult)
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Repeated Level_27 persisted event retained for Game Garage song diagnostics.");
        }

        MusicLabDiscovery.RecordPersistedEvent(evt, level, variant, result?.Score);

        Level5Discovery.RecordLevelPersisted(level);
        Level6Discovery.RecordLevelPersisted(level);
        Level8Discovery.RecordLevelPersisted(level);
        EarlySequenceBlockerPatches.RecordLevelPersisted(level);
        PlantPipesRandomization.OnLevelResultPersisted(level);
        CassetteReceiptRandomization.OnLifecyclePoint("level result persisted");
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

            return;
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

        bool level22 = string.Equals(level, "Level_28", StringComparison.OrdinalIgnoreCase);
        if (level22)
        {
            IReadOnlyList<string> locations = LevelCompletionPolicy.LocationsForPersistedResult(
                level,
                result?.StarsEarned);
            if (locations.Count == 0)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 22 RESULT SUPPRESSED: persisted event had no verified successful Star result (stars={result?.StarsEarned.ToString() ?? "<missing>"}).");
            }

            foreach (string location in locations)
            {
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] AP LOCATION '{location}'.");
                Plugin.AP?.QueueLocation(location);
            }
        }
        else if (LocationMap.InternalToLocationName.TryGetValue(level, out string? locationName))
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] AP LOCATION '{locationName}'.");
            Plugin.AP?.QueueLocation(locationName);
        }
        else
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] Unmapped internal level '{level}'. It will not be sent to Archipelago yet.");
        }
    }

    private sealed record PendingResult(
        string Level,
        string Variant,
        int? Players,
        int? Score,
        string Difficulty,
        int? StarsEarned);
}















internal static class EarlySequenceBlockerPatches
{
    private sealed class BlockedCall
    {
        public object Instance = null!;
        public MethodBase Method = null!;
        public object[] Args = Array.Empty<object>();
    }

    private static readonly object Sync = new();

    private static readonly Dictionary<string, BlockedCall> Blocked =
        new(StringComparer.Ordinal);

    [ThreadStatic]
    private static bool _replaying;

    private static bool _levelOnePersisted;

    public static void RecordLevelPersisted(string level)
    {
        if (string.Equals(level, "Level_05", StringComparison.OrdinalIgnoreCase))
            _levelOnePersisted = true;
    }

    public static void NewSaveCreated() => _levelOnePersisted = false;

    public static bool Prefix(
        MethodBase __originalMethod,
        object? __instance,
        object[]? __args)
    {
        string typeName = __originalMethod.DeclaringType?.Name ?? "";
        string methodName = __originalMethod.Name;

        if (!_replaying &&
            __instance != null &&
            methodName == "Begin" &&
            RootsPresentationPolicy.ShouldSuppressPostLevelOne(
                AreaAccessPrototype.Enabled,
                IntroHubSkip.Compatible,
                typeName,
                _levelOnePersisted))
        {
            bool markedComplete = TryMarkSequenceComplete(__instance);
            Plugin.LoggerInstance?.LogWarning(
                markedComplete
                    ? "[SCRC-AP] REDUNDANT POST-LEVEL-1 ROOTS DIFFICULTY PRESENTATION SUPPRESSED and marked complete."
                    : "[SCRC-AP] REDUNDANT POST-LEVEL-1 ROOTS DIFFICULTY PRESENTATION SUPPRESSED but completion could not be marked.");
            return false;
        }

        if (_replaying ||
            !NativeProgression.RandomizeEarlyProgression ||
            NativeProgression.HasLevel2Access ||
            __instance == null)
        {
            return true;
        }

        string? key = null;
        string? label = null;

        if (typeName == "AssignAndCommentOnMusicDifficultySequenceStep" &&
            methodName == "Begin")
        {
            key = "difficulty-sequence";
            label = "MUSIC DIFFICULTY ASSIGNMENT SEQUENCE";
        }
        else if (typeName == "Hub02MegafierDoorSetHatchOverrideSequenceStep" &&
                 methodName == "Trigger")
        {
            key = "hub02-door-sequence";
            label = "HUB02 MEGAFIER DOOR/HATCH SEQUENCE";
        }
        else
        {
            return true;
        }

        lock (Sync)
        {
            Blocked[key] = new BlockedCall
            {
                Instance = __instance,
                Method = __originalMethod,
                Args = __args?.ToArray() ?? Array.Empty<object>(),
            };
        }

        if (key == "difficulty-sequence")
        {
            bool markedComplete = TryMarkSequenceComplete(__instance);

            Plugin.LoggerInstance?.LogWarning(
                markedComplete
                    ? "[SCRC-AP] SKIPPED MUSIC DIFFICULTY ASSIGNMENT SEQUENCE and MARKED IT COMPLETE until Level 2 Access."
                    : "[SCRC-AP] SKIPPED MUSIC DIFFICULTY ASSIGNMENT SEQUENCE but FAILED TO MARK COMPLETE.");

            return false;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BLOCKED {label} until Level 2 Access.");

        return false;
    }

    private static bool TryMarkSequenceComplete(object instance)
    {
        try
        {
            MethodInfo? mark = instance.GetType().GetMethod(
                "MarkAsComplete",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (mark == null)
                return false;

            ParameterInfo[] ps = mark.GetParameters();
            if (ps.Length != 0)
                return false;

            mark.Invoke(instance, Array.Empty<object>());
            return true;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed to MarkAsComplete difficulty sequence: {ex.GetBaseException()}");
            return false;
        }
    }

    public static void DeveloperReset()
    {
        lock (Sync)
            Blocked.Clear();

        Plugin.LoggerInstance?.LogInfo(
            "[SCRC-AP] DEV reset cached difficulty sequence replay state.");
    }

    public static void ReleaseLevel2Access()
    {
        List<KeyValuePair<string, BlockedCall>> calls;

        lock (Sync)
        {
            calls = Blocked.ToList();
            Blocked.Clear();
        }

        if (calls.Count == 0)
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 2 Access: no blocked early sequence steps were waiting.");
            return;
        }

        foreach (var kvp in calls)
            Replay(kvp.Key, kvp.Value);
    }

    private static void Replay(string key, BlockedCall call)
    {
        try
        {
            _replaying = true;

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] REPLAYING EARLY SEQUENCE {key} after Level 2 Access.");

            if (key == "difficulty-sequence")
            {
                try
                {
                    MethodInfo? reset = call.Instance.GetType().GetMethod(
                        "ResetToStart",
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (reset != null && reset.GetParameters().Length == 0)
                    {
                        reset.Invoke(call.Instance, Array.Empty<object>());
                        Plugin.LoggerInstance?.LogInfo(
                            "[SCRC-AP] Reset difficulty assignment sequence before replay.");
                    }
                }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] Could not reset difficulty sequence before replay: {ex.GetBaseException().Message}");
                }
            }

            if (call.Method is MethodInfo mi)
                mi.Invoke(call.Instance, call.Args);

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] REPLAYED EARLY SEQUENCE {key} successfully.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed replaying early sequence {key}: {ex.GetBaseException()}");
        }
        finally
        {
            _replaying = false;
        }
    }
}


internal static class Hub02MegafierDoorViewPatches
{
    private static readonly object Sync = new();

    private static object? _blockedInstance;
    private static MethodBase? _blockedMethod;
    private static object[]? _blockedArgs;

    [ThreadStatic]
    private static bool _replaying;

    public static bool Prefix(
        MethodBase __originalMethod,
        object? __instance,
        object[]? __args)
    {
        if (_replaying ||
            !NativeProgression.RandomizeEarlyProgression ||
            NativeProgression.HasLevel2Access ||
            __args == null ||
            __args.Length == 0 ||
            __args[0] == null)
        {
            return true;
        }

        object state = __args[0];

        bool? hatchOpen = ReflectionUtil.ReadBool(state, "HatchOpen");

        if (hatchOpen != true)
            return true;

        lock (Sync)
        {
            _blockedInstance = __instance;
            _blockedMethod = __originalMethod;
            _blockedArgs = __args.ToArray();
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] BLOCKED HUB02 MEGAFIER DOOR VIEW HatchOpen=True until Level 2 Access.");

        return false;
    }

    public static void ReleaseLevel2Access()
    {
        object? instance;
        MethodBase? method;
        object[]? args;

        lock (Sync)
        {
            instance = _blockedInstance;
            method = _blockedMethod;
            args = _blockedArgs;

            _blockedInstance = null;
            _blockedMethod = null;
            _blockedArgs = null;
        }

        if (instance == null || method is not MethodInfo mi || args == null)
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 2 Access: no blocked Hub02 Megafier door view refresh was waiting.");
            return;
        }

        try
        {
            _replaying = true;

            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] REPLAYING HUB02 MEGAFIER DOOR VIEW after Level 2 Access.");

            mi.Invoke(instance, args);

            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] REPLAYED HUB02 MEGAFIER DOOR VIEW successfully.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed replaying Hub02 Megafier door view: {ex.GetBaseException()}");
        }
        finally
        {
            _replaying = false;
        }
    }
}

internal static class EarlyRuntimeUnlockPatches
{
    private static readonly object Sync = new();

    private static object? _blockedDifficultyProcessor;
    private static object? _blockedDifficultyRequest;
    private static object? _blockedChickenBlockadeView;

    [ThreadStatic]
    private static bool _replaying;

    public static bool DifficultyRequestPrefix(object? __instance, object[]? __args)
    {
        if (AreaAccessPrototype.Enabled && IntroHubSkip.Compatible)
            return true;

        if (_replaying ||
            !NativeProgression.RandomizeEarlyProgression ||
            NativeProgression.HasLevel2Access)
        {
            return true;
        }

        object? request = __args?.FirstOrDefault(a =>
            a != null && a.GetType().Name == "SetPlayerTrackingDifficultyRequest");

        if (request == null)
            return true;

        lock (Sync)
        {
            _blockedDifficultyProcessor = __instance;
            _blockedDifficultyRequest = request;
        }

        string difficulty = "<unknown>";
        try
        {
            object? d = ReflectionUtil.ReadMember(request, "DifficultyLevel")
                     ?? ReflectionUtil.ReadMember(request, "_DifficultyLevel_k__BackingField");
            if (d != null)
                difficulty = d.ToString() ?? "<unknown>";
        }
        catch { }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BLOCKED RUNTIME DIFFICULTY ASSIGNMENT '{difficulty}' until Level 2 Access.");

        return false;
    }

    public static bool ChickenBlockadePrefix(object? __instance)
    {
        if (_replaying ||
            !NativeProgression.RandomizeEarlyProgression ||
            NativeProgression.HasLevel2Access)
        {
            return true;
        }

        if (__instance != null)
        {
            lock (Sync)
                _blockedChickenBlockadeView = __instance;
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] BLOCKED HUB02 CHICKEN BLOCKADE LEAVE ANIMATION until Level 2 Access.");

        return false;
    }

    public static void DeveloperReset()
    {
        lock (Sync)
        {
            _blockedDifficultyProcessor = null;
            _blockedDifficultyRequest = null;
            _blockedChickenBlockadeView = null;
        }

        Plugin.LoggerInstance?.LogInfo(
            "[SCRC-AP] DEV reset cached runtime unlock replay state.");
    }

    public static void ReleaseLevel2Access()
    {
        object? difficultyProcessor;
        object? difficultyRequest;
        object? chickenView;

        lock (Sync)
        {
            difficultyProcessor = _blockedDifficultyProcessor;
            difficultyRequest = _blockedDifficultyRequest;
            chickenView = _blockedChickenBlockadeView;

            _blockedDifficultyProcessor = null;
            _blockedDifficultyRequest = null;
            _blockedChickenBlockadeView = null;
        }

        Plugin.LoggerInstance?.LogInfo(
            "[SCRC-AP] Level 2 Access received: releasing blocked runtime gate/difficulty actions.");

        if (difficultyProcessor != null && difficultyRequest != null)
            ReplayDifficultyRequest(difficultyProcessor, difficultyRequest);

        if (chickenView != null)
            ReplayChickenBlockade(chickenView);
    }

    private static void ReplayDifficultyRequest(object processor, object request)
    {
        MethodInfo? method = processor.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "ProcessRequest")
                    return false;

                ParameterInfo[] ps;
                try { ps = m.GetParameters(); }
                catch { return false; }

                return ps.Length == 1 && ps[0].ParameterType.IsInstanceOfType(request);
            });

        if (method == null)
        {
            Plugin.LoggerInstance?.LogError(
                "[SCRC-AP] Could not replay SetPlayerTrackingDifficultyRequest.");
            return;
        }

        try
        {
            _replaying = true;
            method.Invoke(processor, new[] { request });

            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] REPLAYED RUNTIME DIFFICULTY ASSIGNMENT after Level 2 Access.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed replaying runtime difficulty assignment: {ex.GetBaseException()}");
        }
        finally
        {
            _replaying = false;
        }
    }

    private static void ReplayChickenBlockade(object view)
    {
        MethodInfo? method = view.GetType().GetMethod(
            "TriggerLeaveAnimation",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null)
        {
            Plugin.LoggerInstance?.LogError(
                "[SCRC-AP] Could not replay Hub02ChickenBlockadeView.TriggerLeaveAnimation.");
            return;
        }

        try
        {
            _replaying = true;
            method.Invoke(view, Array.Empty<object>());

            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] REPLAYED HUB02 CHICKEN BLOCKADE LEAVE ANIMATION after Level 2 Access.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed replaying chicken blockade animation: {ex.GetBaseException()}");
        }
        finally
        {
            _replaying = false;
        }
    }
}



internal sealed class FirstAreaGateKeeper : MonoBehaviour
{
    private sealed class TransformPose
    {
        public Transform Transform = null!;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
    }

    private const string GateRootName = "FirstAreaGate";
    private const string AnimatedGateName = "AnimatedGate";

    private readonly List<TransformPose> _closedPose = new();

    private Transform? _gateRoot;
    private Transform? _animatedGate;
    private int _searchCooldown;
    private bool _reportedLocked;
    private bool _reportedReleased;

    public FirstAreaGateKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        if (!DeveloperHarness.IsHub2Current)
            return;

        if (!NativeProgression.RandomizeEarlyProgression)
            return;

        if (NativeProgression.HasLevel2Access)
        {
            if (_closedPose.Count > 0 && !_reportedReleased)
            {
                _reportedReleased = true;
                _reportedLocked = false;

                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] FIRST AREA GATE RELEASED: Level 2 Access is present; closed-pose enforcement stopped.");
            }

            return;
        }

        _reportedReleased = false;

        if (!IsCurrentGateValid())
        {
            if (_searchCooldown > 0)
            {
                _searchCooldown--;
                return;
            }

            _searchCooldown = 15;
            FindAndCaptureGate();
        }

        if (_closedPose.Count == 0)
            return;

        RestoreClosedPose();

        if (!_reportedLocked)
        {
            _reportedLocked = true;

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] FIRST AREA GATE LOCKED: restoring {_closedPose.Count} AnimatedGate transform(s) to their closed pose until Level 2 Access.");
        }
    }

    private bool IsCurrentGateValid()
    {
        try
        {
            return _gateRoot != null &&
                   _animatedGate != null &&
                   _gateRoot.gameObject != null &&
                   _animatedGate.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private void FindAndCaptureGate()
    {
        Transform? gateRoot = null;

        try
        {
            foreach (Transform tr in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                if (!string.Equals(tr.name, GateRootName, StringComparison.Ordinal))
                    continue;

                string path = BuildHierarchy(tr);

                if (!path.Contains(
                        "Root/GameRoom_Hub2_Logic/Objects/FirstAreaGate",
                        StringComparison.Ordinal))
                    continue;

                gateRoot = tr;
                break;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] FIRST AREA GATE search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (gateRoot == null)
            return;

        Transform? animatedGate = null;

        try
        {
            foreach (Transform tr in gateRoot.GetComponentsInChildren<Transform>(true))
            {
                if (string.Equals(tr.name, AnimatedGateName, StringComparison.Ordinal))
                {
                    animatedGate = tr;
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] FIRST AREA GATE child search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (animatedGate == null)
            return;

        var pose = new List<TransformPose>();

        try
        {
            foreach (Transform tr in animatedGate.GetComponentsInChildren<Transform>(true))
            {
                pose.Add(new TransformPose
                {
                    Transform = tr,
                    LocalPosition = tr.localPosition,
                    LocalRotation = tr.localRotation,
                    LocalScale = tr.localScale
                });
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] FIRST AREA GATE snapshot failed: {ex.GetBaseException().Message}");
            return;
        }

        if (pose.Count == 0)
            return;

        _gateRoot = gateRoot;
        _animatedGate = animatedGate;

        _closedPose.Clear();
        _closedPose.AddRange(pose);

        _reportedLocked = false;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] FIRST AREA GATE CAPTURED at '{BuildHierarchy(animatedGate)}' with {_closedPose.Count} transform(s).");
    }

    private void RestoreClosedPose()
    {
        for (int i = 0; i < _closedPose.Count; i++)
        {
            TransformPose pose = _closedPose[i];
            Transform tr = pose.Transform;

            try
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                tr.localPosition = pose.LocalPosition;
                tr.localRotation = pose.LocalRotation;
                tr.localScale = pose.LocalScale;
            }
            catch
            {
                // Scene reloads invalidate old IL2CPP wrapper objects. Clear and
                // rediscover on the next frame rather than touching stale pointers.
                _gateRoot = null;
                _animatedGate = null;
                _closedPose.Clear();
                _reportedLocked = false;
                return;
            }
        }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 20 && current != null; i++)
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



internal sealed class Level3DoorKeeper : MonoBehaviour
{
    private sealed class VisualTransformState
    {
        public Transform Transform = null!;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public bool ActiveSelf;
    }

    private sealed class RendererState
    {
        public Renderer Renderer = null!;
        public bool Enabled;
    }

    private sealed class AnimatorState
    {
        public Animator Animator = null!;
        public bool Enabled;
    }

    private sealed class LegacyAnimationState
    {
        public Animation Animation = null!;
        public bool Enabled;
    }

    private const string LevelDoorRootName = "Level07DoorAndCover-tofb";
    private const string CoverName = "MegafierCover";
    private const string VisualsName = "Visuals";
    private const string EntranceName = "Level_07_Entrance";
    private const string RevealFlag = "ROOTS_HUB_LEVEL_07_DOOR_REVEALED";

    // State found on the already-progressed save. This is restored when the
    // developer grants Level 3 again.
    private readonly List<VisualTransformState> _baselineTransforms = new();
    private readonly List<RendererState> _baselineRenderers = new();
    private readonly List<AnimatorState> _animators = new();
    private readonly List<LegacyAnimationState> _legacyAnimations = new();

    // Pose generated by rewinding/rebinding the cover animation to its prefab
    // default. This is what is held while AP Level 3 Access is missing.
    private readonly List<VisualTransformState> _lockedTransforms = new();

    private Transform? _doorRoot;
    private Transform? _coverRoot;
    private Transform? _visualsRoot;
    private Transform? _entranceRoot;
    private Transform? _doorCollision;

    private bool _baselineCoverActive;
    private bool _baselineEntranceActive;
    private bool _baselineCollisionActive;
    private bool _baselineCaptured;
    private bool _lockedPosePrepared;

    private int _searchCooldown;
    private bool _reportedLocked;
    private bool _reportedReleased;

    public Level3DoorKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        if (!DeveloperHarness.IsHub2Current)
            return;

        if (!NativeProgression.RandomizeEarlyProgression)
            return;

        if (!TargetsValid())
        {
            if (_searchCooldown > 0)
            {
                _searchCooldown--;
                return;
            }

            _searchCooldown = 15;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (NativeProgression.HasLevel3Access)
        {
            ReleaseHardBlock();
            return;
        }

        ApplyHardBlock();
    }

    private bool TargetsValid()
    {
        try
        {
            return _doorRoot != null &&
                   _coverRoot != null &&
                   _visualsRoot != null &&
                   _entranceRoot != null &&
                   _doorRoot.gameObject != null &&
                   _coverRoot.gameObject != null &&
                   _visualsRoot.gameObject != null &&
                   _entranceRoot.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? doorRoot = null;

        try
        {
            foreach (Transform tr in UnityEngine.Object.FindObjectsOfType<Transform>())
            {
                if (!string.Equals(tr.name, LevelDoorRootName, StringComparison.Ordinal))
                    continue;

                string path = BuildHierarchy(tr);

                if (!path.Contains(
                        "Root/GameRoom_Hub2_Logic/Objects/LevelDoors/Level07DoorAndCover-tofb",
                        StringComparison.Ordinal))
                    continue;

                doorRoot = tr;
                break;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] LEVEL 3 HARD BLOCKER root search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (doorRoot == null)
            return;

        Transform? coverRoot = null;
        Transform? visualsRoot = null;
        Transform? entranceRoot = null;
        Transform? doorCollision = null;

        try
        {
            foreach (Transform tr in doorRoot.GetComponentsInChildren<Transform>(true))
            {
                string name = tr.name ?? string.Empty;
                string path = BuildHierarchy(tr);

                if (coverRoot == null && string.Equals(name, CoverName, StringComparison.Ordinal))
                {
                    coverRoot = tr;
                    continue;
                }

                if (visualsRoot == null &&
                    string.Equals(name, VisualsName, StringComparison.Ordinal) &&
                    path.Contains("/MegafierCover/Visuals", StringComparison.Ordinal))
                {
                    visualsRoot = tr;
                    continue;
                }

                if (entranceRoot == null && string.Equals(name, EntranceName, StringComparison.Ordinal))
                {
                    entranceRoot = tr;
                    continue;
                }

                if (doorCollision == null &&
                    string.Equals(name, "DoorCollision", StringComparison.Ordinal) &&
                    path.Contains("/Level_07_Entrance/AnimatedDoor/DoorCollision", StringComparison.Ordinal))
                {
                    doorCollision = tr;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 3 HARD BLOCKER target search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (coverRoot == null || visualsRoot == null || entranceRoot == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 3 HARD BLOCKER incomplete target set: " +
                $"coverFound={coverRoot != null} visualsFound={visualsRoot != null} " +
                $"entranceFound={entranceRoot != null} collisionFound={doorCollision != null}.");
            return;
        }

        var baselineTransforms = new List<VisualTransformState>();
        var baselineRenderers = new List<RendererState>();
        var animators = new List<AnimatorState>();
        var legacyAnimations = new List<LegacyAnimationState>();

        try
        {
            foreach (Transform tr in visualsRoot.GetComponentsInChildren<Transform>(true))
            {
                baselineTransforms.Add(new VisualTransformState
                {
                    Transform = tr,
                    LocalPosition = tr.localPosition,
                    LocalRotation = tr.localRotation,
                    LocalScale = tr.localScale,
                    ActiveSelf = tr.gameObject.activeSelf
                });
            }

            foreach (Renderer renderer in visualsRoot.GetComponentsInChildren<Renderer>(true))
            {
                baselineRenderers.Add(new RendererState
                {
                    Renderer = renderer,
                    Enabled = renderer.enabled
                });
            }

            foreach (Animator animator in visualsRoot.GetComponentsInChildren<Animator>(true))
            {
                animators.Add(new AnimatorState
                {
                    Animator = animator,
                    Enabled = animator.enabled
                });
            }

            foreach (Animation animation in visualsRoot.GetComponentsInChildren<Animation>(true))
            {
                legacyAnimations.Add(new LegacyAnimationState
                {
                    Animation = animation,
                    Enabled = animation.enabled
                });
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 3 HARD BLOCKER baseline capture failed: {ex.GetBaseException().Message}");
            return;
        }

        _doorRoot = doorRoot;
        _coverRoot = coverRoot;
        _visualsRoot = visualsRoot;
        _entranceRoot = entranceRoot;
        _doorCollision = doorCollision;

        _baselineCoverActive = SafeActiveSelf(coverRoot, true);
        _baselineEntranceActive = SafeActiveSelf(entranceRoot, true);
        _baselineCollisionActive = doorCollision != null && SafeActiveSelf(doorCollision, true);

        _baselineTransforms.Clear();
        _baselineTransforms.AddRange(baselineTransforms);

        _baselineRenderers.Clear();
        _baselineRenderers.AddRange(baselineRenderers);

        _animators.Clear();
        _animators.AddRange(animators);

        _legacyAnimations.Clear();
        _legacyAnimations.AddRange(legacyAnimations);

        _lockedTransforms.Clear();
        _lockedPosePrepared = false;
        _baselineCaptured = true;
        _reportedLocked = false;
        _reportedReleased = false;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 3 HARD BLOCKER TARGETS captured: " +
            $"cover='{BuildHierarchy(coverRoot)}' active={_baselineCoverActive}; " +
            $"visuals='{BuildHierarchy(visualsRoot)}' transforms={_baselineTransforms.Count} " +
            $"renderers={_baselineRenderers.Count} animators={_animators.Count} legacyAnimations={_legacyAnimations.Count}; " +
            $"entrance='{BuildHierarchy(entranceRoot)}' active={_baselineEntranceActive}.");
    }

    private void ApplyHardBlock()
    {
        try
        {
            if (_entranceRoot != null && _entranceRoot.gameObject.activeSelf)
                _entranceRoot.gameObject.SetActive(false);

            if (_coverRoot != null && !_coverRoot.gameObject.activeSelf)
                _coverRoot.gameObject.SetActive(true);

            if (_visualsRoot != null && !_visualsRoot.gameObject.activeSelf)
                _visualsRoot.gameObject.SetActive(true);

            if (!_lockedPosePrepared)
                PrepareLockedPose();

            // Keep all cover objects/renderers present and freeze the transform
            // hierarchy at the animation's default/rebound pose.
            for (int i = 0; i < _lockedTransforms.Count; i++)
            {
                VisualTransformState state = _lockedTransforms[i];
                Transform tr = state.Transform;

                if (tr == null || tr.gameObject == null)
                    continue;

                if (!tr.gameObject.activeSelf)
                    tr.gameObject.SetActive(true);

                tr.localPosition = state.LocalPosition;
                tr.localRotation = state.LocalRotation;
                tr.localScale = state.LocalScale;
            }

            for (int i = 0; i < _baselineRenderers.Count; i++)
            {
                Renderer renderer = _baselineRenderers[i].Renderer;
                if (renderer != null && !renderer.enabled)
                    renderer.enabled = true;
            }

            _reportedReleased = false;

            if (!_reportedLocked)
            {
                _reportedLocked = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 3 HARD BLOCK ACTIVE: Level_07_Entrance disabled; " +
                    $"MegafierCover animation reset/frozen at default pose ({_lockedTransforms.Count} transforms).");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 3 HARD BLOCK apply failed: {ex.GetBaseException().Message}");
            ClearTargets();
        }
    }

    private void PrepareLockedPose()
    {
        if (_visualsRoot == null)
            return;

        // Ensure the full visual hierarchy exists before asking Unity's
        // animation system to restore its default bound pose.
        foreach (VisualTransformState state in _baselineTransforms)
        {
            if (state.Transform != null &&
                state.Transform.gameObject != null &&
                !state.Transform.gameObject.activeSelf)
            {
                state.Transform.gameObject.SetActive(true);
            }
        }

        foreach (RendererState state in _baselineRenderers)
        {
            if (state.Renderer != null)
                state.Renderer.enabled = true;
        }

        int reboundAnimators = 0;
        int rewoundLegacy = 0;

        for (int i = 0; i < _animators.Count; i++)
        {
            Animator animator = _animators[i].Animator;
            if (animator == null)
                continue;

            try
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = false;
                reboundAnimators++;
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] LEVEL 3 animator reset failed on '{animator.name}': {ex.GetBaseException().Message}");
            }
        }

        for (int i = 0; i < _legacyAnimations.Count; i++)
        {
            Animation animation = _legacyAnimations[i].Animation;
            if (animation == null)
                continue;

            try
            {
                animation.enabled = true;
                animation.Rewind();
                animation.Sample();
                animation.Stop();
                animation.enabled = false;
                rewoundLegacy++;
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] LEVEL 3 legacy animation reset failed on '{animation.name}': {ex.GetBaseException().Message}");
            }
        }

        _lockedTransforms.Clear();

        foreach (Transform tr in _visualsRoot.GetComponentsInChildren<Transform>(true))
        {
            _lockedTransforms.Add(new VisualTransformState
            {
                Transform = tr,
                LocalPosition = tr.localPosition,
                LocalRotation = tr.localRotation,
                LocalScale = tr.localScale,
                ActiveSelf = true
            });
        }

        _lockedPosePrepared = _lockedTransforms.Count > 0;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 3 CLOSED POSE PREPARED: reboundAnimators={reboundAnimators}, " +
            $"rewoundLegacyAnimations={rewoundLegacy}, transforms={_lockedTransforms.Count}.");
    }

    private void ReleaseHardBlock()
    {
        if (!_baselineCaptured)
            return;

        try
        {
            bool vanillaRevealReached = NativeProgression.HasObservedVanillaRequest(RevealFlag);

            if (vanillaRevealReached)
            {
                if (_coverRoot != null && _coverRoot.gameObject.activeSelf)
                    _coverRoot.gameObject.SetActive(false);

                if (_entranceRoot != null && !_entranceRoot.gameObject.activeSelf)
                    _entranceRoot.gameObject.SetActive(true);
            }
            else
            {
                RestoreBaselineState();
            }

            _lockedPosePrepared = false;
            _lockedTransforms.Clear();
            _reportedLocked = false;

            if (!_reportedReleased)
            {
                _reportedReleased = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 3 HARD BLOCK RELEASED: Level 3 Access present; vanillaRevealReached={vanillaRevealReached}.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 3 HARD BLOCK release failed: {ex.GetBaseException().Message}");
            ClearTargets();
        }
    }

    private void RestoreBaselineState()
    {
        for (int i = 0; i < _animators.Count; i++)
        {
            AnimatorState state = _animators[i];
            if (state.Animator != null)
                state.Animator.enabled = state.Enabled;
        }

        for (int i = 0; i < _legacyAnimations.Count; i++)
        {
            LegacyAnimationState state = _legacyAnimations[i];
            if (state.Animation != null)
                state.Animation.enabled = state.Enabled;
        }

        for (int i = 0; i < _baselineRenderers.Count; i++)
        {
            RendererState state = _baselineRenderers[i];
            if (state.Renderer != null)
                state.Renderer.enabled = state.Enabled;
        }

        // Restore transform values before parent active states are changed.
        for (int i = 0; i < _baselineTransforms.Count; i++)
        {
            VisualTransformState state = _baselineTransforms[i];
            Transform tr = state.Transform;

            if (tr == null || tr.gameObject == null)
                continue;

            tr.localPosition = state.LocalPosition;
            tr.localRotation = state.LocalRotation;
            tr.localScale = state.LocalScale;
        }

        for (int i = _baselineTransforms.Count - 1; i >= 0; i--)
        {
            VisualTransformState state = _baselineTransforms[i];
            Transform tr = state.Transform;

            if (tr != null && tr.gameObject != null && tr.gameObject.activeSelf != state.ActiveSelf)
                tr.gameObject.SetActive(state.ActiveSelf);
        }

        if (_coverRoot != null && _coverRoot.gameObject.activeSelf != _baselineCoverActive)
            _coverRoot.gameObject.SetActive(_baselineCoverActive);

        if (_entranceRoot != null && _entranceRoot.gameObject.activeSelf != _baselineEntranceActive)
            _entranceRoot.gameObject.SetActive(_baselineEntranceActive);

        if (_doorCollision != null && _doorCollision.gameObject.activeSelf != _baselineCollisionActive)
            _doorCollision.gameObject.SetActive(_baselineCollisionActive);
    }

    private void ClearTargets()
    {
        _doorRoot = null;
        _coverRoot = null;
        _visualsRoot = null;
        _entranceRoot = null;
        _doorCollision = null;

        _baselineTransforms.Clear();
        _baselineRenderers.Clear();
        _animators.Clear();
        _legacyAnimations.Clear();
        _lockedTransforms.Clear();

        _baselineCaptured = false;
        _lockedPosePrepared = false;
        _reportedLocked = false;
        _reportedReleased = false;
    }

    private static bool SafeActiveSelf(Transform tr, bool fallback)
    {
        try
        {
            return tr != null && tr.gameObject != null ? tr.gameObject.activeSelf : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 24 && current != null; i++)
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




internal sealed class Level4VineKeeper : MonoBehaviour
{
    private sealed class TransformState
    {
        public Transform Transform = null!;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public bool ActiveSelf;
    }

    private sealed class RendererState
    {
        public Renderer Renderer = null!;
        public bool Enabled;
    }

    private sealed class AnimatorState
    {
        public Animator Animator = null!;
        public bool Enabled;
    }

    private sealed class LegacyAnimationState
    {
        public Animation Animation = null!;
        public bool Enabled;
    }

    private const string BossWeedsRootName = "BossWeeds-tofb";
    private const string CoverageName = "BossWeedCoverage";
    private const string AnimatedVinesName = "AnimatedVines";
    private const string EntranceName = "Level_08_Entrance";
    private const string WeedColliderName = "WeedCollider";

    private readonly List<TransformState> _baselineTransforms = new();
    private readonly List<TransformState> _lockedTransforms = new();
    private readonly List<RendererState> _baselineRenderers = new();
    private readonly List<AnimatorState> _animators = new();
    private readonly List<LegacyAnimationState> _legacyAnimations = new();

    private Transform? _bossWeedsRoot;
    private Transform? _coverageRoot;
    private Transform? _animatedVinesRoot;
    private Transform? _entranceRoot;
    private Transform? _weedCollider;

    private bool _baselineCoverageActive;
    private bool _baselineEntranceActive;
    private bool _baselineColliderActive;
    private bool _baselineCaptured;
    private bool _lockedPosePrepared;

    private int _searchCooldown;
    private bool _reportedLocked;
    private bool _reportedReleased;

    public Level4VineKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void LateUpdate()
    {
        if (!DeveloperHarness.IsHub2Current)
            return;

        if (!NativeProgression.RandomizeEarlyProgression)
            return;

        if (!TargetsValid())
        {
            if (_searchCooldown > 0)
            {
                _searchCooldown--;
                return;
            }

            _searchCooldown = 15;
            FindTargets();
        }

        if (!TargetsValid())
            return;

        if (NativeProgression.HasLevel4Access)
        {
            ReleaseHardBlock();
            return;
        }

        ApplyHardBlock();
    }

    private bool TargetsValid()
    {
        try
        {
            return _bossWeedsRoot != null &&
                   _coverageRoot != null &&
                   _animatedVinesRoot != null &&
                   _entranceRoot != null &&
                   _bossWeedsRoot.gameObject != null &&
                   _coverageRoot.gameObject != null &&
                   _animatedVinesRoot.gameObject != null &&
                   _entranceRoot.gameObject != null;
        }
        catch
        {
            return false;
        }
    }

    private void FindTargets()
    {
        Transform? bossRoot = null;
        Transform? entrance = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                string path = BuildHierarchy(tr);

                if (bossRoot == null &&
                    string.Equals(tr.name, BossWeedsRootName, StringComparison.Ordinal) &&
                    path.Contains(
                        "Root/GameRoom_Hub2_Logic/Objects/BossWeeds-tofb",
                        StringComparison.Ordinal))
                {
                    bossRoot = tr;
                }

                if (entrance == null &&
                    string.Equals(tr.name, EntranceName, StringComparison.Ordinal) &&
                    path.Contains(
                        "Root/GameRoom_Hub2_Logic/Objects/LevelDoors/Level_08_Entrance",
                        StringComparison.Ordinal))
                {
                    entrance = tr;
                }

                if (bossRoot != null && entrance != null)
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] LEVEL 4 VINE BLOCKER root search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (bossRoot == null || entrance == null)
            return;

        Transform? coverage = null;
        Transform? animatedVines = null;
        Transform? weedCollider = null;

        try
        {
            foreach (Transform tr in bossRoot.GetComponentsInChildren<Transform>(true))
            {
                string name = tr.name ?? string.Empty;
                string path = BuildHierarchy(tr);

                if (coverage == null &&
                    string.Equals(name, CoverageName, StringComparison.Ordinal))
                {
                    coverage = tr;
                    continue;
                }

                if (animatedVines == null &&
                    string.Equals(name, AnimatedVinesName, StringComparison.Ordinal) &&
                    path.Contains("/BossWeedCoverage/AnimatedVines", StringComparison.Ordinal))
                {
                    animatedVines = tr;
                    continue;
                }

                if (weedCollider == null &&
                    string.Equals(name, WeedColliderName, StringComparison.Ordinal) &&
                    path.Contains("/BossWeedCoverage/WeedCollider", StringComparison.Ordinal))
                {
                    weedCollider = tr;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 VINE BLOCKER child search failed: {ex.GetBaseException().Message}");
            return;
        }

        if (coverage == null || animatedVines == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 VINE BLOCKER incomplete target set: " +
                $"coverageFound={coverage != null} animatedVinesFound={animatedVines != null} " +
                $"weedColliderFound={weedCollider != null}.");
            return;
        }

        var baselineTransforms = new List<TransformState>();
        var renderers = new List<RendererState>();
        var animators = new List<AnimatorState>();
        var legacyAnimations = new List<LegacyAnimationState>();

        try
        {
            foreach (Transform tr in animatedVines.GetComponentsInChildren<Transform>(true))
            {
                baselineTransforms.Add(new TransformState
                {
                    Transform = tr,
                    LocalPosition = tr.localPosition,
                    LocalRotation = tr.localRotation,
                    LocalScale = tr.localScale,
                    ActiveSelf = tr.gameObject.activeSelf
                });
            }

            foreach (Renderer renderer in animatedVines.GetComponentsInChildren<Renderer>(true))
            {
                renderers.Add(new RendererState
                {
                    Renderer = renderer,
                    Enabled = renderer.enabled
                });
            }

            foreach (Animator animator in coverage.GetComponentsInChildren<Animator>(true))
            {
                animators.Add(new AnimatorState
                {
                    Animator = animator,
                    Enabled = animator.enabled
                });
            }

            foreach (Animation animation in coverage.GetComponentsInChildren<Animation>(true))
            {
                legacyAnimations.Add(new LegacyAnimationState
                {
                    Animation = animation,
                    Enabled = animation.enabled
                });
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 VINE BLOCKER baseline capture failed: {ex.GetBaseException().Message}");
            return;
        }

        _bossWeedsRoot = bossRoot;
        _coverageRoot = coverage;
        _animatedVinesRoot = animatedVines;
        _entranceRoot = entrance;
        _weedCollider = weedCollider;

        _baselineCoverageActive = SafeActiveSelf(coverage, true);
        _baselineEntranceActive = SafeActiveSelf(entrance, true);
        _baselineColliderActive =
            weedCollider != null && SafeActiveSelf(weedCollider, true);

        _baselineTransforms.Clear();
        _baselineTransforms.AddRange(baselineTransforms);

        _baselineRenderers.Clear();
        _baselineRenderers.AddRange(renderers);

        _animators.Clear();
        _animators.AddRange(animators);

        _legacyAnimations.Clear();
        _legacyAnimations.AddRange(legacyAnimations);

        _lockedTransforms.Clear();
        _lockedPosePrepared = false;
        _baselineCaptured = true;
        _reportedLocked = false;
        _reportedReleased = false;
        NativeLevel4DoorBridge.ResetForDeveloperRelock();

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 4 VINE BLOCKER TARGETS captured: " +
            $"coverage='{BuildHierarchy(coverage)}' active={_baselineCoverageActive}; " +
            $"animatedVines='{BuildHierarchy(animatedVines)}' transforms={_baselineTransforms.Count} " +
            $"renderers={_baselineRenderers.Count} animators={_animators.Count} legacyAnimations={_legacyAnimations.Count}; " +
            $"weedCollider='{(weedCollider != null ? BuildHierarchy(weedCollider) : "<not-found>")}' active={_baselineColliderActive}; " +
            $"entrance='{BuildHierarchy(entrance)}' active={_baselineEntranceActive}.");
    }

    private void ApplyHardBlock()
    {
        try
        {
                NativeLevel4DoorBridge.ResetForDeveloperRelock();

    
            // Functional lock. F12 disables the entire BossWeeds-tofb root,
            // so F7 must restore that root before the coverage/collider
            // children can become active in the hierarchy again.
            if (_entranceRoot != null && _entranceRoot.gameObject.activeSelf)
                _entranceRoot.gameObject.SetActive(false);

            if (_bossWeedsRoot != null && !_bossWeedsRoot.gameObject.activeSelf)
                _bossWeedsRoot.gameObject.SetActive(true);

            if (_coverageRoot != null && !_coverageRoot.gameObject.activeSelf)
                _coverageRoot.gameObject.SetActive(true);

            if (_animatedVinesRoot != null && !_animatedVinesRoot.gameObject.activeSelf)
                _animatedVinesRoot.gameObject.SetActive(true);

            if (_weedCollider != null && !_weedCollider.gameObject.activeSelf)
                _weedCollider.gameObject.SetActive(true);

            if (!_lockedPosePrepared)
            {
                PrepareLockedPose();
            }
            else if (!_reportedLocked)
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] LEVEL 4 VINE BLOCK: reusing cached closed pose ({_lockedTransforms.Count} transforms); no animator rebind needed.");
            }

            for (int i = 0; i < _lockedTransforms.Count; i++)
            {
                TransformState state = _lockedTransforms[i];
                Transform tr = state.Transform;

                if (tr == null || tr.gameObject == null)
                    continue;

                if (!tr.gameObject.activeSelf)
                    tr.gameObject.SetActive(true);

                tr.localPosition = state.LocalPosition;
                tr.localRotation = state.LocalRotation;
                tr.localScale = state.LocalScale;
            }

            for (int i = 0; i < _baselineRenderers.Count; i++)
            {
                Renderer renderer = _baselineRenderers[i].Renderer;
                if (renderer != null && !renderer.enabled)
                    renderer.enabled = true;
            }

            _reportedReleased = false;

            if (!_reportedLocked)
            {
                _reportedLocked = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] LEVEL 4 VINE BLOCK ACTIVE: Level_08_Entrance disabled; " +
                    $"BossWeeds-tofb root + BossWeedCoverage + WeedCollider active; cached thick vine pose enforced ({_lockedTransforms.Count} transforms).");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 VINE BLOCK apply failed: {ex.GetBaseException().Message}");
            ClearTargets();
        }
    }

    private void PrepareLockedPose()
    {
        if (_animatedVinesRoot == null)
            return;

        foreach (TransformState state in _baselineTransforms)
        {
            if (state.Transform != null &&
                state.Transform.gameObject != null &&
                !state.Transform.gameObject.activeSelf)
            {
                state.Transform.gameObject.SetActive(true);
            }
        }

        foreach (RendererState state in _baselineRenderers)
        {
            if (state.Renderer != null)
                state.Renderer.enabled = true;
        }

        int reboundAnimators = 0;
        int rewoundLegacy = 0;

        for (int i = 0; i < _animators.Count; i++)
        {
            Animator animator = _animators[i].Animator;
            if (animator == null)
                continue;

            try
            {
                animator.enabled = true;
                animator.Rebind();
                animator.Update(0f);
                animator.enabled = false;
                reboundAnimators++;
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] LEVEL 4 vine animator reset failed on '{animator.name}': {ex.GetBaseException().Message}");
            }
        }

        for (int i = 0; i < _legacyAnimations.Count; i++)
        {
            Animation animation = _legacyAnimations[i].Animation;
            if (animation == null)
                continue;

            try
            {
                animation.enabled = true;
                animation.Rewind();
                animation.Sample();
                animation.Stop();
                animation.enabled = false;
                rewoundLegacy++;
            }
            catch (Exception ex)
            {
                Plugin.LoggerInstance?.LogDebug(
                    $"[SCRC-AP] LEVEL 4 vine legacy animation reset failed on '{animation.name}': {ex.GetBaseException().Message}");
            }
        }

        _lockedTransforms.Clear();

        foreach (Transform tr in _animatedVinesRoot.GetComponentsInChildren<Transform>(true))
        {
            _lockedTransforms.Add(new TransformState
            {
                Transform = tr,
                LocalPosition = tr.localPosition,
                LocalRotation = tr.localRotation,
                LocalScale = tr.localScale,
                ActiveSelf = true
            });
        }

        _lockedPosePrepared = _lockedTransforms.Count > 0;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 4 VINE CLOSED POSE PREPARED: reboundAnimators={reboundAnimators}, " +
            $"rewoundLegacyAnimations={rewoundLegacy}, transforms={_lockedTransforms.Count}.");
    }

    private void ReleaseHardBlock()
    {
        if (!_baselineCaptured)
            return;

        try
        {
            // v0.32.2 hid BossWeedCoverage and its named WeedCollider, but the
            // game still behaved as though the vines were present. BossWeeds-
            // tofb also owns sibling dialogue/interaction logic, so remove the
            // entire blocker root from Unity's active hierarchy instead of
            // trying to identify each hidden condition/collider individually.
            //
            // Level_08_Entrance is a separate object under Objects/LevelDoors,
            // so it can remain active while BossWeeds-tofb is disabled.
            if (_bossWeedsRoot != null && _bossWeedsRoot.gameObject.activeSelf)
                _bossWeedsRoot.gameObject.SetActive(false);

            if (_entranceRoot != null && !_entranceRoot.gameObject.activeSelf)
                _entranceRoot.gameObject.SetActive(true);

            // Keep the exact first thick-vine pose cached for later F7 relocks.
            _reportedLocked = false;

            if (!_reportedReleased)
            {
                _reportedReleased = true;
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] LEVEL 4 VINE BLOCK RELEASED: Level 4 Access present; BossWeeds-tofb disabled and Level_08_Entrance enabled. IsInteractionEnabled override will control vanilla availability.");
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 VINE BLOCK release failed: {ex.GetBaseException().Message}");
            ClearTargets();
        }
    }

    private void RestoreBaselineState()
    {
        for (int i = 0; i < _animators.Count; i++)
        {
            AnimatorState state = _animators[i];
            if (state.Animator != null)
                state.Animator.enabled = state.Enabled;
        }

        for (int i = 0; i < _legacyAnimations.Count; i++)
        {
            LegacyAnimationState state = _legacyAnimations[i];
            if (state.Animation != null)
                state.Animation.enabled = state.Enabled;
        }

        for (int i = 0; i < _baselineRenderers.Count; i++)
        {
            RendererState state = _baselineRenderers[i];
            if (state.Renderer != null)
                state.Renderer.enabled = state.Enabled;
        }

        for (int i = 0; i < _baselineTransforms.Count; i++)
        {
            TransformState state = _baselineTransforms[i];
            Transform tr = state.Transform;

            if (tr == null || tr.gameObject == null)
                continue;

            tr.localPosition = state.LocalPosition;
            tr.localRotation = state.LocalRotation;
            tr.localScale = state.LocalScale;
        }

        for (int i = _baselineTransforms.Count - 1; i >= 0; i--)
        {
            TransformState state = _baselineTransforms[i];
            Transform tr = state.Transform;

            if (tr != null &&
                tr.gameObject != null &&
                tr.gameObject.activeSelf != state.ActiveSelf)
            {
                tr.gameObject.SetActive(state.ActiveSelf);
            }
        }

        if (_coverageRoot != null &&
            _coverageRoot.gameObject.activeSelf != _baselineCoverageActive)
        {
            _coverageRoot.gameObject.SetActive(_baselineCoverageActive);
        }

        if (_entranceRoot != null &&
            _entranceRoot.gameObject.activeSelf != _baselineEntranceActive)
        {
            _entranceRoot.gameObject.SetActive(_baselineEntranceActive);
        }

        if (_weedCollider != null &&
            _weedCollider.gameObject.activeSelf != _baselineColliderActive)
        {
            _weedCollider.gameObject.SetActive(_baselineColliderActive);
        }
    }

    private void ClearTargets()
    {
        _bossWeedsRoot = null;
        _coverageRoot = null;
        _animatedVinesRoot = null;
        _entranceRoot = null;
        _weedCollider = null;

        _baselineTransforms.Clear();
        _lockedTransforms.Clear();
        _baselineRenderers.Clear();
        _animators.Clear();
        _legacyAnimations.Clear();

        _baselineCaptured = false;
        _lockedPosePrepared = false;
        _reportedLocked = false;
        _reportedReleased = false;
    }

    private static bool SafeActiveSelf(Transform tr, bool fallback)
    {
        try
        {
            return tr != null && tr.gameObject != null
                ? tr.gameObject.activeSelf
                : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 28 && current != null; i++)
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




internal static class NativeLevel4DoorBridge
{
    private const string NativeLibrary = "GameAssembly.dll";
    private const string DoorComponentName = "LevelEntranceDoor";
    private const string RecordInterestMethodName =
        "ManagedInteractiveEntity.RecordInteractionInterest";

    private static IntPtr _doorObjectPointer;
    private static IntPtr _doorClass;

    private static IntPtr _interactionEnabledCodePointer;
    private static byte[]? _interactionEnabledOriginalBytes;
    private static bool _interactionEnabledNativePatched;

    private static bool _interactionSessionStarted;
    private static bool _reportedPatchActive;
    private static bool _reportedInterestActive;
    private static bool _reportedFailure;
    private static bool _reportedTransientInterestFailure;

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_object_get_class(IntPtr obj);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_method_from_name(
        IntPtr klass,
        [MarshalAs(UnmanagedType.LPStr)] string name,
        int argsCount);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_class_get_parent(IntPtr klass);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_runtime_invoke(
        IntPtr method,
        IntPtr obj,
        IntPtr parameters,
        out IntPtr exception);

    [DllImport(NativeLibrary, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr il2cpp_method_get_pointer(IntPtr method);

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

    public static void ResetForDeveloperRelock()
    {
        RestoreInteractionEnabledNativePatch();

        _doorObjectPointer = IntPtr.Zero;
        _doorClass = IntPtr.Zero;
        _interactionSessionStarted = false;
        _reportedPatchActive = false;
        _reportedInterestActive = false;
        _reportedFailure = false;
        _reportedTransientInterestFailure = false;
    }

    public static bool TryEnableVanillaInteraction(GameObject entranceRoot)
    {
        if (_interactionEnabledNativePatched)
            return true;

        try
        {
            if (!EnsureDoorPointer(entranceRoot))
            {
                ReportFailureOnce(
                    "could not resolve the native LevelEntranceDoor instance");
                return false;
            }

            IntPtr method = FindMethodInHierarchy(
                _doorClass,
                "IsInteractionEnabled",
                0);

            if (method == IntPtr.Zero)
            {
                ReportFailureOnce(
                    "native LevelEntranceDoor.IsInteractionEnabled() was not found");
                return false;
            }

            IntPtr codePointer;

            try
            {
                codePointer = il2cpp_method_get_pointer(method);
            }
            catch (EntryPointNotFoundException)
            {
                // Unity 2021 IL2CPP MethodInfo starts with the executable
                // method pointer on this game. This fallback was verified
                // during development but is used only when the export is absent.
                codePointer = Marshal.ReadIntPtr(method);
            }

            if (codePointer == IntPtr.Zero)
            {
                ReportFailureOnce(
                    "native IsInteractionEnabled() code pointer was null");
                return false;
            }

            // Windows x64: mov eax, 1 ; ret
            // LevelEntranceDoor.IsInteractionEnabled() returns a bool in AL/EAX.
            byte[] forceTrue =
            {
                0xB8, 0x01, 0x00, 0x00, 0x00,
                0xC3
            };

            byte[] original = new byte[forceTrue.Length];
            Marshal.Copy(codePointer, original, 0, original.Length);

            const uint PAGE_EXECUTE_READWRITE = 0x40;

            if (!VirtualProtect(
                    codePointer,
                    (UIntPtr)forceTrue.Length,
                    PAGE_EXECUTE_READWRITE,
                    out uint oldProtect))
            {
                ReportFailureOnce(
                    $"VirtualProtect failed at 0x{codePointer.ToInt64():X}; Win32={Marshal.GetLastWin32Error()}");
                return false;
            }

            try
            {
                Marshal.Copy(
                    forceTrue,
                    0,
                    codePointer,
                    forceTrue.Length);

                FlushInstructionCache(
                    GetCurrentProcess(),
                    codePointer,
                    (UIntPtr)forceTrue.Length);
            }
            finally
            {
                VirtualProtect(
                    codePointer,
                    (UIntPtr)forceTrue.Length,
                    oldProtect,
                    out _);
            }

            _interactionEnabledCodePointer = codePointer;
            _interactionEnabledOriginalBytes = original;
            _interactionEnabledNativePatched = true;

            if (!_reportedPatchActive)
            {
                _reportedPatchActive = true;
                Plugin.LoggerInstance?.LogWarning(
                    "[SCRC-AP] LEVEL 4 VANILLA INTERACTION ACTIVE: temporarily enabling LevelEntranceDoor.IsInteractionEnabled() while the player is at the Level 4 mat.");
            }

            return true;
        }
        catch (Exception ex)
        {
            ReportFailureOnce(
                $"native interaction enable failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    public static void RestoreInteractionEnabledNativePatch()
    {
        if (!_interactionEnabledNativePatched)
        {
            _interactionSessionStarted = false;
            _reportedInterestActive = false;
            return;
        }

        IntPtr codePointer = _interactionEnabledCodePointer;
        byte[]? original = _interactionEnabledOriginalBytes;

        try
        {
            if (codePointer != IntPtr.Zero &&
                original != null &&
                original.Length > 0)
            {
                const uint PAGE_EXECUTE_READWRITE = 0x40;

                if (VirtualProtect(
                        codePointer,
                        (UIntPtr)original.Length,
                        PAGE_EXECUTE_READWRITE,
                        out uint oldProtect))
                {
                    try
                    {
                        Marshal.Copy(
                            original,
                            0,
                            codePointer,
                            original.Length);

                        FlushInstructionCache(
                            GetCurrentProcess(),
                            codePointer,
                            (UIntPtr)original.Length);
                    }
                    finally
                    {
                        VirtualProtect(
                            codePointer,
                            (UIntPtr)original.Length,
                            oldProtect,
                            out _);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] LEVEL 4 interaction restore failed: {ex.GetBaseException().Message}");
        }
        finally
        {
            _interactionEnabledNativePatched = false;
            _interactionEnabledCodePointer = IntPtr.Zero;
            _interactionEnabledOriginalBytes = null;
            _interactionSessionStarted = false;
            _reportedInterestActive = false;
            _reportedTransientInterestFailure = false;
        }
    }

    public static bool TryRegisterVanillaInteractionInterest(
        GameObject entranceRoot)
    {
        try
        {
            if (!EnsureDoorPointer(entranceRoot))
                return false;

            if (!_interactionSessionStarted)
            {
                IntPtr sessionMethod = FindMethodInHierarchy(
                    _doorClass,
                    "OnNewInteractionInterestSession",
                    0);

                if (sessionMethod == IntPtr.Zero)
                {
                    ReportFailureOnce(
                        "LevelEntranceDoor.OnNewInteractionInterestSession() was not found");
                    return false;
                }

                IntPtr sessionException;
                il2cpp_runtime_invoke(
                    sessionMethod,
                    _doorObjectPointer,
                    IntPtr.Zero,
                    out sessionException);

                if (sessionException != IntPtr.Zero)
                {
                    _interactionSessionStarted = false;
                    ReportTransientInterestFailureOnce(
                        $"OnNewInteractionInterestSession() returned IL2CPP exception 0x{sessionException.ToInt64():X}");
                    return false;
                }

                _interactionSessionStarted = true;
            }

            IntPtr interestMethod = FindMethodInHierarchy(
                _doorClass,
                RecordInterestMethodName,
                0);

            if (interestMethod == IntPtr.Zero)
            {
                // Retain the simple-name fallback for interop variations.
                interestMethod = FindMethodInHierarchy(
                    _doorClass,
                    "RecordInteractionInterest",
                    0);
            }

            if (interestMethod == IntPtr.Zero)
            {
                ReportFailureOnce(
                    "LevelEntranceDoor interaction-interest method was not found");
                return false;
            }

            IntPtr interestException;
            il2cpp_runtime_invoke(
                interestMethod,
                _doorObjectPointer,
                IntPtr.Zero,
                out interestException);

            if (interestException != IntPtr.Zero)
            {
                _interactionSessionStarted = false;
                ReportTransientInterestFailureOnce(
                    $"RecordInteractionInterest() returned IL2CPP exception 0x{interestException.ToInt64():X}");
                return false;
            }

            _reportedTransientInterestFailure = false;

            if (!_reportedInterestActive)
            {
                _reportedInterestActive = true;
                Plugin.LoggerInstance?.LogInfo(
                    "[SCRC-AP] LEVEL 4 VANILLA INDICATOR PIPELINE ACTIVE: registered the real LevelEntranceDoor with the game's interaction-interest system.");
            }

            return true;
        }
        catch (Exception ex)
        {
            ReportFailureOnce(
                $"vanilla interaction-interest registration failed: {ex.GetBaseException().Message}");
            return false;
        }
    }

    private static bool EnsureDoorPointer(GameObject entranceRoot)
    {
        if (_doorObjectPointer != IntPtr.Zero &&
            _doorClass != IntPtr.Zero)
        {
            return true;
        }

        object? rawComponent = FindComponentByNativeName(
            entranceRoot,
            DoorComponentName);

        if (rawComponent == null)
            return false;

        IntPtr pointer = GetIl2CppPointer(rawComponent);
        if (pointer == IntPtr.Zero)
            return false;

        IntPtr klass = il2cpp_object_get_class(pointer);
        if (klass == IntPtr.Zero)
            return false;

        _doorObjectPointer = pointer;
        _doorClass = klass;
        return true;
    }

    private static IntPtr FindMethodInHierarchy(
        IntPtr klass,
        string methodName,
        int argCount)
    {
        IntPtr current = klass;

        for (int depth = 0;
             current != IntPtr.Zero && depth < 12;
             depth++)
        {
            IntPtr method = il2cpp_class_get_method_from_name(
                current,
                methodName,
                argCount);

            if (method != IntPtr.Zero)
                return method;

            current = il2cpp_class_get_parent(current);
        }

        return IntPtr.Zero;
    }

    private static object? FindComponentByNativeName(
        GameObject root,
        string componentName)
    {
        MethodInfo? getByName = typeof(GameObject).GetMethod(
            "GetComponent",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(string) },
            modifiers: null);

        if (getByName == null)
            return null;

        object? direct = getByName.Invoke(
            root,
            new object[] { componentName });

        if (direct != null)
            return direct;

        foreach (Transform tr in root.transform.GetComponentsInChildren<Transform>(true))
        {
            if (tr == null || tr.gameObject == null)
                continue;

            object? found = getByName.Invoke(
                tr.gameObject,
                new object[] { componentName });

            if (found != null)
                return found;
        }

        return null;
    }

    private static IntPtr GetIl2CppPointer(object value)
    {
        try
        {
            PropertyInfo? property = value.GetType().GetProperty(
                "Pointer",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Instance);

            return property?.GetValue(value) is IntPtr pointer
                ? pointer
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    private static void ReportTransientInterestFailureOnce(string message)
    {
        if (_reportedTransientInterestFailure)
            return;

        _reportedTransientInterestFailure = true;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 4 VANILLA INTERACTION RETRY: {message}. The bridge will start a fresh native interaction session and retry.");
    }

    private static void ReportFailureOnce(string message)
    {
        if (_reportedFailure)
            return;

        _reportedFailure = true;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] LEVEL 4 VANILLA ENTRANCE BRIDGE FAILED: {message}.");
    }
}


internal sealed class Level4EntranceProxy : MonoBehaviour
{
    private const string EntranceName = "Level_08_Entrance";
    private const float TriggerDistance = 1.20f;
    private const float PlayerSearchRadius = 8.0f;

    private Transform? _entranceRoot;
    private Transform? _triggerPoint;
    private Transform? _playerMarker;

    private int _entranceSearchCooldown;
    private int _playerSearchCooldown;

    private bool _reportedReady;
    private bool _reportedPlayer;
    private bool _reportedInRange;

    private int _nativePatchArmedFrame = -1;
    private int _interactionRetryFrame;

    public Level4EntranceProxy(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        if (!DeveloperHarness.IsHub2Current)
        {
            SuspendOutsideHub();
            return;
        }

        if (!NativeProgression.RandomizeEarlyProgression ||
            !NativeProgression.HasLevel4Access)
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
            _nativePatchArmedFrame = -1;
            _interactionRetryFrame = 0;
            _reportedInRange = false;
            return;
        }

        if (!EntranceValid())
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();

            if (_entranceSearchCooldown > 0)
            {
                _entranceSearchCooldown--;
                return;
            }

            _entranceSearchCooldown = 30;
            FindEntrance();
        }

        if (!EntranceValid())
            return;

        if (!PlayerValid())
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();

            if (_playerSearchCooldown > 0)
            {
                _playerSearchCooldown--;
                return;
            }

            _playerSearchCooldown = 20;
            FindPlayerMarker();
        }

        if (!PlayerValid())
            return;

        Transform trigger = _triggerPoint ?? _entranceRoot!;
        float distance;

        try
        {
            distance = Vector3.Distance(
                _playerMarker!.position,
                trigger.position);
        }
        catch
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
            _playerMarker = null;
            return;
        }

        if (distance > TriggerDistance)
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
            _nativePatchArmedFrame = -1;
            _interactionRetryFrame = 0;
            _reportedInRange = false;
            return;
        }

        if (!_reportedInRange)
        {
            _reportedInRange = true;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 4 VANILLA ENTRANCE RANGE: player is {distance:0.###} from the Level 4 mat.");
        }

        if (_entranceRoot == null)
            return;

        bool interactionEnabled =
            NativeLevel4DoorBridge.TryEnableVanillaInteraction(
                _entranceRoot.gameObject);

        if (!interactionEnabled)
        {
            _nativePatchArmedFrame = -1;
            return;
        }

        // Do not invoke the inherited interaction-interest callbacks on the
        // exact frame that the native IsInteractionEnabled machine code is
        // changed. Give the game's own InteractiveObject update one frame to
        // observe the enabled state first.
        if (_nativePatchArmedFrame < 0)
        {
            _nativePatchArmedFrame = Time.frameCount;
            _interactionRetryFrame = Time.frameCount + 1;
            return;
        }

        if (Time.frameCount <= _nativePatchArmedFrame ||
            Time.frameCount < _interactionRetryFrame)
        {
            return;
        }

        bool registered =
            NativeLevel4DoorBridge.TryRegisterVanillaInteractionInterest(
                _entranceRoot.gameObject);

        if (!registered)
        {
            // A native exception can be transient while the interaction
            // manager is changing session state. Retry a fresh session on a
            // later frame instead of permanently disabling the door.
            _interactionRetryFrame = Time.frameCount + 2;
        }
    }

    private void SuspendOutsideHub()
    {
        NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();

        _entranceRoot = null;
        _triggerPoint = null;
        _playerMarker = null;

        _entranceSearchCooldown = 0;
        _playerSearchCooldown = 0;

        _reportedReady = false;
        _reportedPlayer = false;
        _reportedInRange = false;

        _nativePatchArmedFrame = -1;
        _interactionRetryFrame = 0;
    }

    private void OnDisable()
    {
        NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
    }

    private void OnDestroy()
    {
        NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
    }

    private bool EntranceValid()
    {
        try
        {
            return _entranceRoot != null &&
                   _entranceRoot.gameObject != null &&
                   _entranceRoot.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private bool PlayerValid()
    {
        try
        {
            return _playerMarker != null &&
                   _playerMarker.gameObject != null &&
                   _playerMarker.gameObject.activeInHierarchy;
        }
        catch
        {
            return false;
        }
    }

    private void FindEntrance()
    {
        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                if (!string.Equals(
                        tr.name,
                        EntranceName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                string path = BuildHierarchy(tr);

                if (!path.Contains(
                        "Root/GameRoom_Hub2_Logic/Objects/LevelDoors/Level_08_Entrance",
                        StringComparison.Ordinal))
                {
                    continue;
                }

                _entranceRoot = tr;
                _triggerPoint = FindDoorwayMatPoint(tr);
                _playerMarker = null;
                _reportedPlayer = false;
                _reportedInRange = false;

                if (!_reportedReady)
                {
                    _reportedReady = true;
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] LEVEL 4 VANILLA ENTRANCE READY: entrance='{path}', " +
                        $"trigger='{BuildHierarchy(_triggerPoint ?? tr)}', radius={TriggerDistance:0.##}.");
                }

                return;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 4 vanilla entrance search failed: {ex.GetBaseException().Message}");
        }
    }

    private static Transform? FindDoorwayMatPoint(Transform entrance)
    {
        Transform? best = null;

        try
        {
            foreach (Transform tr in entrance.GetComponentsInChildren<Transform>(true))
            {
                string name = tr.name ?? string.Empty;

                if (name.Contains(
                        "MatCollision",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return tr;
                }

                if (best == null &&
                    string.Equals(
                        name,
                        "Mat",
                        StringComparison.OrdinalIgnoreCase))
                {
                    best = tr;
                }
            }
        }
        catch
        {
        }

        return best ?? entrance;
    }

    private void FindPlayerMarker()
    {
        if (_entranceRoot == null)
            return;

        Transform? exactPlayerRoot = null;
        Transform? fallbackPlayerChild = null;

        try
        {
            foreach (Transform tr in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (tr == null || tr.gameObject == null)
                    continue;

                if (!tr.gameObject.activeInHierarchy)
                    continue;

                string name = tr.name ?? string.Empty;
                string path = BuildHierarchy(tr);
                string lowerName = name.ToLowerInvariant();
                string lowerPath = path.ToLowerInvariant();

                // The old loose "player" matcher could latch onto:
                // GameRoomSystems/CollisionDetectionSlaves/PlayerRestrictionRing
                // which is not the moving player. Require the actual spawned
                // PlayerCharacter hierarchy.
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
                    exactPlayerRoot = tr;
                    break;
                }

                fallbackPlayerChild ??= tr;
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogDebug(
                $"[SCRC-AP] Level 4 player-marker search failed: {ex.GetBaseException().Message}");
            return;
        }

        Transform? chosen = exactPlayerRoot ?? fallbackPlayerChild;

        if (chosen == null)
            return;

        _playerMarker = chosen;

        float distance = -1f;
        try
        {
            Transform trigger = _triggerPoint ?? _entranceRoot;
            distance = Vector3.Distance(chosen.position, trigger.position);
        }
        catch
        {
        }

        if (!_reportedPlayer)
        {
            _reportedPlayer = true;
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] LEVEL 4 player marker='{BuildHierarchy(chosen)}' " +
                $"distance={(distance >= 0f ? distance.ToString("0.###") : "<unknown>")}.");
        }
    }

    private static bool LooksPlayerLike(
        string name,
        string path)
    {
        string lowerPath = path.ToLowerInvariant();

        return lowerPath.Contains(
                   "/spawnedentitycontainer/playercharacter",
                   StringComparison.Ordinal) &&
               !lowerPath.Contains(
                   "/collisiondetectionslaves/",
                   StringComparison.Ordinal);
    }

    private static string BuildHierarchy(Transform tr)
    {
        try
        {
            var names = new List<string>();
            Transform? current = tr;

            for (int i = 0; i < 24 && current != null; i++)
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

    private static bool _level4ProxyTransitionRequested;
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

        PhoneBoothDiagnostic.RecordObservedTransition(
            observedRoom,
            SafeSimpleValue(transitionType),
            SafeSimpleValue(spawnPoint),
            SafeSimpleValue(partOfLoadValue));

        Level5Discovery.RecordTransition(observedRoom);
        Level6Discovery.RecordTransition(observedRoom);
        Level8Discovery.RecordTransition(observedRoom);
        MusicLabDiscovery.RecordTransition(observedRoom);

        lock (Sync)
        {
            _currentRoomId = observedRoom;

            if (string.Equals(observedRoom, HubRoomId, StringComparison.Ordinal))
                _level4ProxyTransitionRequested = false;
            else if (string.Equals(observedRoom, Level4RoomId, StringComparison.Ordinal))
                _level4ProxyTransitionRequested = true;
        }
        BottomHudDiagnostic.OnRoomTransition(observedRoom);
        CassetteReceiptRandomization.OnLifecyclePoint(
            $"room transition to {observedRoom}");

        // IsInteractionEnabled() is patched at the native machine-code level
        // while the player stands on the Level 4 mat. Restore it synchronously
        // as soon as any transition leaves Hub2 so no part of the destination
        // room ever runs with the temporary global method patch.
        if (!string.Equals(
                observedRoom,
                HubRoomId,
                StringComparison.Ordinal))
        {
            NativeLevel4DoorBridge.RestoreInteractionEnabledNativePatch();
        }

        // Levels 6-13 share the same native LevelEntranceDoor method and
        // temporarily force it false only near their own locked entrance.
        // Every room transition restores the original bytes synchronously;
        // destination-room keepers can safely reacquire their own owner after
        // the new scene is active. This prevents Hub1A and Hub1B locks from
        // leaking into one another or into gameplay rooms.
        NativeLevel6DoorBridge.ResetForSceneChange();
        BunkerStarRequirementKeeper.ResetForSceneChange();
        Hub1EntranceLookup.ResetForSceneChange();
        Hub3EntranceLookup.ResetForSceneChange();
        Hub7EntranceLookup.ResetForSceneChange();
        Hub4EntranceLookup.ResetForSceneChange();

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

    public static bool TryEnterLevel4FromDoorProxy()
    {
        if (!NativeProgression.HasLevel4Access)
            return false;

        lock (Sync)
        {
            if (_level4ProxyTransitionRequested)
                return true;

            _level4ProxyTransitionRequested = true;
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV DIRECT LEVEL 4 TRANSITION: requesting GameRoom_08 through GameFlowRequestProcessor.");

        if (TransitionToRoom(Level4RoomId, partOfLoadLevel: true))
            return true;

        lock (Sync)
            _level4ProxyTransitionRequested = false;

        return false;
    }

    public static void GrantLevel9Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F2: simulating local receipt of 'Level 9 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 9 Access");
    }

    public static void ResetLevel9Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+SHIFT+F2: resetting Level 9 Access only.");

        NativeProgression.DeveloperResetLevel9Access();
    }

    public static void GrantLevel18Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F11: simulating local receipt of 'Level 18 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 18 Access");
    }

    public static void ResetLevel18Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F11: resetting Level 18 Access only.");

        NativeProgression.DeveloperResetLevel18Access();
    }

    public static void GrantLevel19Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F12: simulating local receipt of 'Level 19 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 19 Access");
    }

    public static void ResetLevel19Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F12: resetting Level 19 Access only.");

        NativeProgression.DeveloperResetLevel19Access();
    }

    public static void GrantLevel22Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+SHIFT+F10: simulating local receipt of 'Level 22 Access'.");

        NativeProgression.ApplyArchipelagoItem(
            "Level 22 Access");
    }

    public static void ResetLevel22Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+SHIFT+F10: resetting Level 22 Access only.");

        NativeProgression.DeveloperResetLevel22Access();
    }

    public static void GrantLevel21Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+SHIFT+F11: simulating local receipt of 'Level 21 Access'.");

        NativeProgression.ApplyArchipelagoItem(
            "Level 21 Access");
    }

    public static void ResetLevel21Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+SHIFT+F11: resetting Level 21 Access only.");

        NativeProgression.DeveloperResetLevel21Access();
    }

    public static void GrantLevel20Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+SHIFT+F12: simulating local receipt of 'Level 20 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 20 Access");
    }

    public static void ResetLevel20Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+SHIFT+F12: resetting Level 20 Access only.");

        NativeProgression.DeveloperResetLevel20Access();
    }

    public static void GrantLevel17Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F10: simulating local receipt of 'Level 17 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 17 Access");
    }

    public static void ResetLevel17Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F10: resetting Level 17 Access only.");

        NativeProgression.DeveloperResetLevel17Access();
    }

    public static void GrantLevel16Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F9: simulating local receipt of 'Level 16 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 16 Access");
    }

    public static void ResetLevel16Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F9: resetting Level 16 Access only.");

        NativeProgression.DeveloperResetLevel16Access();
    }

    public static void GrantLevel15Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F8: simulating local receipt of 'Level 15 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 15 Access");
    }

    public static void ResetLevel15Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F8: resetting Level 15 Access only.");

        NativeProgression.DeveloperResetLevel15Access();
    }

    public static void GrantLevel14Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F7: simulating local receipt of 'Level 14 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 14 Access");
    }

    public static void ResetLevel14Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F7: resetting Level 14 Access only.");

        NativeProgression.DeveloperResetLevel14Access();
    }

    public static void GrantLevel13Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F6: simulating local receipt of 'Level 13 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 13 Access");
    }

    public static void ResetLevel13Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F6: resetting Level 13 Access only.");

        NativeProgression.DeveloperResetLevel13Access();
    }

    public static void GrantLevel12Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F5: simulating local receipt of 'Level 12 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 12 Access");
    }

    public static void ResetLevel12Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F5: resetting Level 12 Access only.");

        NativeProgression.DeveloperResetLevel12Access();
    }

    public static void GrantLevel11Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F3: simulating local receipt of 'Level 11 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 11 Access");
    }

    public static void ResetLevel11Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F3: resetting Level 11 Access only.");

        NativeProgression.DeveloperResetLevel11Access();
    }

    public static void GrantLevel10Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F2: simulating local receipt of 'Level 10 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 10 Access");
    }

    public static void ResetLevel10Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+SHIFT+F2: resetting Level 10 Access only.");

        NativeProgression.DeveloperResetLevel10Access();
    }

    public static void GrantLevel8Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+F1: simulating local receipt of 'Level 8 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 8 Access");
    }

    public static void ResetLevel8Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV ALT+SHIFT+F1: resetting Level 8 Access only.");

        NativeProgression.DeveloperResetLevel8Access();
    }

    public static void GrantLevel7Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+F1: simulating local receipt of 'Level 7 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 7 Access");
    }

    public static void ResetLevel7Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV CTRL+SHIFT+F1: resetting Level 7 Access only.");

        NativeProgression.DeveloperResetLevel7Access();
    }

    public static void GrantLevel6Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F1: simulating local receipt of 'Level 6 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 6 Access");
    }

    public static void ResetLevel6Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV SHIFT+F1: resetting Level 6 Access only.");

        NativeProgression.DeveloperResetLevel6Access();
    }

    public static void ResetLevel5Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F2: resetting Level 5 Access only.");

        NativeProgression.DeveloperResetLevel5Access();

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F2 COMPLETE: Level 5 permission reset. Vanilla Hip Glasses / Chicken Bucket / bucket-minion / King lift-chat progress remains unchanged.");
    }

    public static void GrantLevel5Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F3: simulating local receipt of 'Level 5 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 5 Access");
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

    public static void ForceEnterLevel4()
    {
        if (!Enabled)
            return;

        if (!NativeProgression.HasLevel4Access)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] DEV F6 refused: Level 4 Access is not currently granted. Press F12 first.");
            return;
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F6: invoking the production Level 4 entrance transition path.");

        if (!TryEnterLevel4FromDoorProxy())
        {
            Plugin.LoggerInstance?.LogError(
                "[SCRC-AP] DEV F6 FAILED: production Level 4 transition path could not request GameRoom_08.");
        }
    }

    public static void ResetAndReplayPostLevel1Hub()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F8: resetting local Level 2 test state and reloading GameRoom_Hub2.");

        NativeProgression.DeveloperResetLevel2Access();
        EarlyRuntimeUnlockPatches.DeveloperReset();
        EarlySequenceBlockerPatches.DeveloperReset();

        if (!ReloadHub())
        {
            Plugin.LoggerInstance?.LogError(
                "[SCRC-AP] DEV F8 failed to reload Hub 2. Send the log; constructor metadata will show what request shape the game expects.");
        }
    }

    public static void GrantLevel2Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F9: simulating local receipt of 'Level 2 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 2 Access");
    }

    public static void GrantLevel3Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F10: simulating local receipt of 'Level 3 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 3 Access");
    }

    public static void GrantLevel4Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F12: simulating local receipt of 'Level 4 Access'.");

        NativeProgression.ApplyArchipelagoItem("Level 4 Access");
    }

    public static void ResetLevel4Locally()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F7: resetting Level 4 Access only.");

        NativeProgression.DeveloperResetLevel4Access();

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F7 COMPLETE: Level 4 permission reset; Level_08_Entrance should be disabled and vines restored on the next LateUpdate.");
    }

    public static void ResetLevel3AndReloadHub()
    {
        if (!Enabled)
            return;

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F11: resetting Level 3 Access only. The hard blocker will disable Level_07_Entrance directly; no Hub reload is required.");

        if (!NativeProgression.HasLevel2Access)
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] DEV F11: Level 2 Access was not present in this session; granting it locally so the Level 3 test does not disturb Level 2.");

            NativeProgression.ApplyArchipelagoItem("Level 2 Access");
        }

        NativeProgression.DeveloperResetLevel3Access();

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV F11 COMPLETE: Level 3 permission reset; Level 2 preserved. Level_07_Entrance should be disabled on the next LateUpdate.");
    }

    private static bool ReloadHub()
    {
        return TransitionToRoom(HubRoomId, partOfLoadLevel: false);
    }

    private static bool TransitionToRoom(string roomId, bool partOfLoadLevel)
    {
        object? processor;
        MethodInfo? process;
        Type? requestType;
        Type? roomType;
        object? transitionType;
        Type? transitionTypeType;
        Type? spawnPointType;
        object? capturedSpawnPoint;

        lock (Sync)
        {
            processor = _gameFlowProcessor;
            process = _gameFlowProcessRequest;
            requestType = _transitionRequestType;
            roomType = _roomIdentifierType;
            transitionType = _capturedTransitionType;
            transitionTypeType = _transitionTypeType;
            spawnPointType = _spawnPointType;
            capturedSpawnPoint = _capturedSpawnPoint;
        }

        if (processor == null || process == null || requestType == null || roomType == null)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] DEV HARNESS cannot transition to {roomId}: GameFlow has not been captured yet.");
            return false;
        }

        object? targetId = BuildIdentifier(roomType, roomId);
        if (targetId == null)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] DEV HARNESS could not construct GameRoomIdentifier({roomId}).");
            return false;
        }

        object? request = BuildTransitionRequest(
            requestType,
            targetId,
            transitionType,
            transitionTypeType,
            spawnPointType,
            capturedSpawnPoint);

        if (request == null)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] DEV HARNESS could not construct TransitionToGameRoomRequest for {roomId}.");
            return false;
        }

        WriteMember(request, "RoomToGoTo", targetId);
        WriteMember(request, "_RoomToGoTo_k__BackingField", targetId);
        WriteMember(request, "PartOfLoadLevel", partOfLoadLevel);
        WriteMember(request, "_PartOfLoadLevel_k__BackingField", partOfLoadLevel);

        if (transitionType != null)
        {
            WriteMember(request, "TransitionType", transitionType);
            WriteMember(request, "_TransitionType_k__BackingField", transitionType);
        }

        if (capturedSpawnPoint != null)
        {
            WriteMember(request, "SpecificSpawnPoint", capturedSpawnPoint);
            WriteMember(request, "_SpecificSpawnPoint_k__BackingField", capturedSpawnPoint);
        }

        try
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] DEV DIRECT TRANSITION REQUEST: room={roomId} " +
                $"transitionType={SafeSimpleValue(transitionType)} " +
                $"spawnPoint={SafeSimpleValue(capturedSpawnPoint)} " +
                $"partOfLoadLevel={partOfLoadLevel}.");

            process.Invoke(processor, new[] { request });

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] DEV DIRECT TRANSITION INVOKED successfully for {roomId}.");

            return true;
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] DEV transition to {roomId} failed: {ex.GetBaseException()}");
            return false;
        }
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

        if (SuppressOverride || !DeveloperHarness.Enabled)
            return;

        if (MusicLabPointOverride.OverrideScore is int forced)
            __result = forced;
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
    private static CassetteRegularSavePointerJoinProbe _regularSavePointerJoinProbe = new();
    private static CassetteDiskCommitRuntime _diskCommit = new();
    private static CassetteBundleRoutingDiagnosticRuntime _bundleRoutingDiagnostics = new();
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
    private static CassetteBoundedDiagnosticSignatureDeduplicator _saveSynchronizationObservationDiagnostics = new(8);
    [ThreadStatic] private static bool _applyingNativeGrant;

    internal static bool Enabled { get; private set; }
    internal static bool IsApplyingNativeGrant => _applyingNativeGrant;

    internal static void Configure()
    {
        lock (Sync)
        {
            Enabled = false; _slotDataSynchronized = false;
            _playerSaveRequestProcessor = null; _joinedSaveDataRequestProcessor = null; _runtime = new CassetteSaveEpochRuntime(); _saveIdentity = new CassetteProcessorSaveIdentityStabilizer();
            _regularSavePointerJoinProbe = new CassetteRegularSavePointerJoinProbe();
            _diskCommit = new CassetteDiskCommitRuntime(); _bundleRoutingDiagnostics = new CassetteBundleRoutingDiagnosticRuntime(); _activeSaveGeneration = 0; _activeSavePointer = 0;
            _mostRecentQueueLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
            _mostRecentResultLogDeduper = new CassetteDiagnosticSignatureDeduplicator();
            _unityReconciliationRequested = false; _unityReconciliationReason = string.Empty; _lastIdentityDiagnostic = string.Empty;
            _saveSynchronizationReady = false; _saveSynchronizationDeferredLogged = false; _saveSynchronizationReadyLogged = false;
            _saveSynchronizationObservationDiagnostics = new CassetteBoundedDiagnosticSignatureDeduplicator(8);
        }
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        CassetteSlotCompatibilityResult compatibility = CassetteSlotDataCompatibility.Validate(slotData);
        bool enabled = compatibility.Compatible;
        lock (Sync) { Enabled = enabled; _slotDataSynchronized = true; if (!enabled) _runtime.DeactivateSave(); }
        Plugin.LoggerInstance?.LogWarning(enabled
            ? $"[SCRC-AP] CASSETTE RECEIPT RECONCILIATION ENABLED entries={CassetteCatalog.All.Count}. AP-owned cassettes will be persisted as HAVE_IN_BAG; deposited cassettes remain deposited."
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

    internal static void BeginSaveStateLifecycleDiagnostic(int requestSlot, out long token)
    {
        CassetteDiskCommitDiagnosticContext context = default;
        object? processor = null;
        bool log;
        lock (Sync)
        {
            log = _diskCommit.TryBeginSaveStateLifecycleDiagnostic(requestSlot, out token, out context);
            if (log) processor = _playerSaveRequestProcessor;
        }
        if (log) LogSaveStateLifecycleDiagnostic(context, processor, requestSlot);
    }

    internal static void CompleteSaveStateLifecycleDiagnostic(long token, int requestSlot)
    {
        if (token == 0) return;
        CassetteDiskCommitDiagnosticContext context = default;
        object? processor = null;
        bool log;
        lock (Sync)
        {
            log = _diskCommit.TryCompleteSaveStateLifecycleDiagnostic(token, requestSlot, out context);
            if (log) processor = _playerSaveRequestProcessor;
        }
        if (log) LogSaveStateLifecycleDiagnostic(context, processor, requestSlot);
    }

    private static void LogSaveStateLifecycleDiagnostic(
        CassetteDiskCommitDiagnosticContext context,
        object? processor,
        int requestSlot)
    {
        bool stateReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteLifecycleDiagnosticState(
            processor,
            context.ActiveSongs,
            out CassettePublicWriteDiagnosticState state,
            out string stateStage);
        string phase = context.Phase is CassetteDiskCommitDiagnosticPhase.LifecycleBefore ? "BEFORE" : "AFTER";
        string pointer = stateReadable ? $"0x{state.StatePointer:X}" : "<unavailable>";
        string success = stateReadable
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(state.WriteState.LastSuccessTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string failure = stateReadable
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(state.WriteState.LastFailureTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string redundancyIndex = stateReadable
            ? state.RedundancyBundleIndex.ToString(CultureInfo.InvariantCulture)
            : "<unavailable>";
        string redundancyRevision = stateReadable
            ? state.RedundancyBundleRevision.ToString(CultureInfo.InvariantCulture)
            : "<unavailable>";
        string statuses = stateReadable
            ? string.Join(",", state.Statuses.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"))
            : "<unavailable>";
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE SAVE LIFECYCLE SNAPSHOT attempt={context.Attempt.Id} phase='{phase}' generation={context.Attempt.Generation} epoch={context.Attempt.Epoch} slot={context.Attempt.Slot} requestSlot={requestSlot} expectedPointer=0x{context.Attempt.Pointer:X} " +
            $"StatePointer={pointer} RedundancyBundleIndex={redundancyIndex} RedundancyBundleRevision={redundancyRevision} LastSuccessTime='{success}' LastFailureTime='{failure}' Statuses='[{statuses}]' activeSongs='[{string.Join(",", context.ActiveSongs)}]' stateStage='{stateStage}'.");
    }

    internal static void BeginBundleRoutingDiagnostic(
        CassetteBundleRoutingBoundary boundary,
        object? processor,
        object? request,
        MethodBase? originalMethod,
        bool originalAllowed,
        out CassetteBundleRoutingDiagnosticToken token)
    {
        token = default;
        try
        {
            string songKey = "<all>";
            string? requestSong = null;
            if (boundary is CassetteBundleRoutingBoundary.RecordSong &&
                CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(
                    request, out CassetteRecordSongRoutingPayload payload, out _))
            {
                requestSong = payload.Song;
                songKey = payload.Song;
            }
            else if (boundary is CassetteBundleRoutingBoundary.RecordSong)
            {
                songKey = "<unreadable>";
            }

            lock (Sync)
            {
                if (!_slotDataSynchronized || !Enabled || _saveIdentity.Pending || !_runtime.HasActiveSave ||
                    !_runtime.ActiveSlot.HasValue || _activeSavePointer == 0)
                    return;
                CassetteDiskCommitAttempt previewAttempt = _diskCommit.PreviewRoutingDiagnosticAttempt(
                    _activeSaveGeneration,
                    _runtime.Epoch,
                    _runtime.ActiveSlot.Value,
                    _activeSavePointer);
                CassetteDiskCommitAttempt activeAttemptValue;
                CassetteDiskCommitAttempt? activeAttempt =
                    _diskCommit.TryGetSubmittedAttempt(out activeAttemptValue) ||
                    _diskCommit.TryGetPreparedAttempt(out activeAttemptValue)
                        ? activeAttemptValue
                        : null;
                CassetteDiskCommitAttempt attempt;
                if (boundary is CassetteBundleRoutingBoundary.RecordSong)
                {
                    if (!_applyingNativeGrant && !activeAttempt.HasValue) return;
                    attempt = activeAttempt ?? previewAttempt;
                    if (_applyingNativeGrant && !activeAttempt.HasValue)
                        _bundleRoutingDiagnostics.EstablishCandidate(attempt);
                    if (!string.IsNullOrWhiteSpace(requestSong))
                        _bundleRoutingDiagnostics.RegisterRelevantSong(
                            attempt,
                            requestSong,
                            establishCandidate: _applyingNativeGrant && !activeAttempt.HasValue);
                }
                else if (!_bundleRoutingDiagnostics.TryResolveGlobalAttempt(
                             activeAttempt,
                             previewAttempt,
                             out attempt))
                {
                    return;
                }
                string[] songs = _diskCommit.Songs
                    .Concat(_bundleRoutingDiagnostics.GetRelevantSongs(attempt))
                    .Concat(string.IsNullOrWhiteSpace(requestSong) ? Array.Empty<string>() : new[] { requestSong })
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(song => song, StringComparer.Ordinal)
                    .ToArray();
                if (!_bundleRoutingDiagnostics.TryBegin(attempt, boundary, songKey, songs, out token))
                    return;
                token = token with { OriginalAllowed = originalAllowed };
            }
            LogBundleRoutingDiagnostic(token, processor, request, originalMethod);
        }
        catch
        {
            // Behavior-neutral diagnostic failure cannot affect the native request.
            token = default;
        }
    }

    internal static void CompleteBundleRoutingDiagnostic(
        CassetteBundleRoutingDiagnosticToken token,
        object? processor,
        object? request,
        MethodBase? originalMethod)
    {
        if (token.TokenId == 0) return;
        try
        {
            CassetteBundleRoutingDiagnosticToken completed;
            lock (Sync)
            {
                bool sameIdentity = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                    _activeSaveGeneration == token.Attempt.Generation &&
                    _runtime.Epoch == token.Attempt.Epoch &&
                    _runtime.ActiveSlot == token.Attempt.Slot &&
                    _activeSavePointer == token.Attempt.Pointer;
                if (!sameIdentity || !_bundleRoutingDiagnostics.TryComplete(token, out completed))
                    return;
            }
            LogBundleRoutingDiagnostic(completed, processor, request, originalMethod);
        }
        catch
        {
            // Behavior-neutral diagnostic failure cannot affect the native request.
        }
    }

    private static void LogBundleRoutingDiagnostic(
        CassetteBundleRoutingDiagnosticToken token,
        object? processor,
        object? request,
        MethodBase? originalMethod)
    {
        bool payloadReadable;
        string payloadStage;
        string requestSong = "<n/a>";
        string requestStatus = "<n/a>";
        string requestBundle = "<n/a>";
        string requestWriteType = "<n/a>";
        switch (token.Boundary)
        {
            case CassetteBundleRoutingBoundary.RecordSong:
                payloadReadable = CassetteSaveTransactionAdapter.TryReadRecordSongCassetteStatusRequest(
                    request, out CassetteRecordSongRoutingPayload recordPayload, out payloadStage);
                if (payloadReadable)
                {
                    requestSong = recordPayload.Song;
                    requestStatus = recordPayload.Status;
                    requestBundle = recordPayload.BundlePresent
                        ? $"present=True value='{recordPayload.Bundle ?? "<null>"}'"
                        : "present=False value='<null>'";
                }
                break;
            case CassetteBundleRoutingBoundary.PersistBundle:
                payloadReadable = CassetteSaveTransactionAdapter.TryReadPersistSaveChangeBundleRequest(
                    request, out CassettePersistBundleRoutingPayload persistPayload, out payloadStage);
                if (payloadReadable)
                {
                    requestBundle = persistPayload.BundlePresent
                        ? $"present=True value='{persistPayload.Bundle ?? "<null>"}'"
                        : "present=False value='<null>'";
                    requestWriteType = persistPayload.WriteType;
                }
                break;
            case CassetteBundleRoutingBoundary.PersistAllBundles:
                payloadReadable = CassetteSaveTransactionAdapter.TryReadPersistAllSaveChangeBundlesRequest(
                    request, out string? persistAllWriteType, out payloadStage);
                if (payloadReadable) requestWriteType = persistAllWriteType ?? "<null>";
                break;
            default:
                payloadReadable = request != null && string.Equals(
                    request.GetType().Name,
                    "DiscardAllUnstagedSaveStateChangesRequest",
                    StringComparison.Ordinal);
                payloadStage = payloadReadable ? "success" : "routing-discard-incompatible";
                break;
        }

        bool stateReadable;
        int observedSlot = token.Attempt.Slot;
        CassetteBundleRoutingState state;
        string stateStage;
        if (token.Boundary is CassetteBundleRoutingBoundary.RecordSong)
        {
            stateReadable = CassetteSaveTransactionAdapter.TryReadPlayerSaveBundleRoutingState(
                processor, token.ExpectedPointer, token.Songs, out state, out stateStage);
        }
        else
        {
            stateReadable = CassetteSaveTransactionAdapter.TryReadSaveDataBundleRoutingState(
                processor, token.ExpectedPointer, token.Songs, out observedSlot, out state, out stateStage);
        }
        string pointer = stateReadable ? $"0x{state.StatePointer:X}" : "<unavailable>";
        string hasUnstaged = stateReadable ? state.HasUnstagedChanges.ToString() : "<unavailable>";
        string statuses = stateReadable
            ? string.Join(",", state.Statuses.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"))
            : "<unavailable>";
        string methodIdentity =
            $"{originalMethod?.DeclaringType?.Name ?? "<unknown>"}.{originalMethod?.Name ?? "ProcessRequest"}({request?.GetType().Name ?? "<null>"})";
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE BUNDLE ROUTING attempt={token.Attempt.Id} phase='{token.Phase.ToString().ToUpperInvariant()}' boundary='{token.Boundary}' generation={token.Attempt.Generation} epoch={token.Attempt.Epoch} slot={token.Attempt.Slot} observedSlot={observedSlot} expectedPointer=0x{token.ExpectedPointer:X} " +
            $"requestType='{request?.GetType().Name ?? "<null>"}' method='{methodIdentity}' originalAllowed={token.OriginalAllowed} requestSong='{requestSong}' requestStatus='{requestStatus}' requestBundle='{requestBundle}' requestWriteType='{requestWriteType}' payloadReadable={payloadReadable} payloadStage='{payloadStage}' " +
            $"StatePointer={pointer} HasUnstagedChanges={hasUnstaged} statuses='[{statuses}]' activeSongs='[{string.Join(",", token.Songs)}]' stateStage='{stateStage}'.");
    }

    internal static void QueueSaveBoundarySignal(int expectedSlot, CassetteSaveBoundarySignalKind kind)
    {
        lock (Sync)
        {
            _regularSavePointerJoinProbe.Cancel();
            if (kind is CassetteSaveBoundarySignalKind.Selection)
                _joinedSaveDataRequestProcessor = null;
            object? processor = CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(_playerSaveRequestProcessor)
                ? _playerSaveRequestProcessor
                : null;
            _saveIdentity.Signal(expectedSlot, kind, processor);
            _unityReconciliationRequested = false;
            _unityReconciliationReason = string.Empty;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE BOUNDARY PENDING slot={expectedSlot} kind='{kind}'; prior epoch suspended pending two stable processor observations.");
    }

    internal static void BeginMostRecentSelectionBoundary(object? saveDataProcessor)
    {
        string stage;
        bool log;
        lock (Sync)
        {
            // Suspension is deliberately first: a malformed callback can never leave the old epoch eligible for work.
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
                $"[SCRC-AP] CASSETTE MOST-RECENT SAVE BOUNDARY stage='{stage}'; prior epoch suspended pending unique regular-save pointer join.");
    }

    internal static void ActivateLoadedSave(long generation, int slot, long pointer, string reason)
    {
        long epoch;
        lock (Sync)
        {
            _runtime.ActivateSave(slot);
            _activeSaveGeneration = generation;
            _activeSavePointer = pointer;
            _diskCommit.Reset();
            _bundleRoutingDiagnostics.Reset();
            _saveSynchronizationReady = false;
            _saveSynchronizationDeferredLogged = false;
            _saveSynchronizationReadyLogged = false;
            _saveSynchronizationObservationDiagnostics = new CassetteBoundedDiagnosticSignatureDeduplicator(8);
            epoch = _runtime.Epoch;
        }
        Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE SAVE EPOCH ACTIVATED epoch={epoch} slot={slot} reason='{reason}'.");
        RequestUnityReconciliation("save selection completed");
    }

    internal static void DeactivateLoadedSave(string reason)
    {
        lock (Sync)
        {
            _runtime.DeactivateSave();
            _activeSaveGeneration = 0;
            _activeSavePointer = 0;
            _joinedSaveDataRequestProcessor = null;
            _diskCommit.Reset();
            _bundleRoutingDiagnostics.Reset();
            _saveSynchronizationReady = false;
            _saveSynchronizationDeferredLogged = false;
            _saveSynchronizationReadyLogged = false;
            _saveSynchronizationObservationDiagnostics = new CassetteBoundedDiagnosticSignatureDeduplicator(8);
        }
        Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE SAVE EPOCH INACTIVE reason='{reason}'.");
    }

    internal static void TryReconcile(string reason)
    {
        string[] songs;
        object? joinedSaveDataProcessor;
        long generation;
        long epoch;
        int slot;
        long pointer;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_slotDataSynchronized || !Enabled || !_runtime.HasActiveSave) return;
            songs = _runtime.PendingSongs.ToArray();
            joinedSaveDataProcessor = _joinedSaveDataRequestProcessor;
            generation = _activeSaveGeneration;
            epoch = _runtime.Epoch;
            slot = _runtime.ActiveSlot!.Value;
            pointer = _activeSavePointer;
        }
        if (!TryConfirmSaveSynchronizationReady(
                generation, epoch, slot, pointer, joinedSaveDataProcessor,
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
        object? joinedSaveDataProcessor,
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

        bool ready = CassetteSaveTransactionAdapter.TryConfirmSaveSynchronizationReady(
            joinedSaveDataProcessor, slot, pointer, out stage);
        bool logDeferred = false;
        bool logReady = false;
        lock (Sync)
        {
            bool current = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                _activeSaveGeneration == generation && _runtime.Epoch == epoch &&
                _runtime.ActiveSlot == slot && _activeSavePointer == pointer &&
                ReferenceEquals(_joinedSaveDataRequestProcessor, joinedSaveDataProcessor);
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
        if (!ready)
            LogSaveSynchronizationObservationDiagnostic(
                generation, epoch, slot, pointer, joinedSaveDataProcessor, stage);
        if (logReady)
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] CASSETTE SAVE SYNCHRONIZATION READY epoch={epoch} slot={slot} pointer=0x{pointer:X}; pending reconciliation resumed by lifecycle observation.");
        return ready;
    }

    private static void LogSaveSynchronizationObservationDiagnostic(
        long generation,
        long epoch,
        int expectedSlot,
        long expectedPointer,
        object? retainedSaveDataProcessor,
        string gateStage)
    {
        try
        {
            bool readable = CassetteSaveTransactionAdapter.TryReadSaveSynchronizationObservationDiagnostic(
                retainedSaveDataProcessor,
                out CassetteSaveSynchronizationObservationState state,
                out string diagnosticStage);
            string enquirySlot = state.HasEnquirySlot
                ? state.EnquirySlot.ToString(CultureInfo.InvariantCulture)
                : "<unavailable>";
            string enquiryPointer = state.HasEnquiryStatePointer
                ? $"0x{state.EnquiryStatePointer:X}"
                : "<unavailable>";
            string validExisting = state.HasValidExistingSaveSelected
                ? state.IsAValidExistingSaveSelected.ToString()
                : "<unavailable>";
            string registeredPointer = state.HasRegisteredPersistProcessorPointer
                ? $"0x{state.RegisteredPersistProcessorPointer:X}"
                : "<unavailable>";
            string retainedPointer = state.HasRetainedSaveDataProcessorPointer
                ? $"0x{state.RetainedSaveDataProcessorPointer:X}"
                : "<unavailable>";
            string processorIdentityOutcome = state.HasProcessorPointersMatch
                ? state.ProcessorPointersMatch ? "match" : "mismatch"
                : "<unavailable>";
            string registeredSelectedSlot = state.HasRegisteredSelectedSlot
                ? state.RegisteredSelectedSlot.ToString(CultureInfo.InvariantCulture)
                : "<unavailable>";
            string signature =
                $"gate={gateStage}|enquiryStage={state.EnquiryStage}|enquirySlot={enquirySlot}|enquiryPointer={enquiryPointer}|valid={validExisting}|" +
                $"registered={registeredPointer}|retained={retainedPointer}|identity={processorIdentityOutcome}|registeredStateStage={state.RegisteredStateStage}|registeredSlot={registeredSelectedSlot}|diagnostic={diagnosticStage}";
            bool shouldLog;
            lock (Sync)
            {
                bool current = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                    _activeSaveGeneration == generation && _runtime.Epoch == epoch &&
                    _runtime.ActiveSlot == expectedSlot && _activeSavePointer == expectedPointer &&
                    ReferenceEquals(_joinedSaveDataRequestProcessor, retainedSaveDataProcessor);
                shouldLog = current && _saveSynchronizationObservationDiagnostics.ShouldLog(signature);
            }
            if (!shouldLog) return;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE SAVE SYNCHRONIZATION OBSERVATION generation={generation} epoch={epoch} expectedSlot={expectedSlot} expectedPointer=0x{expectedPointer:X} " +
                $"enquirySlot={enquirySlot} enquiryStatePointer={enquiryPointer} IsAValidExistingSaveSelected={validExisting} enquiryStage='{state.EnquiryStage}' " +
                $"registeredPersistProcessorPointer={registeredPointer} retainedSaveDataProcessorPointer={retainedPointer} processorIdentity='{processorIdentityOutcome}' " +
                $"registeredSelectedSlot={registeredSelectedSlot} registeredStateStage='{state.RegisteredStateStage}' gateStage='{gateStage}' readable={readable} diagnosticStage='{diagnosticStage}'.");
        }
        catch
        {
            // Synchronization comparisons are diagnostic-only and cannot affect the existing gate.
        }
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
        object? joinedSaveDataProcessor;
        long generation;
        long epoch;
        int slot;
        long pointer;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_slotDataSynchronized || !Enabled || !_runtime.HasActiveSave || !_runtime.IsPending(nativeSong)) return;
            processor = _playerSaveRequestProcessor;
            joinedSaveDataProcessor = _joinedSaveDataRequestProcessor;
            generation = _activeSaveGeneration;
            epoch = _runtime.Epoch;
            slot = _runtime.ActiveSlot!.Value;
            pointer = _activeSavePointer;
        }
        if (!TryConfirmSaveSynchronizationReady(
                generation, epoch, slot, pointer, joinedSaveDataProcessor,
                allowObservationProbe: false, out _))
            return;
        if (!CassetteSaveTransactionAdapter.IsCompatiblePlayerSaveRequestProcessor(processor) ||
            !CassetteSaveTransactionAdapter.TryReadCassetteStatus(processor!, nativeSong, out string? status))
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE DEFERRED nativeSong='{nativeSong}' reason='{reason}' detail='selected save or compatible processor unavailable'.");
            return;
        }
        bool submit;
        lock (Sync)
        {
            if (!_runtime.HasActiveSave || _runtime.Epoch != epoch) return;
            bool newlyVerifiedBag = verificationDue && _runtime.RecordVerification(nativeSong, status);
            if (newlyVerifiedBag) _diskCommit.Stage(nativeSong);
            else _runtime.Observe(nativeSong, status);
            if (!_runtime.IsPending(nativeSong))
            {
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE VERIFIED nativeSong='{nativeSong}' status='{status}' epoch={epoch} reason='{reason}'.");
                return;
            }
            submit = _runtime.CanSubmit(nativeSong, status, processorAvailable: true);
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

    internal static void OnPlayerSaveWriteCompletedEvent(object? nativeEvent)
    {
        CassetteDiskCommitAttempt attempt;
        bool submitted;
        bool terminalOnly;
        object? joinedSaveDataProcessor;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave) return;
            submitted = _diskCommit.TryGetSubmittedAttempt(out attempt);
            bool prepared = !submitted && _diskCommit.TryGetPreparedAttempt(out attempt);
            terminalOnly = !submitted && !prepared && _diskCommit.TryGetLastTerminalAttempt(out attempt);
            if (!submitted && !prepared && !terminalOnly) return;
            joinedSaveDataProcessor = _joinedSaveDataRequestProcessor;
        }
        if (!terminalOnly && !TryConfirmSaveSynchronizationReady(
                attempt.Generation, attempt.Epoch, attempt.Slot, attempt.Pointer, joinedSaveDataProcessor,
                allowObservationProbe: false, out _))
            return;
        if (!CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEventHeader(
                nativeEvent, out int eventSlot, out bool succeeded, out string stage))
        {
            if (terminalOnly) return;
            if (!submitted) return;
            bool shouldLog;
            lock (Sync) shouldLog = _diskCommit.TryReportRejectedEvent(attempt);
            if (shouldLog)
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE DISK COMMIT EVENT REJECTED stage='{stage}'; active transaction remains fail-closed pending.");
            return;
        }

        if (terminalOnly)
        {
            bool logLateEvent;
            lock (Sync)
            {
                bool sameIdentity = !_saveIdentity.Pending && _runtime.HasActiveSave &&
                    _activeSaveGeneration == attempt.Generation && _runtime.Epoch == attempt.Epoch &&
                    _runtime.ActiveSlot == attempt.Slot && _activeSavePointer == attempt.Pointer;
                logLateEvent = sameIdentity && _diskCommit.TryClaimLateEvent(attempt, eventSlot);
            }
            if (logLateEvent)
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE DISK COMMIT LATE_EVENT attempt={attempt.Id} generation={attempt.Generation} epoch={attempt.Epoch} slot={attempt.Slot} pointer=0x{attempt.Pointer:X} eventSlot={eventSlot} eventSucceeded={succeeded}; header recorded only and terminal transaction remains closed.");
            return;
        }

        CassetteDiskCommitEventOutcome outcome;
        CassetteDiskCommitDiagnosticContext eventDiagnostic = default;
        CassetteDiskCommitDiagnosticContext failureDiagnostic = default;
        object? eventDiagnosticProcessor = null;
        bool logEventDiagnostic = false;
        bool logFailureDiagnostic = false;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave ||
                _activeSaveGeneration != attempt.Generation || _runtime.Epoch != attempt.Epoch ||
                _runtime.ActiveSlot != attempt.Slot || _activeSavePointer != attempt.Pointer)
                return;
            if (!submitted)
            {
                if (eventSlot == attempt.Slot)
                    logEventDiagnostic = _diskCommit.TryClaimDiagnostic(
                        attempt, CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored, out eventDiagnostic);
                outcome = CassetteDiskCommitEventOutcome.Ignored;
            }
            else
            {
                outcome = _diskCommit.ObserveWriteCompletedEvent(attempt, eventSlot, succeeded);
                if (outcome is not CassetteDiskCommitEventOutcome.Ignored)
                    logEventDiagnostic = _diskCommit.TryClaimDiagnostic(
                        attempt, CassetteDiskCommitDiagnosticPhase.Event, out eventDiagnostic);
                if (outcome is CassetteDiskCommitEventOutcome.Failure)
                    logFailureDiagnostic = _diskCommit.TryClaimDiagnostic(
                        attempt, CassetteDiskCommitDiagnosticPhase.Failure, out failureDiagnostic);
            }
            if (logEventDiagnostic || logFailureDiagnostic)
                eventDiagnosticProcessor = _playerSaveRequestProcessor;
        }
        if (logEventDiagnostic)
            LogDiskCommitDiagnostic(
                eventDiagnostic,
                eventDiagnosticProcessor,
                $"eventSlot={eventSlot} eventSucceeded={succeeded}");
        if (logFailureDiagnostic)
            LogDiskCommitDiagnostic(
                failureDiagnostic,
                eventDiagnosticProcessor,
                $"eventSlot={eventSlot} eventSucceeded={succeeded}");
        if (outcome is CassetteDiskCommitEventOutcome.Ignored) return;
        if (outcome is CassetteDiskCommitEventOutcome.Failure)
        {
            CassetteSaveTransactionAdapter.TryReadPlayerSaveWriteCompletedEventFailureReason(
                nativeEvent, out string? failureReason);
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE DISK COMMIT EVENT FAILURE epoch={attempt.Epoch} slot={attempt.Slot} failureReason='{failureReason ?? "<unavailable>"}'; transaction failed closed.");
            return;
        }
        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] CASSETTE DISK COMMIT EVENT epoch={attempt.Epoch} slot={attempt.Slot} succeeded=True; immediate public-state verification requested.");
        TickDiskCommit(TimeSpan.Zero);
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
            if (!_saveSynchronizationReady) return;
            ready = _runtime.Tick(elapsed).ToArray();
        }
        foreach (string song in ready) TryReconcileSong(song, "bounded delayed verification", verificationDue: true);
        TickDiskCommit(elapsed);
    }

    private static void TickDiskCommit(TimeSpan elapsed)
    {
        object? processor; object? joinedSaveDataProcessor; long generation; long epoch; int slot; long pointer; string[] songs;
        CassetteDiskCommitAttempt activeAttempt;
        bool submitted;
        lock (Sync)
        {
            if (_saveIdentity.Pending || !_runtime.HasActiveSave || !_diskCommit.HasWork) return;
            processor = _playerSaveRequestProcessor; generation = _activeSaveGeneration; epoch = _runtime.Epoch;
            joinedSaveDataProcessor = _joinedSaveDataRequestProcessor;
            slot = _runtime.ActiveSlot!.Value; pointer = _activeSavePointer;
            songs = _diskCommit.Songs.ToArray();
            submitted = _diskCommit.TryGetSubmittedAttempt(out activeAttempt);
        }
        if (!TryConfirmSaveSynchronizationReady(
                generation, epoch, slot, pointer, joinedSaveDataProcessor,
                allowObservationProbe: false, out _))
            return;
        bool identityReadable = CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(
            processor, out long observedPointer, out string identityStage);
        bool statusesRetained = TryReadDiskCommitBoundary(
            processor,
            pointer,
            songs,
            identityReadable,
            observedPointer,
            identityStage,
            out CassetteDiskCommitFailureDiagnostic failureKind);
        TimeSpan activeUpdateElapsed = CassetteDiskCommitRuntime.CapActiveUpdateElapsed(elapsed);
        if (!CassetteSaveTransactionAdapter.TryReadPublicWriteState(processor, out CassettePublicWriteState writeState, out string writeStage))
        {
            CassetteDiskCommitOutcome unavailableOutcome = CassetteDiskCommitOutcome.None;
            bool reportDeferred = false;
            bool reportStillPending = false;
            CassetteDiskCommitDiagnosticContext unavailableDiagnostic = default;
            bool logUnavailableDiagnostic = false;
            lock (Sync)
            {
                unavailableOutcome = submitted
                    ? _diskCommit.ObserveUnavailable(
                        activeAttempt, statusesRetained, failureKind, activeUpdateElapsed, out reportStillPending)
                    : _diskCommit.ObservePreSubmitUnavailable(
                        epoch, slot, pointer, identityReadable, identityReadable ? observedPointer : 0,
                        statusesRetained, elapsed, out reportDeferred);
                if (submitted && reportStillPending)
                    logUnavailableDiagnostic = _diskCommit.TryClaimDiagnostic(
                        activeAttempt, CassetteDiskCommitDiagnosticPhase.StillPending, out unavailableDiagnostic);
                if (submitted && unavailableOutcome is CassetteDiskCommitOutcome.HardTimeout)
                    logUnavailableDiagnostic = _diskCommit.TryClaimDiagnostic(
                        activeAttempt, CassetteDiskCommitDiagnosticPhase.HardTimeout, out unavailableDiagnostic) ||
                        logUnavailableDiagnostic;
                if (submitted && unavailableOutcome is CassetteDiskCommitOutcome.Failure)
                    logUnavailableDiagnostic = _diskCommit.TryClaimDiagnostic(
                        activeAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out unavailableDiagnostic) ||
                        logUnavailableDiagnostic;
            }
            if (logUnavailableDiagnostic)
                LogDiskCommitDiagnostic(unavailableDiagnostic, processor, $"pollStage='{writeStage}'");
            if (reportDeferred)
                Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] CASSETTE DISK COMMIT DEFERRED stage='{writeStage}'.");
            if (reportStillPending)
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] CASSETTE DISK COMMIT STILL_PENDING epoch={epoch} slot={slot}; native write remains within the 130-second active-update watchdog.");
            if (unavailableOutcome is CassetteDiskCommitOutcome.HardTimeout)
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE DISK COMMIT HARD_TIMEOUT epoch={epoch} slot={slot}; completion is indeterminate and automatic resubmission is blocked until save identity resets.");
            if (unavailableOutcome is CassetteDiskCommitOutcome.Failure or CassetteDiskCommitOutcome.Timeout or CassetteDiskCommitOutcome.Cancelled)
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE DISK COMMIT {unavailableOutcome.ToString().ToUpperInvariant()} epoch={epoch} slot={slot}; grant remains retryable.");
            return;
        }
        if (!submitted)
        {
            bool prepared;
            CassetteDiskCommitAttempt preparedAttempt = default;
            CassetteDiskCommitDiagnosticContext preDiagnostic = default;
            bool logPreDiagnostic = false;
            lock (Sync)
            {
                prepared = _runtime.Epoch == epoch && _activeSavePointer == pointer &&
                    _activeSaveGeneration == generation &&
                    _diskCommit.TryPrepare(generation, epoch, slot, pointer, writeState, out preparedAttempt);
                if (prepared)
                    logPreDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt, CassetteDiskCommitDiagnosticPhase.Pre, out preDiagnostic);
            }
            if (!prepared) return;
            if (logPreDiagnostic)
                LogDiskCommitDiagnostic(
                    preDiagnostic, processor, transactionBaseline: writeState);
            string persistDetail = "status regression";
            bool persistSubmitted = false;
            if (statusesRetained)
            {
                CassetteDiskCommitDiagnosticContext preTargetDiagnostic = default;
                bool logPreTargetDiagnostic;
                lock (Sync)
                    logPreTargetDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt, CassetteDiskCommitDiagnosticPhase.PreTarget, out preTargetDiagnostic);
                if (logPreTargetDiagnostic)
                    LogDiskCommitTargetDiagnostic(preTargetDiagnostic, processor, joinedSaveDataProcessor);

                persistSubmitted = CassetteSaveTransactionAdapter.TrySubmitDefaultUrgentPersist(out persistDetail);

                CassetteDiskCommitDiagnosticContext postTargetDiagnostic = default;
                bool logPostTargetDiagnostic;
                lock (Sync)
                    logPostTargetDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt, CassetteDiskCommitDiagnosticPhase.PostTarget, out postTargetDiagnostic);
                if (logPostTargetDiagnostic)
                    LogDiskCommitTargetDiagnostic(postTargetDiagnostic, processor, joinedSaveDataProcessor);
            }
            if (!statusesRetained || !persistSubmitted)
            {
                CassetteDiskCommitFailureDiagnostic submissionFailure = !statusesRetained
                    ? failureKind
                    : CassetteDiskCommitFailureDiagnostic.SubmissionFailed(persistDetail);
                CassetteDiskCommitDiagnosticContext submissionFailureDiagnostic = default;
                bool logSubmissionFailureDiagnostic;
                lock (Sync)
                {
                    _diskCommit.FailPrepared(preparedAttempt, submissionFailure);
                    logSubmissionFailureDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt,
                        CassetteDiskCommitDiagnosticPhase.Failure,
                        out submissionFailureDiagnostic);
                }
                if (logSubmissionFailureDiagnostic)
                    LogDiskCommitDiagnostic(submissionFailureDiagnostic, processor, $"submitStage='{persistDetail}'");
                Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE DISK COMMIT SUBMISSION FAILED detail='{persistDetail}'.");
                return;
            }
            bool postIdentityReadable = CassetteSaveTransactionAdapter.TryGetProcessorSaveIdentity(
                processor, out long postSubmitPointer, out string postIdentityStage);
            bool postStatusesRetained = TryReadDiskCommitBoundary(
                processor,
                pointer,
                songs,
                postIdentityReadable,
                postSubmitPointer,
                postIdentityStage,
                out CassetteDiskCommitFailureDiagnostic postBoundaryFailure);
            bool postStateReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteState(
                processor, out CassettePublicWriteState postSubmitState, out string postSubmitStage);
            bool postFailureAdvanced = postStateReadable && postSubmitState.LastFailureTime.HasValue &&
                (!writeState.LastFailureTime.HasValue || postSubmitState.LastFailureTime.Value > writeState.LastFailureTime.Value);
            bool postFailureChanged = postStateReadable && !string.IsNullOrWhiteSpace(postSubmitState.FailureReason) &&
                !string.Equals(postSubmitState.FailureReason, writeState.FailureReason, StringComparison.Ordinal);
            CassetteDiskCommitFailureDiagnostic postFailureKind = !postIdentityReadable ||
                postSubmitPointer != pointer || !postStatusesRetained
                ? postBoundaryFailure
                : !postStateReadable
                    ? CassetteDiskCommitFailureDiagnostic.SubmissionFailed(postSubmitStage)
                    : postFailureAdvanced
                        ? CassetteDiskCommitFailureDiagnostic.FailureTimeAdvanced(
                            writeState.LastFailureTime, postSubmitState.LastFailureTime!.Value)
                        : postFailureChanged
                            ? CassetteDiskCommitFailureDiagnostic.FailureReasonChanged(
                                writeState.FailureReason, postSubmitState.FailureReason!)
                            : CassetteDiskCommitFailureDiagnostic.SubmissionFailed(
                                "post-submit baseline rejected");
            bool markedSubmitted;
            CassetteDiskCommitDiagnosticContext postDiagnostic = default;
            CassetteDiskCommitDiagnosticContext postFailureDiagnostic = default;
            bool logPostDiagnostic = false;
            bool logPostFailureDiagnostic = false;
            lock (Sync)
            {
                bool stillCurrent = _runtime.Epoch == epoch && _activeSavePointer == pointer &&
                    _activeSaveGeneration == generation;
                if (stillCurrent)
                    logPostDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt, CassetteDiskCommitDiagnosticPhase.Post, out postDiagnostic);
                markedSubmitted = stillCurrent && postIdentityReadable && postSubmitPointer == pointer &&
                    postStatusesRetained && postStateReadable && !postFailureAdvanced && !postFailureChanged &&
                    _diskCommit.MarkSubmitted(preparedAttempt, postSubmitState);
                if (!markedSubmitted && stillCurrent)
                {
                    _diskCommit.MarkSubmissionIndeterminate(preparedAttempt, postFailureKind);
                    logPostFailureDiagnostic = _diskCommit.TryClaimDiagnostic(
                        preparedAttempt,
                        CassetteDiskCommitDiagnosticPhase.Failure,
                        out postFailureDiagnostic);
                }
            }
            if (logPostDiagnostic)
                LogDiskCommitDiagnostic(
                    postDiagnostic,
                    processor,
                    $"baselineStage='{postSubmitStage}'",
                    markedSubmitted ? postSubmitState : null);
            if (logPostFailureDiagnostic)
                LogDiskCommitDiagnostic(
                    postFailureDiagnostic,
                    processor,
                    $"baselineStage='{postSubmitStage}'");
            if (!markedSubmitted)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] CASSETTE DISK COMMIT SUBMISSION INDETERMINATE epoch={epoch} slot={slot} stage='{postSubmitStage}'; post-submit baseline could not be proven and automatic resubmission is blocked until save identity resets.");
                return;
            }
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE DISK COMMIT SUBMITTED epoch={epoch} slot={slot} songs='[{string.Join(",", songs)}]' {persistDetail}; completion pending.");
            return;
        }
        CassetteDiskCommitOutcome outcome;
        bool reportActiveStillPending;
        CassetteDiskCommitDiagnosticContext activeDiagnostic = default;
        bool logActiveDiagnostic = false;
        lock (Sync)
        {
            outcome = _diskCommit.Observe(
                activeAttempt, writeState, statusesRetained, failureKind, activeUpdateElapsed, out reportActiveStillPending);
            if (reportActiveStillPending)
                logActiveDiagnostic = _diskCommit.TryClaimDiagnostic(
                    activeAttempt, CassetteDiskCommitDiagnosticPhase.StillPending, out activeDiagnostic);
            if (outcome is CassetteDiskCommitOutcome.HardTimeout)
                logActiveDiagnostic = _diskCommit.TryClaimDiagnostic(
                    activeAttempt, CassetteDiskCommitDiagnosticPhase.HardTimeout, out activeDiagnostic) ||
                    logActiveDiagnostic;
            if (outcome is CassetteDiskCommitOutcome.Failure)
                logActiveDiagnostic = _diskCommit.TryClaimDiagnostic(
                    activeAttempt, CassetteDiskCommitDiagnosticPhase.Failure, out activeDiagnostic) ||
                    logActiveDiagnostic;
        }
        if (logActiveDiagnostic) LogDiskCommitDiagnostic(activeDiagnostic, processor);
        if (reportActiveStillPending)
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] CASSETTE DISK COMMIT STILL_PENDING epoch={epoch} slot={slot}; native write remains within the 130-second active-update watchdog.");
        if (outcome is CassetteDiskCommitOutcome.Success)
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE DISK COMMIT VERIFIED epoch={epoch} slot={slot}; public write completion advanced.");
        else if (outcome is CassetteDiskCommitOutcome.HardTimeout)
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] CASSETTE DISK COMMIT HARD_TIMEOUT epoch={epoch} slot={slot}; completion is indeterminate and automatic resubmission is blocked until save identity resets.");
        else if (outcome is CassetteDiskCommitOutcome.Failure or CassetteDiskCommitOutcome.Timeout or CassetteDiskCommitOutcome.Cancelled)
            Plugin.LoggerInstance?.LogWarning($"[SCRC-AP] CASSETTE DISK COMMIT {outcome.ToString().ToUpperInvariant()} epoch={epoch} slot={slot}; grant remains retryable.");
    }

    private static bool TryReadDiskCommitBoundary(
        object? processor,
        long expectedPointer,
        IReadOnlyList<string> songs,
        bool identityReadable,
        long observedPointer,
        string identityStage,
        out CassetteDiskCommitFailureDiagnostic failureKind)
    {
        failureKind = default;
        if (!identityReadable)
        {
            failureKind = CassetteDiskCommitFailureDiagnostic.IdentityUnreadable(identityStage);
            return false;
        }
        if (observedPointer != expectedPointer)
        {
            failureKind = CassetteDiskCommitFailureDiagnostic.PointerMismatch(expectedPointer, observedPointer);
            return false;
        }
        foreach (string song in songs)
        {
            if (!CassetteSaveTransactionAdapter.TryReadCassetteStatus(
                    processor!, song, out string? status, out string statusStage))
            {
                failureKind = CassetteDiskCommitFailureDiagnostic.StatusUnreadable(song, statusStage);
                return false;
            }
            if (!string.Equals(status, CassetteRandomizationPolicy.HaveInBag, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, CassetteRandomizationPolicy.HaveDeposited, StringComparison.OrdinalIgnoreCase))
            {
                failureKind = CassetteDiskCommitFailureDiagnostic.StatusRegression(song, status);
                return false;
            }
        }
        return true;
    }

    private static void LogDiskCommitDiagnostic(
        CassetteDiskCommitDiagnosticContext context,
        object? processor,
        string? detail = null,
        CassettePublicWriteState? transactionBaseline = null)
    {
        string[] allSongs = context.ActiveSongs.Concat(context.QueuedSongs)
            .Distinct(StringComparer.Ordinal).OrderBy(song => song, StringComparer.Ordinal).ToArray();
        bool stateReadable = CassetteSaveTransactionAdapter.TryReadPublicWriteDiagnosticState(
            processor,
            context.Attempt.Pointer,
            allSongs,
            out CassettePublicWriteDiagnosticState state,
            out string stateStage);
        string statuses = stateReadable
            ? string.Join(",", state.Statuses.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"))
            : $"<unavailable:{stateStage}>";
        string phase = context.Phase switch
        {
            CassetteDiskCommitDiagnosticPhase.Pre => "PRE",
            CassetteDiskCommitDiagnosticPhase.Post => "POST",
            CassetteDiskCommitDiagnosticPhase.Event => "EVENT",
            CassetteDiskCommitDiagnosticPhase.StillPending => "STILL_PENDING",
            CassetteDiskCommitDiagnosticPhase.HardTimeout => "HARD_TIMEOUT",
            CassetteDiskCommitDiagnosticPhase.Failure => "FAILURE",
            CassetteDiskCommitDiagnosticPhase.PreparedEventIgnored => "PREPARED_EVENT_IGNORED",
            _ => context.Phase.ToString().ToUpperInvariant(),
        };
        CassettePublicWriteState writeState = state.WriteState;
        string successTime = stateReadable
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(writeState.LastSuccessTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string failureTime = stateReadable
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(writeState.LastFailureTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string currentGameTime = stateReadable
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(state.CurrentGameTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string hasChanges = stateReadable ? writeState.HasChanges.ToString() : "<unavailable>";
        string requiresWriteToDisk = stateReadable ? writeState.RequiresWriteToDisk.ToString() : "<unavailable>";
        string hasUnstagedChanges = stateReadable ? state.HasUnstagedChanges.ToString() : "<unavailable>";
        string redundancyIndex = stateReadable
            ? state.RedundancyBundleIndex.ToString(CultureInfo.InvariantCulture) : "<unavailable>";
        string redundancyRevision = stateReadable
            ? state.RedundancyBundleRevision.ToString(CultureInfo.InvariantCulture) : "<unavailable>";
        string baselineSuccessTime = transactionBaseline.HasValue
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(transactionBaseline.Value.LastSuccessTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string baselineFailureTime = transactionBaseline.HasValue
            ? CassetteDiskCommitDiagnosticFormatter.FormatNullableDouble(transactionBaseline.Value.LastFailureTime)
            : "present=<unavailable> value=<unavailable> bits=<unavailable>";
        string baselineHasChanges = transactionBaseline.HasValue
            ? transactionBaseline.Value.HasChanges.ToString() : "<unavailable>";
        string baselineRequiresWriteToDisk = transactionBaseline.HasValue
            ? transactionBaseline.Value.RequiresWriteToDisk.ToString() : "<unavailable>";
        string baselineFailureReason = transactionBaseline.HasValue
            ? transactionBaseline.Value.FailureReason ?? "<null>" : "<unavailable>";
        string failureKind = context.FailureKind is CassetteDiskCommitFailureKind.None
            ? "<none>"
            : context.FailureKind.ToString();
        string failureDetail = string.IsNullOrWhiteSpace(context.FailureDetail)
            ? "<none>"
            : context.FailureDetail;
        string suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" {detail}";
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE DISK COMMIT SNAPSHOT attempt={context.Attempt.Id} phase='{phase}' eventOrdinal={context.EventOrdinal} elapsedSeconds={context.Elapsed.TotalSeconds.ToString("R", CultureInfo.InvariantCulture)} " +
            $"generation={context.Attempt.Generation} epoch={context.Attempt.Epoch} slot={context.Attempt.Slot} pointer=0x{context.Attempt.Pointer:X} " +
            $"successTime='{successTime}' failureTime='{failureTime}' currentGameTime='{currentGameTime}' " +
            $"baselineSuccessTime='{baselineSuccessTime}' baselineFailureTime='{baselineFailureTime}' baselineHasChanges={baselineHasChanges} baselineRequiresWriteToDisk={baselineRequiresWriteToDisk} baselineFailureReason='{baselineFailureReason}' " +
            $"HasChanges={hasChanges} RequiresWriteToDisk={requiresWriteToDisk} " +
            $"HasUnstagedChanges={hasUnstagedChanges} redundancyIndex={redundancyIndex} redundancyRevision={redundancyRevision} " +
            $"failureReason='{(stateReadable ? writeState.FailureReason ?? "<null>" : "<unavailable>")}' failureKind='{failureKind}' failureDetail='{failureDetail}' stateStage='{stateStage}' statuses='[{statuses}]' " +
            $"activeSongs='[{string.Join(",", context.ActiveSongs)}]' queuedSongs='[{string.Join(",", context.QueuedSongs)}]'{suffix}.");
    }

    private static void LogDiskCommitTargetDiagnostic(
        CassetteDiskCommitDiagnosticContext context,
        object? playerProcessor,
        object? retainedSaveDataProcessor)
    {
        try
        {
        string[] allSongs = context.ActiveSongs.Concat(context.QueuedSongs)
            .Distinct(StringComparer.Ordinal).OrderBy(song => song, StringComparer.Ordinal).ToArray();
        bool readable = CassetteSaveTransactionAdapter.TryReadDiskCommitTargetDiagnostic(
            playerProcessor,
            retainedSaveDataProcessor,
            context.Attempt.Slot,
            context.Attempt.Pointer,
            allSongs,
            out CassetteDiskCommitTargetDiagnosticState state,
            out int registryCount,
            out string stage);
        string phase = context.Phase is CassetteDiskCommitDiagnosticPhase.PreTarget ? "PRE_TARGET" : "POST_TARGET";
        string registryCountValue = registryCount >= 0 ? registryCount.ToString(CultureInfo.InvariantCulture) : "<unavailable>";
        string playerProcessorPointer = state.HasPlayerProcessorPointer ? $"0x{state.PlayerProcessorPointer:X}" : "<unavailable>";
        string registeredPersistProcessorPointer = state.HasRegisteredPersistProcessorPointer ? $"0x{state.RegisteredPersistProcessorPointer:X}" : "<unavailable>";
        string retainedSaveDataProcessorPointer = state.HasRetainedSaveDataProcessorPointer ? $"0x{state.RetainedSaveDataProcessorPointer:X}" : "<unavailable>";
        string saveDataStatePointer = state.HasSaveDataStatePointer ? $"0x{state.SaveDataStatePointer:X}" : "<unavailable>";
        string selectedSlot = state.HasSelectedPlayerSaveSlot ? state.SelectedPlayerSaveSlot.ToString(CultureInfo.InvariantCulture) : "<unavailable>";
        string selectedEntryPointer = state.HasSelectedEntryPointer ? $"0x{state.SelectedEntryPointer:X}" : "<unavailable>";
        string expectedSlotEntryPointer = state.HasExpectedSlotEntryPointer ? $"0x{state.ExpectedSlotEntryPointer:X}" : "<unavailable>";
        string playerEffectiveStatuses = state.HasPlayer
            ? string.Join(",", state.Player.EffectiveStatuses.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))
            : "<unavailable>";
        string playerCanonicalStatuses = state.HasPlayer
            ? string.Join(",", state.Player.CanonicalStatuses.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value ?? "<null>"}"))
            : "<unavailable>";
        string selectedEffectiveStatuses = state.HasSelected
            ? string.Join(",", state.Selected.EffectiveStatuses.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value}"))
            : "<unavailable>";
        string selectedCanonicalStatuses = state.HasSelected
            ? string.Join(",", state.Selected.CanonicalStatuses.OrderBy(pair => pair.Key, StringComparer.Ordinal).Select(pair => $"{pair.Key}={pair.Value ?? "<null>"}"))
            : "<unavailable>";
        string playerStatePointer = state.HasPlayer ? $"0x{state.Player.StatePointer:X}" : "<unavailable>";
        string selectedStatePointer = state.HasSelected ? $"0x{state.Selected.StatePointer:X}" : "<unavailable>";
        string playerFlags = state.HasPlayer
            ? $"HasUnstaged={state.Player.HasUnstagedChanges} HasChanges={state.Player.HasChanges} Requires={state.Player.RequiresWriteToDisk} defaultBundlePresent={state.Player.DefaultBundlePresent} defaultBundleChangeCount={state.Player.DefaultBundleChangeCount}"
            : "HasUnstaged=<unavailable> HasChanges=<unavailable> Requires=<unavailable> defaultBundlePresent=<unavailable> defaultBundleChangeCount=<unavailable>";
        string selectedFlags = state.HasSelected
            ? $"HasUnstaged={state.Selected.HasUnstagedChanges} HasChanges={state.Selected.HasChanges} Requires={state.Selected.RequiresWriteToDisk} defaultBundlePresent={state.Selected.DefaultBundlePresent} defaultBundleChangeCount={state.Selected.DefaultBundleChangeCount}"
            : "HasUnstaged=<unavailable> HasChanges=<unavailable> Requires=<unavailable> defaultBundlePresent=<unavailable> defaultBundleChangeCount=<unavailable>";
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] CASSETTE DISK COMMIT TARGET attempt={context.Attempt.Id} phase='{phase}' eventOrdinal={context.EventOrdinal} elapsedSeconds={context.Elapsed.TotalSeconds.ToString("R", CultureInfo.InvariantCulture)} " +
            $"generation={context.Attempt.Generation} epoch={context.Attempt.Epoch} expectedSlot={context.Attempt.Slot} expectedPointer=0x{context.Attempt.Pointer:X} " +
            $"registryCount={registryCountValue} " +
            $"playerProcessorPointer={playerProcessorPointer} registeredPersistProcessorPointer={registeredPersistProcessorPointer} retainedSaveDataProcessorPointer={retainedSaveDataProcessorPointer} " +
            $"saveDataStatePointer={saveDataStatePointer} selectedSlot={selectedSlot} selectedEntryPointer={selectedEntryPointer} expectedSlotEntryPointer={expectedSlotEntryPointer} " +
            $"playerStatePointer={playerStatePointer} player{playerFlags} playerEffectiveStatuses='[{playerEffectiveStatuses}]' playerCanonicalStatuses='[{playerCanonicalStatuses}]' " +
            $"selectedStatePointer={selectedStatePointer} selected{selectedFlags} selectedEffectiveStatuses='[{selectedEffectiveStatuses}]' selectedCanonicalStatuses='[{selectedCanonicalStatuses}]' " +
            $"effectiveStatuses='player=[{playerEffectiveStatuses}];selected=[{selectedEffectiveStatuses}]' canonicalStatuses='player=[{playerCanonicalStatuses}];selected=[{selectedCanonicalStatuses}]' " +
            $"activeSongs='[{string.Join(",", context.ActiveSongs)}]' queuedSongs='[{string.Join(",", context.QueuedSongs)}]' readable={readable} stage='{stage}'.");
        }
        catch
        {
            // Target snapshots are strictly observational. Even logger/formatter failures cannot alter commit flow.
        }
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
    public const string SourceLocationName = "Roots - Level 3 - Frog and Hippo";
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
            ImplementationVersion = implementation;
            Enabled = requested &&
                      implementation.StartsWith("area-routing-plant-pipes-0.15", StringComparison.OrdinalIgnoreCase);
            _slotDataSynchronized = true;
            _compatible = Enabled && repairContractValid;
            _runtime.Configure(_slotDataSynchronized, _compatible);
        }

        Plugin.LoggerInstance?.LogWarning(
            Enabled
                ? $"[SCRC-AP] ROOTS PLANT PIPES RANDOMIZATION ENABLED implementation='{implementation}' item='{ItemName}' source='{SourceLocationName}' durableRepair={repairClaimed}. Level 3 Frog/Hippo source check is live; vanilla '{NativeAbilityFlag}' is suppressed there while '{NativeSourceMarkerFlag}' remains native, and AP ownership is reconciled against the selected save."
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
            $"[SCRC-AP] ROOTS PLANT PIPES SOURCE AP CHECK flag='{flag}' location='{SourceLocationName}'. Native Frog/Hippo source marker retained; native Plant Pipes ability grant is randomized.");
        Plugin.AP?.QueueLocation(SourceLocationName);
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

internal static class RootsBucketRandomization
{
    internal const string HipGlassesItem = "Hip Glasses";
    internal const string ChickenBucketItem = "Chicken Bucket";
    internal const string HipGlassesLocation = "Roots - Level 4 - Hip Glasses";
    internal const string BucketTradeLocation = "Roots - Bucket Minion Trade";
    internal const string HipGlassesNativeFlag = "HIP_GLASSES_BAG_ITEM";
    internal const string HipGlassesSourceFlag = "LEVEL_08_GLASSES_COLLECTED";
    internal const string BucketTradeFlag = "ROOTS_HUB_BUCKET_MINION_SWAPPED_FOR_GLASSES";
    internal const string ChickenBucketNativeFlag = "CHICKEN_BUCKET_BAG_ITEM";
    internal const string ChickenConsumedFlag = "LEVEL_09_COMBO_ABILITY_EARNED";

    private static readonly object Sync = new();
    private static object? _playerSaveRequestProcessor;
    private static bool _slotDataSynchronized;
    private static bool _compatible;
    private static int _hipGlassesReceived;
    private static int _chickenBucketReceived;
    private static bool _hipGrantAppliedThisProcess;
    private static bool _chickenGrantAppliedThisProcess;
    private static bool _nativeProbePendingLogged;

    [ThreadStatic]
    private static int _applyingArchipelagoGrant;

    internal static void Configure()
    {
        lock (Sync)
        {
            _playerSaveRequestProcessor = null;
            _slotDataSynchronized = false;
            _compatible = false;
            _hipGlassesReceived = 0;
            _chickenBucketReceived = 0;
            _hipGrantAppliedThisProcess = false;
            _chickenGrantAppliedThisProcess = false;
            _nativeProbePendingLogged = false;
        }
    }

    internal static void ApplySlotData(Dictionary<string, object>? slotData)
    {
        string implementation = ReadSlotString(slotData, "implementation_version");
        bool requested = ReadSlotBool(slotData, "randomize_hip_glasses_chicken_bucket");
        bool metadataMatches =
            string.Equals(ReadSlotString(slotData, "hip_glasses_item"), HipGlassesItem, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "chicken_bucket_item"), ChickenBucketItem, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_source_location"), HipGlassesLocation, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "bucket_minion_trade_location"), BucketTradeLocation, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_source_flag"), HipGlassesSourceFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "hip_glasses_native_flag"), HipGlassesNativeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "bucket_trade_flag"), BucketTradeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "chicken_bucket_native_flag"), ChickenBucketNativeFlag, StringComparison.Ordinal) &&
            string.Equals(ReadSlotString(slotData, "combo_bucket_conversion_flag"), ChickenConsumedFlag, StringComparison.Ordinal);
        bool compatible = RootsBucketRandomizationPolicy.IsCompatible(implementation, requested) && metadataMatches;

        lock (Sync)
        {
            _slotDataSynchronized = true;
            _compatible = compatible;
        }

        Plugin.LoggerInstance?.LogWarning(
            compatible
                ? $"[SCRC-AP] ROOTS BUCKET RANDOMIZATION ENABLED implementation='{implementation}'. Level 4 and Bucket Minion sources are AP checks; received inventory reconciles against native consumption markers."
                : $"[SCRC-AP] ROOTS BUCKET RANDOMIZATION disabled implementation='{implementation}' requested={requested} metadataMatches={metadataMatches}; native behavior remains unchanged.");

        TryFlushPendingNativeGrants();
    }

    internal static bool TryApplyItem(string itemName)
    {
        bool hip = string.Equals(itemName, HipGlassesItem, StringComparison.Ordinal);
        bool chicken = string.Equals(itemName, ChickenBucketItem, StringComparison.Ordinal);
        if (!hip && !chicken)
            return false;

        int count;
        lock (Sync)
        {
            if (hip)
                count = ++_hipGlassesReceived;
            else
                count = ++_chickenBucketReceived;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] ROOTS BUCKET ITEM RECEIVED item='{itemName}' receivedCount={count}.");
        TryFlushPendingNativeGrants();
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

    internal static void TryFlushPendingNativeGrants()
    {
        if (_applyingArchipelagoGrant != 0)
            return;

        object? processor;
        bool synchronized;
        bool compatible;
        int hipReceived;
        int chickenReceived;
        bool hipApplied;
        bool chickenApplied;
        lock (Sync)
        {
            processor = _playerSaveRequestProcessor;
            synchronized = _slotDataSynchronized;
            compatible = _compatible;
            hipReceived = _hipGlassesReceived;
            chickenReceived = _chickenBucketReceived;
            hipApplied = _hipGrantAppliedThisProcess;
            chickenApplied = _chickenGrantAppliedThisProcess;
        }

        if (!synchronized || !compatible || processor == null || (hipReceived <= 0 && chickenReceived <= 0))
            return;

        if (!TryReadProgressionFlag(HipGlassesNativeFlag, out bool hipHeld) ||
            !TryReadProgressionFlag(BucketTradeFlag, out bool hipConsumed) ||
            !TryReadProgressionFlag(ChickenBucketNativeFlag, out bool chickenHeld) ||
            !TryReadProgressionFlag(ChickenConsumedFlag, out bool chickenConsumed))
        {
            bool log;
            lock (Sync)
            {
                log = !_nativeProbePendingLogged;
                _nativeProbePendingLogged = true;
            }
            if (log)
                Plugin.LoggerInstance?.LogInfo("[SCRC-AP] ROOTS BUCKET reconciliation deferred until native progression enquiries are available.");
            return;
        }

        lock (Sync)
            _nativeProbePendingLogged = false;

        RootsBucketReconciliation result = RootsBucketRandomizationPolicy.Reconcile(
            synchronized,
            compatible,
            new RootsBucketLifecycleState(
                hipReceived,
                chickenReceived,
                hipHeld || hipApplied,
                hipConsumed,
                chickenHeld || chickenApplied,
                chickenConsumed));

        ApplyGrantDecision(processor, HipGlassesItem, HipGlassesNativeFlag, result.HipGlasses, hip: true);
        ApplyGrantDecision(processor, ChickenBucketItem, ChickenBucketNativeFlag, result.ChickenBucket, hip: false);
    }

    private static void ApplyGrantDecision(
        object processor,
        string itemName,
        string flagName,
        RootsBucketGrantDecision decision,
        bool hip)
    {
        if (decision == RootsBucketGrantDecision.Consumed)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] ROOTS BUCKET reconciliation item='{itemName}' result=consumed; received history will not restore it.");
            return;
        }
        if (decision != RootsBucketGrantDecision.Apply)
            return;

        _applyingArchipelagoGrant++;
        bool submitted;
        string detail;
        try
        {
            submitted = WeedKillerRandomization.TrySubmitProgressionFlag(processor, flagName, true, out detail);
        }
        finally
        {
            _applyingArchipelagoGrant--;
        }

        if (!submitted)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS BUCKET native grant pending item='{itemName}' flag='{flagName}'. {detail}");
            return;
        }

        lock (Sync)
        {
            if (hip)
                _hipGrantAppliedThisProcess = true;
            else
                _chickenGrantAppliedThisProcess = true;
        }
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS BUCKET NATIVE GRANT APPLIED item='{itemName}' flag='{flagName}'. {detail}");
    }

    internal static bool ShouldSuppressVanillaGrant(object request, string flag)
    {
        bool synchronized;
        bool compatible;
        lock (Sync)
        {
            synchronized = _slotDataSynchronized;
            compatible = _compatible;
        }

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        bool suppress = _applyingArchipelagoGrant == 0 &&
                        RootsBucketRandomizationPolicy.ShouldSuppressVanillaGrant(
                            synchronized,
                            compatible,
                            DeveloperHarness.CurrentRoomId,
                            flag,
                            value);
        if (suppress)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] ROOTS BUCKET VANILLA GRANT SUPPRESSED flag='{flag}' room='{DeveloperHarness.CurrentRoomId}'. Native story progression continues; inventory must come from Archipelago.");
        }
        return suppress;
    }

    internal static void RecordSourceCollected(object request, string flag)
    {
        bool active;
        lock (Sync)
            active = _slotDataSynchronized && _compatible;
        if (!active || !(ReflectionUtil.ReadBool(request, "Value") ?? false))
            return;

        string? location = null;
        if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_08", StringComparison.Ordinal) &&
            string.Equals(flag, HipGlassesSourceFlag, StringComparison.Ordinal))
            location = HipGlassesLocation;
        else if (string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_Hub2", StringComparison.Ordinal) &&
                 string.Equals(flag, BucketTradeFlag, StringComparison.Ordinal))
            location = BucketTradeLocation;

        if (location == null)
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] ROOTS BUCKET SOURCE AP CHECK flag='{flag}' location='{location}'. Native marker retained.");
        Plugin.AP?.QueueLocation(location);
    }

    internal static bool TryReadProgressionFlag(string flagName, out bool value)
    {
        value = false;
        Assembly? assembly = ReflectionUtil.GameAssembly;
        if (assembly == null)
            return false;

        Type? enquiries = ReflectionUtil.SafeGetTypes(assembly)
            .FirstOrDefault(t => string.Equals(t.Name, "CurrentPlayerSaveEnquiries", StringComparison.Ordinal));
        if (enquiries == null)
            return false;

        foreach (MethodInfo method in enquiries.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        {
            if (method.ReturnType != typeof(bool))
                continue;
            ParameterInfo[] parameters;
            try { parameters = method.GetParameters(); }
            catch { continue; }
            if (parameters.Length != 1 ||
                !parameters[0].ParameterType.Name.Contains("GameProgressionFlag", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                Type flagType = parameters[0].ParameterType;
                object flagValue = flagType.IsEnum
                    ? Enum.Parse(flagType, flagName, ignoreCase: false)
                    : Activator.CreateInstance(flagType, flagName)
                      ?? throw new InvalidOperationException($"Could not construct {flagType.FullName}.");
                object? raw = method.Invoke(null, new[] { flagValue });
                if (raw is bool result)
                {
                    value = result;
                    return true;
                }
            }
            catch { }
        }
        return false;
    }

    private static string ReadSlotString(Dictionary<string, object>? slotData, string key) =>
        slotData != null && slotData.TryGetValue(key, out object? raw)
            ? raw?.ToString() ?? string.Empty
            : string.Empty;

    private static bool ReadSlotBool(Dictionary<string, object>? slotData, string key)
    {
        if (slotData == null || !slotData.TryGetValue(key, out object? raw) || raw == null)
            return false;
        if (raw is bool value)
            return value;
        return bool.TryParse(raw.ToString(), out bool parsed) && parsed;
    }
}

#if false // Retained only as historical diagnostic source; excluded from shipped builds.
internal static class BottomHudInputDiagnostic
{
    [ThreadStatic]
    private static bool _nativeHandledInput;
    [ThreadStatic]
    private static bool _nativeProcessedHold;

    private static int _runUpdateRootLogged;
    private static int _runUpdateLogged;
    private static int _obtainStateLogged;
    private static int _obtainStateResultLogged;
    private static int _obtainPlayerStateLogged;
    private static int _registrationLogged;
    private static int _handleInputLogged;
    private static int _startEditingLogged;
    private static int _fallbackLogged;
    private static int _playerStateLogged;
    private static int _nativeHoldFallbackLogged;
    private static int _nativeHoldTriggeredLogged;
    private static readonly HashSet<string> NativeHoldProgressResults = new(StringComparer.Ordinal);
    private static readonly object HoldTraceSync = new();
    private static readonly HashSet<string> HoldTraceResults = new(StringComparer.Ordinal);
    private static IntPtr _editHoldButtonPointer;
    private static int _editHoldButtonCaptured;

    public static void RunUpdatePrefix() =>
        LogOnce(ref _runUpdateRootLogged, "RunUpdate reached");

    public static void RunUpdateForPlayerPrefix(object? __instance)
    {
        _nativeHandledInput = false;
        _nativeProcessedHold = false;
        if (__instance != null)
            CaptureEditHoldButton(__instance);
        LogOnce(ref _runUpdateLogged, "RunUpdateForPlayer reached");
    }

    public static void RunUpdateForPlayerPostfix(object? __instance, object[]? __args)
    {
        if (__instance == null || __args == null || __args.Length == 0)
            return;

        object? player = __args[0];
        if (player == null)
            return;

        try
        {
            CaptureEditHoldButton(__instance);
            MethodInfo? obtainPlayerState = __instance.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => string.Equals(m.Name, "ObtainPlayerDetailsState", StringComparison.Ordinal) &&
                                     m.GetParameters().Length == 1);
            object? playerState = obtainPlayerState?.Invoke(__instance, new[] { player });
            if (playerState != null && ShouldLog(ref _playerStateLogged))
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] BOTTOM HUD PLAYER STATE editMode={ReflectionUtil.ReadMember(playerState, "EditMode")?.ToString() ?? "<unavailable>"} " +
                    $"isNotEditing={ReflectionUtil.ReadMember(playerState, "IsNotEditing")?.ToString() ?? "<unavailable>"} " +
                    $"isEditing={ReflectionUtil.ReadMember(playerState, "IsEditing")?.ToString() ?? "<unavailable>"} " +
                    $"holdButtonStateAvailable={ReflectionUtil.ReadMember(playerState, "HoldButtonState") != null} " +
                    "readOnly=True mutationRequested=False.");
            }
            if (!BottomHudInputPolicy.ShouldRunFallback(
                    AreaAccessPrototype.Enabled,
                    IntroHubSkip.Compatible,
                    AreaAccessPrototype.HasArea("Roots"),
                    DeveloperHarness.CurrentRoomId,
                    playerState != null,
                    _nativeHandledInput))
                return;

            MethodInfo? handlePlayerInput = __instance.GetType().GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .FirstOrDefault(m => string.Equals(m.Name, "HandlePlayerInput", StringComparison.Ordinal) &&
                                     m.GetParameters().Length == 2);
            if (handlePlayerInput == null)
                return;

            handlePlayerInput.Invoke(__instance, new[] { player, playerState });
            LogOnce(ref _fallbackLogged,
                "fallback supplied existing player state to native HandlePlayerInput; no selection synthesized");

            DriveNativeHoldIfNeeded(__instance, __args, player, playerState);
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] BOTTOM HUD INPUT fallback failed safely: {ex.GetBaseException().Message}");
        }
    }

    public static void HoldCheckPostfix(object? __instance, MethodBase? __originalMethod, ref bool __result)
    {
        if (__instance == null || __originalMethod == null ||
            NativePointer(__instance) == IntPtr.Zero ||
            NativePointer(__instance) != _editHoldButtonPointer)
            return;

        _nativeProcessedHold = true;

        string key = $"{__originalMethod.Name}={__result}";
        lock (HoldTraceSync)
        {
            if (!HoldTraceResults.Add(key))
                return;
        }
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD HOLD {key} room='{DeveloperHarness.CurrentRoomId}' readOnly=True mutationRequested=False.");
    }

    private static void DriveNativeHoldIfNeeded(
        object controller,
        object[] runUpdateArgs,
        object player,
        object playerState)
    {
        bool isNotEditing = ReflectionUtil.ReadMember(playerState, "IsNotEditing") is bool value && value;
        if (!BottomHudInputPolicy.ShouldDriveNativeHold(
                AreaAccessPrototype.Enabled,
                IntroHubSkip.Compatible,
                AreaAccessPrototype.HasArea("Roots"),
                DeveloperHarness.CurrentRoomId,
                isNotEditing,
                _nativeProcessedHold))
            return;

        object? holdLogic = ReflectionUtil.ReadMember(controller, "editHoldButtonLogic");
        object? holdState = ReflectionUtil.ReadMember(playerState, "HoldButtonState");
        if (holdLogic == null || holdState == null || runUpdateArgs.Length < 2)
            return;

        MethodInfo? runHold = holdLogic.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => string.Equals(m.Name, "RunUpdateForPlayer", StringComparison.Ordinal) &&
                                 m.GetParameters().Length == 6);
        if (runHold == null)
            return;

        float deltaTime = Convert.ToSingle(runUpdateArgs[1]);
        object?[] holdArgs = { deltaTime, player, holdState, true, false, 0f };
        runHold.Invoke(holdLogic, holdArgs);
        LogOnce(ref _nativeHoldFallbackLogged,
            "fallback advanced the existing native edit hold state because vanilla skipped it");

        bool didTrigger = holdArgs[4] is bool triggerValue && triggerValue;
        float fill = holdArgs[5] == null ? -1f : Convert.ToSingle(holdArgs[5]);
        int fillPercent = fill < 0f ? -1 : Math.Clamp((int)Math.Round(fill * 100f), 0, 100);
        string progressKey = $"triggered={didTrigger} fillPercent={fillPercent}";
        lock (HoldTraceSync)
        {
            if (NativeHoldProgressResults.Add(progressKey))
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] BOTTOM HUD NATIVE HOLD {progressKey} readOnly=True mutationRequested=False.");
            }
        }

        if (!didTrigger)
            return;

        MethodInfo? startEditing = controller.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m => string.Equals(m.Name, "StartEditing", StringComparison.Ordinal) &&
                                 m.GetParameters().Length == 1);
        if (startEditing == null)
            return;
        startEditing.Invoke(controller, new[] { player });
        LogOnce(ref _nativeHoldTriggeredLogged,
            "native edit hold completed and controller StartEditing was invoked");
    }

    private static void CaptureEditHoldButton(object controller)
    {
        if (_editHoldButtonPointer != IntPtr.Zero)
            return;
        object? hold = ReflectionUtil.ReadMember(controller, "editHoldButtonLogic");
        IntPtr pointer = hold == null ? IntPtr.Zero : NativePointer(hold);
        if (pointer == IntPtr.Zero)
            return;
        _editHoldButtonPointer = pointer;
        if (Interlocked.Exchange(ref _editHoldButtonCaptured, 1) != 0)
            return;
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD HOLD captured rewiredActionId={ReflectionUtil.ReadMember(hold, "rewiredActionId")?.ToString() ?? "<unavailable>"} " +
            $"actionForBlockChecks={ReflectionUtil.ReadMember(hold, "actionForBlockChecks")?.ToString() ?? "<unavailable>"} " +
            $"holdTimeRequired={ReflectionUtil.ReadMember(hold, "holdTimeRequired")?.ToString() ?? "<unavailable>"} " +
            $"includeGlobalKeyboard={ReflectionUtil.ReadMember(hold, "includeGlobalKeyboard")?.ToString() ?? "<unavailable>"} " +
            $"includeGlobalMouse={ReflectionUtil.ReadMember(hold, "includeGlobalMouse")?.ToString() ?? "<unavailable>"} " +
            "readOnly=True mutationRequested=False.");
    }

    private static IntPtr NativePointer(object value)
    {
        try
        {
            return value.GetType().GetProperty(
                       "Pointer",
                       BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(value) is IntPtr pointer
                ? pointer
                : IntPtr.Zero;
        }
        catch
        {
            return IntPtr.Zero;
        }
    }

    public static void ObtainStatePrefix() =>
        LogOnce(ref _obtainStateLogged, "ObtainState reached");

    public static void ObtainStatePostfix(object? __result)
    {
        if (!ShouldLog(ref _obtainStateResultLogged))
            return;
        object? states = __result == null
            ? null
            : ReflectionUtil.ReadMember(__result, "PlayerDetailsSwitcherStates") ??
              ReflectionUtil.ReadMember(__result, "_PlayerDetailsSwitcherStates_k__BackingField");
        object? count = states == null ? null : ReflectionUtil.ReadMember(states, "Count");
        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD INPUT state resultAvailable={__result != null} playerStateDictionaryAvailable={states != null} playerStateCount={count?.ToString() ?? "<unavailable>"} readOnly=True mutationRequested=False.");
    }

    public static void ObtainPlayerDetailsStatePrefix() =>
        LogOnce(ref _obtainPlayerStateLogged, "ObtainPlayerDetailsState reached");

    public static void HandlePlayerInputPrefix()
    {
        _nativeHandledInput = true;
        LogOnce(ref _handleInputLogged, "HandlePlayerInput reached");
    }

    public static void StartEditingPrefix() =>
        LogOnce(ref _startEditingLogged, "StartEditing reached");

    public static void NewPlayerRegisteredPrefix() =>
        LogOnce(ref _registrationLogged, "NewPlayerWasRegisteredEvent reached");

    private static void LogOnce(ref int gate, string message)
    {
        if (!ShouldLog(ref gate))
            return;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BOTTOM HUD INPUT {message} room='{DeveloperHarness.CurrentRoomId}' readOnly=True mutationRequested=False.");
    }

    private static bool ShouldLog(ref int gate) =>
        AreaAccessPrototype.Enabled &&
        IntroHubSkip.Compatible &&
        AreaAccessPrototype.HasArea("Roots") &&
        string.Equals(DeveloperHarness.CurrentRoomId, RootsComputerPolicy.RoomId, StringComparison.OrdinalIgnoreCase) &&
        Interlocked.Exchange(ref gate, 1) == 0;
}
#endif

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
    private long _lastTimestamp;

    public CassetteReceiptReconciliationKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        TimeSpan elapsed = _lastTimestamp == 0
            ? TimeSpan.Zero
            : TimeSpan.FromSeconds((double)(now - _lastTimestamp) / System.Diagnostics.Stopwatch.Frequency);
        _lastTimestamp = now;
        CassetteReceiptRandomization.TickUnity(elapsed);
    }
}


internal static class GarageCartridgeAccess
{
    internal readonly record struct CartridgeDefinition(
        string Song,
        string ItemName,
        string RelativePath);

    internal static readonly CartridgeDefinition[] Cartridges =
    {
        new("Bloody Tears", "Bloody Tears Cartridge", "CartridgeHolder_BloodyTears/GR27_GameCartridge_BloodyTears"),
        new("Gradius Remix", "Gradius Remix Cartridge", "CartridgeHolder_LoveShine/GR27_GameCartridge_Gradius"),
        new("Smooch", "Smooch Cartridge", "CartridgeHolder_Smooch/GR27_GameCartridge_Smooch"),
        new("Superstar", "Superstar Cartridge", "CartridgeHolder_StarEater/GR27_GameCartridge_Superstar"),
        new("Vampire Killer", "Vampire Killer Cartridge", "CartridgeHolder_VampireKiller/GR27_GameCartridge_VampireKiller"),
        new("Wag the Dog", "Wag the Dog Cartridge", "CartridgeHolder_SuperCrazyRhythmCastle/GR27_GameCartridge_WagTheDog"),
    };

    private static readonly object Sync = new();
    private static readonly HashSet<string> OwnedSongs = new(StringComparer.OrdinalIgnoreCase);
    public static bool Enabled { get; private set; }
    public static bool SourceRandomizationEnabled { get; private set; }
    public static bool VanillaEntranceEnabled { get; private set; }
    public static string ImplementationVersion { get; private set; } = string.Empty;

    public static void Configure()
    {
        Enabled = false;
        SourceRandomizationEnabled = false;
        VanillaEntranceEnabled = false;
        ImplementationVersion = string.Empty;
        lock (Sync)
        {
            OwnedSongs.Clear();
        }
    }

    public static bool TryApplyItem(string itemName)
    {
        foreach (CartridgeDefinition cartridge in Cartridges)
        {
            if (!string.Equals(cartridge.ItemName, itemName, StringComparison.OrdinalIgnoreCase))
                continue;

            bool added;
            lock (Sync)
                added = OwnedSongs.Add(cartridge.Song);

            Plugin.LoggerInstance?.LogWarning(
                added
                    ? $"[SCRC-AP] GAME GARAGE CARTRIDGE RECEIVED song='{cartridge.Song}' item='{cartridge.ItemName}' routingEnabled={Enabled}."
                    : $"[SCRC-AP] GAME GARAGE CARTRIDGE already owned song='{cartridge.Song}' item='{cartridge.ItemName}' routingEnabled={Enabled}.");
            return true;
        }

        return false;
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

    public static bool ShouldSuppressVanillaSourceGrant(object request, string flag)
    {
        if (!SourceRandomizationEnabled || string.Equals(DeveloperHarness.CurrentRoomId, "GameRoom_27", StringComparison.Ordinal))
            return false;

        bool value = ReflectionUtil.ReadBool(request, "Value") ?? false;
        if (!value || !TryParseCartridgeProgressionFlag(flag, out string song, out string kind))
            return false;

        if (!string.Equals(kind, "BAG_ITEM", StringComparison.Ordinal))
            return false;

        if (!GarageVanillaEntrancePolicy.ShouldSuppressSourceGrant(VanillaEntranceEnabled, song))
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
        if (!value || !TryParseCartridgeProgressionFlag(flag, out string song, out string kind) ||
            !string.Equals(kind, "COLLECTED", StringComparison.Ordinal))
            return;

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

    private static bool TryParseCartridgeProgressionFlag(string flag, out string song, out string kind)
    {
        song = string.Empty;
        kind = string.Empty;
        if (string.IsNullOrWhiteSpace(flag) ||
            !flag.Contains("CARTRIDGE", StringComparison.OrdinalIgnoreCase))
            return false;

        if (flag.EndsWith("_BAG_ITEM", StringComparison.OrdinalIgnoreCase))
            kind = "BAG_ITEM";
        else if (flag.EndsWith("_COLLECTED", StringComparison.OrdinalIgnoreCase))
            kind = "COLLECTED";
        else
            return false;

        string compact = new string(flag.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
        if (compact.Contains("bloodytears")) song = "Bloody Tears";
        else if (compact.Contains("gradius")) song = "Gradius Remix";
        else if (compact.Contains("smooch")) song = "Smooch";
        else if (compact.Contains("superstar")) song = "Superstar";
        else if (compact.Contains("vampirekiller")) song = "Vampire Killer";
        else if (compact.Contains("wagthedog") || compact.Contains("supercrazyrhythmcastle")) song = "Wag the Dog";
        else return false;

        return true;
    }

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
    private const string GarageRoomId = "GameRoom_27";
    private const string CartridgeRootPath = "Root/GameRoom_27_Logic/Objects/Cartridges";

    private readonly Dictionary<string, GameObject> _objects =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _releasedThisVisit =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _lastOwned =
        new(StringComparer.OrdinalIgnoreCase);

    private string _lastRoom = string.Empty;
    private float _nextPoll;
    private bool _waitingLogged;

    public GarageCartridgeAccessKeeper(IntPtr pointer) : base(pointer)
    {
    }

    private void Update()
    {
        string room = DeveloperHarness.CurrentRoomId;
        if (!string.Equals(room, _lastRoom, StringComparison.Ordinal))
        {
            _lastRoom = room;
            _objects.Clear();
            _releasedThisVisit.Clear();
            _lastOwned.Clear();
            _waitingLogged = false;
            _nextPoll = 0f;
        }

        if (!GarageCartridgeAccess.Enabled || !string.Equals(room, GarageRoomId, StringComparison.Ordinal))
            return;

        if (Time.unscaledTime < _nextPoll)
            return;
        _nextPoll = Time.unscaledTime + 0.15f;

        if (!EnsureBindings())
            return;

        foreach (GarageCartridgeAccess.CartridgeDefinition cartridge in GarageCartridgeAccess.Cartridges)
        {
            if (!_objects.TryGetValue(cartridge.Song, out GameObject? obj) || obj == null)
                continue;

            bool owned = GarageCartridgeAccess.HasCartridge(cartridge.Song);
            GarageObjectDecision decision = GarageAvailabilityPolicy.Decide(
                enabled: true,
                compatible: true,
                ownsCartridge: owned,
                role: GarageObjectRole.SongCartridge);
            if (decision == GarageObjectDecision.Inactive)
            {
                if (obj.activeSelf)
                    obj.SetActive(false);
                _releasedThisVisit.Remove(cartridge.Song);
            }
            else if (decision == GarageObjectDecision.Active &&
                     !_releasedThisVisit.Contains(cartridge.Song))
            {
                // Release the real Garage cartridge exactly once per room visit.
                // After this, vanilla may reparent/deactivate it while the player
                // carries/inserts it; do not continuously force it active.
                if (!obj.activeSelf)
                    obj.SetActive(true);
                _releasedThisVisit.Add(cartridge.Song);
                MusicLabDiscovery.NotifyRandomizedGarageCartridgeReleased(cartridge.Song);
            }

            if (!_lastOwned.TryGetValue(cartridge.Song, out bool previousOwned) || previousOwned != owned)
            {
                _lastOwned[cartridge.Song] = owned;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] GAME GARAGE CARTRIDGE STATE song='{cartridge.Song}' owned={owned} objectActive={obj.activeSelf} releasedThisVisit={_releasedThisVisit.Contains(cartridge.Song)}.");
            }
        }
    }

    private bool EnsureBindings()
    {
        if (_objects.Count == GarageCartridgeAccess.Cartridges.Length)
            return true;

        GameObject? root = GameObject.Find(CartridgeRootPath);
        if (root == null)
        {
            if (!_waitingLogged)
            {
                _waitingLogged = true;
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] GAME GARAGE CARTRIDGE ROUTING waiting for '{CartridgeRootPath}'.");
            }
            return false;
        }

        _waitingLogged = false;
        foreach (GarageCartridgeAccess.CartridgeDefinition cartridge in GarageCartridgeAccess.Cartridges)
        {
            Transform? tr = null;
            try { tr = root.transform.Find(cartridge.RelativePath); }
            catch { }
            if (tr != null && tr.gameObject != null)
                _objects[cartridge.Song] = tr.gameObject;
        }

        if (_objects.Count == GarageCartridgeAccess.Cartridges.Length)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] GAME GARAGE CARTRIDGE ROUTING BINDINGS READY: all six native cartridge objects resolved.");
            return true;
        }

        return false;
    }
}

internal static class AreaPhoneConditionPatches
{
    private static readonly HashSet<string> Logged = new(StringComparer.OrdinalIgnoreCase);

    public static bool BoolConditionPrefix(object? __instance, ref bool __result, MethodBase __originalMethod)
    {
        try
        {
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

    public static bool Enabled { get; private set; }
    public static bool APDrivenStartingArea { get; private set; }
    public static string StartingArea { get; private set; } = "AP precollected item";

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
        if (!Enabled || !APDrivenStartingArea)
            return;

        if (slotData == null || slotData.Count == 0)
        {
            Plugin.LoggerInstance?.LogWarning(
                "[SCRC-AP] AREA ACCESS SLOT DATA missing/empty; all major-area phones remain locked until a normal Area Access item is received.");
            return;
        }

        string implementationVersion = slotData.TryGetValue("implementation_version", out object? rawVersion)
            ? rawVersion?.ToString() ?? string.Empty
            : string.Empty;

        string starterItem = slotData.TryGetValue("starting_area_item", out object? rawStarter)
            ? rawStarter?.ToString() ?? string.Empty
            : string.Empty;

        string starterArea = slotData.TryGetValue("starting_area", out object? rawArea)
            ? rawArea?.ToString() ?? string.Empty
            : string.Empty;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] AREA ACCESS SLOT DATA implementation='{implementationVersion}' startingAreaItem='{starterItem}' startingArea='{starterArea}'.");

        if (!implementationVersion.StartsWith("area-routing", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] AREA ACCESS SEED INCOMPATIBLE: server slot data implementation='{implementationVersion}'. Generate AND HOST a NEW seed with APWorld v0.13; replacing the installed .apworld does not change an already-generated seed. All major-area phones remain locked for safety.");
            return;
        }

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

        if (Input.GetKeyDown(KeyCode.F1))
        {
            bool shift =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            if (alt && shift)
                DeveloperHarness.ResetLevel8Locally();
            else if (alt)
                DeveloperHarness.GrantLevel8Locally();
            else if (control && shift)
                DeveloperHarness.ResetLevel7Locally();
            else if (control)
                DeveloperHarness.GrantLevel7Locally();
            else if (shift)
                DeveloperHarness.ResetLevel6Locally();
            else
                DeveloperHarness.GrantLevel6Locally();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            bool shift =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (alt && shift)
                DeveloperHarness.ResetLevel10Locally();
            else if (alt)
                DeveloperHarness.GrantLevel10Locally();
            else if (control && shift)
                DeveloperHarness.ResetLevel9Locally();
            else if (control)
                DeveloperHarness.GrantLevel9Locally();
            else
                DeveloperHarness.ResetLevel5Locally();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            if (control)
                DeveloperHarness.GrantLevel11Locally();
            else
                DeveloperHarness.ResetLevel11Locally();
        }

        if (Input.GetKeyDown(KeyCode.F4))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool shift =
                Input.GetKey(KeyCode.LeftShift) ||
                Input.GetKey(KeyCode.RightShift);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (shift)
                DeveloperHarness.CycleMusicLabPointOverride();
            else if (control)
                DeveloperHarness.GrantLevel5Locally();
            else if (DiagnosticHotkeyRouting.ForF4(DeveloperHarness.CurrentRoomId, control, shift, alt) == DiagnosticHotkeyAction.Level4ToLiftQuest)
                Level5Discovery.ManualBegin();
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
                MusicLabDiscovery.ScanNativeIdentityCandidates();

            if (control)
                DeveloperHarness.GrantLevel12Locally();
            else if (alt)
                DeveloperHarness.ResetLevel12Locally();
            else if (shift)
                DeveloperHarness.RetryBunkerStarRequirementOnce();
            else if (DiagnosticHotkeyRouting.ForPlainF5(DeveloperHarness.CurrentRoomId) == DiagnosticHotkeyAction.Level4ToLiftQuest)
                Level5Discovery.ScanCurrentScene();
            else
                DeveloperHarness.ScanPostAct1RouteObjects();
        }

        if (Input.GetKeyDown(KeyCode.F6))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (control)
                DeveloperHarness.GrantLevel13Locally();
            else if (alt)
                DeveloperHarness.ResetLevel13Locally();
            else
                DeveloperHarness.ForceEnterLevel4();
        }

        if (Input.GetKeyDown(KeyCode.F7))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (control)
                DeveloperHarness.GrantLevel14Locally();
            else if (alt)
                DeveloperHarness.ResetLevel14Locally();
            else
                DeveloperHarness.ResetLevel4Locally();
        }

        if (Input.GetKeyDown(KeyCode.F8))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (control)
                DeveloperHarness.GrantLevel15Locally();
            else if (alt)
                DeveloperHarness.ResetLevel15Locally();
            else
                DeveloperHarness.ResetAndReplayPostLevel1Hub();
        }

        if (Input.GetKeyDown(KeyCode.F9))
        {
            bool control =
                Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);

            bool alt =
                Input.GetKey(KeyCode.LeftAlt) ||
                Input.GetKey(KeyCode.RightAlt);

            if (control)
                DeveloperHarness.GrantLevel16Locally();
            else if (alt)
                DeveloperHarness.ResetLevel16Locally();
            else
                DeveloperHarness.GrantLevel2Locally();
        }

        if (Input.GetKeyDown(KeyCode.F10))
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

            if (control && shift)
                DeveloperHarness.GrantLevel22Locally();
            else if (alt && shift)
                DeveloperHarness.ResetLevel22Locally();
            else if (control)
                DeveloperHarness.GrantLevel17Locally();
            else if (alt)
                DeveloperHarness.ResetLevel17Locally();
            else
                DeveloperHarness.GrantLevel3Locally();
        }

        if (Input.GetKeyDown(KeyCode.F11))
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

            if (control && shift)
                DeveloperHarness.GrantLevel21Locally();
            else if (alt && shift)
                DeveloperHarness.ResetLevel21Locally();
            else if (control)
                DeveloperHarness.GrantLevel18Locally();
            else if (alt)
                DeveloperHarness.ResetLevel18Locally();
            else
                DeveloperHarness.ResetLevel3AndReloadHub();
        }

        if (Input.GetKeyDown(KeyCode.F12))
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

            if (control && shift)
                DeveloperHarness.GrantLevel20Locally();
            else if (alt && shift)
                DeveloperHarness.ResetLevel20Locally();
            else if (control)
                DeveloperHarness.GrantLevel19Locally();
            else if (alt)
                DeveloperHarness.ResetLevel19Locally();
            else
                DeveloperHarness.GrantLevel4Locally();
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
        EarlySequenceBlockerPatches.NewSaveCreated();
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

        // The Hub6 phone prefab can still produce its transition request even
        // when the child OnTrigger reactor is inactive. Treat this request
        // boundary as the authoritative Area Access gate. Only departures
        // from Hub6 are filtered; world booths returning to Hub6 and all
        // normal intra-area room transitions remain untouched.
        string originRoom = DeveloperHarness.CurrentRoomId;
        if (AreaAccessPrototype.Enabled &&
            string.Equals(originRoom, "GameRoom_Hub6", StringComparison.Ordinal) &&
            roomId != null &&
            AreaAccessPrototype.TryGetAreaForHubRoom(roomId, out AreaAccessPrototype.AreaDefinition destinationArea) &&
            !AreaAccessPrototype.HasArea(destinationArea.AreaName))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] AREA PHONE TRANSITION BLOCKED area='{destinationArea.AreaName}' destination='{roomId}' origin='{originRoom}' reason='Area Access locked'.");
            return false;
        }

        if (__instance != null &&
            !RootsIntroCutsceneBypass.ShouldAllowTransition(
                __instance, __originalMethod, __args, roomId))
            return false;

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

        // v0.67.30: cassette stages did not reliably issue
        // SetScoredSongInCurrentLevelRequest at startup. While a stage launched
        // directly from Hub6 is active, poll read-only static enquiry getters for
        // a few seconds instead. This is identity discovery only; no AP location
        // is awarded from merely starting a cassette.
        if (!MusicLabDiscovery.IsCassetteStageInProgress)
            return;

        if (Time.unscaledTime < _nextMusicLabPoll)
            return;

        _nextMusicLabPoll = Time.unscaledTime + 0.25f;
        MusicLabDiscovery.PollCassetteStartState();
    }
}

internal static class MusicLabDiscovery
{
    public const string Hub6RoomId = "GameRoom_Hub6";
    public const string GarageRoomId = "GameRoom_27";

    private static readonly object Sync = new();
    private static readonly Harmony DiagnosticHarmony = new(Plugin.PluginGuid + ".musiclabdiag");
    private static readonly HashSet<MethodBase> PatchedGarageMethods = new();
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
    private static bool _garageSongHookInstalled;
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
    private static bool _garageHookMissingNoticeLogged;
    private static bool _currentSongPollFailureLogged;

    // v0.67.30 cassette-start discovery. These are read-only runtime probes and
    // are reset whenever Hub6 launches a non-Garage music stage.
    private static int _cassetteStartPollCount;
    private static bool _cassetteStartGetterProbeResolved;
    private static readonly List<MethodInfo> CassetteStartGetterMethods = new();
    private static readonly Dictionary<string, string> CassetteStartLastValues = new(StringComparer.Ordinal);

    public static bool GarageSongHookInstalled
    {
        get
        {
            lock (Sync)
                return _garageSongHookInstalled;
        }
    }

    public static bool IsMusicLabRoom(string? room) =>
        string.Equals(room, Hub6RoomId, StringComparison.Ordinal) ||
        string.Equals(room, GarageRoomId, StringComparison.Ordinal);

    public static bool IsCassetteStageInProgress
    {
        get
        {
            lock (Sync)
                return _musicLabStageInProgress;
        }
    }

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
                _cassetteStartPollCount = 0;
                _cassetteStartGetterProbeResolved = false;
                CassetteStartGetterMethods.Clear();
                CassetteStartLastValues.Clear();
            }

            if (toHub6)
            {
                _musicLabStageInProgress = false;
                _cassetteStartPollCount = 0;
                _cassetteStartGetterProbeResolved = false;
                CassetteStartGetterMethods.Clear();
                CassetteStartLastValues.Clear();
            }

            if (toGarage)
            {
                _currentGarageSong = string.Empty;
                _garageHookMissingNoticeLogged = false;
                _currentSongPollFailureLogged = false;
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

    public static void NotifyRandomizedGarageCartridgeReleased(string song)
    {
        lock (Sync)
        {
            // If the diagnostic baseline already exists, a cartridge received
            // while standing in Game Garage must become an insertion candidate
            // immediately. If the baseline is not ready yet, the normal first
            // poll will rebuild it from the AP-filtered state.
            if (_garageCartridgeBaselineReady)
                GarageCartridgeBaselineActive[song] = true;
        }
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

    public static void RecordGarageScoredSongSequenceBegin(object instance)
    {
        if (!string.Equals(DeveloperHarness.CurrentRoomId, GarageRoomId, StringComparison.Ordinal))
            return;

        Transform? transform = null;
        try
        {
            if (instance is Component component)
                transform = component.transform;
            else
                transform = ReflectionUtil.UnwrapNullable(
                    ReflectionUtil.ReadMember(instance, "transform")) as Transform;
        }
        catch { }

        string hierarchy = string.Empty;
        if (transform != null)
        {
            try
            {
                var names = new List<string>();
                Transform? current = transform;
                for (int i = 0; i < 24 && current != null; i++)
                {
                    names.Add(current.name ?? "<unnamed>");
                    current = current.parent;
                }

                names.Reverse();
                hierarchy = string.Join("/", names);
            }
            catch { }
        }

        string friendly = FriendlyGarageSong(hierarchy);
        if (string.IsNullOrWhiteSpace(friendly))
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB GARAGE exact scored-song Begin observed but hierarchy did not map to a known cartridge; instanceType='{instance?.GetType().FullName ?? "<null>"}' hierarchy='{(string.IsNullOrWhiteSpace(hierarchy) ? "<unavailable>" : hierarchy)}'.");
            return;
        }

        lock (Sync)
            _currentGarageSong = friendly;

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB GARAGE SONG CAPTURE source='SetScoredSongInCurrentLevelSequenceStep.Begin' song='{friendly}' hierarchy='{hierarchy}'.");

        if (_active)
            Record("GARAGE SONG SEQUENCE BEGIN", $"song='{friendly}' hierarchy='{hierarchy}'");
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

    private static void ResolveCassetteStartGetterMethods()
    {
        lock (Sync)
        {
            if (_cassetteStartGetterProbeResolved)
                return;

            _cassetteStartGetterProbeResolved = true;
            CassetteStartGetterMethods.Clear();
        }

        var found = new List<MethodInfo>();

        // v0.67.36: v0.67.30 proved these exact LevelEnquiries getters become
        // readable a few polls after GameRoom_10 loads. Use only those known,
        // read-only getters now instead of rediscovering/dumping two dozen
        // enquiry methods for every cassette start.
        try
        {
            Type? levelType = FindExactGameType("LevelEnquiries");
            if (levelType != null)
            {
                string[] names =
                {
                    "GetCurrentLevelIdentifier",
                    "GetCurrentLevelVariantIdentifier",
                    "GetCurrentLevelState",
                };

                foreach (string name in names)
                {
                    MethodInfo? method = levelType.GetMethod(
                        name,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                        binder: null,
                        types: Type.EmptyTypes,
                        modifiers: null);
                    if (method != null)
                        found.Add(method);
                }
            }

            Type? scoringType = FindExactGameType("LevelScoringEnquiries");
            if (scoringType != null)
            {
                MethodInfo? rewardType = scoringType.GetMethod(
                    "GetActiveLevelVariantRewardType",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                    binder: null,
                    types: Type.EmptyTypes,
                    modifiers: null);
                if (rewardType != null)
                    found.Add(rewardType);
            }
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE START focused getter setup failed: {ex.GetBaseException().Message}");
        }

        lock (Sync)
        {
            CassetteStartGetterMethods.Clear();
            CassetteStartGetterMethods.AddRange(found);
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] MUSIC LAB CASSETTE START GETTER PROBE READY: focusedCandidates={found.Count}. Read-only LevelEnquiries/LevelScoringEnquiries getters only.");
    }

    public static void PollCassetteStartState()
    {
        string room = DeveloperHarness.CurrentRoomId;
        if (string.Equals(room, Hub6RoomId, StringComparison.Ordinal) ||
            string.Equals(room, GarageRoomId, StringComparison.Ordinal))
            return;

        int poll;
        lock (Sync)
        {
            if (!_musicLabStageInProgress)
                return;

            if (_cassetteStartPollCount >= 16)
                return;

            _cassetteStartPollCount++;
            poll = _cassetteStartPollCount;
        }

        ResolveCassetteStartGetterMethods();

        List<MethodInfo> methods;
        lock (Sync)
            methods = CassetteStartGetterMethods.ToList();

        bool emittedUsefulValue = false;
        foreach (MethodInfo method in methods)
        {
            object? value;
            try
            {
                value = ReflectionUtil.UnwrapNullable(method.Invoke(null, null));
            }
            catch (Exception ex)
            {
                if (poll == 1)
                {
                    string methodName;
                    try { methodName = $"{method.DeclaringType?.FullName}.{method.Name}"; }
                    catch { methodName = "<metadata unavailable>"; }
                    Plugin.LoggerInstance?.LogInfo(
                        $"[SCRC-AP] MUSIC LAB CASSETTE START GETTER invoke skipped getter='{methodName}' reason='{ex.GetBaseException().Message}'.");
                }
                continue;
            }

            if (value == null)
                continue;

            string text = SafeDiagnosticValue(value);
            if (string.IsNullOrWhiteSpace(text) ||
                string.Equals(text, "<null>", StringComparison.OrdinalIgnoreCase))
                continue;

            string key = $"{method.DeclaringType?.FullName}.{method.Name}";
            bool changed;
            lock (Sync)
            {
                changed = !CassetteStartLastValues.TryGetValue(key, out string? oldValue) ||
                          !string.Equals(oldValue, text, StringComparison.Ordinal);
                if (changed)
                    CassetteStartLastValues[key] = text;
            }

            if (!changed)
                continue;

            emittedUsefulValue = true;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE START STATE room='{room}' poll={poll} getter='{key}' value='{text}'.");

            Type valueType = value.GetType();
            bool simple =
                value is string || value is bool || value is byte || value is sbyte ||
                value is short || value is ushort || value is int || value is uint ||
                value is long || value is ulong || value is float || value is double ||
                value is decimal || valueType.IsEnum ||
                valueType.Name.Contains("Identifier", StringComparison.OrdinalIgnoreCase);

            bool unityNamespace =
                valueType.Namespace?.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase) == true;

            if (!simple && !unityNamespace)
            {
                DumpRelevantMembers(
                    value,
                    $"CASSETTE START STATE {method.DeclaringType?.Name}.{method.Name}",
                    new[] { "song", "track", "level", "variant", "identifier", "current", "scored" });
            }
        }

        if (poll == 1)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE START PROBE ACTIVE room='{room}'. Wait 2-4 seconds; no AP location is awarded by this probe.");
        }
        else if (poll == 16 && !emittedUsefulValue)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB CASSETTE START PROBE COMPLETE room='{room}' with no new readable current song/level/variant value on the final poll.");
        }
    }

    public static void PollGarageCurrentSong()
    {
        if (!_active || !string.Equals(DeveloperHarness.CurrentRoomId, GarageRoomId, StringComparison.Ordinal))
            return;

        ResolveScoringProbeMethods();
        if (_getCurrentSongMethod == null)
            return;

        try
        {
            object? value = ReflectionUtil.UnwrapNullable(_getCurrentSongMethod.Invoke(null, null));
            string rawSong = ReflectionUtil.ExtractIdentifier(value) ?? value?.ToString() ?? string.Empty;
            string friendly = FriendlyGarageSong(rawSong);
            if (string.IsNullOrWhiteSpace(friendly))
                return;

            bool changed;
            lock (Sync)
            {
                changed = !string.Equals(_currentGarageSong, friendly, StringComparison.Ordinal);
                _currentGarageSong = friendly;
            }

            if (changed)
            {
                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] MUSIC LAB GARAGE LIVE SONG CAPTURE raw='{rawSong}' mapped='{friendly}'.");
            }
        }
        catch (Exception ex)
        {
            if (_currentSongPollFailureLogged)
                return;

            _currentSongPollFailureLogged = true;
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB GARAGE live GetCurrentSong poll failed once: {ex.GetBaseException().Message}");
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
        TryInstallGarageSongHook();
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
            $"[SCRC-AP] MUSIC LAB GARAGE selectedSong='{(string.IsNullOrWhiteSpace(current) ? "<unknown/not started>" : current)}' runtimeHookInstalled={GarageSongHookInstalled}.");
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

    public static void TryInstallGarageSongHook()
    {
        if (!DeveloperHarness.Enabled ||
            !string.Equals(DeveloperHarness.CurrentRoomId, GarageRoomId, StringComparison.Ordinal))
        {
            return;
        }

        int patchedNow = 0;
        int exactObjectsFound = 0;

        foreach (string sequenceName in GarageSequenceNames)
        {
            string path =
                $"Root/GameRoom_27_Logic/GameRoom_27_Script/MiscSequences/{sequenceName}/SetScoredSong";

            GameObject? go = GameObject.Find(path);
            if (go == null)
                continue;

            exactObjectsFound++;

            Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppArrayBase<Component> components;
            try { components = go.GetComponents<Component>(); }
            catch { continue; }

            foreach (Component component in components)
            {
                if (component == null)
                    continue;

                Type type = component.GetType();
                string fullTypeName = type.FullName ?? type.Name ?? string.Empty;

                // The lookup is already restricted to the six exact
                // Play*/SetScoredSong GameObjects. Do not rely on the wrapper type
                // name containing "song"; v0.67.13 proved that assumption false.
                // Skip only obvious Unity engine components and inspect declared Begin.
                if (fullTypeName.StartsWith("UnityEngine.", StringComparison.OrdinalIgnoreCase))
                    continue;

                MethodInfo? begin = null;
                try
                {
                    begin = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .FirstOrDefault(m =>
                            string.Equals(m.Name, "Begin", StringComparison.Ordinal) &&
                            m.DeclaringType == type);
                }
                catch { }

                if (begin == null)
                    continue;

                lock (Sync)
                {
                    if (PatchedGarageMethods.Contains(begin))
                        continue;
                }

                try
                {
                    MethodInfo? prefix = typeof(MusicLabDiscovery).GetMethod(
                        nameof(GarageSetScoredSongPrefix),
                        BindingFlags.NonPublic | BindingFlags.Static);

                    if (prefix == null)
                        continue;

                    DiagnosticHarmony.Patch(begin, prefix: new HarmonyMethod(prefix));

                    lock (Sync)
                    {
                        PatchedGarageMethods.Add(begin);
                        _garageSongHookInstalled = true;
                    }

                    patchedNow++;

                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MUSIC LAB GARAGE SONG HOOK READY: componentType='{type.FullName}' method='{begin.Name}' sourcePath='{path}'. Prefix only records identity when the instance path is under GameRoom_27/Play*.");
                }
                catch (Exception ex)
                {
                    Plugin.LoggerInstance?.LogWarning(
                        $"[SCRC-AP] MUSIC LAB GARAGE song-hook patch failed type='{type.FullName}': {ex.GetBaseException().Message}");
                }
            }
        }

        if (!GarageSongHookInstalled && exactObjectsFound > 0)
        {
            bool shouldLog;
            lock (Sync)
            {
                shouldLog = !_garageHookMissingNoticeLogged;
                _garageHookMissingNoticeLogged = true;
            }

            if (shouldLog)
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] MUSIC LAB GARAGE Begin-hook unavailable on managed Component wrappers; found {exactObjectsFound} exact SetScoredSong objects. the current build uses cartridge-holder pickup/insertion state as the primary identity path; request/result-time song members remain fallbacks only.");
            }
        }
        else if (patchedNow > 0)
        {
            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] MUSIC LAB GARAGE installed {patchedNow} new narrow song-selection hook(s).");
        }
    }

    private static void GarageSetScoredSongPrefix(object? __instance)
    {
        if (__instance is not Component component || component.transform == null)
            return;

        string path = BuildHierarchy(component.transform);
        if (!path.Contains("Root/GameRoom_27_Logic/", StringComparison.OrdinalIgnoreCase))
            return;

        string? song = ExtractGarageSongFromPath(path);
        if (string.IsNullOrWhiteSpace(song))
            return;

        lock (Sync)
            _currentGarageSong = song;

        Record("GARAGE SONG SELECTED", $"song='{song}' path='{path}' component='{component.GetType().FullName}'.");
    }

    private static string? ExtractGarageSongFromPath(string path)
    {
        foreach (string sequenceName in GarageSequenceNames)
        {
            if (!path.Contains("/" + sequenceName + "/", StringComparison.OrdinalIgnoreCase))
                continue;

            return sequenceName switch
            {
                "PlayBloodyTears" => "Bloody Tears",
                "PlayGradiusRemix" => "Gradius Remix",
                "PlaySmooch" => "Smooch",
                "PlaySuperstar" => "Superstar",
                "PlayVampireKiller" => "Vampire Killer",
                "PlayWagTheDog" => "Wag the Dog",
                _ => sequenceName,
            };
        }

        return null;
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

    [ThreadStatic]
    private static bool _applyingFromArchipelago;

    public static bool RandomizeEarlyProgression { get; set; }

    private static readonly IReadOnlyDictionary<string, string[]> ItemToFlags =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Level 2 Access"] = new[]
            {
                "ROOTS_HUB_GATE_OPENED",
                "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE",
            },
            ["Level 3 Access"] = new[]
            {
                "ROOTS_HUB_LEVEL_07_DOOR_REVEALED",
            },
            ["Level 4 Access"] = new[]
            {
                "AP_LEVEL_4_ACCESS",
            },
            ["Level 5 Access"] = new[]
            {
                "AP_LEVEL_5_ACCESS",
            },
            ["Level 6 Access"] = new[]
            {
                "AP_LEVEL_6_ACCESS",
            },
            ["Level 7 Access"] = new[]
            {
                "AP_LEVEL_7_ACCESS",
            },
            ["Level 8 Access"] = new[]
            {
                "AP_LEVEL_8_ACCESS",
            },
            ["Level 9 Access"] = new[]
            {
                "AP_LEVEL_9_ACCESS",
            },
            ["Level 10 Access"] = new[]
            {
                "AP_LEVEL_10_ACCESS",
            },
            ["Level 11 Access"] = new[]
            {
                "AP_LEVEL_11_ACCESS",
            },
            ["Level 12 Access"] = new[]
            {
                "AP_LEVEL_12_ACCESS",
            },
            ["Level 13 Access"] = new[]
            {
                "AP_LEVEL_13_ACCESS",
            },
            ["Level 14 Access"] = new[]
            {
                "AP_LEVEL_14_ACCESS",
            },
            ["Level 15 Access"] = new[]
            {
                "AP_LEVEL_15_ACCESS",
            },
            ["Level 16 Access"] = new[]
            {
                "AP_LEVEL_16_ACCESS",
            },
            ["Level 17 Access"] = new[]
            {
                "AP_LEVEL_17_ACCESS",
            },
            ["Level 18 Access"] = new[]
            {
                "AP_LEVEL_18_ACCESS",
            },
            ["Level 19 Access"] = new[]
            {
                "AP_LEVEL_19_ACCESS",
            },
            ["Level 20 Access"] = new[]
            {
                "AP_LEVEL_20_ACCESS",
            },
            ["Level 21 Access"] = new[]
            {
                "AP_LEVEL_21_ACCESS",
            },
            ["Level 22 Access"] = new[]
            {
                "AP_LEVEL_22_ACCESS",
            },
        };

    private static readonly HashSet<string> RandomizedVanillaFlags =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "ROOTS_HUB_GATE_OPENED",
            "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE",
            "ROOTS_HUB_LEVEL_07_DOOR_REVEALED",
        };

    // Flags AP has explicitly granted permission for.
    private static readonly HashSet<string> GrantedFlags =
        new(StringComparer.OrdinalIgnoreCase);

    // If vanilla tried to set a randomized flag before AP granted it, retain the
    // game's own request object so we can replay that exact native request later.
    private static readonly Dictionary<string, object> BlockedRequests =
        new(StringComparer.OrdinalIgnoreCase);

    // Tracks that vanilla actually reached a randomized progression point in
    // this game process. Unlike BlockedRequests, this is intentionally retained
    // after replay so physical keepers can decide whether prerequisites were
    // naturally completed before an AP permission arrived.
    private static readonly HashSet<string> ObservedVanillaRequests =
        new(StringComparer.OrdinalIgnoreCase);

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

    public static bool ShouldBlockVanillaRequest(
        string flagName,
        object request,
        object? processor)
    {
        CapturePlayerSaveRequestProcessor(processor);

        if (!RandomizeEarlyProgression ||
            _applyingFromArchipelago ||
            !RandomizedVanillaFlags.Contains(flagName))
        {
            return false;
        }

        lock (ProcessorLock)
        {
            ObservedVanillaRequests.Add(flagName);

            if (GrantedFlags.Contains(flagName))
            {
                Plugin.LoggerInstance?.LogInfo(
                    $"[SCRC-AP] ALLOWING NATIVE PROGRESSION FLAG {flagName}; Archipelago already granted access.");
                return false;
            }

            BlockedRequests[flagName] = request;
        }

        Plugin.LoggerInstance?.LogWarning(
            $"[SCRC-AP] BLOCKED VANILLA PROGRESSION FLAG {flagName} until Archipelago grants it.");

        if (flagName == "ROOTS_HUB_GATE_OPENED" ||
            flagName == "ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE" ||
            flagName == "ROOTS_HUB_LEVEL_07_DOOR_REVEALED")
        {
            try
            {
                string stack = Environment.StackTrace
                    .Replace("\r", "")
                    .Replace("\n", " <= ");

                Plugin.LoggerInstance?.LogWarning(
                    $"[SCRC-AP] BLOCK SOURCE STACK {flagName}: {stack}");
            }
            catch { }
        }

        return true;
    }

    public static bool HasGrantedFlag(string flagName)
    {
        lock (ProcessorLock)
            return GrantedFlags.Contains(flagName);
    }

    public static bool HasObservedVanillaRequest(string flagName)
    {
        lock (ProcessorLock)
            return ObservedVanillaRequests.Contains(flagName);
    }

    public static bool HasLevel2Access =>
        HasGrantedFlag("ROOTS_HUB_GATE_OPENED") &&
        HasGrantedFlag("ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE");

    public static bool HasLevel3Access =>
        HasGrantedFlag("ROOTS_HUB_LEVEL_07_DOOR_REVEALED");

    public static bool HasLevel4Access =>
        HasGrantedFlag("AP_LEVEL_4_ACCESS");

    public static bool HasLevel5Access =>
        HasGrantedFlag("AP_LEVEL_5_ACCESS");

    public static bool HasLevel6Access =>
        HasGrantedFlag("AP_LEVEL_6_ACCESS");

    public static bool HasLevel7Access =>
        HasGrantedFlag("AP_LEVEL_7_ACCESS");

    public static bool HasLevel8Access =>
        HasGrantedFlag("AP_LEVEL_8_ACCESS");

    public static bool HasLevel9Access =>
        HasGrantedFlag("AP_LEVEL_9_ACCESS");

    public static bool HasLevel10Access =>
        HasGrantedFlag("AP_LEVEL_10_ACCESS");

    public static bool HasLevel11Access =>
        HasGrantedFlag("AP_LEVEL_11_ACCESS");

    public static bool HasLevel12Access =>
        HasGrantedFlag("AP_LEVEL_12_ACCESS");

    public static bool HasLevel13Access =>
        HasGrantedFlag("AP_LEVEL_13_ACCESS");

    public static bool HasLevel14Access =>
        HasGrantedFlag("AP_LEVEL_14_ACCESS");

    public static bool HasLevel15Access =>
        HasGrantedFlag("AP_LEVEL_15_ACCESS");

    public static bool HasLevel16Access =>
        HasGrantedFlag("AP_LEVEL_16_ACCESS");

    public static bool HasLevel17Access =>
        HasGrantedFlag("AP_LEVEL_17_ACCESS");

    public static bool HasLevel18Access =>
        HasGrantedFlag("AP_LEVEL_18_ACCESS");

    public static bool HasLevel19Access =>
        HasGrantedFlag("AP_LEVEL_19_ACCESS");

    public static bool HasLevel20Access =>
        HasGrantedFlag("AP_LEVEL_20_ACCESS");

    public static bool HasLevel21Access =>
        HasGrantedFlag("AP_LEVEL_21_ACCESS");

    public static bool HasLevel22Access =>
        HasGrantedFlag("AP_LEVEL_22_ACCESS");

    public static void DeveloperResetLevel2Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("ROOTS_HUB_GATE_OPENED");
            GrantedFlags.Remove("ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE");

            BlockedRequests.Remove("ROOTS_HUB_GATE_OPENED");
            BlockedRequests.Remove("ROOTS_HUB_DIFFICULTY_ASSIGNMENT_COMPLETE");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: Level 2 Access permission and cached early progression requests cleared.");
    }

    public static void DeveloperResetLevel3Access()
    {
        const string flagName = "ROOTS_HUB_LEVEL_07_DOOR_REVEALED";

        lock (ProcessorLock)
        {
            GrantedFlags.Remove(flagName);
            BlockedRequests.Remove(flagName);
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 3 Access permission and cached Level 3 door request cleared. Vanilla save data was left unchanged.");
    }

    public static void DeveloperResetLevel4Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_4_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 4 Access permission cleared.");
    }

    public static void DeveloperResetLevel5Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_5_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 5 Access permission cleared. Vanilla Hip Glasses / Chicken Bucket / lift-chat progression was left unchanged.");
    }

    public static void DeveloperResetLevel6Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_6_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 6 Access permission cleared. Manual button, Room02 story/blocker state, and Devil Mode progression were left unchanged.");
    }

    public static void DeveloperResetLevel7Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_7_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 7 Access permission cleared. Demolition Certificate, Training Centre Level21, King/hand progression, and Minim Tower were left unchanged.");
    }

    public static void DeveloperResetLevel8Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_8_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 8 Access permission cleared. Minim Tower completion state and Level11Door_DevilMode were left unchanged.");
    }

    public static void DeveloperResetLevel9Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_9_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 9 Access permission cleared. Minim Tower completion, Level20 locked-cover state, and hand progression were left unchanged.");
    }

    public static void DeveloperResetLevel10Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_10_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 10 Access permission cleared. Vault completion and hub hand progression were left unchanged.");
    }

    public static void DeveloperResetLevel11Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_11_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 11 Access permission cleared. Wooden Spoon, Saw Disc, Act 1 music/trader progression, MEAT_HUB_ACT_ONE_MUSIC_DONE, MEAT_HUB_GATE_OPENED, and Bee Mode were left unchanged.");
    }

    public static void DeveloperResetLevel12Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_12_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 12 Access permission cleared. Smooch Cartridge, Pied Piper/Hypno Pan ability, Act 2 music, cat escort, bouncer progression, and other Hub4 doors were left unchanged.");
    }

    public static void DeveloperResetLevel13Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_13_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 13 Access permission cleared. Act 3 music, Scruffy escort, bouncer progression, Hypno Pan/crowd behavior, and PlaceholderLevelDoor_03 were left unchanged.");
    }

    public static void DeveloperResetLevel14Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_14_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 14 Access permission cleared. Act 4 music, mouse revolution, Hypno Pan mouse escort, bouncer progression, chilli crowd, and PlaceholderLevelDoor_04 were left unchanged.");
    }

    public static void DeveloperResetLevel15Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_15_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 15 Access permission cleared. Cell Tower intro, Central Mainframe vanilla conditions, ExternalDoor/HR5B progression, Level16AttemptedCondition, EntranceDoor, and FutureLevelEntranceDoor were left unchanged.");
    }

    public static void DeveloperResetLevel16Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_16_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 16 Access permission cleared. Bizzle, Clive, Bee Modes, both Super Nectar drops, Prince notes/conditions, and all other Hub5B progression were left unchanged.");
    }

    public static void DeveloperResetLevel17Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_17_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 17 Access permission cleared. Cold Storage's vanilla heist unlock, Violance/VIOLIN_ABILITY, Hypno Pan requirement, NPCs, Star Eater, and all story progression were left unchanged.");
    }

    public static void DeveloperResetLevel18Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_18_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 18 Access permission cleared. The Darkness shield, Weed Killer opening, Minim's Eye deposit, totem completion, and all Tower of Fear story progression were left unchanged.");
    }

    public static void DeveloperResetLevel19Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_19_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 19 Access permission cleared. Escape Devil Mode, Violance opening, Minim's Mind/Brain deposit, totem completion, and all Tower of Fear story progression were left unchanged.");
    }

    public static void DeveloperResetLevel20Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_20_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 20 Access permission cleared. Hypno Pan route/totem opening, Minim's Heart deposit, totem completion, and all Tower of Fear story progression were left unchanged.");
    }

    public static void DeveloperResetLevel21Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_21_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 21 Access permission cleared. Royal Corridor intro/progression and Locker Room Devil Mode were left unchanged.");
    }

    public static void DeveloperResetLevel22Access()
    {
        lock (ProcessorLock)
        {
            GrantedFlags.Remove("AP_LEVEL_22_ACCESS");
        }

        Plugin.LoggerInstance?.LogWarning(
            "[SCRC-AP] DEV RESET: local Level 22 Access permission cleared. Star Eater bridge, fridge/ice sequence, king cutscene, and all Royal Corridor progression were left unchanged.");
    }

    public static void ApplyArchipelagoItem(string itemName)
    {
        if (AreaAccessPrototype.TryApplyItem(itemName))
            return;

        if (CassetteReceiptRandomization.TryApplyItem(itemName))
            return;

        if (!ItemToFlags.TryGetValue(itemName, out string[]? flags))
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] No native progression mapping yet for received item '{itemName}'.");
            return;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] Archipelago granted native progression package for '{itemName}'.");

        foreach (string flagName in flags)
            GrantFlag(flagName);

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] FINISHED GRANTING ITEM '{itemName}'.");

        if (string.Equals(itemName, "Level 2 Access", StringComparison.OrdinalIgnoreCase))
        {
            EarlyRuntimeUnlockPatches.ReleaseLevel2Access();
            Hub02MegafierDoorViewPatches.ReleaseLevel2Access();
            EarlySequenceBlockerPatches.ReleaseLevel2Access();
        }

        if (string.Equals(itemName, "Level 3 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 3 Access received: Level07 door permission released. Vanilla weed-killer prerequisites are otherwise left intact.");
        }

        if (string.Equals(itemName, "Level 4 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 4 Access received: Level08/vine hard-block permission released. Plant Pipes remain a vanilla Level 3 reward.");
        }

        if (string.Equals(itemName, "Level 5 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 5 Access received: final Lift Quest / Level_09 entrance permission released. Hip Glasses, Chicken Bucket, bucket-minion trade/blockade, and King lift chat remain vanilla prerequisites.");
        }

        if (string.Equals(itemName, "Level 6 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 6 Access received: normal Boring Room Level02Door permission released. Manual button traversal, missing-button/phone-booth story, Room02 blocker state, and Devil Mode remain vanilla.");
        }

        if (string.Equals(itemName, "Level 7 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 7 Access received: Demolition Training / internal Level_19 entrance permission released. Demolition Certificate, Level21, training story, Minim Tower, and hand barriers remain vanilla.");
        }

        if (string.Equals(itemName, "Level 8 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 8 Access received: normal Minim Tower / internal Level_11 entrance permission released. Level11Door_DevilMode remains vanilla and untouched.");
        }

        if (string.Equals(itemName, "Level 9 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 9 Access received: normal School Trip / internal Level_20 entrance permission released. Vanilla Minim Tower prerequisite and Level20 locked-cover logic remain authoritative.");
        }

        if (string.Equals(itemName, "Level 10 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 10 Access received: normal The Vault / internal Level_01 entrance permission released.");
        }

        if (string.Equals(itemName, "Level 11 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 11 Access received: normal Act 1: Flavor / internal Level_12 entrance permission released. Vanilla Act 1 music progression and Bee Mode remain untouched.");
        }

        if (string.Equals(itemName, "Level 12 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 12 Access received: normal Act 2: Sauce and Spice entrance permission released. Vanilla Hypno Pan, Act 2 music, cat/bouncer requirements, and other Hub4 doors remain untouched.");
        }

        if (string.Equals(itemName, "Level 13 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 13 Access received: normal Act 3: Montage entrance permission released. Vanilla Act 3 music, Scruffy/bouncer requirements, crowd/Hypno Pan behavior, and PlaceholderLevelDoor_03 remain untouched.");
        }

        if (string.Equals(itemName, "Level 14 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 14 Access received: normal Act 4: Habanero entrance permission released. Vanilla Act 4 music, mouse-revolution/bouncer requirements, Hypno Pan mouse escort, chilli crowd, and PlaceholderLevelDoor_04 remain untouched.");
        }

        if (string.Equals(itemName, "Level 15 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 15 Access received: Central Mainframe Level16Door entrance permission released. ExternalDoor/HR5B forward progression and the rest of Cell Tower remain vanilla.");
        }

        if (string.Equals(itemName, "Level 16 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 16 Access received: Thief Prince LevelEntranceDoor entrance permission released. Bizzle, Clive, Bee Modes, Super Nectar, and Prince/Cell Tower progression remain vanilla.");
        }

        if (string.Equals(itemName, "Level 17 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 17 Access received: Cold Storage Level21Door entrance permission released. Violance/VIOLIN_ABILITY, Hypno Pan, Old Tom/Thief Prince, Star Eater, and heist progression remain vanilla.");
        }

        if (string.Equals(itemName, "Level 18 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 18 Access received: The Darkness normal entrance permission released. Weed Killer, Minim's Eye, Darkness totem, and Tower story progression remain vanilla.");
        }

        if (string.Equals(itemName, "Level 19 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 19 Access received: Escape normal entrance permission released. Escape Devil Mode is untouched; Violance, Minim's Mind/Brain, Complexity totem, and Tower story progression remain vanilla.");
        }

        if (string.Equals(itemName, "Level 20 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 20 Access received: Loneliness normal entrance permission released. Hypno Pan, Minim's Heart, Loneliness totem, and Tower story progression remain vanilla.");
        }

        if (string.Equals(itemName, "Level 21 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 21 Access received: Locker Room normal LevelEntranceDoor_14 permission released. Royal Corridor intro and LevelEntranceDoor_14_DevilMode remain vanilla.");
        }

        if (string.Equals(itemName, "Level 22 Access", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.LoggerInstance?.LogInfo(
                "[SCRC-AP] Level 22 Access received: King Ferdinand I LevelEntranceDoor_28_default permission released. Star Eater bridge, fridge/ice sequence, king cutscene, and Royal Corridor progression remain vanilla.");
        }
    }

    private static void GrantFlag(string flagName)
    {
        object? blockedRequest = null;
        object? processor = null;

        lock (ProcessorLock)
        {
            GrantedFlags.Add(flagName);

            if (BlockedRequests.TryGetValue(flagName, out object? req))
            {
                blockedRequest = req;
                BlockedRequests.Remove(flagName);
            }

            processor = _playerSaveRequestProcessor;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] AP GRANTED FLAG {flagName}.");

        if (blockedRequest == null)
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] No blocked native request for {flagName}; the game's next natural request will be allowed.");
            return;
        }

        if (processor == null)
        {
            // Keep it for later in the unlikely event the processor disappeared.
            lock (ProcessorLock)
                BlockedRequests[flagName] = blockedRequest;

            Plugin.LoggerInstance?.LogWarning(
                $"[SCRC-AP] Cannot replay {flagName} yet because PlayerSaveRequestProcessor is unavailable.");
            return;
        }

        ReplayNativeRequest(flagName, blockedRequest, processor);
    }

    private static void ReplayNativeRequest(
        string flagName,
        object request,
        object processor)
    {
        MethodInfo? method = processor.GetType().GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .FirstOrDefault(m =>
            {
                if (m.Name != "ProcessRequest") return false;

                ParameterInfo[] ps;
                try { ps = m.GetParameters(); }
                catch { return false; }

                return ps.Length == 1 &&
                       ps[0].ParameterType.IsInstanceOfType(request);
            });

        if (method == null)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Could not find native ProcessRequest method to replay {flagName}.");
            return;
        }

        try
        {
            _applyingFromArchipelago = true;

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] REPLAYING BLOCKED NATIVE FLAG {flagName} after Archipelago grant.");

            method.Invoke(processor, new[] { request });

            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] REPLAYED NATIVE FLAG {flagName} successfully.");
        }
        catch (Exception ex)
        {
            Plugin.LoggerInstance?.LogError(
                $"[SCRC-AP] Failed replaying native flag {flagName}: {ex.GetBaseException()}");
        }
        finally
        {
            _applyingFromArchipelago = false;
        }
    }
}

internal static class ProgressionPatches
{
    public static bool ProgressionRequestPrefix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);
        RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.TryFlushPendingNativeGrant();
        PlantPipesRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PlantPipesRandomization.TryFlushPendingNativeGrant();
        PreviewAbilityRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.OnLifecyclePoint("progression request prefix");
        RootsBucketRandomization.CapturePlayerSaveRequestProcessor(__instance);
        RootsBucketRandomization.TryFlushPendingNativeGrants();

        object? req = ReflectionUtil.FindArg(__args, "RecordGameProgressionInSaveDataRequest");
        if (req == null) return true;

        string flag = ReflectionUtil.ReadMember(req, "Flag")?.ToString()
                      ?? ReflectionUtil.ReadMember(req, "_Flag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        if (GarageCartridgeAccess.ShouldSuppressVanillaSourceGrant(req, flag))
            return false;

        if (WeedKillerRandomization.ShouldSuppressGeckoVanillaGrant(req, flag))
            return false;

        if (PlantPipesRandomization.ShouldSuppressFrogHippoVanillaGrant(req, flag))
            return false;

        if (RootsBucketRandomization.ShouldSuppressVanillaGrant(req, flag))
            return false;

        if (NativeProgression.ShouldBlockVanillaRequest(flag, req, __instance))
            return false;

        return true;
    }

    private static readonly string[] GateKeywords =
    {
        "GATE", "DOOR", "HUB", "ROOT", "LOBBY", "LIFT", "ELEVATOR",
        "OPEN", "UNLOCK", "INTRO", "BRIDGE", "ACCESS", "CHICKEN", "BUCKET",
        "COMBO", "BLOCKADE", "BUTTON", "HAND", "PHONE", "PLUNGER", "BORING",
        "VAULT", "MINIM", "TOWER", "CLEAN_HUB", "ROOM_02",
        "LEVEL_04", "LEVEL_05", "LEVEL_06", "LEVEL_07", "LEVEL_08", "LEVEL_09",
        "LEVEL_10", "LEVEL_11", "LEVEL_12", "LEVEL_13", "LEVEL_14"
    };

    public static void ProgressionRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);
        RootsIntroCutsceneBypass.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.CapturePlayerSaveRequestProcessor(__instance);
        WeedKillerRandomization.TryFlushPendingNativeGrant();
        PlantPipesRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PlantPipesRandomization.TryFlushPendingNativeGrant();
        CassetteReceiptRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.CapturePlayerSaveRequestProcessor(__instance);
        PreviewAbilityRandomization.OnLifecyclePoint("progression request postfix");
        RootsBucketRandomization.CapturePlayerSaveRequestProcessor(__instance);
        RootsBucketRandomization.TryFlushPendingNativeGrants();

        object? req = ReflectionUtil.FindArg(__args, "RecordGameProgressionInSaveDataRequest");
        if (req == null) return;

        string flag = ReflectionUtil.ReadMember(req, "Flag")?.ToString()
                      ?? ReflectionUtil.ReadMember(req, "_Flag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        Level5Discovery.RecordProgressionRequest(flag);
        Level6Discovery.RecordProgressionRequest(flag);
        Level8Discovery.RecordProgressionRequest(flag);
        MusicLabDiscovery.RecordProgressionRequest(req, flag);
        GarageCartridgeAccess.RecordVanillaSourceCollected(req, flag);
        WeedKillerRandomization.RecordGeckoSourceCollected(req, flag);
        PlantPipesRandomization.RecordFrogHippoSourceCollected(req, flag);
        RootsBucketRandomization.RecordSourceCollected(req, flag);

        bool interesting = GateKeywords.Any(k =>
            flag.Contains(k, StringComparison.OrdinalIgnoreCase));

        Plugin.LoggerInstance?.LogInfo(
            interesting
                ? $"[SCRC-AP] ===== ROUTE/GATE PROGRESSION REQUEST ====="
                : $"[SCRC-AP] ===== PROGRESSION REQUEST =====");

        DumpAllSimpleMembers(req, "ProgressionRequest");
    }

    public static void ProgressionFlagEventPostfix(object[]? __args)
    {
        object? evt = ReflectionUtil.FindArg(__args, "GameProgressionFlagUpdatedEvent");
        if (evt == null) return;

        string flag = ReflectionUtil.ReadMember(evt, "ProgressionFlag")?.ToString()
                      ?? ReflectionUtil.ReadMember(evt, "_ProgressionFlag_k__BackingField")?.ToString()
                      ?? "<unknown>";

        Level5Discovery.RecordProgressionFlagUpdated(flag);
        Level6Discovery.RecordProgressionFlagUpdated(flag);
        Level8Discovery.RecordProgressionFlagUpdated(flag);
        MusicLabDiscovery.RecordProgressionFlagUpdated(evt, flag);

        bool interesting = GateKeywords.Any(k =>
            flag.Contains(k, StringComparison.OrdinalIgnoreCase));

        Plugin.LoggerInstance?.LogInfo(
            interesting
                ? $"[SCRC-AP] ===== ROUTE/GATE FLAG UPDATED ====="
                : $"[SCRC-AP] ===== PROGRESSION FLAG UPDATED =====");

        DumpAllSimpleMembers(evt, "ProgressionFlagEvent");
    }

    public static void BagItemRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "ObtainBagItemRequest");
        if (req == null) return;

        Level5Discovery.RecordBagItem(req);
        Level6Discovery.RecordBagItem(req);
        Level8Discovery.RecordBagItem(req);
        MusicLabDiscovery.RecordBagItem(req);

        Plugin.LoggerInstance?.LogInfo("[SCRC-AP] ===== BAG ITEM OBTAINED =====");
        DumpAllSimpleMembers(req, "BagItemRequest");
    }

    public static void AbilityItemRequestPostfix(object? __instance, object[]? __args)
    {
        NativeProgression.CapturePlayerSaveRequestProcessor(__instance);

        object? req = ReflectionUtil.FindArg(__args, "EarnAbilityItemRequest");
        if (req == null) return;

        Level5Discovery.RecordAbilityItem(req);
        Level6Discovery.RecordAbilityItem(req);
        Level8Discovery.RecordAbilityItem(req);
        MusicLabDiscovery.RecordAbilityItem(req);

        Plugin.LoggerInstance?.LogInfo("[SCRC-AP] ===== ABILITY ITEM EARNED =====");
        DumpAllSimpleMembers(req, "AbilityItemRequest");
    }

    private static void DumpAllSimpleMembers(object obj, string prefix)
    {
        var seen = new HashSet<string>();
        Type t = obj.GetType();

        foreach (PropertyInfo prop in t.GetProperties(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (prop.GetIndexParameters().Length != 0) continue;
            if (!seen.Add(prop.Name)) continue;

            object? value;
            try { value = prop.GetValue(obj); }
            catch { continue; }

            LogMember(prefix, prop.Name, value);
        }

        foreach (FieldInfo field in t.GetFields(
                     BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!seen.Add(field.Name)) continue;

            object? value;
            try { value = field.GetValue(obj); }
            catch { continue; }

            LogMember(prefix, field.Name, value);
        }
    }

    private static void LogMember(string prefix, string name, object? value)
    {
        if (value == null)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] {prefix}.{name}=<null>");
            return;
        }

        object? unwrapped = ReflectionUtil.UnwrapNullable(value);
        if (unwrapped == null)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] {prefix}.{name}=<nullable-null>");
            return;
        }

        Type vt = unwrapped.GetType();

        if (vt.IsPrimitive || vt.IsEnum || unwrapped is string || unwrapped is decimal)
        {
            Plugin.LoggerInstance?.LogInfo($"[SCRC-AP] {prefix}.{name}={unwrapped}");
            return;
        }

        string? identifier = ReflectionUtil.ExtractIdentifier(unwrapped);
        if (!string.IsNullOrWhiteSpace(identifier))
        {
            Plugin.LoggerInstance?.LogInfo(
                $"[SCRC-AP] {prefix}.{name}=<{vt.Name}> {identifier}");
            return;
        }

        Plugin.LoggerInstance?.LogInfo(
            $"[SCRC-AP] {prefix}.{name}=<{vt.FullName}> {unwrapped}");
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

internal static class ReflectionUtil
{
    public static Assembly? GameAssembly => AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => string.Equals(
            a.GetName().Name, "Assembly-CSharp", StringComparison.OrdinalIgnoreCase));

    public static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
        catch { return Array.Empty<Type>(); }
    }

    public static object? FindArg(object[]? args, string typeName) =>
        args?.FirstOrDefault(a => a != null && a.GetType().Name == typeName);

    public static object? ReadMember(object? obj, string name)
    {
        if (obj == null) return null;
        Type t = obj.GetType();

        try
        {
            PropertyInfo? p = t.GetProperty(
                name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (p != null && p.GetIndexParameters().Length == 0)
                return p.GetValue(obj);
        }
        catch { }

        try
        {
            FieldInfo? f =
                t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                ?? t.GetField(
                    $"<{name}>k__BackingField",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (f != null) return f.GetValue(obj);
        }
        catch { }

        return null;
    }

    public static bool? ReadBool(object obj, string name)
    {
        object? v = ReadMember(obj, name);
        if (v is bool b) return b;
        return bool.TryParse(v?.ToString(), out bool parsed) ? parsed : null;
    }

    public static int? ReadInt(object obj, string name)
    {
        object? v = ReadMember(obj, name);
        if (v is int i) return i;
        return int.TryParse(v?.ToString(), out int parsed) ? parsed : null;
    }

    public static object? UnwrapNullable(object? value)
    {
        if (value == null) return null;
        Type t = value.GetType();
        string name = t.FullName ?? t.Name;

        if (!name.Contains("Nullable`1", StringComparison.Ordinal))
            return value;

        object? hasValue = ReadMember(value, "HasValue");
        if (hasValue is bool b && !b) return null;

        object? inner = ReadMember(value, "Value");
        return inner ?? value;
    }

    public static string? ExtractIdentifier(object? value)
    {
        value = UnwrapNullable(value);
        if (value == null) return null;
        if (value is string s) return s;

        foreach (string n in new[] { "id", "ID", "Id", "value", "Value" })
        {
            object? member = ReadMember(value, n);
            if (member is string ms && !string.IsNullOrWhiteSpace(ms))
                return ms;
        }

        string text = value.ToString() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(text)
            && text != value.GetType().Name
            && text != value.GetType().FullName)
            return text;

        return null;
    }
}
