import unittest
import test_character_quest_items as character_tests
from test_world_integration import State


class QuestChecksWorldTests(unittest.TestCase):
    setUp = character_tests.CharacterQuestItemWorldTests.setUp
    make_world = character_tests.CharacterQuestItemWorldTests.make_world

    def test_new_pool_contains_three_rewards(self):
        for difficulty, count in enumerate((164, 222, 280, 316)):
            world = self.make_world(difficulty)
            names = [item.name for item in world.multiworld.itempool]
            self.assertEqual(len(names), count)
            for name in ('Plunger', 'Meoo', 'Maniac'):
                self.assertEqual(names.count(name), 1)
            self.assertEqual(names.count('Stardust'), count - 137)

    def test_quest_inputs_are_progression_and_characters_are_useful(self):
        world = self.make_world()
        for name in ('Old Game Data', 'Car Battery', 'Plunger'):
            self.assertEqual(world.create_item(name).classification, 'progression')
        for name in ('Meoo', 'Maniac'):
            self.assertEqual(world.create_item(name).classification, 'useful')

    def test_schema18_preserves_character_inputs_and_exports_quest_contract(self):
        data = self.make_world().fill_slot_data()
        self.assertEqual(data['schema_version'], 19)
        self.assertEqual(data.get('quest_checks_schema'), 1)
        self.assertTrue(data['implementation_version'].endswith('-quest-checks-0.26-check-expansion-0.27-ap-stars-0.28'))
        self.assertEqual(set(data.get('quest_items', {})), {'Plunger', 'Meoo', 'Maniac'})
        self.assertEqual(set(data.get('quest_locations', {})), {
            'Lobby - Plunger Pickup', 'Lobby - Car Battery Hand-In',
            'Game Garage - Old Game Data Hand-In', 'Roots - Star Eater Fed'})

    def test_hand_in_checks_require_inputs_but_never_character_rewards(self):
        world = self.make_world()
        for name, required, region in (
            ('Lobby - Car Battery Hand-In', 'Car Battery', 'Lobby'),
            ('Game Garage - Old Game Data Hand-In', 'Old Game Data', 'Game Garage'),
        ):
            self.assertIn(name, world.location_name_to_id)
            location = world.multiworld.get_location(name, 1)
            self.assertEqual(location.parent_region.name, region)
            self.assertFalse(location.access_rule(State([])))
            self.assertTrue(location.access_rule(State([required, 'Lobby Access'])))
            self.assertTrue(location.item_rule(world.create_item('Plant Pipes')))

    def test_star_eater_is_progression_safe_under_ap_star_contract(self):
        world = self.make_world()
        self.assertIn('Roots - Star Eater Fed', world.location_name_to_id)
        source = world.multiworld.get_location('Roots - Star Eater Fed', 1)
        self.assertEqual(source.parent_region.name, 'Roots')
        for name in ('Plant Pipes', 'Old Game Data', 'Car Battery', 'Meoo', 'Maniac'):
            self.assertTrue(source.item_rule(world.create_item(name)))
        self.assertTrue(source.item_rule(world.create_item('Stardust')))

    def test_plunger_source_requires_conservative_lobby_route_but_not_its_reward(self):
        world = self.make_world()
        self.assertIn('Lobby - Plunger Pickup', world.location_name_to_id)
        source = world.multiworld.get_location('Lobby - Plunger Pickup', 1)
        self.assertEqual(source.parent_region.name, 'Lobby')
        self.assertFalse(source.access_rule(State([])))
        self.assertFalse(source.access_rule(State(['Lobby Access'])))
        owned=[name for name in world.item_name_to_id if name!='Plunger']+['Star']*66
        self.assertTrue(source.access_rule(State(owned)))
        self.assertTrue(source.item_rule(world.create_item('Stardust')))
        self.assertTrue(source.item_rule(world.create_item('Old Game Data')))
        self.assertTrue(source.item_rule(world.create_item('Plunger')))

    def test_new_wire_ids_are_exact_and_do_not_reuse_lobby_reservations(self):
        world = self.make_world()
        data = world.fill_slot_data()
        self.assertEqual(data.get('quest_items'), {
            'Plunger': 187256161, 'Meoo': 187256162, 'Maniac': 187256163})
        self.assertEqual(data.get('quest_locations'), {
            'Lobby - Plunger Pickup': 187256294,
            'Lobby - Car Battery Hand-In': 187256295,
            'Game Garage - Old Game Data Hand-In': 187256296,
            'Roots - Star Eater Fed': 187256297})
        for name, item_id in data['quest_items'].items():
            self.assertEqual(world.create_item(name).code, item_id)
        for name, location_id in data['quest_locations'].items():
            self.assertEqual(world.multiworld.get_location(name, 1).address, location_id)
