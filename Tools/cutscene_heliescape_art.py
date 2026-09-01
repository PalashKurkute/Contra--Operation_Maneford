"""Pixel art for Cutscene_HelicopterEscape: the General standing and
running (singed, helmet askew) and leaning out of his helicopter,
Tala standing and running, and the helipads.

The compound behind them is AfterStage1's night art, and the
helicopters and the Mecha-Lion are the gameplay sprites. Run from
the repo root:
    python Tools/cutscene_heliescape_art.py

Writes Assets/Cutscenes/HeliEscape/Art/<name>.png. Unity imports
them as point-filtered sprites at 16 pixels per unit.
"""

import os

import numpy as np

import cutscene_intro_art as kit
from cutscene_intro_art import Canvas, rgb
from cutscene_afterstage2_art import (
    general_head, thick, disc, SKIN, SKIN_DARK, MANE, MANE_DARK, HAIR,
    UNI, UNI_DARK, UNI_LIGHT, BOOT, BOOT_LIGHT, GOLD, GOLD_DARK, INK,
)
from cutscene_afterstage1_art import STEEL, STEEL_DARK, STEEL_LIGHT

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "HeliEscape", "Art")
rng = np.random.default_rng(61)

# Tala (colours from her portrait)
TALA_SKIN, TALA_SKIN_DARK = "#b07850", "#7a5034"
HAT, HAT_DARK, BAND = "#a8844a", "#6e5428", "#4a3418"
BRAID = "#2a1a12"
SHIRT, SHIRT_DARK, SHIRT_LIGHT = "#4e6a34", "#34481e", "#6a8a46"
KHAKI, KHAKI_DARK = "#8a7a52", "#5e5236"


def singe(c, specks=10):
    """After the explosion: charred mane and hair, soot on him."""
    swap = {rgb(MANE): rgb("#4a3020"), rgb(MANE_DARK): rgb("#20140c"),
            rgb(HAIR): rgb("#2a1a10")}
    for y in range(c.h):
        for x in range(c.w):
            px = tuple(int(v) for v in c.get(x, y))
            if px in swap:
                c.set(x, y, swap[px])
    soot = [(x, y) for y in range(c.h) for x in range(c.w)
            if tuple(int(v) for v in c.get(x, y)) in (rgb(SKIN), rgb(UNI), rgb(UNI_LIGHT))]
    for i in rng.choice(len(soot), specks, replace=False):
        c.set(*soot[i], "#2a2626")


# =========================
# THE GENERAL, STANDING, FACING RIGHT
# =========================

def general_standing(pose):
    """Poses: shout (fist up), run_0, run_1. A stubby king, 30x38."""
    c = Canvas(30, 38)

    # Legs: standing, or mid-scurry
    legs = {"shout": ((11, 26, 11, 34), (15, 26, 16, 34)),
            "run_0": ((12, 26, 7, 33), (14, 26, 19, 32)),
            "run_1": ((12, 26, 14, 34), (14, 26, 12, 34))}[pose]
    for i, (x0, y0, x1, y1) in enumerate(legs):
        thick(c, x0, y0, x1, y1, UNI_DARK if i == 0 else UNI, 1)
        c.rect(x1 - 1, y1 + 1, x1 + 3, y1 + 2, BOOT)
        c.set(x1, y1 + 1, BOOT_LIGHT)

    # Torso
    c.rect(9, 16, 17, 27, UNI)
    c.line(9, 16, 9, 27, UNI_DARK)
    c.line(17, 16, 17, 27, UNI_LIGHT)
    c.line(9, 25, 17, 25, "#3a2414")
    c.set(16, 25, GOLD)
    for i, color in enumerate(("#c83030", "#3050c0", "#e0c040")):
        c.set(13 + i, 19, color)
    c.rect(10, 16, 16, 17, GOLD)

    general_head(c, 10, 5, "shout", askew=2)

    # The near arm
    if pose == "shout":
        thick(c, 14, 18, 19, 13, UNI_LIGHT)
        thick(c, 19, 13, 21, 6, UNI_LIGHT)
        c.rect(20, 3, 23, 6, SKIN)
        c.line(20, 7, 22, 7, GOLD)
    else:
        swing = 1 if pose == "run_0" else -1
        thick(c, 13, 18, 13 + 5 * swing, 23, UNI_LIGHT)
        c.rect(13 + 6 * swing - 1, 23, 13 + 6 * swing, 24, SKIN)

    singe(c)
    c.save("general_" + pose)


