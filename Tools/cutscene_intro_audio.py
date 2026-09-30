"""Sound for Cutscene_Intro: ambience, score, effects and the
finished voiceover.

Everything except the voice is synthesized here (no licensing
issues), and the score is written to the cutscene's shot timing.
If you retime the Timeline, change the times in SHOTS and rerun.

Run from the repo root, after Tools/cutscene_intro_vo.ps1:
    python Tools/cutscene_intro_audio.py

Writes Assets/Cutscenes/Intro/Audio/<name>.wav (44.1 kHz, 16-bit)
and prints each voice line's length for the subtitle timing.
"""

import os
import sys
import wave

import numpy as np
from scipy import signal

SR = 44100
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "Assets", "Cutscenes", "Intro", "Audio")
VO_IN = os.path.join(ROOT, "Temp", "CutsceneVO")
ROAR = os.path.join(ROOT, "Assets", "Audio", "SFX", "roar.wav")

rng = np.random.default_rng(19)

# Shot timing in seconds, matching the Intro Timeline
SHOTS = {
    "roar": 7.8,      # the distant roar, mid aerial shot
    "gate": 9.4,      # cut to the chained gate
    "sky": 13.9,      # cut to the sky, the plane and the parachute
    "chute": 15.3,    # the parachute opens
    "cut": 18.6,      # hard cut to black and silence
    "title": 19.1,    # OPERATION MANEFORD slams in
}


# =========================
# BUILDING BLOCKS
# =========================

def n(seconds):
    return int(round(SR * seconds))


def times(seconds):
    return np.arange(n(seconds)) / SR


def sos(kind, freq, order=2):
    return signal.butter(order, freq, btype=kind, fs=SR, output="sos")


def lowpass(x, freq, order=2):
    return signal.sosfilt(sos("lowpass", freq, order), x, axis=0)


def highpass(x, freq, order=2):
    return signal.sosfilt(sos("highpass", freq, order), x, axis=0)


def bandpass(x, low, high, order=2):
    return signal.sosfilt(sos("bandpass", [low, high], order), x, axis=0)


def sweep_lowpass(x, start, end, block=256):
    """Lowpass whose cutoff glides from start to end Hz."""
    out = np.zeros_like(x)
    zi = None
    count = (len(x) + block - 1) // block

    for i in range(count):
        cutoff = start * (end / start) ** (i / max(count - 1, 1))
        coeffs = sos("lowpass", min(cutoff, SR * 0.45))

        if zi is None:
            zi = signal.sosfilt_zi(coeffs) * 0.0

        chunk = x[i * block:(i + 1) * block]
        out[i * block:(i + 1) * block], zi = signal.sosfilt(
            coeffs, chunk, zi=zi
        )

    return out


def white(seconds):
    return rng.standard_normal(n(seconds))


def brown(seconds):
    x = np.cumsum(rng.standard_normal(n(seconds)))
    return highpass(x, 20)


def phase(freq, seconds):
    f = np.broadcast_to(freq, (n(seconds),))
    return np.cumsum(f) / SR


def sine(freq, seconds):
    return np.sin(2 * np.pi * phase(freq, seconds))


def saw(freq, seconds):
    p = phase(freq, seconds)
    return 2.0 * (p % 1.0) - 1.0


def supersaw(freq, seconds, voices=5, detune=0.012):
    out = np.zeros(n(seconds))

    for i in range(voices):
        spread = (i - (voices - 1) / 2) * detune
        out += np.roll(saw(freq * (1 + spread), seconds), rng.integers(SR))

    return out / voices


def envelope(seconds, attack, release, curve=1.0):
    """Rises over attack, holds, falls over release."""
    t = times(seconds)
    rise = np.clip(t / max(attack, 1e-4), 0, 1)
    fall = np.clip((seconds - t) / max(release, 1e-4), 0, 1)
    return (rise * fall) ** curve


def decay(seconds, length):
    return np.exp(-times(seconds) / length)


