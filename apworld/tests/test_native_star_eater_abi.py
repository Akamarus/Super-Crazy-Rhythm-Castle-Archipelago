"""Guard the native ABI: all arguments are pointers, so C# cannot detect swaps.

The installed Il2CppInterop.Runtime metadata declares obj, field, value.
Both September 18 crash dumps stop in the reversed SetReference P/Invoke.
"""
from pathlib import Path
import re
import unittest

SOURCE = Path(__file__).resolve().parents[2] / "client" / "ApStarEaterThresholds.cs"

class NativeStarEaterAbiTests(unittest.TestCase):
    def test_native_write_uses_object_then_field_for_apply_and_restore(self):
        source = SOURCE.read_text(encoding="utf-8-sig")
        calls = re.findall(r"il2cpp_field_set_value_object\(([^;]+)\);", source)
        self.assertEqual(calls, [
            "component.Pointer, field, newPointer",
            "component.Pointer, field, original.Pointer",
            "replacement.Component.Pointer, replacement.Field, original",
        ], "il2cpp_field_set_value requires object, field, value, with a direct object pointer, never a pointer-to-pointer")

if __name__ == "__main__":
    unittest.main()
