"""Generates placeholder pixel-art props for the dialogue scene backdrops:
the parachute, the broken collar, the base fence, gate and banner, the
kennel, the helicopter cockpit frame and a light glow.

They are drawn at the world's pixel density (16 pixels per unit) so
they sit naturally next to the forest tiles. The banner reuses the
General's portrait, so run generate_portraits.py first.

Run from the repo root:
    python Tools/generate_dialogue_props.py

Writes Assets/Dialogue/Props/*.png
"""

import math
import os

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "Dialogue", "Props")
PORTRAITS = os.path.join(HERE, "..", "Assets", "Dialogue", "Portraits")

OUTLINE = (20, 16, 26, 255)


def rgb(hex_color, alpha=255):
    h = hex_color.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha)


def canvas(w, h):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def outline(img, color=OUTLINE):
    """1-pixel dark outline around everything opaque."""
    px = np.array(img)
    solid = px[:, :, 3] > 0
    grown = solid.copy()
    grown[1:, :] |= solid[:-1, :]
    grown[:-1, :] |= solid[1:, :]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    px[grown & ~solid] = color
    return Image.fromarray(px, "RGBA")


def parachute():
    """A crumpled olive parachute, tilted, strings hanging down."""
    w, h = 64, 48
    img, d = canvas(w, h)
    olive, khaki, shade = rgb("#6b7a45"), rgb("#b8ad78"), rgb("#4f5a32")

    # dome made of alternating gores
    cx, cy, rx, ry = 32, 22, 28, 18
    for i in range(7):
        a0 = math.pi + math.pi * i / 7
        a1 = math.pi + math.pi * (i + 1) / 7
        pts = [(cx, cy + 4)]
        for k in range(9):
            a = a0 + (a1 - a0) * k / 8
            pts.append((cx + rx * math.cos(a), cy + ry * math.sin(a)))
        d.polygon(pts, fill=olive if i % 2 == 0 else khaki)
    # crumpled hem with a few droops
    hem = [(4, 22), (10, 27), (16, 23), (23, 29), (30, 24), (37, 30), (44, 24), (51, 28), (57, 23), (60, 22)]
    d.polygon([(4, 20)] + hem + [(60, 20)], fill=olive)
    d.line(hem, fill=shade, width=1)
    # shading on the underside of the dome
    for x in range(6, 58, 2):
        d.point((x, 21 + (x % 4 == 0)), fill=shade)
    # strings converging to the harness
    for sx, sy in [(10, 27), (23, 29), (37, 30), (51, 28)]:
        d.line([(sx, sy), (31, 44)], fill=rgb("#d8d2b8"), width=1)
    d.rectangle((28, 42, 34, 46), fill=rgb("#3a3020"))
    img = img.rotate(-14, resample=Image.NEAREST, expand=False)
    return outline(img)


def collar():
    """The broken mind-control collar: two halves of a steel band,
    a red light and a cracked chip."""
    w, h = 32, 16
    img, d = canvas(w, h)
    steel, dark, light = rgb("#5a616b"), rgb("#2e333a"), rgb("#aeb6c0")
    # left half
    d.arc((1, 3, 17, 14), 90, 300, fill=steel, width=3)
    # right half, a little apart and tilted
    d.arc((14, 2, 30, 13), 250, 70, fill=steel, width=3)
    d.arc((1, 3, 17, 14), 120, 200, fill=light, width=1)
    d.arc((14, 2, 30, 13), 280, 340, fill=light, width=1)
    # the control box with its red light and a crack
    d.rectangle((11, 9, 19, 14), fill=dark)
    d.rectangle((12, 10, 13, 11), fill=rgb("#ff3b30"))
    d.line([(15, 9), (17, 12), (16, 14)], fill=rgb("#8fd0ff"), width=1)
    return outline(img)


def sparkle():
    """A 4-point glint."""
    w = h = 9
    img, d = canvas(w, h)
    white = (255, 255, 255, 255)
    d.line([(4, 0), (4, 8)], fill=(255, 255, 255, 200))
    d.line([(0, 4), (8, 4)], fill=(255, 255, 255, 200))
    d.point((4, 4), fill=white)
    d.point((3, 4), fill=white)
    d.point((5, 4), fill=white)
    d.point((4, 3), fill=white)
    d.point((4, 5), fill=white)
    return img