def ramp(seconds, start, end):
    return np.linspace(start, end, n(seconds))


def place(track, clip, at):
    """Mixes clip into track starting at 'at' seconds."""
    start = n(at)

    if start >= len(track):
        return

    end = min(len(track), start + len(clip))
    track[start:end] += clip[:end - start]


def pan(mono, position):
    """position -1 (left) .. 1 (right), constant power."""
    position = np.broadcast_to(position, mono.shape)
    angle = (position + 1) * np.pi / 4
    return np.stack([mono * np.cos(angle), mono * np.sin(angle)], axis=1)


def stereo(mono):
    return pan(mono, 0.0)


def reverb(x, length=2.0, mix=0.3, tone=4000):
    """Convolution with decaying noise; a different tail per side."""
    if x.ndim == 1:
        x = stereo(x)

    tail = n(length)
    out = np.zeros((len(x) + tail, 2))

    for ch in range(2):
        ir = rng.standard_normal(tail) * np.exp(-times(length) / (length / 6))
        ir = lowpass(ir, tone)
        ir[:n(0.012)] = 0  # pre-delay
        ir /= np.sqrt(np.sum(ir ** 2))

        wet = signal.fftconvolve(x[:, ch], ir)[:len(out)]
        out[:len(wet), ch] += wet * mix
        out[:len(x), ch] += x[:, ch] * (1 - mix)

    return out


def fade(x, fade_in=0.0, fade_out=0.0):
    x = x.copy()

    if fade_in > 0:
        k = min(n(fade_in), len(x))
        ramp_in = np.linspace(0, 1, k)
        x[:k] *= ramp_in[:, None] if x.ndim == 2 else ramp_in

    if fade_out > 0:
        k = min(n(fade_out), len(x))
        ramp_out = np.linspace(1, 0, k)
        x[-k:] *= ramp_out[:, None] if x.ndim == 2 else ramp_out

    return x


def write(name, data, peak=0.89):
    os.makedirs(OUT, exist_ok=True)

    data = np.asarray(data, dtype=np.float64)
    top = np.max(np.abs(data))

    if top > 0:
        data = data / top * peak

    channels = 1 if data.ndim == 1 else data.shape[1]
    pcm = (np.clip(data, -1, 1) * 32767).astype("<i2")

    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(channels)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())

    print("  %-16s %5.2f s" % (name, len(data) / SR))


def read(path):
    with wave.open(path, "rb") as w:
        rate = w.getframerate()
        channels = w.getnchannels()
        raw = np.frombuffer(w.readframes(w.getnframes()), dtype="<i2")

    x = raw.astype(np.float64) / 32768.0

    if channels > 1:
        x = x.reshape(-1, channels).mean(axis=1)

    if rate != SR:
        x = signal.resample_poly(x, SR, rate)

    return x


# =========================
# VOICE
# =========================

def trim(x, threshold_db=-42, pad=0.03):
    level = np.abs(x)
    loud = np.where(level > 10 ** (threshold_db / 20))[0]

    if len(loud) == 0:
        return x

    start = max(0, loud[0] - n(pad))
    end = min(len(x), loud[-1] + n(pad * 3))
    return x[start:end]


def radio_voice(x):
    """Light radio colour: narrow band, a little grit, faint hiss."""
    x = trim(x)
    x = highpass(x, 260, 4)
    x = lowpass(x, 4600, 4)
    x = np.tanh(x * 2.2) / np.tanh(2.2)

    hiss = bandpass(white(len(x) / SR), 1200, 5200) * 0.012
    x = x + hiss
    return fade(x, 0.005, 0.03)


def voice_lines():
    lengths = {}

    for name in ["pip_01", "pip_02", "pip_03", "pip_04"]:
        path = os.path.join(VO_IN, name + ".wav")

        if not os.path.exists(path):
            sys.exit("Missing " + path +
                     " (run Tools/cutscene_intro_vo.ps1 first)")

        line = radio_voice(read(path))
        write("vo_" + name, line, 0.95)
        lengths[name] = len(line) / SR

    return lengths


