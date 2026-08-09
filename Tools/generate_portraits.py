"""Generates placeholder pixel-art portraits for the dialogue cast, plus the
small UI textures the dialogue box uses.

Each speaker gets one 64x64 portrait per emotion (neutral, angry, smug,
surprised, panic, soft). They face right; the right-hand portrait slot
mirrors them. Replace any PNG with real art later and keep its name.

Run from the repo root:
    python Tools/generate_portraits.py

Writes Assets/Dialogue/Portraits/<speaker>_<emotion>.png and
Assets/Dialogue/UI/*.png
"""

import os

import numpy as np
from PIL import Image, ImageDraw

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "Dialogue")
PORTRAITS = os.path.join(ROOT, "Portraits")
UI = os.path.join(ROOT, "UI")

W = H = 64
EMOTIONS = ["neutral", "angry", "smug", "surprised", "panic", "soft"]
OUTLINE = (24, 18, 30, 255)


def rgb(hex_color, alpha=255):
    h = hex_color.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha)


# =========================
# PIXEL PAINTING
# =========================

class Pic:
    def __init__(self):
        self.px = np.zeros((H, W, 4), np.uint8)

    # ----- masks -----
    @staticmethod
    def _mask(draw_fn):
        im = Image.new("L", (W, H), 0)
        draw_fn(ImageDraw.Draw(im))
        return np.array(im) > 0

    def ellipse(self, box):
        return self._mask(lambda d: d.ellipse(box, fill=255))

    def rect(self, box):
        return self._mask(lambda d: d.rectangle(box, fill=255))

    def poly(self, points):
        return self._mask(lambda d: d.polygon(points, fill=255))

    def line(self, points, width=1):
        return self._mask(lambda d: d.line(points, fill=255, width=width))

    # ----- painting -----
    def fill(self, mask, color):
        self.px[mask] = color

    def dot(self, x, y, color):
        if 0 <= x < W and 0 <= y < H:
            self.px[y, x] = color

    def dots(self, points, color):
        for x, y in points:
            self.dot(x, y, color)

    def solid(self):
        return self.px[:, :, 3] > 0

    def shade(self, mask, color, dx, dy, width=1):
        """Paint the edge band of `mask` that faces direction (dx, dy)."""
        inner = mask.copy()
        for k in range(1, width + 1):
            inner &= shift(mask, -dx * k, -dy * k)
        self.fill(mask & ~inner, color)

    def outline(self, color=OUTLINE):
        a = self.solid()
        grown = a | shift(a, 1, 0) | shift(a, -1, 0) | shift(a, 0, 1) | shift(a, 0, -1)
        self.px[grown & ~a] = color

    def mirror_ok(self):
        return self

    def save(self, path, scale=1):
        img = Image.fromarray(self.px, "RGBA")
        if scale != 1:
            img = img.resize((W * scale, H * scale), Image.NEAREST)
        img.save(path)


def shift(mask, dx, dy):
    """Move a mask by (dx, dy) pixels (things outside become False)."""
    out = np.zeros_like(mask)
    h, w = mask.shape
    xs = slice(max(dx, 0), w + min(dx, 0))
    xd = slice(max(-dx, 0), w + min(-dx, 0))
    ys = slice(max(dy, 0), h + min(dy, 0))
    yd = slice(max(-dy, 0), h + min(-dy, 0))
    out[ys, xs] = mask[yd, xd]
    return out


# =========================
# SHARED BODY PARTS
# =========================

HEAD = (19, 8, 45, 40)


def head_mask(p, chin=True):
    m = p.ellipse(HEAD)
    if chin:
        # a slightly pointed jaw on the side the face looks towards
        m |= p.poly([(24, 30), (44, 28), (40, 40), (33, 43), (27, 40)])
    return m


def draw_head(p, skin, shadow, highlight=None):
    m = head_mask(p)
    p.fill(m, skin)
    p.shade(m, shadow, -1, 1, 2)          # shadow on the back of the head and under the jaw
    if highlight:
        p.shade(m, highlight, 1, -1, 1)
    # ear (on the far side, we see it on the left)
    ear = p.ellipse((17, 22, 22, 30))
    p.fill(ear, skin)
    p.fill(p.rect((19, 24, 20, 27)), shadow)
    # nose points right
    p.dots([(42, 26), (42, 27), (43, 28)], skin)
    p.dots([(41, 28), (42, 29)], shadow)
    return m


