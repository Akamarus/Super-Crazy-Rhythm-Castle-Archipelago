"""Seed-authored cassette bundles; ordinal IDs have permanent meanings."""
from .cassettes import CASSETTES

BUNDLE_ITEM_NAME_TO_ID = {
    f"Cassette Bundle {i:02}": 187256163 + i for i in range(1, 16)
}
CASSETTE_NAMES = frozenset(c.item_name for c in CASSETTES)


def validate_bundles(size, groups):
    """Return an independent, canonical map or reject a partial/ambiguous contract."""
    if type(size) is not int or size not in (1, 2, 3):
        raise ValueError("cassette_bundle_size must be 1, 2 or 3")
    if not isinstance(groups, dict):
        raise ValueError("cassette_bundles must be a dictionary")
    expected = list(BUNDLE_ITEM_NAME_TO_ID)[:0 if size == 1 else (len(CASSETTES)+size-1)//size]
    if set(groups) != set(expected):
        raise ValueError("cassette_bundles has missing or unexpected bundle names")
    result = {}
    seen = set()
    for index, name in enumerate(expected):
        songs = groups[name]
        length = min(size, len(CASSETTES)-index*size)
        if not isinstance(songs, (list, tuple)) or len(songs) != length:
            raise ValueError(f"{name} has invalid cassette count")
        for song in songs:
            if not isinstance(song, str) or song not in CASSETTE_NAMES or song in seen:
                raise ValueError(f"{name} contains an unknown or repeated cassette")
            seen.add(song)
        result[name] = list(songs)
    if size > 1 and seen != CASSETTE_NAMES:
        raise ValueError("cassette_bundles must cover every cassette exactly once")
    return result


def generate_bundles(size, rng):
    if type(size) is not int or size not in (1, 2, 3):
        raise ValueError("cassette_bundle_size must be 1, 2 or 3")
    if size == 1:
        return {}
    songs = [c.item_name for c in CASSETTES]
    rng.shuffle(songs)
    groups = {name: songs[i*size:(i+1)*size]
              for i, name in enumerate(list(BUNDLE_ITEM_NAME_TO_ID)[:(len(songs)+size-1)//size])}
    return validate_bundles(size, groups)


def bundle_for_cassette(groups):
    return {song: bundle for bundle, songs in groups.items() for song in songs}
