"""Sound for Cutscene_Ending: the General's helicopter sputters and
crashes at dawn; Rook and Tala walk up to the wreck; the General
kneels, they talk, Tala lifts the lion helmet off; then sunrise on
the ridge, Rook sitting among the freed lions.

Everything except the voices is synthesized (with the earlier
cutscenes' building blocks), and the score is written to the shot
timing. If you retime the Timeline, change the times in SHOTS and
rerun.

Run from the repo root, after Tools/cutscene_ending_vo.ps1:
    python Tools/cutscene_ending_audio.py

Writes Assets/Cutscenes/Ending/Audio/<name>.wav (44.1 kHz, 16-bit)
and prints each voice line's length for the subtitles.
"""

import os
import sys

import numpy as np
from scipy import signal

import cutscene_intro_audio as kit
from cutscene_intro_audio import (
    n, times, lowpass, highpass, bandpass, white, brown, sine, saw,
    envelope, decay, place, pan, stereo, reverb, fade, write, read, trim,
    gusts, crickets, metal_clink, taiko, pad, typing, SR,
)
from cutscene_afterstage1_audio import (
    bell, brass, D2, E2, F2, G2, A2, Bb2, A1, Bb1, C2,
    C3, D3, E3, F3, Fs3, G3, A3, B3, D4, E4, Fs4, A4,
)
from cutscene_heliescape_audio import crackle

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "Ending", "Audio")
VO_IN = os.path.join(kit.ROOT, "Temp", "CutsceneVO")

rng = np.random.default_rng(73)

Cs4, G4, B4, Cs3, Bb3 = 277.18, 392.0, 493.88, 138.59, 233.08

# Shot timing in seconds, matching the Ending Timeline
# (Assets/Cutscenes/Editor/EndingBuilder.cs)
SHOTS = {
    "dive": 1.9,        # the engine dies; it spins down
    "crash": 3.0,       # cut to the field
    "impact": 3.7,      # it hits
    "rest": 4.6,        # rolls onto its side, still
    "approach": 5.8,    # Rook and Tala walk up, guns raised
    "crawl": 10.0,      # the General crawls out
    "kneel": 12.2,      # and kneels
    "two": 13.0,        # cut to the two of them
    "gen_01": 13.4,     # "Go on, then. Finish it."
    "rook_01": 18.4,    # "Nobody's finishing anything."
    "lower": 19.6,      # Rook lowers his gun
    "close": 20.8,      # cut close on the General
    "rook_02": 21.2,    # "They were never your army. They were just lions."
    "gen_02": 26.4,     # "They were the only ones... who ever bowed."
    "tala_in": 30.8,    # cut: Tala steps in
    "reach": 32.0,      # reaches for the helmet
    "lift": 32.4,       # lifts it off
    "tala_01": 33.6,    # "It's over, Leopold."
    "light": 34.0,      # the first light of the sun reaches them
    "white": 36.6,      # fade to white
    "ridge": 37.5,      # sunrise on the ridge
    "caption": 38.5,    # the place caption
    "lion_in": 40.0,    # a lion walks up
    "lion_down": 42.4,  # and lies down beside him
    "touch": 44.0,      # he rests his hand on its mane
    "wide": 46.0,       # pull back: the sun, the far rock
    "black": 52.5,      # fade to black
    "end": 54.0,
}

CAPTION = [("MANEFORD WILDLIFE RESERVE", 1.1), ("DAY 10  -  06:12", 0.8)]


# =========================
# VOICES, OUT IN THE FIELD
# =========================

def field_voice(x, deeper):
    x = trim(x)
    if deeper:
        x = signal.resample_poly(x, 100, 94)
    x = highpass(x, 110, 2)
    x = lowpass(x, 7500, 2)
    return fade(reverb(x, 1.6, 0.12, 4000), 0.005, 0.3)


def voice_lines():
    lengths = {}

    for name, deeper in (("gen_01", True), ("rook_01", False), ("rook_02", False),
                         ("gen_02", True), ("tala_01", False)):
        path = os.path.join(VO_IN, "en_" + name + ".wav")

        if not os.path.exists(path):
            sys.exit("Missing " + path + " (run Tools/cutscene_ending_vo.ps1 first)")

        raw = trim(read(path))
        write("vo_" + name, field_voice(raw, deeper), 0.95)
        lengths[name] = len(raw) / SR * (100 / 94 if deeper else 1)

    return lengths


# =========================
# THE CRASH
# =========================

