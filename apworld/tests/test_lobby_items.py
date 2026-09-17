import unittest

from support import load_scrc_world, FakeMultiWorld, OptionValue
import random
import types


class LobbyItemWorldTests(unittest.TestCase):
    def setUp(self):
        self.module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)

    def make_world(self):
        world = object.__new__(self.module.SCRCWorld)
        world.player = 1
        world.random = random.Random(913)
        world.multiworld = FakeMultiWorld()
        world.options = types.SimpleNamespace(
            required_stars=OptionValue(50),
            difficulty=OptionValue(0),
            starting_area=OptionValue(0),
        )
        world.generate_early()
        world.create_regions()
        world.create_items()
        return world

    def test_lobby_pickup_ids_are_reserved_without_activating_unproven_sources(self):
        world = self.make_world()
        locations = {
            location.name: location
            for region in world.multiworld.regions
            for location in region.locations
            if location.address is not None
        }
        expected = {
            "Lobby - Important Letters Pickup": 187256292,
            "Lobby - Bean Trumpet Award": 187256293,
        }
        for name, location_id in expected.items():
            self.assertEqual(self.module.LOCATION_NAME_TO_ID[name], location_id)
            self.assertNotIn(name, locations)

        pool = [item.name for item in world.multiworld.itempool]
        self.assertEqual(pool.count("Important Letters"), 0)
        self.assertEqual(pool.count("Bean Trumpet"), 0)
        self.assertEqual(world.create_item("Important Letters").code, 187256156)
        bean = world.create_item("Bean Trumpet")
        self.assertEqual(bean.code, 187256157)
        self.assertEqual(bean.classification, "useful")
        self.assertFalse(world.fill_slot_data()["randomize_lobby_letters_bean_trumpet"])

    def test_certificate_id_is_reserved_but_not_generated_before_source_recovery(self):
        world = self.make_world()
        self.assertIn("Demolition Certificate", world.item_name_to_id)
        self.assertEqual(world.create_item("Demolition Certificate").code, 187256158)
        self.assertEqual(
            [item.name for item in world.multiworld.itempool].count("Demolition Certificate"),
            0,
        )
        self.assertFalse(world.fill_slot_data()["randomize_demolition_certificate"])

    def test_lobby_pickup_sources_stay_out_of_live_region_graph(self):
        world = self.make_world()
        for name in ("Lobby - Important Letters Pickup", "Lobby - Bean Trumpet Award"):
            self.assertIn(name, self.module.LOCATION_NAME_TO_ID)
            self.assertNotIn(name, {
                location.name
                for region in world.multiworld.regions
                for location in region.locations
            })


if __name__ == "__main__":
    unittest.main()
