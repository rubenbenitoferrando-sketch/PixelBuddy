"""Base común para los personajes de PixelBuddy (mismo estilo que el ninja).

Cada personaje es un módulo en tools/chars/ con:
  ID, NAMES = {"es": ..., "en": ...}, VARIANTS = [nombres], FPS, LOOP_FROM, ANCHOR,
  build_states(variant) -> {estado: [Canvas, ...]}
`python3 tools/make_characters.py` los genera todos en assets/avatars/<ID>/.
"""
import json
import math
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))
import make_sprites as ms  # noqa: E402

W, H = ms.W, ms.H
OUT = ms.OUT
SKIN, SKIN_D = ms.SKIN, ms.SKIN_D
EYE = ms.EYE
WHITE = (255, 255, 255, 255)
SHINE = ms.SHINE
ZCOL = ms.ZCOL
SWEAT = ms.SWEAT
Canvas, frame, shadow, in_ellipse = ms.Canvas, ms.frame, ms.shadow, ms.in_ellipse

COLORS = list(ms.VARIANTS)          # azul, rojo, verde, morado, naranja, amarillo
RUN_W, RUN_OFF = 48, 16              # frames de "trabaja": la estela va detrás
WIDE = 40                            # frames con algo a la derecha (signos, aro, pantalla…)

STATES = ["idle", "run", "katana", "train", "sleep", "alert", "ask"]


def rgba(c):
    return tuple(c) + (255,) if len(c) == 3 else tuple(c)


def palette(name):
    """(color, oscuro, claro) de la variante de color."""
    (b, bd, bl), _ = ms.VARIANTS[name]
    return rgba(b), rgba(bd), rgba(bl)


def shade(c, f):
    """Aclara (f > 1) u oscurece (f < 1) un color."""
    return tuple(max(0, min(255, round(v * f))) for v in c[:3]) + (255,)


# ---------------------------------------------------------------- imágenes

def holder(img):
    h = Canvas()
    w = img.width
    h.img, h.px = img, img.load()
    h.set = lambda x, y, c: (0 <= x < w and 0 <= y < H) and h.px.__setitem__((x, y), c)
    h.get = lambda x, y: h.px[x, y] if (0 <= x < w and 0 <= y < H) else (0, 0, 0, 0)
    return h


def widen(cv, width, offset=0):
    wide = Image.new("RGBA", (width, H), (0, 0, 0, 0))
    wide.alpha_composite(cv.img, (offset, 0))
    return wide


def wide_canvas(width):
    """Lienzo más ancho que 32 px con la misma interfaz que Canvas."""
    class Wide(Canvas):
        def __init__(self):
            self.img = Image.new("RGBA", (width, H), (0, 0, 0, 0))
            self.px = self.img.load()

        def set(self, x, y, c):
            if 0 <= x < width and 0 <= y < H:
                self.px[x, y] = c

        def get(self, x, y):
            if 0 <= x < width and 0 <= y < H:
                return self.px[x, y]
            return (0, 0, 0, 0)

        def outline(self):
            src = self.img.copy().load()
            for y in range(H):
                for x in range(width):
                    if src[x, y][3] == 0:
                        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                            nx, ny = x + dx, y + dy
                            if 0 <= nx < width and 0 <= ny < H and src[nx, ny][3] > 200 \
                                    and src[nx, ny] not in NO_OUTLINE:
                                self.px[x, y] = OUT
                                break
    return Wide()


NO_OUTLINE = set()   # colores que no llevan contorno (agua, chispas, humo…)


def wframe(width, builder):
    cv = wide_canvas(width)
    builder(cv)
    return cv


def outline(cv):
    """Contorno que respeta NO_OUTLINE también en lienzos de 32 px."""
    if hasattr(cv, "img") and cv.img.width != W:
        cv.outline()
        return
    src = cv.img.copy().load()
    for y in range(H):
        for x in range(W):
            if src[x, y][3] == 0:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < W and 0 <= ny < H and src[nx, ny][3] > 200 \
                            and src[nx, ny] not in NO_OUTLINE:
                        cv.px[x, y] = OUT
                        break