def radio_click(closing=False):
    """A squelch: a click and a short burst of static."""
    length = 0.22
    burst = bandpass(white(length), 900, 4200) * decay(length, 0.05)
    click = np.zeros(n(length))
    click[:n(0.004)] = np.linspace(1, -1, n(0.004))

    x = burst * 0.35 + click * 0.6

    if closing:
        x = x[::-1] * 0.8

    return fade(x, 0.0, 0.02)


# =========================
# AMBIENCE
# =========================

def gusts(seconds, speed=1.0):
    t = times(seconds)
    g = (0.55 +
         0.25 * np.sin(2 * np.pi * 0.07 * speed * t + 1.3) +
         0.20 * np.sin(2 * np.pi * 0.19 * speed * t + 0.4))
    return np.clip(g, 0.1, 1.0)


def crickets(seconds, pitch, position):
    out = np.zeros(n(seconds))
    chirp = sine(pitch, 0.016) * np.hanning(n(0.016))
    t = rng.uniform(0.0, 0.5)

    while t < seconds - 0.3:
        for k in range(3):
            place(out, chirp, t + k * 0.034)

        t += rng.uniform(0.6, 1.2)

    return pan(out, position)


def forest_ambience():
    seconds = SHOTS["sky"] + 0.5
    wind = lowpass(brown(seconds), 420) * gusts(seconds)
    wind /= np.max(np.abs(wind))

    leaves = bandpass(white(seconds), 2200, 6500) * gusts(seconds) ** 3

    left = wind * 0.7 + leaves * 0.05
    right = np.roll(wind, n(0.3)) * 0.7 + np.roll(leaves, n(0.17)) * 0.05
    bed = np.stack([left, right], axis=1)

    bed += crickets(seconds, 4500, -0.6) * 0.05
    bed += crickets(seconds, 5150, 0.55) * 0.04

    write("amb_forest", fade(bed, 1.5, 0.4), 0.5)


def sky_ambience():
    start = SHOTS["sky"] - 0.3
    seconds = SHOTS["cut"] - start
    chute = SHOTS["chute"] - start

    roar = bandpass(white(seconds), 160, 1300) * gusts(seconds, 3.0)

    # Freefall rush that builds until the canopy opens
    rush = bandpass(white(seconds), 900, 4200)
    t = times(seconds)
    rush_env = np.clip((t - 0.4) / (chute - 0.4), 0, 1) ** 2
    rush_env[t > chute] = np.exp(-(t[t > chute] - chute) / 0.12)

    # Canopy fabric fluttering after it opens
    flutter = lowpass(white(seconds), 1400)
    flutter *= 0.5 + 0.5 * np.sin(2 * np.pi * 11 * t + 3 * np.sin(t))
    flutter *= np.clip((t - chute) / 0.05, 0, 1) * \
        (0.35 + 0.65 * np.exp(-np.clip(t - chute, 0, None) / 0.8))

    mono = roar * 0.5 + rush * 0.5 * rush_env + flutter * 0.25
    bed = np.stack([mono, np.roll(mono, n(0.21))], axis=1)

    # Hard cut to silence with the picture
    write("amb_sky", fade(bed, 0.3, 0.01), 0.6)


# =========================
# EFFECTS
# =========================

def distant_roar():
    roar = read(ROAR)
    roar = signal.resample_poly(roar, 10, 9)  # a touch deeper
    roar = lowpass(roar, 950, 4)
    roar = np.concatenate([roar, np.zeros(n(0.2))])

    write("sfx_roar_distant", reverb(roar, 3.2, 0.55, 2500), 0.8)
    write("sfx_roar_echo", reverb(lowpass(roar, 700), 4.5, 0.7, 1800), 0.7)


