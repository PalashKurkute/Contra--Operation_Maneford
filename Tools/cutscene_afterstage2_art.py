"""Pixel art for Cutscene_AfterStage2: the General's control room
(a wall of CCTV monitors, his throne, the big red button), the
General and Dobbs in profile, and the hangar where the Mecha-Lion
sleeps.

Drawn at 16 pixels per world unit, reusing the intro's and
AfterStage1's helpers. Run from the repo root:
    python Tools/cutscene_afterstage2_art.py

Writes Assets/Cutscenes/AfterStage2/Art/<name>.png. Unity imports
them as point-filtered sprites at 16 pixels per unit.
"""

import os

import numpy as np
from PIL import Image

import cutscene_intro_art as kit
from cutscene_intro_art import BAYER, Canvas, rgb
from cutscene_afterstage1_art import (
    GLYPHS, text_width, draw_text, concrete_wall,
    CON_DARK, CON, CON_LIGHT, STEEL_DARK, STEEL, STEEL_LIGHT, ROYAL_RED, ROYAL_GOLD,
)

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "AfterStage2", "Art")
MECHA = os.path.join(kit.ROOT, "Assets", "BossFight", "Art")
rng = np.random.default_rng(47)

GLYPHS.update({
    "(": [".X", "X.", "X.", "X.", ".X"],
    ")": ["X.", ".X", ".X", ".X", "X."],
    "-": ["...", "...", "XXX", "...", "..."],
    "1": [".X", "XX", ".X", ".X", ".X"],
})

# Monitor glow; the room is lit by its screens
WALL_DARK, WALL, WALL_LIGHT = "#0c0f18", "#151a26", "#202838"
BEZEL, BEZEL_LIGHT = "#07080c", "#343c4e"
GOLD, GOLD_DARK, GOLD_LIGHT = "#d8a830", "#8a6418", "#f4d870"
VELVET, VELVET_DARK, VELVET_LIGHT = "#6a1420", "#3e0a12", "#962632"

# The General (colours from his portrait)
SKIN, SKIN_DARK = "#e8b088", "#b47454"
HELM, HELM_DARK, HELM_LIGHT = "#d8a830", "#8a6418", "#f6dc78"
MANE, MANE_DARK = "#e88a28", "#a44e12"
PLUME = "#d42a2a"
STACHE, STACHE_DARK = "#e4e4e8", "#9a9aa4"
HAIR = "#9a5420"
UNI, UNI_DARK, UNI_LIGHT = "#454a22", "#2a2e14", "#626a30"
BOOT, BOOT_LIGHT = "#15151c", "#3a3a4a"
CUP, CUP_DARK = "#f4f0e8", "#b8b0a8"
TEA = "#8a4a1a"
INK = "#14100c"

# Dobbs
DOBBS_HELM, DOBBS_HELM_DARK = "#5a6a30", "#3a4620"
VEST, VEST_DARK, VEST_STRIPE = "#e87a20", "#a85010", "#f4d040"


def thick(c, x0, y0, x1, y1, color, r=1):
    """A limb: a line r pixels either side."""
    steps = max(abs(x1 - x0), abs(y1 - y0), 1)
    for i in range(steps + 1):
        x = round(x0 + (x1 - x0) * i / steps)
        y = round(y0 + (y1 - y0) * i / steps)
        c.rect(x - r, y - r, x + r, y + r, color)


def disc(c, cx, cy, rx, ry, color, y_max=None):
    for y in range(cy - ry, cy + ry + 1):
        if y_max is not None and y > y_max:
            continue
        for x in range(cx - rx, cx + rx + 1):
            if ((x - cx) / (rx + 0.5)) ** 2 + ((y - cy) / (ry + 0.5)) ** 2 <= 1:
                c.set(x, y, color)


# =========================
# THE GENERAL, SEATED, FACING RIGHT
# =========================

