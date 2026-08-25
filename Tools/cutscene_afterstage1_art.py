"""Pixel art for Cutscene_AfterStage1: the forest clearing at blue
hour where the last collar breaks, and the General's compound at
night (keep, wings, fence, gate, watchtowers, floodlights).

Drawn at the forest pack's scale (16 pixels per world unit), like
the intro, and reusing its drawing helpers. Run from the repo root:
    python Tools/cutscene_afterstage1_art.py

Writes Assets/Cutscenes/AfterStage1/Art/<name>.png. Unity imports
them as point-filtered sprites at 16 pixels per unit.
"""

import os

import numpy as np
from PIL import Image

import cutscene_intro_art as kit
from cutscene_intro_art import BAYER, Canvas, rgb, pack_sprite, silhouette

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "AfterStage1", "Art")
BANNER = os.path.join(kit.ROOT, "Assets", "Dialogue", "Props", "Banner.png")
rng = np.random.default_rng(23)


# =========================
# PIXEL FONT (signs, stencils)
# =========================

GLYPHS = {
    "A": [".X.", "X.X", "XXX", "X.X", "X.X"],
    "B": ["XX.", "X.X", "XX.", "X.X", "XX."],
    "C": [".XX", "X..", "X..", "X..", ".XX"],
    "D": ["XX.", "X.X", "X.X", "X.X", "XX."],
    "E": ["XXX", "X..", "XX.", "X..", "XXX"],
    "F": ["XXX", "X..", "XX.", "X..", "X.."],
    "G": [".XX", "X..", "X.X", "X.X", ".XX"],
    "H": ["X.X", "X.X", "XXX", "X.X", "X.X"],
    "I": ["XXX", ".X.", ".X.", ".X.", "XXX"],
    "K": ["X.X", "X.X", "XX.", "X.X", "X.X"],
    "L": ["X..", "X..", "X..", "X..", "XXX"],
    "M": ["X...X", "XX.XX", "X.X.X", "X...X", "X...X"],
    "N": ["X..X", "XX.X", "X.XX", "X..X", "X..X"],
    "O": [".X.", "X.X", "X.X", "X.X", ".X."],
    "P": ["XX.", "X.X", "XX.", "X..", "X.."],
    "R": ["XX.", "X.X", "XX.", "X.X", "X.X"],
    "S": [".XX", "X..", ".X.", "..X", "XX."],
    "T": ["XXX", ".X.", ".X.", ".X.", ".X."],
    "U": ["X.X", "X.X", "X.X", "X.X", "XXX"],
    "V": ["X.X", "X.X", "X.X", "X.X", ".X."],
    "W": ["X...X", "X...X", "X.X.X", "XX.XX", "X...X"],
    "Y": ["X.X", "X.X", ".X.", ".X.", ".X."],
    "!": ["X", "X", "X", ".", "X"],
    " ": ["..", "..", "..", "..", ".."],
}


def text_width(text):
    return sum(len(GLYPHS[c][0]) + 1 for c in text) - 1


def draw_text(c, text, x, y, color, shadow=None):
    for ch in text:
        glyph = GLYPHS[ch]
        for row, line in enumerate(glyph):
            for col, mark in enumerate(line):
                if mark == "X":
                    if shadow:
                        c.set(x + col, y + row + 1, shadow)
                    c.set(x + col, y + row, color)
        x += len(glyph[0]) + 1


def dithered(c, x0, y0, x1, y1, top, bottom, power=1.0):
    """Vertical two-colour blend with an ordered dither."""
    for y in range(y0, y1 + 1):
        frac = ((y - y0) / max(y1 - y0, 1)) ** power
        for x in range(x0, x1 + 1):
            c.set(x, y, bottom if frac > BAYER[y % 4, x % 4] else top)


# =========================
# SKIES
# =========================