def draw_neck(p, skin, shadow):
    neck = p.rect((27, 36, 37, 47))
    p.fill(neck, skin)
    p.fill(p.rect((27, 36, 30, 47)), shadow)


def draw_torso(p, color, shadow, top=46):
    body = p.poly([(4, 63), (8, top + 4), (20, top), (44, top), (56, top + 4), (60, 63)])
    p.fill(body, color)
    p.shade(body, shadow, -1, 0, 3)
    return body


# =========================
# FACES
# =========================

EYE_L = 28   # x of the near eye
EYE_R = 36   # x of the far eye
EYE_Y = 23


def draw_eyes(p, kind, iris, white=(240, 240, 236, 255), skin_shadow=None):
    dark = iris
    if kind == "normal":
        for x in (EYE_L, EYE_R):
            p.fill(p.rect((x, EYE_Y, x + 1, EYE_Y + 1)), dark)
            p.dot(x + 1, EYE_Y, white)
    elif kind == "narrow":
        for x in (EYE_L, EYE_R):
            p.fill(p.rect((x - 1, EYE_Y + 1, x + 2, EYE_Y + 1)), dark)
    elif kind == "lidded":
        for x in (EYE_L, EYE_R):
            p.fill(p.rect((x, EYE_Y + 1, x + 1, EYE_Y + 1)), dark)
            if skin_shadow:
                p.fill(p.rect((x - 1, EYE_Y, x + 2, EYE_Y)), skin_shadow)
    elif kind == "wide":
        for x in (EYE_L, EYE_R):
            p.fill(p.rect((x - 1, EYE_Y - 1, x + 2, EYE_Y + 2)), white)
            p.fill(p.rect((x, EYE_Y, x + 1, EYE_Y + 1)), dark)
    elif kind == "tiny":            # panicked: big whites, pin-prick pupils
        for x in (EYE_L, EYE_R):
            p.fill(p.rect((x - 1, EYE_Y - 1, x + 2, EYE_Y + 2)), white)
            p.dot(x + 1, EYE_Y, dark)
    elif kind == "closed":          # happy ^ ^
        for x in (EYE_L, EYE_R):
            p.dots([(x - 1, EYE_Y + 1), (x, EYE_Y), (x + 1, EYE_Y), (x + 2, EYE_Y + 1)], dark)


def draw_brows(p, kind, color):
    l, r = EYE_L, EYE_R
    y = EYE_Y - 3
    shapes = {
        "flat": [((l - 1, y), (l + 2, y)), ((r - 1, y), (r + 2, y))],
        "angry": [((l - 1, y - 1), (l + 2, y + 1)), ((r - 1, y + 1), (r + 2, y - 1))],
        "raised": [((l - 1, y - 2), (l + 2, y - 2)), ((r - 1, y - 2), (r + 2, y - 2))],
        "worried": [((l - 1, y), (l + 2, y - 2)), ((r - 1, y - 2), (r + 2, y))],
        "smug": [((l - 1, y), (l + 2, y)), ((r - 1, y - 1), (r + 2, y - 2))],
        "soft": [((l - 1, y - 1), (l + 2, y - 1)), ((r - 1, y - 1), (r + 2, y - 1))],
    }
    for a, b in shapes[kind]:
        p.fill(p.line([a, b]), color)
        p.fill(p.line([(a[0], a[1] + 1), (b[0], b[1] + 1)]) & p.line([a, b]), color)


MOUTH_Y = 33


