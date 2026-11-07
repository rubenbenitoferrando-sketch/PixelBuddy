"""Genera docs/characters.gif: todos los personajes en todos los estados, animados.
Necesita Pillow. Uso: python3 tools/make_readme_gif.py
"""
import json
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parent.parent
AV = ROOT / "assets" / "avatars"
STATES = ["run", "katana", "ask", "alert", "train", "sleep"]
LABELS = ["working", "done", "question", "error", "waiting", "sleeping"]
Z, CW, BG = 3, 56, (232, 236, 242)


def frames(d, man, variant, st):
    info = man["states"][st]
    fw, ax = info["frameSize"][0], info["anchorX"]
    sheet = Image.open(d / f"{man['prefix']}_{variant}_{st}.png").convert("RGBA")
    out = []
    for i in range(sheet.width // fw):
        c = Image.new("RGBA", (CW, 32), (0, 0, 0, 0))
        c.alpha_composite(sheet.crop((i * fw, 0, i * fw + fw, 32)), (32 - ax, 0))
        out.append(c.resize((CW * Z, 32 * Z), Image.NEAREST))
    return out, info


def main():
    rows = []
    order = ["ninja", "astronauta", "programador", "futbolista", "baloncestista", "piloto",
             "perro", "bombero", "estudiante", "tenista"]
    dirs = [AV / i for i in order if (AV / i / "manifest.json").exists()]
    dirs += [p for p in sorted(AV.iterdir()) if (p / "manifest.json").exists() and p not in dirs]
    for r, d in enumerate(dirs):
        man = json.loads((d / "manifest.json").read_text())
        variant = man["variants"][r % len(man["variants"])]   # un color distinto por fila
        rows.append([frames(d, man, variant, st) for st in STATES])
    h = len(rows) * 32 * Z + 28
    seq = []
    for t in range(72):
        im = Image.new("RGB", (len(STATES) * CW * Z, h), BG)
        dr = ImageDraw.Draw(im)
        for c, lab in enumerate(LABELS):
            dr.text((c * CW * Z + 80, h - 20), lab, fill=(40, 40, 60))
        for r, row in enumerate(rows):
            for c, (fr, info) in enumerate(row):
                k = int(t * info["fps"] / 12)
                n, lf = len(fr), info["loopFrom"]
                idx = k if k < n else lf + (k - n) % max(1, n - lf)
                im.paste(fr[idx], (c * CW * Z, r * 32 * Z), fr[idx])
        seq.append(im)
    out = ROOT / "docs" / "characters.gif"
    out.parent.mkdir(exist_ok=True)
    seq[0].save(out, save_all=True, append_images=seq[1:], duration=83, loop=0, optimize=True)
    print(f"✓ {out}")


if __name__ == "__main__":
    main()
