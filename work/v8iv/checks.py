"""Measured checks for teaser v8iv (option C): dip floors, plain-cut deltas,
motion, silence runs, loudness."""
import subprocess
import numpy as np
import re
from pathlib import Path

VID = "teaser.mp4"
DIPS = [7.40, 16.98, 27.38]                 # kept dip boundaries
PLAIN = [1.92, 3.90, 4.72, 5.65, 8.03, 10.30, 12.10, 13.32, 14.17, 20.47,
         23.52, 25.98, 30.68]                # restored plain cuts
OUT = Path("work/v8iv/checks.txt")
lines = []


def decode_y(window_start, dur):
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-ss", f"{window_start:.4f}",
         "-t", f"{dur:.4f}", "-i", VID, "-vf", "scale=160:90,format=gray",
         "-f", "rawvideo", "-pix_fmt", "gray", "-"],
        capture_output=True)
    return np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)


def luma_delta_at(boundary, half=0.03):
    """max frame-to-frame luma delta (0-255) around a cut."""
    x = decode_y(boundary - half, 2 * half)
    n = 160 * 90
    if n == 0 or x.size < 2 * n:
        return None
    frames = x.reshape(-1, n)
    d = np.abs(np.diff(frames, axis=0)).mean(axis=1)
    return float(d.max())


def dip_floor(boundary, hw=0.05):
    """min luma (0-1) in the dip window around a section boundary."""
    x = decode_y(boundary - hw, 2 * hw)
    if x.size == 0:
        return None
    return float(x.min() / 255.0)


# 1) kept dip floors >= 0.25
lines.append("=== KEPT DIPS (section boundaries) — floor luma (0-1) ===")
for b in DIPS:
    f = dip_floor(b)
    lines.append(f"  {b:.2f}: floor={f:.3f}  {'OK' if f and f >= 0.25 else 'FAIL'}")

# 2) deltas across plain cuts < 30 (0-255)
lines.append("\n=== PLAIN CUTS — max frame luma delta (0-255) < 30 ===")
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
g = lambda k: re.search(rf"{k}:\s*([-\d.]+)", r.stderr)
print(f"  loudness integrated={g('Input Integrated')} LUFS  "
      f"truepeak={g('Input True Peak')} dBTP  LRA={g('Input LRA')} LU")
