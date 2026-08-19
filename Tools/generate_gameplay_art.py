"""Generates placeholder pixel art for gameplay: the lions' collars, the
Mecha-Lion boss and its attacks, and the helicopter chase.

Run from the repo root (after generate_portraits.py, which the
cockpit uses for the General's face):
    python Tools/generate_gameplay_art.py

Writes:
    Assets/Sprites/Lion/LionCollar.png
    Assets/Sprites/FX/CollarHalf.png, Shard.png
    Assets/BossFight/Art/*.png
    Assets/HelicopterStage/Art/*.png
"""

import math
import os

import numpy as np
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
ASSETS = os.path.join(HERE, "..", "Assets")
PORTRAITS = os.path.join(ASSETS, "Dialogue", "Portraits")

OUTLINE = (20, 16, 26, 255)


def rgb(hex_color, alpha=255):
    h = hex_color.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), alpha)


def canvas(w, h):
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    return img, ImageDraw.Draw(img)


def outline(img, color=OUTLINE):
    px = np.array(img)
    solid = px[:, :, 3] > 0
    grown = solid.copy()
    grown[1:, :] |= solid[:-1, :]
    grown[:-1, :] |= solid[1:, :]
    grown[:, 1:] |= solid[:, :-1]
    grown[:, :-1] |= solid[:, 1:]
    px[grown & ~solid] = color
    return Image.fromarray(px, "RGBA")


def save(img, *path):
    full = os.path.join(ASSETS, *path)
    os.makedirs(os.path.dirname(full), exist_ok=True)
    img.save(full)
    print("art:", os.path.relpath(full, ASSETS))


# =========================
# LION COLLARS
# =========================

def lion_collar():
    """The collar a controlled lion wears (seen from the side)."""
    img, d = canvas(9, 6)
    d.rectangle((0, 1, 8, 4), fill=rgb("#3a3f47"))
    d.line([(0, 1), (8, 1)], fill=rgb("#8a929c"))
    d.rectangle((3, 0, 6, 5), fill=rgb("#2a2e35"))
    d.point((4, 2), fill=rgb("#ff3b30"))
    d.point((5, 2), fill=rgb("#ff8a80"))
    return img


def collar_half():
    """One half of a shattered collar."""
    img, d = canvas(8, 8)
    d.arc((0, 0, 11, 7), 90, 270, fill=rgb("#5a616b"), width=2)
    d.arc((0, 0, 11, 7), 120, 200, fill=rgb("#aeb6c0"), width=1)
    d.point((2, 6), fill=rgb("#ff3b30"))
    return outline(img)


def shard():
    img, d = canvas(2, 2)
    d.rectangle((0, 0, 1, 1), fill=(255, 255, 255, 255))
    return img


# =========================
# MECHA-LION (faces left)
# =========================

STEEL = rgb("#6b7684")
STEEL_DARK = rgb("#3f4652")
STEEL_LIGHT = rgb("#a9b4c2")
GOLD = rgb("#e0b030")
GOLD_DARK = rgb("#9c7414")


def mecha_body():
    """Torso, piston legs and a cable tail."""
    w, h = 104, 56
    img, d = canvas(w, h)
    # tail cable with a spiked tip (on the right, it faces left)
    d.line([(88, 18), (98, 12), (101, 4)], fill=STEEL_DARK, width=3)
    d.polygon([(97, 4), (103, 0), (103, 7)], fill=GOLD)
    # legs: thigh plates and pistons
    for x in (20, 36, 66, 80):
        d.rectangle((x, 30, x + 9, 46), fill=STEEL_DARK)
        d.rectangle((x + 2, 44, x + 7, 52), fill=STEEL_LIGHT)
        d.rectangle((x - 2, 51, x + 11, 55), fill=STEEL_DARK)      # foot
        d.line([(x + 1, 32), (x + 1, 44)], fill=STEEL)
    # torso
    d.rounded_rectangle((10, 10, 94, 38), radius=10, fill=STEEL)
    # armour plates with rivets
    for x0 in (16, 38, 60):
        d.rectangle((x0, 13, x0 + 18, 26), fill=STEEL_LIGHT)
        d.rectangle((x0 + 1, 14, x0 + 17, 25), fill=STEEL)
        for rx in (x0 + 2, x0 + 16):
            d.point((rx, 15), fill=STEEL_LIGHT)
            d.point((rx, 24), fill=STEEL_LIGHT)
    # belly vents glowing orange
    for x in range(22, 86, 8):
        d.rectangle((x, 31, x + 4, 33), fill=rgb("#ff9a3c"))
    d.line([(12, 37), (92, 37)], fill=STEEL_DARK, width=2)
    # royal stripe
    d.rectangle((10, 27, 94, 29), fill=GOLD)
    return outline(img)