def sputter():
    """The rotor labouring, the engine coughing, then the whine of the fall."""
    seconds = SHOTS["impact"] + 0.1
    t = times(seconds)
    rate = np.where(t < SHOTS["dive"], 11 + 2 * np.sin(2 * np.pi * 1.3 * t), 11 * np.exp(-(t - SHOTS["dive"]) / 1.2))
    phase = np.cumsum(rate) / SR
    chop = np.maximum(0, np.sin(2 * np.pi * phase)) ** 6
    chop = lowpass(white(seconds), 600) * chop * 2 + sine(55, seconds) * chop
    x = chop * 0.6
    for at in (0.5, 0.9, 1.3, 1.55, 1.8):                  # backfires
        place(x, (lowpass(white(0.12), 1500) * decay(0.12, 0.03)) * 1.2, at)
    fall = sine(900 * np.exp(-np.clip(t - SHOTS["dive"], 0, None) / 1.5) + 200, seconds)
    x += fall * 0.15 * (t > SHOTS["dive"]) * np.clip((t - SHOTS["dive"]) / 0.5, 0, 1)
    write("sfx_sputter", fade(reverb(pan(x, np.clip(t / seconds * 1.6 - 0.8, -0.8, 0.8)), 1.5, 0.25), 0.3, 0.02), 0.7)


def crash():
    seconds = 4.5
    t = times(seconds)
    boom = sine(30 + 60 * np.exp(-t / 0.06), seconds) * decay(seconds, 0.8)
    crunch = bandpass(white(seconds), 300, 4000) * decay(seconds, 0.25)
    x = boom * 1.1 + crunch * 0.7
    for _ in range(18):
        place(x, metal_clink(0.4, rng.uniform(500, 2600)) * rng.uniform(0.15, 0.4), rng.uniform(0.0, 1.6))
    glass = highpass(white(0.6), 3500) * decay(0.6, 0.12)
    place(x, glass * 0.4, 0.05)
    place(x, metal_clink(1.2, 380) * 0.8, 0.95)            # it settles onto its side
    write("sfx_crash", reverb(stereo(x), 2.4, 0.3, 2500), 0.95)


def ring():
    """The ringing in their ears afterwards."""
    seconds = 5.0
    x = sine(4100, seconds) * envelope(seconds, 0.05, 3.5, 1.5) * 0.3
    write("sfx_ring", stereo(x), 0.25)


def field_ambience():
    """Before dawn in the field: crickets, a little wind."""
    seconds = SHOTS["white"] + 1.0
    x = crickets(seconds, 4200, -0.4) + crickets(seconds, 4700, 0.5)
    wind = gusts(seconds, 0.5)
    x = x * 0.5 + (stereo(wind) if wind.ndim == 1 else wind) * 0.5
    write("amb_field", fade(x, 0.5, 1.0), 0.45)


def steps():
    """Rook and Tala walking through the grass, slowly."""
    seconds = SHOTS["crawl"] - SHOTS["approach"]
    out = np.zeros((n(seconds), 2))
    for start, gap, where in ((0.2, 0.55, -0.3), (0.5, 0.6, -0.6)):
        at = start
        while at < seconds - 0.3:
            step = bandpass(white(0.18), 800, 5000) * envelope(0.18, 0.02, 0.12) * 0.5
            step += sine(80, 0.18) * decay(0.18, 0.03) * 0.4
            place(out, pan(step * rng.uniform(0.6, 1), where), at)
            at += gap * rng.uniform(0.95, 1.05)
    write("sfx_steps", reverb(out, 0.9, 0.15)[:len(out)], 0.4)


def crawl():
    """Scraping out of the wreck, a cough, a groan as he kneels."""
    seconds = 2.6
    x = np.zeros(n(seconds))
    for at in (0.1, 0.6, 1.1, 1.5):
        place(x, bandpass(white(0.35), 400, 3000) * envelope(0.35, 0.05, 0.2) * 0.5, at)
    cough = bandpass(white(0.15), 400, 1800) * envelope(0.15, 0.01, 0.09)
    for at in (0.8, 0.97):
        place(x, cough * 0.6, at)
    place(x, metal_clink(0.4, 900) * 0.3, 0.3)
    write("sfx_crawl", reverb(x, 0.9, 0.18), 0.5)


def gun_lower():
    x = metal_clink(0.3, 1700) * 0.6 + bandpass(white(0.3), 600, 3000) * envelope(0.3, 0.02, 0.2) * 0.3
    write("sfx_gun_lower", reverb(x, 0.8, 0.15), 0.4)