def draw_mouth(p, kind, dark, teeth=(245, 245, 240, 255), lip=None):
    y = MOUTH_Y
    if kind == "flat":
        p.fill(p.line([(32, y), (37, y)]), dark)
    elif kind == "frown":
        p.dots([(31, y + 1), (32, y), (33, y), (34, y), (35, y), (36, y), (37, y + 1)], dark)
    elif kind == "smirk":
        p.dots([(31, y), (32, y), (33, y), (34, y), (35, y), (36, y - 1), (37, y - 2)], dark)
    elif kind == "smile":
        p.dots([(31, y - 1), (32, y), (33, y + 1), (34, y + 1), (35, y + 1), (36, y), (37, y - 1)], dark)
    elif kind == "o":
        p.fill(p.ellipse((33, y - 1, 36, y + 2)), dark)
    elif kind == "open":            # panicked yell
        p.fill(p.rect((31, y - 1, 38, y + 3)), dark)
        p.fill(p.rect((32, y - 1, 37, y - 1)), teeth)
    elif kind == "grit":            # clenched teeth
        p.fill(p.rect((31, y - 1, 38, y + 1)), teeth)
        p.fill(p.line([(31, y), (38, y)]), (190, 180, 175, 255))
        p.shade(p.rect((30, y - 2, 39, y + 2)), dark, 0, 0)
        p.fill(p.rect((30, y - 2, 39, y - 2)) | p.rect((30, y + 2, 39, y + 2)), dark)
        p.fill(p.rect((30, y - 1, 30, y + 1)) | p.rect((39, y - 1, 39, y + 1)), dark)


FACES = {
    #            eyes      brows      mouth
    "neutral":  ("normal", "flat",    "flat"),
    "angry":    ("narrow", "angry",   "grit"),
    "smug":     ("lidded", "smug",    "smirk"),
    "surprised": ("wide",  "raised",  "o"),
    "panic":    ("tiny",   "worried", "open"),
    "soft":     ("closed", "soft",    "smile"),
}


def draw_face(p, emotion, iris, brow, mouth, skin_shadow, blush=None, sweat=True):
    eyes, brows, mouth_kind = FACES[emotion]
    draw_eyes(p, eyes, iris, skin_shadow=skin_shadow)
    draw_brows(p, brows, brow)
    draw_mouth(p, mouth_kind, mouth)
    if emotion == "soft" and blush:
        p.dots([(26, 28), (27, 28), (38, 28), (39, 28)], blush)
    if emotion == "panic" and sweat:
        drop = (150, 210, 255, 255)
        p.dots([(23, 14), (22, 15), (23, 15), (22, 16), (23, 16)], drop)
        p.dot(23, 14, (230, 245, 255, 255))
    if emotion == "angry":
        vein = (200, 40, 40, 255)
        p.dots([(40, 12), (42, 12), (41, 13), (40, 14), (42, 14)], vein)


# =========================
# CHARACTERS
# =========================

def rook(emotion):
    """Deadpan commando: red bandana, stubble, olive tank top, dog tags."""
    skin, shadow = rgb("#d39a6a"), rgb("#a8724a")
    p = Pic()
    draw_torso(p, rgb("#5b6b3a"), rgb("#434f2a"))
    # bare shoulders around the tank top
    p.fill(p.poly([(6, 63), (9, 51), (19, 47), (21, 63)]), skin)
    p.fill(p.poly([(43, 47), (55, 51), (58, 63), (45, 63)]), skin)
    p.fill(p.poly([(6, 63), (9, 51), (12, 50), (10, 63)]), shadow)
    draw_neck(p, skin, shadow)
    draw_head(p, skin, shadow)
    # short dark hair
    hair = p.ellipse((18, 5, 46, 24)) & ~p.rect((30, 15, 47, 30))
    hair |= p.rect((18, 12, 23, 24))
    p.fill(hair, rgb("#3a2a20"))
    p.shade(hair, rgb("#5a4030"), 1, -1, 1)
    # bandana across the forehead, knot tails behind
    band = p.poly([(18, 13), (46, 13), (46, 17), (18, 18)])
    p.fill(band, rgb("#c8332b"))
    p.fill(p.line([(18, 18), (46, 17)]), rgb("#8f1f1a"))
    p.fill(p.poly([(17, 15), (8, 19), (10, 22), (18, 18)]), rgb("#c8332b"))
    p.fill(p.poly([(17, 17), (11, 24), (14, 25), (19, 19)]), rgb("#a8291f"))
    # stubble along the jaw
    for x, y in [(30, 37), (32, 38), (34, 38), (36, 37), (38, 36), (40, 35), (31, 39), (35, 39), (39, 37)]:
        p.dot(x, y, shadow)
    draw_face(p, emotion, rgb("#2a1c14"), rgb("#2e2018"), rgb("#6b3a2a"), shadow, blush=rgb("#e08070"))
    # dog tags
    p.fill(p.line([(25, 47), (32, 55), (39, 47)]), rgb("#b8bec6"))
    p.fill(p.rect((31, 55, 33, 58)), rgb("#d8dde3"))
    p.outline()
    return p


