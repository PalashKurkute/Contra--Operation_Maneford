"""Main menu music: a looping D minor march for OPERATION MANEFORD.

Synthesized with the cutscenes' building blocks, in the intro score's
key and colours (taiko, a string ostinato, brass), at 120 BPM. Four
bars of groove, then the tune over twelve. It loops seamlessly: the
notes and reverb that ring past the last bar are folded back onto the
first, so the join can't be heard.

Run from the repo root:
    python Tools/generate_menu_music.py

Writes Assets/Audio/Music/mus_menu.wav (44.1 kHz, 16-bit stereo).
"""

import os

import numpy as np

import cutscene_intro_audio as kit
from cutscene_intro_audio import (
    n, times, lowpass, highpass, white, sine, saw, envelope, decay,
    place, pan, reverb, write, taiko, pad, string_stab,
)
from cutscene_afterstage1_audio import brass, snare

kit.OUT = os.path.join(kit.ROOT, "Assets", "Audio", "Music")

BPM = 120
EIGHTH = 60 / BPM / 2
BAR = EIGHTH * 8
BARS = 16
LOOP = BAR * BARS

# Room for notes and the crash to ring past the end before folding
TAIL = 3.0


def hz(midi):
    return 440.0 * 2 ** ((midi - 69) / 12)


# One chord per bar: (pad voicing, bass root), as MIDI notes
Dm = ((50, 53, 57), 38)
Bb = ((50, 53, 58), 34)
F = ((48, 53, 57), 41)
C = ((48, 52, 55), 36)
Gm = ((50, 55, 58), 43)
A = ((49, 52, 57), 33)

CHORDS = [
    Dm, Dm, Bb, C,      # groove
    Dm, Bb, F, C,       # the tune
    Dm, Bb, Gm, A,      # again, reaching higher
    Bb, C, Dm, A,       # the climb, and back round to the top
]

# The tune from bar 5, a list per bar of (MIDI note, eighths);
# None is a rest
MELODY = [
    [(62, 2), (69, 3), (67, 1), (65, 2)],
    [(65, 1), (67, 1), (69, 2), (70, 2), (69, 2)],
    [(72, 3), (69, 1), (65, 2), (69, 2)],
    [(67, 6), (64, 1), (65, 1)],
    [(69, 2), (74, 3), (72, 1), (69, 2)],
    [(70, 2), (69, 2), (67, 2), (65, 2)],
    [(67, 3), (70, 1), (74, 2), (72, 2)],
    [(69, 4), (76, 2), (73, 2)],
    [(74, 6), (None, 2)],
    [(76, 2), (74, 2), (72, 2), (76, 2)],
    [(77, 4), (74, 2), (69, 2)],
    [(69, 6), (None, 2)],
]

# Bass octave jumps on each eighth of a bar
BASS_PATTERN = [0, 0, 12, 0, 0, 0, 12, 0]


def track():
    return np.zeros(n(LOOP + TAIL))


def bar_start(bar):
    return bar * BAR


# =========================
# INSTRUMENTS
# =========================

def lead(midi, length):
    """Brass with a delayed vibrato, and a soft horn an octave down."""
    ring = length + 0.15
    t = times(ring)
    wobble = np.clip((t - 0.18) / 0.2, 0, 1)
    vibrato = 1 + 0.004 * np.sin(2 * np.pi * 5.5 * t) * wobble

    f = hz(midi) * vibrato
    return brass([f], ring, 2400) + 0.35 * brass([f / 2], ring, 1100)


def bass(midi, length):
    """A saw an octave up, so it reads on small speakers, over a
    quiet sine sub."""
    f = hz(midi)
    x = saw(f * 2, length) * 0.6 + sine(f, length) * 0.35
    return lowpass(x, 900) * envelope(length, 0.005, 0.06)


def hat(level):
    return highpass(white(0.05), 7000) * decay(0.05, 0.012) * level


def crash():
    return highpass(white(2.5), 4000) * decay(2.5, 0.6)


# =========================
# PARTS
# =========================

