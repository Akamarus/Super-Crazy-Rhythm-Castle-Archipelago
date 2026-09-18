import unittest
import test_character_quest_items as character_tests
from test_world_integration import State

class ExpandedChecksTests(unittest.TestCase):
    setUp = character_tests.CharacterQuestItemWorldTests.setUp
    make_world = character_tests.CharacterQuestItemWorldTests.make_world

    def test_exact_catalog_counts_and_capacity(self):
        for difficulty, expected, safe in ((0,164,152),(1,222,209),(2,280,266),(3,316,302)):
            world=self.make_world(difficulty)
            data=world.fill_slot_data()
            self.assertEqual(data['schema_version'],19)
            self.assertEqual(data['expanded_checks_schema'],1)
            self.assertEqual(len(data['expanded_check_locations']),39)
            self.assertEqual(set(data['expanded_check_locations'].values()),{187256292,*range(187256298,187256336)})
            locations=[l for r in world.multiworld.regions for l in r.locations if l.address is not None]
            self.assertEqual(len(locations),expected)
            self.assertEqual(len({l.address for l in locations}),expected)
            self.assertEqual(sum(l.item_rule(world.create_item('Star')) for l in locations),safe)
            self.assertEqual(sum(i.name!='Stardust' for i in world.multiworld.itempool),137)
            self.assertTrue(data['star_items_active'])
            self.assertFalse(data['randomize_lobby_letters_bean_trumpet'])

    def test_verified_native_sources_have_item_and_prior_level_dependencies(self):
        world=self.make_world()
        owned=[name for name in world.item_name_to_id]+['Star']*66
        cases={
            'Roots - Combo Bucket Conversion': ('Weed Killer','Plant Pipes','Hip Glasses','Chicken Bucket'),
            'Lobby - Important Letters Delivery': ('Roots Access','Lobby Access','Chicken Bucket'),
            'Lobby - Plunger Hand-In': ('Lobby Access','Plunger'),
            'Meat Dimension - Return Scruffy': ('Meat Dimension Access','Hypno Pan'),
            'Cell Tower - Deliver Super Nectar': ('Cell Tower Access','Roots Access','Meat Dimension Access'),
            'Tower of Fear - Restore Eye Statue': ('Tower of Fear Access','Plant Pipes','Violance'),
            'Tower of Fear - Restore Mind Statue': ('Tower of Fear Access','Hypno Pan','Violance'),
            'Tower of Fear - Restore Heart Statue': ('Tower of Fear Access','Plant Pipes','Hypno Pan','Violance'),
            'Royal Corridor - Star Eater Fed': ('Royal Corridor Access','Tower of Fear Access','Violance','Weed Killer','Plant Pipes'),
            'Royal Corridor - King Ferdinand Unlocked': ('Royal Corridor Access',),
        }
        for name,required in cases.items():
            location=world.multiworld.get_location(name,1)
            self.assertTrue(location.item_rule(world.create_item('Star')),name)
            self.assertTrue(location.access_rule(State(owned)),name)
            for missing in required:
                self.assertFalse(location.access_rule(State(i for i in owned if i!=missing)),(name,missing))
        for name in ('Demonic Room - Completion','Nectar Party - Completion',
                     'Secret Bunker - Gecko Interaction','Lobby - Demolition Certificate Award'):
            self.assertFalse(world.multiworld.get_location(name,1).item_rule(world.create_item('Star')),name)

    def test_special_results_are_supplemental_not_normal_stars(self):
        world=self.make_world()
        data=world.fill_slot_data()
        self.assertTrue(data['expanded_special_completions_active'])
        self.assertFalse(data['special_variant_locations_active'])
        self.assertEqual(data['special_variant_locations'],[])
        specials=[entry for entry in self.module.EXPANDED_CHECKS if entry.variant]
        self.assertEqual(len(specials),6)
        self.assertEqual(len({(e.level,e.variant) for e in specials}),6)
        for entry in specials:
            self.assertNotIn(entry.name,data['active_campaign_locations'])
            self.assertTrue(entry.name.endswith(' - Completion'))

if __name__=='__main__': unittest.main()
