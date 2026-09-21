import unittest
from collections import Counter
import test_world_integration as integration
State=integration.State

class APStarsTests(unittest.TestCase):
    setUp = integration.WorldIntegrationTests.setUp
    make_world = integration.WorldIntegrationTests.make_world
    build_world = integration.WorldIntegrationTests.build_world
    def test_ap_star_pool_contract_and_safe_capacity(self):
        for difficulty in range(4):
            world = self.build_world(difficulty=difficulty)
            world.create_items()
            data = world.fill_slot_data()
            self.assertEqual(data['schema_version'], 19)
            self.assertTrue(data['star_items_active'])
            self.assertEqual(Counter(i.name for i in world.multiworld.itempool)['Star'], 66)
            locations=[l for r in world.multiworld.regions for l in r.locations if l.address is not None]
            self.assertGreaterEqual(sum(l.item_rule(world.create_item('Star')) for l in locations),134)
            self.assertEqual(data['star_victory_schema'],1)
            self.assertEqual(data['star_item_id'],187256118)
            self.assertEqual(data['victory_level_internal_id'],'Level_28')

    def test_zero_star_opening_and_nonretroactive_goal_rule(self):
        for goal in (1,25,50,66):
            world=self.make_world(required_stars=goal)
            world.generate_early(); world.create_regions(); world.set_rules()
            self.assertEqual(world.generated_star_requirements['Level 1'],0)
            self.assertEqual(world.generated_star_requirements['Level 2'],0)
            self.assertTrue(all(v<goal for v in world.generated_star_requirements.values()))
            victory=world.multiworld.get_location('Victory',1)
            state=State(['Royal Corridor Access', 'Plant Pipes']+['Star']*(goal-1))
            self.assertFalse(victory.access_rule(state))
            state.collect('Star')
            self.assertTrue(victory.access_rule(state))
            self.assertEqual(victory.parent_region.name,'Royal Corridor')
            # The event represents a subsequent repeatable clear, not a saved early-clear bit.
            self.assertFalse(world.multiworld.completion_condition[1](state))

    def test_routing_note_matches_active_star_contract(self):
        data = self.build_world().fill_slot_data()
        note = data['routing_logic_note']
        self.assertTrue(data['client_star_gate_enforcement_active'])
        self.assertTrue(data['post_threshold_victory_active'])
        self.assertIn('generated Star gates are active', note)
        self.assertIn('Plant Pipes', note)
        self.assertIn('subsequent Level 22 clear', note)
        self.assertNotIn('inactive previews', note)

    def test_level_22_requires_plant_pipes_for_all_rewards_and_victory(self):
        for difficulty in range(4):
            world = self.build_world(difficulty=difficulty)
            world.set_rules()
            owned = [name for name in self.module.ITEM_NAME_TO_ID
                     if name not in ('Star', 'Plant Pipes')]
            state = State(owned + ['Star'] * 66)
            names = [name for name in world.active_location_names
                     if name.startswith('Level 22 - ')]
            names += ['Victory', 'Royal Corridor - Bunker Keycard Award',
                      'Royal Corridor - King Ferdinand Unlocked',
                      'Cassette Source - Another Day In Paradise']
            for name in names:
                with self.subTest(difficulty=difficulty, location=name):
                    rule = world.multiworld.get_location(name, 1).access_rule
                    self.assertFalse(rule(state), 'Plant Pipes must not be placed behind this route')
            state.collect('Plant Pipes')
            for name in names:
                with self.subTest(difficulty=difficulty, location=name):
                    self.assertTrue(world.multiworld.get_location(name, 1).access_rule(state))

    def test_campaign_gate_boundaries_and_partial_plant_pipes_source(self):
        world=self.build_world()
        owned=[name for name in self.module.ITEM_NAME_TO_ID if name!='Star']
        for number in range(1,23):
            location=world.multiworld.get_location(f'Level {number} - Completion',1)
            gate=world.generated_star_requirements[f'Level {number}']
            self.assertTrue(location.access_rule(State(owned+['Star']*gate)),number)
            if gate:
                self.assertFalse(location.access_rule(State(owned+['Star']*(gate-1))),number)
        source=world.multiworld.get_location(self.module.ROOTS_LEVEL3_FROG_HIPPO,1)
        self.assertTrue(source.access_rule(State(['Roots Access','Weed Killer']+['Star']*66)))
        self.assertFalse(source.access_rule(State(['Roots Access','Plant Pipes']+['Star']*66)))

    def test_in_level_combo_source_cannot_bypass_level_five_gate(self):
        world=self.build_world()
        world.generated_star_requirements.update({'Level 4': 3,'Level 5': 6})
        owned=[name for name in self.module.ITEM_NAME_TO_ID if name!='Star']
        source=world.multiworld.get_location('Roots - Combo Bucket Conversion',1)
        self.assertFalse(source.access_rule(State(owned+['Star']*5)))
        self.assertTrue(source.access_rule(State(owned+['Star']*6)))

    def test_capacity_guard_counts_useful_rewards_that_cannot_use_filler_only_slots(self):
        world=self.build_world()
        locations=[l for r in world.multiworld.regions for l in r.locations if l.address is not None]
        for index,location in enumerate(locations):
            location.item_rule=lambda item,index=index: index<136 or item.name=='Stardust'
        with self.assertRaisesRegex(ValueError,'137.*136'):
            world.create_items()

if __name__=='__main__': unittest.main()
