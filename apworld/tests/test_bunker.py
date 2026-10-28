import unittest
import test_world_integration as legacy
from support import OptionValue

class BunkerTests(unittest.TestCase):
    setUp = legacy.WorldIntegrationTests.setUp
    make_world = legacy.WorldIntegrationTests.make_world
    reachable_regions = staticmethod(legacy.WorldIntegrationTests.reachable_regions)
    location_is_reachable = staticmethod(legacy.WorldIntegrationTests.location_is_reachable)
    def build(self, goal=0, stars=40, enabled=True):
        w=self.make_world(required_stars=stars)
        w.options.bunker_progression=OptionValue(int(enabled))
        w.options.goal=OptionValue(goal)
        w.generate_early(); w.create_regions(); w.set_rules()
        return w
    def reachable(self,w,name,items):
        return self.location_is_reachable(w,w.multiworld.get_location(name,1),legacy.State(items))
    def test_keycard_opens_bunker_without_boss(self):
        w=self.build(); items=['Lobby Access','Bunker Keycard']+['Star']*40
        self.assertTrue(self.reachable(w,'Secret Bunker - Gecko Interaction',items))
        self.assertFalse(self.reachable(w,'Secret Bunker - Gecko Interaction',[x for x in items if x!='Bunker Keycard']))
    def test_randomized_items_and_checks_exist_once(self):
        w=self.build(); w.create_items()
        for n in ['Bunker Keycard','Demon Key','Virus','Cheat Codes','Game Ender']:
            self.assertEqual(1,sum(i.name==n for i in w.multiworld.itempool))
        for n in ['Lift Quest - Virus Retrieved','School Trip - Cheat Codes Retrieved','Meat Dimension - Game Ender Created','Central Mainframe - Game Ender Used','Secret Bunker - Demon Key Pickup','True Ending - Completion']:
            self.assertIsNotNone(w.multiworld.get_location(n,1).address)
    def test_assembly_requires_inputs_not_output(self):
        w=self.build(); items=['Lobby Access','Bunker Keycard','Meat Dimension Access','Virus','Cheat Codes']+['Star']*40
        self.assertTrue(self.reachable(w,'Meat Dimension - Game Ender Created',items))
        for missing in ['Virus','Cheat Codes']:
            self.assertFalse(self.reachable(w,'Meat Dimension - Game Ender Created',[x for x in items if x!=missing]+['Game Ender']))
    def test_demon_key_gates_devil_not_true_ending(self):
        w=self.build(goal=1); all_items=list(self.module.ITEM_NAME_TO_ID)+['Star']*66
        no_key=[x for x in all_items if x!='Demon Key']
        self.assertFalse(self.reachable(w,'Demonic Room - Completion',no_key))
        self.assertTrue(self.reachable(w,'Victory',no_key))
        self.assertFalse(self.reachable(w,'Victory',[x for x in no_key if x!='Game Ender']))
        self.assertTrue(self.reachable(w,'Secret Bunker - Demon Key Pickup',no_key))
    def test_star_gate_strict_and_legacy_unchanged(self):
        for stars in (2,3,40,66):
            w=self.build(stars=stars); data=w.fill_slot_data()
            self.assertLess(data['star_eater_requirements']['Secret Bunker'],data['generated_star_requirements']['Level 22'])
        with self.assertRaises(ValueError) as invalid: self.build(stars=1)
        self.assertEqual("OptionError",type(invalid.exception).__name__)
        w=self.build(stars=1,enabled=False)
        self.assertEqual(66,w.fill_slot_data()['star_eater_requirements']['Secret Bunker'])
    def test_contract_and_tracker_roundtrip(self):
        w=self.build(goal=1); d=w.fill_slot_data()
        self.assertEqual(21,d['schema_version'])
        self.assertEqual('true_ending',d['goal'])
        restored=self.module.SCRCWorld.interpret_slot_data(d)
        self.assertTrue(restored['bunker_progression'])
        self.assertEqual('true_ending',restored['goal'])
        bad=dict(d,goal='nonsense')
        with self.assertRaises(ValueError): self.module.SCRCWorld.interpret_slot_data(bad)
    def test_bunker_sources_can_hold_foreign_progression(self):
        w=self.build(); item=w.create_item('Star'); item.player=2
        for n in ['Secret Bunker - Star Eater Fed','Cartridge Pickup - Superstar','Demonic Room - Completion']:
            self.assertTrue(w.multiworld.get_location(n,1).item_rule(item))
    def test_true_goal_requires_enabled_bunker(self):
        with self.assertRaises(ValueError) as invalid: self.build(goal=1,enabled=False)
        self.assertEqual("OptionError",type(invalid.exception).__name__)

    def test_tracker_replays_goal_items_and_old_location_set(self):
        for active in (False,True):
            w=self.build(goal=int(active),enabled=active)
            data=w.fill_slot_data()
            replay=self.make_world(seed=999)
            replay.options.bunker_progression=OptionValue(int(not active))
            replay.options.goal=OptionValue(0)
            replay.multiworld.re_gen_passthrough={self.module.GAME_NAME:data}
            replay.generate_early(); replay.create_regions(); replay.set_rules(); replay.create_items()
            self.assertEqual(w.goal,replay.goal)
            self.assertEqual(w.bunker_progression,replay.bunker_progression)
            self.assertEqual(w.generated_star_requirements,replay.generated_star_requirements)
            self.assertEqual(w.active_location_names,replay.active_location_names)
            self.assertEqual(int(active),sum(i.name=='Demon Key' for i in replay.multiworld.itempool))
    def test_tracker_rejects_tampered_bunker_contract(self):
        from copy import deepcopy
        d=self.build(goal=1).fill_slot_data()
        for key,value in [('bunker_progression',False),('schema_version',19),('true_ending_native_flag','OTHER'),('bunker_item_ids',{}),('bunker_check_ids',{})]:
            bad=deepcopy(d); bad[key]=value
            with self.subTest(key=key),self.assertRaises(ValueError): self.module.SCRCWorld.interpret_slot_data(bad)
        bad=deepcopy(d);bad['star_eater_requirements']['Secret Bunker']=66
        with self.assertRaises(ValueError):self.module.SCRCWorld.interpret_slot_data(bad)
    def test_legacy_without_new_metadata_keeps_old_rules(self):
        w=self.build(enabled=False);d=w.fill_slot_data()
        for key in ('bunker_progression','goal','bunker_item_ids','bunker_check_ids','true_ending_native_flag'):d.pop(key)
        restored=self.module.SCRCWorld.interpret_slot_data(d)
        self.assertFalse(restored['bunker_progression'])
        self.assertEqual('king_ferdinand',restored['goal'])
    def test_received_game_ender_bypasses_upstream_gecko_sources(self):
        w=self.build(goal=1)
        items=['Lobby Access','Bunker Keycard','Cell Tower Access','Plant Pipes','Royal Corridor Access','Game Ender']+['Star']*40
        self.assertTrue(self.reachable(w,'Victory',items))
        self.assertFalse(self.reachable(w,'Meat Dimension - Game Ender Created',items))
        self.assertTrue(self.reachable(w,'Victory',[x for x in items if x!='Game Ender']) is False)
if __name__=='__main__': unittest.main()

