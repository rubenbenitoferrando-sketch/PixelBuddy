"""Genera los sprites pixel art del astronauta (32x32 por frame, mismos estados que el ninja).

Estados (mismas claves que el ninja para que la app los trate igual):
  idle   flota suavemente
  run    vuela con la mochila propulsora (frames 48x32, llama y humo detrás)
  katana planta una bandera de su color que ondea (frames 40x32)
  train  estiramientos de gravedad cero
  sleep  flotando acurrucado con Zzz
  alert  exclamación roja al lado (40x32)
  ask    interrogación azul al lado (40x32)
Variantes: el traje es siempre blanco; cambian el anillo del cuello, el cinturón,
las luces del pecho y la bandera.
Uso: python3 tools/make_astronaut.py [carpeta]   (por defecto assets/avatars/astronauta)
"""
import json
import math
import sys
from pathlib import Path

from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import make_sprites as ms  # noqa: E402  (Canvas, contorno, sombra, signos, Zzz…)

W, H = ms.W, ms.H
OUT = ms.OUT

SUIT = (238, 241, 248, 255)
SUIT_S = (190, 198, 216, 255)     # sombra del traje
PACK = (150, 158, 178, 255)       # mochila
PACK_D = (104, 112, 132, 255)
BOOT = (72, 78, 96, 255)
GLOVE = (120, 128, 148, 255)
PANEL = (58, 64, 84, 255)
VISOR = (26, 38, 82, 255)
VISOR_L = (52, 82, 150, 255)
GLINT = (255, 255, 255, 255)
GLINT_C = (140, 220, 255, 255)
POLE = (205, 210, 222, 255)
FLAME = [(255, 250, 220, 255), (255, 214, 90, 255), (255, 140, 40, 255), (220, 60, 30, 255)]
SMOKE = (210, 216, 228)
RED_LED = (240, 70, 70, 255)
GREEN_LED = (90, 230, 120, 255)

RUN_W, RUN_OFF = 48, 16
FLAG_W = 40


def palette(name):
    (b, bd, bl), _ = ms.VARIANTS[name]
    a = lambda t: t + (255,)
    return a(b), a(bd), a(bl)


