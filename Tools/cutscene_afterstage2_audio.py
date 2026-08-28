"""Sound for Cutscene_AfterStage2: tea time in the General's control
room, the twelfth soldier going down on the monitors, the spit-take,
the big red button, the alarm, Dobbs and the fuel gauge, and the
Mecha-Lion waking in its hangar.

Everything except the voices is synthesized (with the intro's and
AfterStage1's building blocks), and the score is written to the
cutscene's shot timing. If you retime the Timeline, change the times
in SHOTS and rerun.

Run from the repo root, after Tools/cutscene_afterstage2_vo.ps1:
    python Tools/cutscene_afterstage2_audio.py

Writes Assets/Cutscenes/AfterStage2/Audio/<name>.wav (44.1 kHz,
16-bit) and prints each voice line's length for the subtitles.
"""

import os
import sys

import numpy as np
from scipy import signal

import cutscene_intro_audio as kit
from cutscene_intro_audio import (
    n, times, lowpass, highpass, bandpass, sweep_lowpass, white, brown,
    sine, saw, envelope, decay, place, pan, stereo, reverb, fade,
    write, read, trim, gusts, metal_clink, taiko, string_stab, pad, typing, SR,
)
from cutscene_afterstage1_audio import (
    bell, brass, snare, D2, E2, F2, G2, A2, Bb2, G1, A1, Bb1, C2,
    C3, D3, E3, F3, Fs3, G3, A3, B3, D4, E4, Fs4,
)

kit.OUT = os.path.join(kit.ROOT, "Assets", "Cutscenes", "AfterStage2", "Audio")
VO_IN = os.path.join(kit.ROOT, "Temp", "CutsceneVO")
ROAR = os.path.join(kit.ROOT, "Assets", "Audio", "SFX", "roar.wav")

rng = np.random.default_rng(53)

# Shot timing in seconds, matching the AfterStage2 Timeline
# (Assets/Cutscenes/Editor/AfterStage2Builder.cs)
SHOTS = {
    "caption": 0.9,     # the place caption types on
    "sip": 1.5,         # the cup goes up; a long slurp
    "monitor": 5.0,     # cut to the big screen
    "fire": 5.4,        # on screen: Rook fires
    "hit": 5.65,        #   the twelfth soldier is hit
    "down": 6.0,        #   and goes down
    "units_down": 6.4,  # ALL UNITS DOWN
    "tea": 7.0,         # cut to the General, mid-sip
    "spit": 7.5,        # the spit-take
    "order": 9.0,       # cut: he points
    "gen_01": 9.4,      # "Wake the Mecha-Lion."
    "button": 11.4,     # cut to the button
    "cover": 11.7,      # the glass flips up
    "slam": 12.5,       # his fist comes down
    "alarm": 13.3,      # cut to the room, red light, klaxon
    "dobbs_01": 13.8,   # "Sir, about the fuel gauge..."
    "not_now": 15.9,    # cut to the General
    "gen_02": 16.0,     # "Not now, Dobbs!"
    "hangar": 18.0,     # cut to the hangar at night
    "door": 18.4,       # the door rolls up
    "eyes": 21.3,       # two red eyes in the dark
    "glow": 22.0,       # the head lights up, servos
    "roar": 22.6,       # the roar
    "cut": 24.1,        # hard cut to black
    "end": 24.9,
}

CAPTION = [("ROYAL CONTROL ROOM", 1.0), ("DAY 9  -  23:58", 0.8)]


# =========================
# VOICES, IN THE ROOM
# =========================

def room_voice(x, deeper):
    x = trim(x)
    if deeper:
        x = signal.resample_poly(x, 100, 94)
    x = highpass(x, 120, 2)
    x = lowpass(x, 7000, 2)
    return fade(reverb(x, 0.9, 0.16, 5000), 0.005, 0.2)


def voice_lines():
    lengths = {}

    for name, deeper in (("gen_01", True), ("dobbs_01", False), ("gen_02", True)):
        path = os.path.join(VO_IN, "s2_" + name + ".wav")

        if not os.path.exists(path):
            sys.exit("Missing " + path +
                     " (run Tools/cutscene_afterstage2_vo.ps1 first)")

        raw = trim(read(path))
        write("vo_" + name, room_voice(raw, deeper), 0.95)
        lengths[name] = len(raw) / SR * (100 / 94 if deeper else 1)

    return lengths


