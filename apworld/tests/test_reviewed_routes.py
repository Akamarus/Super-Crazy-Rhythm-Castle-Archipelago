import unittest
import test_world_integration as integration


class ReviewedRouteTests(unittest.TestCase):
    setUp = integration.WorldIntegrationTests.setUp
    make_world = integration.WorldIntegrationTests.make_world
    build_world = integration.WorldIntegrationTests.build_world

    def reachable(self, world, name, items):
        return integration.WorldIntegrationTests.location_is_reachable(
            world, world.multiworld.get_location(name, 1), integration.State(items))

    def test_direct_hub_sources(self):
        world = self.build_world()
        for name, access in (
            ('Lobby - Plunger Pickup', 'Lobby Access'),
            ('Cell Tower - Meet the Bees', 'Cell Tower Access'),
            ('Meat Dimension - Frying Pan Pickup', 'Meat Dimension Access'),
            ('Cartridge Pickup - Smooch', 'Meat Dimension Access'),
        ):
            with self.subTest(name=name):
                self.assertTrue(self.reachable(world, name, [access]))
                self.assertFalse(self.reachable(world, name, []))

    def test_bucket_trade_without_campaign_or_stars(self):
        world = self.build_world()
        for name in ('Roots - Bucket Minion Trade', 'Bucket Minion Trade Complete'):
            self.assertTrue(self.reachable(world, name, ['Roots Access', 'Hip Glasses']))
            self.assertFalse(self.reachable(world, name, ['Roots Access']))

    def test_royal_eater_only_needs_access_and_its_stars(self):
        world = self.build_world()
        gate = world.generated_star_requirements['Level 21']
        name = 'Royal Corridor - Star Eater Fed'
        self.assertTrue(self.reachable(world, name, ['Royal Corridor Access'] + ['Star'] * gate))
        self.assertFalse(self.reachable(world, name, ['Royal Corridor Access'] + ['Star'] * (gate-1)))
        self.assertFalse(self.reachable(world, name, ['Star'] * gate))

    def test_certificate_uses_boring_room_not_demolition(self):
        world = self.build_world()
        world.generated_star_requirements['Level 6'] = 4
        world.generated_star_requirements['Level 7'] = 50
        self.assertTrue(self.reachable(world, 'Lobby - Demolition Certificate Award', ['Lobby Access'] + ['Star']*4))

    def test_bee_routes_without_mainframe_or_pipes(self):
        world = self.build_world()
        for name, access in (('Nectar Party - Completion', 'Roots Access'), ('Act 1B: Nectar - Completion', 'Meat Dimension Access')):
            items = [access, 'Cell Tower Access', 'Hypno Pan'] + ['Star']*40
            self.assertTrue(self.reachable(world, name, items), name)
            self.assertFalse(self.reachable(world, name, [i for i in items if i != 'Cell Tower Access']))
            self.assertFalse(self.reachable(world, name, [i for i in items if i != 'Hypno Pan']))

    def test_fish_tears_return_route_still_needs_award(self):
        world = self.build_world()
        items = ['Lobby Access', 'Cell Tower Access', 'Meat Dimension Access', 'Hypno Pan'] + ['Star']*40
        name = 'Lobby - Fish Tears Delivery'
        world.generated_star_requirements['Level 9'] = 60
        self.assertTrue(self.reachable(world, name, items))
        self.assertFalse(self.reachable(world, name, [i for i in items if i != 'Meat Dimension Access']))
        self.assertFalse(self.reachable(world, name, [i for i in items if i != 'Cell Tower Access']))

    def test_lift_quest_lobby_entrance_preserves_own_star_gate(self):
        world = self.build_world()
        gate = world.generated_star_requirements['Level 5']
        for name in ('Level 5 - Completion', 'Cassette Source - Badass', 'Cassette Source - Heavy Metal'):
            items = ['Roots Access', 'Lobby Access'] + ['Star']*gate
            self.assertTrue(self.reachable(world, name, items), name)
            self.assertFalse(self.reachable(world, name, items[:-1]))
            self.assertFalse(self.reachable(world, name, [i for i in items if i != 'Lobby Access']))

    def test_loneliness_keeps_hypno_pan(self):
        world = self.build_world()
        items = ['Tower of Fear Access', 'Violance'] + ['Star']*40
        for name in ('Level 20 - Completion', 'Cassette Source - Party Non Stop'):
            self.assertFalse(self.reachable(world, name, items))
            self.assertTrue(self.reachable(world, name, items + ['Hypno Pan']))

    def test_lobby_bypass_does_not_create_combo_bucket_without_inputs(self):
        world = self.build_world()
        items = ['Roots Access', 'Lobby Access'] + ['Star']*40
        for name in ('Combo Bucket Event', 'Roots - Combo Bucket Conversion'):
            self.assertFalse(self.reachable(world, name, items), name)
            self.assertFalse(self.reachable(world, name, items + ['Hip Glasses']), name)
            self.assertTrue(self.reachable(world, name, items + ['Hip Glasses', 'Chicken Bucket']), name)