def pip(emotion):
    """Wildlife vet on the radio: glasses, headset, bun, teal scrubs."""
    skin, shadow = rgb("#8d5a3b"), rgb("#6a4129")
    p = Pic()
    draw_torso(p, rgb("#2a9d8f"), rgb("#1f776c"))
    # white coat collar
    p.fill(p.poly([(14, 63), (18, 47), (27, 46), (31, 63)]), rgb("#ececec"))
    p.fill(p.poly([(36, 63), (38, 46), (47, 47), (51, 63)]), rgb("#ececec"))
    p.fill(p.poly([(14, 63), (18, 47), (20, 47), (17, 63)]), rgb("#c4c8cc"))
    draw_neck(p, skin, shadow)
    draw_head(p, skin, shadow)
    # black hair with a bun on the back of the head; bangs swept back
    hair_color, hair_light = rgb("#1d1a22"), rgb("#3a3444")
    hair = p.ellipse((18, 5, 46, 22)) & ~p.poly([(26, 17), (40, 12), (47, 12), (47, 26), (24, 26)])
    hair |= p.rect((18, 10, 22, 28))
    p.fill(hair, hair_color)
    bun = p.ellipse((10, 3, 21, 14))
    p.fill(bun, hair_color)
    p.shade(bun | hair, hair_light, 1, -1, 1)
    draw_face(p, emotion, rgb("#1a1010"), rgb("#1d1a22"), rgb("#4a2418"), shadow, blush=rgb("#c06050"))
    # round glasses
    frame = rgb("#202028")
    for x in (EYE_L, EYE_R):
        ring = p.ellipse((x - 3, EYE_Y - 3, x + 4, EYE_Y + 4)) & ~p.ellipse((x - 2, EYE_Y - 2, x + 3, EYE_Y + 3))
        p.fill(ring, frame)
    p.fill(p.line([(EYE_L + 4, EYE_Y), (EYE_R - 3, EYE_Y)]), frame)
    p.dot(EYE_L - 1, EYE_Y - 2, (210, 235, 255, 255))
    p.dot(EYE_R - 1, EYE_Y - 2, (210, 235, 255, 255))
    # headset: band over the head, earpiece, mic boom to the mouth
    gray, dark = rgb("#6b7280"), rgb("#3f4550")
    p.fill(p.line([(19, 22), (22, 8), (30, 4), (36, 5)], 2), gray)
    p.fill(p.ellipse((16, 21, 23, 30)), dark)
    p.fill(p.line([(21, 29), (27, 35), (32, 35)], 1), gray)
    p.fill(p.rect((32, 34, 34, 36)), rgb("#22d3ee"))
    p.outline()
    return p


def tala(emotion):
    """Reserve ranger: campaign hat, long braid, green shirt, badge."""
    skin, shadow = rgb("#b07850"), rgb("#86583a")
    p = Pic()
    draw_torso(p, rgb("#4f6b3a"), rgb("#3b5129"))
    p.fill(p.poly([(26, 46), (32, 54), (38, 46)]), rgb("#3b5129"))      # collar V
    p.fill(p.rect((44, 52, 48, 56)), rgb("#e2b33c"))                    # badge
    p.dot(46, 54, rgb("#a07820"))
    draw_neck(p, skin, shadow)
    draw_head(p, skin, shadow)
    # dark hair and a braid over the near shoulder
    hair_color = rgb("#2b1d16")
    hair = p.rect((18, 14, 24, 32)) | (p.ellipse((18, 8, 46, 22)) & ~p.rect((30, 15, 47, 26)))
    p.fill(hair, hair_color)
    braid = p.poly([(18, 30), (23, 30), (21, 52), (16, 52)])
    p.fill(braid, hair_color)
    for y in range(33, 52, 4):
        p.fill(p.line([(17, y), (22, y + 1)]), rgb("#4a3226"))
    # ranger hat: wide brim and a pinched crown
    p.fill(p.poly([(8, 15), (56, 13), (54, 17), (10, 19)]), rgb("#8b6b3d"))
    crown = p.poly([(19, 14), (22, 3), (32, 0), (42, 3), (45, 13)])
    p.fill(crown, rgb("#a07c48"))
    p.fill(p.line([(32, 0), (32, 6)]), rgb("#7c5c30"))
    p.fill(p.rect((19, 11, 45, 13)) & crown, rgb("#5b3a1e"))
    p.shade(crown, rgb("#c49a5c"), 1, -1, 1)
    draw_face(p, emotion, rgb("#1e1410"), rgb("#2b1d16"), rgb("#5a2c1e"), shadow, blush=rgb("#d08060"))
    p.outline()
    return p


