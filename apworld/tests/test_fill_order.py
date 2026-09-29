import unittest
from types import SimpleNamespace
from support import load_scrc_world


class FillOrderTests(unittest.TestCase):
    def test_counted_stars_are_placed_before_access_items_without_moving_other_players(self):
        module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)
        World = module.SCRCWorld
        owner = SimpleNamespace(player=1)
        other = SimpleNamespace(player=2, name="Star")
        star1 = SimpleNamespace(player=1, name="Star")
        star2 = SimpleNamespace(player=1, name="Star")
        access = SimpleNamespace(player=1, name="Lobby Access")
        pipes = SimpleNamespace(player=1, name="Plant Pipes")
        points = SimpleNamespace(player=1, name="Music Lab Point")
        cassette = SimpleNamespace(player=1, name="Zen Cassette")
        hand_in = SimpleNamespace(player=1, name="Old Game Data")
        pool = [star1, other, points, access, cassette, star2, pipes, hand_in]
        World.fill_hook(owner, pool, [], [], [])
        self.assertEqual(pool, [access, other, pipes, cassette, hand_in, points, star1, star2])
        self.assertEqual(len({id(item) for item in pool}), 8)

    def test_filler_predicate_rejects_combined_important_flags(self):
        from enum import IntFlag
        from unittest.mock import patch
        module, cleanup = load_scrc_world()
        self.addCleanup(cleanup)
        placement = __import__(module.__package__ + ".placement", fromlist=["filler_item_allowed"])
        class Classification(IntFlag):
            filler = 0
            progression = 1
            useful = 2
            trap = 4
            skip_balancing = 8
        with patch.object(placement, "ItemClassification", Classification):
            for value in (0, 4):
                self.assertTrue(placement.filler_item_allowed(SimpleNamespace(classification=Classification(value))))
            for value in (1, 2, 3, 5, 6, 9, 10):
                self.assertFalse(placement.filler_item_allowed(SimpleNamespace(classification=Classification(value))))
