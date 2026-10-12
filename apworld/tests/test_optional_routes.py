import unittest
import test_world_integration as integration
State = integration.State


class OptionalRouteTests(unittest.TestCase):
    setUp = integration.WorldIntegrationTests.setUp
    make_world = integration.WorldIntegrationTests.make_world
    build_world = integration.WorldIntegrationTests.build_world

    def test_reported_checks_are_not_available_with_only_roots(self):
        world = self.build_world()
        for name in ("Cartridge Pickup - Smooch", "Cartridge Pickup - Superstar",
                     "Cartridge Pickup - Wag the Dog", "Nectar Party - Completion"):
            self.assertFalse(world.multiworld.get_location(name, 1).access_rule(State(["Roots Access"])), name)

    def test_optional_sources_require_their_native_routes_and_remain_filler_only(self):
        world = self.build_world()
        owned = [n for n in world.item_name_to_id if n != "Star"] + ["Star"] * 66
        cases = {
            "Cartridge Pickup - Smooch": ("Meat Dimension Access",),
            "Cartridge Pickup - Superstar": ("Lobby Access", "Royal Corridor Access", "Plant Pipes"),
            "Cartridge Pickup - Wag the Dog": ("Lobby Access", "Royal Corridor Access", "Tower of Fear Access", "Weed Killer", "Violance", "Hypno Pan"),
            "Nectar Party - Completion": ("Cell Tower Access", "Roots Access", "Hypno Pan"),
            "Act 1B: Nectar - Completion": ("Cell Tower Access", "Meat Dimension Access", "Hypno Pan"),
            "Demonic Room - Completion": ("Lobby Access", "Royal Corridor Access"),
            "Demonic Tower - Completion": ("Lobby Access", "Royal Corridor Access", "Weed Killer"),
            "Demonic Escape - Completion": ("Tower of Fear Access", "Royal Corridor Access", "Violance"),
            "Demonic Lockers - Completion": ("Tower of Fear Access", "Royal Corridor Access", "Weed Killer", "Plant Pipes"),
            "Secret Bunker - Gecko Interaction": ("Lobby Access", "Royal Corridor Access"),
            "Secret Bunker - Star Eater Fed": ("Lobby Access", "Royal Corridor Access"),
            "Lobby - Demolition Certificate Award": ("Lobby Access",),
        }
        for name, required in cases.items():
            loc = world.multiworld.get_location(name, 1)
            self.assertTrue(loc.access_rule(State(owned)), name)
            for item in required:
                self.assertFalse(loc.access_rule(State(n for n in owned if n != item)), (name, item))
            self.assertFalse(loc.item_rule(world.create_item("Star")), name)
            self.assertFalse(loc.item_rule(world.create_item("Meoo")), name)
            self.assertTrue(loc.item_rule(world.create_item("Stardust")), name)

    def test_bunker_star_reward_requires_all_66_stars_independent_of_goal(self):
        world = self.build_world()
        owned = [n for n in world.item_name_to_id if n != "Star"]
        for name in ("Cartridge Pickup - Superstar", "Secret Bunker - Star Eater Fed"):
            rule = world.multiworld.get_location(name, 1).access_rule
            self.assertFalse(rule(State(owned + ["Star"] * 65)), name)
            self.assertTrue(rule(State(owned + ["Star"] * 66)), name)

    def test_smooch_is_available_before_act_one(self):
        world = self.build_world()
        world.generated_star_requirements["Level 11"] = 12
        rule = world.multiworld.get_location("Cartridge Pickup - Smooch", 1).access_rule
        self.assertTrue(rule(State(["Meat Dimension Access"])))
        self.assertFalse(rule(State(["Star"] * 66)))
        self.assertTrue(rule(State(["Meat Dimension Access"] + ["Star"] * 12)))

    def test_devil_cassette_alias_dispatches_its_own_route(self):
        world = self.build_world()
        owned = [n for n in world.item_name_to_id if n != "Star"] + ["Star"] * 40
        for level, completion, song in (
            (19, "Demonic Escape - Completion", "Fumblin Around"),
            (21, "Demonic Lockers - Completion", "Keep On Hustlin"),
        ):
            with self.subTest(song=song):
                # Isolate dispatch with a synthetic gate map. Valid generated gates
                # are monotonic: Bunker entry currently shadows these alternatives.
                world.generated_star_requirements = dict.fromkeys(world.generated_star_requirements, 0)
                world.generated_star_requirements[f"Level {level}"] = 41
                self.assertTrue(world.multiworld.get_location(completion, 1).access_rule(State(owned)))
                source = next(e.source_name for e in self.module.CASSETTES if e.display_song == song)
                self.assertTrue(world.multiworld.get_location(source, 1).access_rule(State(owned)))
                blocked = [n for n in owned if n != "Royal Corridor Access"]
                self.assertFalse(world.multiworld.get_location(source, 1).access_rule(State(blocked)))
