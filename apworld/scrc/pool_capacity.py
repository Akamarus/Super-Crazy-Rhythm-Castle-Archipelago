"""Fit local non-filler demand without discarding progression or changing IDs."""

from collections import Counter
from typing import Iterable, Sequence

from .music_lab_points import MUSIC_LAB_POINT_ITEMS


def fit_non_filler_pool(
    item_names: Sequence[str],
    eligible_location_names: Iterable[str],
    excluded_location_names: Iterable[str],
) -> list[str]:
    """Consolidate existing point denominations only when safe slots run short.

    The caller supplies addressed, unfilled locations allowing non-filler items.
    Exclusions are supplied explicitly because AP applies their item rules after
    create_items. This is a conservative local budget, not a substitute for AP's
    reachability, locality, or other per-item placement checks.
    """
    capacity = len(set(eligible_location_names) - set(excluded_location_names))
    pool = list(item_names)
    shortage = len(pool) - capacity
    if shortage <= 0:
        return pool

    single, bundle, large = (entry.name for entry in MUSIC_LAB_POINT_ITEMS)
    counts = Counter(pool)
    # Each ten singles -> one bundle saves nine slots; each two bundles ->
    # one large bundle saves one. Enumerating both avoids needless compression
    # when a small shortage can be resolved without removing the singles.
    choices = [
        (9 * singles_exchanged + bundles_exchanged,
         singles_exchanged, bundles_exchanged)
        for singles_exchanged in range(counts[single] // 10 + 1)
        for bundles_exchanged in range((counts[bundle] + singles_exchanged) // 2 + 1)
    ]
    fitting = [choice for choice in choices if choice[0] >= shortage]
    if not fitting:
        minimum_required = len(pool) - max(choice[0] for choice in choices)
        raise ValueError(
            f"{minimum_required} non-filler instances remain after maximum Music Lab "
            f"point consolidation, exceeding {capacity} modeled non-excluded locations. "
            "Reduce excluded locations or choose a setting with more available checks."
        )

    _, singles_exchanged, bundles_exchanged = min(fitting)
    for _ in range(10 * singles_exchanged):
        pool.remove(single)
    pool.extend([bundle] * singles_exchanged)
    for _ in range(2 * bundles_exchanged):
        pool.remove(bundle)
    pool.extend([large] * bundles_exchanged)
    return pool