def helmet_lift():
    seconds = 1.2
    t = times(seconds)
    scrape = bandpass(white(seconds), 1500, 5000) * envelope(seconds, 0.1, 0.4) * 0.3
    x = scrape + metal_clink(1.2, 2200)[:n(seconds)] * 0.2
    place(x, metal_clink(0.8, 1400) * 0.4, 0.6)
    write("sfx_helmet", reverb(x, 1.2, 0.25), 0.45)


# =========================
# SUNRISE
# =========================

def dawn_ambience():
    seconds = SHOTS["end"] - SHOTS["ridge"]
    wind = gusts(seconds, 0.4)
    out = (stereo(wind) if wind.ndim == 1 else wind) * 0.4
    at = 0.6
    while at < seconds - 1:
        base = rng.uniform(2600, 4200)
        for k in range(int(rng.integers(2, 6))):            # a little phrase
            length = rng.uniform(0.05, 0.12)
            f = base * (1 + 0.25 * np.sin(np.linspace(0, np.pi, n(length))) * rng.choice([-1, 1]))
            chirp = np.sin(2 * np.pi * np.cumsum(f) / SR) * envelope(length, 0.01, 0.04)
            place(out, pan(chirp * 0.12, rng.uniform(-0.8, 0.8)), at + k * rng.uniform(0.09, 0.16))
        at += rng.uniform(0.8, 2.2)
    write("amb_dawn", fade(reverb(out, 1.8, 0.3)[:len(out)], 1.0, 1.5), 0.45)


def purr():
    """A big cat, contented."""
    seconds = 3.0
    t = times(seconds)
    x = lowpass(brown(seconds), 220) * (0.55 + 0.45 * np.sin(2 * np.pi * 24 * t)) ** 2
    x /= np.max(np.abs(x))
    x *= (0.6 + 0.4 * np.sin(2 * np.pi * 0.7 * t)) * envelope(seconds, 0.4, 1.0)
    write("sfx_purr", stereo(x), 0.5)


def caption_typing():
    for i, (text, seconds) in enumerate(CAPTION):
        write("sfx_type_%d" % (i + 1), typing(len(text), seconds), 0.3)


# =========================
# MUSIC
# =========================

def piano(f, length=3.0):
    t = times(length)
    x = (sine(f, length) + 0.45 * sine(2 * f, length) * decay(length, 0.9) +
         0.2 * sine(3 * f, length) * decay(length, 0.4) + 0.08 * sine(4 * f, length) * decay(length, 0.25))
    return lowpass(x * decay(length, 1.4) * np.clip(t / 0.004, 0, 1), 4500)


def chord(music, at, notes, length=3.0, level=0.18):
    for k, f in enumerate(notes):
        place(music, piano(f, length) * level, at + k * 0.03)


def strings(notes, seconds, attack=1.5, release=1.5, tone=1200):
    return pad(notes, seconds, attack, release, tone)


def horn(notes, length):
    return brass(notes, length, 800) * envelope(length, 0.35, 0.6)


