"""Programador: sentado en su silla de oficina con ruedas. Cambia el color del portátil."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, SHINE, WHITE, arm, frame, head, holder, legs,
                  outline, palette, shade, shadow, speed_lines, symbols, torso, wframe, widen, zzz,
                  NO_OUTLINE)

ID = "programador"
NAMES = {"es": "Programador", "en": "Developer"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 6, "train": 5, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

HAIR = (70, 46, 34, 255)
HOODIE = (92, 100, 118, 255)
JEANS = (62, 84, 128, 255)
SNEAK = (232, 232, 238, 255)
CHAIR = (52, 56, 70, 255)
CHAIR_L = (84, 90, 110, 255)
METAL = (190, 196, 210, 255)
METAL_D = (130, 136, 152, 255)
WHEEL = (30, 30, 38, 255)
SCREEN = (22, 32, 54, 255)
CHECK = (90, 230, 120, 255)
MUG = (240, 240, 244, 255)
COFFEE = (120, 74, 40, 255)
STEAM = (220, 226, 236, 170)
BITS = (120, 230, 150, 200)
NO_OUTLINE.update({STEAM, BITS})


def chair(cv, ox=0, oy=0, roll=0):
    cv.rect(6 + ox, 13 + oy, 8 + ox, 23 + oy, CHAIR)            # respaldo
    cv.rect(7 + ox, 14 + oy, 7 + ox, 21 + oy, CHAIR_L)
    cv.rect(9 + ox, 24 + oy, 20 + ox, 25 + oy, CHAIR)           # asiento
    cv.rect(13 + ox, 26 + oy, 14 + ox, 27 + oy, METAL_D)        # pie
    cv.rect(9 + ox, 28 + oy, 19 + ox, 28 + oy, METAL_D)         # base
    for x in (9, 14, 19):                                       # ruedas
        cv.set(x + ox, 29 + oy, WHEEL)
        cv.set(x + ox + (1 if roll % 2 else -1), 29 + oy, WHEEL)


def laptop(cv, col, ox=0, oy=0):
    b, bd, bl = col
    cv.rect(17 + ox, 21 + oy, 25 + ox, 21 + oy, METAL)          # teclado
    cv.rect(17 + ox, 22 + oy, 25 + ox, 22 + oy, METAL_D)
    cv.rect(25 + ox, 14 + oy, 26 + ox, 21 + oy, b)              # tapa (vista de lado)
    cv.rect(25 + ox, 14 + oy, 25 + ox, 21 + oy, bl)
    cv.set(26 + ox, 14 + oy, bd)


def typing_arms(cv, tap):
    # brazo de detrás hasta el teclado
    cv.rect(10, 20, 15, 21, shade(HOODIE, 0.8))
    cv.rect(16, 20 - (tap == 0), 17, 20 - (tap == 0), (226, 186, 145, 255))
    # brazo de delante
    cv.rect(21, 19, 22, 21, HOODIE)
    cv.rect(23, 20 - (tap == 1), 24, 20 - (tap == 1), (242, 199, 155, 255))


def seated(cv, col, *, tap=0, eyes="open", mouth="smile", roll=0, head_ox=0, head_oy=0,
           arms="type", mug=None):
    chair(cv, roll=roll)
    legs(cv, "chair", pants=JEANS, shoes=SNEAK)
    torso(cv, shirt=HOODIE, collar=shade(HOODIE, 1.2))
    cv.rect(15, 18, 17, 19, shade(HOODIE, 0.85))                 # capucha detrás del cuello
    laptop(cv, col)
    if arms == "type":
        typing_arms(cv, tap)
    elif arms == "mug":
        cv.rect(10, 20, 15, 21, shade(HOODIE, 0.8))
        cv.rect(16, 20, 17, 20, (226, 186, 145, 255))
        mx, my = mug
        cv.rect(21, 19, 22, 21, HOODIE)
        cv.rect(21, my + 2, 22, 20, HOODIE)
        cv.rect(mx, my, mx + 2, my + 3, MUG)                      # taza
        cv.rect(mx, my, mx + 2, my, COFFEE)
        cv.set(mx + 3, my + 1, MUG); cv.set(mx + 3, my + 2, MUG)  # asa
        cv.set(mx - 1, my + 2, (242, 199, 155, 255))
    elif arms == "sleep":
        cv.rect(10, 20, 17, 21, shade(HOODIE, 0.8))
        cv.rect(18, 20, 24, 20, HOODIE)
    head(cv, head_ox, head_oy, hair=HAIR, eyes=eyes, mouth=mouth)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: teclea tranquilo y parpadea
    def idle(p):
        def b(cv):
            seated(cv, col, tap=p % 2, eyes="closed" if p == 3 else "open")
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: rueda en la silla tecleando rapidísimo; deja un rastro de bits
    run = []
    for i in range(6):
        core = frame(lambda cv, i=i: (seated(cv, col, tap=i % 2, eyes="focus", roll=i),
                                      outline(cv), shadow(cv, 12)))
        wide = holder(widen(core, RUN_W, RUN_OFF))
        for k in range(5):
            x = RUN_OFF + 4 - ((i * 3 + k * 7) % 20)
            y = 9 + (k * 5) % 14
            if (i + k) % 2:
                wide.set(x, y, BITS); wide.set(x, y + 1, BITS)            # "1"
            else:
                for q in ((x, y), (x + 1, y), (x, y + 1), (x + 1, y + 1)):  # "0"
                    wide.set(*q, BITS)
        speed_lines(wide, i)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): gira el portátil y la pantalla muestra un ✓ verde (40 px)
    def show(p):
        def b(cv):
            chair(cv)
            legs(cv, "chair", pants=JEANS, shoes=SNEAK)
            torso(cv, shirt=HOODIE)
            happy = p >= 1
            head(cv, hair=HAIR, eyes="happy" if happy else "open", mouth="open" if happy else "smile")
            bb, bd, bl = col
            if p == 0:   # lo levanta (aún de lado)
                cv.rect(21, 17, 23, 19, HOODIE); cv.rect(24, 16, 25, 16, (242, 199, 155, 255))
                cv.rect(24, 15, 32, 15, METAL); cv.rect(32, 8, 33, 15, bb)
            else:        # lo enseña de frente
                cv.rect(21, 16, 23, 19, HOODIE)
                cv.rect(24, 4, 35, 13, bb)                    # marco
                cv.rect(25, 5, 34, 12, SCREEN)                # pantalla
                cv.rect(24, 4, 35, 4, bl)
                cv.rect(24, 14, 35, 14, METAL)                # teclado
                cv.set(24, 15, (242, 199, 155, 255)); cv.set(35, 15, (242, 199, 155, 255))
                if p >= 2:
                    for (x, y) in ((27, 9), (28, 10), (29, 11), (30, 10), (31, 9), (32, 8), (33, 7)):
                        cv.set(x, y, CHECK); cv.set(x, y - 1, CHECK)
            outline(cv)
            if p >= 2:   # destellos
                for (x, y) in ((22, 3), (37, 6), (36, 1), (23, 9))[p % 2::2]:
                    cv.set(x, y, WHITE)
            shadow(cv, 14)
        return wframe(WIDE, b)
    st["katana"] = [show(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): bebe café con vapor
    mugs = [(23, 19), (23, 16), (22, 13), (22, 13)]
    def coffee(p):
        def b(cv):
            seated(cv, col, arms="mug", mug=mugs[p], eyes="closed" if p == 3 else "open",
                   mouth="o" if p >= 2 else "smile")
            outline(cv); shadow(cv, 14)
            mx, my = mugs[p]
            for k in range(2):
                cv.set(mx + 1 + ((p + k) % 2), my - 2 - k * 2, STEAM)
        return frame(b)
    st["train"] = [coffee(p) for p in range(4)]

    # SLEEP: cabeza sobre el teclado
    def sleep(p):
        def b(cv):
            seated(cv, col, arms="sleep", eyes="closed", mouth="o", head_ox=3, head_oy=4 + (p % 2 == 1) * 0)
            outline(cv); shadow(cv, 14)
            zzz(cv, p, base=(24, 4))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (seated(cv, col, tap=p % 2), outline(cv), shadow(cv, 14)))))
    return st
