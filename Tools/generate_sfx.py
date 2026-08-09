"""Generates the game's retro (NES-style) sound effects as WAV files.

Every sound is synthesized from scratch (square/triangle waves, NES-style
noise, filters), so there are no licensing issues and any sound can be
tweaked here and regenerated.

Run from the repo root:
    python Tools/generate_sfx.py

Writes Assets/Audio/SFX/<name>.wav (44.1 kHz, 16-bit mono). Volumes and
pitch variation are set in Unity, in Assets/Resources/SoundLibrary.asset.
"""

import os
import sys
import wave

import numpy as np
from scipy import signal

SR = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "Assets", "Audio", "SFX")

rng = np.random.default_rng(7)


# =========================
# BUILDING BLOCKS
# =========================

def samples(seconds):
    return int(round(SR * seconds))


def times(seconds):
    return np.arange(samples(seconds)) / SR


def per_sample(value, n):
    """A number becomes a constant array; an array is trimmed/padded to n."""
    if np.isscalar(value):
        return np.full(n, float(value))
    value = np.asarray(value, dtype=float)
    if len(value) >= n:
        return value[:n]
    return np.concatenate([value, np.full(n - len(value), value[-1])])


def tone(freq, seconds, wave="square", duty=0.5):
    """Oscillator. freq can be a number or one value per sample."""
    n = samples(seconds)
    phase = np.cumsum(per_sample(freq, n)) / SR
    p = phase % 1.0

    if wave == "square":
        return np.where(p < duty, 1.0, -1.0)
    if wave == "triangle":
        # NES triangle: 16 volume steps
        return np.round((1.0 - 4.0 * np.abs(p - 0.5)) * 7.5) / 7.5
    if wave == "saw":
        return 2.0 * p - 1.0
    return np.sin(2.0 * np.pi * phase)


def slide(f0, f1, seconds, exponential=True):
    """Pitch glide from f0 to f1."""
    x = np.linspace(0.0, 1.0, samples(seconds))
    if exponential:
        return f0 * (f1 / f0) ** x
    return f0 + (f1 - f0) * x


def white(seconds):
    return rng.uniform(-1.0, 1.0, samples(seconds))


def nes_noise(seconds, rate, short_mode=False):
    """NES-style noise: a 15-bit shift register clocked at `rate` Hz.
    Lower rates sound crunchier; short_mode sounds metallic."""
    n = samples(seconds)
    rate = per_sample(rate, n) / SR
    out = np.empty(n)
    register = 1
    tap = 6 if short_mode else 1
    level = 1.0
    clock = 0.0

    for i in range(n):
        clock += rate[i]
        while clock >= 1.0:
            clock -= 1.0
            feedback = (register & 1) ^ ((register >> tap) & 1)
            register = (register >> 1) | (feedback << 14)
            level = 1.0 if register & 1 else -1.0
        out[i] = level

    return out


def decay(seconds, tau, attack=0.002):
    """Exponential decay envelope with a short attack (no clicks)."""
    t = times(seconds)
    env = np.exp(-t / tau)
    if attack > 0:
        env *= np.clip(t / attack, 0.0, 1.0)
    return env


def smooth_random(seconds, rate_hz, seed_offset=0):
    """Slowly wandering random curve between -1 and 1."""
    n = samples(seconds)
    points = max(2, int(seconds * rate_hz) + 2)
    local = np.random.default_rng(100 + seed_offset)
    knots = local.uniform(-1.0, 1.0, points)
    x = np.linspace(0, points - 1, n)
    return np.interp(x, np.arange(points), knots)


def lowpass(x, cutoff, order=2):
    b, a = signal.butter(order, cutoff / (SR / 2), "low")
    return signal.lfilter(b, a, x)


def highpass(x, cutoff, order=2):
    b, a = signal.butter(order, cutoff / (SR / 2), "high")
    return signal.lfilter(b, a, x)


def bandpass(x, low, high, order=2):
    b, a = signal.butter(order, [low / (SR / 2), high / (SR / 2)], "band")
    return signal.lfilter(b, a, x)


