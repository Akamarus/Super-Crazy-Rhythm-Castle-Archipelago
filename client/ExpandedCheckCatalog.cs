namespace RhythmCastleAP;

internal sealed record ExpandedCheckEntry(long Id, string Name, string Room, string Flag, string? Level = null, string? Variant = null, string? Character = null);

internal static class ExpandedCheckCatalog
{
    internal static readonly IReadOnlyList<ExpandedCheckEntry> Entries = new ExpandedCheckEntry[]
    {
        new(187256298, "Roots - Combo Bucket Conversion", "GameRoom_09", "LEVEL_09_COMBO_ABILITY_EARNED"),
        new(187256299, "Lobby - Important Letters Delivery", "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED"),
        new(187256300, "Lobby - Plunger Hand-In", "GameRoom_Hub1A", "LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED"),
        new(187256301, "Lobby - Star Eater Fed", "GameRoom_Hub1A", "LOBBY_HUB_STAR_EATER_FED"),
        new(187256302, "Lobby - Fish Tears Delivery", "GameRoom_Hub1A", "LOBBY_HUB_FISH_TEARS_DEPOSITED"),
        new(187256303, "Meat Dimension - Act 1 Music Delivery", "GameRoom_Hub4", "MEAT_HUB_ACT_ONE_MUSIC_DONE"),
        new(187256304, "Meat Dimension - Act 2 Music Delivery", "GameRoom_Hub4", "MEAT_HUB_ACT_TWO_MUSIC_DONE"),
        new(187256305, "Meat Dimension - Act 3 Music Delivery", "GameRoom_Hub4", "MEAT_HUB_ACT_THREE_MUSIC_DONE"),
        new(187256306, "Meat Dimension - Act 4 Music Delivery", "GameRoom_Hub4", "MEAT_HUB_ACT_FOUR_MUSIC_DONE"),
        new(187256307, "Meat Dimension - Return Cat", "GameRoom_Hub4", "MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE"),
        new(187256308, "Meat Dimension - Return Scruffy", "GameRoom_Hub4", "MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE"),
        new(187256309, "Meat Dimension - Mouse Revolution", "GameRoom_Hub4", "MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED"),
        new(187256310, "Cell Tower - Deliver Super Nectar", "GameRoom_Hub5B", "PRISON_HUB_BEES_ESCAPED"),
        new(187256311, "Tower of Fear - Restore Eye Statue", "GameRoom_Hub3", "MADNESS_HUB_DARKNESS_AREA_COMPLETED"),
        new(187256312, "Tower of Fear - Restore Mind Statue", "GameRoom_Hub3", "MADNESS_HUB_COMPLEXITY_AREA_COMPLETED"),
        new(187256313, "Tower of Fear - Restore Heart Statue", "GameRoom_Hub3", "MADNESS_HUB_LONELINESS_AREA_COMPLETED"),
        new(187256314, "Royal Corridor - Star Eater Fed", "GameRoom_Hub7", "KING_CORRIDOR_STAR_EATER_FED"),
        new(187256292, "Lobby - Important Letters Pickup", "GameRoom_Hub1A", "LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED"),
        new(187256315, "Lobby - Demolition Certificate Award", "GameRoom_19", "LOBBY_HUB_TOOLS_CERTIFICATE"),
        new(187256316, "Meat Dimension - Wooden Spoon Pickup", "GameRoom_Hub4", "MEAT_HUB_WOODEN_SPOON_COLLECTED"),
        new(187256317, "Meat Dimension - Saw Disc Pickup", "GameRoom_Hub4", "MEAT_HUB_SAW_DISC_COLLECTED"),
        new(187256318, "Meat Dimension - Frying Pan Pickup", "GameRoom_Hub4", "MEAT_HUB_FRYING_PAN_COLLECTED"),
        new(187256319, "Meat Dimension - Fish Tears Award", "GameRoom_23", "LEVEL_23_FISH_TEARS_COLLECTED"),
        new(187256320, "Cell Tower - Meet the Bees", "GameRoom_Hub5B", "PRISON_HUB_SPOKEN_TO_BEES"),
        new(187256321, "Tower of Fear - Eye Pickup", "GameRoom_Hub3", "MADNESS_HUB_EYE_COLLECTED"),
        new(187256322, "Tower of Fear - Mind Pickup", "GameRoom_Hub3", "MADNESS_HUB_BRAIN_COLLECTED"),
        new(187256323, "Tower of Fear - Heart Pickup", "GameRoom_Hub3", "MADNESS_HUB_HEART_COLLECTED"),
        new(187256324, "Royal Corridor - Bunker Keycard Award", "GameRoom_28", "LEVEL_28_KEY_CARD_COLLECTED"),
        new(187256325, "Lobby - Use Bunker Keycard", "GameRoom_Hub1A", "LOBBY_HUB_LIFT_KEY_USED"),
        new(187256326, "Secret Bunker - Gecko Interaction", "GameRoom_Hub8", "BUNKER_HUB_GECKO_INITIAL_INTERACTION"),
        new(187256327, "Secret Bunker - Star Eater Fed", "GameRoom_Hub8", "BUNKER_HUB_STAR_EATER_FED"),
        new(187256328, "Nectar Party - Completion", "", "", "Level_06", "LevelVariant_BeeMode"),
        new(187256329, "Act 1B: Nectar - Completion", "", "", "Level_12", "LevelVariant_BeeMode"),
        new(187256330, "Demonic Room - Completion", "", "", "Level_02", "LevelVariant_DevilMode"),
        new(187256331, "Demonic Tower - Completion", "", "", "Level_11", "LevelVariant_DevilMode"),
        new(187256332, "Demonic Escape - Completion", "", "", "Level_13", "LevelVariant_DevilMode"),
        new(187256333, "Demonic Lockers - Completion", "", "", "Level_14", "LevelVariant_DevilMode"),
    };
    internal static readonly IReadOnlyList<ExpandedCheckEntry> StarEntries = Entries.Concat(new ExpandedCheckEntry[] {
        new(187256334, "Cell Tower - Star Eater Fed", "GameRoom_Hub5B", "PRISON_HUB_STAR_EATER_FED"),
        new(187256335, "Royal Corridor - King Ferdinand Unlocked", "GameRoom_28B", "", Character: "KING"),
    }).ToArray();
}
