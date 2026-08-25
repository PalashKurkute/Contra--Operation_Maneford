"""Sound for Cutscene_AfterStage1: the last collar breaking in the
forest, then the General's compound, his voice over the base
loudspeakers, the alarm and the soldiers marching out.

Everything except the voice is synthesized (with the intro's
building blocks), and the score is written to the cutscene's shot
timing. If you retime the Timeline, change the times in SHOTS and
rerun.

Run from the repo root, after Tools/cutscene_afterstage1_vo.ps1:
    python Tools/cutscene_afterstage1_audio.py

Writes Assets/Cutscenes/AfterStage1/Audio/<name>.wav (44.1 kHz,
16-bit) and prints each voice line's length for the subtitles.
"""

import os
import sys

import numpy as np
from scipy import signal

import cutscene_intro_audio as kit
from cutscene_intro_audio import (
    n, times, lowpass, highpass, bandpass, sweep_lowpass, white, brown,
    sine, saw, supersaw, envelope, decay, place, pan, stereo, reverb, fade,
    write, read, trim, gusts, crickets, metal_clink, taiko, string_stab, pad,
    typing, SR,
)

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "AfterStage1", "Audio")
VO_IN = os.path.join(kit.ROOT, "Temp", "CutsceneVO")
SFX = os.path.join(kit.ROOT, "Assets", "Audio", "SFX")

rng = np.random.default_rng(31)

# Shot timing in seconds, matching the AfterStage1 Timeline
# (Assets/Cutscenes/Editor/AfterStage1Builder.cs)
SHOTS = {
    "shatter": 2.6,     # the last collar breaks
    "shake": 3.1,       # the lion shakes its head
    "two_shot": 3.8,    # cut to Rook and the lion
    "rise": 4.2,        # it gets up
    "walk": 4.7,        # turns away, a few steps
    "look": 5.85,       # looks back at Rook
    "run": 7.15,        # runs into the forest
    "roar": 8.7,        # a roar far off; searchlights over the hills
    "base": 9.8,        # cut to the compound at night
    "caption": 10.3,    # the place caption types on
    "pa_on": 11.85,     # the loudspeakers click on
    "gen_01": 12.35,    # "He freed my lions?"
    "banner": 14.4,     # cut to the banner
    "gen_02": 15.1,     # "Fine."
    "gen_03": 16.4,     # "Release the soldiers."
    "alarm": 18.5,      # alarm beacons and klaxon
    "gate": 19.3,       # cut to the gate
    "gate_open": 19.5,  # the gate rolls open
    "march": 20.1,      # the soldiers march out
    "cut": 23.6,        # hard cut to black on the last hit
    "end": 24.4,
}

# Caption lines, as typed in the Timeline
CAPTION = [("ROYAL MANEFORD COMPOUND", 1.0), ("EAST PERIMETER  21:40", 0.8)]


# =========================
# VOICE: THE GENERAL ON THE PA
# =========================

def pa_voice(x, distant):
    """Through cheap horn loudspeakers, bouncing round the compound."""
    x = trim(x)
    x = signal.resample_poly(x, 100, 94)        # a touch deeper and slower
    x = highpass(x, 380, 4)
    x = lowpass(x, 3400, 4)
    x = x + bandpass(x, 1100, 1900) * 1.3       # horn honk
    x = np.tanh(x * 3.0) / np.tanh(3.0)

    out = np.zeros(len(x) + n(1.4))
    place(out, x, 0.0)

    # Slapback off the walls, then a big outdoor tail
    for delay, gain in ((0.19, 0.42), (0.41, 0.24), (0.66, 0.12)):
        place(out, lowpass(x, 2200) * gain, delay)

    if distant:
        wet = reverb(out, 3.0, 0.5, 2400)
        wet = lowpass(wet, 2800)
    else:
        wet = reverb(out, 2.0, 0.3, 3200)

    return fade(wet, 0.005, 0.4)


