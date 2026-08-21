from __future__ import annotations

import random


STARTING_AREA_RANDOM = 0
STARTING_AREA_ROOTS = 1
STARTING_AREA_LOBBY = 2
STARTING_AREA_MEAT_DIMENSION = 3
STARTING_AREA_CELL_TOWER = 4

STARTING_AREA_NAMES = {
    STARTING_AREA_RANDOM: "Random",
    STARTING_AREA_ROOTS: "Roots",
    STARTING_AREA_LOBBY: "Lobby",
    STARTING_AREA_MEAT_DIMENSION: "Meat Dimension",
    STARTING_AREA_CELL_TOWER: "Cell Tower",
}

STARTING_AREA_TO_ITEM = {
    STARTING_AREA_ROOTS: "Roots Access",
    STARTING_AREA_LOBBY: "Lobby Access",
    STARTING_AREA_MEAT_DIMENSION: "Meat Dimension Access",
    STARTING_AREA_CELL_TOWER: "Cell Tower Access",
}

VALIDATED_STARTING_AREAS = ("Roots Access",)


def resolve_starting_area(requested: int, rng: random.Random) -> str:
    if isinstance(requested, bool) or not isinstance(requested, int):
        raise ValueError("starting_area must be an integer option value from 0 through 4")
    if requested not in STARTING_AREA_NAMES:
        raise ValueError("starting_area must be from 0 through 4")
    if requested == STARTING_AREA_RANDOM:
        return rng.choice(VALIDATED_STARTING_AREAS)

    item_name = STARTING_AREA_TO_ITEM[requested]
    if item_name not in VALIDATED_STARTING_AREAS:
        area_name = STARTING_AREA_NAMES[requested]
        raise ValueError(f"{area_name} starting area is not validated yet")
    return item_name
