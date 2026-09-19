"""Conservative achievable native routes for schema19.

Evidence: docs/HISTORICAL_GAMEPLAY_EVIDENCE.md (Lobby branching, Meat,
Cell and Tower sections), work/batch-native-audit.md and the accepted grouped
native-source run. This is a conservative candidate graph; a fresh full
playthrough must still verify hidden native story conditions. Predicates mean the player can perform the named action;
they never synthesize a native story flag. Vanilla consumables are not AP items.
"""
from .campaign_levels import CAMPAIGN_LEVELS

# Ordered successful native actions required before each level. The conservative
# Lobby route includes Lift Quest even when an arrival normalization skips it.
LEVEL_PREDECESSORS = {
    1: (), 2: (1,), 3: (2,), 4: (3,), 5: (4,),
    6: (5,), 7: (6,), 8: (7,), 9: (7,), 10: (8,9),
    11: (), 12: (11,), 13: (12,), 14: (13,),
    15: (), 16: (15,2,11), 17: (7,16),
    18: (), 19: (), 20: (), 21: (18,19,20), 22: (),
}
LEVEL_ITEMS = {
    3: ('Weed Killer','Plant Pipes'), 4: ('Weed Killer','Plant Pipes'),
    5: ('Hip Glasses','Chicken Bucket'),
    12: ('Hypno Pan',),13: ('Hypno Pan',),14: ('Hypno Pan',),
    17: ('Hypno Pan',),18: ('Violance','Plant Pipes'),
    19: ('Violance',),20: ('Violance','Hypno Pan'),
    21: ('Violance','Plant Pipes','Weed Killer'),
    # No-pipes boss clears depend on luck; never require that route in logic.
    22: ('Plant Pipes',),
}


def star_eater_requirements(requirements):
    # Feeding does not consume stars. Optional Bunker retains its native66;
    # no progression item is placed behind its unmodeled demon route.
    return {'Roots': min(3, requirements['Level 3']),
            'Lobby': requirements['Level 7'],
            'Cell Tower': requirements['Level 16'],
            'Royal Corridor': requirements['Level 21'],
            'Secret Bunker': 66}


def can_complete(world, state, number):
    player=world.player
    level=CAMPAIGN_LEVELS[number-1]
    if not state.has(f'{level.area} Access',player): return False
    if state.count('Star',player)<world.generated_star_requirements[f'Level {number}']: return False
    if not all(state.has(item,player) for item in LEVEL_ITEMS.get(number,())): return False
    return all(can_complete(world,state,prior) for prior in LEVEL_PREDECESSORS[number])


# These actions have source mapping plus historically supported native quest paths. Every value
# is (completed campaign routes, AP item inputs). Empty routes are only used for
# physical hub pickups explicitly recorded before the first level of the area.
SOURCE_PATHS = {
 'Roots - Combo Bucket Conversion': ((5,),('Hip Glasses','Chicken Bucket')),
 'Lobby - Important Letters Pickup': ((6,),()),
 'Lobby - Important Letters Delivery': ((6,),()),
 'Lobby - Plunger Pickup': ((6,),()),
 'Lobby - Plunger Hand-In': ((6,),('Plunger',)),
 'Lobby - Fish Tears Delivery': ((8,9,10,14),()),
 'Meat Dimension - Wooden Spoon Pickup': ((),()),
 'Meat Dimension - Saw Disc Pickup': ((),()),
 'Meat Dimension - Act 1 Music Delivery': ((),()),
 'Meat Dimension - Frying Pan Pickup': ((11,),()),
 'Meat Dimension - Act 2 Music Delivery': ((11,),('Hypno Pan',)),
 'Meat Dimension - Return Cat': ((11,),('Hypno Pan',)),
 'Meat Dimension - Act 3 Music Delivery': ((12,),('Hypno Pan',)),
 'Meat Dimension - Return Scruffy': ((12,),('Hypno Pan',)),
 'Meat Dimension - Act 4 Music Delivery': ((13,),('Hypno Pan',)),
 'Meat Dimension - Mouse Revolution': ((13,),('Hypno Pan',)),
 'Meat Dimension - Fish Tears Award': ((14,),()),
 'Cell Tower - Meet the Bees': ((15,),()),
 'Cell Tower - Deliver Super Nectar': ((15,2,11),()),
 'Tower of Fear - Eye Pickup': ((19,),()),
 'Tower of Fear - Mind Pickup': ((20,),()),
 'Tower of Fear - Heart Pickup': ((18,),()),
 'Tower of Fear - Restore Eye Statue': ((18,19),('Plant Pipes',)),
 'Tower of Fear - Restore Mind Statue': ((19,20),('Violance',)),
 'Tower of Fear - Restore Heart Statue': ((18,20),('Hypno Pan',)),
 'Royal Corridor - Bunker Keycard Award': ((22,),()),
 'Lobby - Use Bunker Keycard': ((6,22),()),
 'Royal Corridor - King Ferdinand Unlocked': ((22,),()),
}


def source_rule(world, name, state):
    if name in SOURCE_PATHS:
        levels,items=SOURCE_PATHS[name]
        return all(can_complete(world,state,n) for n in levels) and all(
            state.has(item,world.player) for item in items)
    requirements=star_eater_requirements(world.generated_star_requirements)
    if name=='Roots - Star Eater Fed':
        return state.count('Star',world.player)>=requirements['Roots']
    if name=='Lobby - Star Eater Fed':
        return source_rule(world,'Lobby - Plunger Hand-In',state) and state.count('Star',world.player)>=requirements['Lobby']
    if name=='Cell Tower - Star Eater Fed':
        return can_complete(world,state,16) and state.count('Star',world.player)>=requirements['Cell Tower']
    if name=='Royal Corridor - Star Eater Fed':
        # Royal phone arrives on the opposite side of the bridge. Traverse all
        # Tower branches and Locker Room; the boss never depends on this route.
        return can_complete(world,state,21) and state.count('Star',world.player)>=requirements['Royal Corridor']
    raise ValueError(f'No modeled native route for {name}')

SAFE_SOURCES=frozenset(SOURCE_PATHS)|frozenset(f'{area} - Star Eater Fed' for area in ('Roots','Lobby','Cell Tower','Royal Corridor'))