# =========================
# THE CONTROL ROOM
# =========================

def blip(f, length=0.07):
    return sine(f, length) * envelope(length, 0.003, 0.03)


def room_ambience():
    seconds = SHOTS["hangar"] + 0.05
    hum = lowpass(brown(seconds), 160)
    hum /= np.max(np.abs(hum))
    hum = hum * 0.5 + (sine(50, seconds) * 0.06 + sine(100, seconds) * 0.04)
    whine = sine(7800, seconds) * 0.006

    beeps = np.zeros(n(seconds))
    at = 0.4
    while at < seconds - 0.3:
        place(beeps, blip(rng.choice([1180, 1560, 2100, 2350])) * 0.12, at)
        at += rng.uniform(0.6, 1.6)

    x = reverb(hum + whine + beeps, 0.8, 0.2, 4000)[:n(seconds)]
    write("amb_room", fade(x, 0.4, 0.05), 0.4)


def caption_typing():
    for i, (text, seconds) in enumerate(CAPTION):
        write("sfx_type_%d" % (i + 1), typing(len(text), seconds), 0.35)


def sip():
    """A long, noble slurp."""
    seconds = 1.0
    t = times(seconds)
    x = bandpass(white(seconds), 900, 3200) * (0.6 + 0.4 * np.sin(2 * np.pi * 23 * t))
    x *= envelope(seconds, 0.12, 0.35) * (0.6 + 0.4 * np.sin(2 * np.pi * 1.7 * t))
    x += bandpass(white(seconds), 300, 700) * decay(seconds, 0.3) * 0.3
    place(x, blip(260, 0.12) * 0.4, 0.88)               # the swallow
    write("sfx_sip", reverb(x, 0.6, 0.12), 0.45)


def static_burst():
    seconds = 0.3
    x = bandpass(white(seconds), 1500, 8000) * envelope(seconds, 0.005, 0.12)
    x += sine(15700, seconds) * 0.1 * envelope(seconds, 0.005, 0.1)
    write("sfx_static", stereo(x), 0.4)


def cctv():
    """Through the monitor's tiny speaker: a shot, the hit, a body down."""
    seconds = 1.2
    x = np.zeros(n(seconds))
    shot = white(0.15) * decay(0.15, 0.03) + sine(140, 0.15) * decay(0.15, 0.04)
    place(x, shot, 0.0)
    place(x, white(0.1) * decay(0.1, 0.02) * 0.7, SHOTS["hit"] - SHOTS["fire"])
    thud = sine(90, 0.25) * decay(0.25, 0.06) + white(0.25) * decay(0.25, 0.04) * 0.4
    place(x, thud * 0.8, SHOTS["down"] - SHOTS["fire"])
    x = bandpass(x, 700, 4500, 4)
    x = np.tanh(x * 2.5)
    write("sfx_cctv", reverb(x, 0.4, 0.15, 3000), 0.5)


def spit():
    """Pfffft, a splash of tea and a cough."""
    seconds = 1.1
    t = times(seconds)
    spray = highpass(white(seconds), 1800) * envelope(seconds, 0.01, 0.4, 2.0)
    spray *= (0.7 + 0.3 * np.sin(2 * np.pi * 31 * t)) * (t < 0.55)
    lips = saw(95, 0.18) * decay(0.18, 0.06) * 0.5
    splash = np.zeros(n(seconds))
    for _ in range(26):
        drop = lowpass(white(0.03), 2500) * decay(0.03, 0.008)
        place(splash, drop * rng.uniform(0.2, 0.6), rng.uniform(0.25, 0.75))
    cough = bandpass(white(0.14), 400, 1800) * envelope(0.14, 0.01, 0.08)
    x = spray * 0.8 + splash
    place(x, lips, 0.0)
    place(x, cough * 0.7, 0.78)
    place(x, cough * 0.5, 0.93)
    write("sfx_spit", reverb(x, 0.7, 0.15), 0.7)