def general(emotion):
    """General Maneford: lion-mane helmet, big mustache, medals."""
    skin, shadow = rgb("#e0a888"), rgb("#b47a5e")
    p = Pic()
    torso = draw_torso(p, rgb("#4a4f2a"), rgb("#363a1e"))
    # gold epaulettes, medals, collar
    p.fill(p.poly([(6, 56), (10, 49), (20, 47), (20, 52), (10, 55)]), rgb("#e0b030"))
    p.fill(p.poly([(44, 47), (55, 49), (59, 56), (54, 55), (44, 52)]), rgb("#e0b030"))
    for i, c in enumerate(["#c8332b", "#2f6fd0", "#e0b030", "#c8332b"]):
        p.fill(p.rect((40 + i * 3, 54, 41 + i * 3, 57)), rgb(c))
    p.fill(p.poly([(26, 46), (32, 51), (38, 46)]), rgb("#e0b030"))
    draw_neck(p, skin, shadow)
    # the mane: spiky gold/orange around the helmet (behind the head)
    mane_dark, mane, mane_light = rgb("#a8611a"), rgb("#d98e2b"), rgb("#f2b84b")
    spikes = []
    for i in range(13):
        a = np.pi * (0.95 + 1.1 * i / 12)
        cx, cy = 31 + 22 * np.cos(a), 22 + 21 * np.sin(a)
        bx, by = 31 + 11 * np.cos(a - 0.22), 22 + 11 * np.sin(a - 0.22)
        ex, ey = 31 + 11 * np.cos(a + 0.22), 22 + 11 * np.sin(a + 0.22)
        spikes.append([(bx, by), (cx, cy), (ex, ey)])
    for i, s in enumerate(spikes):
        p.fill(p.poly(s), mane_dark if i % 2 else mane)
    mane_back = p.poly([(18, 24), (10, 34), (14, 44), (22, 40)])
    p.fill(mane_back, mane_dark)
    draw_head(p, skin, shadow, highlight=rgb("#f0c4a8"))
    # metal helmet dome with a crest
    helmet = p.ellipse((18, 3, 46, 26)) & ~p.rect((0, 19, 64, 64))
    p.fill(helmet, rgb("#c9a227"))
    p.shade(helmet, rgb("#f4d35e"), 1, -1, 1)
    p.shade(helmet, rgb("#8a6d14"), -1, 1, 1)
    p.fill(p.rect((18, 18, 46, 19)), rgb("#8a6d14"))
    p.fill(p.rect((30, 1, 33, 5)), rgb("#c8332b"))
    draw_face(p, emotion, rgb("#2a2018"), rgb("#7a7a7a"), rgb("#5a2a20"), shadow, blush=rgb("#e88878"))
    # grand mustache (over the top of the mouth)
    stache = p.poly([(29, 31), (34, 29), (41, 30), (44, 33), (40, 32), (34, 31), (31, 33)])
    p.fill(stache, rgb("#d8d4cc"))
    p.shade(stache, rgb("#a8a49c"), 0, 1, 1)
    p.outline()
    return p


