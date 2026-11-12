from BaseClasses import ItemClassification


def non_progression_item_allowed(item) -> bool:
    classification = item.classification
    progression = ItemClassification.progression
    try:
        return not bool(classification & progression)
    except TypeError:
        return classification != progression


def filler_item_allowed(item) -> bool:
    """Accept any world's filler/traps without stranding useful or progression items."""
    classification = item.classification
    try:
        return not bool(classification & (ItemClassification.progression | ItemClassification.useful))
    except TypeError:
        return classification not in (ItemClassification.progression, ItemClassification.useful)