def mecha_head(roaring=False):
    """Steel lion head with a spiky golden mane (the missile pods)."""
    w, h = 60, 60
    img, d = canvas(w, h)
    cx, cy = 32, 30
    spikes = []
    for i in range(22):
        a = 2 * math.pi * i / 22
        r = 28 if i % 2 == 0 else 19
        spikes.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(spikes, fill=GOLD)
    inner = []
    for i in range(22):
        a = 2 * math.pi * i / 22
        r = 22 if i % 2 == 0 else 16
        inner.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    d.polygon(inner, fill=GOLD_DARK)
    # missile pods in the mane
    for a in (-2.2, -1.6, -1.0):
        px, py = cx + 22 * math.cos(a), cy + 22 * math.sin(a)
        d.ellipse((px - 3, py - 3, px + 3, py + 3), fill=STEEL_DARK)
        d.point((px, py), fill=rgb("#ff3b30"))
    # face plate, looking left
    d.rounded_rectangle((10, 16, 46, 46), radius=8, fill=STEEL)
    d.rectangle((12, 18, 44, 22), fill=STEEL_LIGHT)
    # muzzle and jaw
    d.rounded_rectangle((4, 30, 26, 44), radius=4, fill=STEEL_LIGHT)
    if roaring:
        d.rectangle((4, 40, 24, 52), fill=rgb("#2a0e10"))
        for x in range(6, 24, 4):
            d.polygon([(x, 40), (x + 2, 44), (x + 4, 40)], fill=(240, 240, 230, 255))
        d.rectangle((4, 50, 24, 53), fill=STEEL)
    else:
        d.rectangle((4, 42, 24, 46), fill=STEEL_DARK)
    d.rectangle((4, 32, 8, 35), fill=rgb("#1a1a1a"))            # nose
    # glowing eyes
    d.rectangle((16, 25, 22, 28), fill=rgb("#ff2a1a"))
    d.point((17, 26), fill=rgb("#ffd0c0"))
    d.rectangle((30, 25, 35, 28), fill=rgb("#ff2a1a"))
    return outline(img)


def mecha_cockpit():
    """The General's glass dome on the Mecha-Lion's back."""
    w, h = 30, 26
    img, d = canvas(w, h)
    d.rectangle((2, 20, 27, 25), fill=STEEL_DARK)
    d.rectangle((2, 20, 27, 21), fill=GOLD)
    face = Image.open(os.path.join(PORTRAITS, "general_smug.png")).convert("RGBA")
    face = face.resize((22, 22), Image.NEAREST)
    face = face.transpose(Image.FLIP_LEFT_RIGHT)   # look left, like the lion
    img.alpha_composite(face, (4, 0))
    d = ImageDraw.Draw(img)
    d.arc((1, 0, 28, 38), 180, 360, fill=rgb("#bfe8ff"), width=2)
    d.line([(8, 6), (6, 12)], fill=(255, 255, 255, 200))
    return outline(img)


def armor_plate():
    img, d = canvas(14, 10)
    d.polygon([(0, 2), (12, 0), (13, 8), (2, 9)], fill=STEEL_LIGHT)
    d.polygon([(2, 3), (11, 2), (11, 7), (3, 7)], fill=STEEL)
    d.point((3, 4), fill=STEEL_LIGHT)
    d.point((10, 3), fill=STEEL_LIGHT)
    return outline(img)


def missile():
    """Points right; rotate it to fly in any direction."""
    img, d = canvas(14, 6)
    d.rectangle((3, 1, 10, 4), fill=rgb("#d8dde3"))
    d.polygon([(10, 1), (13, 2), (13, 3), (10, 4)], fill=rgb("#d0301f"))
    d.polygon([(3, 1), (1, 0), (1, 1)], fill=STEEL_DARK)
    d.polygon([(3, 4), (1, 5), (1, 4)], fill=STEEL_DARK)
    d.rectangle((0, 2, 2, 3), fill=rgb("#ffb03a"))
    img = outline(img)
    d = ImageDraw.Draw(img)
    d.point((0, 2), fill=rgb("#fff2a0"))
    return img


def shockwave():
    """A crescent of energy rolling along the ground (moves right)."""
    w, h = 26, 14
    img, d = canvas(w, h)
    d.ellipse((2, 0, 30, 28), fill=rgb("#7fe0ff", 220))
    d.ellipse((0, 4, 24, 30), fill=(0, 0, 0, 0))
    d.ellipse((6, 2, 30, 28), outline=(255, 255, 255, 255))
    px = np.array(img)
    px[h - 1:, :, :] = 0
    return Image.fromarray(px, "RGBA")


# =========================
# HELICOPTER CHASE
# =========================