def dobbs(emotion):
    """Corporal Dobbs: helmet too big, big ears, freckles, orange vest."""
    skin, shadow = rgb("#f0c8a0"), rgb("#c89b74")
    p = Pic()
    draw_torso(p, rgb("#6b7a45"), rgb("#4f5a32"))
    vest = p.poly([(12, 63), (16, 50), (24, 47), (26, 63)]) | p.poly([(38, 63), (40, 47), (48, 50), (52, 63)])
    p.fill(vest, rgb("#f08c2a"))
    p.fill(p.rect((12, 56, 52, 57)) & vest, rgb("#f6e27a"))           # reflective stripe
    draw_neck(p, skin, shadow)
    draw_head(p, skin, shadow)
    # big ears
    ear = p.ellipse((13, 20, 22, 32))
    p.fill(ear, skin)
    p.fill(p.ellipse((15, 23, 19, 29)), rgb("#e0a080"))
    draw_face(p, emotion, rgb("#2a2a40"), rgb("#8a6040"), rgb("#7a3a2a"), shadow, blush=rgb("#f09080"))
    # freckles
    p.dots([(25, 27), (27, 28), (39, 27), (41, 28), (26, 29)], rgb("#c47a4a"))
    # oversized helmet sliding down to the eyebrows
    helmet = p.ellipse((14, 0, 50, 26)) & ~p.rect((0, 17, 64, 64))
    helmet |= p.poly([(12, 16), (52, 15), (50, 18), (14, 19)])
    p.fill(helmet, rgb("#5a6b3c"))
    p.shade(helmet, rgb("#7a8b55"), 1, -1, 1)
    p.fill(p.poly([(12, 16), (52, 15), (50, 18), (14, 19)]), rgb("#465430"))
    p.fill(p.line([(22, 4), (40, 3)]), rgb("#465430"))
    # a strap hanging loose
    p.fill(p.line([(20, 19), (21, 34)]), rgb("#3a3020"))
    p.outline()
    return p


def pa(emotion):
    """The base intercom: a loudspeaker horn on a pole."""
    p = Pic()
    metal, dark, light = rgb("#9aa0a6"), rgb("#5f656c"), rgb("#c8ced4")
    p.fill(p.rect((14, 30, 18, 63)), dark)                                   # pole
    p.fill(p.rect((10, 26, 22, 32)), metal)                                  # bracket
    horn = p.poly([(18, 22), (26, 22), (48, 8), (52, 10), (52, 46), (48, 48), (26, 34), (18, 34)])
    p.fill(horn, metal)
    p.shade(horn, light, 0, -1, 1)
    p.shade(horn, dark, 0, 1, 2)
    p.fill(p.ellipse((46, 8, 54, 48)), dark)
    p.fill(p.ellipse((48, 12, 53, 44)), rgb("#2a2e33"))
    p.fill(p.rect((12, 20, 20, 36)), metal)                                  # driver box
    p.shade(p.rect((12, 20, 20, 36)), dark, -1, 0, 2)
    # sound waves
    waves = rgb("#e8eef4")
    if emotion in ("angry", "panic", "surprised"):
        waves = rgb("#ffcf6b")
    for r in (6, 10):
        arc = p._mask(lambda d, r=r: d.arc((52 - r, 28 - r * 2, 52 + r, 28 + r * 2), -60, 60, fill=255))
        p.fill(arc, waves)
    p.outline()
    return p


def sultan(emotion):
    """Sultan the alpha lion. 'angry' is a full roar."""
    p = Pic()
    mane, mane_dark, mane_light = rgb("#b5651d"), rgb("#7a3f12"), rgb("#d9873a")
    fur, fur_dark, muzzle = rgb("#e0a84c"), rgb("#b8822e"), rgb("#f2d59a")
    mane_mask = p.ellipse((4, 2, 58, 62))
    p.fill(mane_mask, mane)
    for i in range(16):
        a = 2 * np.pi * i / 16
        pts = [(31 + 22 * np.cos(a - 0.15), 32 + 24 * np.sin(a - 0.15)),
               (31 + 31 * np.cos(a), 32 + 33 * np.sin(a)),
               (31 + 22 * np.cos(a + 0.15), 32 + 24 * np.sin(a + 0.15))]
        p.fill(p.poly(pts), mane_dark if i % 2 else mane)
    p.shade(mane_mask, mane_light, 1, -1, 2)
    face = p.ellipse((16, 12, 48, 50))
    p.fill(face, fur)
    p.shade(face, fur_dark, -1, 1, 2)
    p.fill(p.ellipse((18, 10, 26, 18)) | p.ellipse((38, 10, 46, 18)), fur)   # ears
    p.fill(p.ellipse((20, 12, 24, 16)) | p.ellipse((40, 12, 44, 16)), mane_dark)
    snout = p.ellipse((24, 30, 42, 48))
    p.fill(snout, muzzle)
    p.fill(p.poly([(29, 30), (37, 30), (33, 35)]), rgb("#5a3a2a"))            # nose
    gold, black = rgb("#e8c040"), rgb("#1a1208")
    if emotion == "angry":
        # roaring: narrowed eyes, open jaw with fangs
        for x in (25, 37):
            p.fill(p.line([(x - 1, 24), (x + 3, 26)]), black)
        p.fill(p.ellipse((26, 37, 40, 52)), rgb("#5a1a1a"))
        p.fill(p.ellipse((29, 44, 37, 51)), rgb("#c84a5a"))
        p.dots([(28, 38), (28, 39), (38, 38), (38, 39), (29, 50), (37, 50)], rgb("#f8f4e8"))
    else:
        for x in (25, 37):
            p.fill(p.rect((x, 23, x + 2, 25)), gold)
            p.fill(p.rect((x + 1, 23, x + 1, 25)), black)
        p.fill(p.line([(33, 35), (33, 39), (29, 41)]) | p.line([(33, 39), (37, 41)]), rgb("#7a4a30"))
    p.outline()
    return p


