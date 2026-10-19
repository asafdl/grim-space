"""Profile the music library so the teaser cut can be timed to a real beat map.

For every track: duration, an RMS envelope per second, the loudest second, and
the best 30 s window for a trailer (quiet enough to open under a hook, loud
enough to peak on the end card).
"""
import json
import subprocess
import sys
from pathlib import Path

import numpy as np

MUSIC = Path("assets/music/terraforming-mars-tracks")
WINDOW = 30.0


def decode(path: Path) -> tuple[np.ndarray, int]:
    raw = subprocess.run(
        [
            "ffmpeg", "-v", "error", "-i", str(path),
            "-f", "s16le", "-ac", "1", "-ar", "22050", "-",
        ],
        capture_output=True,
        check=True,
    ).stdout
    return np.frombuffer(raw, dtype=np.int16).astype(np.float32) / 32768.0, 22050


def rms_envelope(samples: np.ndarray, rate: int, step: int = 22050) -> np.ndarray:
    n = len(samples) // step
    trimmed = samples[: n * step].reshape(n, step)
    return np.sqrt(np.maximum((trimmed ** 2).mean(axis=1), 1e-12))


def db(value: float) -> float:
    return 20.0 * float(np.log10(max(value, 1e-9)))


def profile(path: Path) -> dict:
    samples, rate = decode(path)
    env = rms_envelope(samples, rate)
    seconds = len(env)
    if seconds < WINDOW:
        return {"track": path.name, "seconds": seconds, "usable": False}

    env_db = 20.0 * np.log10(np.maximum(env, 1e-9))

    # Best window: the end of the window should be the loudest part of the
    # window (the end card), and the opening should be at least 4 dB below the
    # body so the hook has room.
    best = None
    for start in range(0, seconds - int(WINDOW) + 1):
        w = env_db[start:start + int(WINDOW)]
        body = w[3:27]
        peak = float(w.max())
        if peak < -11.0:
            continue
        score = peak - float(body.mean())
        if best is None or score > best[0]:
            best = (score, start, peak, float(body.mean()), float(w[0]))

    if best is None:
        return {"track": path.name, "seconds": seconds, "usable": False}

    _, start, peak, body_mean, open_level = best
    return {
        "track": path.name,
        "seconds": seconds,
        "usable": True,
        "window_start": start,
        "window_end": start + WINDOW,
        "peak_db": round(peak, 2),
        "body_db": round(body_mean, 2),
        "open_db": round(open_level, 2),
        "open_to_peak_db": round(peak - open_level, 2),
        "score": round(best[0], 2),
    }


def main() -> None:
    tracks = sorted(MUSIC.glob("track-*.ogg"))
    results = []
    for track in tracks:
        try:
            results.append(profile(track))
        except subprocess.CalledProcessError as error:
            results.append({"track": track.name, "error": str(error)})

    usable = [r for r in results if r.get("usable")]
    usable.sort(key=lambda r: r["score"], reverse=True)
    print(json.dumps(usable[:12], indent=2))
    print("--- unusable ---")
    for r in results:
        if not r.get("usable"):
            print(r)


if __name__ == "__main__":
    sys.exit(main())