def draw_astronaut(cv, col, *, bob=0, legs="stand", arms="rest", visor="open",
                   lean=0, head_dx=0, head_dy=0, led=0):
    band, band_d, band_l = col
    ox, oy = lean, bob
    # ---- mochila (detrás, a la izquierda) ----
    cv.rect(6 + ox, 17 + oy, 9 + ox, 25 + oy, PACK)
    cv.rect(6 + ox, 24 + oy, 9 + ox, 25 + oy, PACK_D)
    cv.set(7 + ox, 19 + oy, PACK_D); cv.set(7 + ox, 21 + oy, PACK_D)

    # ---- piernas ----
    if legs == "stand":
        cv.rect(12 + ox, 25 + oy, 14 + ox, 28 + oy, SUIT)
        cv.rect(18 + ox, 25 + oy, 20 + ox, 28 + oy, SUIT)
        cv.rect(12 + ox, 25 + oy, 12 + ox, 28 + oy, SUIT_S)
        cv.rect(11 + ox, 29 + oy, 14 + ox, 29 + oy, BOOT)
        cv.rect(18 + ox, 29 + oy, 21 + ox, 29 + oy, BOOT)
    elif legs == "fly":   # piernas estiradas hacia atrás, por debajo del cuerpo
        cv.rect(10 + ox, 26 + oy, 16 + ox, 27 + oy, SUIT)
        cv.rect(8 + ox, 26 + oy, 9 + ox, 27 + oy, BOOT)
        cv.rect(13 + ox, 28 + oy, 19 + ox, 29 + oy, SUIT_S)
        cv.rect(11 + ox, 28 + oy, 12 + ox, 29 + oy, BOOT)
    elif legs == "curl":  # rodillas recogidas
        cv.rect(11 + ox, 25 + oy, 21 + ox, 27 + oy, SUIT)
        cv.rect(11 + ox, 27 + oy, 21 + ox, 27 + oy, SUIT_S)
        cv.rect(19 + ox, 28 + oy, 22 + ox, 28 + oy, BOOT)
        cv.rect(10 + ox, 28 + oy, 13 + ox, 28 + oy, BOOT)

    # ---- brazos (detrás del casco) ----
    if arms == "rest":
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, SUIT_S); cv.set(8 + ox, 24 + oy, GLOVE)
        cv.rect(23 + ox, 20 + oy, 24 + ox, 23 + oy, SUIT); cv.set(24 + ox, 24 + oy, GLOVE)
    elif arms == "out":
        cv.rect(4 + ox, 20 + oy, 9 + ox, 21 + oy, SUIT_S); cv.rect(2 + ox, 20 + oy, 3 + ox, 21 + oy, GLOVE)
        cv.rect(23 + ox, 20 + oy, 28 + ox, 21 + oy, SUIT); cv.rect(29 + ox, 20 + oy, 30 + ox, 21 + oy, GLOVE)
    elif arms == "up":    # brazos en V hacia arriba, por fuera del casco
        for k in range(5):
            cv.rect(8 - k + ox, 19 - k + oy, 9 - k + ox, 20 - k + oy, SUIT_S)
            cv.rect(23 + k + ox, 19 - k + oy, 24 + k + ox, 20 - k + oy, SUIT)
        cv.rect(3 + ox, 14 + oy, 4 + ox, 14 + oy, GLOVE); cv.rect(28 + ox, 14 + oy, 29 + ox, 14 + oy, GLOVE)
    elif arms == "forward":   # volando: brazos hacia delante
        cv.rect(23 + ox, 19 + oy, 27 + ox, 20 + oy, SUIT); cv.rect(28 + ox, 19 + oy, 29 + ox, 20 + oy, GLOVE)
        cv.rect(21 + ox, 21 + oy, 25 + ox, 22 + oy, SUIT_S); cv.set(26 + ox, 22 + oy, GLOVE)
    elif arms == "flag":      # sujeta el mástil
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, SUIT_S); cv.set(8 + ox, 24 + oy, GLOVE)
        cv.rect(23 + ox, 19 + oy, 25 + ox, 20 + oy, SUIT); cv.rect(26 + ox, 18 + oy, 27 + ox, 20 + oy, GLOVE)
    elif arms == "flag_up":   # levanta la bandera para clavarla
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, SUIT_S); cv.set(8 + ox, 24 + oy, GLOVE)
        cv.rect(23 + ox, 16 + oy, 24 + ox, 20 + oy, SUIT); cv.rect(25 + ox, 15 + oy, 27 + ox, 16 + oy, GLOVE)

    # ---- cuerpo ----
    cv.rect(10 + ox, 19 + oy, 22 + ox, 25 + oy, SUIT)
    cv.rect(10 + ox, 19 + oy, 11 + ox, 25 + oy, SUIT_S)
    cv.rect(15 + ox, 20 + oy, 19 + ox, 22 + oy, PANEL)            # panel del pecho
    cv.set(16 + ox, 21 + oy, band_l)
    cv.set(18 + ox, 21 + oy, GREEN_LED if led % 2 == 0 else RED_LED)
    cv.rect(10 + ox, 24 + oy, 22 + ox, 24 + oy, band)              # cinturón

    # ---- casco ----
    hx, hy = ox + head_dx, oy + head_dy
    cx, cy, rx, ry = 16.0 + hx, 10.0 + hy, 9.0, 8.5
    for y in range(H):
        for x in range(W):
            if ms.in_ellipse(x, y, cx, cy, rx, ry):
                cv.set(x, y, SUIT)
    for y in range(H):   # sombra inferior izquierda del casco
        for x in range(W):
            if ms.in_ellipse(x, y, cx, cy, rx, ry) and not ms.in_ellipse(x, y, cx + 1.5, cy - 1.2, rx, ry):
                cv.set(x, y, SUIT_S)
    # anillo del cuello (color de la sesión)
    for x in range(10 + hx, 23 + hx):
        cv.set(x, 18 + hy, band)
    cv.set(10 + hx, 18 + hy, band_d); cv.set(22 + hx, 18 + hy, band_d)
    # visor (mirando a la derecha)
    vcx, vcy = 18.5 + hx, 10.5 + hy
    for y in range(H):
        for x in range(W):
            if ms.in_ellipse(x, y, vcx, vcy, 6.0, 4.6):
                lower = y + 0.5 > vcy + 1.5
                cv.set(x, y, VISOR_L if (lower and visor == "open") else VISOR)
    if visor == "open":
        cv.set(20 + hx, 8 + hy, GLINT); cv.set(21 + hx, 8 + hy, GLINT); cv.set(22 + hx, 9 + hy, GLINT)
        cv.set(15 + hx, 13 + hy, GLINT_C)
    elif visor == "glint2":
        cv.set(19 + hx, 8 + hy, GLINT); cv.set(20 + hx, 8 + hy, GLINT); cv.set(21 + hx, 9 + hy, GLINT)
        cv.set(16 + hx, 13 + hy, GLINT_C)
    elif visor == "sleep":   # visor oscurecido: "ojos cerrados"
        cv.rect(15 + hx, 11 + hy, 22 + hx, 11 + hy, VISOR_L)


