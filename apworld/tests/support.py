from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path
import dataclasses
import sys
import types


SCRC_DIR = Path(__file__).resolve().parents[1] / "scrc"


def load_scrc_module(module_name: str):
    path = SCRC_DIR / f"{module_name}.py"
    package_name = "scrc_test_modules"
    package = sys.modules.get(package_name)
    if package is None:
        package = types.ModuleType(package_name)
        package.__path__ = [str(SCRC_DIR)]
        sys.modules[package_name] = package
    spec = spec_from_file_location(f"{package_name}.{module_name}", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {path}")
    module = module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


class OptionValue:
    def __init__(self, value: int):
        self.value = value


class FakeLocation:
    def __init__(self, name: str):
        self.name = name
        self.access_rule = None


class FakeMultiWorld:
    def __init__(self):
        self.itempool = []
        self.precollected = []
        self.completion_condition = {}
        self.regions = []

    def push_precollected(self, item):
        self.precollected.append(item)

    def get_location(self, name: str, player: int):
        for region in self.regions:
            for location in region.locations:
                if location.name == name and location.player == player:
                    return location
        raise KeyError(name)


def load_scrc_world():
    """Load the real world package against explicit lightweight AP boundaries."""
    controlled_names = (
        "BaseClasses",
        "Options",
        "worlds",
        "worlds.AutoWorld",
        "worlds.generic",
        "worlds.generic.Rules",
        "scrc_test_world",
    )
    previous = {name: sys.modules.get(name) for name in controlled_names}

    base_classes = types.ModuleType("BaseClasses")

    class ItemClassification:
        progression = "progression"
        filler = "filler"

    class Item:
        def __init__(self, name, classification, code, player):
            self.name = name
            self.classification = classification
            self.code = code
            self.player = player

    class Location:
        def __init__(self, player, name, address, parent):
            self.player = player
            self.name = name
            self.address = address
            self.parent_region = parent
            self.access_rule = lambda state: True
            self.item_rule = lambda item: True
            self.item = None

        def place_locked_item(self, item):
            self.item = item

    class Region:
        def __init__(self, name, player, multiworld):
            self.name = name
            self.player = player
            self.multiworld = multiworld
            self.locations = []
            self.connections = []

        def connect(self, other_region, name, rule=None):
            self.connections.append(
                types.SimpleNamespace(
                    name=name,
                    connected_region=other_region,
                    access_rule=rule or (lambda state: True),
                )
            )

    class Tutorial:
        def __init__(self, *args):
            self.args = args

    base_classes.ItemClassification = ItemClassification
    base_classes.Item = Item
    base_classes.Location = Location
    base_classes.Region = Region
    base_classes.Tutorial = Tutorial

    options = types.ModuleType("Options")

    class Choice:
        pass

    class Range:
        pass

    @dataclasses.dataclass
    class PerGameCommonOptions:
        accessibility: object

    options.Choice = Choice
    options.Range = Range
    options.PerGameCommonOptions = PerGameCommonOptions

    worlds = types.ModuleType("worlds")
    worlds.__path__ = []
    auto_world = types.ModuleType("worlds.AutoWorld")

    class WebWorld:
        pass

    class World:
        pass

    auto_world.WebWorld = WebWorld
    auto_world.World = World
    generic = types.ModuleType("worlds.generic")
    generic.__path__ = []
    rules = types.ModuleType("worlds.generic.Rules")

    def set_rule(location, rule):
        location.access_rule = rule

    rules.set_rule = set_rule

    replacements = {
        "BaseClasses": base_classes,
        "Options": options,
        "worlds": worlds,
        "worlds.AutoWorld": auto_world,
        "worlds.generic": generic,
        "worlds.generic.Rules": rules,
    }
    sys.modules.update(replacements)

    package_name = "scrc_test_world"
    spec = spec_from_file_location(
        package_name,
        SCRC_DIR / "__init__.py",
        submodule_search_locations=[str(SCRC_DIR)],
    )
    if spec is None or spec.loader is None:
        raise RuntimeError("cannot load SCRC world package")
    module = module_from_spec(spec)
    sys.modules[package_name] = module
    spec.loader.exec_module(module)

    def cleanup():
        for name in tuple(sys.modules):
            if name == package_name or name.startswith(f"{package_name}."):
                sys.modules.pop(name, None)
        for name, old_module in previous.items():
            if old_module is None:
                sys.modules.pop(name, None)
            else:
                sys.modules[name] = old_module

    return module, cleanup
