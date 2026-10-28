"""Measured checks for teaser v8vi (user-ruled: film-speed section fades).

PACE LAW (2nd user order): section dips at 7.40, 16.98, 27.38 must be REAL
SLOW fades -- total ramp 45-60 frames (0.75-1.0 s @60fps), smooth eased in/out,
bottom near-black <=0.10. Plain chroma-matched cuts elsewhere stay AS-IS.
Keep room tone, motion, loudness. Motion continuity around dips 0.5-3.0.
"""
import subprocess
import numpy as np
import re
from pathlib import Path

VID = "teaser.mp4"
DIPS = [7.40, 16.98, 27.38]                 # section-change boundaries -> film-speed fade
PLAIN = [1.92, 3.90, 4.72, 5.65, 8.03, 10.30, 12.10, 13.32, 14.17, 20.47,
         23.52, 25.98, 30.68]                # plain chroma-matched cuts (AS-IS)
OUT = Path("work/v8vi/checks.txt")
lines = []
FPS = 60.0


def decode_y(window_start, dur):
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-ss", f"{window_start:.4f}",
         "-t", f"{dur:.4f}", "-i", VID, "-vf", "scale=160:90,format=gray",
         "-f", "rawvideo", "-pix_fmt", "gray", "-"],
        capture_output=True)
    return np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)


def luma_series(t0, t1):
    """Per-frame luma (0-255) over [t0, t1]."""
    x = decode_y(t0, t1 - t0)
    if x.size == 0:
        return None
    n = 160 * 90
    return x.reshape(-1, n).mean(axis=1)


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


def ramp_frames(boundary, span=0.70, thr=0.90):
    """Total ramp length (frames) around a section dip.

    Measures each side against its OWN shot level (the two shots flanking a
    section boundary differ in brightness, so a single shared threshold would
    overcount the darker side). Walks back from the boundary while luma < thr *
  pre_level (outgoing shot) and forward while luma < thr * post_level
    (incoming shot). Returns (ramp_frames, down_frames, up_frames, floor_luma).
    """
    s = luma_series(boundary - span, boundary + span)
    if s is None:
        return None
    n = len(s)
    mid = n // 2  # frame at the boundary
    # clean pre/post levels sampled just outside each fade (T +/- 0.50 s, i.e.
    # just before fade-out starts / just after fade-in ends). Single accurate
    # frames, one per flanking shot, so short incoming segments never bleed
    # into the next shot.
    pre = luma_at(boundary - 0.50)
    post = luma_at(boundary + 0.50)
    if pre is None or post is None or pre <= 1.0 or post <= 1.0:
        return None
    down = 0
    i = mid - 1
    while i >= 0 and s[i] < thr * pre:
        down += 1
        i -= 1
    up = 0
    i = mid + 1
    while i < n and s[i] < thr * post:
        up += 1
        i += 1
    floor = float(s.min() / 255.0)
    return (down + up + 1, down, up, floor)


# 1) kept dips: floor <= 0.10 + ramp length 45-60 frames
lines.append("=== KEPT DIPS (section boundaries) — film-speed fade, floor <= 0.10, ramp 45-60f ===")
worst_floor = 0.0
ramp_ok = True
for b in DIPS:
    f = dip_floor(b)
    worst_floor = max(worst_floor, f if f is not None else 0.0)
    rm = ramp_frames(b)
    rf, down, up, fl = rm
    ok_floor = f is not None and f <= 0.10
    ok_ramp = rm is not None and 45 <= rf <= 60
    ramp_ok = ramp_ok and ok_ramp
    lines.append(
        f"  {b:.2f}: floor={f:.3f}  {'OK' if ok_floor else 'FAIL'}  "
        f"ramp={rf}f (~{rf/FPS:.2f}s) down={down}f up={up}f  "
        f"{'OK' if ok_ramp else 'FAIL'}  (law: 45-60f / 0.75-1.0s)"
    )
lines.append(f"  worst dip floor = {worst_floor:.3f}  {'OK' if worst_floor <= 0.10 else 'FAIL'}")
lines.append(f"  ramp law 45-60f: {'OK' if ramp_ok else 'FAIL'}")

# 2) plain cuts: delta < 30 (AS-IS)
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

# 3) motion 0.5-3.0
lines.append("\n=== MOTION (mean per-frame luma |delta|, 0-255) ===")
x = decode_y(0, 34.63)
n = 160 * 90
frames = x.reshape(-1, n)
d = np.abs(np.diff(frames, axis=0)).mean(axis=1)
motion = float(d.mean())
lines.append(f"  mean motion = {motion:.2f}  {'OK' if 0.5 <= motion <= 3.0 else 'FAIL'}")

OUT.write_text("\n".join(lines) + "\n")
print("\n".join(lines))

# Audio checks (loudness + no new dead air)
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
