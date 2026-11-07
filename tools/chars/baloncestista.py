"""Baloncestista: camiseta de tirantes y pantalón del color de la sesión, balón naranja."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, arm, frame, head, holder, legs,
                  outline, palette, shade, shadow, speed_lines, symbols, torso, wframe, widen, zzz,
                  NO_OUTLINE)

ID = "baloncestista"
NAMES = {"es": "Baloncestista", "en": "Basketball player"}
VARIANTS = COLORS
FPS = {"idle": 5, "run": 12, "katana": 7, "train": 8, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

SKIN = (178, 122, 86, 255)
SKIN_D = (140, 92, 62, 255)
HAIR = (26, 22, 22, 255)
SNEAK = (240, 240, 244, 255)
BALL = (236, 120, 40, 255)
BALL_L = (255, 170, 90, 255)
SEAM = (90, 40, 20, 255)
RIM = (240, 90, 30, 255)
BOARD = (244, 246, 250, 255)
NET = (225, 230, 240, 255)
SWISH = (255, 255, 255, 200)
NO_OUTLINE.update({NET, SWISH})


def ball(cv, x, y, rot=0):
    for dy, row in enumerate([".###.", "#####", "#####", "#####", ".###."]):
        for dx, ch in enumerate(row):
            if ch == "#":
                cv.set(x + dx, y + dy, BALL)
    cv.set(x + 1, y + 1, BALL_L)
    if rot % 2 == 0:
        for d in range(5):
            cv.set(x + 2, y + d, SEAM) if 0 < d < 4 or True else None
        cv.set(x + 1, y + 2, SEAM); cv.set(x + 3, y + 2, SEAM)
    else:
        for d in range(5):
            cv.set(x + d, y + 2, SEAM)
        cv.set(x + 2, y + 1, SEAM); cv.set(x + 2, y + 3, SEAM)
    for q in ((x, y), (x + 4, y), (x, y + 4), (x + 4, y + 4)):
        if cv.get(*q)[:3] == SEAM[:3]:
            cv.set(*q, (0, 0, 0, 0))


def player(cv, col, *, legs_pose="stand", arm_f="rest", arm_b="rest", eyes="open",
           mouth="smile", oy=0):
    b, bd, bl = col
    legs(cv, legs_pose, 0, oy if legs_pose == "jump" else 0, pants=bd, shoes=SNEAK)
    arm(cv, arm_b, 0, oy, sleeve=SKIN, skin=SKIN, side="back")
    torso(cv, 0, oy, shirt=b, shirt_d=bd)
    cv.rect(13, 18 + oy, 13, 19 + oy, SKIN); cv.rect(19, 18 + oy, 19, 19 + oy, SKIN)   # tirantes
    cv.rect(15, 20 + oy, 17, 21 + oy, WHITE)                                          # número
    cv.set(16, 21 + oy, b)
    arm(cv, arm_f, 0, oy, sleeve=SKIN, skin=SKIN)
    head(cv, 0, oy, skin=SKIN, hair=HAIR, eyes=eyes, mouth=mouth)
    cv.rect(10, 6 + oy, 22, 6 + oy, WHITE) if False else None


def hoop(cv, net_frame=0):
    cv.rect(35, 1, 36, 12, BOARD)                    # tablero
    cv.rect(35, 4, 35, 8, RIM)
    cv.rect(37, 2, 37, 16, (120, 126, 140, 255))     # poste
    cv.rect(29, 9, 34, 9, RIM)                       # aro
    for k, x in enumerate(range(29, 35)):            # red
        for y in range(10, 14):
            if (x + y + net_frame) % 2 == 0:
                cv.set(x, y, NET)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: bota el balón despacio
    bounce = [20, 23, 25, 23]
    def idle(p):
        def b(cv):
            player(cv, col, arm_f="forward_low", eyes="closed" if p == 3 else "open")
            ball(cv, 25, bounce[p], p)
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: corre botando el balón (48 px)
    run = []
    by = [21, 24, 25, 21, 24, 25]
    for i in range(6):
        def b(cv, i=i):
            player(cv, col, legs_pose=f"r{i}", arm_f="forward_low", arm_b="rest" if i < 3 else "forward",
                   eyes="focus", oy=-(i % 3 == 1))
            ball(cv, 26, by[i], i)
            outline(cv); shadow(cv, 12)
        wide = holder(widen(frame(b), RUN_W, RUN_OFF))
        speed_lines(wide, i)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): tira y encesta en el aro (40 px)
    def shot(p):
        def b(cv):
            hoop(cv, p)
            if p == 0:
                player(cv, col, legs_pose="jump", arm_f="raise", eyes="focus", oy=-2)
                ball(cv, 25, 2, 0)
            elif p == 1:
                player(cv, col, legs_pose="stand", arm_f="front_up", eyes="focus")
                ball(cv, 27, 2, 1)
            else:
                player(cv, col, legs_pose="jump" if p % 2 else "stand", arm_f="raise",
                       eyes="happy", mouth="open", oy=-2 if p % 2 else 0)
                ys = {2: 9, 3: 13, 4: 20, 5: 25}
                ball(cv, 30, ys[p], p)
            outline(cv)
            if p == 2:   # ¡swish!
                for (x, y) in ((27, 8), (36, 13), (27, 13), (28, 15)):
                    cv.set(x, y, SWISH)
            hoop(cv, p) if p in (2, 3) else None    # la red por delante del balón
            shadow(cv, 12)
        return wframe(WIDE, b)
    st["katana"] = [shot(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): gira el balón en un dedo
    def spin(p):
        def b(cv):
            player(cv, col, arm_f="raise", eyes="focus")
            ball(cv, 25, 4, p)
            outline(cv)
            for (x, y) in (((24, 6), (31, 5)), ((30, 7), (24, 4)), ((24, 5), (31, 6)), ((30, 4), (24, 7)))[p]:
                cv.set(x, y, SWISH)
            shadow(cv, 14)
        return frame(b)
    st["train"] = [spin(p) for p in range(4)]

    # SLEEP: sentado con el balón en el regazo
    def sleep(p):
        def b(cv):
            legs(cv, "sit", pants=col[1], shoes=SNEAK)
            oy = 1 + (p % 2)
            arm(cv, "rest", 0, oy, sleeve=SKIN, skin=SKIN, side="back")
            torso(cv, 0, oy, shirt=col[0], shirt_d=col[1])
            head(cv, 0, oy, skin=SKIN, hair=HAIR, eyes="closed", mouth="o")
            ball(cv, 19, 21, 0)
            arm(cv, "forward_low", 0, oy, sleeve=SKIN, skin=SKIN)
            outline(cv); shadow(cv, 16)
            zzz(cv, p, base=(23, 3))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (player(cv, col, arm_f="forward_low"),
                                                  ball(cv, 25, 23, 0), outline(cv), shadow(cv, 14)))))
    return st