def fence():
    """One repeat of the base's chain-link fence with barbed wire."""
    w, h = 32, 48
    img, d = canvas(w, h)
    post, rail, mesh = rgb("#6b7280"), rgb("#8a929c"), rgb("#9aa0a6", 150)
    # mesh diamonds
    for x in range(-48, 48, 4):
        d.line([(x, 8), (x + 40, 48)], fill=mesh)
        d.line([(x + 40, 8), (x, 48)], fill=mesh)
    d.rectangle((0, 6, 31, 7), fill=rail)                 # top rail
    d.rectangle((0, 6, 2, 47), fill=post)                 # post
    d.rectangle((0, 0, 1, 6), fill=post)                  # barbed-wire arm
    # barbed wire coils
    for x in range(0, 32, 6):
        d.ellipse((x, 0, x + 6, 5), outline=rgb("#b0b6be"))
    return img


def gate():
    """Heavy double gate with a hazard sign."""
    w, h = 72, 56
    img, d = canvas(w, h)
    metal, dark, light = rgb("#4a4f57"), rgb("#2c3036"), rgb("#7a828c")
    d.rectangle((0, 0, 71, 55), fill=dark)
    for x in range(3, 70, 6):
        d.rectangle((x, 2, x + 2, 53), fill=metal)
        d.line([(x, 2), (x, 53)], fill=light)
    d.rectangle((0, 0, 71, 3), fill=metal)
    d.rectangle((0, 26, 71, 29), fill=metal)
    d.rectangle((0, 52, 71, 55), fill=metal)
    d.line([(35, 0), (35, 55)], fill=rgb("#14161a"), width=2)  # the doors meet
    # hazard sign
    d.rectangle((26, 8, 45, 20), fill=rgb("#f2c230"))
    for x in range(26, 46, 4):
        d.line([(x, 20), (x + 4, 16)], fill=rgb("#1a1a1a"))
    d.rectangle((28, 9, 43, 15), fill=rgb("#f2c230"))
    d.line([(35, 10), (35, 13)], fill=rgb("#1a1a1a"))
    d.point((35, 15), fill=rgb("#1a1a1a"))
    return outline(img)


def banner():
    """A giant red banner with the General's face in his mane helmet."""
    w, h = 80, 112
    img, d = canvas(w, h)
    red, red_dark, gold = rgb("#9b1c1c"), rgb("#6e1212"), rgb("#e0b030")
    d.rectangle((0, 0, 79, 3), fill=rgb("#5a4a30"))         # pole
    d.rectangle((4, 4, 75, 100), fill=red)
    # cloth folds
    for x in (18, 40, 62):
        d.line([(x, 4), (x, 100)], fill=red_dark, width=2)
    d.rectangle((4, 4, 75, 100), outline=gold, width=2)
    # fringe
    for x in range(5, 75, 4):
        d.line([(x, 101), (x, 107)], fill=gold)
    # The General's portrait, whatever size it was drawn at, fitted to 64x64
    face = Image.open(os.path.join(PORTRAITS, "general_smug.png")).convert("RGBA")
    if face.size != (64, 64):
        face = face.resize((64, 64), Image.NEAREST)
    img.alpha_composite(face, (8, 18))
    # a gold crown above the portrait
    d = ImageDraw.Draw(img)
    d.polygon([(30, 16), (32, 8), (36, 13), (40, 6), (44, 13), (48, 8), (50, 16)], fill=gold)
    return outline(img)


def kennel():
    """A locked steel kennel on a concrete base."""
    w, h = 88, 56
    img, d = canvas(w, h)
    bars, frame, light = rgb("#3f444c"), rgb("#5a616b"), rgb("#8a929c")
    d.rectangle((0, 48, 87, 55), fill=rgb("#7a7f86"))       # concrete
    d.line([(0, 48), (87, 48)], fill=rgb("#9aa0a6"))
    d.rectangle((2, 4, 85, 47), fill=rgb("#141820"))        # dark inside
    for x in range(10, 80, 6):
        d.ellipse((x, 41, x + 9, 47), fill=rgb("#c8a050"))  # straw
    for x in range(2, 86, 6):
        d.rectangle((x, 4, x + 1, 47), fill=bars)
        d.point((x, 6), fill=light)
    d.rectangle((0, 0, 87, 5), fill=frame)
    d.rectangle((0, 0, 3, 47), fill=frame)
    d.rectangle((84, 0, 87, 47), fill=frame)
    d.rectangle((40, 4, 43, 47), fill=frame)                # door edge
    # padlock
    d.arc((44, 18, 52, 26), 180, 360, fill=rgb("#b88a20"), width=2)
    d.rectangle((44, 22, 52, 30), fill=rgb("#e0b030"))
    d.point((48, 26), fill=rgb("#5a4a10"))
    return outline(img)


