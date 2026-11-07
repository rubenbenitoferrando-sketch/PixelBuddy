"""Perro: cada sesión trae una raza distinta (y el collar del color de la sesión)."""
from PIL import ImageOps

from base import (COLORS, RUN_OFF, RUN_W, WIDE, OUT, WHITE, EYE, SHINE, frame, holder,
                  in_ellipse, outline, palette, shade, shadow, speed_lines, symbols, wframe,
                  widen, zzz, NO_OUTLINE, H)

ID = "perro"
NAMES = {"es": "Perro", "en": "Dog"}
VARIANTS = ["labrador", "dalmata", "husky", "beagle", "salchicha", "carlino"]
FPS = {"idle": 6, "run": 12, "katana": 8, "train": 8, "sleep": 2, "alert": 4, "ask": 4}
LOOP_FROM = {"katana": 1}
ANCHOR = {"run": RUN_OFF + 16}

NOSE = (30, 26, 30, 255)
TONGUE = (240, 110, 130, 255)
BONE = (246, 240, 226, 255)
BONE_D = (214, 204, 184, 255)
HEART = (255, 110, 140, 255)
SWIRL = (255, 255, 255, 190)
NO_OUTLINE.update({SWIRL, HEART})

#          cuerpo            sombra            hocico/claro       oreja             extra
BREEDS = {
    "labrador":  ((226, 186, 110), (186, 142, 74), (244, 214, 150), (198, 152, 82), None),
    "dalmata":   ((246, 246, 248), (204, 208, 218), (255, 255, 255), (44, 42, 48), "spots"),
    "husky":     ((126, 134, 152), (90, 96, 112), (240, 242, 246), (110, 118, 136), "husky"),
    "beagle":    ((196, 124, 62), (146, 86, 40), (246, 244, 240), (150, 88, 44), "beagle"),
    "salchicha": ((156, 82, 40), (112, 56, 26), (196, 116, 64), (120, 62, 30), "long"),
    "carlino":   ((224, 194, 146), (184, 152, 106), (52, 46, 48), (44, 40, 42), "pug"),
}


def c4(c):
    return tuple(c) + (255,)


def blob(cv, cx, cy, rx, ry, col):
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if in_ellipse(x, y, cx, cy, rx, ry):
                cv.set(x, y, col)