def draw_flag(px, x0, y0, phase, col, width=8, height=5):
    """Tela de la bandera: la mitad exterior sube y baja 1 px para que ondee."""
    band, band_d, band_l = col
    pts = {}
    for i in range(width):
        dy = 0 if i < width // 2 else (0, 1, 0, -1)[phase % 4]
        for j in range(height):
            c = band_l if j == 0 else band_d if j == height - 1 else band
            pts[(x0 + i, y0 + j + dy)] = c
    for q in ((x0 + 2, y0 + 2), (x0 + 3, y0 + 2)):   # emblema blanco
        pts[q] = GLINT
    keys = set(pts)
    for (x, y) in keys:
        for dx, dy in ((1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in keys and 0 <= q[0] < FLAG_W and 0 <= q[1] < H and px[q][3] == 0:
                px[q] = OUT
    for q, c in pts.items():
        if 0 <= q[0] < FLAG_W and 0 <= q[1] < H:
            px[q] = c


def draw_pole(px, x, top, bottom):
    """Mástil fino: 1 px claro con contorno solo a la izquierda y bola dorada arriba."""
    for y in range(top, bottom + 1):
        px[x, y] = POLE
        if px[x - 1, y][3] == 0:
            px[x - 1, y] = OUT
    px[x, top - 1] = (255, 214, 90, 255)
    px[x - 1, top - 1] = OUT if px[x - 1, top - 1][3] == 0 else px[x - 1, top - 1]


def widen(core, width, offset=0):
    wide = Image.new("RGBA", (width, H), (0, 0, 0, 0))
    wide.alpha_composite(core.img, (offset, 0))
    return wide


def holder(img):
    h = ms.Canvas()
    h.img = img
    return h


def float_shadow(cv, bob):
    # cuanto más arriba flota, más pequeña la sombra
    ms.shadow(cv, 12 if bob >= 0 else 10)


def build_states(col):
    st = {}

    # IDLE: flota suavemente, el reflejo del visor se mueve
    idle_bob = (0, -1, -1, 0)
    idle = []
    for p in range(4):
        def b(cv, p=p):
            draw_astronaut(cv, col, bob=idle_bob[p], visor="glint2" if p == 2 else "open", led=p // 2)
            cv.outline(); float_shadow(cv, idle_bob[p])
        idle.append(ms.frame(b))
    st["idle"] = idle

    # RUN: vuela con la mochila propulsora (48x32): llama y humo detrás
    run = []
    for i in range(6):
        bob = -(i % 3 == 1)
        def b(cv, i=i, bob=bob):
            draw_astronaut(cv, col, bob=bob, legs="fly", arms="forward", lean=1,
                           head_dx=2, head_dy=1, led=i)
            cv.outline(); ms.shadow(cv, 10)
        core = ms.frame(b)
        wide = widen(core, RUN_W, RUN_OFF)
        px = wide.load()
        # humo: bolitas grises que se quedan atrás
        for k in range(4):
            x = RUN_OFF + 2 - ((i * 3 + k * 6) % 18)
            y = 23 + bob + (k % 2) - (k // 2)
            alpha = 150 - k * 30
            for dx in range(-1, 2):
                for dy in range(-1, 2):
                    if abs(dx) + abs(dy) <= 1 + (k % 2) and 0 <= x + dx < RUN_W:
                        if px[x + dx, y + dy][3] == 0:
                            px[x + dx, y + dy] = SMOKE + (alpha,)
        # llama de la mochila, parpadea
        fx, fy = RUN_OFF + 6, 25 + bob
        length = (7, 10, 8, 6, 10, 8)[i]
        for k in range(length):
            half = 2 if k < length // 3 else 1 if k < length * 2 // 3 else 0
            for dy in range(-half, half + 1):
                tone = min(3, (k * 4) // length + abs(dy))
                px[fx - 1 - k, fy + dy] = FLAME[tone]
        # líneas de velocidad
        for k, (y, ln) in enumerate([(9, 8), (14, 12), (19, 7)]):
            x0 = RUN_OFF + 2 - ((i * 4 + k * 7) % 20) - ln
            for x in range(x0, x0 + ln):
                if 0 <= x < RUN_W and px[x, y][3] == 0:
                    px[x, y] = (255, 255, 255, int(60 + 150 * (x - x0) / ln))
        run.append(holder(wide))
    st["run"] = run

    # KATANA (= celebrar): levanta la bandera, la clava y ondea (40x32)
    flag = []
    def raise_flag(cv):
        draw_astronaut(cv, col, arms="flag_up")
        cv.outline(); ms.shadow(cv)
    def plant(cv):
        draw_astronaut(cv, col, arms="flag")
        cv.outline(); ms.shadow(cv)
    f0 = widen(ms.frame(raise_flag), FLAG_W)
    p0 = f0.load()
    draw_pole(p0, 27, 3, 21)
    draw_flag(p0, 28, 3, 0, col, width=7)
    flag.append(holder(f0))
    f1 = widen(ms.frame(plant), FLAG_W)
    p1 = f1.load()
    draw_pole(p1, 27, 5, 29)
    for (dx, dy) in ((-3, 0), (2, 0), (-2, -1), (1, -1)):   # polvo al clavarla
        p1[27 + dx, 29 + dy] = SMOKE + (220,)
    draw_flag(p1, 28, 5, 0, col)
    flag.append(holder(f1))
    for p in range(4):
        def b(cv, p=p):
            draw_astronaut(cv, col, arms="flag", led=p)
            cv.outline(); ms.shadow(cv)
        f = widen(ms.frame(b), FLAG_W)
        pp = f.load()
        draw_pole(pp, 27, 4, 29)
        draw_flag(pp, 28, 4, p + 1, col)
        flag.append(holder(f))
    st["katana"] = flag

    # TRAIN: estiramientos de gravedad cero + partículas
    tr = []
    poses = ("rest", "out", "up", "out")
    for p in range(4):
        def b(cv, p=p):
            draw_astronaut(cv, col, arms=poses[p], bob=-1 if poses[p] == "up" else 0, led=p)
            cv.outline(); ms.shadow(cv)
            for (x, y) in ((3, 6 + p), (28, 4 + (p * 2) % 6), (5, 15 - p)):
                cv.set(x, y, ms.ZCOL)
        tr.append(ms.frame(b))
    st["train"] = tr

    # SLEEP: acurrucado flotando, visor oscuro, Zzz
    sl = []
    zpos = [[(23, 3)], [(25, 1), (23, 4)], [(27, 0), (24, 2)], [(26, 0)]]
    for p in range(4):
        def b(cv, p=p):
            draw_astronaut(cv, col, legs="curl", visor="sleep", bob=-(p % 2))
            cv.outline(); ms.shadow(cv, 10)
            for (zx, zy) in zpos[p]:
                for (dx, dy) in ((0, 0), (1, 0), (2, 0), (3, 0), (2, 1), (1, 2), (0, 3), (1, 3), (2, 3), (3, 3)):
                    cv.set(zx + dx, zy + dy, ms.ZCOL)
        sl.append(ms.frame(b))
    st["sleep"] = sl

    # ALERT / ASK: flotando quieto y el signo al lado (40x32)
    def standing(p):
        def b(cv):
            draw_astronaut(cv, col, bob=-(p % 2))
            cv.outline(); ms.shadow(cv)
        return b
    bounce = (2, 1, 2, 3)
    st["alert"] = [ms.with_symbol(ms.frame(standing(p)), "!", 31, bounce[p],
                                  ms.ALERT_L if p == 1 else ms.ALERT) for p in range(4)]
    st["ask"] = [ms.with_symbol(ms.frame(standing(p)), "?", 30, bounce[p],
                                ms.ASK_L if p == 1 else ms.ASK) for p in range(4)]
    return st


FPS = {"idle": 4, "run": 12, "katana": 6, "train": 6, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}


def main(out, prefix="astronauta"):
    out = Path(out)
    out.mkdir(parents=True, exist_ok=True)
    for old in out.glob(f"{prefix}_*.png"):
        old.unlink()
    manifest = {"id": prefix, "prefix": prefix, "name": {"es": "Astronauta", "en": "Astronaut"},
                "frameSize": [W, H], "variants": list(ms.VARIANTS), "states": {}}
    for name in ms.VARIANTS:
        states = build_states(palette(name))
        for st, frames in states.items():
            fw = frames[0].img.width
            sheet = Image.new("RGBA", (fw * len(frames), H), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                sheet.paste(f.img, (i * fw, 0))
            sheet.save(out / f"{prefix}_{name}_{st}.png")
            manifest["states"][st] = {
                "frames": len(frames), "fps": FPS[st], "loopFrom": LOOP_FROM.get(st, 0),
                "frameSize": [fw, H], "anchorX": ANCHOR.get(st, 16),
            }
    (out / "manifest.json").write_text(json.dumps(manifest, indent=2))
    return manifest


if __name__ == "__main__":
    print(json.dumps(main(sys.argv[1] if len(sys.argv) > 1 else "assets/avatars/astronauta"), indent=1))