def voice_lines():
    lengths = {}

    for name, distant in (("gen_01", True), ("gen_02", False), ("gen_03", False)):
        path = os.path.join(VO_IN, name + ".wav")

        if not os.path.exists(path):
            sys.exit("Missing " + path +
                     " (run Tools/cutscene_afterstage1_vo.ps1 first)")

        raw = trim(read(path))
        write("vo_" + name, pa_voice(raw, distant), 0.95)
        lengths[name] = len(raw) / SR * 100 / 94

    return lengths


def pa_on():
    """The mic opens: a click, mains hum and a squeal of feedback."""
    seconds = 1.2
    t = times(seconds)
    click = np.zeros(n(seconds))
    click[:n(0.006)] = np.linspace(1, -1, n(0.006))

    hum = (sine(60, seconds) * 0.15 + sine(120, seconds) * 0.1) * \
        envelope(seconds, 0.02, 0.5)

    whine = sine(2150 + 160 * t, seconds)
    whine *= np.clip((t - 0.05) / 0.25, 0, 1) * np.exp(-np.clip(t - 0.45, 0, None) / 0.18)
    whine *= 0.8 + 0.2 * np.sin(2 * np.pi * 7 * t)

    x = bandpass(click * 0.6 + hum + whine * 0.5, 250, 5200)
    write("sfx_pa_on", reverb(x, 2.4, 0.45, 3000), 0.55)


# =========================
# THE FOREST: THE LAST COLLAR
# =========================

def twilight_ambience():
    seconds = SHOTS["base"] + 0.05
    t = times(seconds)

    wind = lowpass(brown(seconds), 380) * gusts(seconds, 0.8)
    wind /= np.max(np.abs(wind))
    leaves = bandpass(white(seconds), 2400, 7000) * gusts(seconds) ** 3

    left = wind * 0.6 + leaves * 0.05
    right = np.roll(wind, n(0.3)) * 0.6 + np.roll(leaves, n(0.17)) * 0.05
    bed = np.stack([left, right], axis=1)

    # The crickets stop dead when the collar breaks, then carry on
    bugs = crickets(seconds, 4300, -0.6) * 0.07 + crickets(seconds, 4900, 0.5) * 0.06
    after = t - SHOTS["shatter"]
    hush = np.ones(len(t))
    hush[after > 0] = np.clip((after[after > 0] - 1.8) / 1.6, 0, 1)
    bed += bugs * hush[:, None]

    write("amb_twilight", fade(bed, 0.6, 0.02), 0.45)


def collar_buzz():
    """The collar overloading: hum, sparks and a quickening beep."""
    seconds = SHOTS["shatter"] + 0.01
    t = times(seconds)
    grow = (t / seconds) ** 1.5

    pitch = 100 * (1 + 0.18 * grow)
    hum = saw(pitch, seconds) * 0.5 + saw(pitch * 2, seconds) * 0.25
    hum = bandpass(hum, 150, 3500) * (0.2 + 0.8 * grow)

    crackle = np.zeros(n(seconds))
    at = 0.1
    while at < seconds - 0.03:
        burst = highpass(white(0.02), 2000) * decay(0.02, 0.004)
        place(crackle, burst * rng.uniform(0.3, 1.0), at)
        at += rng.exponential(0.16 * (1.1 - at / seconds) + 0.012)

    beeps = np.zeros(n(seconds))
    for at in (0.5, 1.15, 1.65, 2.0, 2.22, 2.37, 2.47, 2.54):
        place(beeps, sine(2700, 0.05) * envelope(0.05, 0.003, 0.02), at)

    x = fade(hum * 0.5 + crackle * 0.6 + beeps * 0.25, 0.3, 0.004)
    write("sfx_collar_buzz", reverb(pan(x, -0.1), 0.6, 0.15), 0.6)