CAST = {
    "rook": rook,
    "pip": pip,
    "tala": tala,
    "general": general,
    "dobbs": dobbs,
    "pa": pa,
    "sultan": sultan,
}


# =========================
# UI TEXTURES
# =========================

def ui_box():
    """24x24 dialogue frame for 9-slicing (8 px borders)."""
    s = 24
    px = np.zeros((s, s, 4), np.uint8)
    px[:, :] = (18, 22, 38, 236)
    border = (200, 210, 225, 255)
    edge = (8, 10, 18, 255)
    px[0, :] = px[-1, :] = px[:, 0] = px[:, -1] = edge
    px[1:3, 1:-1] = border
    px[-3:-1, 1:-1] = border
    px[1:-1, 1:3] = border
    px[1:-1, -3:-1] = border
    px[3, 3:-3] = (60, 70, 100, 255)          # inner bevel
    for x, y in [(0, 0), (1, 0), (0, 1), (s - 1, 0), (s - 2, 0), (s - 1, 1),
                 (0, s - 1), (1, s - 1), (0, s - 2), (s - 1, s - 1), (s - 2, s - 1), (s - 1, s - 2)]:
        px[y, x] = (0, 0, 0, 0)
    for x, y in [(1, 1), (s - 2, 1), (1, s - 2), (s - 2, s - 2)]:
        px[y, x] = edge
    return Image.fromarray(px, "RGBA")


def ui_noise():
    rng = np.random.default_rng(3)
    v = rng.integers(0, 256, (128, 128))
    a = (rng.random((128, 128)) < 0.55) * 255
    px = np.stack([v, v, v, a], -1).astype(np.uint8)
    return Image.fromarray(px, "RGBA")


def ui_scanlines():
    px = np.zeros((4, 4, 4), np.uint8)
    px[0:2, :] = (255, 255, 255, 255)
    return Image.fromarray(px, "RGBA")


def ui_arrow():
    px = np.zeros((7, 11, 4), np.uint8)
    gold, dark = (255, 214, 74, 255), (60, 40, 10, 255)
    for row in range(6):
        px[row, row:11 - row] = gold
    px[6, 5] = dark
    for row in range(6):
        px[row, row] = dark
        px[row, 10 - row] = dark
    return Image.fromarray(px, "RGBA")


def ui_gradient():
    """Dark at the bottom, clear at the top (behind the text box)."""
    h = 64
    alpha = (np.linspace(0, 1, h) ** 1.6 * 215).astype(np.uint8)
    px = np.zeros((h, 4, 4), np.uint8)
    px[:, :, 3] = alpha[:, None]
    px[:, :, :3] = (6, 8, 14)
    return Image.fromarray(px, "RGBA")


def main():
    os.makedirs(PORTRAITS, exist_ok=True)
    os.makedirs(UI, exist_ok=True)

    for name, draw in CAST.items():
        for emotion in EMOTIONS:
            draw(emotion).save(os.path.join(PORTRAITS, "%s_%s.png" % (name, emotion)))
        print("portraits:", name)

    ui_box().save(os.path.join(UI, "DialogueBox.png"))
    ui_noise().save(os.path.join(UI, "Static.png"))
    ui_scanlines().save(os.path.join(UI, "Scanlines.png"))
    ui_arrow().save(os.path.join(UI, "ContinueArrow.png"))
    ui_gradient().save(os.path.join(UI, "BottomShade.png"))
    print("ui textures done")


if __name__ == "__main__":
    main()