def sweep_lowpass(x, f0, f1, passes=2):
    """Low-pass whose cutoff glides from f0 to f1 (one-pole, run twice)."""
    cutoff = f0 * (f1 / f0) ** np.linspace(0.0, 1.0, len(x))
    alpha = 1.0 - np.exp(-2.0 * np.pi * cutoff / SR)

    for _ in range(passes):
        y = np.empty(len(x))
        acc = 0.0
        for i in range(len(x)):
            acc += alpha[i] * (x[i] - acc)
            y[i] = acc
        x = y

    return x


def crush(x, bits):
    """Fewer amplitude steps: a grittier 8-bit sound."""
    steps = 2 ** (bits - 1)
    return np.round(x * steps) / steps


def mix(*parts):
    """parts: (sound, start_seconds, gain)."""
    end = max(samples(start) + len(sound) for sound, start, _ in parts)
    out = np.zeros(end)
    for sound, start, gain in parts:
        s = samples(start)
        out[s:s + len(sound)] += gain * sound
    return out


def echo(x, delays_and_gains, damping=3000):
    """Cheap room: delayed, darker copies of the sound."""
    tail = max(samples(d) for d, _ in delays_and_gains)
    out = np.concatenate([x, np.zeros(tail)])
    dark = lowpass(x, damping)
    for delay, gain in delays_and_gains:
        s = samples(delay)
        out[s:s + len(x)] += gain * dark
    return out


def finish(x, peak=0.9, fade_ms=6):
    """Remove DC, fade the tail, normalize."""
    x = highpass(np.asarray(x, dtype=float), 25, order=1)
    n = min(samples(fade_ms / 1000.0), len(x))
    x[-n:] *= np.linspace(1.0, 0.0, n)
    top = np.max(np.abs(x))
    if top > 0:
        x = x / top * peak
    return x


NOTE_INDEX = {"C": 0, "C#": 1, "D": 2, "D#": 3, "E": 4, "F": 5, "F#": 6,
              "G": 7, "G#": 8, "A": 9, "A#": 10, "B": 11}


def freq(note):
    """'C5' -> 523.25 Hz"""
    name, octave = note[:-1], int(note[-1])
    midi = 12 * (octave + 1) + NOTE_INDEX[name]
    return 440.0 * 2.0 ** ((midi - 69) / 12.0)