def birds():
    seconds = 2.0
    out = np.zeros(n(seconds))

    # Dozens of wing beats bunched at the start
    for _ in range(70):
        t = abs(rng.normal(0.25, 0.35))
        flap = bandpass(white(0.05), 500, 2600) * np.hanning(n(0.05))
        place(out, flap * rng.uniform(0.3, 1.0), min(t, seconds - 0.1))

    # A few alarmed calls
    for _ in range(6):
        t = rng.uniform(0.05, 1.2)
        length = rng.uniform(0.08, 0.16)
        f = rng.uniform(2400, 3600) * (1 + 0.25 * np.sin(
            2 * np.pi * 28 * times(length)))
        call = sine(f, length) * np.hanning(n(length))
        place(out, call * 0.25, t)

    write("sfx_birds", reverb(out, 1.4, 0.35), 0.7)


def drone_buzz():
    seconds = 3.9
    t = times(seconds)

    # Startled by the roar: the motors stutter and dip
    startle = SHOTS["roar"] + 0.3 - 5.6
    pitch = 190 + 6 * np.sin(2 * np.pi * 0.7 * t)
    wobble = np.exp(-np.clip(t - startle, 0, None) / 0.5) * (t > startle)
    pitch = pitch * (1 - 0.18 * wobble * np.sin(2 * np.pi * 9 * t))

    motor = saw(pitch, seconds) + 0.5 * saw(pitch * 2.01, seconds)
    motor = bandpass(motor, 180, 3000)
    motor *= envelope(seconds, 0.8, 0.6)

    write("sfx_drone", pan(motor, np.linspace(-0.8, 0.7, n(seconds))), 0.5)


def metal_clink(length=0.35, base=None):
    base = base or rng.uniform(1600, 2400)
    partials = [1.0, 2.76, 5.40, 8.93]
    x = np.zeros(n(length))

    for i, ratio in enumerate(partials):
        x += sine(base * ratio, length) * decay(length, 0.08 / (i + 1))

    return x / len(partials)


def chain_rattle():
    seconds = 1.6
    out = np.zeros(n(seconds))
    t = 0.0

    while t < 1.1:
        place(out, metal_clink() * rng.uniform(0.4, 1.0), t)
        t += abs(rng.normal(0.07, 0.04))

    # The padlock knocks against the bars
    place(out, metal_clink(0.6, 700) * 1.4, 0.55)

    write("sfx_chain", reverb(out, 1.2, 0.25), 0.7)


def sign_creak():
    seconds = 1.3
    t = times(seconds)
    f = 95 + 40 * np.sin(2 * np.pi * 0.6 * t) + 8 * rng.standard_normal(len(t))
    creak = saw(lowpass(f, 30), seconds)
    creak *= (0.5 + 0.5 * (np.sin(2 * np.pi * 31 * t) > 0.3))
    creak = bandpass(creak, 300, 2400) * envelope(seconds, 0.2, 0.5)

    write("sfx_creak", reverb(creak, 0.8, 0.2), 0.45)


def sparks():
    seconds = 0.9
    out = np.zeros(n(seconds))

    for _ in range(40):
        t = abs(rng.normal(0.1, 0.18))
        crack = highpass(white(0.008), 2500) * np.hanning(n(0.008))
        place(out, crack * rng.uniform(0.3, 1.0), min(t, seconds - 0.02))

    buzz = saw(120, seconds) * decay(seconds, 0.12)
    out += bandpass(buzz, 200, 2000) * 0.3

    write("sfx_sparks", reverb(out, 0.6, 0.2), 0.6)


