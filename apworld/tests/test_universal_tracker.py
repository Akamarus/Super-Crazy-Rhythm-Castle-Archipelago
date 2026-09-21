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

    def test_no_input_mutation_and_json_key_order_independent(self):
        slot=self.world().fill_slot_data(); before=copy.deepcopy(slot)
        slot['generated_star_requirements']=dict(reversed(list(slot['generated_star_requirements'].items())))
        result=self.module.SCRCWorld.interpret_slot_data(slot)
        result['generated_star_requirements']['Level 3']=99
        self.assertEqual(slot['generated_star_requirements'],before['generated_star_requirements'])

    def test_normal_generation_still_uses_seed(self):
        self.assertNotEqual(self.world(1).generated_star_requirements,self.world(900).generated_star_requirements)

if __name__ == '__main__': unittest.main()