def speed_lines(cv, i, off=RUN_OFF, rows=((9, 10), (14, 14), (19, 8), (24, 11))):
    for k, (y, ln) in enumerate(rows):
        x0 = (off + 4) - ((i * 4 + k * 7) % 22) - ln
        for x in range(x0, x0 + ln):
            if 0 <= x < cv.img.width and cv.get(x, y)[3] == 0:
                t = (x - x0) / ln
                cv.set(x, y, (255, 255, 255, int(60 + 150 * t)))


def zzz(cv, p, base=(23, 5)):
    zpos = [[(0, 0)], [(2, -3), (0, 1)], [(4, -5), (1, -2)], [(3, -5)]]
    for (dx0, dy0) in zpos[p % 4]:
        zx, zy = base[0] + dx0, base[1] + dy0
        for (dx, dy) in ((0, 0), (1, 0), (2, 0), (3, 0), (2, 1), (1, 2), (0, 3), (1, 3), (2, 3), (3, 3)):
            cv.set(zx + dx, zy + dy, ZCOL)


def symbols(stand):
    """alert / ask a partir de una función stand(p) -> Canvas de 32 px."""
    bounce = (2, 1, 2, 3)
    return {
        "alert": [ms.with_symbol(stand(p), "!", 31, bounce[p], ms.ALERT_L if p == 1 else ms.ALERT)
                  for p in range(4)],
        "ask": [ms.with_symbol(stand(p), "?", 30, bounce[p], ms.ASK_L if p == 1 else ms.ASK)
                for p in range(4)],
    }


# ---------------------------------------------------------------- humano chibi

def head(cv, ox=0, oy=0, *, skin=SKIN, hair=(60, 40, 30, 255), style="short",
         eyes="open", mouth="smile", skin_d=None):
    """Cabeza grande mirando a la derecha. Centro (16, 10)."""
    skin_d = skin_d or (SKIN_D if skin == SKIN else shade(skin, 0.82))
    cx, cy, rx, ry = 16.0 + ox, 10.0 + oy, 7.5, 7.0
    for y in range(H):
        for x in range(cv.img.width):
            if in_ellipse(x, y, cx, cy, rx, ry):
                cv.set(x, y, skin)
    hair_d = shade(hair, 0.75)
    for y in range(H):
        for x in range(cv.img.width):
            if not in_ellipse(x, y, cx, cy, rx, ry):
                continue
            top = y + 0.5 < cy - 2.0
            back = x + 0.5 < cx - 3.5 and y + 0.5 < cy + 2.5
            if style == "short" and (top or back):
                cv.set(x, y, hair)
            elif style == "fringe" and (top or back or (y + 0.5 < cy - 0.5 and x + 0.5 > cx + 3)):
                cv.set(x, y, hair)
            elif style == "bald" and back and y + 0.5 > cy - 2:
                cv.set(x, y, hair)
    if style in ("short", "fringe"):
        for x in range(int(cx - 5), int(cx + 5)):      # brillo del pelo
            if cv.get(x, int(cy - 6))[:3] == hair[:3]:
                cv.set(x, int(cy - 6), shade(hair, 1.35)) if x % 3 == 0 else None
        cv.set(int(cx - 5), int(cy + 1), hair_d)
    # oreja
    cv.set(int(cx - 2), int(cy + 1), skin_d)
    # ojos
    for ex in (int(cx + 2), int(cx + 5)):
        ey = int(cy + 1)
        if eyes == "open":
            cv.set(ex, ey, EYE); cv.set(ex, ey + 1, EYE); cv.set(ex, ey - 1 + 0, EYE)
            cv.set(ex, ey - 1, SHINE)
        elif eyes == "closed":
            cv.set(ex - 1, ey + 1, EYE); cv.set(ex, ey + 1, EYE)
        elif eyes == "happy":
            cv.set(ex - 1, ey + 1, EYE); cv.set(ex, ey, EYE); cv.set(ex + 1, ey + 1, EYE)
        elif eyes == "focus":
            cv.set(ex, ey, EYE); cv.set(ex, ey + 1, EYE); cv.set(ex - 1, ey - 1, EYE); cv.set(ex, ey - 1, EYE)
    # mejilla y boca
    cv.set(int(cx + 1), int(cy + 3), (240, 160, 150, 255))
    mx, my = int(cx + 4), int(cy + 4)
    if mouth == "smile":
        cv.set(mx - 1, my, skin_d); cv.set(mx, my + 0, skin_d)
    elif mouth == "open":
        cv.set(mx, my, (150, 60, 60, 255)); cv.set(mx - 1, my, (150, 60, 60, 255))
    elif mouth == "o":
        cv.set(mx, my, (150, 60, 60, 255))


