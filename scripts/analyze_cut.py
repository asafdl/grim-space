"""Measure a cut from its pixels: shot boundaries, black-outs, dead frames.

The point is to never trust a text-only report about what a video contains.
Everything below is computed from the decoded frames.
"""
import argparse
import json
import subprocess
from pathlib import Path

import numpy as np


def decode(path: Path, width: int, height: int) -> np.ndarray:
    raw = subprocess.run(
        [
            "ffmpeg", "-v", "error", "-i", str(path),
            "-vf", f"scale={width}:{height}", "-pix_fmt", "gray",
            "-f", "rawvideo", "-",
        ],
        capture_output=True,
        check=True,
    ).stdout
    return np.frombuffer(raw, dtype=np.uint8).reshape(-1, height, width)


def analyse(path: Path, width: int, height: int, fps: float) -> dict:
    frames = decode(path, width, height)
    count = len(frames)
    flat = frames.reshape(count, -1).astype(np.int16)

    luma = flat.mean(axis=1)
    delta = np.zeros(count)
    changed = np.zeros(count)
    for i in range(1, count):
        diff = np.abs(flat[i] - flat[i - 1])
        delta[i] = diff.mean()
        changed[i] = float((diff > 12).mean())

    # A cut is a large, whole-frame change; a dissolve is a ramp.
    cuts = []
    for i in range(1, count):
        if delta[i] > 25 and changed[i] > 0.35:
            # Skip if this is part of a longer ramp (a dissolve).
            ramp = sum(1 for j in range(i, min(i + 18, count)) if delta[j] > 6)
            if ramp <= 2:
                cuts.append(i)

    blackouts = [i for i in range(count) if luma[i] < 4.0]

    # Seconds in which nothing moved at all.
    per_second = {}
    for i in range(count):
        second = int(i / fps)
        per_second.setdefault(second, 0)
        if delta[i] > 0.0:
            per_second[second] += 1
    dead_seconds = sorted(s for s, moved in per_second.items() if moved == 0)

    return {
        "frames": count,
        "duration": count / fps,
        "min_luma": float(luma.min()),
        "max_luma": float(luma.max()),
        "mean_luma": float(luma.mean()),
        "max_delta": float(delta.max()),
        "max_delta_frame": int(delta.argmax()),
        "cut_frames": cuts,
        "cut_times": [round(i / fps, 3) for i in cuts],
        "blackout_frames": blackouts,
        "blackout_times": [round(i / fps, 3) for i in blackouts],
        "dead_seconds": dead_seconds,
        "luma_per_second": {
            s: round(float(luma[int(s * fps):int((s + 1) * fps)].mean()), 2)
            for s in range(int(count / fps))
        },
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("video")
    parser.add_argument("--width", type=int, default=320)
    parser.add_argument("--height", type=int, default=180)
    parser.add_argument("--fps", type=float, default=60.0)
    args = parser.parse_args()

    result = analyse(Path(args.video), args.width, args.height, args.fps)
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