def general_head(c, x, y, face, askew=0):
    """Face box top-left at (x, y): gold lion helmet, walrus moustache.
    askew slides the helmet back and tips it (after an explosion)."""
    hx, hy = -askew, askew // 2
    # Ponytail, then the mane spikes round the helmet
    c.rect(x - 3, y + 5, x - 1, y + 11, HAIR)
    c.set(x - 2, y + 12, HAIR)
    for sx, sy in ((-5, 1), (-6, 4), (-5, 7), (-3, -2), (0, -4), (4, -5), (8, -3)):
        c.rect(x + sx + hx, y + sy + hy, x + sx + hx + 1, y + sy + hy + 1, MANE)
        c.set(x + sx + hx, y + sy + hy + 1, MANE_DARK)

    # Face, ear, nose
    c.rect(x, y + 3, x + 9, y + 11, SKIN)
    c.line(x, y + 3, x, y + 11, SKIN_DARK)
    c.line(x + 1, y + 12, x + 7, y + 12, SKIN_DARK)
    c.rect(x + 2, y + 6, x + 3, y + 8, SKIN_DARK)
    c.rect(x + 10, y + 5, x + 10, y + 7, SKIN)
    c.set(x + 11, y + 7, SKIN_DARK)

    # Eye and brow
    if face == "sip":
        c.line(x + 6, y + 6, x + 8, y + 6, SKIN_DARK)
        c.line(x + 6, y + 5, x + 8, y + 5, STACHE_DARK)
    elif face == "spit":
        c.rect(x + 6, y + 5, x + 8, y + 7, "#ffffff")
        c.set(x + 8, y + 6, INK)
        c.line(x + 6, y + 3, x + 8, y + 3, STACHE)
    elif face == "shout":
        c.set(x + 7, y + 6, INK)
        c.set(x + 8, y + 6, INK)
        c.line(x + 5, y + 4, x + 6, y + 4, STACHE)
        c.line(x + 7, y + 5, x + 9, y + 5, STACHE)
    else:
        c.set(x + 7, y + 6, INK)
        c.line(x + 6, y + 5, x + 8, y + 5, STACHE_DARK)

    # Open mouth under the moustache
    if face in ("spit", "shout"):
        c.rect(x + 6, y + 11, x + 9, y + 13, "#5a1418")
        c.line(x + 6, y + 14, x + 9, y + 14, SKIN_DARK)

    # The walrus moustache, drooping at the ends
    c.line(x + 5, y + 9, x + 10, y + 9, STACHE)
    c.line(x + 7, y + 10, x + 11, y + 10, STACHE)
    c.set(x + 11, y + 11, STACHE_DARK)
    c.set(x + 5, y + 10, STACHE_DARK)

    # Helmet: a gold dome, brim, a red jewel and the plume
    x, y = x + hx, y + hy
    disc(c, x + 4, y + 3, 6, 5, HELM, y_max=y + 3)
    disc(c, x + 5, y + 1, 3, 2, HELM_LIGHT, y_max=y)
    c.line(x - 2, y + 4, x + 12, y + 4 - askew // 2, HELM_DARK)
    c.line(x - 1, y + 3, x + 11, y + 3 - askew // 2, HELM)
    c.set(x + 9, y, PLUME)
    c.rect(x + 3, y - 5, x + 4, y - 3, PLUME)
    c.set(x + 5 - askew, y - 6, PLUME)


def general(pose):
    """Poses: sit, sip, spit, shout (pointing), reach (to the button)."""
    c = Canvas(44, 50)
    lean = {"reach": 3, "spit": 1, "shout": 1}.get(pose, 0)

    # Legs, side on: thigh along the seat, shin down, boot
    c.rect(12, 31, 25, 35, UNI)
    c.line(12, 31, 25, 31, UNI_LIGHT)
    c.rect(22, 35, 26, 45, UNI)
    c.line(26, 35, 26, 45, UNI_LIGHT)
    c.line(22, 36, 22, 45, UNI_DARK)
    c.rect(21, 46, 30, 49, BOOT)
    c.line(22, 46, 29, 46, BOOT_LIGHT)

    # Torso, leaning with the pose
    for y in range(17, 34):
        off = round(lean * (33 - y) / 16)
        c.line(10 + off, y, 20 + off, y, UNI)
        c.set(10 + off, y, UNI_DARK)
        c.set(20 + off, y, UNI_LIGHT)
    c.line(10, 31, 20, 31, "#3a2414")
    c.rect(19, 30, 20, 32, GOLD)
    for y in (21, 24, 27):
        c.set(19 + round(lean * (33 - y) / 16), y, GOLD)
    for i, color in enumerate(("#c83030", "#3050c0", "#e0c040")):
        c.rect(15 + lean + i, 21, 15 + lean + i, 22, color)

    # Gold epaulette with a fringe
    c.rect(11 + lean, 17, 18 + lean, 18, GOLD)
    for x in range(11 + lean, 19 + lean, 2):
        c.set(x, 19, GOLD_DARK)

    face = {"sip": "sip", "spit": "spit", "shout": "shout", "reach": "shout"}.get(pose, "calm")
    general_head(c, 13 + lean, 5 + (1 if pose == "spit" else 0), face)

    # The near arm, cup and saucer
    sx, sy = 16 + lean, 19
    if pose == "sit":
        thick(c, sx, sy, 17, 26, UNI_LIGHT)
        thick(c, 17, 27, 23, 27, UNI_LIGHT)
        c.rect(24, 26, 25, 28, SKIN)
        c.rect(25, 22, 28, 25, CUP)
        c.line(25, 22, 28, 22, GOLD)
        c.line(28, 23, 28, 25, CUP_DARK)
        c.set(29, 23, CUP)
        c.line(23, 26, 31, 26, CUP_DARK)
    elif pose == "sip":
        thick(c, sx, sy, 20, 25, UNI_LIGHT)
        thick(c, 20, 25, 23, 17, UNI_LIGHT)
        c.rect(23, 15, 24, 17, SKIN)
        c.rect(23, 12, 26, 15, CUP)
        c.line(23, 12, 23, 15, GOLD)
        c.line(26, 12, 26, 15, CUP_DARK)
    elif pose == "spit":
        thick(c, sx, sy, 21, 24, UNI_LIGHT)
        thick(c, 21, 24, 27, 21, UNI_LIGHT)
        c.rect(28, 20, 29, 22, SKIN)
        # The cup tipping over, tea going everywhere
        for i in range(4):
            c.line(29 + i, 16 + i, 31 + i, 15 + i, CUP)
        c.line(29, 16, 31, 15, GOLD)
        for dx, dy in ((33, 14), (35, 13), (34, 16), (36, 15), (37, 12)):
            c.set(dx, dy, TEA)
    elif pose == "shout":
        thick(c, sx, sy, 22, 17, UNI_LIGHT)
        thick(c, 22, 17, 29, 13, UNI_LIGHT)
        c.rect(30, 11, 31, 13, SKIN)
        c.line(32, 11, 34, 10, SKIN)
    elif pose == "reach":
        thick(c, sx, sy + 1, 25, 24, UNI_LIGHT)
        thick(c, 25, 24, 33, 28, UNI_LIGHT)
        c.rect(34, 27, 37, 30, SKIN)
        c.line(34, 30, 37, 30, SKIN_DARK)

    # Gold cuff
    cuffs = {"sit": (22, 27), "sip": (22, 18), "spit": (26, 21), "shout": (28, 13), "reach": (32, 28)}
    cx, cy = cuffs[pose]
    c.rect(cx, cy - 1, cx + 1, cy + 1, GOLD)

    c.save("general_" + pose)


def throne():
    """Side view, facing right: a tall velvet back with a lion finial."""
    c = Canvas(44, 50)
    c.rect(3, 6, 10, 37, VELVET)
    c.line(3, 6, 3, 37, VELVET_DARK)
    c.line(10, 6, 10, 37, VELVET_LIGHT)
    for y in range(9, 36, 5):
        c.set(6, y, GOLD_DARK)
        c.set(7, y + 2, GOLD_DARK)
    c.rect(2, 4, 11, 6, GOLD)
    c.line(2, 4, 11, 4, GOLD_LIGHT)
    # Lion head finial
    disc(c, 7, 1, 3, 2, GOLD)
    c.set(9, 1, INK)
    c.rect(10, 1, 11, 2, GOLD_DARK)
    # Seat cushion, apron and gilded legs
    c.rect(8, 34, 28, 37, VELVET)
    c.line(8, 34, 28, 34, VELVET_LIGHT)
    c.line(8, 38, 28, 38, GOLD)
    for x in (9, 26):
        c.rect(x, 39, x + 1, 48, GOLD_DARK)
        c.set(x + 1, 39, GOLD)
        c.rect(x - 1, 48, x + 2, 49, GOLD)
    c.save("throne")


# =========================
# DOBBS, STANDING, FACING RIGHT
# =========================

def dobbs(raised):
    c = Canvas(24, 36)
    # Legs and boots
    c.rect(8, 23, 10, 33, UNI)
    c.rect(12, 23, 14, 33, UNI_DARK)
    c.rect(7, 34, 11, 35, BOOT)
    c.rect(12, 34, 16, 35, BOOT)
    # Body in his orange vest
    c.rect(7, 12, 15, 23, UNI)
    c.rect(8, 13, 15, 21, VEST)
    c.line(8, 13, 8, 21, VEST_DARK)
    c.line(8, 17, 15, 17, VEST_STRIPE)
    c.line(7, 22, 15, 22, "#2e2414")
    # Head, helmet
    c.rect(9, 5, 15, 11, SKIN)
    c.set(16, 8, SKIN)
    c.set(14, 7, INK)
    c.rect(10, 7, 11, 9, SKIN_DARK)
    disc(c, 12, 5, 5, 3, DOBBS_HELM, y_max=5)
    c.line(7, 5, 18, 5, DOBBS_HELM_DARK)
    c.set(13, 10, "#a05050")
    # The near arm: down, or up ("Sir...?")
    if raised:
        thick(c, 12, 13, 15, 7, UNI_LIGHT)
        thick(c, 15, 7, 16, 1, UNI_LIGHT)
        c.rect(15, 0, 17, 1, SKIN)
    else:
        thick(c, 12, 13, 13, 20, UNI_LIGHT)
        c.rect(12, 21, 14, 22, SKIN)
    c.save("dobbs_raised" if raised else "dobbs")


# =========================
# CCTV FEEDS (32x20, the big screen is these at 2x)
# =========================

FW, FH = 32, 20
FEED_DARK, FEED, FEED_LIGHT, FEED_HOT = "#06180c", "#0f3a1c", "#2a7a3e", "#c8ffd4"


def feed_scene(frame, variant):
    """Rook downs a soldier on a base camera. Frames: 0 aim, 1 fire,
    2 hit, 3 down."""
    c = Canvas(FW, FH)
    c.rect(0, 0, FW - 1, FH - 1, FEED_DARK)
    c.rect(0, 13, FW - 1, FH - 1, FEED)
    if variant == 0:
        c.rect(2, 4, 11, 12, FEED)             # a hut
        c.rect(4, 7, 6, 9, FEED_LIGHT)
    else:
        c.rect(25, 2, 27, 12, FEED)            # a watchtower
        c.rect(23, 2, 29, 3, FEED)
        c.rect(18, 10, 22, 12, FEED)           # crates

    rx = 6 + variant * 2
    c.set(rx + 1, 8, FEED_HOT)                 # Rook
    c.rect(rx, 9, rx + 1, 11, FEED_HOT)
    c.set(rx, 12, FEED_HOT)
    c.set(rx + 2, 12, FEED_HOT)
    c.line(rx + 2, 10, rx + 3, 10, FEED_HOT)
    if frame == 1:
        c.set(rx + 4, 10, "#ffffff")
        c.line(rx + 8, 10, rx + 10, 10, FEED_HOT)

    sx = 23 - variant * 2                      # the soldier
    if frame in (0, 1):
        c.set(sx, 8, FEED_LIGHT)
        c.rect(sx - 1, 9, sx, 11, FEED_LIGHT)
        c.set(sx - 1, 12, FEED_LIGHT)
        c.set(sx + 1, 12, FEED_LIGHT)
    elif frame == 2:
        c.rect(sx, 7, sx + 2, 11, "#ffffff")
    else:
        c.line(sx, 12, sx + 4, 12, FEED_LIGHT)
        c.set(sx + 5, 11, FEED_LIGHT)
    return c


def crt(c, blink):
    """Scanlines, dark corners, a REC light."""
    for y in range(0, c.h, 2):
        for x in range(c.w):
            r, g, b, a = c.get(x, y)
            c.set(x, y, (int(r * 0.75), int(g * 0.75), int(b * 0.75), a))
    for x, y in ((0, 0), (c.w - 1, 0), (0, c.h - 1), (c.w - 1, c.h - 1)):
        c.set(x, y, "#000000")
    if blink:
        c.set(2, 2, "#ff3030")
    return c


def upscale(c, k=2):
    big = Canvas(c.w * k, c.h * k)
    big.px = np.repeat(np.repeat(c.px, k, axis=0), k, axis=1)
    return big


def text_screen(lines, w, h, ink, back, scale=1):
    c = Canvas(w, h)
    c.rect(0, 0, w - 1, h - 1, back)
    total = len(lines) * 6 - 1
    y = (h // scale - total) // 2
    small = Canvas(w // scale, h // scale)
    small.px[:] = 0
    for line in lines:
        draw_text(small, line, (w // scale - text_width(line)) // 2, y, ink)
        y += 6
    big = upscale(small, scale) if scale > 1 else small
    mask = big.px[:, :, 3] > 0
    c.px[mask] = big.px[mask]
    return crt(c, False)


def feeds():
    for variant in (0, 1):
        for frame in range(4):
            crt(feed_scene(frame, variant), frame % 2 == 0).save("feed_%d_%d" % (variant, frame))

    for frame in range(4):
        crt(upscale(feed_scene(frame, 0)), frame % 2 == 0).save("big_%d" % frame)
    text_screen(["ALL UNITS", "DOWN"], 64, 40, "#ff4040", "#1a0406").save("big_down")

    for i in range(2):
        noise = Canvas(FW, FH)
        for y in range(FH):
            for x in range(FW):
                v = int(rng.integers(20, 200))
                noise.set(x, y, (v, v, v, 255))
        crt(noise, False).save("static_%d" % i)

    text_screen(["WARNING"], FW, FH, "#ffe0e0", "#a01418").save("warn_on")
    text_screen(["WARNING"], FW, FH, "#701010", "#280406").save("warn_off")
    text_screen(["MECHA-LION", "WAKING"], 64, 40, "#ffd040", "#300608").save("big_mecha")


# =========================
# THE CONTROL ROOM
# =========================

# Screens on the wall (x, y of the top-left screen pixel); the big
# one is 64x40, the rest 32x20. AfterStage2Builder places the feeds
# from these.
SMALL_SCREENS = [(88, 10), (124, 10), (88, 34), (124, 34), (232, 10), (232, 34)]
BIG_SCREEN = (162, 10)


def bezel(c, x, y, w, h):
    c.rect(x - 3, y - 3, x + w + 2, y + h + 2, BEZEL_LIGHT)
    c.rect(x - 2, y - 2, x + w + 1, y + h + 1, BEZEL)
    c.rect(x, y, x + w - 1, y + h - 1, "#020403")


def room_wall():
    w, h = 288, 112
    c = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            c.set(x, y, WALL_LIGHT if (y > 60 and BAYER[y % 4, x % 4] > 0.8) else WALL)
    for x in range(0, w, 24):
        c.line(x, 0, x, h - 1, WALL_DARK)
    c.rect(0, 0, w - 1, 3, WALL_DARK)
    c.line(0, 4, w - 1, 4, GOLD_DARK)

    # The General's portrait in a gold frame, of course
    portrait = np.array(Image.open(os.path.join(kit.ROOT, "Assets", "Dialogue",
                                                "Portraits", "general_smug.png")).convert("RGBA"))
    # (high on the wall, so Dobbs's raised hand shows below it)
    c.rect(6, 6, 75, 75, GOLD)
    c.rect(8, 8, 73, 73, GOLD_DARK)
    c.line(6, 6, 75, 6, GOLD_LIGHT)
    alpha = portrait[:, :, 3] > 0
    region = c.px[9:73, 9:73]
    region[:] = rgb(VELVET_DARK)
    region[alpha] = portrait[alpha]

    for x, y in SMALL_SCREENS:
        bezel(c, x, y, 32, 20)
    bezel(c, BIG_SCREEN[0], BIG_SCREEN[1], 64, 40)

    # Status strip under the screens: little lamps
    for x in range(88, 268, 6):
        c.set(x, 62, "#40ff80" if rng.random() < 0.6 else "#ff5040")
    c.line(84, 64, 268, 64, BEZEL_LIGHT)

    # Crest over the big screen
    draw_text(c, "ROYAL WATCH", 194 - text_width("ROYAL WATCH") // 2, 54, GOLD_DARK)
    c.save("room_wall")


def console():
    """The desk under the screens: 176x30."""
    w, h = 176, 30
    c = Canvas(w, h)
    c.rect(0, 6, w - 1, h - 1, STEEL)
    c.rect(2, 0, w - 3, 5, STEEL_LIGHT)
    c.line(2, 0, w - 3, 0, "#6a7080")
    c.line(0, 6, w - 1, 6, STEEL_DARK)
    for x in range(6, w - 6, 5):
        color = ("#40ff80", "#ffd040", "#ff5040", "#40a0ff")[int(rng.integers(0, 4))]
        c.set(x, 2, color)
        c.set(x + 1, 3, "#20242c")
    for x in range(10, w - 10, 30):
        c.rect(x, 10, x + 18, 24, STEEL_DARK)
        for y in range(12, 24, 3):
            c.line(x + 2, y, x + 16, y, "#1c1f26")
    c.save("console")


def room_floor():
    w, h = 288, 20
    c = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            c.set(x, y, "#10121a" if (x // 8 + y // 8) % 2 else "#14171f")
    c.line(0, 0, w - 1, 0, "#2a3040")
    # The royal runner to the throne
    c.rect(40, 1, 140, 5, VELVET_DARK)
    c.line(40, 1, 140, 1, VELVET)
    c.save("room_floor")


def button_small():
    """The button by the throne, in the wide: a post with a red cap."""
    c = Canvas(10, 30)
    c.rect(3, 6, 6, 29, STEEL)
    c.line(3, 6, 3, 29, STEEL_LIGHT)
    c.rect(1, 4, 8, 6, GOLD)
    c.rect(2, 1, 7, 3, "#d02020")
    c.rect(3, 1, 4, 1, "#ff9090")
    c.rect(1, 27, 8, 29, STEEL_DARK)
    c.save("button_small")


# =========================
# THE BUTTON, CLOSE
# =========================

def button_panel():
    w, h = 160, 96
    c = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            c.set(x, y, STEEL if BAYER[y % 4, x % 4] > 0.3 else "#2e323c")
    # Hazard border round the button well
    for x in range(40, 120):
        for y in (20, 21, 22, 70, 71, 72):
            c.set(x, y, "#e0b020" if ((x + y) // 4) % 2 else "#141414")
    for y in range(20, 73):
        for x in (40, 41, 42, 117, 118, 119):
            c.set(x, y, "#e0b020" if ((x + y) // 4) % 2 else "#141414")
    c.rect(43, 23, 116, 69, STEEL_DARK)

    # Label plate
    c.rect(44, 76, 115, 92, "#e8e0c8")
    c.rect(45, 77, 114, 91, "#f4ecd4")
    c.line(44, 92, 115, 92, "#8a8070")
    draw_text(c, "DO NOT PRESS", 80 - text_width("DO NOT PRESS") // 2, 79, "#b01818")
    draw_text(c, "(ROYAL)", 80 - text_width("(ROYAL)") // 2, 85, GOLD_DARK)
    for sx in (46, 112):
        c.set(sx, 78, "#6a6050")
        c.set(sx, 90, "#6a6050")

    # Little crown above the plate
    for i, x in enumerate(range(74, 87)):
        top = 6 if i % 4 == 0 else 9
        c.line(x, top, x, 13, GOLD)
    c.line(74, 13, 86, 13, GOLD_DARK)
    c.save("button_panel")


def big_button(pressed):
    c = Canvas(48, 32)
    c.rect(4, 22, 43, 31, "#3a3e48")           # housing
    c.line(4, 22, 43, 22, "#6a7080")
    top = 12 if pressed else 2
    disc(c, 24, top + 10, 16, 10, "#a01010", y_max=21)
    disc(c, 24, top + 9, 15, 9, "#e02424", y_max=20)
    disc(c, 18, top + 5, 5, 2, "#ff8080")
    c.rect(8, 20, 39, 21, "#700808")
    c.save("button_down" if pressed else "button_up")


def glass_cover(open_):
    """A hinged glass flap. Closed it sits over the button; open it
    stands up from the hinge at the back."""
    glass, edge = rgb("#a0d8ff", 70), rgb("#d8f0ff", 160)
    if open_:
        c = Canvas(56, 12)
        c.rect(2, 0, 53, 9, glass)
        c.line(2, 0, 53, 0, edge)
        c.line(2, 9, 53, 9, edge)
        c.rect(0, 10, 55, 11, STEEL_LIGHT)
    else:
        c = Canvas(56, 40)
        c.rect(2, 2, 53, 37, glass)
        c.line(2, 2, 53, 2, edge)
        c.line(2, 2, 2, 37, edge)
        c.line(53, 2, 53, 37, edge)
        c.line(6, 6, 14, 6, rgb("#ffffff", 140))
        c.rect(0, 0, 55, 1, STEEL_LIGHT)
    c.save("cover_open" if open_ else "cover_closed")


def fist():
    """His gloved fist coming down from above, olive sleeve, gold cuff."""
    c = Canvas(28, 48)
    c.rect(8, 0, 19, 29, UNI)
    c.line(8, 0, 8, 29, UNI_DARK)
    c.line(19, 0, 19, 29, UNI_LIGHT)
    c.rect(7, 28, 20, 32, GOLD)
    c.line(7, 32, 20, 32, GOLD_DARK)
    c.rect(5, 33, 22, 46, SKIN)
    c.line(5, 33, 5, 46, SKIN_DARK)
    for x in (9, 13, 17):
        c.line(x, 40, x, 46, SKIN_DARK)
    c.line(5, 47, 22, 47, SKIN_DARK)
    c.save("fist")


# =========================
# THE HANGAR
# =========================

# The door opening in the facade: x 40..215, y 40..143
DOOR_W, DOOR_H = 176, 104


def hangar_facade():
    w, h = 256, 144
    c = Canvas(w, h)
    concrete_wall(c, 0, 0, w - 1, h - 1, lit_from_below=True)
    c.line(0, 0, w - 1, 0, CON_LIGHT)
    # The opening, framed with hazard stripes
    for y in range(36, h):
        for x in range(36, 220):
            stripe = "#c9a227" if ((x + y) // 5) % 2 else "#1a1a1a"
            c.set(x, y, stripe)
    c.px[40:h, 40:216] = 0
    draw_text(c, "HANGAR 1", 128 - text_width("HANGAR 1") // 2 - 10, 16, "#c9a227")
    disc(c, 152, 18, 4, 4, ROYAL_GOLD)
    c.set(153, 17, CON_DARK)
    c.save("hangar_facade")


def hangar_door():
    c = Canvas(DOOR_W, DOOR_H)
    for y in range(DOOR_H):
        shade = (STEEL_LIGHT, STEEL, STEEL, STEEL_DARK)[y % 4]
        c.line(0, y, DOOR_W - 1, y, shade)
    for y in range(DOOR_H - 10, DOOR_H):
        for x in range(DOOR_W):
            c.set(x, y, "#c9a227" if ((x - y) // 6) % 2 else "#1a1a1a")
    c.rect(68, 30, 107, 50, ROYAL_RED)
    c.line(68, 30, 107, 30, "#a03040")
    draw_text(c, "ROYAL", 88 - text_width("ROYAL") // 2, 38, ROYAL_GOLD)
    c.save("hangar_door")


def hangar_inside():
    c = Canvas(DOOR_W, DOOR_H)
    for y in range(DOOR_H):
        for x in range(DOOR_W):
            c.set(x, y, "#07080c" if BAYER[y % 4, x % 4] > 0.15 else "#0c0e14")
    for x in range(0, DOOR_W, 22):
        c.line(x, 0, x, 80, "#0e1018")
    c.rect(0, 82, DOOR_W - 1, DOOR_H - 1, "#0c0d12")
    for x in range(10, DOOR_W, 28):
        c.line(x, 92, x + 12, 92, "#4a3e14")
    c.save("hangar_inside")


def mecha_eyes():
    """Just the red eye pixels of the Mecha-Lion's head, to light up
    over the dark head."""
    for src, name in (("MechaHead.png", "mecha_eyes"), ("MechaHeadRoar.png", "mecha_eyes_roar")):
        px = np.array(Image.open(os.path.join(MECHA, src)).convert("RGBA"))
        r, g, b = px[:, :, 0].astype(int), px[:, :, 1].astype(int), px[:, :, 2].astype(int)
        red = (r > 180) & (g < 90) & (b < 90) & (px[:, :, 3] > 0)
        out = Canvas(px.shape[1], px.shape[0])
        out.px[red] = px[red]
        out.save(name)


def main():
    print("Writing AfterStage2 art to " + os.path.normpath(kit.OUT))
    for pose in ("sit", "sip", "spit", "shout", "reach"):
        general(pose)
    throne()
    dobbs(False)
    dobbs(True)
    feeds()
    room_wall()
    console()
    room_floor()
    button_small()
    button_panel()
    big_button(False)
    big_button(True)
    glass_cover(False)
    glass_cover(True)
    fist()
    hangar_facade()
    hangar_door()
    hangar_inside()
    mecha_eyes()


if __name__ == "__main__":
    main()
