"""Estudiante: jersey, mochila y libros. Cambia el color de las gafas."""
from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, arm, frame, head, holder, legs,
                  outline, palette, shade, shadow, speed_lines, symbols, torso, wframe, widen, zzz,
                  NO_OUTLINE)

ID = "estudiante"
NAMES = {"es": "Estudiante", "en": "Student"}
VARIANTS = COLORS
FPS = {"idle": 4, "run": 12, "katana": 7, "train": 5, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 2}
ANCHOR = {"run": RUN_OFF + 16}

HAIR = (120, 74, 40, 255)
SWEATER = (64, 74, 112, 255)
KHAKI = (176, 154, 112, 255)
SNEAK = (236, 236, 240, 255)
PACK = (112, 132, 98, 255)
PACK_D = (82, 98, 72, 255)
BOOK = (60, 92, 176, 255)
BOOK_D = (40, 62, 130, 255)
PAGE = (246, 244, 236, 255)
PAGE_D = (210, 206, 196, 255)
RED = (220, 40, 40, 255)
SPARK = (255, 240, 150, 255)
NO_OUTLINE.update({SPARK})


def glasses(cv, col, oy=0, ox=0):
    b = col[0]
    for (x0, x1) in ((17 + ox, 19 + ox), (20 + ox, 22 + ox)):
        for x in range(x0, x1 + 1):
            cv.set(x, 9 + oy, b); cv.set(x, 12 + oy, b)
        cv.set(x0, 10 + oy, b); cv.set(x0, 11 + oy, b)
        cv.set(x1, 10 + oy, b); cv.set(x1, 11 + oy, b)
    cv.rect(14 + ox, 10 + oy, 16 + ox, 10 + oy, shade(b, 0.8))          # patilla


def backpack(cv, oy=0):
    cv.rect(6, 17 + oy, 10, 24 + oy, PACK)
    cv.rect(6, 22 + oy, 10, 24 + oy, PACK_D)
    cv.rect(7, 19 + oy, 9, 19 + oy, PACK_D)


def book_closed(cv, x, y):
    cv.rect(x, y, x + 4, y + 4, BOOK)
    cv.rect(x, y, x, y + 4, BOOK_D)
    cv.rect(x + 4, y + 1, x + 4, y + 3, PAGE)


def student(cv, col, *, legs_pose="stand", arm_f="rest", arm_b="rest", eyes="open",
            mouth="smile", oy=0, pack=True):
    if pack:
        backpack(cv, oy)
    legs(cv, legs_pose, 0, oy if legs_pose == "jump" else 0, pants=KHAKI, shoes=SNEAK)
    arm(cv, arm_b, 0, oy, sleeve=SWEATER, side="back")
    torso(cv, 0, oy, shirt=SWEATER, collar=WHITE)
    cv.rect(12, 18 + oy, 12, 23 + oy, PACK_D) if pack else None   # tirante
    arm(cv, arm_f, 0, oy, sleeve=SWEATER)
    head(cv, 0, oy, hair=HAIR, style="fringe", eyes=eyes, mouth=mouth)
    glasses(cv, col, oy)


def letter_a_plus(cv, x, y):
    for (dx, dy) in ((1, 0), (0, 1), (2, 1), (0, 2), (1, 2), (2, 2), (0, 3), (2, 3), (0, 4), (2, 4)):
        cv.set(x + dx, y + dy, RED)
    for (dx, dy) in ((5, 1), (4, 2), (5, 2), (6, 2), (5, 3)):
        cv.set(x + dx, y + dy, RED)


def build_states(variant):
    col = palette(variant)
    st = {}

    # IDLE: con el libro en la mano
    def idle(p):
        def b(cv):
            student(cv, col, arm_f="forward_low", eyes="closed" if p == 3 else "open", oy=(p == 2))
            book_closed(cv, 24, 19 + (p == 2))
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["idle"] = [idle(p) for p in range(4)]

    # RUN: corre con la mochila y los libros bajo el brazo (48 px)
    run = []
    for i in range(6):
        def b(cv, i=i):
            student(cv, col, legs_pose=f"r{i}", arm_f="forward_low" if i < 3 else "rest",
                    eyes="focus", oy=-(i % 3 == 1))
            book_closed(cv, 22, 19 - (i % 3 == 1))
            outline(cv); shadow(cv, 12)
        wide = holder(widen(frame(b), RUN_W, RUN_OFF))
        speed_lines(wide, i)
        if i % 2 == 0:   # una hoja que se le escapa
            x = RUN_OFF - 2 - i * 2
            wide.rect(x, 12 + i // 2, x + 2, 13 + i // 2, PAGE)
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): levanta el examen con un "A+" (40 px)
    def exam(p):
        def b(cv):
            up = p >= 2 and p % 2 == 0
            student(cv, col, legs_pose="jump" if up else "stand", arm_f="raise",
                    eyes="happy" if p >= 1 else "open", mouth="open" if p >= 1 else "smile",
                    oy=-2 if up else 0)
            oy = -2 if up else 0
            px0, py0 = 25, (8 if p == 0 else 1) + oy
            cv.rect(px0, py0, px0 + 9, py0 + 8, PAGE)
            cv.rect(px0, py0 + 8, px0 + 9, py0 + 8, PAGE_D)
            if p >= 1:
                letter_a_plus(cv, px0 + 2, py0 + 2)
            outline(cv)
            if p >= 2:
                for (x, y) in (((23, 2), (36, 12), (37, 3)), ((24, 9), (37, 7), (35, 0)))[p % 2]:
                    cv.set(x, y, SPARK)
            shadow(cv, 12 if up else 14)
        return wframe(WIDE, b)
    st["katana"] = [exam(p) for p in range(6)]

    # TRAIN (= espera tu mensaje): lee pasando páginas
    def read(p):
        def b(cv):
            student(cv, col, arm_f="forward", eyes="focus" if p != 3 else "closed")
            cv.rect(22, 17, 29, 21, PAGE)                  # libro abierto
            cv.rect(25, 17, 26, 21, PAGE_D)
            cv.rect(22, 21, 29, 21, BOOK)
            for y in (18, 19, 20):
                cv.rect(23, y, 24, y, PAGE_D) if y % 2 == 0 else None
            fx = (27, 26, 24, 23)[p]                        # hoja que pasa
            cv.rect(fx, 16, fx + 1, 20, WHITE)
            outline(cv); shadow(cv, 14)
        return frame(b)
    st["train"] = [read(p) for p in range(4)]

    # SLEEP: dormido sobre el libro abierto
    def sleep(p):
        def b(cv):
            legs(cv, "sit", pants=KHAKI, shoes=SNEAK)
            backpack(cv, 2)
            arm(cv, "rest", 0, 1, sleeve=SWEATER, side="back")
            torso(cv, 0, 1, shirt=SWEATER, collar=WHITE)
            cv.rect(19, 23, 27, 25, PAGE); cv.rect(23, 23, 23, 25, PAGE_D); cv.rect(19, 25, 27, 25, BOOK)
            arm(cv, "forward_low", 0, 1, sleeve=SWEATER)
            head(cv, 3, 6 + (p % 2 == 1) * 0, hair=HAIR, style="fringe", eyes="closed", mouth="o")
            glasses(cv, col, 6, 3)
            outline(cv); shadow(cv, 16)
            zzz(cv, p, base=(25, 6))
        return frame(b)
    st["sleep"] = [sleep(p) for p in range(4)]

    st.update(symbols(lambda p: frame(lambda cv: (student(cv, col, arm_f="forward_low"),
                                                  book_closed(cv, 24, 19), outline(cv), shadow(cv, 14)))))
    return st