def shatter():
    seconds = 2.4
    crack = highpass(white(seconds), 1800) * decay(seconds, 0.025)
    pop = sine(40 + 90 * np.exp(-times(seconds) / 0.08), seconds) * \
        decay(seconds, 0.15)

    chime = np.zeros(n(seconds))
    for _ in range(9):
        at = min(abs(rng.normal(0.03, 0.06)), 0.4)
        hit = metal_clink(rng.uniform(0.5, 1.2), rng.uniform(2200, 4200))
        place(chime, hit * rng.uniform(0.4, 1.0), at)

    sparkle = np.zeros(n(seconds))
    for _ in range(40):
        at = min(abs(rng.normal(0.15, 0.4)), seconds - 0.05)
        tick = sine(rng.uniform(5000, 9000), 0.03) * np.hanning(n(0.03))
        place(sparkle, tick * rng.uniform(0.1, 0.4), at)

    x = crack * 0.7 + pop * 0.9 + chime * 0.6 + sparkle * 0.4

    # The game's own collar break underneath, so it's the same sound
    place(x, read(os.path.join(SFX, "collar_break.wav")) * 0.8, 0.0)

    write("sfx_shatter", reverb(x, 2.0, 0.3, 6000), 0.95)


def head_shake():
    out = np.zeros(n(0.9))

    for k in range(4):
        rustle = bandpass(white(0.11), 700, 4000) * np.hanning(n(0.11))
        place(out, rustle * (1 - k * 0.15), k * 0.12)

    snort = bandpass(white(0.25), 250, 1400) * envelope(0.25, 0.01, 0.2)
    place(out, snort * 0.9, 0.5)

    write("sfx_shake", reverb(out, 0.7, 0.15), 0.55)


def rise():
    """Getting up: grass, and a low rumble of a breath."""
    seconds = 1.0
    grass = bandpass(white(seconds), 900, 5000) * envelope(seconds, 0.15, 0.5) * 0.4
    growl = read(os.path.join(SFX, "growl.wav"))
    growl = lowpass(signal.resample_poly(growl, 5, 4), 900)[:n(seconds)]

    x = grass.copy()
    place(x, growl * 0.5 * envelope(len(growl) / SR, 0.2, 0.4), 0.1)
    write("sfx_rise", reverb(x, 0.8, 0.2), 0.45)


def chuff():
    """The look back: a soft, friendly lion chuff."""
    out = np.zeros(n(0.9))

    for at, gain in ((0.0, 1.0), (0.22, 0.7)):
        breath = bandpass(white(0.18), 180, 1100) * envelope(0.18, 0.01, 0.15, 1.5)
        rumble = sine(70 * (1 + 0.1 * np.sin(2 * np.pi * 30 * times(0.18))), 0.18) * \
            envelope(0.18, 0.01, 0.15)
        place(out, (breath * 0.9 + rumble * 0.5) * gain, at)

    write("sfx_chuff", reverb(out, 1.2, 0.25, 2500), 0.55)


def paw(length=0.12):
    thud = sine(35 + 55 * np.exp(-times(length) / 0.05), length) * decay(length, 0.03)
    grass = bandpass(white(length), 1500, 6000) * decay(length, 0.02)
    return thud * 0.8 + grass * 0.35


def lion_walk():
    out = np.zeros(n(1.0))
    for k in range(4):
        place(out, paw() * rng.uniform(0.6, 0.9), k * 0.22)
    write("sfx_lion_walk", stereo(out), 0.35)


def lion_run():
    """A gallop into the trees, getting further away."""
    seconds = 2.0
    mono = np.zeros(n(seconds))
    at, k = 0.0, 0

    while at < 1.15:
        gain = 1 - at / 1.4
        place(mono, paw(0.1) * gain * (1.0 if k % 2 == 0 else 0.7), at)
        at += 0.09 if k % 2 == 0 else 0.17
        k += 1

    rustle = bandpass(white(0.8), 800, 5000) * envelope(0.8, 0.05, 0.6) * gusts(0.8, 20)
    place(mono, rustle * 0.5, 0.95)

    write("sfx_lion_run", pan(mono, np.linspace(0.0, 0.9, n(seconds))), 0.55)


# =========================
# THE COMPOUND
# =========================

