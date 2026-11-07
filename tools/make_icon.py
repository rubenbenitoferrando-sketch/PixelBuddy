"""Genera el icono de la app (assets/AppIcon.icns): cielo nocturno pixel art con el
astronauta en grande y con colores intensos. Necesita Pillow y la herramienta `iconutil` de macOS.
Uso: python3 tools/make_icon.py
"""
import shutil
import subprocess
import tempfile
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance

ROOT = Path(__file__).resolve().parent.parent
AV = ROOT / "assets" / "avatars"
S = 1024
BODY, RADIUS = 824, 185          # plantilla de iconos de macOS: cuerpo 824 px, esquinas ~185
TOP, BOTTOM = (24, 28, 66), (52, 58, 120)


def frame(avatar, prefix, variant, state="idle", index=0):
    sheet = Image.open(AV / avatar / f"{prefix}_{variant}_{state}.png").convert("RGBA")
    return sheet.crop((index * 32, 0, index * 32 + 32, 32))


def _body_mask(off):
    m = Image.new("L", (S, S), 0)
    ImageDraw.Draw(m).rounded_rectangle([off, off, off + BODY - 1, off + BODY - 1], RADIUS, fill=255)
    return m


def main():
    icon = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    off = (S - BODY) // 2

    # fondo: degradado vertical en franjas de "píxel" de 16 px
    bg = Image.new("RGBA", (BODY, BODY))
    d = ImageDraw.Draw(bg)
    for y in range(0, BODY, 16):
        t = y / BODY
        c = tuple(round(a + (b - a) * t) for a, b in zip(TOP, BOTTOM)) + (255,)
        d.rectangle([0, y, BODY, y + 16], fill=c)
    # estrellas pixeladas
    for (x, y, s) in [(90, 110, 16), (300, 70, 12), (520, 140, 16), (700, 90, 12),
                      (640, 260, 12), (150, 300, 12), (420, 230, 8), (740, 330, 16)]:
        d.rectangle([x, y, x + s, y + s], fill=(235, 240, 255, 255))
    # suelo
    d.rectangle([0, BODY - 150, BODY, BODY], fill=(30, 34, 70, 255))
    d.rectangle([0, BODY - 150, BODY, BODY - 138], fill=(70, 78, 140, 255))

    mask = Image.new("L", (BODY, BODY), 0)
    ImageDraw.Draw(mask).rounded_rectangle([0, 0, BODY - 1, BODY - 1], RADIUS, fill=255)
    icon.paste(bg, (off, off), mask)

    # halo pixelado detrás del astronauta para que resalte sobre el cielo
    halo = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    hd = ImageDraw.Draw(halo)
    cx, cy = S // 2, S // 2 - 20
    for r, a in ((330, 40), (270, 60), (210, 80)):
        for y in range(cy - r, cy + r, 16):
            for x in range(cx - r, cx + r, 16):
                if (x + 8 - cx) ** 2 + (y + 8 - cy) ** 2 <= r * r:
                    hd.rectangle([x, y, x + 15, y + 15], fill=(120, 160, 255, a))
    icon.alpha_composite(Image.composite(halo, Image.new("RGBA", (S, S), (0, 0, 0, 0)), _body_mask(off)))

    # astronauta: 32 px x 20 = 640 px, colores más intensos, de pie sobre el suelo
    scale = 20
    astro = frame("astronauta", "astronauta", "rojo")
    astro = ImageEnhance.Color(astro).enhance(1.8)
    astro = ImageEnhance.Contrast(astro).enhance(1.2)
    astro = astro.resize((32 * scale, 32 * scale), Image.NEAREST)
    feet = off + BODY - 150 + 40 - 32 * scale
    icon.alpha_composite(astro, ((S - 32 * scale) // 2, feet))

    tmp = Path(tempfile.mkdtemp())
    iconset = tmp / "AppIcon.iconset"
    iconset.mkdir()
    for size in (16, 32, 128, 256, 512):
        for mult in (1, 2):
            px = size * mult
            name = f"icon_{size}x{size}{'@2x' if mult == 2 else ''}.png"
            icon.resize((px, px), Image.LANCZOS).save(iconset / name)
    out = ROOT / "assets" / "AppIcon.icns"
    subprocess.run(["iconutil", "-c", "icns", str(iconset), "-o", str(out)], check=True)
    icon.save(ROOT / "assets" / "AppIcon.png")
    shutil.rmtree(tmp)
    print(f"✓ {out}")


if __name__ == "__main__":
    main()