def helicopter(body, trim, accent, faces_right, rotor_frame, royal=False):
    w, h = 64, 32
    img, d = canvas(w, h)
    # tail boom and tail rotor
    d.polygon([(4, 12), (30, 13), (30, 17), (4, 15)], fill=body)
    d.rectangle((0, 7, 4, 17), fill=body)
    if rotor_frame == 0:
        d.line([(2, 6), (2, 18)], fill=(220, 220, 220, 200), width=1)
    else:
        d.line([(-2, 12), (6, 12)], fill=(220, 220, 220, 200), width=1)
    # cabin
    d.rounded_rectangle((24, 8, 58, 26), radius=8, fill=body)
    d.polygon([(44, 9), (58, 14), (58, 20), (44, 20)], fill=rgb("#9fd8f0"))    # glass
    d.line([(46, 11), (50, 18)], fill=(255, 255, 255, 180))
    d.rectangle((26, 20, 56, 22), fill=trim)
    # skids
    d.line([(28, 30), (56, 30)], fill=rgb("#2a2e35"), width=2)
    d.line([(32, 26), (32, 30)], fill=rgb("#2a2e35"))
    d.line([(50, 26), (50, 30)], fill=rgb("#2a2e35"))
    # rotor mast and blades (two frames for a spinning look)
    d.rectangle((39, 4, 42, 8), fill=rgb("#2a2e35"))
    if rotor_frame == 0:
        d.line([(14, 3), (64, 3)], fill=rgb("#c8ced4"), width=2)
    else:
        d.line([(26, 3), (54, 3)], fill=rgb("#c8ced4"), width=2)
    if royal:
        # the General's touches: a gold crown and a mane decal
        d.polygon([(30, 14), (32, 10), (34, 13), (36, 9), (38, 13), (40, 10), (42, 14)], fill=accent)
        for i in range(6):
            a = math.pi * (0.2 + i / 6)
            d.line([(36, 18), (36 + 6 * math.cos(a), 18 + 5 * math.sin(a))], fill=rgb("#d98e2b"))
    else:
        d.rectangle((30, 12, 38, 15), fill=accent)       # number plate
    img = outline(img)
    if not faces_right:
        img = img.transpose(Image.FLIP_LEFT_RIGHT)
    return img


def rocket():
    """Points left (fired at the player)."""
    img, d = canvas(16, 6)
    d.rectangle((3, 1, 12, 4), fill=rgb("#4a4f57"))
    d.polygon([(3, 1), (0, 2), (0, 3), (3, 4)], fill=rgb("#e0b030"))
    d.polygon([(12, 1), (15, 0), (15, 1)], fill=rgb("#2c3036"))
    d.polygon([(12, 4), (15, 5), (15, 4)], fill=rgb("#2c3036"))
    img = outline(img)
    d = ImageDraw.Draw(img)
    d.rectangle((14, 2, 15, 3), fill=rgb("#ffb03a"))
    return img


def drone(frame):
    """A small quadcopter with a lion-chip collar ring and a red eye."""
    img, d = canvas(20, 14)
    d.rectangle((4, 6, 15, 11), fill=rgb("#3a3f47"))
    d.ellipse((6, 5, 13, 12), fill=rgb("#5a616b"))
    d.ellipse((7, 6, 12, 11), outline=GOLD)                     # the collar
    d.point((9, 8), fill=rgb("#ff3b30"))
    d.point((10, 8), fill=rgb("#ff3b30"))
    d.line([(1, 4), (6, 6)], fill=rgb("#2a2e35"))
    d.line([(18, 4), (13, 6)], fill=rgb("#2a2e35"))
    blade = rgb("#c8ced4")
    if frame == 0:
        d.line([(0, 3), (4, 3)], fill=blade)
        d.line([(15, 3), (19, 3)], fill=blade)
    else:
        d.line([(1, 2), (3, 4)], fill=blade)
        d.line([(16, 2), (18, 4)], fill=blade)
    return outline(img)


def heli_bullet():
    img, d = canvas(6, 2)
    d.rectangle((0, 0, 5, 1), fill=rgb("#fff2a0"))
    d.point((5, 0), fill=(255, 255, 255, 255))
    return img


def main():
    save(lion_collar(), "Sprites", "Lion", "LionCollar.png")
    save(collar_half(), "Sprites", "FX", "CollarHalf.png")
    save(shard(), "Sprites", "FX", "Shard.png")

    save(mecha_body(), "BossFight", "Art", "MechaBody.png")
    save(mecha_head(False), "BossFight", "Art", "MechaHead.png")
    save(mecha_head(True), "BossFight", "Art", "MechaHeadRoar.png")
    save(mecha_cockpit(), "BossFight", "Art", "MechaCockpit.png")
    save(armor_plate(), "BossFight", "Art", "ArmorPlate.png")
    save(missile(), "BossFight", "Art", "Missile.png")
    save(shockwave(), "BossFight", "Art", "Shockwave.png")

    for f in (0, 1):
        save(helicopter(rgb("#4f6b3a"), rgb("#2f4020"), rgb("#e8e8e8"), True, f),
             "HelicopterStage", "Art", "PlayerHeli_%d.png" % f)
        save(helicopter(rgb("#9b1c1c"), rgb("#e0b030"), rgb("#e0b030"), False, f, royal=True),
             "HelicopterStage", "Art", "GeneralHeli_%d.png" % f)
        save(drone(f), "HelicopterStage", "Art", "Drone_%d.png" % f)
    save(rocket(), "HelicopterStage", "Art", "Rocket.png")
    save(heli_bullet(), "HelicopterStage", "Art", "HeliBullet.png")


if __name__ == "__main__":
    main()