def base_ambience():
    seconds = SHOTS["end"] - SHOTS["base"] + 0.1
    t = times(seconds)

    gen = sum(sine(f, seconds) * a for f, a in
              ((50, 0.5), (100, 0.35), (150, 0.2), (200, 0.1)))
    gen = lowpass(gen * (0.85 + 0.15 * np.sin(2 * np.pi * 3.1 * t)), 400)
    buzz = bandpass(saw(120, seconds), 2000, 6000) * 0.05

    wind = lowpass(brown(seconds), 300) * gusts(seconds, 0.6)
    wind /= np.max(np.abs(wind))

    bed = stereo(gen * 0.35 + buzz)
    bed += np.stack([wind, np.roll(wind, n(0.27))], axis=1) * 0.4
    bed += crickets(seconds, 4600, -0.7) * 0.03 + crickets(seconds, 5200, 0.7) * 0.03

    write("amb_base", fade(bed, 0.02, 0.01), 0.45)


def searchlight():
    """Servo whine of the searchlights turning."""
    seconds = 4.4
    t = times(seconds)
    f = 340 + 60 * np.sin(2 * np.pi * 0.25 * t)
    whine = bandpass(saw(f, seconds) * 0.5 + sine(f * 2, seconds) * 0.3, 300, 2500)
    whine *= 0.5 + 0.5 * np.abs(np.sin(2 * np.pi * 0.25 * t))
    whine *= envelope(seconds, 0.8, 1.0)
    write("sfx_searchlight", pan(whine, 0.5 * np.sin(2 * np.pi * 0.12 * t)), 0.25)


def caption_typing():
    for i, (text, seconds) in enumerate(CAPTION):
        write("sfx_type_%d" % (i + 1), typing(len(text), seconds), 0.35)


def alarm():
    """A whooping klaxon, echoing round the compound."""
    seconds = SHOTS["cut"] - SHOTS["alarm"] + 0.02
    out = np.zeros(n(seconds))
    at = 0.0

    while at < seconds:
        length = 0.8
        rise_t = times(length) / length
        f = 420 + 620 * rise_t ** 0.7
        whoop = np.sign(sine(f, length)) * 0.4 + saw(f, length) * 0.6
        place(out, bandpass(whoop, 300, 3000) * envelope(length, 0.02, 0.1), at)
        at += 1.05

    echoed = out.copy()
    for delay, gain in ((0.23, 0.35), (0.47, 0.2)):
        place(echoed, lowpass(out, 2000) * gain, delay)

    x = reverb(echoed, 2.5, 0.4, 3000)[:n(seconds)]
    write("sfx_alarm", fade(x, 0.0, 0.01), 0.7)


def gate_roll():
    seconds = 1.9
    t = times(seconds)

    rumble = lowpass(brown(seconds), 180)
    rumble /= np.max(np.abs(rumble))
    rumble *= np.clip(t / 0.1, 0, 1) * np.clip((1.25 - t) / 0.2, 0, 1)

    rattle = np.zeros(n(seconds))
    at = 0.05
    while at < 1.2:
        place(rattle, metal_clink(0.12, rng.uniform(900, 1600)) * rng.uniform(0.2, 0.6), at)
        at += abs(rng.normal(0.06, 0.02))

    squeak = sine(1400 + 200 * np.sin(2 * np.pi * 1.3 * t), seconds) * 0.08 * (t < 1.2)

    x = rumble * 0.8 + rattle * 0.6 + squeak
    place(x, metal_clink(0.9, 520) * 1.2, 1.25)   # it stops with a clank
    write("sfx_gate", reverb(stereo(x), 1.6, 0.3), 0.8)


def march():
    """Boots in step: six soldiers, not quite together."""
    seconds = SHOTS["cut"] - SHOTS["march"] + 0.02
    out = np.zeros((n(seconds), 2))
    at = 0.0

    while at < seconds:
        build = min(1.0, 0.35 + at / 2.0)
        for _ in range(6):
            step = sine(45 + 70 * np.exp(-times(0.1) / 0.03), 0.1) * decay(0.1, 0.025) * 0.8
            step += bandpass(white(0.1), 800, 4000) * decay(0.1, 0.015) * 0.5
            step += metal_clink(0.1, rng.uniform(2500, 4000))[:n(0.1)] * 0.08
            hit = pan(step, rng.uniform(-0.5, 0.5)) * build * rng.uniform(0.6, 1.0)
            place(out, hit, max(0.0, at + rng.normal(0, 0.012)))
        at += 0.5

    x = reverb(out, 1.4, 0.25, 3000)[:len(out)]
    write("sfx_march", fade(x, 0.0, 0.01), 0.75)


