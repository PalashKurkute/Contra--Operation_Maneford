"""Sound for Cutscene_HelicopterEscape: the Mecha-Lion fails and
explodes, the General climbs out of the wreck ("This is not over!
The King will return!"), scurries to his helicopter and lifts off
("After a short break!"), and Rook and Tala take the other one.

Everything except the voice is synthesized (with the earlier
cutscenes' building blocks), and the score is written to the shot
timing. If you retime the Timeline, change the times in SHOTS and
rerun.

Run from the repo root, after Tools/cutscene_heliescape_vo.ps1:
    python Tools/cutscene_heliescape_audio.py

Writes Assets/Cutscenes/HeliEscape/Audio/<name>.wav (44.1 kHz,
16-bit) and prints each voice line's length for the subtitles.
"""

import os
import sys

import numpy as np

import cutscene_intro_audio as kit
from cutscene_intro_audio import (
    n, times, lowpass, highpass, bandpass, white, brown, sine, saw,
    envelope, decay, place, pan, stereo, reverb, fade, write, read, trim,
    gusts, metal_clink, taiko, string_stab, pad, SR,
)
from cutscene_afterstage1_audio import (
    bell, brass, snare, D2, F2, G2, A2, Bb2, A1, Bb1, C2,
    C3, D3, E3, F3, Fs3, G3, A3, B3, D4, E4, Fs4, A4,
)
from cutscene_afterstage2_audio import room_voice, pluck

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "HeliEscape", "Audio")
VO_IN = os.path.join(kit.ROOT, "Temp", "CutsceneVO")

rng = np.random.default_rng(67)

# Shot timing in seconds, matching the HeliEscape Timeline
# (Assets/Cutscenes/Editor/HeliEscapeBuilder.cs)
SHOTS = {
    "spark": 0.4,       # the Mecha-Lion sparks
    "stagger": 0.9,     # it staggers, powering down
    "fall": 1.6,        # it keels over
    "thud": 2.1,        # hits the ground
    "explode": 2.4,     # and explodes
    "wreck": 3.2,       # cut to the burning wreck
    "hatch": 3.6,       # a plate pops off
    "climb": 3.9,       # the General climbs out, coughing
    "gen_01": 4.2,      # "This is not over! The King will return!"
    "run": 8.3,         # cut wide: he scurries to his helicopter
    "board": 9.7,       # in, door slams, rotor spins up
    "lift": 10.3,        # cut: lift-off
    "gen_02": 10.9,     # "After a short break!"
    "away": 12.6,       # off he flies
    "team": 13.2,       # cut: Rook and Tala run for the other one
    "reach": 14.6,      # in they get
    "spin": 14.9,       # its rotor spins up
    "rise": 15.9,       # lift-off, after him
    "cut": 17.6,        # hard cut to black
    "end": 18.4,
}


# =========================
# VOICE
# =========================

def voice_lines():
    lengths = {}

    for name in ("gen_01", "gen_02"):
        path = os.path.join(VO_IN, "he_" + name + ".wav")

        if not os.path.exists(path):
            sys.exit("Missing " + path +
                     " (run Tools/cutscene_heliescape_vo.ps1 first)")

        raw = trim(read(path))
        write("vo_" + name, room_voice(raw, True), 0.95)
        lengths[name] = len(raw) / SR * 100 / 94

    return lengths


# =========================
# THE MECHA-LION GOES DOWN
# =========================

def crackle(seconds, density=40, tone=5000):
    x = np.zeros(n(seconds))
    for _ in range(int(seconds * density)):
        length = rng.uniform(0.004, 0.02)
        place(x, highpass(white(length), 1500) * decay(length, length / 3) * rng.uniform(0.2, 1),
              rng.uniform(0, seconds - 0.02))
    return lowpass(x, tone)


def sparks():
    seconds = SHOTS["explode"] - SHOTS["spark"]
    x = crackle(seconds, 55) * np.linspace(0.4, 1, n(seconds))
    zap = np.sign(sine(120, seconds)) * 0.08 * (rng.random(n(seconds)) < 0.4)
    write("sfx_sparks", reverb(x + bandpass(zap, 200, 3000), 0.8, 0.2), 0.5)


def power_down():
    """Its servos winding down, and a groan of metal as it keels over."""
    seconds = 1.6
    t = times(seconds)
    f = 1100 * np.exp(-t / 0.5) + 70
    whine = (sine(f, seconds) + 0.4 * saw(f / 2, seconds)) * envelope(seconds, 0.05, 0.4)
    groan = bandpass(saw(55 + 8 * np.sin(2 * np.pi * 3 * t), seconds), 80, 900)
    groan *= np.clip((t - 0.6) / 0.3, 0, 1) * envelope(seconds, 0.0, 0.3)
    write("sfx_power_down", reverb(whine * 0.5 + groan * 0.6, 1.4, 0.3), 0.6)


def thud():
    seconds = 1.2
    boom = sine(28 + 50 * np.exp(-times(seconds) / 0.04), seconds) * decay(seconds, 0.3)
    x = boom + lowpass(white(seconds), 700) * decay(seconds, 0.06) * 0.6
    place(x, metal_clink(0.6, 600) * 0.4, 0.03)
    write("sfx_thud", reverb(x, 1.2, 0.2), 0.8)