LEG_PHASES = {   # (muslo/pierna delantera, pie, pierna trasera, pie) como rectángulos
    "r0": ((9, 26, 11, 27), (6, 28, 9, 28), (19, 25, 21, 28), (19, 29, 23, 29)),
    "r1": ((11, 25, 13, 28), (10, 29, 13, 29), (17, 25, 19, 27), (18, 27, 21, 27)),
    "r2": ((14, 25, 16, 28), (14, 29, 17, 29), (14, 25, 16, 26), (12, 27, 15, 27)),
    "r3": ((19, 25, 21, 28), (19, 29, 23, 29), (9, 26, 11, 27), (6, 28, 9, 28)),
    "r4": ((17, 25, 19, 27), (18, 27, 21, 27), (11, 25, 13, 28), (10, 29, 13, 29)),
    "r5": ((14, 25, 16, 26), (12, 27, 15, 27), (14, 25, 16, 28), (14, 29, 17, 29)),
}


def legs(cv, pose="stand", ox=0, oy=0, *, pants=(50, 60, 90, 255), shoes=(40, 40, 50, 255),
         socks=None):
    if pose == "stand":
        cv.rect(12 + ox, 25 + oy, 14 + ox, 28, pants)
        cv.rect(18 + ox, 25 + oy, 20 + ox, 28, pants)
        if socks:
            cv.rect(12 + ox, 27, 14 + ox, 28, socks); cv.rect(18 + ox, 27, 20 + ox, 28, socks)
        cv.rect(11 + ox, 29, 14 + ox, 29, shoes)
        cv.rect(18 + ox, 29, 21 + ox, 29, shoes)
    elif pose == "sit":            # sentado en el suelo, rodillas hacia delante
        cv.rect(11 + ox, 25 + oy, 22 + ox, 27, pants)
        cv.rect(20 + ox, 26, 22 + ox, 28, pants)
        cv.rect(20 + ox, 29, 24 + ox, 29, shoes)
        cv.rect(9 + ox, 28, 13 + ox, 29, shoes)
    elif pose == "chair":          # sentado en una silla: muslos horizontales
        cv.rect(13 + ox, 23 + oy, 21 + ox, 24 + oy, pants)
        cv.rect(20 + ox, 25 + oy, 21 + ox, 27 + oy, pants)
        cv.rect(20 + ox, 28 + oy, 23 + ox, 28 + oy, shoes)
    elif pose == "kick":           # pierna derecha levantada (pisar / tocar balón)
        cv.rect(12 + ox, 25 + oy, 14 + ox, 28, pants)
        cv.rect(11 + ox, 29, 14 + ox, 29, shoes)
        cv.rect(18 + ox, 25 + oy, 20 + ox, 26 + oy, pants)
        cv.rect(20 + ox, 26 + oy, 22 + ox, 27 + oy, socks or pants)
        cv.rect(22 + ox, 26 + oy, 24 + ox, 27 + oy, shoes)
    elif pose == "jump":
        cv.rect(12 + ox, 25 + oy, 14 + ox, 27 + oy, pants); cv.rect(11 + ox, 28 + oy, 14 + ox, 28 + oy, shoes)
        cv.rect(18 + ox, 25 + oy, 20 + ox, 26 + oy, pants); cv.rect(20 + ox, 26 + oy, 23 + ox, 27 + oy, shoes)
    else:                          # fases de carrera
        a, b, c, d = LEG_PHASES[pose]
        cv.rect(a[0] + ox, a[1] + oy, a[2] + ox, a[3] + oy, pants)
        cv.rect(b[0] + ox, b[1] + oy, b[2] + ox, b[3] + oy, shoes)
        cv.rect(c[0] + ox, c[1] + oy, c[2] + ox, c[3] + oy, pants)
        cv.rect(d[0] + ox, d[1] + oy, d[2] + ox, d[3] + oy, shoes)


