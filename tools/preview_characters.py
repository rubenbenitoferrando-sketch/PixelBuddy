"""Vista previa animada de uno o varios personajes en todos sus estados y colores.

Uso: python3 tools/preview_characters.py <carpeta_avatars> <salida.gif> id1 [id2 ...]
Arriba de cada personaje: todos los estados animados (primer color). Debajo: sus 6 variantes.
"""
import json
import sys
from pathlib import Path

from PIL import Image, ImageDraw

STATES = ["idle", "run", "katana", "train", "sleep", "ask", "alert"]
LABELS = ["idle", "trabaja", "termina", "espera", "duerme", "pregunta", "error"]
Z, CW, BG = 4, 56, (232, 236, 242)


def frames(d, man, variant, st):
    info = man["states"][st]
    fw, ax = info["frameSize"][0], info["anchorX"]
    sheet = Image.open(d / f"{man['prefix']}_{variant}_{st}.png").convert("RGBA")
    res = []
    for i in range(sheet.width // fw):
        c = Image.new("RGBA", (CW, 32), (0, 0, 0, 0))
        c.alpha_composite(sheet.crop((i * fw, 0, i * fw + fw, 32)), (32 - ax, 0))
        res.append(c.resize((CW * Z, 32 * Z), Image.NEAREST))
    return res, info


def pick(fr, info, t):
    k = int(t * info["fps"] / 12)
    n, lf = len(fr), info["loopFrom"]
    return fr[k if k < n else lf + (k - n) % max(1, n - lf)]


def main(root, out, ids):
    root = Path(root)
    blocks = []
    for cid in ids:
        d = root / cid
        man = json.loads((d / "manifest.json").read_text())
        states = [frames(d, man, man["variants"][0], st) for st in STATES]
        variants = [frames(d, man, v, "idle") for v in man["variants"]]
        blocks.append((man, states, variants))
    rowh = 32 * Z
    h = sum(2 * rowh + 40 for _ in blocks)
    seq = []
    for t in range(72):
        im = Image.new("RGB", (len(STATES) * CW * Z, h), BG)
        dr = ImageDraw.Draw(im)
        y = 0
        for man, states, variants in blocks:
            dr.text((8, y + 4), f"{man['name']['es']}  ·  {', '.join(man['variants'])}", fill=(30, 30, 50))
            y += 20
            for c, (fr, info) in enumerate(states):
                f = pick(fr, info, t)
                im.paste(f, (c * CW * Z, y), f)
                dr.text((c * CW * Z + 70, y + rowh - 6), LABELS[c], fill=(60, 60, 80))
            y += rowh + 6
            for c, (fr, info) in enumerate(variants):
                f = pick(fr, info, t)
                im.paste(f, (c * CW * Z, y), f)
            y += rowh + 14
        seq.append(im)
    seq[0].save(out, save_all=True, append_images=seq[1:], duration=83, loop=0)
    print("✓", out)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3:])