def score():
    sh = SHOTS
    total = sh["end"]
    music = np.zeros(n(total))

    # --- The fall: a low drone and heartbeats into the impact
    place(music, strings([D2, A2, Bb2], sh["impact"], 0.5, 0.05, 500) * 0.3, 0.0)
    for at in (1.2, 2.0, 2.6, 3.0, 3.3, 3.5):
        place(music, taiko(0.6, 50) * 0.4, at)
    place(music, taiko(2.4, 40) * 0.9, sh["impact"])

    # --- After: near silence, a soft low pad, slow heartbeats
    place(music, strings([D2, A2], sh["crawl"] - sh["rest"] + 1.0, 2.5, 1.0, 400) * 0.25, sh["rest"])
    for at in np.arange(sh["approach"], sh["crawl"], 1.1):
        place(music, taiko(0.8, 46) * 0.18, at)

    # --- The talk: sparse piano in D minor, strings underneath
    progression = [
        (sh["crawl"] + 0.5, [D3, F3, A3]), (sh["kneel"] + 0.3, [Bb2 * 1, D3, F3]),
        (sh["two"] + 0.2, [D2 * 2, F3, A3]), (sh["gen_01"] + 2.4, [Bb2, D3, F3]),
        (sh["rook_01"] - 0.2, [F2 * 2, A3, C3 * 2]), (sh["lower"] + 0.2, [C3, E3, G3]),
        (sh["close"] + 0.2, [D3, F3, A3]), (sh["rook_02"] + 2.2, [Bb2, D3, F3]),
        (sh["gen_02"] - 0.4, [G2 * 2, Bb2 * 2, D3 * 2]), (sh["gen_02"] + 2.0, [A2, Cs3, E3]),
    ]
    for at, notes in progression:
        chord(music, at, notes, 3.2, 0.16)
    for at, f in ((sh["kneel"] + 1.6, A3 * 2), (sh["gen_01"] + 4.3, F3 * 2), (sh["rook_01"] + 2.2, E3 * 2),
                  (sh["rook_02"] + 4.4, D4), (sh["gen_02"] + 3.8, Cs4)):
        place(music, piano(f, 2.5) * 0.1, at)
    place(music, strings([D2, A2, D3], sh["gen_02"] - sh["two"], 3.0, 1.5, 700) * 0.22, sh["two"])
    place(music, strings([G2, Bb2, D3, G3], sh["tala_in"] - sh["gen_02"] + 1.0, 1.5, 1.5, 900) * 0.3,
          sh["gen_02"] - 0.4)

    # --- Tala: the turn to major, a chime on the helmet, the light
    chord(music, sh["tala_in"] + 0.2, [G2 * 2, B3, D4], 3.5, 0.15)
    place(music, bell(A4, 2.5) * 0.12, sh["lift"] + 0.1)
    place(music, bell(D4 * 2, 3.0) * 0.1, sh["lift"] + 0.5)
    chord(music, sh["tala_01"] + 0.1, [A2 * 2, Cs4, E4], 3.5, 0.15)
    swell_len = sh["ridge"] - sh["light"] + 0.6
    swell = strings([D2, A2, D3, Fs3, A3], swell_len, swell_len * 0.8, 0.6, 1600)
    place(music, swell * np.linspace(0.3, 1, n(swell_len)) * 0.45, sh["light"])
    place(music, horn([D3, A2], swell_len) * 0.18, sh["light"] + 0.4)

    # --- Sunrise: the theme, D major, slow; it opens up on the wide
    bloom = strings([D2, A2, D3, Fs3], 3.0, 0.1, 2.0, 1800)
    place(music, bloom * 0.35, sh["white"] + 0.3)
    bars = [(sh["ridge"], [D2, A2, D3, Fs3]), (sh["ridge"] + 3.0, [G2, D3, G3]),
            (sh["lion_in"] + 0.5, [E2 * 2, G3, B3]), (sh["lion_down"] - 0.4, [A2, Cs3, E3, A3]),
            (sh["touch"] - 0.2, [D2, A2, D3, Fs3]), (sh["wide"], [G2, D3, G3, B3]),
            (sh["wide"] + 2.4, [A2, E3, A3, Cs4]), (sh["wide"] + 4.6, [D2, A2, D3, Fs3, A3])]
    for k, (at, notes) in enumerate(bars):
        nxt = bars[k + 1][0] if k + 1 < len(bars) else sh["end"]
        level = 0.28 if at < sh["touch"] - 0.5 else 0.42
        place(music, strings(notes, nxt - at + 1.2, 1.0, 1.2, 1300 if level < 0.4 else 2000) * level, at)
        chord(music, at, [f * 2 for f in notes[1:3]], 3.0, 0.1)

    melody = [(0.6, Fs4), (1.35, E4), (2.1, D4), (3.3, A3 * 2), (4.2, B4), (4.95, A4), (5.7, Fs4),
              (6.9, E4), (7.8, Fs4), (8.4, A4), (9.3, D4 * 2), (10.5, Cs4 * 2), (11.4, B4),
              (12.3, A4), (13.6, Fs4), (14.4, D4 * 2)]
    for at, f in melody:
        t0 = sh["ridge"] + at
        place(music, piano(f, 3.0) * 0.17, t0)
        if t0 >= sh["touch"] - 0.3:
            place(music, horn([f / 2], 1.4) * 0.12, t0)
    place(music, bell(D4 * 2, 4.0) * 0.12, sh["touch"])
    place(music, taiko(2.4, 42) * 0.35, sh["wide"])

    mixed = reverb(music, 3.0, 0.3, 3500)[:n(total)]
    write("mus_ending", fade(mixed, 0.0, total - sh["black"]), 0.75)


def main():
    print("Voice:")
    lengths = voice_lines()

    print("The crash:")
    sputter()
    crash()
    ring()
    field_ambience()
    steps()
    crawl()
    gun_lower()
    helmet_lift()

    print("Sunrise:")
    dawn_ambience()
    purr()
    caption_typing()

    print("Music:")
    score()

    print("Voice line lengths (for the subtitles):")
    for name, length in lengths.items():
        print("  %s %.2f s, from %.2f to %.2f" %
              (name, length, SHOTS[name], SHOTS[name] + length))


if __name__ == "__main__":
    main()
