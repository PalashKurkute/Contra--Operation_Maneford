"""Turns AI-generated portrait art into the dialogue portraits.

Drop the images into Art/PortraitsRaw as <speaker>_<emotion>.png (prompts and
naming in Tools/portrait_prompts.md). For each speaker this:
  - keys out the flat background and the watermark speck,
  - frames every emotion the same way as the neutral one,
  - shrinks to SIZE pixels with one shared palette per speaker,
  - adds a dark outline,
and writes Assets/Dialogue/Portraits/<speaker>_<emotion>.png over the old art
(same names, so Cast.asset keeps pointing at them). Emotions with no image
borrow the closest one. Speakers with no images keep their old art.

Run from the repo root:
    python Tools/import_portraits.py [--size 128] [--colors 48]

Also writes a contact sheet to Temp/Captures/portraits_sheet.png.
"""

import argparse
import os
import re

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

REPO = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
RAW = os.path.join(REPO, "Art", "PortraitsRaw")
OUT = os.path.join(REPO, "Assets", "Dialogue", "Portraits")
SHEET = os.path.join(REPO, "Temp", "Captures", "portraits_sheet.png")

SPEAKERS = ["rook", "pip", "tala", "general", "dobbs", "sultan"]
ALIASES = {"gen": "general", "maneford": "general", "priya": "pip"}
EMOTIONS = ["neutral", "angry", "smug", "surprised", "panic", "soft"]

# A missing emotion borrows the first of these that exists
FALLBACK = {
    "neutral": ["soft", "smug", "surprised", "angry", "panic"],
    "angry": ["neutral"],
    "smug": ["neutral"],
    "surprised": ["panic", "neutral"],
    "panic": ["surprised", "neutral"],
    "soft": ["neutral"],
}

# Speakers whose art faces left; the game expects everyone facing right
FLIP = set()

OUTLINE = (24, 18, 30)

# How close to the background colour still counts as background (0-255)
TOLERANCE = 56

# Empty space above the head, as a fraction of the frame
HEAD_ROOM = 0.05


def find_raw(folder):
    found = {}

    for name in sorted(os.listdir(folder)):
        stem, ext = os.path.splitext(name.lower())

        if ext not in (".png", ".jpg", ".jpeg", ".webp"):
            continue

        words = re.split(r"[\s_\-]+", stem)
        speaker = ALIASES.get(words[0], words[0])
        emotion = next((w for w in words[1:] if w in EMOTIONS), "neutral")

        if speaker in SPEAKERS:
            found[(speaker, emotion)] = os.path.join(folder, name)
        else:
            print("skipped (unknown speaker):", name)

    return found


def subject_mask(rgb):
    """True on the character: not the flat background, not small specks."""
    border = np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]])
    bg = np.median(border, axis=0)
    near = np.abs(rgb.astype(int) - bg).max(axis=2) < TOLERANCE

    # Background is what touches the border, plus big enclosed pockets of
    # it (between an arm and the body)
    labels, count = ndimage.label(near)
    sizes = ndimage.sum(near, labels, range(count + 1))
    edge = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    big = np.flatnonzero(sizes > near.size * 0.001)
    background = np.isin(labels, np.union1d(edge, big)) & near

    # Drop specks (the watermark) and the anti-aliased background fringe
    subject = ndimage.binary_erosion(~background, iterations=2)
    labels, count = ndimage.label(subject)

    if count > 1:
        sizes = ndimage.sum(subject, labels, range(1, count + 1))
        subject = np.isin(labels, np.flatnonzero(sizes >= sizes.max() * 0.02) + 1)

    return subject


def frame_of(mask):
    """Square frame: the bust's bottom on the frame's bottom, a little room
    above the head, centred on the shoulders."""
    ys, xs = np.nonzero(mask)
    top, bottom = ys.min(), ys.max() + 1
    side = (bottom - top) / (1 - HEAD_ROOM)

    # The shoulders stay put when the hands move
    low = mask[max(top, bottom - int(side * 0.15)):bottom]
    cols = np.flatnonzero(low.any(axis=0))
    left = (cols.min() + cols.max()) / 2 - side / 2

    # A body cut off by the image's side stays cut off by the frame's side
    # instead of ending in a straight edge in mid-air
    lo = 0 if mask[:, 0].any() else -np.inf
    hi = mask.shape[1] if mask[:, -1].any() else np.inf
    side = min(side, hi - lo)
    left = min(max(left, lo), hi - side)

    return left, bottom, side


