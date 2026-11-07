"""Tenista: ropa blanca y cinta en la frente. Cambia el color de la raqueta."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, SKIN, arm, frame, head, holder,
                  in_ellipse, legs, outline, palette, shade, shadow, speed_lines, symbols, torso,
                  wframe, widen, zzz, NO_OUTLINE)

ID = "tenista"
NAMES = {"es": "Tenista", "en": "Tennis player"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 8, "train": 6, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

HAIR = (214, 170, 90, 255)
POLO = (246, 246, 250, 255)
POLO_D = (206, 210, 222, 255)
SHORTS = (236, 238, 244, 255)
SNEAK = (244, 244, 248, 255)
SOCK = (250, 250, 252, 255)
BAND = (230, 60, 70, 255)
STRINGS = (232, 236, 240, 255)
GRIP = (50, 50, 60, 255)
TBALL = (210, 240, 70, 255)
TOWEL = (240, 242, 246, 255)
TOWEL_D = (196, 202, 214, 255)
SPARK = (255, 240, 150, 255)
NO_OUTLINE.update({SPARK})


def racket(cv, col, hx, hy, direction="up"):
    """Raqueta con el mango en la mano (hx, hy). direction: up, down, forward."""
    b, bd, bl = col
    if direction == "up":
        cv.rect(hx, hy - 3, hx, hy, GRIP); cx, cy, rx, ry = hx + 0.5, hy - 7.5, 2.6, 3.6
    elif direction == "down":
        cv.rect(hx, hy, hx, hy + 2, GRIP); cx, cy, rx, ry = hx + 0.5, hy + 6.5, 2.6, 3.6
    else:
        cv.rect(hx, hy, hx + 2, hy, GRIP); cx, cy, rx, ry = hx + 6.5, hy + 0.5, 3.6, 2.6
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if in_ellipse(x, y, cx, cy, rx, ry):
                inner = in_ellipse(x, y, cx, cy, rx - 1, ry - 1)
                cv.set(x, y, (STRINGS if (x + y) % 2 else shade(STRINGS, 0.85)) if inner else b)


def ball(cv, x, y):
    cv.rect(x, y, x + 1, y + 1, TBALL)


def player(cv, col, *, legs_pose="stand", arm_f="rest", arm_b="rest", eyes="open",
           mouth="smile", oy=0):
    legs(cv, legs_pose, 0, oy if legs_pose == "jump" else 0, pants=SHORTS, shoes=SNEAK, socks=SOCK)
    arm(cv, arm_b, 0, oy, sleeve=POLO, side="back")
    torso(cv, 0, oy, shirt=POLO, shirt_d=POLO_D, collar=POLO_D)
    arm(cv, arm_f, 0, oy, sleeve=POLO)
    head(cv, 0, oy, hair=HAIR, eyes=eyes, mouth=mouth)
    for x in range(9, 24):                      # cinta en la frente
        if cv.get(x, 6 + oy)[3] > 0:
            cv.set(x, 6 + oy, BAND)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: de pie con la raqueta abajo
    def idle(p):
        def b(cv):
            player(cv, col, eyes="closed" if p == 3 else "open", oy=(p == 2))
            racket(cv, col, 23, 24 + (p == 2), "down")
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: corre de lado con la raqueta preparada (48 px)
    run = []
    for i in range(6):
        def b(cv, i=i):
            player(cv, col, legs_pose=f"r{i}", arm_f="forward_low", eyes="focus", oy=-(i % 3 == 1))
            racket(cv, col, 26, 22 - (i % 3 == 1), "forward")
            outline(cv); shadow(cv, 12)
        wide = holder(widen(frame(b), RUN_W, RUN_OFF))
        speed_lines(wide, i)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): ¡punto! puño en alto con la raqueta y salto (40 px)
    def point(p):
        def b(cv):
            up = p >= 2 and p % 2 == 0
            oy = -2 if up else 0
            if p == 0:   # remate
                player(cv, col, arm_f="raise", eyes="focus")
                racket(cv, col, 26, 10, "up")
                ball(cv, 33, 4)
            else:
                player(cv, col, legs_pose="jump" if up else "stand", arm_f="raise",
                       eyes="happy", mouth="open", oy=oy)
                racket(cv, col, 26, 10 + oy, "up")
            outline(cv)
            if p >= 1:
                for (x, y) in (((32, 3), (34, 10), (22, 1)), ((33, 7), (31, 0), (23, 4)))[p % 2]:
                    cv.set(x, y, SPARK)
            shadow(cv, 12 if up else 14)
        return wframe(WIDE, b)
    st["katana"] = [point(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): bota la pelota antes de sacar
    bys = [22, 25, 28, 25]
    def bounce(p):
        def b(cv):
            player(cv, col, arm_f="forward_low", arm_b="forward", eyes="focus")
            racket(cv, col, 9, 22, "down") if False else None
            ball(cv, 26, bys[p])
            racket(cv, col, 14, 21, "down")
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["train"] = [bounce(p) for p in range(4)]

    # SLEEP: sentado con la toalla por la cabeza
    def sleep(p):
        def b(cv):
            legs(cv, "sit", pants=SHORTS, shoes=SNEAK)
            racket(cv, col, 5, 22, "down")
            oy = 1 + (p % 2)
            arm(cv, "rest", 0, oy, sleeve=POLO, side="back")
            torso(cv, 0, oy, shirt=POLO, shirt_d=POLO_D)
            arm(cv, "forward_low", 0, oy, sleeve=POLO)
            head(cv, 0, oy, hair=HAIR, eyes="closed", mouth="o")
            for y in range(1 + oy, 9 + oy):          # toalla sobre la cabeza
                for x in range(7, 25):
                    if in_ellipse(x, y, 16, 8 + oy, 8.5, 7.5) and y < 8 + oy:
                        cv.set(x, y, TOWEL)
            cv.rect(8, 8 + oy, 10, 17 + oy, TOWEL)    # cae por detrás
            cv.rect(8, 15 + oy, 10, 15 + oy, col[0])  # franja del color de la raqueta
            cv.rect(11, 2 + oy, 14, 2 + oy, TOWEL_D)
            outline(cv); shadow(cv, 16)
            zzz(cv, p, base=(24, 3))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (player(cv, col), racket(cv, col, 23, 24, "down"),
                                                  outline(cv), shadow(cv, 14)))))
    return st