# =========================
# MUSIC
# =========================

D2, E2, F2, Fs2, G2, A2, Bb2, B2 = 73.42, 82.41, 87.31, 92.50, 98.00, 110.0, 116.54, 123.47
G1, A1, Bb1, C2 = 49.00, 55.00, 58.27, 65.41
C3, Cs3, D3, E3, F3, Fs3, G3, A3, B3 = (130.81, 138.59, 146.83, 164.81, 174.61,
                                        185.00, 196.00, 220.00, 246.94)
D4, E4, Fs4, A4 = 293.66, 329.63, 369.99, 440.00


def bell(f, length=1.8):
    t = times(length)
    x = (sine(f, length) +
         0.4 * sine(f * 2.01, length) * decay(length, 0.4) +
         0.15 * sine(f * 3.98, length) * decay(length, 0.2))
    return x * decay(length, 0.7) * np.clip(t / 0.005, 0, 1)


def brass(notes, length, tone=1300):
    x = np.zeros(n(length))
    for f in notes:
        x += supersaw(f, length, 4, 0.006) + 0.4 * supersaw(f * 2, length, 3, 0.006)
    x = sweep_lowpass(x / len(notes), tone * 0.5, tone)
    return x * envelope(length, 0.04, min(0.3, length * 0.5))


def snare(level=1.0):
    length = 0.18
    body = sine(190 * (1 + 0.5 * decay(length, 0.01)), length) * decay(length, 0.04)
    rattle = bandpass(white(length), 1500, 7000) * decay(length, 0.06)
    return (body * 0.5 + rattle * 0.8) * level


