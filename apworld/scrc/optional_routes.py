"""Conservative access routes for optional checks; placement remains filler-only.

Native scene evidence and traversal limits are recorded in
 docs/testing/2026-09-29-optional-tracker-routes.md.
These predicates describe actions a player can perform, not flags already set.
"""
from .native_logic import can_complete, source_rule, star_eater_requirements

DEVIL_CHECKS = (
    "Demonic Room - Completion", "Demonic Tower - Completion",
    "Demonic Escape - Completion", "Demonic Lockers - Completion",
)
OPTIONAL_ROUTE_CHECKS = frozenset(DEVIL_CHECKS + (
    "Cartridge Pickup - Smooch", "Cartridge Pickup - Superstar",
    "Cartridge Pickup - Wag the Dog", "Nectar Party - Completion",
    "Act 1B: Nectar - Completion", "Secret Bunker - Gecko Interaction",
    "Secret Bunker - Star Eater Fed", "Lobby - Demolition Certificate Award",
))


def can_enter_bunker(world, state):
    # The boss supplies the native keycard; the existing Lobby route reaches
    # its slot. The Demon Key is then freely obtainable inside the Bunker.
    return source_rule(world, "Lobby - Use Bunker Keycard", state)


def optional_route(world, name, state):
    player = world.player
    if name == "Cartridge Pickup - Smooch":
        # User confirmed immediate pickup from the hub.
        return state.has('Meat Dimension Access', player)
    if name == "Lobby - Demolition Certificate Award":
        return can_complete(world, state, 6)
    if name in ("Nectar Party - Completion", "Act 1B: Nectar - Completion"):
        # The Bees can be reached directly from the Cell Tower phone booth.
        base = 2 if name == "Nectar Party - Completion" else 11
        return (state.has('Cell Tower Access', player)
                and can_complete(world, state, base)
                and state.has("Hypno Pan", player))
    if not can_enter_bunker(world, state):
        return False
    if name == "Secret Bunker - Gecko Interaction":
        return True
    if name in ("Cartridge Pickup - Superstar", "Secret Bunker - Star Eater Fed"):
        return state.count("Star", player) >= star_eater_requirements(
            world.generated_star_requirements, getattr(world,"bunker_progression",False))["Secret Bunker"]
    if getattr(world,"bunker_progression",False) and name in (*DEVIL_CHECKS,"Cartridge Pickup - Wag the Dog") and not state.has("Demon Key",player):
        return False
    if name == "Demonic Room - Completion":
        return can_complete(world, state, 5) and state.has("Lobby Access", player)
    if name == "Demonic Tower - Completion":
        return can_complete(world, state, 7) and state.has("Weed Killer", player)
    if name == "Demonic Escape - Completion":
        return state.has("Tower of Fear Access", player) and state.has("Violance", player)
    if name == "Demonic Lockers - Completion":
        # Approach from the Tower side of the broken Royal bridge. The Devil
        # door itself requires the key, not an ordinary Locker Room clear.
        return (all(can_complete(world, state, n) for n in (18, 19, 20))
                and state.has("Royal Corridor Access", player)
                and state.has("Weed Killer", player))
    if name == "Cartridge Pickup - Wag the Dog":
        return all(optional_route(world, check, state) for check in DEVIL_CHECKS)
    raise ValueError(f"Unknown optional route: {name}")
