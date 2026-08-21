from importlib.util import module_from_spec, spec_from_file_location
from pathlib import Path


SCRC_DIR = Path(__file__).resolve().parents[1] / "scrc"


def load_scrc_module(module_name: str):
    path = SCRC_DIR / f"{module_name}.py"
    spec = spec_from_file_location(f"scrc_test_{module_name}", path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"cannot load {path}")
    module = module_from_spec(spec)
    spec.loader.exec_module(module)
    return module