def gradient_sky(name, stops, stars, moon, streaks):
    w, h = 384, 256
    c = Canvas(w, h)
    rows = [s[0] for s in stops]
    colors = [rgb(s[1]) for s in stops]

    for y in range(h):
        i = np.searchsorted(rows, y, side="right") - 1
        i = min(max(i, 0), len(rows) - 2)
        frac = np.clip((y - rows[i]) / (rows[i + 1] - rows[i]), 0, 1)
        frac = np.clip((frac - 0.5) * 2.5 + 0.5, 0, 1)

        for x in range(w):
            upper = frac > BAYER[y % 4, x % 4]
            c.set(x, y, colors[i + 1] if upper else colors[i])

    count, depth = stars
    for _ in range(count):
        x, y = rng.integers(0, w), int(abs(rng.normal(0, depth)))
        if y < depth * 2.4:
            bright = rng.random() < 0.22
            c.set(x, y, "#eef0ff" if bright else "#7f86b8")
            if bright and rng.random() < 0.3:
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    c.set(x + dx, y + dy, "#4b5288")

    mx, my, r, shadow = moon
    for y in range(my - r - 6, my + r + 7):
        for x in range(mx - r - 6, mx + r + 7):
            d = np.hypot(x - mx, y - my)
            lit = d <= r and (shadow is None or
                             np.hypot(x - mx - shadow, y - my + 2) > r)
            if lit:
                c.set(x, y, "#f4f1df" if d < r - 1 else "#d8d4c0")
            elif d <= r + 5 and BAYER[y % 4, x % 4] > 0.55 + (d - r) * 0.08:
                if shadow is None:
                    c.set(x, y, "#2a3058")

    if shadow is None:
        # Full moon: a few darker maria
        for dx, dy, rr in ((-3, -2, 3), (3, 2, 2), (-1, 4, 2)):
            for y in range(my + dy - rr, my + dy + rr + 1):
                for x in range(mx + dx - rr, mx + dx + rr + 1):
                    if np.hypot(x - mx - dx, y - my - dy) <= rr:
                        c.set(x, y, "#c9c4ad")

    for cx, cy, length, lit, dark in streaks:
        for x in range(cx - length // 2, cx + length // 2):
            edge = abs(x - cx) / (length / 2)
            thick = 1 + int(2 * (1 - edge ** 2))
            for k in range(thick):
                c.set(x, cy - k, dark if k else lit)

    c.save(name)


def skies():
    # The visible band of a shot is only ~100 rows, so the whole
    # gradient (stars, moon, glow) is packed into the upper rows
    gradient_sky("sky_twilight", [
        (0, "#070a1c"), (30, "#0c1230"), (55, "#141c40"), (75, "#1e2650"),
        (92, "#2a3060"), (106, "#3a3a6c"), (118, "#4e4476"), (128, "#664e7a"),
        (138, "#845878"), (148, "#a46470"), (158, "#c27668"), (256, "#c27668"),
    ], (90, 40), (96, 40, 8, 4), [
        (60, 112, 80, "#8a5a7c", "#2e3462"), (250, 124, 70, "#a0607a", "#3b3e6c"),
        (330, 100, 50, "#7a5278", "#2a3060"), (150, 134, 40, "#b06c74", "#4a4472"),
    ])

    gradient_sky("sky_night", [
        (0, "#020309"), (40, "#050816"), (70, "#080d1e"), (95, "#0c1226"),
        (112, "#141a30"), (124, "#1f1f36"), (134, "#2e2636"), (144, "#3f2f36"),
        (156, "#4e3a36"), (256, "#4e3a36"),
    ], (150, 50), (300, 52, 12, None), [
        (90, 96, 110, "#1f2440", "#121630"), (240, 84, 70, "#232845", "#141831"),
    ])


# =========================
# RECOLOURED FOREST LAYERS
# =========================

TWILIGHT_LAYERS = {
    "Background4": ("tw_mist", "#3a3a68", "#5c5484"),
    "Background3": ("tw_hills", "#3b3c6a", "#5f5a88"),
    "Background2": ("tw_pines_far", "#262a52", "#45467a"),
    "Background1": ("tw_pines_near", "#161a38", "#2e3160"),
}

NIGHT_LAYERS = {
    "Background4": ("night_far", "#161a30", "#262942"),
    "Background3": ("night_hills", "#10132a", "#20233e"),
    "Background2": ("night_pines", "#090c1c", "#1a1d33"),
}


def forest_layers():
    pack = np.array(Image.open(kit.PACK).convert("RGBA"))

    for table in (TWILIGHT_LAYERS, NIGHT_LAYERS):
        for sprite, (name, body, rim) in table.items():
            layer = silhouette(pack_sprite(pack, sprite), body, rim, 3)
            rows = np.where(layer.px[:, :, 3].any(axis=1))[0]
            solid = np.where(layer.px[:, :, 3].all(axis=1))[0]
            layer.save(name)
            print("    first row %d, solid from row %d" % (rows[0], solid[0]))

    for sprite, suffix in (("Tree4", "1"), ("Tree5", "2")):
        silhouette(pack_sprite(pack, sprite),
                   "#0b0d1e", "#262a4c").save("tw_tree_" + suffix)
        # Lit from the compound behind: a warm sodium rim
        silhouette(pack_sprite(pack, sprite),
                   "#020308", "#3a2a26").save("night_tree_" + suffix)


def clearing_ground():
    """The forest pack's grass and earth, graded to blue hour."""
    pack = np.array(Image.open(kit.PACK).convert("RGBA")).astype(float)
    top = pack[464:480, 32:48]
    earth = pack[480:496, 32:48]

    w = 480
    strip = np.zeros((48, w, 4))
    for x in range(0, w, 16):
        strip[0:16, x:x + 16] = top
        strip[16:32, x:x + 16] = earth
        strip[32:48, x:x + 16] = earth

    grade = np.array([0.40, 0.46, 0.70])
    strip[:, :, :3] = np.clip(strip[:, :, :3] * grade + [6, 7, 20], 0, 255)
    # Darker with depth
    for y in range(16, 48):
        strip[y, :, :3] *= 1 - (y - 16) / 48

    c = Canvas(w, 48)
    c.px = strip.astype(np.uint8)

    for x in range(2, w - 2, 3):
        if rng.random() < 0.6:
            tall = rng.integers(2, 7)
            c.line(x, 1, x + rng.integers(-1, 2), 1 - tall,
                   "#2a4248" if rng.random() < 0.7 else "#3a5a5a")

    c.save("clearing_ground")


def foreground_grass(name, w, h, dark, rim):
    """A strip of dark grass, ferns and bushes for the foreground."""
    c = Canvas(w, h)
    heights = np.convolve(rng.uniform(0, 1, w + 16), np.ones(17) / 17, "valid")

    for x in range(w):
        base = int(h - 6 - heights[x] * 10)
        for y in range(base, h):
            c.set(x, y, dark)

    for _ in range(w // 2):
        x = rng.integers(0, w)
        tall = rng.integers(6, h - 2)
        lean = rng.integers(-3, 4)
        c.line(x, h - 1, x + lean, h - 1 - tall, dark)
        if rng.random() < 0.3:
            c.set(x + lean, h - 1 - tall, rim)

    # Bush clumps
    for _ in range(w // 60):
        cx, r = rng.integers(10, w - 10), rng.integers(6, 11)
        for y in range(h - r * 2, h):
            for x in range(cx - r * 2, cx + r * 2):
                if ((x - cx) / 2) ** 2 + (y - (h - r)) ** 2 <= r * r:
                    edge = ((x - cx) / 2) ** 2 + (y - (h - r)) ** 2 > (r - 1.2) ** 2
                    c.set(x, y, rim if edge and y < h - r else dark)

    c.save(name)


# =========================
# THE CLEARING: STORY PROPS
# =========================

def tent_torn():
    """A ranger tent, collapsed and clawed, blue-hour colours."""
    w, h = 56, 30
    c = Canvas(w, h)
    canvas_dark, canvas, canvas_lit = "#2a3346", "#36435a", "#4b5b72"

    # Sagging ridge, pole snapped on the right
    ridge = [(4, 26), (14, 8), (30, 4), (40, 12), (46, 18), (52, 27)]
    for x in range(w):
        top = None
        for (x0, y0), (x1, y1) in zip(ridge, ridge[1:]):
            if x0 <= x <= x1:
                top = int(y0 + (y1 - y0) * (x - x0) / (x1 - x0))
        if top is None:
            continue
        for y in range(top, h - 2):
            slope = (y - top) / max(h - 2 - top, 1)
            color = canvas_lit if x < 30 and slope < 0.3 else canvas
            if x > 30:
                color = canvas_dark
            c.set(x, y, color)
        c.set(x, top, "#5c6f88" if x < 30 else "#3e4c62")

    # Torn flap and claw slashes
    for k in range(3):
        c.line(18 + k * 4, 12 + k, 24 + k * 4, 24, "#10131e")
    c.rect(34, 18, 40, 27, "#10131e")
    c.line(35, 18, 41, 27, canvas_lit)

    c.line(30, 4, 30, 27, "#4a3a2a")      # pole
    c.line(46, 18, 49, 10, "#4a3a2a")     # snapped pole
    c.line(0, 28, w - 1, 28, "#1a2030")   # guy line shadow
    c.line(2, 27, 8, 27, "#6a6a5a")       # pegs
    c.save("tent_torn")


def cage_broken():
    w, h = 40, 34
    c = Canvas(w, h)
    bar, bar_lit = "#2a2d3c", "#596080"

    c.rect(0, h - 4, w - 1, h - 1, "#1c1e2a")
    c.line(0, h - 4, w - 1, h - 4, "#3a3e52")
    c.rect(0, 2, w - 1, 4, "#1c1e2a")
    c.line(0, 2, w - 1, 2, "#3a3e52")

    for x in range(2, w - 2, 5):
        if 16 <= x <= 22:
            # Bent outwards: something got out
            c.line(x, 5, x - 6 + (x - 16), 16, bar)
            c.line(x, h - 5, x + 3, 22, bar)
            continue
        c.line(x, 5, x, h - 5, bar)
        c.line(x + 1, 5, x + 1, h - 5, bar_lit)

    # Door hanging off one hinge
    c.line(w - 2, 6, w + 0, h - 6, bar_lit)
    c.save("cage_broken")


def collar_parts():
    """The mind-control collar close up: band, chip and LED, and
    the two halves it breaks into."""
    band, band_lit, band_dark = "#3a3e4a", "#7e8494", "#1c1e26"

    c = Canvas(6, 9)
    c.rect(1, 0, 4, 8, band)
    c.line(1, 0, 1, 8, band_lit)
    c.line(4, 0, 4, 8, band_dark)
    c.rect(0, 3, 5, 6, "#24262e")         # chip housing
    c.line(0, 3, 5, 3, "#5a5e6c")
    c.set(2, 4, "#ff3b3b")                # LED (off state drawn dark)
    c.set(3, 4, "#7a1c1c")
    c.set(2, 5, "#30d0ff")                # data light
    c.save("collar")

    for name, flip in (("collar_half_a", False), ("collar_half_b", True)):
        h = Canvas(5, 6)
        pts = [(1, 0), (2, 0), (3, 1), (3, 2), (2, 3), (3, 4), (2, 5), (1, 5)]
        for x, y in pts:
            h.set(4 - x if flip else x, y, band)
        h.set(4 - 1 if flip else 1, 1, band_lit)
        h.set(4 - 1 if flip else 1, 2, band_lit)
        h.set(4 - 2 if flip else 2, 4, "#ff3b3b" if not flip else "#30d0ff")
        h.save(name)

    led = Canvas(3, 3)
    led.set(1, 1, "#ffd0d0")
    for dx, dy in ((0, 1), (2, 1), (1, 0), (1, 2)):
        led.set(dx, dy, "#ff3030")
    led.save("collar_led")


# =========================
# THE COMPOUND
# =========================

CON_DARK, CON, CON_LIGHT = "#15171e", "#20232c", "#2c303b"
CON_LIT, CON_HOT = "#4a4038", "#6b5640"     # concrete under sodium light
STEEL_DARK, STEEL, STEEL_LIGHT = "#14161c", "#272a33", "#4c505e"
WIN_LIT, WIN_DIM, WIN_DARK = "#f0c26a", "#a87a3e", "#0a0c12"
ROYAL_RED, ROYAL_GOLD = "#7a1820", "#c9a227"


def concrete_wall(c, x0, y0, x1, y1, lit_from_below=True, seam=16):
    """Panels with seams; warm floodlight glow rising from the base."""
    for y in range(y0, y1 + 1):
        glow = (y - y0) / max(y1 - y0, 1) if lit_from_below else 0
        for x in range(x0, x1 + 1):
            if glow ** 2.2 > BAYER[y % 4, x % 4] + 0.25:
                color = CON_LIT if glow < 0.93 else CON_HOT
            elif glow ** 1.4 > BAYER[y % 4, x % 4] + 0.1:
                color = CON_LIGHT
            else:
                color = CON
            c.set(x, y, color)

    for y in range(y0 + seam, y1, seam):
        c.line(x0, y, x1, y, CON_DARK)
    for row, y in enumerate(range(y0, y1, seam)):
        offset = 0 if row % 2 else seam
        for x in range(x0 + offset, x1, seam * 2):
            c.line(x, y, x, min(y + seam, y1), CON_DARK)

    c.line(x0, y0, x0, y1, CON_DARK)
    c.line(x1, y0, x1, y1, CON_DARK)


def window(c, x, y, w, h, state):
    c.rect(x - 1, y - 1, x + w, y + h, CON_DARK)
    color = {"lit": WIN_LIT, "dim": WIN_DIM, "dark": WIN_DARK}[state]
    c.rect(x, y, x + w - 1, y + h - 1, color)
    if state != "dark":
        c.line(x, y, x + w - 1, y, "#fff0c0" if state == "lit" else "#d0a060")
        # Blinds
        for yy in range(y + 2, y + h, 2):
            c.line(x, yy, x + w - 1, yy, WIN_DIM if state == "lit" else "#7a5a30")
    c.line(x, y + h, x + w - 1, y + h, CON_LIGHT)


def keep():
    """The central keep: a concrete bunker with a castle parapet,
    because he is the King. The banner hangs on its face."""
    w, h = 160, 176
    c = Canvas(w, h)
    x0, x1, top = 16, 143, 26

    concrete_wall(c, x0, top, x1, h - 1)

    # Crenellated parapet
    c.rect(x0 - 4, top - 4, x1 + 4, top + 3, CON_LIGHT)
    c.line(x0 - 4, top - 4, x1 + 4, top - 4, "#3e4352")
    c.line(x0 - 4, top + 3, x1 + 4, top + 3, CON_DARK)
    x = x0 - 4
    while x + 9 <= x1 + 4:
        c.rect(x, top - 13, x + 9, top - 5, CON_LIGHT)
        c.line(x, top - 13, x + 9, top - 13, "#3e4352")
        c.line(x + 9, top - 13, x + 9, top - 5, CON_DARK)
        x += 16

    # Gold trim band under the parapet
    c.line(x0, top + 5, x1, top + 5, ROYAL_GOLD)
    c.line(x0, top + 6, x1, top + 6, "#6a5418")

    # The recess where the banner hangs, and its rod brackets
    c.rect(36, top + 12, 123, top + 132, CON_DARK)
    c.line(36, top + 12, 123, top + 12, "#0c0d12")
    for bx in (38, 120):
        c.rect(bx, top + 10, bx + 1, top + 14, STEEL_LIGHT)

    # Slit windows either side of the banner
    for wy in range(top + 22, top + 120, 24):
        window(c, 22, wy, 6, 10, "lit" if wy % 48 else "dark")
        window(c, 131, wy, 6, 10, "dim" if wy % 48 else "lit")

    # Roll-up door at the base
    d0, d1, dy = 52, 107, h - 34
    c.rect(d0 - 2, dy - 3, d1 + 2, h - 1, CON_DARK)
    for y in range(dy, h - 1):
        c.line(d0, y, d1, y, STEEL if y % 3 else STEEL_DARK)
    c.line(d0, dy, d1, dy, STEEL_LIGHT)
    c.rect(d0 + 18, dy + 4, d1 - 18, dy + 10, ROYAL_RED)
    c.line(d0 + 18, dy + 4, d1 - 18, dy + 4, "#b03040")
    for k in range(0, d1 - d0, 8):     # hazard stripes at the foot
        c.line(d0 + k, h - 4, d0 + k + 3, h - 1, "#c9a227")

    # Antenna and a flag on the roof
    c.line(30, 0, 30, top - 14, STEEL_LIGHT)
    c.set(30, 0, "#ff4040")
    c.line(129, 2, 129, top - 14, STEEL_LIGHT)
    c.rect(130, 2, 138, 7, ROYAL_RED)
    c.line(130, 2, 138, 2, "#a02a34")
    c.set(133, 4, ROYAL_GOLD)
    c.set(134, 4, ROYAL_GOLD)

    c.save("keep")


def wing():
    """Barracks block either side of the keep."""
    w, h = 112, 72
    c = Canvas(w, h)
    top = 10

    concrete_wall(c, 0, top, w - 1, h - 1, seam=12)
    c.rect(0, top - 3, w - 1, top, CON_LIGHT)
    c.line(0, top - 3, w - 1, top - 3, "#3e4352")

    # Roof clutter: a vent, pipes, a satellite dish
    c.rect(10, top - 9, 21, top - 4, STEEL)
    c.line(10, top - 9, 21, top - 9, STEEL_LIGHT)
    for vx in range(11, 21, 2):
        c.line(vx, top - 8, vx, top - 5, STEEL_DARK)
    c.line(70, top - 4, 70, top - 8, STEEL_LIGHT)
    for k in range(7):
        c.set(66 + k, top - 9 - (3 - abs(3 - k)) // 2, STEEL_LIGHT)

    states = ["lit", "dark", "dim", "lit", "dark", "lit"]
    for i, wx in enumerate(range(8, w - 10, 17)):
        window(c, wx, top + 14, 9, 8, states[i % len(states)])
        window(c, wx, top + 36, 9, 8, states[(i + 3) % len(states)])

    # Pipe along the wall
    c.line(0, top + 30, w - 1, top + 30, STEEL)
    c.line(0, top + 29, w - 1, top + 29, STEEL_LIGHT)

    c.save("wing")


def chain_link(c, x0, x1, top, bottom, mesh, post, post_lit):
    for y in range(top, bottom + 1):
        for x in range(x0, x1 + 1):
            if (x + y) % 4 == 0 or (x - y) % 4 == 0:
                c.set(x, y, mesh)

    for px in range(x0, x1 + 1, 32):
        c.rect(px, top - 8, px + 2, bottom, post)
        c.line(px + 2, top - 8, px + 2, bottom, post_lit)
        # Angled arm for the barbed wire
        c.line(px, top - 8, px - 4, top - 12, post_lit)

    for wy in (top - 6, top - 10):
        for x in range(x0, x1 + 1):
            c.set(x, wy, "#3a3d48")
            if x % 5 == 0:
                c.set(x, wy - 1, "#7a7e8c")
                c.set(x, wy + 1, "#7a7e8c")

    c.line(x0, bottom, x1, bottom, post)


def fence():
    w, h = 256, 60
    c = Canvas(w, h)
    chain_link(c, 0, w - 1, 14, h - 1, "#2a2d38", "#1b1d24", "#4a4e5c")

    # Warning plates every so often
    for px in (52, 180):
        c.rect(px, 30, px + 13, 38, "#5a1a17")
        c.rect(px + 1, 31, px + 12, 37, "#b8342c")
        c.rect(px + 6, 32, px + 7, 34, "#f0e6d0")
        c.set(px + 6, 36, "#f0e6d0")
        c.set(px + 7, 36, "#f0e6d0")
    c.save("fence")


def gate_parts():
    # A sliding gate panel: steel bars in a frame, hazard stripes
    w, h = 40, 56
    c = Canvas(w, h)
    c.rect(0, 0, w - 1, h - 1, STEEL_DARK)
    for x in range(3, w - 3, 4):
        c.rect(x, 3, x + 1, h - 4, STEEL)
        c.line(x, 3, x, h - 4, STEEL_LIGHT)
    for y in (2, h // 2, h - 4):
        c.rect(1, y, w - 2, y + 1, STEEL)
        c.line(1, y, w - 2, y, STEEL_LIGHT)
    for k in range(0, w + 8, 6):
        c.line(k, h - 3, k - 3, h - 1, "#c9a227")
    c.line(0, 0, 0, h - 1, STEEL_LIGHT)
    c.save("gate_panel")

    # A concrete pillar either side
    p = Canvas(12, 76)
    concrete_wall(p, 0, 6, 11, 75, seam=10)
    p.rect(0, 0, 11, 6, CON_LIGHT)
    p.line(0, 0, 11, 0, "#3e4352")
    p.rect(3, 1, 8, 3, "#3a0d10")         # beacon base
    p.save("gate_pillar")

    # The arch sign over the gate
    text = "ROYAL PROPERTY"
    s = Canvas(text_width(text) + 10, 13)
    s.rect(0, 0, s.w - 1, s.h - 1, "#2a0a0e")
    s.rect(1, 1, s.w - 2, s.h - 2, ROYAL_RED)
    s.line(1, 1, s.w - 2, 1, "#a8303c")
    s.line(1, s.h - 2, s.w - 2, s.h - 2, "#4a1016")
    draw_text(s, text, 5, 4, ROYAL_GOLD, "#3a0c10")
    s.save("gate_sign")

    # Bright floodlit yard seen through the open gate
    g = Canvas(80, 56)
    for y in range(56):
        for x in range(80):
            d = abs(x - 40) / 40
            v = (1 - d) ** 0.8 * (0.55 + 0.45 * y / 56)
            if v > BAYER[y % 4, x % 4] * 0.9 + 0.08:
                g.set(x, y, "#f6d68a" if v > 0.62 else "#c08a4a")
            else:
                g.set(x, y, "#5a3e2a")
    g.save("gate_glow")


def beacon():
    c = Canvas(6, 5)
    c.rect(1, 0, 4, 3, "#ff2a2a")
    c.rect(2, 0, 3, 1, "#ffb0b0")
    c.rect(0, 4, 5, 4, STEEL_LIGHT)
    c.save("beacon")
    off = Canvas(6, 5)
    off.rect(1, 0, 4, 3, "#4a0c10")
    off.rect(2, 0, 3, 1, "#7a2a2a")
    off.rect(0, 4, 5, 4, STEEL_LIGHT)
    off.save("beacon_off")


def watchtower():
    w, h = 48, 160
    c = Canvas(w, h)
    wood, wood_lit, wood_dark = "#2a2420", "#4a3c30", "#15110e"

    # Stilts and X bracing
    for x in (6, 40):
        c.rect(x, 52, x + 2, h - 1, wood)
        c.line(x + 2, 52, x + 2, h - 1, wood_lit)
    for y in range(60, h - 10, 24):
        c.line(8, y, 40, y + 22, wood_dark)
        c.line(40, y, 8, y + 22, wood_dark)
        c.line(6, y, 42, y, wood)
    # Ladder
    c.line(20, 56, 20, h - 1, wood_lit)
    c.line(26, 56, 26, h - 1, wood_lit)
    for y in range(58, h - 1, 5):
        c.line(20, y, 26, y, wood)

    # Cabin with a lit slit and a roof
    c.rect(2, 26, 45, 52, wood)
    c.rect(2, 26, 45, 27, wood_lit)
    for x in range(4, 45, 5):
        c.line(x, 28, x, 52, wood_dark)
    c.rect(6, 33, 41, 38, WIN_DARK)
    c.rect(10, 34, 18, 37, WIN_DIM)
    c.rect(0, 50, 47, 53, wood_lit)
    c.line(0, 53, 47, 53, wood_dark)
    for k in range(10):
        c.line(0 + k, 25 - k, 47 - k, 25 - k, STEEL if k % 3 else STEEL_LIGHT)

    # Searchlight drum on the roof
    c.rect(18, 4, 29, 13, STEEL)
    c.line(18, 4, 29, 4, STEEL_LIGHT)
    c.rect(28, 5, 31, 12, "#fff4d0")
    c.rect(20, 14, 27, 15, STEEL_DARK)
    c.save("tower")


def flood_pole():
    c = Canvas(14, 72)
    c.rect(6, 6, 7, 71, STEEL)
    c.line(7, 6, 7, 71, STEEL_LIGHT)
    c.rect(0, 2, 13, 5, STEEL_DARK)
    for lx in (1, 8):
        c.rect(lx, 5, lx + 4, 7, STEEL)
        c.rect(lx + 1, 7, lx + 3, 7, "#fff2c4")
    c.save("flood_pole")


def pa_horns():
    """Three loudspeaker horns on a bracket: the General's voice."""
    c = Canvas(22, 16)
    c.rect(10, 8, 11, 15, STEEL)
    c.line(4, 8, 17, 8, STEEL_LIGHT)
    for cx, cy, flip in ((4, 4, -1), (17, 4, 1), (10, 2, 0)):
        if flip == 0:
            c.rect(cx - 2, cy - 2, cx + 3, cy + 3, STEEL)
            c.rect(cx - 1, cy - 1, cx + 2, cy + 2, "#0a0b10")
            c.line(cx - 2, cy - 2, cx + 3, cy - 2, STEEL_LIGHT)
            continue
        for k in range(5):
            c.line(cx + flip * k, cy - 1 - k // 2, cx + flip * k, cy + 2 + k // 2,
                   STEEL if k < 4 else STEEL_LIGHT)
        c.set(cx + flip * 4, cy, "#0a0b10")
        c.set(cx + flip * 4, cy + 1, "#0a0b10")
    c.save("pa_horns")


def yard_props():
    # Sandbags
    c = Canvas(48, 14)
    for row, y in enumerate((8, 3)):
        for x in range(row * 5, 48 - row * 5, 10):
            c.rect(x, y, x + 9, y + 5, "#3a3426")
            c.line(x, y, x + 9, y, "#5a5038")
            c.line(x + 9, y, x + 9, y + 5, "#221e16")
    c.save("sandbags")

    # Crate stack with royal stencils
    c = Canvas(44, 30)
    for x0, y0, x1, y1 in ((0, 12, 21, 29), (22, 12, 43, 29), (10, 0, 31, 11)):
        c.rect(x0, y0, x1, y1, "#3a2c1c")
        c.rect(x0 + 1, y0 + 1, x1 - 1, y1 - 1, "#4e3a24")
        c.line(x0 + 1, y0 + 1, x1 - 1, y0 + 1, "#6a5034")
        c.line(x0 + 1, y0 + 1, x1 - 1, y1 - 1, "#3a2c1c")
    draw_text(c, "ROYAL", 2, 18, "#c9a227")
    draw_text(c, "ROYAL", 24, 18, "#c9a227")
    c.save("crates")


def mini_banner(w, h):
    """The General's banner shrunk for the far shot, snapped back to
    the banner's own colours so it stays crisp pixel art."""
    src = np.array(Image.open(BANNER).convert("RGBA"))
    small = np.array(Image.fromarray(src).resize((w, h), Image.BOX)).astype(int)

    opaque = src[:, :, 3] > 0
    palette = np.unique(src[opaque][:, :3], axis=0).astype(int)

    out = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            if small[y, x, 3] < 128:
                continue
            d = np.sum((palette - small[y, x, :3]) ** 2, axis=1)
            out[y, x, :3] = palette[np.argmin(d)]
            out[y, x, 3] = 255
    return out


def compound_far():
    """The compound on the horizon at night: dark blocks, the keep
    with the General's banner, two watchtowers, lit windows and the
    fence line, as seen from the forest edge."""
    w, h = 288, 84
    c = Canvas(w, h)
    dark, rim = "#0b0e1a", "#262a40"
    base = h - 8

    # Ground haze where the floodlights hit
    for y in range(base - 4, h):
        for x in range(w):
            glow = max(0.0, 1 - abs(x - 144) / 120) * (1 - abs(y - base) / 8)
            if glow > BAYER[y % 4, x % 4] + 0.2:
                c.set(x, y, "#3a2e2c" if glow > 0.6 else "#221e26")

    def block(x0, x1, top, windows=True):
        c.rect(x0, top, x1, base, dark)
        c.line(x0, top, x1, top, rim)
        if windows:
            for wx in range(x0 + 3, x1 - 2, 5):
                for wy in range(top + 4, base - 2, 5):
                    if rng.random() < 0.45:
                        c.set(wx, wy, "#f0c060" if rng.random() < 0.6 else "#a8743a")
                        c.set(wx + 1, wy, "#c08a40")

    block(58, 120, base - 18)
    block(168, 230, base - 18)
    block(120, 168, base - 56, windows=False)

    # Keep parapet and the banner
    for x in range(120, 169, 6):
        c.rect(x, base - 59, x + 3, base - 56, dark)
        c.line(x, base - 59, x + 3, base - 59, rim)
    banner = mini_banner(24, 34)
    for y in range(34):
        for x in range(24):
            if banner[y, x, 3]:
                # Night: the banner only catches the floodlights
                lit = banner[y, x].astype(float)
                lit[:3] *= 0.62 + 0.18 * y / 34
                c.px[base - 52 + y, 132 + x] = lit.astype(np.uint8)
    for wy in range(base - 50, base - 20, 6):
        c.set(124, wy, "#f0c060")
        c.set(163, wy, "#a8743a")

    # Watchtowers with the searchlight drums lit
    for tx in (50, 238):
        c.line(tx - 3, base - 40, tx - 4, base, dark)
        c.line(tx + 3, base - 40, tx + 4, base, dark)
        c.line(tx - 3, base - 30, tx + 3, base - 20, dark)
        c.line(tx + 3, base - 30, tx - 3, base - 20, dark)
        c.rect(tx - 5, base - 47, tx + 5, base - 40, dark)
        c.line(tx - 5, base - 47, tx + 5, base - 47, rim)
        c.rect(tx - 1, base - 50, tx + 1, base - 48, "#fff2c4")

    # Fence line along the front, with floodlight poles
    for x in range(6, w - 6):
        c.set(x, base - 6, "#3a3d48")
        if x % 8 == 0:
            c.line(x, base - 7, x, base, "#2a2d38")
        elif (x + base) % 3 == 0:
            c.set(x, base - 3, "#1d2030")
    for px in (90, 144, 198):
        c.line(px, base - 14, px, base, "#2a2d38")
        c.set(px - 1, base - 15, "#fff2c4")
        c.set(px + 1, base - 15, "#fff2c4")

    c.rect(0, base + 1, w - 1, h - 1, "#0a0b14")
    c.save("compound_far")


def field_far():
    """The dark meadow between the forest edge and the compound."""
    w, h = 480, 40
    c = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            mist = (1 - y / h) ** 2 * 0.6
            c.set(x, y, "#1a1a2a" if mist > BAYER[y % 4, x % 4] + 0.15 else "#0b0c16")
    for _ in range(300):
        x, y = rng.integers(0, w), rng.integers(4, h)
        c.set(x, y, "#141626")
    c.save("field_far")


def base_ground():
    w, h = 640, 48
    c = Canvas(w, h)
    for y in range(h):
        for x in range(w):
            depth = y / h
            c.set(x, y, "#1d1b20" if depth < 0.12 else
                  ("#17151a" if depth < 0.5 else "#0f0e12"))
    for _ in range(900):
        x, y = rng.integers(0, w), rng.integers(2, h)
        c.set(x, y, "#2c2830" if rng.random() < 0.7 else "#3a3540")
    # Tyre ruts and the lit edge
    for y in (6, 10):
        for x in range(w):
            if rng.random() < 0.85:
                c.set(x, y, "#121015")
    c.line(0, 0, w - 1, 0, "#3a3038")
    c.save("base_ground")


def main():
    print("Writing AfterStage1 art to " + os.path.normpath(kit.OUT))
    skies()
    forest_layers()
    clearing_ground()
    foreground_grass("fg_grass", 480, 30, "#05060f", "#1a1c34")
    foreground_grass("fg_brush", 480, 34, "#020308", "#2a2026")
    tent_torn()
    cage_broken()
    collar_parts()
    keep()
    wing()
    fence()
    gate_parts()
    beacon()
    watchtower()
    flood_pole()
    pa_horns()
    yard_props()
    compound_far()
    field_far()
    base_ground()


if __name__ == "__main__":
    main()
