"""Supplemental passive source checks; native rewards are preserved.

The original schema18 flags below are retained as catalog history. Schema19
installs candidate native-route rules from native_logic; unsupported special
completion and Bunker paths remain Stardust-only.
"""
from dataclasses import dataclass

@dataclass(frozen=True)
class ExpandedCheck:
    name: str
    location_id: int
    region: str
    room: str = ""
    flag: str = ""
    level: str = ""
    variant: str = ""
    required_items: tuple[str, ...] = ()
    progression_safe: bool = False

EXPANDED_CHECKS = (
    ExpandedCheck('Roots - Combo Bucket Conversion', 187256298, 'Roots', room='GameRoom_09', flag='LEVEL_09_COMBO_ABILITY_EARNED', required_items=('Weed Killer', 'Plant Pipes', 'Hip Glasses', 'Chicken Bucket', 'Bucket Minion Trade Complete'), progression_safe=True),
    ExpandedCheck('Lobby - Important Letters Delivery', 187256299, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_MENIAL_TASK_ITEM_DEPOSITED'),
    ExpandedCheck('Lobby - Plunger Hand-In', 187256300, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_MEAT_DOOR_BLOCKER_PLUNGERED'),
    ExpandedCheck('Lobby - Star Eater Fed', 187256301, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_STAR_EATER_FED'),
    ExpandedCheck('Lobby - Fish Tears Delivery', 187256302, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_FISH_TEARS_DEPOSITED'),
    ExpandedCheck('Meat Dimension - Act 1 Music Delivery', 187256303, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_ONE_MUSIC_DONE'),
    ExpandedCheck('Meat Dimension - Act 2 Music Delivery', 187256304, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_TWO_MUSIC_DONE'),
    ExpandedCheck('Meat Dimension - Act 3 Music Delivery', 187256305, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_THREE_MUSIC_DONE'),
    ExpandedCheck('Meat Dimension - Act 4 Music Delivery', 187256306, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_FOUR_MUSIC_DONE'),
    ExpandedCheck('Meat Dimension - Return Cat', 187256307, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_TWO_BOUNCER_REQUIREMENT_DONE'),
    ExpandedCheck('Meat Dimension - Return Scruffy', 187256308, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_ACT_THREE_BOUNCER_REQUIREMENT_DONE'),
    ExpandedCheck('Meat Dimension - Mouse Revolution', 187256309, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_MOUSE_REVOLUTION_TRIGGERED'),
    ExpandedCheck('Cell Tower - Deliver Super Nectar', 187256310, 'Cell Tower', room='GameRoom_Hub5B', flag='PRISON_HUB_BEES_ESCAPED'),
    ExpandedCheck('Tower of Fear - Restore Eye Statue', 187256311, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_DARKNESS_AREA_COMPLETED'),
    ExpandedCheck('Tower of Fear - Restore Mind Statue', 187256312, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_COMPLEXITY_AREA_COMPLETED'),
    ExpandedCheck('Tower of Fear - Restore Heart Statue', 187256313, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_LONELINESS_AREA_COMPLETED'),
    ExpandedCheck('Royal Corridor - Star Eater Fed', 187256314, 'Royal Corridor', room='GameRoom_Hub7', flag='KING_CORRIDOR_STAR_EATER_FED'),
    ExpandedCheck('Lobby - Important Letters Pickup', 187256292, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_MENIAL_TASK_ITEM_COLLECTED'),
    ExpandedCheck('Lobby - Demolition Certificate Award', 187256315, 'Lobby', room='GameRoom_19', flag='LOBBY_HUB_TOOLS_CERTIFICATE'),
    ExpandedCheck('Meat Dimension - Wooden Spoon Pickup', 187256316, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_WOODEN_SPOON_COLLECTED'),
    ExpandedCheck('Meat Dimension - Saw Disc Pickup', 187256317, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_SAW_DISC_COLLECTED'),
    ExpandedCheck('Meat Dimension - Frying Pan Pickup', 187256318, 'Meat Dimension', room='GameRoom_Hub4', flag='MEAT_HUB_FRYING_PAN_COLLECTED'),
    ExpandedCheck('Meat Dimension - Fish Tears Award', 187256319, 'Meat Dimension', room='GameRoom_23', flag='LEVEL_23_FISH_TEARS_COLLECTED'),
    ExpandedCheck('Cell Tower - Meet the Bees', 187256320, 'Cell Tower', room='GameRoom_Hub5B', flag='PRISON_HUB_SPOKEN_TO_BEES'),
    ExpandedCheck('Tower of Fear - Eye Pickup', 187256321, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_EYE_COLLECTED'),
    ExpandedCheck('Tower of Fear - Mind Pickup', 187256322, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_BRAIN_COLLECTED'),
    ExpandedCheck('Tower of Fear - Heart Pickup', 187256323, 'Tower of Fear', room='GameRoom_Hub3', flag='MADNESS_HUB_HEART_COLLECTED'),
    ExpandedCheck('Royal Corridor - Bunker Keycard Award', 187256324, 'Royal Corridor', room='GameRoom_28', flag='LEVEL_28_KEY_CARD_COLLECTED'),
    ExpandedCheck('Lobby - Use Bunker Keycard', 187256325, 'Lobby', room='GameRoom_Hub1A', flag='LOBBY_HUB_LIFT_KEY_USED'),
    ExpandedCheck('Secret Bunker - Gecko Interaction', 187256326, 'Secret Bunker', room='GameRoom_Hub8', flag='BUNKER_HUB_GECKO_INITIAL_INTERACTION'),
    ExpandedCheck('Secret Bunker - Star Eater Fed', 187256327, 'Secret Bunker', room='GameRoom_Hub8', flag='BUNKER_HUB_STAR_EATER_FED'),
    ExpandedCheck('Nectar Party - Completion', 187256328, 'Roots', level='Level_06', variant='LevelVariant_BeeMode'),
    ExpandedCheck('Act 1B: Nectar - Completion', 187256329, 'Meat Dimension', level='Level_12', variant='LevelVariant_BeeMode'),
    ExpandedCheck('Demonic Room - Completion', 187256330, 'Secret Bunker', level='Level_02', variant='LevelVariant_DevilMode'),
    ExpandedCheck('Demonic Tower - Completion', 187256331, 'Secret Bunker', level='Level_11', variant='LevelVariant_DevilMode'),
    ExpandedCheck('Demonic Escape - Completion', 187256332, 'Secret Bunker', level='Level_13', variant='LevelVariant_DevilMode'),
    ExpandedCheck('Demonic Lockers - Completion', 187256333, 'Secret Bunker', level='Level_14', variant='LevelVariant_DevilMode'),
    ExpandedCheck('Cell Tower - Star Eater Fed',187256334,'Cell Tower',room='GameRoom_Hub5B',flag='PRISON_HUB_STAR_EATER_FED'),
    ExpandedCheck('Royal Corridor - King Ferdinand Unlocked',187256335,'Royal Corridor',room='GameRoom_28B'),
)
EXPANDED_CHECK_LOCATION_NAME_TO_ID = {entry.name: entry.location_id for entry in EXPANDED_CHECKS}
