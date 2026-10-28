"""Measured checks for teaser v8v (user-law: slow dark dips, gray floors banned).

Amended floor law (2026-10-28 by user eye): exposure-matched gray dip floors
REFUTED -- gray reads as screen flash. Standing law: section dips slow (12-14f)
and near-black (<=0.10 luma), plain chroma-matched cuts elsewhere.
"""
import subprocess
import numpy as np
import re
from pathlib import Path

VID = "teaser.mp4"
DIPS = [7.40, 16.98, 27.38]                 # slow dark dips (section boundaries)
PLAIN = [1.92, 3.90, 4.72, 5.65, 8.03, 10.30, 12.10, 13.32, 14.17, 20.47,
         23.52, 25.98, 30.68]                # plain chroma-matched cuts
OUT = Path("work/v8v/checks.txt")
lines = []


def decode_y(window_start, dur):
    """Decode a window with ACCURATE seeking (-i before -s avoids keyframe snap)."""
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", VID,
         "-ss", f"{window_start:.4f}", "-t", f"{dur:.4f}",
         "-vf", "scale=160:90,format=gray",
         "-f", "rawvideo", "-pix_fmt", "gray", "-"],
        capture_output=True)
    return np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)


def luma_at(t, eps=0.02):
    """Mean luma (0-255) of a single frame at time t (accurate seek)."""
    x = decode_y(max(0.0, t - eps), 2 * eps)
    if x.size == 0:
        return None
    return float(x.mean())


def luma_delta_at(boundary, eps=0.02):
    """luma step (0-255) across a plain cut: |luma(b-eps) - luma(b+eps)|."""
    a = luma_at(boundary, eps)
    b = luma_at(boundary + 2 * eps, eps)
    if a is None or b is None:
        return None
    return abs(b - a)


def dip_floor(boundary, hw=0.10):
    """min luma (0-1) in the dip window around a section boundary."""
    x = decode_y(boundary - hw, 2 * hw)
    if x.size == 0:
        return None
    return float(x.min() / 255.0)


# 1) kept dip floors <= 0.10 (AMENDED law: near-black, not gray)
lines.append("=== KEPT DIPS (section boundaries) — floor luma (0-1), law <= 0.10 ===")
worst_floor = 0.0
for b in DIPS:
    f = dip_floor(b)
    worst_floor = max(worst_floor, f if f is not None else 0.0)
    ok = f is not None and f <= 0.10
    lines.append(f"  {b:.2f}: floor={f:.3f}  {'OK' if ok else 'FAIL'}  (law: <= 0.10 near-black)")
lines.append(f"  worst dip floor = {worst_floor:.3f}  {'OK' if worst_floor <= 0.10 else 'FAIL'}")

# 2) deltas across plain cuts < 30 (0-255)
lines.append("\n=== PLAIN CUTS — luma step across cut (0-255) < 30 ===")
worst = 0
for b in PLAIN:
    d = luma_delta_at(b)
    if d is None:
        lines.append(f"  {b:.2f}: N/A")
        continue
    worst = max(worst, d)
    lines.append(f"  {b:.2f}: delta={d:.1f}  {'OK' if d < 30 else 'FAIL'}")
lines.append(f"  worst plain-cut delta = {worst:.1f}")

# 3) motion 0.5-3.0 (mean |delta| per second across file)
lines.append("\n=== MOTION (mean per-frame luma |delta|, 0-255) ===")
x = decode_y(0, 34.63)
n = 160 * 90
frames = x.reshape(-1, n)
d = np.abs(np.diff(frames, axis=0)).mean(axis=1)
motion = float(d.mean())
lines.append(f"  mean motion = {motion:.2f}  {'OK' if 0.5 <= motion <= 3.0 else 'FAIL'}")

OUT.write_text("\n".join(lines) + "\n")
print("\n".join(lines))

# Audio checks (silence runs + loudness)
print("\n=== AUDIO ===")
r = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", VID,
                    "-map", "0:a", "-af", "silencedetect=noise=-30dB:d=0.2",
                    "-f", "null", "-"], capture_output=True, text=True)
silence = re.findall(r"silence_start: ([\d.]+).*silence_end: ([\d.]+)", r.stderr)
if silence:
    print("  SILENCE RUNS FOUND:")
    for s, e in silence:
        print(f"    {s}s -> {e}s  (dur {float(e)-float(s):.2f}s)")
else:
    print("  zero silence runs: OK")

r = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-i", VID, "-map", "0:a",
                    "-af", "loudnorm=print_format=summary", "-f", "null", "-"],
                   capture_output=True, text=True)
def G(k):
    m = re.search(rf"{k}:\s*([-\d.]+)", r.stderr)
    return m.group(1) if m else None
li = G("Input Integrated"); tp = G("Input True Peak"); lra = G("Input LRA")
li_ok = li and abs(float(li) - (-14.0)) <= 2.0
print(f"  loudness integrated={li} LUFS  truepeak={tp} dBTP  LRA={lra} LU  "
      f"{'OK (-14 LUFS class)' if li_ok else 'CHECK'}")
