"""Futbolista: camiseta del color de la sesión, pantalón blanco y balón."""
import random

from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, arm, frame, head, holder, legs,
                  outline, palette, shade, shadow, speed_lines, symbols, torso, wframe, widen, zzz,
                  NO_OUTLINE)

ID = "futbolista"
NAMES = {"es": "Futbolista", "en": "Footballer"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 8, "train": 6, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

HAIR = (34, 28, 26, 255)
SHORTS = (236, 238, 244, 255)
BOOTS = (30, 30, 36, 255)
BALL = (248, 248, 250, 255)
BALL_D = (40, 40, 48, 255)


def ball(cv, x, y, rot=0):
    """Balón de 5x5 con parches que giran."""
    for dy, row in enumerate([".###.", "#####", "#####", "#####", ".###."]):
        for dx, ch in enumerate(row):
            if ch == "#":
                cv.set(x + dx, y + dy, BALL)
    patches = [[(2, 2), (0, 1), (4, 3)], [(1, 2), (3, 0), (3, 4)], [(2, 1), (4, 2), (0, 3)],
               [(3, 2), (1, 0), (1, 4)]][rot % 4]
    for (dx, dy) in patches:
        cv.set(x + dx, y + dy, BALL_D)


def player(cv, col, *, legs_pose="stand", arm_f="rest", arm_b="rest", eyes="open", mouth="smile",
           oy=0):
    b, bd, bl = col
    legs(cv, legs_pose, 0, oy if legs_pose in ("jump",) else 0, pants=SHORTS, shoes=BOOTS, socks=bd)
    arm(cv, arm_b, 0, oy, sleeve=b, side="back")
    torso(cv, 0, oy, shirt=b, shirt_d=bd, collar=WHITE)
    cv.rect(16, 20 + oy, 17, 22 + oy, bl)                      # número en la camiseta
    arm(cv, arm_f, 0, oy, sleeve=b)
    head(cv, 0, oy, hair=HAIR, style="fringe", eyes=eyes, mouth=mouth)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: pisa el balón y respira
    def idle(p):
        def b(cv):
            ball(cv, 21, 25, 0)
            player(cv, col, legs_pose="kick", eyes="closed" if p == 3 else "open")
            outline(cv); shadow(cv, 16)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: corre conduciendo el balón (48 px)
    run = []
    for i in range(6):
        def b(cv, i=i):
            player(cv, col, legs_pose=f"r{i}", arm_f="forward_low" if i < 3 else "rest",
                   arm_b="rest" if i < 3 else "forward", eyes="focus", oy=-(i % 3 == 1))
            ball(cv, 25, 25 - (i % 3 == 0), i)
            outline(cv); shadow(cv, 12)
        wide = holder(widen(frame(b), RUN_W, RUN_OFF))
        speed_lines(wide, i)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): ¡gol! chuta y celebra saltando con el puño en alto (40 px)
    rnd = random.Random(7)
    confetti = [[(rnd.randrange(2, 38), rnd.randrange(0, 14)) for _ in range(9)] for _ in range(4)]
    colors = [palette(c)[0] for c in COLORS]
    def goal(p):
        def b(cv):
            if p == 0:
                player(cv, col, legs_pose="kick", arm_f="back", arm_b="forward", eyes="focus")
                ball(cv, 26, 23, 1)
            elif p == 1:
                player(cv, col, legs_pose="stand", arm_f="out", arm_b="out", eyes="happy", mouth="open")
                ball(cv, 34, 14, 2)
            else:
                up = p % 2 == 0
                player(cv, col, legs_pose="jump" if up else "stand", arm_f="raise", arm_b="rest",
                       eyes="happy", mouth="open", oy=-2 if up else 0)
            outline(cv)
            if p >= 1:
                for k, (x, y) in enumerate(confetti[p % 4]):
                    cv.set(x, y, colors[k % len(colors)])
            shadow(cv, 12 if p >= 2 and p % 2 == 0 else 14)
        return wframe(WIDE, b)
    st["katana"] = [goal(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): toques de balón
    heights = [23, 17, 13, 17]
    def juggle(p):
        def b(cv):
            player(cv, col, legs_pose="kick" if p == 0 else "stand", arm_f="out" if p == 0 else "rest",
                   arm_b="out" if p == 0 else "rest", eyes="focus")
            ball(cv, 22 if p == 0 else 24, heights[p], p)
            outline(cv); shadow(cv, 16)
        return frame(b)
    st["train"] = [juggle(p) for p in range(4)]

    # SLEEP: sentado sobre el balón
    def sleep(p):
        def b(cv):
            legs(cv, "sit", pants=SHORTS, shoes=BOOTS)
            ball(cv, 7, 25, 0)
            player_oy = 1 + (p % 2)
            arm(cv, "rest", 0, player_oy, sleeve=col[0], side="back")
            torso(cv, 0, player_oy, shirt=col[0], shirt_d=col[1], collar=WHITE)
            arm(cv, "rest", 0, player_oy, sleeve=col[0])
            head(cv, 0, player_oy, hair=HAIR, style="fringe", eyes="closed", mouth="o")
            outline(cv); shadow(cv, 16)
            zzz(cv, p, base=(23, 4))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (ball(cv, 21, 25, 0),
                                                  player(cv, col, legs_pose="kick"),
                                                  outline(cv), shadow(cv, 16)))))
    return st
