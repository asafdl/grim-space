#!/usr/bin/env python3
"""Generate the sun's event timeline and its soundtrack.

The timeline (assets/data/sun_events.json) is the single source of truth for
both the renderer and the sound, so a flare that is visible is also audible.

The soundtrack is a solar rumble:
  * a deep sub hum (a low sine plus brown noise) that is always there,
  * a boiling crackle whose density follows the prominence activity, and
  * pressure swells on the transient events (a flare sting, a CME shove).

Output: assets/data/sun_events.json and dist/sun_rumble.wav
"""
import json
import math
import os
import subprocess
import sys

import numpy as np

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
DURATION = 12.0
SAMPLE_RATE = 48000
SEED = 4.37
ERUPT_PERIOD = 13.0
SITE_COUNT = 5

# The shader's site hash and eruption envelope, replicated so the sound
# breathes with the prominences.
def hash11(n):
    return (math.sin(n * 127.1 + 311.7) * 43758.5453123) % 1.0


def smoothstep(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3.0 - 2.0 * t)


def eruption_envelope(t):
    total = 0.0
    for i in range(SITE_COUNT):
        h1 = hash11(i * 17.13 + SEED)
        h2 = hash11(i * 31.71 + SEED + 5.3)
        ph = (t / ERUPT_PERIOD + h2) % 1.0
        env = smoothstep(0.0, 0.16, ph) * (1.0 - smoothstep(0.42, 0.92, ph))
        total += env * (0.45 + 0.85 * hash11(i * 47.33 + SEED + 11.9))
    return min(total / SITE_COUNT, 1.0)


EVENTS = [
    {"t": 1.35, "type": "flare", "strength": 0.95, "duration": 1.1,
     "dir": [0.55, 0.34], "size": 0.16,
     "audio": "sting"},
    {"t": 4.10, "type": "cme", "strength": 0.80, "duration": 4.5,
     "angle": -1.15, "radius": 0.42,
     "audio": "shove"},
    {"t": 6.60, "type": "rain", "strength": 0.85, "duration": 3.2,
     "audio": "rain"},
    {"t": 9.20, "type": "flare", "strength": 0.70, "duration": 0.9,
     "dir": [2.40, -0.30], "size": 0.13,
     "audio": "sting"},
]

SPOTS = [
    {"dir": [0.55, 0.34], "radius": 0.052, "strength": 0.80, "drift": 0.012},
    {"dir": [-0.85, 0.22], "radius": 0.034, "strength": 0.55, "drift": 0.009},
    {"dir": [2.10, -0.41], "radius": 0.041, "strength": 0.65, "drift": 0.014},
]


def write_timeline():
    os.makedirs(os.path.join(ROOT, 'assets', 'data'), exist_ok=True)
    path = os.path.join(ROOT, 'assets', 'data', 'sun_events.json')
    with open(path, 'w', encoding='utf-8') as handle:
        json.dump({
            "duration": DURATION,
            "erupt_period": ERUPT_PERIOD,
            "seed": SEED,
            "spots": SPOTS,
            "events": EVENTS,
        }, handle, indent=1)
    print(f'wrote {path}')


def event_envelope(t, event):
    start = event["t"]
    duration = event["duration"]
    elapsed = t - start
    if elapsed < 0 or elapsed > duration:
        return 0.0
    attack = max(0.0, min(1.0, elapsed / min(duration * 0.12, 0.25)))
    decay = 1.0 - max(0.0, min(1.0, (elapsed - duration * 0.12) / (duration * 0.88)))
    return attack * (1.0 - decay) * event["strength"]


def bandpass(x, low, high):
    spectrum = np.fft.rfft(x)
    freqs = np.fft.rfftfreq(len(x), 1.0 / SAMPLE_RATE)
    spectrum[(freqs < low) | (freqs > high)] = 0
    return np.fft.irfft(spectrum, len(x))


