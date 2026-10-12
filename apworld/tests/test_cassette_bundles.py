import copy
import types
import unittest
from collections import Counter
import test_world_integration as integration
from support import OptionValue

class CassetteBundleTests(unittest.TestCase):
    setUp = integration.WorldIntegrationTests.setUp
    make_world = integration.WorldIntegrationTests.make_world

    def bundle_world(self, size=2, seed=22, slot=None):
        w = self.make_world(seed=seed)
        w.options.cassette_bundle_size = OptionValue(size)
        if slot is not None:
            w.multiworld.re_gen_passthrough = {w.game: slot}
        w.generate_early(); w.create_regions(); w.set_rules(); w.create_items()
        return w

    def test_permanent_bundle_ids_follow_reserved_legacy_items(self):
        expected={f'Cassette Bundle {i:02}':187256163+i for i in range(1,16)}
        self.assertEqual({n:i for n,i in self.module.ITEM_NAME_TO_ID.items()
                          if n.startswith('Cassette Bundle ')},expected)
        self.assertEqual(self.module.ITEM_NAME_TO_ID['Maniac'],187256163)
        self.assertEqual(len(set(self.module.ITEM_NAME_TO_ID.values())),len(self.module.ITEM_NAME_TO_ID))

    def test_create_items_honors_excluded_capacity(self):
        w=self.make_world()
        w.generate_early();w.create_regions()
        eligible=[l.name for r in w.multiworld.regions for l in r.locations
                  if l.address is not None and l.item_rule(w.create_item('Star'))]
        w.options.exclude_locations=OptionValue(set(eligible[:18]))
        w.create_items()
        pool=[i.name for i in w.multiworld.itempool if i.name!='Stardust']
        self.assertLessEqual(len(pool),len(eligible)-18)
        self.assertEqual(len(w.multiworld.itempool),164)
        self.assertEqual(Counter(pool)['Star'],66)

    def test_modes_replace_pool_but_preserve_all_checks(self):
        for size in (1,2,3):
            w = self.bundle_world(size)
            slot = w.fill_slot_data()
            self.assertEqual(slot.get('cassette_bundle_size'),size)
            groups = slot['cassette_bundles']
            self.assertEqual(len(groups),0 if size == 1 else 30//size)
            pool = Counter(i.name for i in w.multiworld.itempool)
            self.assertEqual(sum(pool.values()),164)
            self.assertEqual(pool['Star'],66)
            names = {c.item_name for c in self.catalog.CASSETTES}
            if size > 1:
                self.assertEqual(Counter(n for g in groups.values() for n in g),Counter(names))
                self.assertTrue(all(len(g)==size for g in groups.values()))
                self.assertTrue(all(pool[n]==1 for n in groups))
                self.assertTrue(all(pool[n]==0 for n in names))
                self.assertEqual(slot['schema_version'],20)
            else:
                self.assertTrue(all(pool[n]==1 for n in names))
                self.assertEqual(slot['schema_version'],19)

    def test_seed_groups_deterministic_and_different(self):
        a = self.bundle_world(seed=4).fill_slot_data()['cassette_bundles']
        self.assertEqual(a,self.bundle_world(seed=4).fill_slot_data()['cassette_bundles'])
        self.assertNotEqual(a,self.bundle_world(seed=40).fill_slot_data()['cassette_bundles'])

    def test_bundle_and_individual_receipts_unlock_exact_songs(self):
        w=self.bundle_world()
        groups=w.fill_slot_data()['cassette_bundles']
        name, members=next(iter(groups.items()))
        for c in self.catalog.CASSETTES:
            rule=w.multiworld.get_location(f'Music Lab Cassette - {c.display_song} - Bronze',1).access_rule
            self.assertEqual(rule(integration.State([name])),c.item_name in members)
            self.assertTrue(rule(integration.State([c.item_name])))
            self.assertFalse(rule(integration.State([])))

    def test_tracker_preserves_groups_despite_rng_and_option(self):
        source=self.bundle_world(3,4).fill_slot_data()
        tracker=self.bundle_world(1,98,self.module.SCRCWorld.interpret_slot_data(source))
        self.assertEqual(tracker.fill_slot_data()['cassette_bundles'],source['cassette_bundles'])
        self.assertEqual(tracker.fill_slot_data()['cassette_bundle_size'],3)

    def test_invalid_bundle_contract_rejected(self):
        slot=self.bundle_world().fill_slot_data()
        for mutate in (
            lambda d:d.pop('cassette_bundles'),
            lambda d:d.pop('active_campaign_locations'),
            lambda d:d.pop('star_eater_requirements'),
            lambda d:d.update(cassette_bundle_size=4),
            lambda d:d.update(cassette_bundle_size=True),
            lambda d:d['cassette_bundles'].pop('Cassette Bundle 01'),
            lambda d:d['cassette_bundles']['Cassette Bundle 01'].__setitem__(0,'Stardust'),
            lambda d:d['cassette_bundles']['Cassette Bundle 01'].__setitem__(0,d['cassette_bundles']['Cassette Bundle 02'][0]),
            lambda d:d.update(schema_version=19),
        ):
            bad=copy.deepcopy(slot);mutate(bad)
            with self.assertRaises(ValueError): self.module.SCRCWorld.interpret_slot_data(bad)

    def test_legacy_slot_without_bundle_fields_works(self):
        slot=self.bundle_world(1).fill_slot_data()
        slot.pop('cassette_bundle_size');slot.pop('cassette_bundles')
        w=self.bundle_world(3,98,self.module.SCRCWorld.interpret_slot_data(slot))
        self.assertEqual(w.fill_slot_data()['cassette_bundles'],{})
        self.assertEqual(w.fill_slot_data()['cassette_bundle_size'],1)

if __name__=='__main__':unittest.main()