def score():
    sh = SHOTS
    total = sh["end"]
    music = np.zeros(n(total))

    # --- The collar: unease, a high tremolo and a riser into the break
    place(music, pad([D2, A2], sh["shatter"] + 0.2, 1.2, 0.2, 450) * 0.35, 0.0)
    t = times(sh["shatter"])
    trem = sine(A4, sh["shatter"]) * (0.6 + 0.4 * np.sin(2 * np.pi * 9 * t))
    place(music, trem * (t / sh["shatter"]) ** 2 * 0.06, 0.0)
    riser = highpass(white(1.4), 4000) * np.linspace(0, 1, n(1.4)) ** 3
    place(music, riser * 0.12, sh["shatter"] - 1.4)

    # --- Released: a warm D major swell, then a small tune for the look
    place(music, taiko(1.6, 52) * 0.5, sh["shatter"])
    place(music, pad([D2, A2, D3, Fs3], sh["two_shot"] - sh["shatter"] + 0.6, 0.9, 0.5, 900)
          * 0.5, sh["shatter"])
    place(music, pad([G2, D3, G3, B3], sh["look"] - sh["two_shot"] + 0.4, 0.6, 0.4, 900)
          * 0.7, sh["two_shot"])
    place(music, pad([Fs2, A2, D3, Fs3], sh["run"] - sh["look"] + 0.4, 0.4, 0.4, 1000)
          * 0.75, sh["look"])
    place(music, pad([A2, E3, A3, Cs3 * 2], sh["roar"] - sh["run"] + 0.3, 0.2, 0.4, 1200)
          * 0.75, sh["run"])

    tune = [(4.3, A3), (4.75, D4), (5.2, E4), (5.85, Fs4), (6.6, E4),
            (7.15, D4), (7.45, Fs4), (7.75, A4)]
    for at, f in tune:
        place(music, bell(f) * 0.26, at)

    # --- Far off, something else: a dark Bb, heartbeats into the cut
    place(music, pad([Bb1, F2, Bb2], sh["base"] - sh["roar"] + 0.2, 0.6, 0.15, 500)
          * 0.45, sh["roar"])
    for at in (sh["base"] - 0.65, sh["base"] - 0.3):
        place(music, taiko(0.9, 50) * 0.45, at)

    # --- The compound: a pompous, sinister royal motif in D minor
    place(music, taiko(2.0, 44) * 1.0, sh["base"])
    drone_len = sh["alarm"] - sh["base"]
    place(music, pad([D2 / 2, D2, A2, F3], drone_len, 1.0, 0.3, 550) * 0.4, sh["base"])

    motif = [(0.0, D3, 0.45), (0.5, A2, 0.22), (0.75, Bb2, 0.22), (1.0, A2, 0.9)]
    for start in (sh["base"] + 0.5,):
        for at, f, length in motif:
            place(music, brass([f, f / 2], length + 0.1) * 0.35, start + at)

    place(music, taiko(1.4, 48) * 0.6, sh["pa_on"] - 0.4)

    # The banner: a low brass chord under "Fine."
    place(music, taiko(1.8, 46) * 0.8, sh["banner"])
    place(music, brass([Bb1, F2, Bb2, D3], 1.6, 900) * 0.3, sh["banner"])

    # "Release the soldiers.": the floor drops out, a held low swell
    place(music, pad([D2 / 2, D2], sh["alarm"] - sh["gen_03"], 1.2, 0.05, 300) * 0.5,
          sh["gen_03"])

    # --- The alarm: a snare march and a driving ostinato into the hit
    beat = 0.5
    eighth = beat / 2
    at, i = sh["alarm"], 0
    chords = [(D2, D3), (D2, D3), (Bb1, Bb2), (C2, C3)]
    while at < sh["cut"] - 0.02:
        progress = (at - sh["alarm"]) / (sh["cut"] - sh["alarm"])
        level = 0.4 + 0.6 * progress

        low, high = chords[(i // 8) % len(chords)]
        place(music, string_stab(low * 2 if i % 2 else low) * 0.35 * level, at)

        accent = 1.0 if i % 4 == 0 else (0.55 if i % 2 == 0 else 0.35)
        place(music, snare(accent) * 0.35 * level, at)
        if i % 4 == 3:
            place(music, snare(0.3) * 0.25 * level, at + eighth / 2)   # drag

        if i % 8 == 0:
            place(music, taiko(1.0, 52) * 0.7 * level, at)
            place(music, brass([high, high * 1.5], beat * 1.8) * 0.28 * level, at)

        at += eighth
        i += 1

    # Snare roll and a rising cymbal into the cut
    roll = 1.2
    at = sh["cut"] - roll
    while at < sh["cut"] - 0.02:
        lvl = 0.15 + 0.85 * ((at - sh["cut"] + roll) / roll) ** 2
        place(music, snare(lvl) * 0.3, at)
        at += 0.05
    cym = highpass(white(1.5), 5000) * np.linspace(0, 1, n(1.5)) ** 4
    place(music, cym * 0.22, sh["cut"] - 1.5)

    # --- The hit on the cut to black, ringing out
    hit_len = total - sh["cut"]
    boom = sine(30 + 45 * np.exp(-times(hit_len) / 0.2), hit_len) * decay(hit_len, 0.9)
    crack = highpass(white(hit_len), 2000) * decay(hit_len, 0.03)
    chord = brass([D2, A2, D3, F3, A3], hit_len, 2200) * decay(hit_len, 0.6)
    place(music, boom * 1.0 + crack * 0.3 + chord * 0.45, sh["cut"])

    mixed = reverb(music, 2.2, 0.25, 3500)[:n(total)]
    write("mus_afterstage1", fade(mixed, 0.0, 0.3), 0.75)


def main():
    print("Voice:")
    lengths = voice_lines()
    pa_on()

    print("Forest:")
    twilight_ambience()
    collar_buzz()
    shatter()
    kit.birds()
    head_shake()
    rise()
    chuff()
    lion_walk()
    lion_run()
    kit.distant_roar()

    print("Compound:")
    base_ambience()
    searchlight()
    caption_typing()
    alarm()
    gate_roll()
    march()

    print("Music:")
    score()

    print("Voice line lengths (for the subtitles):")
    for name, length in lengths.items():
        print("  %s %.2f s, from %.2f to %.2f" %
              (name, length, SHOTS[name], SHOTS[name] + length))


if __name__ == "__main__":
    main()
