"""Pixel art for Cutscene_Intro that the forest pack doesn't have:
the dusk sky, clouds, the reserve gate and its sign, the drone,
the transport plane, the parachute, birds and a rock.

Drawn pixel by pixel at the forest pack's scale (16 pixels per
world unit), in a dusk palette. Run from the repo root:
    python Tools/cutscene_intro_art.py

Writes Assets/Cutscenes/Intro/Art/<name>.png. Unity imports them
as point-filtered sprites at 16 pixels per unit.
"""

import os

import numpy as np
from PIL import Image

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "Assets", "Cutscenes", "Intro", "Art")
PACK = os.path.join(ROOT, "Assets",
                    "2D Pixel Art Platformer Biome - American Forest",
                    "Sprites.png")

rng = np.random.default_rng(9)

# 4x4 ordered-dither thresholds
BAYER = (np.array([[0, 8, 2, 10],
                   [12, 4, 14, 6],
                   [3, 11, 1, 9],
                   [15, 7, 13, 5]]) + 0.5) / 16


def rgb(hex_color, alpha=255):
    h = hex_color.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha)


class Canvas:
    def __init__(self, width, height):
        self.w = width
        self.h = height
        self.px = np.zeros((height, width, 4), dtype=np.uint8)

    def set(self, x, y, color):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.px[y, x] = rgb(color) if isinstance(color, str) else color

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y, x]
        return (0, 0, 0, 0)

    def filled(self, x, y):
        return self.get(x, y)[3] > 0

    def rect(self, x0, y0, x1, y1, color):
        """Inclusive rectangle."""
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.set(x, y, color)

    def line(self, x0, y0, x1, y1, color):
        dx, dy = abs(x1 - x0), -abs(y1 - y0)
        sx = 1 if x0 < x1 else -1
        sy = 1 if y0 < y1 else -1
        err = dx + dy

        while True:
            self.set(x0, y0, color)
            if x0 == x1 and y0 == y1:
                return
            e2 = 2 * err
            if e2 >= dy:
                err += dy
                x0 += sx
            if e2 <= dx:
                err += dx
                y0 += sy

    def save(self, name):
        os.makedirs(OUT, exist_ok=True)
        Image.fromarray(self.px, "RGBA").save(os.path.join(OUT, name + ".png"))
        print("  %-14s %dx%d" % (name, self.w, self.h))


# =========================
# PIXEL FONT (signs)
# =========================

GLYPHS = {
    "A": [".X.", "X.X", "XXX", "X.X", "X.X"],
    "D": ["XX.", "X.X", "X.X", "X.X", "XX."],
    "E": ["XXX", "X..", "XX.", "X..", "XXX"],
    "F": ["XXX", "X..", "XX.", "X..", "X.."],
    "I": ["XXX", ".X.", ".X.", ".X.", "XXX"],
    "L": ["X..", "X..", "X..", "X..", "XXX"],
    "M": ["X...X", "XX.XX", "X.X.X", "X...X", "X...X"],
    "N": ["X..X", "XX.X", "X.XX", "X..X", "X..X"],
    "O": [".X.", "X.X", "X.X", "X.X", ".X."],
    "R": ["XX.", "X.X", "XX.", "X.X", "X.X"],
    "S": [".XX", "X..", ".X.", "..X", "XX."],
    "V": ["X.X", "X.X", "X.X", "X.X", ".X."],
    "W": ["X...X", "X...X", "X.X.X", "XX.XX", "X...X"],
    " ": ["..", "..", "..", "..", ".."],
}


def text_width(text):
    return sum(len(GLYPHS[c][0]) + 1 for c in text) - 1


def draw_text(canvas, text, x, y, color, shadow=None):
    for c in text:
        glyph = GLYPHS[c]
        for row, line in enumerate(glyph):
            for col, mark in enumerate(line):
                if mark == "X":
                    if shadow:
                        canvas.set(x + col, y + row + 1, shadow)
                    canvas.set(x + col, y + row, color)
        x += len(glyph[0]) + 1


# =========================
# SKY
# =========================

# (row, colour) stops from the top of the night to the horizon glow
SKY_STOPS = [
    (0, "#0e1230"), (40, "#161a3d"), (62, "#22224d"), (80, "#35295c"),
    (96, "#4f3066"), (110, "#70386b"), (122, "#96436c"), (134, "#bd5568"),
    (146, "#dc7262"), (158, "#ee9563"), (170, "#f6b86e"), (184, "#fad688"),
]