def plane_flyby():
    seconds = 4.6
    t = times(seconds)
    closest = 1.6

    # Doppler: pitch drops as it passes
    doppler = 1 + 0.06 * np.tanh((closest - t) * 1.5)
    prop = 72 * doppler

    engine = saw(prop, seconds) + 0.6 * saw(prop * 1.5, seconds)
    engine *= 0.7 + 0.3 * np.sin(2 * np.pi * phase(prop * 0.25, seconds))
    engine = lowpass(engine, 900)

    loudness = 1 / (1 + ((t - closest) / 1.2) ** 2)
    engine *= loudness * envelope(seconds, 0.5, 1.0)

    position = np.tanh((t - closest) * 0.9)
    write("sfx_plane", reverb(pan(engine, position), 2.0, 0.3), 0.7)


def chute_open():
    seconds = 1.2
    snap = highpass(white(seconds), 500) * decay(seconds, 0.04)
    thump = sine(70 * np.exp(-times(seconds) / 0.2), seconds) * \
        decay(seconds, 0.12)
    rustle = bandpass(white(seconds), 600, 3000) * \
        decay(seconds, 0.35) * 0.4

    x = snap * 0.8 + thump * 0.9 + rustle
    write("sfx_chute", reverb(x, 1.0, 0.2), 0.85)


def typing(letters, seconds):
    out = np.zeros(n(seconds + 0.1))
    gap = seconds / letters

    for i in range(letters):
        tick = bandpass(white(0.012), 1800, 7000) * decay(0.012, 0.003)
        place(out, tick * rng.uniform(0.6, 1.0), i * gap)

    return out


def caption_typing():
    # Matches the caption's two lines in the Timeline
    write("sfx_type_1", typing(25, 1.0), 0.35)
    write("sfx_type_2", typing(16, 0.6), 0.35)


def whoosh_in():
    seconds = 0.5
    x = white(seconds)
    x = sweep_lowpass(x, 250, 6000)
    x *= np.linspace(0, 1, n(seconds)) ** 3
    write("sfx_whoosh", stereo(fade(x, 0.0, 0.01)), 0.6)


# =========================
# MUSIC
# =========================

D2, E2, F2, G2, A2, Bb2 = 73.42, 82.41, 87.31, 98.00, 110.0, 116.54
G1, A1, Bb1, Eb2 = 49.00, 55.00, 58.27, 77.78
Cs3, D3, E3, F3, G3 = 138.59, 146.83, 164.81, 174.61, 196.00


def pad(notes, seconds, attack=2.0, release=1.0, tone=900):
    x = np.zeros(n(seconds))

    for f in notes:
        x += supersaw(f, seconds, 5, 0.01)

    x = lowpass(x / len(notes), tone, 2)
    return x * envelope(seconds, attack, release)


def taiko(seconds=1.5, pitch=58):
    body = sine(pitch * (1 + 1.5 * decay(seconds, 0.03)), seconds)
    skin = bandpass(white(seconds), 100, 900) * decay(seconds, 0.05)
    return (body * 0.9 + skin * 0.6) * decay(seconds, 0.35)


def string_stab(f, seconds=0.2):
    x = supersaw(f, seconds, 3, 0.008) + 0.5 * supersaw(f * 2, seconds, 3)
    x = lowpass(x, 1800)
    return x * envelope(seconds, 0.005, 0.12)


