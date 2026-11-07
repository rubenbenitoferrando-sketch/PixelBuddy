"""Genera los personajes de tools/chars/ en assets/avatars/<id>/ (necesita Pillow).

Uso:
  python3 tools/make_characters.py                 # todos
  python3 tools/make_characters.py futbolista ...  # solo esos
  python3 tools/make_characters.py --out <carpeta> # en otra carpeta (para vistas previas)
"""
import importlib
import sys
from pathlib import Path

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE / "chars"))
import base  # noqa: E402

ALL = ["programador", "futbolista", "baloncestista", "piloto", "perro", "bombero",
       "estudiante", "tenista"]


def main(argv):
    out = HERE.parent / "assets" / "avatars"
    if "--out" in argv:
        i = argv.index("--out")
        out = Path(argv[i + 1])
        argv = argv[:i] + argv[i + 2:]
    names = argv or [n for n in ALL if (HERE / "chars" / f"{n}.py").exists()]
    for n in names:
        print("✓", base.write(importlib.import_module(n), out))


if __name__ == "__main__":
    main(sys.argv[1:])