def shrink(rgb, mask, frame, size):
    left, bottom, side = frame
    left, top = int(round(left)), int(round(bottom - side))
    box = (left, top, left + int(round(side)), top + int(round(side)))

    # Outside the character is outline-dark, so edges average to a dark rim
    color = rgb.copy()
    color[~mask] = OUTLINE

    color = Image.fromarray(color).crop(box).resize((size, size), Image.BOX)
    alpha = Image.fromarray(mask.astype(np.uint8) * 255).crop(box).resize((size, size), Image.BOX)

    return np.array(color), np.array(alpha) >= 128


def shared_palette(images, colors):
    pixels = np.concatenate([rgb[alpha] for rgb, alpha in images])
    strip = Image.fromarray(pixels.reshape(1, -1, 3))

    return strip.quantize(colors=colors, method=Image.Quantize.MEDIANCUT, kmeans=2)


def add_outline(rgb, alpha):
    grown = ndimage.binary_dilation(alpha, structure=ndimage.generate_binary_structure(2, 1))
    ring = grown & ~alpha

    rgb[ring] = OUTLINE

    return rgb, grown


def contact_sheet(rows, size):
    scale, pad = 2, 8
    cell = size * scale + pad
    sheet = Image.new("RGB", (pad + cell * len(EMOTIONS), pad + (cell + 12) * len(rows)), (58, 64, 82))
    draw = ImageDraw.Draw(sheet)

    for r, (speaker, cells) in enumerate(rows):
        for c, emotion in enumerate(EMOTIONS):
            pic, source = cells[emotion]
            x, y = pad + c * cell, pad + r * (cell + 12)
            big = pic.resize((size * scale, size * scale), Image.NEAREST)
            sheet.paste(big, (x, y + 12), big)
            label = f"{speaker} {emotion}" + ("" if source == emotion else f" <{source}")
            draw.text((x, y), label, fill=(230, 230, 230) if source == emotion else (150, 150, 160))

    os.makedirs(os.path.dirname(SHEET), exist_ok=True)
    sheet.save(SHEET)
    print("contact sheet:", os.path.normpath(SHEET))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--size", type=int, default=128)
    parser.add_argument("--colors", type=int, default=48, help="palette per speaker, 0 = no quantizing")
    parser.add_argument("--raw", default=RAW)
    parser.add_argument("--out", default=OUT)
    args = parser.parse_args()

    raw = find_raw(args.raw)
    rows = []

    for speaker in SPEAKERS:
        have = {e: raw[(speaker, e)] for e in EMOTIONS if (speaker, e) in raw}

        if not have:
            continue

        sources = {}

        for emotion, path in have.items():
            rgb = np.array(Image.open(path).convert("RGB"))

            if speaker in FLIP:
                rgb = rgb[:, ::-1].copy()

            sources[emotion] = (rgb, subject_mask(rgb))

        # Every emotion uses the neutral framing when the image sizes match,
        # so the face doesn't jump between expressions
        ref = "neutral" if "neutral" in sources else next(iter(sources))
        ref_frame = frame_of(sources[ref][1])

        shrunk = {}

        for emotion, (rgb, mask) in sources.items():
            same = rgb.shape == sources[ref][0].shape
            shrunk[emotion] = shrink(rgb, mask, ref_frame if same else frame_of(mask), args.size)

        palette = shared_palette(list(shrunk.values()), args.colors) if args.colors else None
        done = {}

        for emotion, (rgb, alpha) in shrunk.items():
            if palette is not None:
                rgb = np.array(Image.fromarray(rgb).quantize(palette=palette, dither=Image.Dither.NONE).convert("RGB"))

            rgb, alpha = add_outline(rgb, alpha)
            done[emotion] = Image.fromarray(np.dstack([rgb, alpha.astype(np.uint8) * 255]), "RGBA")

        cells = {}

        # Neutral comes first, so it's the last resort for the rest
        for emotion in EMOTIONS:
            last_resort = cells["neutral"][1] if cells else next(iter(done))
            source = emotion if emotion in done else next(
                (f for f in FALLBACK[emotion] if f in done), last_resort)
            cells[emotion] = (done[source], source)
            done[source].save(os.path.join(args.out, f"{speaker}_{emotion}.png"))

        borrowed = [f"{e}<{s}" for e, (_, s) in cells.items() if s != e]
        print(f"{speaker}: {len(have)} images" + (f", borrowed {', '.join(borrowed)}" if borrowed else ""))
        rows.append((speaker, cells))

    if rows:
        contact_sheet(rows, args.size)
    else:
        print("no images found in", os.path.normpath(args.raw))


if __name__ == "__main__":
    main()