def melody(notes, wave="square", duty=0.5, vibrato=0.0, release=0.03):
    """notes: list of (note or None for a rest, seconds)."""
    out = []
    for note, seconds in notes:
        n = samples(seconds)
        if note is None:
            out.append(np.zeros(n))
            continue

        f = np.full(n, freq(note))
        if vibrato:
            t = times(seconds)
            # vibrato fades in, like a held note on the NES
            f *= 1.0 + vibrato * np.sin(2 * np.pi * 6.0 * t) * np.clip(t / 0.15, 0, 1)

        x = tone(f, seconds, wave, duty)
        env = np.ones(n)
        a = min(samples(0.004), n)
        r = min(samples(release), n // 2)
        env[:a] = np.linspace(0.0, 1.0, a)
        if r > 0:
            env[-r:] *= np.linspace(1.0, 0.0, r)
        out.append(x * env)

    return np.concatenate(out)


def fade_from(x, start_seconds, tau):
    """Let the end of a phrase ring out."""
    s = samples(start_seconds)
    if s < len(x):
        x = x.copy()
        x[s:] *= np.exp(-np.arange(len(x) - s) / SR / tau)
    return x


# =========================
# GAMEPLAY
# =========================

def shoot():
    """Player rifle: a punchy 'pew' with a noise crack."""
    body = tone(slide(1400, 200, 0.10), 0.10, "square", 0.5) * decay(0.10, 0.035)
    crack = highpass(nes_noise(0.06, 18000), 1200) * decay(0.06, 0.012)
    return finish(crush(mix((body, 0, 0.55), (crack, 0, 0.6)), 6))


def enemy_shoot():
    """Soldier rifle: lower and duller than the player's."""
    body = tone(slide(800, 120, 0.13), 0.13, "square", 0.25) * decay(0.13, 0.045)
    crack = lowpass(nes_noise(0.08, 9000), 3500) * decay(0.08, 0.02)
    return finish(crush(mix((body, 0, 0.55), (crack, 0, 0.6)), 6))


def hit():
    """A bullet hits an enemy."""
    ping = tone(slide(2000, 900, 0.05), 0.05, "square", 0.25) * decay(0.05, 0.012)
    tick = highpass(white(0.035), 2500) * decay(0.035, 0.008)
    return finish(mix((ping, 0, 0.5), (tick, 0, 0.7)))


def explosion():
    """Enemy destroyed: NES noise crunch falling in pitch, plus a low thump."""
    seconds = 0.9
    crunch = nes_noise(seconds, slide(9000, 500, seconds)) * decay(seconds, 0.22, attack=0.003)
    crunch = sweep_lowpass(crunch, 6000, 300)
    thump = tone(slide(150, 38, 0.35), 0.35, "sine") * decay(0.35, 0.10)
    return finish(np.tanh(1.8 * mix((crunch, 0, 1.0), (thump, 0, 0.9))))


def jump():
    sweep = tone(slide(240, 560, 0.15, exponential=False), 0.15, "square", 0.25)
    return finish(crush(sweep * decay(0.15, 0.09, attack=0.004), 6), 0.8)


def land():
    thud = lowpass(white(0.08), 500) * decay(0.08, 0.02)
    return finish(thud, 0.8)


def thud():
    """Heavy impact, used by the dialogue {shake} command."""
    boom = tone(slide(100, 35, 0.45), 0.45, "sine") * decay(0.45, 0.13, attack=0.003)
    dust = lowpass(white(0.15), 600) * decay(0.15, 0.035)
    return finish(np.tanh(1.5 * mix((boom, 0, 1.0), (dust, 0, 0.6))))


def bridge():
    """The bridge rises: rumble, three clanks, a chime."""
    rumble = sweep_lowpass(white(0.75), 200, 900)
    rumble *= np.sin(np.linspace(0, np.pi, len(rumble))) ** 0.7

    def clank():
        metal = tone(220, 0.07, "square", 0.5) * decay(0.07, 0.02)
        ring = tone(1100, 0.07, "triangle") * decay(0.07, 0.03)
        click = highpass(white(0.04), 1500) * decay(0.04, 0.01)
        return mix((metal, 0, 0.6), (ring, 0, 0.3), (click, 0, 0.5))

    chime = (tone(freq("E6"), 0.55, "triangle") +
             0.6 * tone(freq("B6"), 0.55, "triangle")) * decay(0.55, 0.16)

    return finish(mix((rumble, 0, 0.7),
                      (clank(), 0.05, 0.8),
                      (clank(), 0.25, 0.8),
                      (clank(), 0.45, 0.8),
                      (chime, 0.62, 0.55)))


def collar_break():
    """A mind-control collar shatters: glass pings and a bright chime."""
    seconds = 0.9
    shatter = np.zeros(samples(seconds))
    for _ in range(16):
        start = samples(rng.uniform(0.0, 0.18))
        ping = tone(rng.uniform(3000, 8000), 0.08, "sine") * decay(0.08, 0.02)
        shatter[start:start + len(ping)] += rng.uniform(0.3, 0.7) * ping
    burst = highpass(white(0.15), 3000) * decay(0.15, 0.035)

    chime = mix(*[(tone(freq(n), 0.6, "triangle") * decay(0.6, 0.22), start, 0.5)
                  for n, start in (("E6", 0.05), ("G#6", 0.12), ("B6", 0.19))])
    sparkle = tone(freq("E7"), 0.5, "sine") * decay(0.5, 0.18)

    return finish(mix((shatter, 0, 0.7), (burst, 0, 0.5),
                      (chime, 0, 0.8), (sparkle, 0.26, 0.3)))


def roar(seconds=1.9, base=95.0, seed=0):
    """A lion's roar: a growling pulse wave through mouth-like formant
    filters, with a rise-and-fall pitch and a short echo."""
    t = times(seconds)

    # Pitch rises in the first third, then sinks; plus a little wobble
    rise = np.clip(t / (0.3 * seconds), 0, 1)
    fall = np.clip((t - 0.3 * seconds) / (0.7 * seconds), 0, 1)
    contour = 0.85 + 0.5 * np.sin(rise * np.pi / 2) - 0.45 * fall
    jitter = 1.0 + 0.10 * smooth_random(seconds, 18, seed)
    f = base * contour * jitter

    voice = tone(f, seconds, "square", 0.18)
    fry = tone(f * 0.5, seconds, "saw")              # gravelly subharmonic
    breath = lowpass(white(seconds), 2500)
    source = voice + 0.6 * fry + 0.7 * breath

    throat = (1.0 * bandpass(source, 220, 600) +
              0.8 * bandpass(source, 650, 1250) +
              0.35 * bandpass(source, 1600, 2600))

    # Rough flutter, swell in, long fade out
    flutter = 1.0 - 0.35 * (0.5 + 0.5 * np.sin(2 * np.pi * 28 * t +
                                                3 * smooth_random(seconds, 6, seed + 1)))
    swell = np.clip(t / 0.22, 0, 1) ** 1.5
    release = np.clip(1.0 - (t - 0.55 * seconds) / (0.45 * seconds), 0, 1) ** 1.6
    x = np.tanh(2.2 * throat * flutter * swell * release)

    return finish(echo(x, [(0.045, 0.35), (0.083, 0.25), (0.12, 0.15)]), 0.95)


def growl():
    """A short, low growl when a controlled lion appears."""
    return roar(seconds=0.75, base=72.0, seed=5)


# =========================
# MUSIC STINGS
# =========================

def level_start():
    """'LEVEL n' card: a short march-like fanfare."""
    lead = melody([("D5", 0.11), (None, 0.03), ("D5", 0.11), (None, 0.03),
                   ("G5", 0.11), (None, 0.03), ("A5", 0.6)], "square", 0.5, vibrato=0.01)
    lead = fade_from(lead, 0.42, 0.25)
    harmony = melody([(None, 0.42), ("F#5", 0.6)], "square", 0.25)
    harmony = fade_from(harmony, 0.42, 0.25)
    bass = melody([("D3", 0.14), ("D3", 0.14), ("G3", 0.14), ("D3", 0.6)], "triangle")
    bass = fade_from(bass, 0.42, 0.3)
    snare = mix(*[(nes_noise(0.05, 12000) * decay(0.05, 0.012), s, 0.4)
                  for s in (0.0, 0.14, 0.28, 0.42)])
    return finish(mix((lead, 0, 0.35), (harmony, 0, 0.2), (bass, 0, 0.55), (snare, 0, 1.0)))


def stage_clear():
    """STAGE CLEAR: a bright rising arpeggio."""
    lead = melody([("G4", 0.09), ("C5", 0.09), ("E5", 0.09), ("G5", 0.09),
                   ("C6", 0.18), (None, 0.06), ("G5", 0.09), ("C6", 0.7)],
                  "square", 0.25, vibrato=0.008)
    lead = fade_from(lead, 0.69, 0.3)
    bass = melody([("C3", 0.36), ("G3", 0.33), ("C4", 0.7)], "triangle")
    bass = fade_from(bass, 0.69, 0.35)
    harmony = fade_from(melody([(None, 0.69), ("E5", 0.7)], "square", 0.5), 0.69, 0.3)
    return finish(mix((lead, 0, 0.4), (bass, 0, 0.6), (harmony, 0, 0.18)))


def game_over():
    """The player is hit: a falling zap, then a sad little phrase."""
    zap_seconds = 0.5
    wobble = 1.0 + 0.06 * np.sin(2 * np.pi * 22 * times(zap_seconds))
    zap = tone(slide(1000, 55, zap_seconds) * wobble, zap_seconds, "square") * decay(zap_seconds, 0.25)

    lead = melody([("E4", 0.2), ("D#4", 0.2), ("D4", 0.2), ("C#4", 0.9)], "square", 0.5, vibrato=0.012)
    lead = fade_from(lead, 0.6, 0.35)
    bass = melody([("E3", 0.2), ("D#3", 0.2), ("D3", 0.2), ("C#3", 0.9)], "triangle")
    bass = fade_from(bass, 0.6, 0.4)
    return finish(mix((zap, 0, 0.6), (lead, 0.55, 0.32), (bass, 0.55, 0.55)))


# =========================
# UI AND DIALOGUE
# =========================

def menu_move():
    return finish(tone(1320, 0.035, "square") * decay(0.035, 0.015), 0.8)


def menu_select():
    first = tone(freq("B5"), 0.05, "square") * decay(0.05, 0.03)
    second = tone(freq("F#6"), 0.10, "square") * decay(0.10, 0.04)
    return finish(mix((first, 0, 1.0), (second, 0.05, 1.0)), 0.85)


def go():
    """The way forward is open."""
    def beep(seconds):
        return (0.6 * tone(freq("A6"), seconds, "square", 0.25) +
                0.4 * tone(freq("A6"), seconds, "triangle")) * decay(seconds, 0.05)
    return finish(mix((beep(0.07), 0, 1.0), (beep(0.12), 0.12, 1.0)), 0.85)


def blip():
    """One 'voice' blip while dialogue types out. Each speaker plays it
    at their own pitch."""
    seconds = 0.045
    x = 0.6 * tone(520, seconds, "square") + 0.3 * tone(1040, seconds, "triangle")
    return finish(crush(x * decay(seconds, 0.018, attack=0.002), 5), 0.8)


def advance():
    """Next line of dialogue."""
    return finish(tone(700, 0.03, "square") * decay(0.03, 0.009), 0.8)


def radio_static():
    """Radio key-up: click, crackling hiss, squelch."""
    seconds = 0.4
    n = samples(seconds)
    hiss = bandpass(white(seconds), 1200, 5000)
    crackle = np.repeat(rng.uniform(0.15, 1.0, n // samples(1 / 80.0) + 1), samples(1 / 80.0))[:n]
    pops = (rng.random(n) < 0.002) * rng.uniform(-1.0, 1.0, n)
    click = tone(1500, 0.012, "square") * decay(0.012, 0.004)
    squelch = tone(900, 0.035, "square") * decay(0.035, 0.012)
    return finish(mix((click, 0, 0.5),
                      (hiss * crackle * decay(seconds, 0.16, attack=0.01), 0.01, 0.75),
                      (pops, 0.01, 0.8),
                      (squelch, 0.36, 0.3)), 0.8)


SOUNDS = {
    # gameplay
    "shoot": shoot,
    "enemy_shoot": enemy_shoot,
    "hit": hit,
    "explosion": explosion,
    "jump": jump,
    "land": land,
    "thud": thud,
    "bridge": bridge,
    "collar_break": collar_break,
    "roar": roar,
    "growl": growl,
    # music stings
    "level_start": level_start,
    "stage_clear": stage_clear,
    "game_over": game_over,
    # UI and dialogue
    "menu_move": menu_move,
    "menu_select": menu_select,
    "go": go,
    "blip": blip,
    "advance": advance,
    "radio_static": radio_static,
}


def save(name, x):
    os.makedirs(OUT, exist_ok=True)
    data = (np.clip(x, -1.0, 1.0) * 32767).astype("<i2")
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(SR)
        f.writeframes(data.tobytes())
    return path


def main(names):
    for name in names or SOUNDS:
        x = SOUNDS[name]()
        save(name, x)
        print("%-14s %.2fs" % (name, len(x) / SR))


if __name__ == "__main__":
    main(sys.argv[1:])