def dog(cv, breed, collar, *, pose="stand", phase=0, eyes="open", tongue=False, tail=0,
        ears_back=False, oy=0, bone=False):
    main, dark, light, ear = (c4(c) for c in BREEDS[breed][:4])
    extra = BREEDS[breed][4]
    longb = extra == "long"
    pug = extra == "pug"
    legh = 3 if longb else 5

    # ----- cola (detrás del cuerpo) -----
    tails = {
        0: [(7, 18), (6, 17), (5, 16), (4, 15)],
        1: [(7, 18), (6, 18), (5, 17), (4, 17)],
        2: [(7, 19), (6, 19), (5, 19), (4, 20)],
        3: [(7, 18), (6, 16), (6, 15), (5, 14)],
    }
    if pose != "curl":
        pts = tails[tail % 4]
        if pug:
            pts = [(7, 17), (6, 16), (7, 15), (8, 16)]
        if longb:
            pts = [(x - 2, y + 2) for (x, y) in pts]
        for k, (x, y) in enumerate(pts):
            c = WHITE if (extra == "beagle" and k == len(pts) - 1) else (main if extra != "husky" else dark)
            cv.set(x, y + oy, c)
            if extra == "husky":
                cv.set(x, y + 1 + oy, light)

    # ----- patas -----
    by = 24 + oy if not longb else 25 + oy
    if pose == "stand":
        legs = [(8, 0), (11, 1), (18, 0), (21, 1)]
        for (x, far) in legs:
            c = dark if far else main
            cv.rect(x, by, x + 1, by + legh - 1, c)
            cv.rect(x, by + legh - 1, x + 1, by + legh - 1, light if extra != "pug" else main)
    elif pose.startswith("run"):
        stretch = phase % 3 == 0
        if stretch:      # patas estiradas (galope)
            cv.rect(5, by + 1, 7, by + 2, dark); cv.rect(3, by + 2, 4, by + 3, dark)
            cv.rect(6, by, 8, by + 1, main); cv.rect(4, by + 1, 5, by + 2, main)
            cv.rect(21, by, 24, by + 1, dark); cv.rect(25, by + 1, 26, by + 2, dark)
            cv.rect(20, by - 1, 23, by, main); cv.rect(24, by, 25, by + 1, main)
        else:            # patas recogidas: separadas y alternando, como un trote
            for (x, far, dx) in ((8, 0, 1), (11, 1, -1), (18, 1, 1), (21, 0, -1)):
                c = dark if far else main
                cv.rect(x, by, x + 1, by + 1, c)
                cv.rect(x + dx, by + 2, x + 1 + dx, by + 3, c)

    # ----- cuerpo -----
    if pose == "curl":
        blob(cv, 14, 25 + oy, 9, 3.6, main)
        for x in range(6, 23):
            if cv.get(x, 27 + oy)[:3] == main[:3]:
                cv.set(x, 27 + oy, dark)
        cv.rect(3, 26 + oy, 6, 27 + oy, main)               # cola enroscada delante
    else:
        if longb:
            blob(cv, 14, 21.5 + oy, 9.5, 3.4, main)
        else:
            blob(cv, 14.5, 20.5 + oy, 7.5, 4.2, main)
        # barriga clara / dibujos de cada raza
        for x in range(4, 24):
            y = (24 if not longb else 24) + oy
            if cv.get(x, y)[:3] == main[:3]:
                cv.set(x, y, dark)
        if extra == "spots":
            for (x, y) in ((10, 18), (11, 18), (15, 20), (18, 18), (12, 22), (19, 21), (8, 20)):
                cv.set(x, y + oy, ear)
        if extra == "beagle":
            blob(cv, 13, 18 + oy, 4.5, 1.8, c4((40, 36, 40)))
        if extra == "husky":
            for x in range(6, 23):
                for y in (22 + oy, 23 + oy):
                    if cv.get(x, y)[:3] == main[:3]:
                        cv.set(x, y, light)

    # ----- cabeza -----
    hx, hy = (23.5, 13.5 + oy) if pose != "curl" else (22.5, 21.5 + oy)
    if pose.startswith("run"):
        hx, hy = 24.0, 13.0 + oy
    blob(cv, hx, hy, 5.2, 4.8, main)
    ix, iy = int(hx), int(hy)
    if extra == "husky":   # máscara blanca
        blob(cv, hx + 2.5, hy + 1.8, 3.2, 2.2, light)
    # hocico y nariz
    mcol = light if not pug else c4((52, 46, 48))
    cv.rect(ix + 3, iy + 1, ix + 6, iy + 3, mcol)
    cv.rect(ix + 6, iy + 1, ix + 7, iy + 2, NOSE)
    cv.rect(ix + 4, iy + 3, ix + 5, iy + 3, shade(mcol, 0.7))
    if tongue:
        cv.rect(ix + 4, iy + 4, ix + 5, iy + 5, TONGUE)
    if bone:
        cv.rect(ix + 3, iy + 3, ix + 10, iy + 3, BONE)
        for bx in (ix + 2, ix + 10):
            cv.rect(bx, iy + 2, bx + 1, iy + 2, BONE); cv.rect(bx, iy + 4, bx + 1, iy + 4, BONE)
            cv.set(bx, iy + 3, BONE)
        cv.rect(ix + 4, iy + 3, ix + 9, iy + 3, BONE_D) if False else None
    # ojo
    ex, ey = ix + 2, iy - 1
    if eyes == "open":
        cv.set(ex, ey, EYE if extra != "husky" else c4((90, 170, 240))); cv.set(ex, ey + 1, EYE)
        cv.set(ex, ey - 1, SHINE) if False else cv.set(ex + 1, ey, SHINE) if extra == "husky" else None
    elif eyes == "happy":
        cv.set(ex - 1, ey + 1, EYE); cv.set(ex, ey, EYE); cv.set(ex + 1, ey + 1, EYE)
    else:
        cv.set(ex - 1, ey + 1, EYE); cv.set(ex, ey + 1, EYE)
    # orejas
    if extra == "husky":
        for (x, y0) in ((ix - 3, iy - 7), (ix - 2, iy - 8), (ix - 1, iy - 7)):
            cv.rect(x, y0, x, iy - 4, ear)
        cv.set(ix - 2, iy - 6, c4((230, 180, 180)))
    elif pug:
        cv.rect(ix - 3, iy - 4, ix - 1, iy - 3, ear)
    else:
        if ears_back:
            cv.rect(ix - 7, iy - 3, ix - 3, iy - 2, ear)
        else:
            cv.rect(ix - 3, iy - 3, ix - 1, iy + 3 + (1 if longb else 0), ear)
            cv.set(ix - 3, iy - 3, main)
    # collar
    if pose != "curl":
        for y in (iy + 4, iy + 5):
            for x in range(ix - 4, ix):
                if cv.get(x, y)[3] > 0:
                    cv.set(x, y, collar)
        cv.set(ix - 2, iy + 6, c4((255, 214, 90)))        # chapita


