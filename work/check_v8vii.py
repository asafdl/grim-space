"""Measured checks for teaser v8vii (slow film-speed fades at ALL boundaries).

Every internal boundary must be a REAL slow fade: total ramp 45-60 frames
(0.75-1.0 s @60fps), smooth in+out, bottom near-black <=0.10 luma.
Keep room tone, motion, loudness. Motion measured outside the fade window.
Zero silence, ~-14 LUFS.
"""
import subprocess
import numpy as np
import re
from pathlib import Path

VID = "teaser.mp4"
BOUNDS = [1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32, 14.17,
          16.98, 20.47, 23.52, 25.98, 27.38, 30.68]
OUT = Path("work/v8vii/checks.txt")
lines = []
FPS = 60.0
DURATION = 34.63
WINDOW = 1.00          # decode window around each boundary (±s)


def decode_y(window_start, dur):
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-ss", f"{window_start:.4f}",
         "-t", f"{dur:.4f}", "-i", VID, "-vf", "scale=160:90,format=gray",
         "-f", "rawvideo", "-pix_fmt", "gray", "-"],
        capture_output=True)
    return np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)


def flank_level(series, idx, side):
    """Estimate flanking shot level using 95th percentile just outside the fade.

    side='pre':  use series[max(0,idx-45):idx-5]  (up to ~0.75s before, excluding
                 the last 5 frames that are fade-out tail)
    side='post': use series[idx+5:min(nf,idx+45)] (from ~0.1s after, excluding
                 the first 5 frames that are fade-in head)
    95th percentile ignores the near-black fade bottom and captures the shot level.
    """
    nf = len(series)
    if side == "pre":
        seg = series[max(0, idx - 45):max(0, idx - 5)]
    else:
        seg = series[min(nf, idx + 5):min(nf, idx + 45)]
    if seg.size == 0:
        return None
    return float(np.percentile(seg, 95))


def ramp_frames(boundary):
    """Total ramp length (frames) around a boundary.

    Anchors at the boundary frame (mid of window). Fade bottom verified near-black.
    Walks out from boundary while luma < 0.95 * flank_level on each side.
    Returns (total_frames, down_frames, up_frames, floor_luma_0to1).
    """
    x = decode_y(boundary - WINDOW, 2 * WINDOW)
    if x.size == 0:
        return None
    n = 160 * 90
    series = x.reshape(-1, n).mean(axis=1)
    nf = len(series)
    mid = nf // 2  # frame at the boundary

    # Fade bottom: min within ±4 frames of the boundary frame.
    lo = max(0, mid - 4)
    hi = min(nf, mid + 5)
    bottom = lo + int(np.argmin(series[lo:hi]))
    floor = float(series[bottom] / 255.0)

    pre_level = flank_level(series, mid, "pre")
    post_level = flank_level(series, mid, "post")
    if pre_level is None or post_level is None or pre_level <= 1.0 or post_level <= 1.0:
        return None

    # Walk left from boundary while below 95% of pre level (in fade-out).
    down = 0
    i = mid - 1
    while i >= 0 and series[i] < 0.95 * pre_level:
        down += 1
        i -= 1
    # Walk right from boundary while below 95% of post level (in fade-in).
    up = 0
    i = mid + 1
    while i < nf and series[i] < 0.95 * post_level:
        up += 1
        i += 1
    return (down + up + 1, down, up, floor)


def motion_outside(boundary, fade_half=0.50, band=0.85):
    """Mean per-frame luma |delta| just outside the fade on each side."""
    def seg_motion(t0, t1):
        if t1 <= t0:
            return None
        x = decode_y(t0, t1 - t0)
        if x.size == 0:
            return None
        n = 160 * 90
        frames = x.reshape(-1, n)
        d = np.abs(np.diff(frames, axis=0)).mean(axis=1)
        return float(d.mean()) if d.size else None

    a = seg_motion(boundary - band, boundary - fade_half)
    b = seg_motion(boundary + fade_half, boundary + band)
    vals = [v for v in (a, b) if v is not None]
    return float(np.mean(vals)) if vals else None


# --- main checks ---------------------------------------------------------
lines.append("=== ALL BOUNDARIES — slow film-speed fade, floor <= 0.10, ramp 45-60f ===")
worst_floor = 0.0
ramp_ok = True
motion_ok = True
for b in BOUNDS:
    rm = ramp_frames(b)
    if rm is None:
        lines.append(f"  {b:.2f}: N/A (could not measure)")
        ramp_ok = False
        continue
    rf, down, up, fl = rm
    worst_floor = max(worst_floor, fl)
    ok_floor = fl <= 0.10
    ok_ramp = 45 <= rf <= 60
    ramp_ok = ramp_ok and ok_ramp
    mv = motion_outside(b)
    ok_motion = mv is not None and 0.5 <= mv <= 3.0
    motion_ok = motion_ok and ok_motion
    lines.append(
        f"  {b:.2f}: floor={fl:.3f}  {'OK' if ok_floor else 'FAIL'}  "
        f"ramp={rf}f (~{rf/FPS:.2f}s) down={down}f up={up}f  "
        f"{'OK' if ok_ramp else 'FAIL'}  (law: 45-60f / 0.75-1.0s)  "
        f"motion_out={mv:.2f}  {'OK' if ok_motion else 'FAIL'}  (0.5-3.0)"
    )
lines.append(f"  worst boundary floor = {worst_floor:.3f}  {'OK' if worst_floor <= 0.10 else 'FAIL'}")
lines.append(f"  ramp law 45-60f all boundaries: {'OK' if ramp_ok else 'FAIL'}")
lines.append(f"  motion 0.5-3.0 all boundaries: {'OK' if motion_ok else 'FAIL'}")

# 2) whole-file motion
lines.append("\n=== WHOLE-FILE MOTION (mean per-frame luma |delta|, 0-255) ===")
x = decode_y(0, DURATION)
n = 160 * 90
frames = x.reshape(-1, n)
d = np.abs(np.diff(frames, axis=0)).mean(axis=1)
motion = float(d.mean())
lines.append(f"  mean motion = {motion:.2f}  {'OK' if 0.5 <= motion <= 3.0 else 'FAIL'}")

OUT.write_text("\n".join(lines) + "\n")
print("\n".join(lines))

# Audio checks (loudness + no silence)
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