def score():
    roar, gate, sky, cut = (SHOTS["roar"], SHOTS["gate"],
                            SHOTS["sky"], SHOTS["cut"])
    total = cut + 0.05
    music = np.zeros(n(total))

    # Dusk: a hushed D minor
    place(music, pad([D2, A2, D3, F3], gate + 0.6, 3.0, 0.8, 700) * 0.55, 0.0)

    # The roar: a low Eb creeps in under it, wrong and uneasy
    swell = pad([Eb2, Bb2], gate - roar + 0.4, 1.4, 0.3, 500)
    place(music, swell * 0.45, roar)

    # Heartbeat after the roar
    for at in (roar + 0.25, roar + 0.7):
        place(music, taiko(1.2, 52) * 0.6, at)

    # The gate: Bb, with a thin high harmonic
    place(music, pad([Bb1, F2, Bb2, D3], sky - gate + 0.5, 0.05, 0.6, 800)
          * 0.55, gate)
    high = sine(880 * (1 + 0.004 * np.sin(2 * np.pi * 5 * times(4.5))), 4.5)
    place(music, high * envelope(4.5, 1.5, 1.0) * 0.05, gate + 0.2)
    place(music, taiko(1.8, 46) * 1.0, gate)

    # The drop: G minor to A, an ostinato that builds to the cut
    turn = sky + 2.3
    place(music, pad([G1, D2, G2, Bb2], turn - sky + 0.3, 0.1, 0.3, 900)
          * 0.6, sky)
    rising = pad([A1, E2, A2, Cs3], cut - turn, 0.2, 0.01, 900)
    rising = sweep_lowpass(rising, 900, 3500) * ramp(cut - turn, 0.6, 1.0)
    place(music, rising * 0.7, turn)
    place(music, taiko(1.8, 46) * 0.9, sky)

    eighth = 60 / 132 / 2
    t = sky
    i = 0

    while t < cut - 0.05:
        if t < turn:
            notes = (G2, D3)
        else:
            notes = (A2, E3)

        level = 0.25 + 0.75 * (t - sky) / (cut - sky)
        place(music, string_stab(notes[i % 2]) * 0.35 * level, t)

        if i % 4 == 0:
            place(music, taiko(0.6, 70) * 0.35 * level, t)

        t += eighth
        i += 1

    # Snare roll and a rising cymbal into the cut
    roll_start = cut - 1.4
    t = roll_start

    while t < cut - 0.03:
        level = 0.1 + 0.9 * ((t - roll_start) / 1.4) ** 2
        hit = bandpass(white(0.05), 1500, 7000) * decay(0.05, 0.02)
        place(music, hit * level * 0.5, t)
        t += 0.045

    cymbal = highpass(white(1.6), 5000) * np.linspace(0, 1, n(1.6)) ** 4
    place(music, cymbal * 0.25, cut - 1.6)

    mixed = reverb(music, 2.4, 0.28, 3500)[:n(total)]

    # Stop dead on the cut to black
    write("mus_intro", fade(mixed, 0.0, 0.015), 0.7)


def title_hit():
    seconds = 5.5
    t = times(seconds)

    boom = sine(32 + 40 * np.exp(-t / 0.25), seconds) * decay(seconds, 1.1)
    crack = highpass(white(seconds), 2000) * decay(seconds, 0.03)

    brass = np.zeros(n(seconds))
    for f in (D3, F3, 220.0, 293.66):
        brass += supersaw(f, seconds, 3, 0.006)
    brass = sweep_lowpass(brass / 4, 4500, 500) * decay(seconds, 1.4)

    metal = np.zeros(n(seconds))
    for f, length in ((523, 2.5), (1187, 1.6), (1789, 1.1), (2803, 0.7)):
        metal += sine(f, seconds) * decay(seconds, length)

    drone = pad([D2 / 2, D2], seconds, 0.2, 2.5, 400)

    x = (boom * 1.0 + crack * 0.35 + brass * 0.55 +
         metal * 0.05 + drone * 0.35)

    write("mus_title_hit", fade(reverb(x, 3.5, 0.35, 3000), 0.0, 1.0), 0.95)


# =========================
# MAIN
# =========================

def main():
    print("Voice:")
    lengths = voice_lines()
    write("sfx_radio_in", radio_click(False), 0.5)
    write("sfx_radio_out", radio_click(True), 0.5)

    print("Ambience:")
    forest_ambience()
    sky_ambience()

    print("Effects:")
    distant_roar()
    birds()
    drone_buzz()
    chain_rattle()
    sign_creak()
    sparks()
    plane_flyby()
    chute_open()
    caption_typing()
    whoosh_in()

    print("Music:")
    score()
    title_hit()

    print("Voice line lengths (for the subtitles):")
    for name, length in lengths.items():
        print("  %s %.2f s" % (name, length))


if __name__ == "__main__":
    main()
