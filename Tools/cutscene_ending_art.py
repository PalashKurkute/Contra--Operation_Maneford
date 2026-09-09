"""Pixel art for Cutscene_Ending: the pre-dawn and sunrise skies, the
sun, the General crawling out of his wreck and kneeling (his helmet
as its own sprite, so Tala can lift it off), Tala reaching and
holding it, and the backlit silhouettes of the last shots (Rook
sitting on the ridge, the far rock).

The forest layers, the field and the lions are earlier art. Run from
the repo root:
    python Tools/cutscene_ending_art.py

Writes Assets/Cutscenes/Ending/Art/<name>.png. Unity imports them as
point-filtered sprites at 16 pixels per unit.
"""

import os

import numpy as np
from PIL import Image

import cutscene_intro_art as kit
from cutscene_intro_art import Canvas, rgb, BAYER
from cutscene_afterstage1_art import gradient_sky
from cutscene_afterstage2_art import (
    general_head, thick, disc, SKIN, UNI, UNI_DARK, UNI_LIGHT, BOOT, BOOT_LIGHT, GOLD,
    HELM, HELM_DARK, HELM_LIGHT, PLUME,
)
import cutscene_heliescape_art as escape

OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "Ending", "Art")
kit.OUT = OUT
rng = np.random.default_rng(71)

SHADE, RIM, RIM_HOT = "#160d12", "#c86a3a", "#ffb070"


# =========================
# SKIES AND THE SUN
# =========================

def skies():
    off = (-100, -100, 1, 0)   # no moon
    gradient_sky("sky_predawn", [
        (0, "#05081a"), (30, "#0a1030"), (60, "#121a44"), (85, "#1c2456"),
        (105, "#2c2e66"), (120, "#433a72"), (132, "#5e4878"), (142, "#7c5478"),
        (150, "#9a6074"), (158, "#b06e70"), (256, "#b06e70"),
    ], (70, 45), off, [
        (80, 118, 90, "#7a5478", "#2c2e66"), (270, 128, 70, "#9a6074", "#433a72"),
    ])
    gradient_sky("sky_sunrise", [
        (0, "#2a4a8a"), (30, "#3a62a0"), (55, "#5a7cb0"), (75, "#8a90b4"),
        (92, "#b8a0a8"), (106, "#e0a890"), (118, "#f4b47a"), (128, "#ffc46a"),
        (138, "#ffd27a"), (148, "#ffe09a"), (158, "#fff0c0"), (256, "#fff0c0"),
    ], (0, 40), off, [
        (70, 96, 110, "#ffd0a0", "#c07878"), (250, 110, 90, "#ffe0b0", "#d08878"),
        (330, 84, 60, "#ffc8a0", "#b07080"),
    ])


def sun():
    c = Canvas(72, 72)
    for y in range(72):
        for x in range(72):
            d = np.hypot(x - 35.5, y - 35.5)
            if d <= 9:
                c.set(x, y, "#fffbe8")
            elif d <= 12:
                c.set(x, y, "#ffe9a0")
            elif d <= 34 and BAYER[y % 4, x % 4] < ((34 - d) / 22) ** 1.6:
                c.set(x, y, rgb("#ffc870", int(60 + 120 * (34 - d) / 22)))
    c.save("sun")


# =========================
# THE GENERAL, BEATEN (drawn facing right)
# =========================

def general_crawl():
    """On all fours, out of the wreck, helmet still on and crooked."""
    c = Canvas(36, 24)
    thick(c, 9, 15, 11, 21, UNI_DARK)          # far leg
    thick(c, 11, 21, 4, 21, UNI_DARK)
    thick(c, 9, 15, 20, 13, UNI, 3)            # torso
    c.line(9, 11, 20, 10, UNI_LIGHT)
    c.rect(13, 11, 18, 11, GOLD)
    thick(c, 10, 16, 13, 21, UNI)              # near leg
    thick(c, 13, 21, 6, 22, UNI)
    c.rect(1, 20, 4, 22, BOOT)
    c.set(2, 20, BOOT_LIGHT)
    thick(c, 19, 14, 23, 21, UNI_LIGHT)        # arm propping him up
    c.rect(23, 21, 25, 22, SKIN)
    general_head(c, 20, 5, "spit", askew=2)
    escape.singe(c, 8)
    c.save("general_crawl")


