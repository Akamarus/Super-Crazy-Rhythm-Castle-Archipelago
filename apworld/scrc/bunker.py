"""Schema21 Bunker contracts; old seeds never opt in implicitly."""
ITEMS = {
    'Bunker Keycard': (187256179, 'LIFT_KEY_BAG_ITEM'),
    'Demon Key': (187256180, 'BUNKER_HUB_DEMON_KEY'),
    'Virus': (187256181, 'KING_VIRUS_DATA_STICK_BAG_ITEM'),
    'Cheat Codes': (187256182, 'KING_VIRUS_CHEAT_CODES_BAG_ITEM'),
    'Game Ender': (187256183, 'KING_VIRUS_GAME_ENDER_BAG_ITEM'),
}
CHECKS = {
    'Lift Quest - Virus Retrieved': (187256336, 'OVERALL_PROGRESS_RETRIEVED_VIRUS'),
    'School Trip - Cheat Codes Retrieved': (187256337, 'OVERALL_PROGRESS_RETRIEVED_CHEAT_CODES'),
    'Meat Dimension - Game Ender Created': (187256338, 'OVERALL_PROGRESS_CREATED_GAME_ENDER'),
    'Central Mainframe - Game Ender Used': (187256339, 'OVERALL_PROGRESS_USED_GAME_ENDER'),
    'Secret Bunker - Demon Key Pickup': (187256340, 'BUNKER_HUB_DEMON_KEY'),
    'True Ending - Completion': (187256341, 'OVERALL_PROGRESS_BEAT_KING_TWO'),
}
ITEM_IDS = {n:v[0] for n,v in ITEMS.items()}
CHECK_IDS = {n:v[0] for n,v in CHECKS.items()}
GOALS = ('king_ferdinand', 'true_ending')

def enabled(world):
    return getattr(world, 'bunker_progression', False)

def can_enter(world, state):
    from .native_logic import can_complete
    return state.has('Bunker Keycard',world.player) and can_complete(world,state,6)

def can_install(world, state):
    from .native_logic import can_complete
    return can_enter(world,state) and state.has('Game Ender',world.player) and can_complete(world,state,15)

def can_finish(world,state):
    from .native_logic import can_complete
    return can_install(world,state) and can_complete(world,state,22)

def rule(world,name,state):
    from .native_logic import can_complete
    if not can_enter(world,state): return False
    if name=='Secret Bunker - Demon Key Pickup': return True
    if name=='Lift Quest - Virus Retrieved': return can_complete(world,state,5)
    if name=='School Trip - Cheat Codes Retrieved': return can_complete(world,state,9)
    if name=='Meat Dimension - Game Ender Created':
        return all(state.has(n,world.player) for n in ('Meat Dimension Access','Virus','Cheat Codes'))
    if name=='Central Mainframe - Game Ender Used': return can_install(world,state)
    if name=='True Ending - Completion': return can_finish(world,state)
    raise ValueError(name)

def metadata(active, goal):
    return {'bunker_progression': active, 'goal': goal,
            'bunker_item_ids': dict(ITEM_IDS) if active else {},
            'bunker_check_ids': dict(CHECK_IDS) if active else {},
            'true_ending_native_flag': 'OVERALL_PROGRESS_BEAT_KING_TWO' if active else ''}
