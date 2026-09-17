namespace RhythmCastleAP;

internal sealed class QuestActionDiagnosticPolicy
{
    // Discovery hints only: matching a hint never establishes an action identity.
    // Bounds last for the process lifetime; repeated F5/reconnect/room entry cannot reset them.
    private sealed record Candidate(string Key, string[] Rooms, string[] Tokens, string? Snapshot = null, string[]? ExtraSnapshots = null);
    private static readonly string[] Lobby = { "GameRoom_Hub1A", "GameRoom_Hub1B" };
    private static readonly string[] Meat = { "GameRoom_Hub4" };
    private static readonly string[] Tower = { "GameRoom_Hub3" };
    private static readonly string[] CharacterItemRooms = { "GameRoom_Hub6", "GameRoom_27" };
    private static readonly Candidate[] Candidates =
    {
        // Exact enum names verified from installed Assembly-CSharp metadata.
        // Collection is not assumed to mean insertion; snapshots establish that separately.
        new("music_lab_old_game_data", CharacterItemRooms,
            new[] { "CLEAN_HUB_MEMORY_CARD_TAKEN", "LEVEL_27_MEMORY_CARD_SCGMD", "MemoryCard" },
            "CLEAN_HUB_MEMORY_CARD_TAKEN", new[] { "LEVEL_27_MEMORY_CARD_SCGMD_BAG_ITEM", "LEVEL_27_MEMORY_CARD_SCGMD_COLLECTED" }),
        new("music_lab_car_battery", new[] { "GameRoom_Hub6", "GameRoom_27", "GameRoom_Hub1A", "GameRoom_Hub1B" },
            new[] { "CLEAN_HUB_GHOST_CAT_BATTERY", "PLAYABLE_CHARACTER_UNLOCKED_MEOO", "GhostCat", "CatBattery" },
            "CLEAN_HUB_GHOST_CAT_BATTERY_REWARDED", new[] { "CLEAN_HUB_GHOST_CAT_BATTERY_BAG_ITEM", "PLAYABLE_CHARACTER_UNLOCKED_MEOO" }),
        new("roots_combo_bucket_conversion", new[] { "GameRoom_Hub2", "GameRoom_09" }, new[] { "LEVEL_09_COMBO", "CHICKEN_BUCKET", "COMBO_BUCKET" }, "LEVEL_09_COMBO_ABILITY_EARNED"),
        new("lobby_important_letters_delivery", Lobby, new[] { "LETTER", "MAIL", "BEAN_TRUMPET" }),
        new("lobby_plunger_hand_in", Lobby, new[] { "PLUNGER", "PLUNGERED" }, "LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED"),
        new("lobby_star_eater_feed", Lobby, new[] { "LOBBY_HUB_STAR_EATER", "StarEater" }, "LOBBY_HUB_STAR_EATER_FED"),
        new("lobby_fish_tears_delivery", Lobby, new[] { "FISH_TEARS", "FishTears" }, "LOBBY_HUB_FISH_TEARS_DEPOSITED"),
        new("meat_hypno_pan_creation", Meat, new[] { "PIED_PIPER_ABILITY", "FRYING_PAN", "WOODEN_SPOON", "Hypno" }),
        new("meat_act_1_music_delivery", Meat, new[] { "ACT_ONE_MUSIC" }, "MEAT_HUB_ACT_ONE_MUSIC_DONE"),
        new("meat_act_3_music_delivery", Meat, new[] { "ACT_THREE_MUSIC" }, "MEAT_HUB_ACT_THREE_MUSIC_DONE"),
        new("meat_act_4_music_delivery", Meat, new[] { "ACT_FOUR_MUSIC" }, "MEAT_HUB_ACT_FOUR_MUSIC_DONE"),
        new("meat_cat_return", Meat, new[] { "ACT_TWO_BOUNCER", "ACT_TWO_MUSIC", "Cat" }),
        new("meat_scruffy_return", Meat, new[] { "ACT_THREE_BOUNCER", "Scruffy" }, "MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE"),
        new("meat_mouse_revolution", Meat, new[] { "MOUSE_REVOLUTION", "ACT_FOUR_BOUNCER", "Mouse" }, "MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED"),
        new("cell_super_nectar_delivery", new[] { "GameRoom_Hub5A", "GameRoom_Hub5B", "GameRoom_Hub5C" }, new[] { "SUPER_NECTAR", "BEES_ESCAPED", "Nectar" }, "PRISON_HUB_BEES_ESCAPED"),
        new("tower_minim_eye_restoration", Tower, new[] { "MADNESS_HUB_EYE", "DARKNESS_AREA", "Darkness", "Eye" }, "MADNESS_HUB_DARKNESS_AREA_COMPLETED"),
        new("tower_minim_mind_restoration", Tower, new[] { "MADNESS_HUB_BRAIN", "COMPLEXITY_AREA", "Complexity", "Brain", "Mind" }, "MADNESS_HUB_COMPLEXITY_AREA_COMPLETED"),
        new("tower_minim_heart_restoration", Tower, new[] { "MADNESS_HUB_HEART", "LONELINESS_AREA", "Loneliness", "Heart" }, "MADNESS_HUB_LONELINESS_AREA_COMPLETED"),
        new("tower_totem_completion", new[] { "GameRoom_Hub3", "GameRoom_Hub5C" }, new[] { "Totem", "OVERALL_PROGRESS_HELPED_SCARED_MINION", "SCARED_MINIM_WRAP_UP" }, "OVERALL_PROGRESS_HELPED_SCARED_MINION"),
        new("royal_star_eater_feed", new[] { "GameRoom_Hub7" }, new[] { "KING_CORRIDOR_STAR_EATER", "StarEater" }, "KING_CORRIDOR_STAR_EATER_FED"),
        new("roots_star_eater_feed", new[] { "GameRoom_Hub2" }, new[] { "ROOTS_HUB_STAR_EATER_FED" }, "ROOTS_HUB_STAR_EATER_FED"),
    };
    private static readonly HashSet<string> Channels = new(StringComparer.Ordinal)
        { "event", "request", "snapshot", "scene", "result-applied", "result-persisted" };
    private static readonly Dictionary<string, string> ResultRooms = new(StringComparer.Ordinal)
    {
        ["GameRoom_09"] = "Level_09", ["GameRoom_02"] = "Level_02",
        ["GameRoom_12"] = "Level_12", ["GameRoom_15"] = "Level_15",
        ["GameRoom_22"] = "Level_22", ["GameRoom_23"] = "Level_23",
        ["GameRoom_16"] = "Level_16", ["GameRoom_03"] = "Level_03",
        ["GameRoom_13"] = "Level_13", ["GameRoom_25"] = "Level_25",
        ["GameRoom_14"] = "Level_14", ["GameRoom_28"] = "Level_28",
    };
    private readonly object _sync = new();
    private readonly Dictionary<string, HashSet<string>> _seen = new(StringComparer.Ordinal);
    private readonly HashSet<string> _snapshots = new(StringComparer.Ordinal);