def cockpit():
    """Helicopter cockpit frame seen from the seats: a screen-sized
    overlay with a see-through windshield."""
    w, h = 288, 162
    img, d = canvas(w, h)
    body, edge, panel = rgb("#15171d"), rgb("#2b2f38"), rgb("#1d2027")
    # roof and pillars around the windshield
    d.polygon([(0, 0), (288, 0), (288, 22), (230, 14), (144, 10), (58, 14), (0, 22)], fill=body)
    d.polygon([(0, 0), (34, 0), (58, 120), (0, 162)], fill=body)
    d.polygon([(288, 0), (254, 0), (230, 120), (288, 162)], fill=body)
    d.polygon([(141, 10), (147, 10), (146, 112), (142, 112)], fill=body)      # middle strut
    d.line([(34, 0), (58, 120)], fill=edge, width=2)
    d.line([(254, 0), (230, 120)], fill=edge, width=2)
    d.line([(58, 14), (144, 10), (230, 14)], fill=edge, width=2)
    # dashboard
    d.polygon([(0, 162), (0, 126), (60, 112), (228, 112), (288, 126), (288, 162)], fill=panel)
    d.line([(0, 126), (60, 112), (228, 112), (288, 126)], fill=edge, width=2)
    for i, gx in enumerate((84, 112, 176, 204)):
        d.ellipse((gx - 9, 120, gx + 9, 138), fill=rgb("#0b0c10"), outline=rgb("#59606c"))
        ang = math.radians(-60 + 37 * i)
        d.line([(gx, 129), (gx + 7 * math.cos(ang), 129 + 7 * math.sin(ang))], fill=rgb("#f2c230"))
    for i, c in enumerate(("#ff3b30", "#33d17a", "#f2c230", "#33d17a", "#22d3ee")):
        d.rectangle((126 + i * 8, 124, 130 + i * 8, 127), fill=rgb(c))
    d.rectangle((128, 134, 160, 146), fill=rgb("#0b0c10"), outline=rgb("#59606c"))
    d.line([(130, 140), (138, 137), (146, 142), (158, 136)], fill=rgb("#33d17a"))
    # cyclic stick
    d.rectangle((140, 146, 146, 162), fill=rgb("#2b2f38"))
    d.ellipse((136, 140, 150, 150), fill=rgb("#3a3f4a"))
    # faint reflections on the glass
    d.line([(70, 30), (96, 92)], fill=(255, 255, 255, 22), width=3)
    d.line([(196, 26), (214, 70)], fill=(255, 255, 255, 18), width=2)
    return img


def lion_silhouette():
    """A proud standing lion facing right, as a solid shape (tint it in
    Unity: black for silhouettes against a sunrise or as a giant shadow)."""
    w, h = 76, 56
    img, d = canvas(w, h)
    c = (255, 255, 255, 255)
    d.ellipse((12, 22, 52, 40), fill=c)                       # body
    d.ellipse((36, 18, 56, 38), fill=c)                       # chest
    for x0, x1, top in ((15, 19, 32), (23, 27, 34), (41, 45, 32), (48, 52, 32)):
        d.rectangle((x0, top, x1, 52), fill=c)                # legs
        d.rectangle((x0 - 1, 52, x1 + 1, 55), fill=c)         # paws
    # mane: a big spiky ring around the head and down the neck
    cx, cy = 55, 20
    spikes = []
    for i in range(20):
        a = 2 * math.pi * i / 20
        r = 16 if i % 2 == 0 else 10
        spikes.append((cx + r * math.cos(a), cy + r * 1.05 * math.sin(a)))
    d.polygon(spikes, fill=c)
    d.ellipse((53, 13, 67, 27), fill=c)                       # head
    d.polygon([(64, 17), (73, 20), (73, 24), (63, 27)], fill=c)  # muzzle
    # tail with a tuft
    d.line([(13, 26), (7, 30), (4, 37)], fill=c, width=2)
    d.ellipse((0, 35, 7, 42), fill=c)
    return img


def glow():
    """Soft round light for distant lights (tint it in Unity)."""
    s = 16
    y, x = np.mgrid[0:s, 0:s]
    r = np.sqrt((x - 7.5) ** 2 + (y - 7.5) ** 2) / 7.5
    a = np.clip(1.0 - r, 0, 1) ** 1.8
    a[r < 0.25] = 1.0
    px = np.zeros((s, s, 4), np.uint8)
    px[:, :, :3] = 255
    px[:, :, 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(px, "RGBA")


def main():
    os.makedirs(OUT, exist_ok=True)
    props = {
        "Parachute": parachute,
        "Collar": collar,
        "Sparkle": sparkle,
        "Fence": fence,
        "Gate": gate,
        "Banner": banner,
        "Kennel": kennel,
        "Cockpit": cockpit,
        "LionSilhouette": lion_silhouette,
        "Glow": glow,
    }
    for name, draw in props.items():
        draw().save(os.path.join(OUT, name + ".png"))
        print("prop:", name)


if __name__ == "__main__":
    main()
