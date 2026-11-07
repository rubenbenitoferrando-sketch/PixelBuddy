"""Genera los sprites pixel art del ninja (32x32 por frame).

Estados: idle, run, katana, train (pensando), sleep.
Variantes de color del traje: una por sesión de Claude Code.
Uso: python3 tools/make_sprites.py [carpeta]   (por defecto assets/avatars/ninja)
"""
import json
import math
import sys
from pathlib import Path

from PIL import Image

W = H = 32

# Colores fijos (cinta y fajín azules, llamas azules)
OUT = (18, 18, 30, 255)          # contorno
SKIN = (242, 199, 155, 255)
SKIN_D = (214, 160, 115, 255)
EYE = (18, 18, 30, 255)
SHINE = (255, 255, 255, 255)
BAND = (47, 125, 225, 255)
BAND_D = (28, 79, 160, 255)
BAND_L = (127, 196, 255, 255)
METAL = (205, 210, 222, 255)
METAL_D = (120, 128, 145, 255)
BLADE = (232, 244, 255, 255)
BLADE_D = (150, 180, 212, 255)
HILT = (58, 46, 42, 255)
GUARD = (212, 160, 23, 255)
FL_CORE = (230, 250, 255, 255)
FL_1 = (110, 227, 255, 255)
FL_2 = (42, 140, 255, 255)
FL_3 = (27, 63, 209, 255)
SHADOW = (0, 0, 0, 70)
WHITE = (255, 255, 255, 255)
ZCOL = (180, 215, 255, 255)
SWEAT = (140, 210, 255, 255)
ALERT = (232, 44, 44, 255)       # exclamación de error
ALERT_L = (255, 110, 100, 255)
ASK = (40, 120, 235, 255)        # interrogación: pregunta o permiso
ASK_L = (90, 165, 255, 255)

SUIT = (43, 45, 66)
SUIT_L = (66, 70, 98)

# Por sesión cambian la cinta (y el fajín) y el fuego. Traje siempre negro.
# nombre: (cinta, cinta_oscura, cinta_clara), (núcleo, fuego1, fuego2, fuego3)
VARIANTS = {
    "azul":    (((47, 125, 225), (28, 79, 160), (127, 196, 255)),
                ((230, 250, 255), (110, 227, 255), (42, 140, 255), (27, 63, 209))),
    "rojo":    (((220, 50, 50), (150, 25, 35), (255, 130, 120)),
                ((255, 250, 220), (255, 210, 80), (255, 120, 30), (210, 40, 20))),
    "verde":   (((60, 180, 80), (30, 115, 50), (150, 235, 150)),
                ((240, 255, 230), (170, 255, 120), (60, 220, 70), (20, 140, 50))),
    "morado":  (((150, 70, 220), (95, 40, 150), (205, 160, 255)),
                ((250, 235, 255), (220, 160, 255), (160, 80, 255), (100, 40, 200))),
    "naranja": (((240, 140, 30), (170, 90, 15), (255, 200, 120)),
                ((255, 250, 225), (255, 220, 110), (255, 150, 40), (220, 90, 10))),
    "amarillo": (((235, 200, 40), (165, 135, 20), (255, 235, 140)),
                 ((255, 255, 235), (255, 245, 150), (250, 210, 50), (200, 150, 20))),
}


def apply_variant(name):
    """Cambia la paleta global de cinta y fuego."""
    (b, bd, bl), (c, f1, f2, f3) = VARIANTS[name]
    a = lambda t: t + (255,)
    globals().update(BAND=a(b), BAND_D=a(bd), BAND_L=a(bl),
                     FL_CORE=a(c), FL_1=a(f1), FL_2=a(f2), FL_3=a(f3))


class Canvas:
    def __init__(self):
        self.img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        self.px = self.img.load()

    def set(self, x, y, c):
        if 0 <= x < W and 0 <= y < H:
            self.px[x, y] = c

    def get(self, x, y):
        if 0 <= x < W and 0 <= y < H:
            return self.px[x, y]
        return (0, 0, 0, 0)

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.set(x, y, c)

    def outline(self):
        """Añade contorno oscuro alrededor de todo lo opaco."""
        src = self.img.copy().load()
        for y in range(H):
            for x in range(W):
                if src[x, y][3] == 0:
                    for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                        nx, ny = x + dx, y + dy
                        if 0 <= nx < W and 0 <= ny < H and src[nx, ny][3] > 200 \
                                and src[nx, ny] not in (FL_1, FL_2, FL_3, FL_CORE, ZCOL, WHITE, SWEAT):
                            self.px[x, y] = OUT
                            break