def explosion():
    seconds = 4.0
    t = times(seconds)
    boom = sine(26 + 70 * np.exp(-t / 0.08), seconds) * decay(seconds, 0.9)
    blast = lowpass(white(seconds), 1200) * decay(seconds, 0.35)
    crack = highpass(white(0.1), 2500) * decay(0.1, 0.02)
    rumble = lowpass(brown(seconds), 120)
    rumble = rumble / np.max(np.abs(rumble)) * decay(seconds, 1.4)
    x = boom * 1.1 + blast * 0.8 + rumble * 0.5
    place(x, crack * 0.8, 0.0)
    place(x, crackle(2.5, 30, 3000) * 0.4, 0.4)
    for _ in range(14):
        place(x, metal_clink(0.3, rng.uniform(800, 2400)) * rng.uniform(0.1, 0.3), rng.uniform(0.4, 2.0))
    write("sfx_explosion", reverb(stereo(x), 2.5, 0.35, 2500), 0.95)


def fire():
    """The wreck burning, with the night wind."""
    seconds = SHOTS["end"] - SHOTS["explode"]
    roar = lowpass(brown(seconds), 280)
    roar = roar / np.max(np.abs(roar)) * (0.7 + 0.3 * np.sin(2 * np.pi * 0.4 * times(seconds)))
    x = stereo(roar * 0.4 + crackle(seconds, 25, 4000) * 0.5)
    wind = gusts(seconds, 0.7)
    x = x + (stereo(wind) if wind.ndim == 1 else wind) * 0.4
    write("amb_fire", fade(x, 0.2, 0.5), 0.5)


def hatch():
    """A plate pops off with a clank and a hiss; he coughs his way out."""
    seconds = 1.6
    x = np.zeros(n(seconds))
    place(x, metal_clink(0.8, 520) * 1.0, 0.0)
    place(x, highpass(white(0.5), 2500) * envelope(0.5, 0.01, 0.4) * 0.3, 0.02)
    cough = bandpass(white(0.15), 400, 1800) * envelope(0.15, 0.01, 0.09)
    for at in (0.35, 0.52, 0.85):
        place(x, cough * 0.6, at)
    write("sfx_hatch", reverb(x, 0.9, 0.2), 0.6)


# =========================
# THE HELICOPTERS
# =========================

def scurry():
    seconds = SHOTS["board"] - SHOTS["run"] + 0.1
    x = np.zeros(n(seconds))
    at = 0.05
    while at < seconds - 0.1:
        step = sine(70 + 60 * np.exp(-times(0.08) / 0.02), 0.08) * decay(0.08, 0.02)
        step += bandpass(white(0.08), 1200, 5000) * decay(0.08, 0.01) * 0.5
        place(x, step * rng.uniform(0.6, 1), at)
        at += 0.14
    place(x, metal_clink(0.15, 3000)[:n(0.15)] * 0.15, 0.3)    # medals jingling
    write("sfx_scurry", reverb(x, 0.6, 0.15), 0.5)


def door_slam():
    seconds = 0.8
    x = sine(60, seconds) * decay(seconds, 0.08) + lowpass(white(seconds), 1500) * decay(seconds, 0.03)
    place(x, metal_clink(0.5, 1100) * 0.4, 0.01)
    write("sfx_door_slam", reverb(x, 0.8, 0.2), 0.7)


def rotor(name, seconds, spin_up, away=None):
    """Blade chop (thumps) and turbine whine. It spins up over
    spin_up seconds; with 'away' it flies off from that time."""
    t = times(seconds)
    speed = np.clip(t / spin_up, 0, 1) ** 0.7
    rate = 3 + 15 * speed
    phase = np.cumsum(rate) / SR
    chop = np.maximum(0, np.sin(2 * np.pi * phase)) ** 6
    chop = lowpass(white(seconds), 600) * chop * 2 + sine(55, seconds) * chop
    whine = (sine(900 + 900 * speed, seconds) * 0.15 + bandpass(white(seconds), 2000, 6000) * 0.05) * speed
    x = chop * (0.4 + 0.6 * speed) + whine
    if away is not None:
        level = np.clip(1 - (t - away) / (seconds - away), 0.05, 1)
        x = x * level ** 1.5
        x = lowpass(x, 3000) * (t >= away) + x * (t < away)
        out = pan(x, np.clip((t - away) / 2, 0, 0.9))
    else:
        out = stereo(x)
    write(name, fade(reverb(out, 1.2, 0.2), 0.0, 0.2), 0.75)


# =========================
# MUSIC
# =========================

