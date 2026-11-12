"""Access rules for optional checks, including the Bunker and Devil quest.

The original route evidence is in docs/archive/testing/2026-09-29-optional-tracker-routes.md.
Schema 21 also models the randomized Bunker Keycard and Demon Key.
"""
from .native_logic import can_complete, source_rule, star_eater_threshold

DEVIL_CHECKS = (
    "Demonic Room - Completion", "Demonic Tower - Completion",
    "Demonic Escape - Completion", "Demonic Lockers - Completion",
)
BUNKER_PROGRESSION_SOURCES = frozenset(DEVIL_CHECKS + (
    "Cartridge Pickup - Superstar", "Cartridge Pickup - Wag the Dog",
    "Secret Bunker - Gecko Interaction", "Secret Bunker - Star Eater Fed",
))


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
    # Legacy seeds use the native keycard after the boss. Schema 21 uses the received item.
    if not source_rule(world, "Lobby - Use Bunker Keycard", state):
        return False
    if name == "Secret Bunker - Gecko Interaction":
        return True
    if name in ("Cartridge Pickup - Superstar", "Secret Bunker - Star Eater Fed"):
        return state.count("Star", player) >= star_eater_threshold(
            world.generated_star_requirements, "Secret Bunker", getattr(world, "bunker_progression", False))
    if getattr(world, "bunker_progression", False) and not state.has("Demon Key", player):
        return False
    if name == "Cartridge Pickup - Wag the Dog":
        return all(_devil_route(world, check, state) for check in DEVIL_CHECKS)
    return _devil_route(world, name, state)


def _devil_route(world, name, state):
    """Check a Devil song after its shared Bunker and Demon Key gates."""
    player = world.player
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
    raise ValueError(f"Unknown optional route: {name}")