def cup():
    """The cup clattering into its saucer."""
    x = np.zeros(n(0.9))
    for at, base in ((0.0, 3100), (0.09, 3600), (0.16, 2900), (0.22, 3300), (0.27, 3500)):
        place(x, metal_clink(0.35, base) * 0.6, at)
    write("sfx_cup", reverb(x, 0.7, 0.2), 0.55)


def cover():
    """The glass flap flips up: a latch click and a creak."""
    seconds = 0.5
    t = times(seconds)
    x = metal_clink(0.2, 2600)[:n(seconds)]
    x = np.pad(x, (0, n(seconds) - len(x)))
    creak = saw(380 + 260 * t / seconds, seconds) * envelope(seconds, 0.05, 0.15) * 0.15
    x += bandpass(creak, 400, 2400)
    place(x, metal_clink(0.25, 1900) * 0.6, 0.32)
    write("sfx_cover", reverb(x, 0.5, 0.15), 0.6)


def slam():
    """A fist on a very big button."""
    seconds = 1.6
    boom = sine(32 + 60 * np.exp(-times(seconds) / 0.05), seconds) * decay(seconds, 0.35)
    knock = lowpass(white(seconds), 900) * decay(seconds, 0.04)
    click = highpass(white(0.02), 3000) * decay(0.02, 0.004)
    x = boom + knock * 0.7
    place(x, click * 0.6, 0.012)
    place(x, metal_clink(0.5, 1300) * 0.3, 0.05)          # the console rattles
    write("sfx_slam", reverb(x, 1.2, 0.2, 2500), 0.9)


def klaxon(seconds, gap=1.0):
    """A-OOO-GA: rises, holds, drops."""
    out = np.zeros(n(seconds))
    at = 0.0
    while at < seconds:
        length = 0.75
        t = times(length) / length
        f = 260 + 300 * np.clip(t * 3, 0, 1) - 120 * np.clip((t - 0.75) * 4, 0, 1)
        honk = saw(f, length) * 0.6 + np.sign(sine(f, length)) * 0.3
        place(out, bandpass(honk, 250, 2600) * envelope(length, 0.03, 0.12), at)
        at += gap
    return out


def alarm():
    seconds = SHOTS["hangar"] - SHOTS["slam"] - 0.3
    x = reverb(klaxon(seconds), 1.6, 0.35, 3000)[:n(seconds)]
    write("sfx_alarm", fade(x, 0.0, 0.05), 0.7)


# =========================
# THE HANGAR
# =========================

def hangar_ambience():
    seconds = SHOTS["end"] - SHOTS["hangar"]
    wind = gusts(seconds, 0.8)
    far = lowpass(klaxon(seconds, 1.15), 900) * 0.35
    far = reverb(far, 3.0, 0.6, 1500)[:n(seconds)]
    x = (stereo(wind) if wind.ndim == 1 else wind) * 0.6 + far
    write("amb_hangar", fade(x, 0.05, 0.6), 0.5)


def door():
    """The big roll-up door: motor, chain, and a clank at the top."""
    seconds = 3.6
    t = times(seconds)
    run = np.clip(t / 0.15, 0, 1) * np.clip((2.6 - t) / 0.15, 0, 1)
    motor = lowpass(saw(48 + 4 * np.sin(2 * np.pi * 0.5 * t), seconds), 400) * run
    rattle = np.zeros(n(seconds))
    at = 0.05
    while at < 2.6:
        place(rattle, metal_clink(0.1, rng.uniform(700, 1400)) * rng.uniform(0.2, 0.5), at)
        at += abs(rng.normal(0.05, 0.015))
    x = motor * 0.7 + rattle * 0.5 + lowpass(brown(seconds), 120) * run * 0.6
    place(x, metal_clink(1.0, 420) * 1.4, 2.6)
    hiss = highpass(white(0.8), 2500) * envelope(0.8, 0.02, 0.5) * 0.3
    place(x, hiss, 2.65)
    write("sfx_door", reverb(stereo(x), 2.2, 0.35, 2500), 0.85)