def score():
    sh = SHOTS
    total = sh["end"]
    music = np.zeros(n(total))

    # --- The machine failing: a dissonant bed, heartbeats speeding up
    place(music, pad([D2, Bb2 / 2 * 2, E3], sh["explode"], 0.3, 0.05, 600) * 0.3, 0.0)
    for at in (0.3, 0.95, 1.45, 1.8, 2.05, 2.25):
        place(music, taiko(0.6, 50) * 0.45, at)
    riser = highpass(white(1.2), 3000) * np.linspace(0, 1, n(1.2)) ** 3
    place(music, riser * 0.15, sh["explode"] - 1.2)

    # After the blast: a low, ringing chord
    place(music, pad([D2 / 2, A1, D2], sh["climb"] - sh["explode"] + 0.4, 0.05, 0.6, 400) * 0.35,
          sh["explode"])

    # --- He pops up: a bonk; then a pompous royal motif under the rant
    place(music, pluck(D2, 0.6) * 0.4, sh["climb"])
    place(music, pluck(A2, 0.6) * 0.35, sh["climb"] + 0.18)
    place(music, pad([D2, A2, F3], sh["run"] - sh["gen_01"], 0.4, 0.2, 700) * 0.3, sh["gen_01"])
    for at, notes in ((sh["gen_01"] + 0.65, [D3, D2]), (sh["gen_01"] + 2.7, [Bb2, Bb1]),
                      (sh["gen_01"] + 3.1, [A2, A1])):
        place(music, brass(notes, 0.4) * 0.35, at)
        place(music, taiko(0.9, 48) * 0.5, at)

    # --- The scurry: quick staccato plucks running down
    run = [D4, C3 * 4 / 2 * 1.0, B3, A3, G3, Fs3, E3, D3]
    at, i = sh["run"], 0
    while at < sh["board"]:
        place(music, pluck(run[i % len(run)], 0.18) * 0.22, at)
        if i % 2 == 0:
            place(music, pluck(D2 if (i // 8) % 2 == 0 else A1, 0.3) * 0.25, at)
        at += 0.14
        i += 1

    # --- Lift-off: a pompous fanfare; a cheeky "ta-dum" after his line
    for k, (at, f) in enumerate(((0.0, D3), (0.3, F3), (0.6, A3), (1.0, D4))):
        place(music, brass([f, f / 2], 0.5 if k < 3 else 1.2) * 0.3, sh["lift"] + at)
    place(music, taiko(1.2, 46) * 0.6, sh["lift"])
    place(music, pluck(A2, 0.4) * 0.4, sh["away"] - 0.05)
    place(music, pluck(D2, 0.9) * 0.45, sh["away"] + 0.2)

    # --- Rook and Tala: the heroes' theme in D major, building to the cut
    eighth = 0.25
    at, i = sh["team"], 0
    chords = [D2, D2, G2, A2]
    while at < sh["cut"] - 0.02:
        progress = (at - sh["team"]) / (sh["cut"] - sh["team"])
        level = 0.45 + 0.55 * progress
        low = chords[(i // 8) % len(chords)]
        place(music, string_stab(low * 2 if i % 2 else low) * 0.3 * level, at)
        place(music, snare(1.0 if i % 4 == 2 else 0.35) * 0.25 * level, at)
        if i % 8 == 0:
            place(music, taiko(1.0, 52) * 0.6 * level, at)
        at += eighth
        i += 1
    for at, f, length in ((0.0, D4, 0.9), (1.0, Fs4, 0.5), (1.5, E4, 0.5), (2.0, A4, 1.4),
                          (3.5, Fs4, 0.4), (3.9, A4, 0.5)):
        place(music, brass([f / 2, f / 4], length + 0.1, 1800) * 0.3, sh["team"] + at)
        place(music, bell(f) * 0.12, sh["team"] + at)

    roll_at = sh["cut"] - 0.9
    while roll_at < sh["cut"] - 0.02:
        place(music, snare(0.2 + 0.8 * ((roll_at - sh["cut"] + 0.9) / 0.9) ** 2) * 0.3, roll_at)
        roll_at += 0.05

    # --- The cut: a big D major hit, ringing out
    hit_len = total - sh["cut"]
    boom = sine(30 + 45 * np.exp(-times(hit_len) / 0.2), hit_len) * decay(hit_len, 0.9)
    chord = brass([D2, A2, D3, Fs3, A3], hit_len, 2200) * decay(hit_len, 0.6)
    place(music, boom + chord * 0.45, sh["cut"])

    mixed = reverb(music, 2.0, 0.22, 3500)[:n(total)]
    write("mus_heliescape", fade(mixed, 0.0, 0.3), 0.75)


def main():
    print("Voice:")
    lengths = voice_lines()

    print("The Mecha-Lion:")
    sparks()
    power_down()
    thud()
    explosion()
    fire()
    hatch()

    print("The helicopters:")
    scurry()
    door_slam()
    rotor("sfx_rotor_general", SHOTS["team"] - SHOTS["board"], 0.9,
          away=SHOTS["away"] - SHOTS["board"])
    rotor("sfx_rotor_team", SHOTS["end"] - SHOTS["spin"], 1.0)

    print("Music:")
    score()

    print("Voice line lengths (for the subtitles):")
    for name, length in lengths.items():
        print("  %s %.2f s, from %.2f to %.2f" %
              (name, length, SHOTS[name], SHOTS[name] + length))


if __name__ == "__main__":
    main()
