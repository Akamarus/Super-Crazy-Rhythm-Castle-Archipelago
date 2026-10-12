from collections import Counter
import random
import unittest

from support import load_scrc_module
import test_world_integration as integration


class PoolCapacityTests(unittest.TestCase):
    setUp = integration.WorldIntegrationTests.setUp
    make_world = integration.WorldIntegrationTests.make_world
    build_world = integration.WorldIntegrationTests.build_world

    def fit(self, pool, eligible, excluded=()):
        return load_scrc_module('pool_capacity').fit_non_filler_pool(pool, eligible, excluded)

    def normal_pool(self):
        world = self.build_world()
        world.create_items()
        pool = [i.name for i in world.multiworld.itempool if i.name != 'Stardust']
        eligible = [l.name for r in world.multiworld.regions for l in r.locations
                    if l.address is not None and l.item is None
                    and l.item_rule(world.create_item('Star'))]
        return pool, eligible

    def test_normal_pool_without_exclusions_keeps_exact_order_and_counts(self):
        pool, eligible = self.normal_pool()
        self.assertEqual(self.fit(pool, eligible), pool)

    def test_normal_exclusions_preserve_all_other_items_and_full_point_value(self):
        pool, eligible = self.normal_pool()
        original = list(pool)
        excluded = eligible[:18] + ['Unmodeled source']
        result = self.fit(pool, eligible, excluded)
        self.assertEqual(len(result), 128)
        self.assertEqual(Counter(result)['Star'], 66)
        values = {'Music Lab Point': 1, 'Music Lab Point Bundle': 10,
                  'Music Lab Point Large Bundle': 20}
        self.assertEqual(sum(values.get(n, 0) for n in result), 180)
        self.assertEqual([n for n in result if n not in values],
                         [n for n in pool if n not in values])
        self.assertEqual(pool, original, 'must not mutate caller input')

    def test_only_excluded_eligible_locations_reduce_capacity(self):
        pool, eligible = self.normal_pool()
        result = self.fit(pool, eligible + eligible[:3],
                          eligible[:15] + [eligible[0], 'Unmodeled source'])
        self.assertEqual(result, pool)

    def test_small_shortage_uses_smallest_available_exchange(self):
        pool, eligible = self.normal_pool()
        result = self.fit(pool, eligible, eligible[:16])
        counts = Counter(result)
        self.assertEqual(len(result), 136)
        self.assertEqual(counts['Music Lab Point'], 10)
        self.assertEqual(counts['Music Lab Point Bundle'], 1)
        self.assertEqual(counts['Music Lab Point Large Bundle'], 8)

    def test_maximum_compression_preserves_180_points(self):
        pool, eligible = self.normal_pool()
        result = self.fit(pool, eligible, eligible[:26])
        self.assertEqual(len(result), 126)
        self.assertEqual(Counter(result)['Music Lab Point Large Bundle'], 9)
        self.assertNotIn('Music Lab Point', result)
        self.assertNotIn('Music Lab Point Bundle', result)

    def test_impossible_capacity_fails_explicitly_without_removing_other_items(self):
        pool, eligible = self.normal_pool()
        with self.assertRaisesRegex(ValueError, r'126.*125'):
            self.fit(pool, eligible, eligible[:27])

    def test_cassette_bundle_pool_needs_no_point_compression_for_same_exclusions(self):
        pool, eligible = self.normal_pool()
        cassette_names = {entry.item_name for entry in self.catalog.CASSETTES}
        bundles = load_scrc_module('cassette_bundles')
        for size in (2, 3):
            with self.subTest(size=size):
                bundled_pool = [name for name in pool if name not in cassette_names]
                bundled_pool.extend(bundles.generate_bundles(size, random.Random(12)))
                self.assertEqual(self.fit(bundled_pool, eligible, eligible[:18]), bundled_pool)


if __name__ == '__main__':
    unittest.main()