def torso(cv, ox=0, oy=0, *, shirt=(80, 90, 110, 255), shirt_d=None, belt=None, collar=None):
    shirt_d = shirt_d or shade(shirt, 0.75)
    cv.rect(11 + ox, 18 + oy, 21 + ox, 24 + oy, shirt)
    cv.rect(11 + ox, 18 + oy, 12 + ox, 24 + oy, shirt_d)
    if collar:
        cv.rect(14 + ox, 18 + oy, 18 + ox, 18 + oy, collar)
    if belt:
        cv.rect(11 + ox, 24 + oy, 21 + ox, 24 + oy, belt)


def arm(cv, pose, ox=0, oy=0, *, sleeve, skin=SKIN, side="front"):
    """Un brazo. side="back" (izquierdo, detrás) o "front" (derecho)."""
    s = shade(sleeve, 0.8) if side == "back" else sleeve
    if side == "back":
        poses = {
            "rest": [((9, 19, 10, 22), s), ((9, 23, 9, 23), skin)],
            "up": [((8, 13, 9, 19), s), ((8, 11, 9, 12), skin)],
            "out": [((5, 19, 10, 20), s), ((3, 19, 4, 20), skin)],
            "back": [((5, 20, 10, 21), s), ((4, 21, 4, 21), skin)],
            "forward": [((10, 20, 13, 21), s), ((14, 21, 14, 21), skin)],
        }
    else:
        poses = {
            "rest": [((22, 19, 23, 22), s), ((23, 23, 23, 23), skin)],
            "up": [((22, 12, 23, 18), s), ((22, 10, 23, 11), skin)],
            "out": [((22, 19, 27, 20), s), ((28, 19, 29, 20), skin)],
            "forward": [((22, 20, 25, 21), s), ((26, 20, 27, 21), skin)],
            "forward_low": [((22, 21, 24, 22), s), ((25, 22, 26, 22), skin)],
            "front_up": [((22, 16, 24, 19), s), ((24, 14, 25, 15), skin)],
            "mouth": [((22, 19, 23, 20), s), ((21, 16, 22, 18), s), ((21, 15, 22, 15), skin)],
            "wipe": [((22, 13, 23, 18), s), ((19, 11, 22, 12), skin)],
            "back": [((22, 20, 26, 21), s), ((27, 21, 27, 21), skin)],
            "raise": [((22, 17, 23, 19), s), ((24, 15, 25, 16), s), ((26, 12, 27, 14), s),
                      ((26, 10, 27, 11), skin)],
        }
    for (x0, y0, x1, y1), c in poses[pose]:
        cv.rect(x0 + ox, y0 + oy, x1 + ox, y1 + oy, c)


def write(module, out_root):
    """Genera las tiras PNG y el manifest de un personaje."""
    out = Path(out_root) / module.ID
    out.mkdir(parents=True, exist_ok=True)
    for old in out.glob(f"{module.ID}_*.png"):
        old.unlink()
    manifest = {"id": module.ID, "prefix": module.ID, "name": module.NAMES,
                "frameSize": [W, H], "variants": list(module.VARIANTS), "states": {}}
    for v in module.VARIANTS:
        states = module.build_states(v)
        for st in STATES:
            frames = states[st]
            fw = frames[0].img.width
            sheet = Image.new("RGBA", (fw * len(frames), H), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                sheet.paste(f.img, (i * fw, 0))
            sheet.save(out / f"{module.ID}_{v}_{st}.png")
            manifest["states"][st] = {
                "frames": len(frames), "fps": module.FPS[st],
                "loopFrom": module.LOOP_FROM.get(st, 0), "frameSize": [fw, H],
                "anchorX": module.ANCHOR.get(st, 16),
            }
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False))
    return out