def sky():
    w, h = 384, 256
    c = Canvas(w, h)
    rows = [s[0] for s in SKY_STOPS]
    colors = [rgb(s[1]) for s in SKY_STOPS]

    for y in range(h):
        # Position between the two nearest stops
        i = np.searchsorted(rows, y, side="right") - 1
        i = min(max(i, 0), len(rows) - 2)
        frac = np.clip((y - rows[i]) / (rows[i + 1] - rows[i]), 0, 1)

        # Solid bands with a dithered seam between them
        frac = np.clip((frac - 0.5) * 2.5 + 0.5, 0, 1)

        for x in range(w):
            upper = frac > BAYER[y % 4, x % 4]
            c.set(x, y, colors[i + 1] if upper else colors[i])

    # Stars, thinning out towards the glow
    for _ in range(90):
        x, y = rng.integers(0, w), int(abs(rng.normal(0, 45)))
        if y < 100:
            bright = rng.random() < 0.25
            c.set(x, y, "#e8e4ff" if bright else "#8d89c0")

    # Low sun with a dithered halo
    sx, sy = 272, 166
    for y in range(sy - 34, sy + 35):
        for x in range(sx - 34, sx + 35):
            d = np.hypot(x - sx, y - sy)
            if d <= 11:
                c.set(x, y, "#fff6d6")
            elif d <= 12.5:
                c.set(x, y, "#ffe7a6")
            elif d <= 32:
                glow = 1 - (d - 12.5) / 19.5
                if glow ** 1.6 > BAYER[y % 4, x % 4]:
                    c.set(x, y, "#fbd28a" if d < 20 else "#f6b86e")

    # Thin streaks of cloud lit pink from below
    for cx, cy, length in ((70, 128, 90), (150, 141, 60), (330, 120, 70),
                           (230, 112, 44), (20, 150, 50)):
        for x in range(cx - length // 2, cx + length // 2):
            edge = abs(x - cx) / (length / 2)
            thick = 1 + int(3 * (1 - edge ** 2))
            for k in range(thick):
                c.set(x, cy - k, "#7a3a6a" if k else "#f39c6a")
            c.set(x, cy - thick, "#5e3164")

    c.save("sky")


# =========================
# CLOUDS
# =========================

def cloud(name, w, h, puffs):
    c = Canvas(w, h)
    mask = np.zeros((h, w), bool)
    base = h - 3

    for _ in range(puffs):
        cx = rng.uniform(w * 0.15, w * 0.85)
        r = rng.uniform(h * 0.25, h * 0.55)
        cy = base - r * rng.uniform(0.2, 0.8)
        yy, xx = np.mgrid[0:h, 0:w]
        mask |= ((xx - cx) / 1.25) ** 2 + (yy - cy) ** 2 <= r * r

    mask[base + 1:, :] = False

    for y in range(h):
        for x in range(w):
            if not mask[y, x]:
                continue

            below = 0
            while y + below + 1 < h and mask[y + below + 1, x]:
                below += 1

            above = 0
            while y - above - 1 >= 0 and mask[y - above - 1, x]:
                above += 1

            if below == 0:
                color = "#f7b77a"   # lit underside
            elif below <= 2:
                color = "#e0806a"
            elif above <= 1:
                color = "#5a2f5e"   # dark top, away from the sun
            else:
                color = "#7f3e68"
                if below <= 4 and BAYER[y % 4, x % 4] > 0.5:
                    color = "#a24f6a"
            c.set(x, y, color)

    c.save(name)


# =========================
# THE RESERVE GATE
# =========================

WOOD_DARK, WOOD, WOOD_LIGHT = "#33200f", "#4a2f1f", "#6e4a2c"
RIM = "#c77a45"
IRON_DARK, IRON, IRON_LIGHT = "#22222a", "#33333d", "#6b6b7a"


def log_post(c, x0, x1, y0, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            if x <= x0 + 1:
                color = WOOD_DARK
            elif x >= x1 - 1:
                color = WOOD_LIGHT
            else:
                color = WOOD
            c.set(x, y, color)
        c.set(x1, y, RIM if y % 7 else WOOD_LIGHT)

    # Bark grain and a knot
    for x in range(x0 + 3, x1 - 1, 3):
        y = y0 + rng.integers(0, 10)
        while y < y1:
            length = rng.integers(4, 12)
            c.line(x, y, x, min(y + length, y1), WOOD_DARK)
            y += length + rng.integers(3, 8)

    ky = (y0 + y1) // 2 + rng.integers(-10, 10)
    c.rect(x0 + 4, ky, x0 + 6, ky + 2, WOOD_DARK)
    c.set(x0 + 5, ky + 1, WOOD_LIGHT)


def chain_link_fence(c, x0, x1, top, bottom):
    for y in range(top, bottom + 1):
        for x in range(x0, x1 + 1):
            if (x + y) % 4 == 0 or (x - y) % 4 == 0:
                c.set(x, y, "#3c3e47")

    for px in range(x0, x1 + 1, 28):
        c.rect(px, top - 6, px + 2, bottom, "#2c2e36")
        c.line(px + 2, top - 6, px + 2, bottom, "#5c5e68")

    # Barbed wire along the top
    for x in range(x0, x1 + 1):
        c.set(x, top - 4, "#5c5e68")
        if x % 5 == 0:
            c.set(x, top - 5, "#8d8f99")
            c.set(x, top - 3, "#8d8f99")


def gate():
    w, h = 256, 104
    c = Canvas(w, h)
    g0 = 56  # left edge of the gate itself

    chain_link_fence(c, 0, g0 + 6, 46, h - 1)
    chain_link_fence(c, g0 + 138, w - 1, 46, h - 1)

    # Doors, behind the posts
    for d0, d1 in ((g0 + 20, g0 + 71), (g0 + 72, g0 + 123)):
        for x in range(d0 + 2, d1 - 1, 6):
            c.rect(x, 44, x + 1, h - 5, IRON)
            c.set(x + 1, 44, IRON_LIGHT)
            c.line(x + 1, 45, x + 1, h - 5, IRON_LIGHT)
            # Spear tips
            c.set(x, 42, IRON_LIGHT)
            c.set(x + 1, 42, IRON_LIGHT)
            c.set(x, 43, IRON)
            c.set(x + 1, 43, IRON)
            c.set(x, 40, IRON_LIGHT)
            c.set(x + 1, 41, IRON_LIGHT)
            c.set(x, 41, IRON_LIGHT)

        for ry in (48, 76, h - 8):
            c.rect(d0, ry, d1, ry + 2, IRON_DARK)
            c.line(d0, ry, d1, ry, IRON_LIGHT)

        c.rect(d0, 44, d0 + 1, h - 5, IRON_DARK)
        c.rect(d1 - 1, 44, d1, h - 5, IRON_DARK)
        c.line(d0 + 2, h - 9, d1 - 2, 50, IRON_DARK)
        c.line(d0 + 2, h - 8, d1 - 2, 51, IRON_DARK)

    # Warning plate on the right door
    c.rect(g0 + 96, 56, g0 + 109, 64, "#5a1a17")
    c.rect(g0 + 97, 57, g0 + 108, 63, "#a8322d")
    c.rect(g0 + 102, 58, g0 + 103, 60, "#f0e6d0")
    c.rect(g0 + 102, 62, g0 + 103, 62, "#f0e6d0")

    # Posts and the crossbeam
    log_post(c, g0 + 8, g0 + 19, 10, h - 1)
    log_post(c, g0 + 124, g0 + 135, 10, h - 1)

    c.rect(g0 + 2, 12, g0 + 141, 21, WOOD)
    c.line(g0 + 2, 12, g0 + 141, 12, RIM)
    c.line(g0 + 2, 13, g0 + 141, 13, WOOD_LIGHT)
    c.rect(g0 + 2, 20, g0 + 141, 21, WOOD_DARK)
    for x in range(g0 + 6, g0 + 140, 9):
        c.line(x, 15, x + 5, 15, WOOD_DARK)
    for ex in (g0 + 2, g0 + 138):
        c.rect(ex, 13, ex + 3, 20, "#8a6a48")
        c.rect(ex + 1, 15, ex + 2, 18, "#5e4128")

    # Claw marks gouged into the left post
    for k in range(3):
        c.line(g0 + 9, 52 + k * 4, g0 + 18, 60 + k * 4, "#a07a52")
        c.line(g0 + 9, 53 + k * 4, g0 + 18, 61 + k * 4, WOOD_DARK)

    # Heavy chain wrapped across the gap in an X, and a padlock
    mid = g0 + 72
    for start, end in (((mid - 15, 59), (mid + 15, 75)),
                       ((mid - 15, 75), (mid + 15, 59))):
        steps = 14
        for k in range(steps + 1):
            x = round(start[0] + (end[0] - start[0]) * k / steps)
            y = round(start[1] + (end[1] - start[1]) * k / steps)
            if k % 2 == 0:
                # A link seen face on: a ring
                c.rect(x - 1, y - 1, x + 2, y + 1, "#4b4d57")
                c.line(x - 1, y - 1, x + 2, y - 1, "#c9cbd4")
                c.set(x - 1, y, "#8d8f99")
                c.set(x + 2, y, "#8d8f99")
                c.line(x, y + 1, x + 1, y + 1, "#8d8f99")
                c.rect(x, y, x + 1, y, IRON_DARK)
            else:
                # A link side on: a short bar
                c.rect(x - 1, y, x + 2, y, "#8d8f99")
                c.set(x, y, "#c9cbd4")

    c.rect(mid - 2, 74, mid + 2, 74, "#b8bac4")
    c.rect(mid - 3, 75, mid - 2, 78, "#b8bac4")
    c.rect(mid + 2, 75, mid + 3, 78, "#8d8f99")
    c.rect(mid - 4, 78, mid + 4, 86, "#b38a3c")
    c.line(mid - 4, 78, mid + 4, 78, "#e2c070")
    c.line(mid - 4, 79, mid - 4, 86, "#e2c070")
    c.line(mid + 4, 79, mid + 4, 86, "#7a5a22")
    c.line(mid - 4, 86, mid + 4, 86, "#7a5a22")
    c.rect(mid, 81, mid, 83, "#2a1a0a")

    c.save("gate")


def gate_sign():
    w, h = 76, 30
    c = Canvas(w, h)

    # Short chains up to the beam
    for x in (14, 61):
        for y in range(0, 9):
            c.set(x, y, "#c9cbd4" if y % 3 == 0 else "#6c6e78")

    c.rect(0, 8, w - 1, h - 1, "#2e1d10")
    c.rect(1, 9, w - 2, 18, "#7a5534")
    c.rect(1, 19, w - 2, h - 2, "#6c4a2d")
    c.line(1, 9, w - 2, 9, "#93693f")
    c.line(1, 19, w - 2, 19, "#82593a")
    c.line(1, 18, w - 2, 18, "#4f3420")
    c.line(1, h - 2, w - 2, h - 2, "#4f3420")
    for x, y in ((3, 11), (w - 4, 11), (3, h - 4), (w - 4, h - 4)):
        c.set(x, y, "#c9cbd4")

    top, bottom = "MANEFORD", "WILDLIFE RESERVE"
    draw_text(c, top, (w - text_width(top)) // 2, 11, "#ead9b0", "#3a2414")
    draw_text(c, bottom, (w - text_width(bottom)) // 2 + 1, 21,
              "#ead9b0", "#3a2414")

    # Three claw gouges, beside the name so it stays readable
    for k in range(3):
        c.line(59 + k * 4, 10, 63 + k * 4, 17, "#2a170b")
        c.line(60 + k * 4, 10, 64 + k * 4, 17, "#b08a5c")

    c.save("gate_sign")


def lantern():
    c = Canvas(7, 11)
    c.rect(2, 0, 4, 0, IRON)
    c.set(3, 1, IRON)
    c.rect(1, 2, 5, 2, IRON_DARK)
    c.rect(1, 3, 5, 8, "#ffd27a")
    c.rect(2, 4, 4, 7, "#fff2c0")
    for x in (1, 3, 5):
        c.line(x, 3, x, 8, IRON_DARK)
    c.rect(1, 9, 5, 9, IRON_DARK)
    c.rect(2, 10, 4, 10, IRON)
    c.save("lantern")


def ground():
    """A strip of the forest pack's grass-topped earth."""
    pack = np.array(Image.open(PACK).convert("RGBA"))
    top = pack[464:480, 32:48]       # TileGround2: grass edge
    earth = pack[480:496, 32:48]     # TileGround5: earth

    w = 480
    strip = np.zeros((48, w, 4), np.uint8)
    for x in range(0, w, 16):
        strip[0:16, x:x + 16] = top
        strip[16:32, x:x + 16] = earth
        strip[32:48, x:x + 16] = earth

    c = Canvas(w, 48)
    c.px = strip

    # Tufts of tall grass along the edge
    for x in range(2, w - 2, 3):
        if rng.random() < 0.55:
            tall = rng.integers(2, 6)
            c.line(x, 1, x + rng.integers(-1, 2), 1 - tall, "#3f6b3a")

    c.save("ground")


# =========================
# DRONE, PLANE, PARACHUTE
# =========================

def drone_flying():
    c = Canvas(13, 6)
    dark = "#1c1830"
    c.rect(4, 2, 8, 3, dark)
    c.line(1, 2, 11, 2, dark)
    c.rect(0, 0, 3, 0, "#3a3552")
    c.rect(9, 0, 12, 0, "#3a3552")
    c.set(1, 1, dark)
    c.set(11, 1, dark)
    c.set(5, 4, dark)
    c.set(7, 4, dark)
    c.set(6, 3, "#ff4646")
    c.save("drone")


def drone_wreck():
    c = Canvas(28, 12)
    body, light, shade = "#50566a", "#8a92a8", "#2c3040"

    c.rect(8, 5, 18, 9, body)
    c.line(8, 5, 18, 5, light)
    c.line(8, 9, 18, 9, shade)
    c.rect(11, 9, 14, 10, shade)
    c.rect(12, 7, 13, 8, "#14141c")    # cracked lens
    c.set(12, 7, "#9fe0ff")

    c.line(3, 6, 8, 7, body)           # bent arm
    c.line(0, 4, 5, 5, light)          # rotor, snapped
    c.line(18, 6, 23, 3, body)         # arm, sticking up
    c.line(21, 1, 26, 2, light)        # rotor
    c.line(19, 11, 26, 11, body)       # broken-off arm lying beside it

    c.set(16, 4, "#e04040")            # loose wires
    c.set(17, 3, "#e04040")
    c.set(15, 4, "#f0c040")
    c.set(15, 3, "#f0c040")

    c.save("drone_wreck")


def plane():
    w, h = 64, 18
    c = Canvas(w, h)
    dark, top, rim = "#1a1730", "#2c2748", "#c96a5a"

    # Fuselage, nose to the right
    for x in range(4, 60):
        half = 3 if x < 52 else max(1, 3 - (x - 52) // 3)
        cy = 9 if x < 50 else 9 - (x - 50) // 5
        for y in range(cy - half, cy + half + 1):
            c.set(x, y, dark)
        c.set(x, cy - half, top)
        c.set(x, cy + half, rim if x % 2 else dark)

    # Tail fin and tailplane
    for k in range(9):
        c.line(4 + k // 2, 6 - k, 12 - k // 3, 6 - k, dark)
    c.line(2, 8, 14, 8, dark)

    # High wing seen edge-on, engines and prop blur
    c.line(18, 5, 46, 5, dark)
    c.line(18, 4, 46, 4, top)
    for ex in (26, 38):
        c.rect(ex, 5, ex + 5, 7, dark)
        c.line(ex + 6, 3, ex + 6, 9, "#3c365c")

    c.save("plane")


def parachute():
    w, h = 44, 40
    c = Canvas(w, h)
    dark, mid, light, high = "#3d4a26", "#5a6b33", "#7f924a", "#a4b468"
    under, rim = "#2a331b", "#d39a5a"

    cx, top, base = 21.5, 1, 15
    for y in range(top, base + 1):
        for x in range(w):
            u = (x - cx) / 22
            dome = top + (base - top) * (u ** 2) * 0.9
            if y < dome or abs(u) > 1:
                continue

            # Panels: alternating shades, light from the right
            panel = int((u + 1) * 4.5)
            shade = [dark, mid, mid, light, light, high, light, mid, dark]
            color = shade[min(panel, 8)]

            if y >= base - 1:
                color = under
            elif y >= base - 2 and u > 0.2:
                color = rim
            c.set(x, y, color)

    # Scalloped hem
    for x in range(0, w, 5):
        c.set(x, base + 1, under)
        c.set(x + 1, base + 1, under)

    # Suspension lines down to the harness
    harness = (22, h - 1)
    for ax in (1, 9, 16, 27, 34, 42):
        c.line(ax, base + 1, harness[0], harness[1], "#b8b096")

    c.save("parachute")


def birds():
    frames = {
        "bird_0": ["X.....X", ".X...X.", "..XXX..", "......."],
        "bird_1": [".......", "...X...", ".XXXXX.", "X.....X"],
    }

    for name, rows in frames.items():
        c = Canvas(7, 4)
        for y, row in enumerate(rows):
            for x, mark in enumerate(row):
                if mark == "X":
                    c.set(x, y, "#1a1226")
        c.save(name)


# =========================
# DUSK VERSIONS OF THE PACK
# =========================

# Pack sprite -> (name, body colour, rim colour). Farther layers
# fade towards the haze of the horizon, nearer ones go dark.
DUSK_LAYERS = {
    "Background4": ("layer_mist", "#c4747a", "#eaa27c"),
    "Background3": ("layer_hills", "#8a4f6e", "#c97a78"),
    "Background2": ("layer_pines_far", "#4f3058", "#8a4f6e"),
    "Background1": ("layer_pines_near", "#2a1c38", "#6a3a5a"),
}

# Unity sprite rects (x, y from the bottom, w, h) in Sprites.png
PACK_RECTS = {
    "Background1": (0, 368, 256, 272),
    "Background2": (256, 368, 256, 272),
    "Background3": (512, 368, 256, 272),
    "Background4": (768, 368, 256, 272),
    "Tree4": (272, 192, 64, 112),
    "Tree5": (336, 192, 64, 112),
}


def pack_sprite(pack, name):
    x, y, w, h = PACK_RECTS[name]
    top = pack.shape[0] - y - h
    return pack[top:top + h, x:x + w]


def silhouette(source, body, rim, repeat=1):
    """Recolours a sprite to one tone, keeping a little of its
    shading, with a warm rim along every top edge."""
    alpha = source[:, :, 3] > 0
    shade = source[:, :, :3].astype(float).mean(axis=2)
    lit = shade > np.median(shade[alpha]) + 12 if alpha.any() else alpha

    h, w = alpha.shape
    c = Canvas(w * repeat, h)
    body_c, rim_c = rgb(body), rgb(rim)
    light_c = tuple(min(255, int(v * 1.12)) for v in body_c[:3]) + (255,)

    for y in range(h):
        for x in range(w):
            if not alpha[y, x]:
                continue
            color = light_c if lit[y, x] else body_c
            if y == 0 or not alpha[y - 1, x]:
                color = rim_c
            for k in range(repeat):
                c.px[y, x + k * w] = color

    return c


def dusk_layers():
    pack = np.array(Image.open(PACK).convert("RGBA"))

    for sprite, (name, body, rim) in DUSK_LAYERS.items():
        # Three repeats side by side, so one sprite covers a pan
        layer = silhouette(pack_sprite(pack, sprite), body, rim, 3)
        rows = np.where(layer.px[:, :, 3].any(axis=1))[0]
        solid = np.where(layer.px[:, :, 3].all(axis=1))[0]
        layer.save(name)
        print("    first row %d, solid from row %d" % (rows[0], solid[0]))

    for sprite, name in (("Tree4", "tree_dark_1"), ("Tree5", "tree_dark_2")):
        silhouette(pack_sprite(pack, sprite), "#160f22", "#3c2440").save(name)


def rock():
    """A dark outcrop for the lion to stand on in the title shot."""
    w, h = 96, 44
    c = Canvas(w, h)
    heights = np.convolve(rng.uniform(0, 1, w + 8), np.ones(9) / 9, "valid")

    for x in range(w):
        edge = abs(x - w * 0.42) / (w * 0.5)
        surface = int(6 + 30 * edge ** 1.8 + heights[x] * 6)
        for y in range(surface, h):
            c.set(x, y, "#140f1e")
        c.set(x, surface, "#d3765a")
        c.set(x, surface + 1, "#5a2e3e")

    c.save("rock")


def main():
    print("Writing intro art to " + os.path.normpath(OUT))
    sky()
    cloud("cloud_1", 96, 28, 7)
    cloud("cloud_2", 64, 20, 5)
    cloud("cloud_3", 128, 26, 9)
    gate()
    gate_sign()
    lantern()
    ground()
    drone_flying()
    drone_wreck()
    plane()
    parachute()
    birds()
    rock()
    dusk_layers()


if __name__ == "__main__":
    main()
