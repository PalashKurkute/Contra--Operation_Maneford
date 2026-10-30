"""Generates pixel art for gameplay: the lion and its collar, the rifle shot, the
Mecha-Lion boss and its attacks, and the helicopter chase.

Run from the repo root (after generate_portraits.py, which the
cockpit uses for the General's face):
    python Tools/generate_gameplay_art.py

Writes:
    Assets/Sprites/Lion/Lion.png, LionCollar.png
    Assets/Sprites/FX/Bullet.png, CollarHalf.png, Shard.png
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
# LION RUN CYCLE (faces left)
# =========================

LION_FUR = rgb("#c98a40")
LION_FUR_LIGHT = rgb("#e6b264")
LION_FUR_DARK = rgb("#8f5726")
LION_FAR = rgb("#7a4a22")
LION_MUZZLE = rgb("#efd29a")
MANE = rgb("#7c3a17")
MANE_LIGHT = rgb("#a8552a")
MANE_DARK = rgb("#53240c")
LION_INK = rgb("#2a1408")

# One gallop: stretched, gathering, collected, pushing off. Each leg is
# shoulder/hip -> knee -> paw, front leg first; the near legs go over
# the body, the far ones behind it in shadow. Paws touch row 36 (the
# outline adds row 37), and the back of the mane stays near (29, 20),
# where the collar overlay sits.
LION_POSES = [
    dict(body=(20, 21, 46, 30), mane=(22, 21), head=(13, 23),
         tail=[(45, 23), (49, 21), (51, 17)],
         far=[[(26, 26), (20, 30), (14, 33)], [(39, 26), (44, 31), (49, 33)]],
         near=[[(24, 27), (16, 31), (9, 34)], [(42, 27), (46, 32), (51, 35)]]),
    dict(body=(20, 17, 45, 27), mane=(22, 17), head=(13, 18),
         tail=[(44, 19), (48, 15), (50, 10)],
         far=[[(27, 24), (29, 30), (27, 36)], [(40, 24), (42, 30), (40, 35)]],
         near=[[(25, 24), (23, 30), (20, 36)], [(42, 24), (37, 29), (33, 34)]]),
    dict(body=(21, 16, 44, 27), mane=(23, 17), head=(14, 19),
         tail=[(43, 18), (47, 13), (50, 12)],
         far=[[(27, 24), (30, 30), (33, 35)], [(39, 24), (37, 30), (38, 36)]],
         near=[[(26, 24), (27, 30), (29, 36)], [(41, 24), (35, 30), (31, 36)]]),
    dict(body=(19, 20, 46, 29), mane=(21, 20), head=(12, 21),
         tail=[(45, 22), (50, 20), (53, 18)],
         far=[[(25, 25), (19, 29), (13, 31)], [(39, 26), (44, 31), (48, 35)]],
         near=[[(23, 26), (15, 29), (8, 31)], [(41, 26), (43, 31), (46, 36)]]),
]


# Lying down, head up (Lion_4): the cutscenes' resting lions. Low and
# long with the paws out front, so it still reads as a silhouette
LION_LYING = dict(body=(23, 28, 50, 35), mane=(21, 23), mane_scale=0.75, head=(13, 22),
                  tail=[(49, 33), (52, 35), (54, 32)],
                  far=[[(26, 32), (16, 34), (8, 34)], [(40, 31), (37, 34), (33, 35)]],
                  near=[[(25, 33), (14, 35), (5, 35)], [(44, 30), (48, 34), (41, 35)]])


# Standing tall, head up (Lion_5): the lion on the rock in the title
# shot and Sultan in the ending
LION_STANDING = dict(body=(23, 18, 46, 28), mane=(20, 15), head=(11, 13),
                     tail=[(45, 21), (50, 23), (52, 17)],
                     far=[[(30, 25), (31, 30), (31, 36)], [(38, 25), (36, 30), (37, 36)]],
                     near=[[(25, 25), (23, 30), (22, 36)], [(43, 24), (45, 30), (44, 36)]])


def _lion_leg(d, leg, fill, front):
    (jx, jy), (kx, ky), (px, py) = leg
    d.line([(jx, jy), (kx, ky)], fill=fill, width=4)
    d.line([(kx, ky), (px, py)], fill=fill, width=3)
    d.ellipse((jx - 3, jy - 3, jx + 3, jy + 2), fill=fill)
    # Paw, toes forward (left)
    d.rectangle((px - 2, py - 1, px + 1, py), fill=fill)


def _lion_shade(img, base, light, dark):
    """Lit from above: top edges light, bottom edges dark."""
    px = np.array(img)
    solid = px[:, :, 3] > 0
    is_base = np.all(px == np.array(base), axis=2)
    above = np.zeros_like(solid)
    above[1:, :] = solid[:-1, :]
    below = np.zeros_like(solid)
    below[:-1, :] = solid[1:, :]
    px[is_base & ~above] = light
    px[is_base & ~below] = dark
    return Image.fromarray(px, "RGBA")


def _lion_mane(d, cx, cy, scale=1.0):
    # A shaggy disc: the radius wobbles in tufts
    tufts = [9, 8, 10, 8, 9, 10, 8, 9, 8, 10, 9, 8]
    points = []
    for i in range(48):
        a = i / 48 * 2 * math.pi
        r = scale * (tufts[(i // 4) % len(tufts)] - (0.8 if i % 4 in (0, 3) else 0))
        # Flatter on top, longer down the chest
        ry = r * (0.85 if math.sin(a) < 0 else 1.05)
        points.append((cx + r * math.cos(a), cy + ry * math.sin(a)))
    d.polygon(points, fill=MANE)


def _lion_mane_texture(img, cx, cy):
    px = np.array(img)
    is_mane = np.all(px == np.array(MANE), axis=2)
    ys, xs = np.nonzero(is_mane)
    for x, y in zip(xs, ys):
        dx, dy = x - cx, y - cy
        strand = (x * 7 + y * 13 + (x * y) % 5) % 7
        if dx + dy > 6 or strand == 0:
            px[y, x] = MANE_DARK
        elif dx + dy < -5 and strand in (2, 4):
            px[y, x] = MANE_LIGHT
    return Image.fromarray(px, "RGBA")


def _lion_head(d, hx, hy):
    # Skull, ear, then a short boxy muzzle with a pale chin
    d.ellipse((hx - 3, hy - 4, hx + 4, hy + 3), fill=LION_FUR)
    d.ellipse((hx - 1, hy - 5, hx + 1, hy - 3), fill=LION_FUR_DARK)
    d.rectangle((hx - 6, hy - 1, hx - 1, hy + 2), fill=LION_FUR)
    d.rectangle((hx - 5, hy + 2, hx - 2, hy + 3), fill=LION_MUZZLE)
    d.line([(hx - 3, hy - 3), (hx + 1, hy - 3)], fill=LION_FUR_LIGHT)
    d.line([(hx - 6, hy - 1), (hx - 6, hy)], fill=LION_INK)
    d.point((hx - 2, hy - 2), fill=LION_INK)
    d.line([(hx - 5, hy + 1), (hx - 3, hy + 1)], fill=LION_FUR_DARK)


def lion_frame(pose):
    w, h = 56, 40

    # Far legs and tail, in shadow
    back, d = canvas(w, h)
    for leg in pose["far"]:
        _lion_leg(d, leg, LION_FAR, True)
    d.line(pose["tail"], fill=LION_FUR_DARK, width=2)
    tx, ty = pose["tail"][-1]
    d.ellipse((tx - 2, ty - 2, tx + 1, ty + 1), fill=MANE_DARK)

    # Body, then the near legs with a dark edge so they stand out
    body, d = canvas(w, h)
    d.ellipse(pose["body"], fill=LION_FUR)
    body = _lion_shade(body, LION_FUR, LION_FUR_LIGHT, LION_FUR_DARK)

    legs, d = canvas(w, h)
    for leg in pose["near"]:
        _lion_leg(d, leg, LION_FUR, True)
    legs = _lion_shade(legs, LION_FUR, LION_FUR_LIGHT, LION_FUR_DARK)
    legs = outline(legs, LION_FUR_DARK)

    # Mane, then the face over it
    front, d = canvas(w, h)
    mx, my = pose["mane"]
    _lion_mane(d, mx, my, pose.get("mane_scale", 1.0))
    front = _lion_mane_texture(front, mx, my)
    d = ImageDraw.Draw(front)
    _lion_head(d, *pose["head"])

    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    for layer in (back, body, legs, front):
        img.alpha_composite(layer)
    return outline(img, LION_INK)


def lion_run():
    """56x40 frames side by side: the gallop (Lion_0..Lion_3), lying
    down (Lion_4) and standing tall (Lion_5)."""
    poses = LION_POSES + [LION_LYING, LION_STANDING]
    sheet = Image.new("RGBA", (56 * len(poses), 40), (0, 0, 0, 0))
    for i, pose in enumerate(poses):
        sheet.alpha_composite(lion_frame(pose), (56 * i, 0))
    return sheet


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


def life_medal():
    """One of the player's hits in the boss fight, shown top-left like
    Contra's life medals."""
    img, d = canvas(11, 16)
    d.polygon([(2, 1), (4, 1), (5, 6), (3, 6)], fill=rgb("#2d6cdf"))
    d.polygon([(6, 1), (8, 1), (7, 6), (5, 6)], fill=rgb("#d0301f"))
    d.ellipse((1, 6, 9, 14), fill=GOLD)
    d.ellipse((3, 8, 7, 12), fill=GOLD_DARK)
    d.point((4, 9), fill=rgb("#fff2a0"))
    d.point((3, 7), fill=rgb("#fff2a0"))
    return outline(img)


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


# =========================
# RIFLE SHOT (player and soldiers)
# =========================

def rifle_bullet():
    # A hot round pellet: white core, amber, red rim
    rim, amber, core = rgb("#c0381c"), rgb("#ffb347"), rgb("#fff6d8")
    rows = [
        " RRRR ",
        "RYWWYR",
        "RWWWYR",
        "RYWYYR",
        "RYYYRR",
        " RRRR ",
    ]
    colors = {"R": rim, "Y": amber, "W": core}
    img, d = canvas(6, 6)
    for y, row in enumerate(rows):
        for x, c in enumerate(row):
            if c in colors:
                d.point((x, y), fill=colors[c])
    return img


def main():
    save(rifle_bullet(), "Sprites", "FX", "Bullet.png")
    save(lion_run(), "Sprites", "Lion", "Lion.png")
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
    save(life_medal(), "BossFight", "Art", "LifeMedal.png")

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
