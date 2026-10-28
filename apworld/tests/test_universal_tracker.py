import copy
import json
import random
import types
import unittest
from support import FakeMultiWorld, OptionValue, load_scrc_world


class UniversalTrackerTests(unittest.TestCase):
    def setUp(self):
        self.module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)

    def world(self, seed=1, goal=40, difficulty=1, passthrough=None):
        w = object.__new__(self.module.SCRCWorld)
        w.player = 1
        w.random = random.Random(seed)
        w.multiworld = FakeMultiWorld()
        if passthrough is not None:
            w.multiworld.re_gen_passthrough = {w.game: passthrough}
            w.multiworld.generation_is_fake = True
        w.options = types.SimpleNamespace(required_stars=OptionValue(goal),
            difficulty=OptionValue(difficulty), starting_area=OptionValue(0))
        w.generate_early()
        w.create_regions()
        w.set_rules()
        return w

    def test_cell_path_capability_and_legacy_reconstruction(self):
        from test_world_integration import State
        source = self.world()
        slot = source.fill_slot_data()
        self.assertIs(slot["cell_tower_open_route"], True)
        items = State(["Cell Tower Access", "Violance"] + ["Star"] * 66)
        for data in (slot, self.module.SCRCWorld.interpret_slot_data(slot)):
            restored = self.world(passthrough=data)
            self.assertTrue(restored.multiworld.get_location("Level 16 - Completion", 1).access_rule(items))
            self.assertTrue(restored.multiworld.get_location("Cell Tower - Star Eater Fed", 1).access_rule(items))
            self.assertFalse(restored.multiworld.get_location("Cell Tower - Deliver Super Nectar", 1).access_rule(items))
        legacy = copy.deepcopy(slot)
        legacy.pop("cell_tower_open_route")
        legacy["implementation_version"] = legacy["implementation_version"].replace("-cell-tower-path-1", "")
        restored = self.world(passthrough=legacy)
        self.assertFalse(restored.multiworld.get_location("Level 16 - Completion", 1).access_rule(items))
        self.assertFalse(restored.fill_slot_data()["cell_tower_open_route"])
        for bad in (False, 1, "true", None):
            broken = copy.deepcopy(slot)
            broken["cell_tower_open_route"] = bad
            with self.assertRaises(ValueError):
                self.module.SCRCWorld.interpret_slot_data(broken)

    def test_lobby_door_routes_and_legacy(self):
        from test_world_integration import State
        source = self.world()
        slot = source.fill_slot_data()
        self.assertTrue(slot["lobby_open_doors"])
        for data in (slot, self.module.SCRCWorld.interpret_slot_data(slot)):
            w = self.world(passthrough=data)
            w.generated_star_requirements.update({"Level 6": 39, "Level 7": 4, "Level 9": 2})
            loc = lambda n: w.multiworld.get_location(f"Level {n} - Completion",1)
            self.assertTrue(loc(7).access_rule(State(["Lobby Access"]+["Star"]*4)))
            self.assertTrue(loc(9).access_rule(State(["Lobby Access"]+["Star"]*2)))
            self.assertFalse(loc(9).access_rule(State(["Lobby Access"]+["Star"])))
            self.assertFalse(loc(7).access_rule(State(["Star"]*66)))
        legacy=copy.deepcopy(slot)
        legacy.pop("lobby_open_doors")
        legacy["implementation_version"]=legacy["implementation_version"].replace("-lobby-doors-1", "")
        w=self.world(passthrough=legacy)
        w.generated_star_requirements.update({"Level 6":39,"Level 7":4,"Level 9":2})
        self.assertFalse(w.multiworld.get_location("Level 9 - Completion",1).access_rule(State(["Lobby Access"]+["Star"]*4)))
        for bad in (False,1,"true",None):
            broken=copy.deepcopy(slot);broken["lobby_open_doors"]=bad
            with self.assertRaises(ValueError): self.module.SCRCWorld.interpret_slot_data(broken)

    def test_tracker_restores_server_gates_with_different_rng_and_options(self):
        for difficulty in range(4):
            for goal in (1, 40, 66):
                with self.subTest(difficulty=difficulty, goal=goal):
                    source = self.world(10, goal, difficulty)
                    slot = json.loads(json.dumps(source.fill_slot_data()))
                    data = self.module.SCRCWorld.interpret_slot_data(slot)
                    tracker = self.world(900, 50, 0, data)
                    self.assertEqual(tracker.generated_star_requirements, source.generated_star_requirements)
                    self.assertEqual(tracker.active_location_names, source.active_location_names)
                    self.assertEqual(tracker.starting_area_item, source.starting_area_item)
                    self.assertEqual(tracker.options.required_stars.value, goal)
                    self.assertEqual(tracker.options.difficulty.value, difficulty)
                    self.assertEqual(tracker.fill_slot_data()['star_eater_requirements'], slot['star_eater_requirements'])

    def test_existing_schema19_slot_uses_corrected_optional_routes(self):
        from test_world_integration import State
        source = self.world()
        slot = json.loads(json.dumps(source.fill_slot_data()))
        tracker = self.world(900, 1, 0, self.module.SCRCWorld.interpret_slot_data(slot))
        for name in ("Cartridge Pickup - Smooch", "Cartridge Pickup - Superstar",
                     "Cartridge Pickup - Wag the Dog", "Nectar Party - Completion"):
            self.assertFalse(tracker.multiworld.get_location(name, 1).access_rule(State(["Roots Access"])))
        self.assertEqual(tracker.generated_star_requirements, slot["generated_star_requirements"])
        self.assertEqual(len(tracker.active_location_names), slot["active_location_count"])

    def test_restored_lobby_route_does_not_require_lift_quest(self):
        from test_world_integration import State
        source = self.world()
        slot = json.loads(json.dumps(source.fill_slot_data()))
        tracker = self.world(900, 1, 0, self.module.SCRCWorld.interpret_slot_data(slot))
        state = State(["Lobby Access", "Plunger"] + ["Star"] * 40)
        for name in ("Level 6 - Completion", "Level 8 - Completion", "Level 10 - Completion",
                     "Lobby - Plunger Hand-In", "Lobby - Star Eater Fed"):
            with self.subTest(name=name):
                self.assertTrue(tracker.multiworld.get_location(name, 1).access_rule(state))
        self.assertEqual(tracker.generated_star_requirements, slot["generated_star_requirements"])

    def test_yaml_less_support_is_explicit(self):
        self.assertTrue(self.module.SCRCWorld.ut_can_gen_without_yaml)
        self.assertIsInstance(vars(self.module.SCRCWorld)['interpret_slot_data'], staticmethod)

    def test_malformed_slot_data_fails_instead_of_rolling_new_logic(self):
        slot = self.world().fill_slot_data()
        for key,value in [('schema_version',18),('required_stars',True),('required_stars',0),
                ('difficulty', {'value': 4}),('starting_area_item','Tower of Fear Access'),
                ('generated_star_requirements',{}),('generated_star_requirements',{'Level 1':99})]:
            with self.subTest(key=key, value=value):
                bad=copy.deepcopy(slot);bad[key]=value
                with self.assertRaises(ValueError): self.module.SCRCWorld.interpret_slot_data(bad)

    def test_authoritative_metadata_rejects_contradictions(self):
        slot = self.world().fill_slot_data()
        changes = {
            "schema_version": 19.0,
            "star_eater_requirements": {"Roots": 66},
            "active_location_count": 0,
            "active_campaign_locations": [],
            "active_campaign_location_tiers": [],
            "active_campaign_star_tiers": [1],
            "active_medal_tiers": ["Bronze"],
            "difficulty_filtering_active": False,
        }
        for key, value in changes.items():
            with self.subTest(key=key):
                bad = copy.deepcopy(slot); bad[key] = value
                with self.assertRaises(ValueError):
                    self.module.SCRCWorld.interpret_slot_data(bad)

    def test_schema19_legacy_omission_is_explicitly_supported(self):
        slot = self.world().fill_slot_data()
        for key in ("star_eater_requirements", "active_location_count",
                    "active_campaign_locations", "active_campaign_location_tiers",
                    "active_campaign_star_tiers", "active_medal_tiers",
                    "difficulty_filtering_active"):
            del slot[key]
        restored = self.world(passthrough=self.module.SCRCWorld.interpret_slot_data(slot))
        self.assertEqual(restored.active_location_names, self.world().active_location_names)

    def test_metadata_rejects_numeric_type_coercion_and_duplicates(self):
        slot = self.world().fill_slot_data()
        changes = {
            "active_location_count": float(slot["active_location_count"]),
            "active_campaign_star_tiers": [True, 2],
            "active_campaign_locations": slot["active_campaign_locations"] * 2,
            "star_eater_requirements": dict(slot["star_eater_requirements"], Roots=3.0),
        }
        for key, value in changes.items():
            with self.subTest(key=key):
                bad = copy.deepcopy(slot); bad[key] = value
                with self.assertRaises(ValueError):
                    self.module.SCRCWorld.interpret_slot_data(bad)

    def test_no_input_mutation_and_json_key_order_independent(self):
        slot=self.world().fill_slot_data(); before=copy.deepcopy(slot)
        slot['generated_star_requirements']=dict(reversed(list(slot['generated_star_requirements'].items())))
        result=self.module.SCRCWorld.interpret_slot_data(slot)
        result['generated_star_requirements']['Level 3']=99
        self.assertEqual(slot['generated_star_requirements'],before['generated_star_requirements'])

    def test_normal_generation_still_uses_seed(self):
        self.assertNotEqual(self.world(1).generated_star_requirements,self.world(900).generated_star_requirements)

if __name__ == '__main__': unittest.main()