def in_ellipse(x, y, cx, cy, rx, ry):
    return ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1.0


def draw_ninja(cv, suit, suit_l, *, bob=0, legs="stand", eyes="open",
               arms="rest", tail=0, lean=0, head_dx=0, head_dy=0):
    oy = bob
    ox = lean
    # ---- piernas ----
    leg_y0 = 25 + oy
    if legs == "stand":
        cv.rect(12 + ox, leg_y0, 14 + ox, 28, suit)
        cv.rect(18 + ox, leg_y0, 20 + ox, 28, suit)
        cv.rect(11 + ox, 29, 14 + ox, 29, OUT)  # pies
        cv.rect(18 + ox, 29, 21 + ox, 29, OUT)
    elif legs == "sit":
        cv.rect(10 + ox, 26 + oy, 22 + ox, 28, suit)
        cv.rect(9 + ox, 29, 13 + ox, 29, OUT)
        cv.rect(19 + ox, 29, 23 + ox, 29, OUT)
    else:  # fases de carrera
        phases = {
            "r0": ((9, 26, 11, 27), (6, 28, 9, 28), (19, 25, 21, 28), (19, 29, 23, 29)),
            "r1": ((11, 25, 13, 28), (10, 29, 13, 29), (17, 25, 19, 27), (18, 27, 21, 27)),
            "r2": ((14, 25, 16, 28), (14, 29, 17, 29), (14, 25, 16, 26), (12, 27, 15, 27)),
            "r3": ((19, 25, 21, 28), (19, 29, 23, 29), (9, 26, 11, 27), (6, 28, 9, 28)),
            "r4": ((17, 25, 19, 27), (18, 27, 21, 27), (11, 25, 13, 28), (10, 29, 13, 29)),
            "r5": ((14, 25, 16, 26), (12, 27, 15, 27), (14, 25, 16, 28), (14, 29, 17, 29)),
        }[legs]
        (a, b, c, d) = phases
        cv.rect(a[0] + ox, a[1] + oy, a[2] + ox, a[3] + oy, suit)
        cv.rect(b[0] + ox, b[1] + oy, b[2] + ox, b[3] + oy, OUT)
        cv.rect(c[0] + ox, c[1] + oy, c[2] + ox, c[3] + oy, suit)
        cv.rect(d[0] + ox, d[1] + oy, d[2] + ox, d[3] + oy, OUT)

    # ---- cuerpo ----
    for y in range(19, 26):
        d = (2 if y < 21 else 1 if y < 23 else 0) if head_dx else 0
        cv.rect(10 + ox + d, y + oy, 22 + ox + d, y + oy, suit)
        if y < 25:
            cv.rect(11 + ox + d, y + oy, 12 + ox + d, y + oy, suit_l)  # brillo
    # fajín azul diagonal
    for i in range(7):
        cv.set(11 + ox + i * 2 // 1 - i, 19 + oy + i, BAND)
        cv.set(12 + ox + i, 19 + oy + i, BAND)
        cv.set(13 + ox + i, 19 + oy + i, BAND_D)
    cv.rect(10 + ox, 24 + oy, 22 + ox, 24 + oy, BAND_D)  # cinturón

    # ---- brazos ----
    if arms == "rest":
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, suit)
        cv.rect(23 + ox, 20 + oy, 24 + ox, 23 + oy, suit)
        cv.set(8 + ox, 24 + oy, SKIN); cv.set(24 + ox, 24 + oy, SKIN)
    elif arms == "run_a":
        cv.rect(6 + ox, 20 + oy, 9 + ox, 21 + oy, suit); cv.set(5 + ox, 21 + oy, SKIN)
        cv.rect(23 + ox, 21 + oy, 25 + ox, 22 + oy, suit); cv.set(26 + ox, 21 + oy, SKIN)
    elif arms == "run_b":
        cv.rect(8 + ox, 21 + oy, 9 + ox, 23 + oy, suit); cv.set(9 + ox, 24 + oy, SKIN)
        cv.rect(23 + ox, 20 + oy, 24 + ox, 23 + oy, suit); cv.set(24 + ox, 24 + oy, SKIN)
    elif arms == "back":  # brazos estirados hacia atrás, en diagonal
        for k in range(7):  # brazo de delante
            x = 10 - k; y = 20 + k // 3
            cv.rect(x + ox, y + oy, x + ox, y + 1 + oy, suit)
        cv.rect(2 + ox, 22 + oy, 3 + ox, 22 + oy, SKIN)
        for k in range(6):  # brazo de detrás (más abajo)
            x = 10 - k; y = 23 + k // 2
            cv.rect(x + ox, y + oy, x + ox, y + 1 + oy, suit_l)
        cv.rect(4 + ox, 26 + oy, 4 + ox, 26 + oy, SKIN)
    elif arms == "punch":
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, suit); cv.set(8 + ox, 24 + oy, SKIN)
        cv.rect(23 + ox, 20 + oy, 28 + ox, 21 + oy, suit)
        cv.rect(29 + ox, 20 + oy, 30 + ox, 21 + oy, SKIN)
    elif arms == "guard":
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, suit); cv.set(8 + ox, 24 + oy, SKIN)
        cv.rect(23 + ox, 20 + oy, 25 + ox, 22 + oy, suit); cv.set(25 + ox, 19 + oy, SKIN)
        cv.set(26 + ox, 19 + oy, SKIN)
    elif arms == "sword":
        cv.rect(8 + ox, 20 + oy, 9 + ox, 23 + oy, suit); cv.set(8 + ox, 24 + oy, SKIN)
        cv.rect(23 + ox, 20 + oy, 25 + ox, 21 + oy, suit)
        cv.rect(25 + ox, 19 + oy, 26 + ox, 20 + oy, SKIN)

    # ---- capucha (cabeza grande chibi) ----
    ox, oy = ox + head_dx, oy + head_dy
    cx, cy, rx, ry = 16.0 + ox, 10.5 + oy, 10.0, 9.0
    for y in range(H):
        for x in range(W):
            if in_ellipse(x, y, cx, cy, rx, ry):
                cv.set(x, y, suit)
    # brillo de la capucha
    for y in range(3 + oy, 8 + oy):
        for x in range(9 + ox, 13 + ox):
            if in_ellipse(x, y, cx - 3, cy - 4, 4.0, 3.2):
                cv.set(x, y, suit_l)

    # ---- abertura de la cara (franja de ojos) ----
    fy0, fy1 = 10 + oy, 14 + oy
    for y in range(fy0, fy1 + 1):
        for x in range(9 + ox, 24 + ox):
            edge = (y in (fy0, fy1)) and (x in (9 + ox, 23 + ox))
            if not edge:
                cv.set(x, y, SKIN)
    cv.rect(10 + ox, fy1, 22 + ox, fy1, SKIN_D)

    # ojos (mirando a la derecha)
    for ex in (14 + ox, 19 + ox):
        if eyes == "open":
            cv.rect(ex, 11 + oy, ex + 1, 13 + oy, EYE)
            cv.set(ex + 1, 11 + oy, SHINE)
        elif eyes == "closed":
            cv.rect(ex - 1, 12 + oy, ex + 2, 12 + oy, EYE)
        elif eyes == "focus":  # ceño fruncido
            cv.rect(ex, 12 + oy, ex + 1, 13 + oy, EYE)
            cv.set(ex + 1, 12 + oy, SHINE)
            cv.set(ex - 1 if ex < 18 + ox else ex + 2, 10 + oy, OUT)
            cv.rect(ex, 11 + oy, ex + 1, 11 + oy, OUT)

    # ---- cinta azul ----
    for y in (6 + oy, 7 + oy, 8 + oy):
        for x in range(W):
            if cv.get(x, y) in (suit, suit_l):
                cv.set(x, y, BAND if y != 8 + oy else BAND_D)
    for x in range(7 + ox, 26 + ox):
        if cv.get(x, 6 + oy) == BAND:
            cv.set(x, 6 + oy, BAND_L) if x % 3 == 0 else None
    # placa metálica
    cv.rect(14 + ox, 6 + oy, 20 + ox, 8 + oy, METAL)
    cv.rect(15 + ox, 7 + oy, 19 + ox, 7 + oy, METAL_D)
    cv.set(15 + ox, 7 + oy, METAL); cv.set(17 + ox, 7 + oy, METAL); cv.set(19 + ox, 7 + oy, METAL)

    # colas de la cinta (detrás, a la izquierda), ondean con "tail"
    tails = {
        0: [(5, 7), (4, 7), (3, 8), (2, 8), (4, 9), (3, 10), (2, 11)],
        1: [(5, 7), (4, 6), (3, 6), (2, 7), (4, 8), (3, 9), (2, 9)],
        2: [(5, 7), (4, 7), (3, 7), (2, 6), (4, 8), (3, 8), (2, 9)],
        3: [(5, 8), (5, 9), (4, 10), (4, 11), (6, 9), (6, 10), (6, 11)],  # caída (quieto)
        # colas largas al viento (carrera)
        4: [(5, 7), (4, 7), (3, 7), (2, 6), (1, 6), (0, 6), (4, 9), (3, 9), (2, 10), (1, 10), (0, 10)],
        5: [(5, 7), (4, 7), (3, 6), (2, 6), (1, 7), (0, 7), (4, 9), (3, 10), (2, 10), (1, 9), (0, 9)],
        6: [(5, 7), (4, 6), (3, 6), (2, 7), (1, 7), (0, 6), (4, 9), (3, 9), (2, 9), (1, 10), (0, 11)],
    }[tail]
    for i, (x, y) in enumerate(tails):
        cv.set(x + ox, y + oy, BAND if i % 2 == 0 else BAND_D)