    internal string? Observe(string channel, string room, string token, bool? before = null, bool? after = null)
    {
        if (!Channels.Contains(channel) || channel == "scene" || !Safe(room, 64) || !Safe(token, 256)) return null;
        string[] candidates;
        if (channel == "result-applied" || channel == "result-persisted")
        {
            if (!ResultRooms.TryGetValue(room, out string? level) ||
                !token.StartsWith(level + "|LevelVariant_", StringComparison.Ordinal)) return null;
            candidates = new[] { "prerequisite-result-context" };
        }
        else
        {
            candidates = Match(room, token);
            if (candidates.Length == 0) return null;
        }
        string state = $"before={before?.ToString() ?? "?"} after={after?.ToString() ?? "?"}";
        if (!Claim(channel, room + "|" + token + "|" + state)) return null;
        return $"[SCRC-AP] QUEST ACTION DIAGNOSTIC readOnly=True channel={channel} room='{room}' token='{token}' {state} candidates='{string.Join(",", candidates)}'. Discovery hints only; no check dispatched.";
    }

    internal string? ObserveScene(string room, string path, string componentTypes, bool activeSelf, bool activeInHierarchy)
    {
        if (!Safe(room, 64) || !Safe(path, 768) || !Safe(componentTypes, 1024, allowEmpty: true) ||
            !Candidates.Any(c => c.Rooms.Contains(room, StringComparer.Ordinal))) return null;
        bool local = path.StartsWith("Root/" + room + "_Logic/", StringComparison.Ordinal) ||
            (Lobby.Contains(room, StringComparer.Ordinal) && path.StartsWith("Root/GameRoom_Hub1_Logic/", StringComparison.Ordinal));
        if (!local) return null;
        string[] candidates = Match(room, path + " " + componentTypes);
        bool gate = path.Contains("LevelEntranceDoor", StringComparison.Ordinal) ||
                    path.Contains("CanEnterCondition", StringComparison.Ordinal) ||
                    componentTypes.Split('|').Any(t => t == "StarTotalUIView" || t == "StarRequirementPointerIndicatorUIView");
        if (candidates.Length == 0 && !gate) return null;
        string state = $"activeSelf={activeSelf} activeInHierarchy={activeInHierarchy}";
        if (!Claim("scene", room + "|" + path + "|" + componentTypes + "|" + state)) return null;
        return $"[SCRC-AP] QUEST ACTION DIAGNOSTIC readOnly=True channel=scene room='{room}' path='{path}' componentTypes='{componentTypes}' {state} candidates='{string.Join(",", candidates)}' gateOrHudCandidate={gate}.";
    }

    internal IReadOnlyList<string> BeginSnapshot(string room)
    {
        var flags = Candidates.Where(c => c.Rooms.Contains(room, StringComparer.Ordinal) && c.Snapshot != null)
            .SelectMany(c => new[] { c.Snapshot! }.Concat(c.ExtraSnapshots ?? Array.Empty<string>()))
            .Distinct(StringComparer.Ordinal).ToArray();
        if (flags.Length == 0) return flags;
        lock (_sync) return _snapshots.Add(room) ? flags : Array.Empty<string>();
    }

    private static string[] Match(string room, string token) => Candidates
        .Where(c => c.Rooms.Contains(room, StringComparer.Ordinal) && c.Tokens.Any(t => token.Contains(t, StringComparison.OrdinalIgnoreCase)))
        .Select(c => c.Key).ToArray();

    private bool Claim(string channel, string key)
    {
        lock (_sync)
        {
            if (!_seen.TryGetValue(channel, out var keys)) _seen[channel] = keys = new(StringComparer.Ordinal);
            return keys.Count < 64 && keys.Add(key);
        }
    }

    private static bool Safe(string value, int maxLength, bool allowEmpty = false) =>
        value != null && (allowEmpty || !string.IsNullOrWhiteSpace(value)) && value.Length <= maxLength &&
        !value.Any(c => char.IsControl(c) || c == '\'');
}