def general_window():
    """His head and a shaken fist, out of the helicopter's window."""
    c = Canvas(26, 22)
    general_head(c, 6, 7, "shout", askew=2)
    thick(c, 17, 21, 20, 12, UNI_LIGHT)
    c.rect(19, 8, 22, 11, SKIN)
    c.line(19, 12, 21, 12, GOLD)
    singe(c, 5)
    c.save("general_window")


# =========================
# TALA, FACING RIGHT
# =========================

def tala(pose):
    """Poses: stand, run_0, run_1. Ranger hat, braid, 22x34."""
    c = Canvas(22, 34)

    legs = {"stand": ((9, 22, 9, 31), (12, 22, 13, 31)),
            "run_0": ((10, 22, 6, 30), (12, 22, 16, 29)),
            "run_1": ((10, 22, 12, 31), (12, 22, 10, 31))}[pose]
    for i, (x0, y0, x1, y1) in enumerate(legs):
        thick(c, x0, y0, x1, y1, KHAKI_DARK if i == 0 else KHAKI, 1)
        c.rect(x1 - 1, y1 + 1, x1 + 2, y1 + 2, BOOT)

    # Shirt, belt
    c.rect(8, 12, 14, 22, SHIRT)
    c.line(8, 12, 8, 22, SHIRT_DARK)
    c.line(14, 12, 14, 22, SHIRT_LIGHT)
    c.line(8, 21, 14, 21, "#3a2a18")
    c.set(12, 15, GOLD)                        # ranger badge

    # Head, braid down her back
    c.rect(9, 5, 14, 11, TALA_SKIN)
    c.line(9, 5, 9, 11, TALA_SKIN_DARK)
    c.set(15, 8, TALA_SKIN)
    c.set(13, 7, INK)
    c.line(12, 10, 13, 10, TALA_SKIN_DARK)
    c.rect(7, 6, 8, 9, BRAID)
    braid_end = 17 if pose == "stand" else 15
    thick(c, 7, 9, 6 if pose == "stand" else 4, braid_end, BRAID, 0)

    # Campaign hat: wide brim, pinched crown
    c.line(5, 5, 17, 5, HAT_DARK)
    c.line(6, 4, 16, 4, HAT)
    c.rect(9, 1, 14, 3, HAT)
    c.line(9, 3, 14, 3, BAND)
    c.set(11, 0, HAT_DARK)
    c.set(12, 0, HAT_DARK)

    # The near arm
    if pose == "stand":
        thick(c, 11, 13, 11, 19, SHIRT_LIGHT, 0)
        c.set(11, 20, TALA_SKIN)
    else:
        swing = 1 if pose == "run_0" else -1
        thick(c, 11, 13, 11 + 4 * swing, 18, SHIRT_LIGHT, 0)
        c.set(11 + 5 * swing, 19, TALA_SKIN)

    c.save("tala_" + pose)


# =========================
# HELIPADS
# =========================

def helipad():
    """A raised pad, side on: steel deck, hazard edge, blue edge lights."""
    w, h = 128, 10
    c = Canvas(w, h)
    c.rect(0, 2, w - 1, h - 1, STEEL)
    c.line(0, 2, w - 1, 2, STEEL_LIGHT)
    c.line(0, h - 1, w - 1, h - 1, STEEL_DARK)
    for x in range(w):
        c.set(x, 3, "#c9a227" if (x // 4) % 2 else "#1a1a1a")
    for x in range(4, w, 20):
        c.rect(x, 0, x + 1, 1, "#60a0ff")
    c.save("helipad")


def main():
    print("Writing HeliEscape art to " + os.path.normpath(kit.OUT))
    for pose in ("shout", "run_0", "run_1"):
        general_standing(pose)
    general_window()
    for pose in ("stand", "run_0", "run_1"):
        tala(pose)
    helipad()


if __name__ == "__main__":
    main()