def eyes():
    """A relay thunk, then a power-up whine."""
    seconds = 1.4
    t = times(seconds)
    f = 180 + 1300 * (t / seconds) ** 1.6
    whine = (sine(f, seconds) + 0.3 * sine(f * 2, seconds) + 0.15 * saw(f / 2, seconds))
    whine *= envelope(seconds, 0.3, 0.3)
    buzz = bandpass(np.sign(sine(100, seconds)), 200, 2000) * 0.15 * np.clip(t / 0.4, 0, 1)
    x = whine * 0.5 + buzz
    place(x, sine(70, 0.2) * decay(0.2, 0.04) + white(0.2) * decay(0.2, 0.01) * 0.4, 0.0)
    write("sfx_eyes", reverb(x, 1.8, 0.35, 3500), 0.6)


def servos():
    seconds = 0.6
    t = times(seconds)
    f = 520 + 180 * np.sin(2 * np.pi * 1.2 * t)
    x = bandpass(saw(f, seconds), 400, 3000) * envelope(seconds, 0.05, 0.15)
    write("sfx_servos", reverb(x, 1.2, 0.3), 0.4)


def mecha_roar():
    """The lions' roar, pitched down and bolted into metal."""
    x = trim(read(ROAR))
    x = signal.resample_poly(x, 10, 7)               # deeper, longer
    t = np.arange(len(x)) / SR
    ring = x * sine(55, len(x) / SR)
    x = x * 0.7 + ring * 0.5
    x = np.tanh(x * 3) / np.tanh(3)
    grind = bandpass(saw(42, len(x) / SR), 60, 900) * np.abs(x) * 0.6
    x = (x + grind) * np.clip((len(x) / SR - t) / 0.4, 0, 1)
    write("sfx_roar", reverb(x, 2.8, 0.4, 2500), 0.95)


# =========================
# MUSIC
# =========================

def pluck(f, length=0.45):
    """A harpsichord-ish pluck for the tea-time minuet."""
    x = saw(f, length) * 0.5 + sine(f * 2, length) * 0.3
    return lowpass(x, 3200) * decay(length, 0.12) * np.clip(times(length) / 0.002, 0, 1)


