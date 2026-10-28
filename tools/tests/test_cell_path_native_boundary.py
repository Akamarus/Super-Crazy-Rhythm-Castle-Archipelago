"""Guard the event bridge implicated by the full final-boss crash dump."""
import unittest
from pathlib import Path

class CellPathNativeBoundaryTests(unittest.TestCase):
    def test_registration_event_never_enters_harmony_value_boxing_bridge(self):
        source = (Path(__file__).resolve().parents[2] / "client/CellTowerPathPatches.cs").read_text()
        self.assertNotIn("harmony.Patch(", source,
                         "Reference-containing native registration event crashes in Harmony's generated value-boxing bridge")
        self.assertIn("INativeDetour.CreateAndApply", source)
        self.assertIn("il2cpp_class_value_size", source)
        self.assertIn("IntPtr evt", source)
        self.assertNotIn("il2cpp_value_box(", source)

if __name__ == "__main__":
    unittest.main()


