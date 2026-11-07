"""Piloto de F1: monoplaza genérico (sin marcas) del color de la sesión, piloto con casco."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, EYE, SKIN, frame, holder,
                  in_ellipse, outline, palette, shade, shadow, speed_lines, symbols, wframe,
                  widen, zzz, NO_OUTLINE)

ID = "piloto"
NAMES = {"es": "Piloto de F1", "en": "F1 driver"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 8, "train": 4, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

TIRE = (30, 30, 36, 255)
TIRE_L = (70, 72, 84, 255)
HUB = (170, 176, 190, 255)
CARBON = (44, 46, 56, 255)
HELMET = (244, 246, 250, 255)
VISOR = (30, 40, 70, 255)
VISOR_L = (90, 130, 200, 255)
SUIT = (240, 240, 244, 255)
FLAME = [(255, 240, 200, 255), (255, 190, 70, 255), (255, 110, 30, 255)]
SMOKE = (200, 206, 218)
CHECK_A, CHECK_B = (250, 250, 252, 255), (24, 24, 30, 255)
POLE = (150, 156, 170, 255)
NO_OUTLINE.update({FLAME[0], FLAME[1], FLAME[2]})


def wheel(cv, cx, cy, r, rot):
    for y in range(cy - r - 1, cy + r + 2):
        for x in range(cx - r - 1, cx + r + 2):
            if in_ellipse(x, y, cx + 0.5, cy + 0.5, r + 0.5, r + 0.5):
                cv.set(x, y, TIRE)
    cv.rect(cx - 1, cy - 1, cx + 1, cy + 1, HUB)
    spoke = [(0, -r), (r, 0), (0, r), (-r, 0)][rot % 4]
    cv.set(cx + spoke[0], cy + spoke[1], TIRE_L)


def car(cv, col, rot=0, oy=0):
    b, bd, bl = col
    # alerón trasero
    cv.rect(2, 14 + oy, 6, 16 + oy, CARBON)
    cv.rect(2, 14 + oy, 6, 14 + oy, b)
    cv.rect(4, 17 + oy, 4, 20 + oy, CARBON)
    # carrocería
    cv.rect(4, 20 + oy, 22, 24 + oy, b)
    cv.rect(4, 20 + oy, 22, 20 + oy, bl)
    cv.rect(22, 22 + oy, 28, 24 + oy, b)          # morro
    cv.rect(22, 22 + oy, 28, 22 + oy, bl)
    cv.rect(4, 24 + oy, 28, 24 + oy, bd)
    cv.rect(9, 19 + oy, 13, 19 + oy, bd)          # toma de aire detrás del piloto
    cv.rect(8, 21 + oy, 11, 23 + oy, WHITE)       # número
    cv.rect(9, 22 + oy, 10, 22 + oy, bd)
    cv.rect(25, 25 + oy, 30, 26 + oy, CARBON)     # alerón delantero
    cv.rect(29, 24 + oy, 30, 24 + oy, b)
    wheel(cv, 7, 26, 3, rot)
    wheel(cv, 23, 27, 2, rot)


def driver(cv, col, *, visor="down", oy=0, arm=None, tilt=0):
    b, bd, bl = col
    hx, hy = 16 + tilt, 15 + oy
    # cuerpo asomando (mono blanco)
    cv.rect(13, 18 + oy, 18, 20 + oy, SUIT)
    # casco
    for y in range(hy - 5, hy + 5):
        for x in range(hx - 5, hx + 6):
            if in_ellipse(x, y, hx + 0.5, hy, 5.0, 4.6):
                cv.set(x, y, HELMET)
    cv.rect(hx - 4, hy - 2, hx + 3, hy - 2, b)    # franja del color del coche
    if visor == "down":
        cv.rect(hx, hy - 1, hx + 4, hy + 1, VISOR)
        cv.set(hx + 3, hy - 1, VISOR_L)
    elif visor == "up":
        cv.rect(hx, hy - 1, hx + 4, hy + 1, SKIN)
        cv.set(hx + 2, hy, EYE); cv.set(hx + 4, hy, EYE)
        cv.rect(hx, hy - 3, hx + 4, hy - 3, VISOR)
    elif visor == "sleep":
        cv.rect(hx, hy - 1, hx + 4, hy + 1, VISOR)
    # halo
    cv.rect(12, 16 + oy, 12, 19 + oy, CARBON)
    cv.rect(12, 16 + oy, 20, 16 + oy, CARBON) if False else None
    if arm == "helmet":
        cv.rect(19, 17 + oy, 21, 18 + oy, SUIT); cv.rect(21, 14 + oy, 22, 16 + oy, SUIT)
        cv.rect(20, 12 + oy, 21, 13 + oy, (40, 40, 50, 255))   # guante
    elif arm == "flag":
        cv.rect(19, 15 + oy, 20, 18 + oy, SUIT); cv.rect(21, 12 + oy, 22, 14 + oy, SUIT)
        cv.rect(22, 10 + oy, 23, 11 + oy, (40, 40, 50, 255))


def flag(cv, phase):
    """Bandera a cuadros que ondea (40 px)."""
    cv.rect(23, 1, 23, 12, POLE)
    cells = {}
    for i in range(12):
        dy = 0 if i < 4 else (0, 1, 0, -1)[(phase + i // 4) % 4]
        for j in range(8):
            c = CHECK_A if ((i // 2) + (j // 2)) % 2 == 0 else CHECK_B
            cells[(24 + i, 1 + j + dy)] = c
    for (x, y) in cells:   # contorno de la tela
        for dx, dy in ((1, 0), (0, 1), (0, -1)):
            q = (x + dx, y + dy)
            if q not in cells and cv.get(*q)[3] == 0:
                cv.set(*q, OUT)
    for q, c in cells.items():
        cv.set(*q, c)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: parado en boxes, el piloto mira al frente
    def idle(p):
        def b(cv):
            driver(cv, col, oy=(p == 2))
            car(cv, col)
            outline(cv); shadow(cv, 26)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: a toda velocidad, ruedas girando, llamas y humo por detrás (48 px)
    run = []
    for i in range(6):
        def b(cv, i=i):
            driver(cv, col, oy=-(i % 2))
            car(cv, col, rot=i, oy=-(i % 2))
            outline(cv); shadow(cv, 24)
        wide = holder(widen(frame(b), RUN_W, RUN_OFF))
        fy = 21 - (i % 2)
        for k in range((5, 7, 6, 4, 7, 5)[i]):
            wide.set(RUN_OFF + 1 - k, fy, FLAME[min(2, k // 2)])
            if k < 3:
                wide.set(RUN_OFF + 1 - k, fy + 1, FLAME[min(2, k // 2 + 1)])
        for k in range(3):
            x = RUN_OFF - 4 - ((i * 3 + k * 6) % 14)
            for dx in (-1, 0, 1):
                if wide.get(x + dx, 23 + k % 2)[3] == 0:
                    wide.set(x + dx, 23 + k % 2, SMOKE + (140 - k * 35,))
        speed_lines(wide, i, rows=((8, 10), (12, 14), (17, 8)))
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): ondea la bandera a cuadros desde el coche (40 px)
    def wave(p):
        def b(cv):
            driver(cv, col, visor="up" if p >= 1 else "down", arm="flag")
            car(cv, col)
            outline(cv)
            flag(cv, p)
            shadow(cv, 26)
        return wframe(WIDE, b)
    st["katana"] = [wave(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): se sube la visera, se ajusta el casco y la baja
    seq = ["down", "up", "up", "down"]
    def adjust(p):
        def b(cv):
            driver(cv, col, visor=seq[p], arm="helmet" if p in (1, 2) else None)
            car(cv, col)
            outline(cv); shadow(cv, 26)
        return frame(b)
    st["train"] = [adjust(p) for p in range(4)]

    # SLEEP: dormido en el coche, casco inclinado
    def sleep(p):
        def b(cv):
            driver(cv, col, visor="sleep", oy=1 + (p % 2), tilt=-1)
            car(cv, col)
            outline(cv); shadow(cv, 26)
            zzz(cv, p, base=(22, 4))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (driver(cv, col), car(cv, col), outline(cv),
                                                  shadow(cv, 26)))))
    return st
