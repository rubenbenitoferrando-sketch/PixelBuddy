"""Bombero: uniforme y casco del color de la sesión, con bandas reflectantes."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, SWEAT, arm, frame, head, holder,
                  in_ellipse, legs, outline, palette, shade, shadow, speed_lines, symbols, torso,
                  wframe, widen, zzz, NO_OUTLINE)

ID = "bombero"
NAMES = {"es": "Bombero", "en": "Firefighter"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 8, "train": 5, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

HAIR = (90, 56, 34, 255)
REFLECT = (246, 222, 70, 255)
BOOTS = (34, 34, 40, 255)
GLOVE = (60, 56, 52, 255)
GOLD = (255, 204, 70, 255)
HOSE = (150, 150, 160, 255)
HOSE_D = (100, 100, 112, 255)
NOZZLE = (214, 170, 70, 255)
WATER = (150, 210, 255, 255)
WATER_L = (225, 245, 255, 255)
FIRE = [(255, 250, 200, 255), (255, 200, 60, 255), (255, 120, 30, 255), (220, 50, 30, 255)]
STEAM = (235, 238, 244, 170)
NO_OUTLINE.update({WATER, WATER_L, STEAM, *FIRE})


def helmet(cv, col, ox=0, oy=0):
    b, bd, bl = col
    hb = shade(bd, 0.9)
    for y in range(0 + oy, 9 + oy):
        for x in range(6 + ox, 27 + ox):
            if in_ellipse(x, y, 16 + ox, 7.5 + oy, 8.0, 6.5) and y <= 7 + oy:
                cv.set(x, y, b)
    cv.rect(10 + ox, 2 + oy, 13 + ox, 2 + oy, bl)               # brillo
    cv.rect(6 + ox, 8 + oy, 25 + ox, 8 + oy, hb)                # ala
    cv.rect(4 + ox, 8 + oy, 8 + ox, 9 + oy, hb)                 # ala trasera, más larga
    cv.rect(20 + ox, 3 + oy, 22 + ox, 6 + oy, GOLD)             # escudo
    cv.set(21 + ox, 4 + oy, b)


def firefighter(cv, col, *, legs_pose="stand", arm_f="rest", arm_b="rest", eyes="open",
                mouth="smile", oy=0, hose=False):
    b, bd, bl = col
    legs(cv, legs_pose, pants=bd, shoes=BOOTS)
    if legs_pose == "stand":
        cv.rect(12, 27, 14, 27, REFLECT); cv.rect(18, 27, 20, 27, REFLECT)
    arm(cv, arm_b, 0, oy, sleeve=b, skin=GLOVE, side="back")
    torso(cv, 0, oy, shirt=b, shirt_d=bd)
    cv.rect(11, 21 + oy, 21, 21 + oy, REFLECT)
    cv.rect(11, 23 + oy, 21, 23 + oy, REFLECT)
    if hose:   # manguera enrollada al hombro
        cv.rect(8, 16 + oy, 13, 17 + oy, HOSE); cv.rect(7, 18 + oy, 8, 22 + oy, HOSE)
        cv.rect(9, 23 + oy, 12, 24 + oy, HOSE_D)
    arm(cv, arm_f, 0, oy, sleeve=b, skin=GLOVE)
    head(cv, 0, oy, hair=HAIR, eyes=eyes, mouth=mouth)
    helmet(cv, col, 0, oy)


def fire(cv, x0, size, phase):
    """Llamas de tamaño 0-3 con la base en y=29 (7 columnas)."""
    if size <= 0:
        return
    top = (0, 5, 9, 13)[size]
    for i in range(7):
        h = top - abs(i - 3) * (2 if size > 1 else 1) + ((i + phase) % 2)
        for k in range(max(1, h)):
            t = k / max(1, h)
            c = FIRE[3] if t > 0.75 else FIRE[2] if t > 0.45 else FIRE[1]
            if t < 0.25 and 2 <= i <= 4:
                c = FIRE[0]
            cv.set(x0 + i, 29 - k, c)


def build_states(variant):
    col = palette(variant)
    st = {}

    st["idle"] = [frame(lambda cv, p=p: (firefighter(cv, col, eyes="closed" if p == 3 else "open",
                                                     oy=(p == 2)), outline(cv), shadow(cv, 14)))
                  for p in range(4)]

    # RUN: corre con la manguera al hombro (48 px)
    run = []
    for i in range(6):
        core = frame(lambda cv, i=i: (firefighter(cv, col, legs_pose=f"r{i}", hose=True,
                                                  arm_f="forward_low" if i < 3 else "rest",
                                                  eyes="focus", oy=-(i % 3 == 1)),
                                      outline(cv), shadow(cv, 12)))
        wide = holder(widen(core, RUN_W, RUN_OFF))
        speed_lines(wide, i)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): apaga un fuego con el chorro de agua (40 px)
    def spray(p):
        def b(cv):
            firefighter(cv, col, arm_f="forward", eyes="focus" if p < 2 else "happy",
                        mouth="smile" if p < 2 else "open")
            cv.rect(26, 20, 27, 21, NOZZLE)
            outline(cv)
            fire(cv, 31, (3, 2, 0, 0, 0, 0)[p], p)
            if p >= 1:   # chorro en arco
                for k in range(10):
                    x = 28 + k
                    y = 20 + (k * k) // 16 + ((k + p) % 3 == 0)
                    cv.set(x, y, WATER_L if (k + p) % 2 else WATER)
                    cv.set(x, y + 1, WATER)
            if p >= 2:   # vapor
                for (x, y) in (((33, 24), (35, 21), (37, 25)), ((34, 22), (36, 19), (32, 20)))[p % 2]:
                    cv.set(x, y, STEAM); cv.set(x + 1, y, STEAM)
            shadow(cv, 14)
        return wframe(WIDE, b)
    st["katana"] = [spray(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): se seca el sudor de la frente
    def wipe(p):
        def b(cv):
            firefighter(cv, col, arm_f="wipe" if p in (1, 2) else "rest", eyes="closed" if p in (1, 2) else "open")
            outline(cv)
            if p in (0, 3):
                cv.set(24, 9 + p, SWEAT); cv.set(24, 10 + p, SWEAT)
            shadow(cv, 14)
        return frame(b)
    st["train"] = [wipe(p) for p in range(4)]

    # SLEEP: sentado con el casco tapándole los ojos
    def sleep(p):
        def b(cv):
            legs(cv, "sit", pants=col[1], shoes=BOOTS)
            oy = 1 + (p % 2)
            arm(cv, "rest", 0, oy, sleeve=col[0], skin=GLOVE, side="back")
            torso(cv, 0, oy, shirt=col[0], shirt_d=col[1])
            cv.rect(11, 21 + oy, 21, 21 + oy, REFLECT)
            arm(cv, "forward_low", 0, oy, sleeve=col[0], skin=GLOVE)
            head(cv, 0, oy, hair=HAIR, eyes="closed", mouth="o")
            helmet(cv, col, 0, oy + 3)
            outline(cv); shadow(cv, 16)
            zzz(cv, p, base=(25, 3))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (firefighter(cv, col), outline(cv), shadow(cv, 14)))))
    return st