def build_states(variant):
    collar = palette(COLORS[VARIANTS.index(variant)])[0]
    st = {}

    def draw(**kw):
        return lambda cv: (dog(cv, variant, collar, **kw), outline(cv), shadow(cv, 18))

    # IDLE: de pie jadeando, mueve la cola
    st["idle"] = [frame(draw(tongue=True, tail=p, oy=(p % 2), eyes="closed" if p == 5 else "open"))
                  for p in range(6)]

    # RUN: galope con las orejas al viento (48 px)
    run = []
    for i in range(6):
        core = frame(lambda cv, i=i: (dog(cv, variant, collar, pose="run", phase=i, tongue=True,
                                          ears_back=True, tail=2, oy=-(i % 3 == 0)),
                                      outline(cv), shadow(cv, 16)))
        wide = holder(widen(core, RUN_W, RUN_OFF))
        speed_lines(wide, i, rows=((12, 10), (17, 14), (22, 8)))
        run.append(wide)
    st["run"] = run

    # KATANA (= terminar): trae un hueso y mueve la cola feliz (40 px)
    def happy(p):
        def b(cv):
            dog(cv, variant, collar, bone=True, tail=p, eyes="happy", oy=-(p % 2))
            outline(cv)
            if p >= 1:
                for (x, y) in (((30, 4), (31, 3), (29, 3)), ((33, 2), (34, 1), (32, 1)))[p % 2]:
                    cv.set(x, y, HEART)
            shadow(cv, 18)
        return wframe(WIDE, b)
    st["katana"] = [happy(p) for p in range(5)]

    # TRAIN (= espera tu mensaje): persigue su cola dando vueltas
    def spin(p):
        cv = frame(lambda cv: (dog(cv, variant, collar, tail=p, tongue=True), outline(cv)))
        if p in (1, 2):
            cv = holder(ImageOps.mirror(cv.img))
        for (x, y) in (((3, 12), (4, 11), (27, 26)), ((28, 11), (29, 12), (4, 26)),
                       ((4, 12), (3, 13), (28, 26)), ((27, 12), (28, 11), (3, 26)))[p]:
            cv.set(x, y, SWIRL)
        shadow(cv, 18)
        return cv
    st["train"] = [spin(p) for p in range(4)]

    # SLEEP: acurrucado
    st["sleep"] = [frame(lambda cv, p=p: (dog(cv, variant, collar, pose="curl", eyes="closed", oy=(p % 2) * 0),
                                          outline(cv), shadow(cv, 20), zzz(cv, p, base=(24, 12))))
                   for p in range(4)]

    st.update(symbols(lambda p: frame(draw(tongue=p % 2 == 0, tail=p))))
    return st