def synth():
    count = int(DURATION * SAMPLE_RATE)
    t = np.arange(count) / SAMPLE_RATE
    rng = np.random.default_rng(20240508)

    # --- the deep sub hum -------------------------------------------------
    sub = 0.55 * np.sin(2 * np.pi * 42.0 * t)
    sub += 0.25 * np.sin(2 * np.pi * 63.5 * t + 0.7)
    brown = rng.normal(0, 1, count)
    brown = np.cumsum(brown) / np.sqrt(count)
    brown = bandpass(brown, 18.0, 160.0)
    hum = sub + 6.0 * brown
    hum *= 0.6 + 0.4 * np.sin(2 * np.pi * 0.13 * t)

    # --- the boiling crackle ----------------------------------------------
    noise = rng.normal(0, 1, count)
    sparse = noise * (np.abs(noise) > 1.55)
    crackle = bandpass(sparse, 900.0, 7000.0) * 22.0

    # The crackle density follows the prominence activity, so the surface
    # sounds busier when a loop is up.
    activity = np.array([eruption_envelope(x) for x in t])
    activity = 0.35 + 0.85 * activity
    crackle *= activity

    # --- the event pressure swells ----------------------------------------
    swell = np.zeros(count)
    sting = np.zeros(count)
    shove = np.zeros(count)
    for event in EVENTS:
        env = np.array([event_envelope(x, event) for x in t])
        if event["audio"] == "sting":
            # A sharp pressure spike: a falling sub tone plus a noise burst.
            burst = bandpass(rng.normal(0, 1, count), 200.0, 2500.0)
            swell += env * np.sin(2 * np.pi * (95.0 - 45.0 * (t - event["t"])) * np.clip(t - event["t"], 0, None)) * 0.9
            sting += env * burst * 0.5
        elif event["audio"] == "shove":
            swell += env * np.sin(2 * np.pi * 30.0 * t + 2.0 * np.sin(2 * np.pi * 0.2 * t)) * 1.6
        elif event["audio"] == "rain":
            # Coronal rain: a scatter of small high drops.
            drops = np.zeros(count)
            for _ in range(90):
                start = int(rng.uniform(event["t"], event["t"] + event["duration"]) * SAMPLE_RATE)
                length = int(rng.uniform(0.01, 0.05) * SAMPLE_RATE)
                if start + length >= count:
                    continue
                shape = np.exp(-np.linspace(0, 6, length))
                drops[start:start + length] += shape * rng.uniform(0.4, 1.0)
            sting += bandpass(drops, 1200.0, 9000.0) * 0.7 * 6.0

    mix = hum * 0.55 + crackle * 0.32 + swell * 0.42 + sting * 0.30
    peak = float(np.max(np.abs(mix)))
    if peak > 0:
        mix = mix / peak * 0.85
    return mix


def main():
    duration = DURATION
    out_name = 'sun_rumble.wav'
    args = sys.argv[1:]
    for i, arg in enumerate(args):
        if arg == '--duration' and i + 1 < len(args):
            duration = float(args[i + 1])
        elif arg == '--out' and i + 1 < len(args):
            out_name = args[i + 1]
    globals()['DURATION'] = duration
    write_timeline()
    os.makedirs(os.path.join(ROOT, 'dist'), exist_ok=True)
    raw = os.path.join(ROOT, 'dist', 'sun_rumble_raw.wav')
    out = os.path.join(ROOT, 'dist', out_name)
    mix = synth()
    import wave
    with wave.open(raw, 'wb') as handle:
        handle.setnchannels(1)
        handle.setsampwidth(2)
        handle.setframerate(SAMPLE_RATE)
        handle.writeframes((np.clip(mix, -1, 1) * 32767).astype('<i2').tobytes())
    # loudness: -16 LUFS integrated, true peak under -1.5 dBTP
    subprocess.run([
        'ffmpeg', '-y', '-v', 'error', '-i', raw,
        '-af', 'loudnorm=I=-16:TP=-1.5:LRA=9',
        '-ar', str(SAMPLE_RATE), out,
    ], check=True)
    print(f'wrote {out}')


if __name__ == '__main__':
    sys.exit(main())