def pads():
    x = track()

    for bar, (voicing, _) in enumerate(CHORDS):
        notes = [hz(m) for m in voicing]
        place(x, pad(notes, BAR + 0.3, 0.08, 0.3, 1100), bar_start(bar))

    return x


def bass_line():
    x = track()

    for bar, (_, root) in enumerate(CHORDS):
        for i, jump in enumerate(BASS_PATTERN):
            accent = 1.0 if i % 2 == 0 else 0.75
            note = bass(root + jump, EIGHTH * 0.9) * accent
            place(x, note, bar_start(bar) + i * EIGHTH)

    return x


def ostinato():
    """Root and fifth, alternating on the eighths, as in the intro."""
    x = track()

    for bar, (_, root) in enumerate(CHORDS):
        for i in range(8):
            note = root + 12 + (7 if i % 2 else 0)
            accent = 1.0 if i % 4 == 0 else 0.7
            place(x, string_stab(hz(note)) * accent, bar_start(bar) + i * EIGHTH)

    return x


def tune():
    x = track()

    for bar, notes in enumerate(MELODY, start=4):
        at = bar_start(bar)

        for midi, eighths in notes:
            length = eighths * EIGHTH

            if midi is not None:
                place(x, lead(midi, length * 0.95), at)

            at += length

    return x


def drums():
    """Returns (drums, hats): the hats are panned on their own."""
    kit_track = track()
    hats = track()

    for bar in range(BARS):
        start = bar_start(bar)
        full = bar >= 2

        # Taiko on 1 and 3, a push on the "and" of 2 once it's going
        place(kit_track, taiko(0.9, 62) * 0.6, start)
        place(kit_track, taiko(0.9, 62) * 0.5, start + 4 * EIGHTH)

        if full:
            place(kit_track, taiko(0.6, 64) * 0.45, start + 3 * EIGHTH)

            # Snare on 2 and 4
            for i in (2, 6):
                place(kit_track, snare(0.55), start + i * EIGHTH)

            for i in range(8):
                place(hats, hat(0.16 if i % 2 else 0.1), start + i * EIGHTH)

    # A fill into the tune
    fill = bar_start(3)
    for i in (5, 6, 7):
        place(kit_track, snare(0.35 + 0.1 * i), fill + i * EIGHTH)

    # A pickup into the second half of the tune
    place(kit_track, taiko(0.6, 64) * 0.6, bar_start(11) + 7 * EIGHTH)

    # A snare roll that climbs back into the top of the loop
    roll = bar_start(15) + 4 * EIGHTH
    steps = 8
    for i in range(steps):
        place(kit_track, snare(0.25 + 0.5 * i / steps), roll + i * EIGHTH / 2)

    # Crashes where the tune starts and where it climbs (the top of
    # the loop gets one too, rung by the roll)
    for bar in (0, 4, 12):
        place(kit_track, crash() * 0.18, bar_start(bar))

    return kit_track, hats


# =========================
# MIX
# =========================

def fold(x):
    """Wraps whatever rings past the loop onto its start."""
    length = n(LOOP)
    out = x[:length].copy()
    extra = x[length:]
    out[:len(extra)] += extra
    return out


def main():
    print("Menu music:")

    kit_track, hats = drums()

    mix = (
        pan(fold(pads()) * 0.32, 0.0) +
        pan(fold(bass_line()) * 0.3, 0.0) +
        pan(fold(ostinato()) * 0.22, -0.3) +
        pan(fold(tune()) * 0.55, 0.05) +
        pan(fold(kit_track) * 0.8, 0.0) +
        pan(fold(hats), 0.35)
    )

    # Reverb (and the rumble filter) over two passes of the loop,
    # keeping the second: the first pass's tail then rings into the
    # top, as it will in game
    length = n(LOOP)
    wet = reverb(np.concatenate([mix, mix]), 1.8, 0.2, 4500)
    music = highpass(wet, 40)[length:2 * length]

    # A little glue
    music = np.tanh(music / np.max(np.abs(music)) * 1.2) / np.tanh(1.2)

    write("mus_menu", music, 0.89)


if __name__ == "__main__":
    main()