def general_kneel(face, name, talk=False):
    """Kneeling, arms hanging. Bare-headed: the helmet is its own sprite."""
    def draw(helmet):
        c = Canvas(30, 39)
        o = 5                                  # room for the plume
        thick(c, 13, 23 + o, 16, 29 + o, UNI_DARK)   # thighs down to the knees
        thick(c, 16, 30 + o, 8, 31 + o, UNI_DARK)    # shins along the ground
        c.rect(4, 30 + o, 8, 32 + o, BOOT)
        c.set(5, 30 + o, BOOT_LIGHT)
        c.rect(10, 13 + o, 17, 24 + o, UNI)          # torso
        c.line(10, 13 + o, 10, 24 + o, UNI_DARK)
        c.line(17, 13 + o, 17, 24 + o, UNI_LIGHT)
        c.line(10, 22 + o, 17, 22 + o, "#3a2414")
        c.rect(11, 13 + o, 16, 14 + o, GOLD)
        for i, color in enumerate(("#c83030", "#3050c0", "#e0c040")):
            c.set(13 + i, 16 + o, color)
        thick(c, 14, 15 + o, 15, 22 + o, UNI_LIGHT, 1)  # arm, hanging
        c.rect(15, 23 + o, 16, 24 + o, SKIN)
        general_head(c, 10, (1 if face != "sip" else 2) + o, face, askew=2, helmet=helmet)
        return c

    # The same soot every time, so the talking frames match the still ones
    bare = draw(False)
    escape.rng = np.random.default_rng(5)
    escape.singe(bare, 8)
    if talk:
        y = (1 if face != "sip" else 2) + 5
        bare.rect(16, y + 11, 19, y + 13, "#5a1418")
    bare.save(name)

    if name == "general_kneel":
        # The helmet: what the helmeted drawing adds over the bare one
        full = draw(True)
        escape.rng = np.random.default_rng(5)
        escape.singe(full, 8)
        keep = {rgb(HELM), rgb(HELM_DARK), rgb(HELM_LIGHT), rgb(PLUME), rgb("#4a3020"), rgb("#20140c")}
        helmet = Canvas(30, 39)
        for y in range(39):
            for x in range(30):
                px = tuple(int(v) for v in full.get(x, y))
                if px in keep and px != tuple(int(v) for v in bare.get(x, y)):
                    helmet.set(x, y, px)
        helmet.save("helmet")


# =========================
# SILHOUETTES AGAINST THE SUN (it rises on the right)
# =========================

def rim(c, right=True):
    """Light the edges that face the sun (right) and the sky (top)."""
    filled = c.px[:, :, 3] > 0
    for y in range(c.h):
        for x in range(c.w):
            if not filled[y, x]:
                continue
            facing = right and (x + 1 >= c.w or not filled[y, x + 1])
            top = y == 0 or not filled[y - 1, x]
            if facing:
                c.set(x, y, RIM_HOT)
            elif top and x > c.w * 0.35:
                c.set(x, y, RIM)


def rook_sit(frame, wind):
    """Sitting on a rock, knee up, rifle leaning beside him. Frame 1
    reaches out to the lion lying in front of him."""
    c = Canvas(48, 48)
    disc(c, 14, 44, 11, 6, SHADE)              # the rock
    c.rect(3, 44, 25, 47, SHADE)
    thick(c, 3, 46, 8, 24, SHADE, 0)           # rifle
    thick(c, 4, 46, 9, 24, SHADE, 0)
    for y in range(22, 38):                    # torso, narrowing to the hips
        k = (y - 22) / 15
        c.line(round(10 + k), y, round(19 - k * 2), y, SHADE)
    c.rect(14, 19, 16, 21, SHADE)              # neck
    disc(c, 15, 14, 5, 5, SHADE)               # head
    tails = ((10, 13), (9, 13), (8, 12), (7, 12)) if wind else ((10, 13), (9, 14), (8, 14), (7, 15))
    for x, y in tails:                         # bandana tails in the wind
        c.set(x, y, SHADE)
        c.set(x, y + 1, SHADE)
    thick(c, 16, 35, 27, 30, SHADE, 2)         # thigh up to the knee
    thick(c, 27, 30, 30, 43, SHADE, 2)         # shin
    c.rect(29, 44, 34, 46, SHADE)
    if frame == 0:
        thick(c, 16, 24, 19, 31, SHADE, 1)     # arm resting on the knee
        thick(c, 19, 31, 27, 28, SHADE, 1)
    else:
        thick(c, 16, 24, 24, 27, SHADE, 1)     # reaching out
        thick(c, 24, 27, 34, 31, SHADE, 1)
        c.rect(35, 31, 37, 33, SHADE)
    rim(c)
    c.save("rook_sit_%d%s" % (frame, "w" if wind else ""))


def ridge():
    """The ridge they sit on: a long dark hilltop with grass on top."""
    w, h = 640, 72
    c = Canvas(w, h)
    for x in range(w):
        top = int(18 + 6 * np.sin(x / 47) + 3 * np.sin(x / 13 + 1))
        c.line(x, top, x, h - 1, SHADE)
        if rng.random() < 0.35:
            c.line(x, top - int(rng.integers(1, 5)), x, top, SHADE)
    rim(c, right=False)
    c.save("ridge")


def far_rock():
    c = Canvas(40, 30)
    for x in range(40):
        top = int(4 + 14 * (abs(x - 17) / 20) ** 1.5 + (2 if x > 25 else 0))
        c.line(x, top, x, 29, "#3a2232")
    c.save("far_rock")


def tala_talk():
    """Her holding pose, mouth open."""
    path = os.path.join(OUT, "tala_hold.png")
    px = np.array(Image.open(path).convert("RGBA"))
    for x, y in ((12, 10), (13, 10), (12, 11)):
        px[y, x] = rgb("#3a1a14")
    Image.fromarray(px, "RGBA").save(os.path.join(OUT, "tala_hold_talk.png"))
    print("  tala_hold_talk 22x34")


def main():
    print("Writing Ending art to " + os.path.normpath(OUT))
    skies()
    sun()
    general_crawl()
    general_kneel("calm", "general_kneel")
    general_kneel("calm", "general_kneel_talk", talk=True)
    general_kneel("sip", "general_kneel_sad")
    general_kneel("sip", "general_kneel_sad_talk", talk=True)
    for pose in ("reach", "hold"):
        escape.tala(pose)
    tala_talk()
    for frame in (0, 1):
        for wind in (False, True):
            rook_sit(frame, wind)
    ridge()
    far_rock()


if __name__ == "__main__":
    main()