def draw_sword(cv, phase, raised=True, ox=0, oy=0):
    """Katana diagonal hacia arriba-derecha con llamas azules animadas."""
    hx, hy = 26 + ox, 20 + oy  # mano
    # empuñadura
    cv.set(hx - 1, hy + 1, HILT); cv.set(hx, hy, HILT)
    cv.set(hx + 1, hy - 1, GUARD); cv.set(hx, hy - 1, GUARD); cv.set(hx + 1, hy, GUARD)
    blade = []
    for i in range(2, 12):
        bx, by = hx + i // 2 + 0, hy - i
        blade.append((bx, by))
    hx -= 1
    blade = [(hx + 1 + (i // 5), hy - 2 - i) for i in range(12)]
    for (bx, by) in blade:
        cv.set(bx, by, BLADE)
        cv.set(bx + 1, by, BLADE_D)
    if not raised:
        return
    # llamas
    flick = [(-2, 0), (-1, -1), (-2, -1), (-1, 0)]
    for i, (bx, by) in enumerate(blade):
        w = 1 + ((i + phase) % 3 == 0)
        jit = math.sin((i + phase * 1.7))
        lx = bx - 1 - (1 if jit > 0.3 else 0)
        rx = bx + 2 + (1 if jit < -0.3 else 0)
        if i < 2:
            continue
        cv.set(lx, by, FL_2 if i % 2 else FL_1)
        cv.set(bx - 1, by, FL_1 if (i + phase) % 2 else FL_CORE)
        cv.set(rx, by, FL_2 if (i + phase) % 2 else FL_3)
        if w == 2:
            cv.set(lx - 1, by, FL_3)
            cv.set(rx + 1, by - 1, FL_3)
    # punta llameante
    tx, ty = blade[-1]
    tip = {
        0: [(0, -1, FL_1), (1, -2, FL_2), (0, -3, FL_3), (1, -1, FL_CORE)],
        1: [(1, -1, FL_1), (0, -2, FL_2), (1, -3, FL_2), (0, -4, FL_3)],
        2: [(0, -1, FL_CORE), (1, -2, FL_1), (2, -3, FL_3), (1, -3, FL_2)],
        3: [(1, -1, FL_1), (1, -2, FL_2), (0, -3, FL_3), (2, -2, FL_3)],
    }[phase % 4]
    for dx, dy, c in tip:
        cv.set(tx + dx, ty + dy, c)
    # chispas
    sparks = [(-3, 4), (3, 7), (-2, 9), (4, 2)]
    sx, sy = sparks[phase % 4]
    cv.set(tx + sx, ty + sy, FL_1)


SYM_W = 40  # alert y ask: frames más anchos para que el signo quepa entero

GLYPHS = {
    "!": ["###",
          "###",
          "###",
          "###",
          "###",
          "###",
          "...",
          "###",
          "###"],
    "?": [".###.",
          "##.##",
          "...##",
          "..##.",
          ".##..",
          ".##..",
          ".....",
          ".##..",
          ".##.."],
}


def with_symbol(core, glyph, x, y, color):
    """Pega el ninja (32 px) en un frame de SYM_W px y dibuja el signo a su derecha,
    relleno de un solo color y con contorno oscuro completo."""
    wide = Image.new("RGBA", (SYM_W, H), (0, 0, 0, 0))
    wide.alpha_composite(core.img, (0, 0))
    px = wide.load()
    pts = {(x + cx, y + cy) for cy, row in enumerate(GLYPHS[glyph])
           for cx, ch in enumerate(row) if ch == "#"}
    for (gx, gy) in pts:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (gx + dx, gy + dy)
            if q not in pts and 0 <= q[0] < SYM_W and 0 <= q[1] < H and px[q][3] == 0:
                px[q] = OUT
    for q in pts:
        px[q] = color
    holder = Canvas()
    holder.img = wide
    return holder


def shadow(cv, width=12, ox=0):
    for x in range(16 - width // 2 + ox, 16 + width // 2 + ox):
        if cv.get(x, 30)[3] == 0:
            cv.set(x, 30, SHADOW)
    for x in range(16 - width // 2 + 2 + ox, 16 + width // 2 - 2 + ox):
        if cv.get(x, 31)[3] == 0:
            cv.set(x, 31, SHADOW)


def frame(builder):
    cv = Canvas()
    builder(cv)
    return cv


def build_states(suit, suit_l):
    s = dict(suit=suit, suit_l=suit_l)
    st = {}

    # IDLE: respiración + parpadeo
    def idle(bob, eyes):
        def b(cv):
            draw_ninja(cv, **s, bob=bob, eyes=eyes, tail=3)
            cv.outline(); shadow(cv)
        return b
    st["idle"] = [frame(idle(0, "open")), frame(idle(0, "open")),
                  frame(idle(1, "open")), frame(idle(1, "closed"))]

    # RUN: inclinado hacia delante, brazos atrás, estela de aire (frames 48x32)
    run = []
    for i in range(6):
        def b(cv, i=i):
            draw_ninja(cv, **s, bob=-(i % 3 == 1), legs=f"r{i}", arms="back",
                       tail=4 + i % 3, eyes="focus", lean=1, head_dx=3, head_dy=1)
            cv.outline(); shadow(cv, 10)
        core = frame(b).img
        wide = Image.new("RGBA", (RUN_W, H), (0, 0, 0, 0))
        # imágenes residuales (silueta translúcida)
        mask = core.getchannel("A").point(lambda v: 255 if v > 200 else 0)
        for dx, alpha in ((-7, 110), (-13, 55)):
            ghost = Image.new("RGBA", (W, H), (200, 225, 255, alpha))
            wide.paste(ghost, (RUN_OFF + dx, 0), mask)
        # líneas de viento que se desplazan hacia atrás
        wpx = wide.load()
        lines = [(9, 10), (14, 14), (18, 8), (22, 12), (26, 9)]
        for k, (y, ln) in enumerate(lines):
            x0 = (RUN_OFF + 4) - ((i * 4 + k * 7) % 22) - ln
            for x in range(x0, x0 + ln):
                if 0 <= x < RUN_W:
                    t = (x - x0) / ln
                    wpx[x, y] = (255, 255, 255, int(60 + 150 * t))
        wide.alpha_composite(core, (RUN_OFF, 0))
        holder = Canvas(); holder.img = wide
        run.append(holder)
    st["run"] = run

    # KATANA: desenvainar (2) + llamas en bucle (4)
    kat = []
    def draw1(cv):
        draw_ninja(cv, **s, arms="guard", tail=3, eyes="focus")
        cv.outline(); shadow(cv)
    def draw2(cv):
        draw_ninja(cv, **s, arms="sword", tail=0, eyes="focus")
        draw_sword(cv, 0, raised=False)
        cv.outline(); shadow(cv)
    kat += [frame(draw1), frame(draw2)]
    for p in range(4):
        def b(cv, p=p):
            draw_ninja(cv, **s, arms="sword", tail=p % 3, eyes="focus", bob=(p // 2) % 2)
            draw_sword(cv, p, raised=False, oy=(p // 2) % 2)
            cv.outline()
            draw_sword(cv, p, raised=True, oy=(p // 2) % 2)
            shadow(cv)
        kat.append(frame(b))
    st["katana"] = kat

    # TRAIN (pensando = entrenando): puñetazos alternos + sudor
    tr = []
    for p in range(4):
        def b(cv, p=p):
            punch = p in (1, 3)
            draw_ninja(cv, **s, arms="punch" if punch else "rest",
                       legs="stand", tail=p % 3, eyes="focus", bob=0 if punch else 1)
            cv.outline(); shadow(cv)
            if punch:
                for y in (19, 22):
                    cv.set(31, y, WHITE)
            if p >= 2:
                cv.set(25, 4 + p, SWEAT); cv.set(25, 5 + p, SWEAT)
        tr.append(frame(b))
    st["train"] = tr

    # SLEEP: sentado, ojos cerrados, Zzz subiendo
    sl = []
    zpos = [[(23, 5)], [(25, 2), (23, 6)], [(27, 0), (24, 3)], [(26, 0)]]
    for p in range(4):
        def b(cv, p=p):
            draw_ninja(cv, **s, eyes="closed", legs="sit", tail=3, bob=1 + (p % 2))
            cv.outline(); shadow(cv, 14)
            for (zx, zy) in zpos[p]:
                for (dx, dy) in ((0, 0), (1, 0), (2, 0), (3, 0), (2, 1), (1, 2), (0, 3), (1, 3), (2, 3), (3, 3)):
                    cv.set(zx + dx, zy + dy, ZCOL)
        sl.append(frame(b))
    st["sleep"] = sl

    # ALERT: error, Claude necesita ayuda. Exclamación roja que salta.
    # ASK: Claude te pregunta o pide permiso. Interrogación azul que salta.
    def standing(p):
        def b(cv):
            draw_ninja(cv, **s, eyes="open", tail=3, bob=p % 2)
            cv.outline(); shadow(cv)
        return b
    bounce = (2, 1, 2, 3)
    al = [with_symbol(frame(standing(p)), "!", 31, bounce[p],
                      ALERT_L if p == 1 else ALERT) for p in range(4)]
    st["ask"] = [with_symbol(frame(standing(p)), "?", 30, bounce[p],
                             ASK_L if p == 1 else ASK) for p in range(4)]
    st["alert"] = al
    return st


RUN_W, RUN_OFF = 48, 16  # la carrera usa frames más anchos para la estela

FPS = {"idle": 4, "run": 12, "katana": 8, "train": 6, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}  # la katana repite solo las llamas


def main(out):
    sp = Path(out)
    sp.mkdir(parents=True, exist_ok=True)
    for old in sp.glob("ninja_*.png"):
        old.unlink()
    manifest = {"id": "ninja", "prefix": "ninja", "name": {"es": "Ninja", "en": "Ninja"},
                "frameSize": [W, H], "variants": list(VARIANTS), "states": {}}
    for name in VARIANTS:
        apply_variant(name)
        states = build_states(SUIT + (255,), SUIT_L + (255,))
        for st, frames in states.items():
            fw = frames[0].img.width
            sheet = Image.new("RGBA", (fw * len(frames), H), (0, 0, 0, 0))
            for i, f in enumerate(frames):
                sheet.paste(f.img, (i * fw, 0))
            sheet.save(sp / f"ninja_{name}_{st}.png")
            manifest["states"][st] = {
                "frames": len(frames), "fps": FPS[st], "loopFrom": LOOP_FROM.get(st, 0),
                "frameSize": [fw, H],
                # x del centro del ninja dentro del frame (para alinear al cambiar de estado)
                "anchorX": RUN_OFF + 16 if st == "run" else 16,
            }
    (sp / "manifest.json").write_text(json.dumps(manifest, indent=2))
    return manifest


if __name__ == "__main__":
    print(json.dumps(main(sys.argv[1] if len(sys.argv) > 1 else "assets/avatars/ninja"), indent=1))