def score():
    sh = SHOTS
    total = sh["end"]
    music = np.zeros(n(total))

    # --- Tea time: a pompous little minuet in G, 3/4, until the spit
    beat = 0.42
    melody = [G3 * 2, B3 * 2, D4 * 2, B3 * 2, A3 * 2, G3 * 2, Fs3 * 2, A3 * 2, D4,
              G3 * 2, B3 * 2, D4 * 2, E4 * 2, D4 * 2, B3 * 2, A3 * 2, G3 * 2, D4]
    bass = [G2, D3, D3, D2, A2, A2, G2, D3, D3, D2, A2, A2]
    at = 0.3
    for i in range(int((sh["spit"] - at) / beat)):
        place(music, pluck(melody[i % len(melody)]) * 0.22, at)
        if i % 3 == 0:
            place(music, pluck(bass[(i // 3) % len(bass)] / 2, 0.8) * 0.3, at)
        else:
            place(music, pluck(bass[(i // 3) % len(bass)], 0.3) * 0.12, at)
        at += beat

    # The record scratches to a stop on the spit
    scratch_len = 0.35
    f = 900 * np.exp(-times(scratch_len) / 0.08) + 80
    scratch = bandpass(white(scratch_len), 600, 3000) * 0.5 + saw(f, scratch_len) * 0.3
    place(music, scratch * envelope(scratch_len, 0.005, 0.1), sh["spit"] - 0.04)

    # --- Something dark: a low D minor bed, the order, a brass "dun-dun"
    place(music, pad([D2, A2, F3], sh["button"] - sh["spit"] - 0.4, 1.0, 0.3, 500) * 0.4,
          sh["spit"] + 0.4)
    place(music, taiko(1.6, 46) * 0.8, sh["order"])
    place(music, brass([D2, A2, D3, F3], 2.0, 900) * 0.3, sh["order"])
    for at_, f_ in ((sh["gen_01"] + 1.65, D2), (sh["gen_01"] + 1.95, Bb1)):
        place(music, brass([f_, f_ * 2], 0.35) * 0.45, at_)
        place(music, taiko(1.0, 50) * 0.5, at_)

    # --- The button: a tremolo riser and heartbeats, a gap, the hit
    rise_len = sh["slam"] - sh["button"] - 0.12
    t = times(rise_len)
    trem = (sine(A3 * 2, rise_len) + sine(Bb2 * 4, rise_len) * 0.6) * \
        (0.6 + 0.4 * np.sin(2 * np.pi * 12 * t)) * (t / rise_len) ** 2
    place(music, trem * 0.08, sh["button"])
    for at_ in (sh["button"] + 0.15, sh["button"] + 0.55, sh["slam"] - 0.4):
        place(music, taiko(0.6, 52) * 0.4, at_)

    hit = 0.9
    place(music, taiko(2.2, 40) * 1.0, sh["slam"])
    place(music, brass([D2, A2, D3, F3, A3], hit, 2000) * decay(hit, 0.4) * 0.5, sh["slam"])

    # --- The alarm: string ostinato and snare, kept under the voices
    eighth = 0.25
    at, i = sh["alarm"], 0
    chords = [D2, D2, Bb1, C2]
    while at < sh["hangar"] - 0.05:
        low = chords[(i // 8) % len(chords)]
        place(music, string_stab(low * 2 if i % 2 else low) * 0.22, at)
        place(music, snare(1.0 if i % 4 == 0 else 0.4) * 0.16, at)
        at += eighth
        i += 1

    # --- The hangar: a deep drone, heartbeats, the door's rising swell
    hangar_len = sh["cut"] - sh["hangar"]
    place(music, pad([D2 / 2, A1, D2], hangar_len, 0.8, 0.1, 300) * 0.5, sh["hangar"])
    for k, at_ in enumerate(np.arange(sh["door"], sh["eyes"] - 0.2, 0.75)):
        place(music, taiko(0.8, 48) * (0.35 + 0.1 * k), at_)
    swell_len = sh["eyes"] - sh["door"]
    swell = brass([D2, Bb2 / 2 * 2, E3], swell_len, 700) * np.linspace(0, 1, n(swell_len)) ** 2
    place(music, swell * 0.3, sh["door"])

    # The eyes: a high, cold bell; the glow: a dissonant swell
    place(music, bell(D4 * 2, 2.5) * 0.25, sh["eyes"])
    place(music, bell(D4 * 2 * 1.06, 2.5) * 0.15, sh["eyes"] + 0.02)
    glow_len = sh["roar"] - sh["glow"]
    place(music, brass([D2, Bb2 / 2 * 2, D3, E3 / 2 * 2], glow_len + 0.1, 1400)
          * np.linspace(0.2, 1, n(glow_len + 0.1)) * 0.35, sh["glow"])

    # The roar: everything in, a snare roll into the cut
    roar_len = sh["cut"] - sh["roar"]
    place(music, taiko(2.0, 38) * 1.0, sh["roar"])
    place(music, brass([D2, A2, D3, F3, A3], roar_len, 2200) * 0.45, sh["roar"])
    at = sh["cut"] - 0.9
    while at < sh["cut"] - 0.02:
        lvl = 0.2 + 0.8 * ((at - sh["cut"] + 0.9) / 0.9) ** 2
        place(music, snare(lvl) * 0.3, at)
        at += 0.05

    # --- The cut to black, ringing out
    hit_len = total - sh["cut"]
    boom = sine(30 + 45 * np.exp(-times(hit_len) / 0.2), hit_len) * decay(hit_len, 0.9)
    chord = brass([D2, A2, D3, F3], hit_len, 1800) * decay(hit_len, 0.5)
    place(music, boom + chord * 0.4, sh["cut"])

    mixed = reverb(music, 2.0, 0.22, 3500)[:n(total)]
    write("mus_afterstage2", fade(mixed, 0.0, 0.3), 0.75)


def main():
    print("Voice:")
    lengths = voice_lines()

    print("Control room:")
    room_ambience()
    caption_typing()
    sip()
    static_burst()
    cctv()
    spit()
    cup()
    cover()
    slam()
    alarm()

    print("Hangar:")
    hangar_ambience()
    door()
    eyes()
    servos()
    mecha_roar()

    print("Music:")
    score()

    print("Voice line lengths (for the subtitles):")
    for name, length in lengths.items():
        print("  %s %.2f s, from %.2f to %.2f" %
              (name, length, SHOTS[name], SHOTS[name] + length))


if __name__ == "__main__":
    main()
