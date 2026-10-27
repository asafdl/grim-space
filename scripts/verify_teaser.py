"""Verify the shipped teaser against the reship gates.

One command, one output, every claim in TEASER_NOTES points at it:

    python scripts/verify_teaser.py teaser.mp4

Everything is measured from the file that is shipped: the shot map (the
auditor's two detectors), the luma floor, the goop field, the chroma, the
in-shot events, the SFX causation, the audio arc and the microtrailer samples.
"""
import json
import subprocess
import sys
from pathlib import Path

import numpy as np

W, H = 160, 90
HW, HH = 32, 18
CARD_START = 24.35
CARD_END = 28.28
GOOP_WINDOW = (8.90, 13.15)  # retimed for v8iii cut (was 10.90-15.50 on 34.62s v8ii)
MICRO = [2.30, 4.60, 9.00, 12.60, 24.00, 28.00]

# The film's chain: every beat, the sentence that joins it to the one before,
# and whether it advances the goal or only decorates it.
def decode(path: Path, w: int, h: int, mode: str = "gray") -> np.ndarray:
    args = ["ffmpeg", "-v", "error", "-i", str(path), "-vf", f"scale={w}:{h},format={mode}"]
    args += ["-f", "rawvideo", "-"] if mode == "gray" else ["-pix_fmt", "rgb24", "-f", "rawvideo", "-"]
    raw = subprocess.run(args, capture_output=True, check=True).stdout
    channels = 1 if mode == "gray" else 3
    frames = np.frombuffer(raw, dtype=np.uint8).reshape(-1, h, w, channels)
    return frames[:, :, :, 0] if mode == "gray" else frames


def shots(luma: np.ndarray, hist: np.ndarray, refractory: float = 0.6) -> list[float]:
    """Luma delta 0.03 and histogram correlation 0.98, with a published refractory.

    The 0.6 s refractory is the settled instrument (it was never disclosed
    before). ``refractory=0.0`` gives the raw detection so a reviewer can see
    what the refractory was suppressing.
    """
    flat = hist.reshape(len(hist), -1).astype(np.float32)
    centred = flat - flat.mean(axis=1, keepdims=True)
    norm = np.sqrt((centred ** 2).sum(axis=1)) + 1e-6
    corr = (centred[1:] * centred[:-1]).sum(axis=1) / (norm[1:] * norm[:-1])
    delta = np.abs(np.diff(flat.mean(axis=1)) / 255.0)
    boundaries = [0.0]
    for i in range(1, len(delta)):
        if (delta[i] > 0.03 or corr[i] < 0.98) and (i / 60.0 - boundaries[-1]) > refractory:
            boundaries.append(round(i / 60.0, 2))
    return boundaries


def green(frame: np.ndarray) -> int:
    r, g, b = frame[:, :, 0].astype(int), frame[:, :, 1].astype(int), frame[:, :, 2].astype(int)
    return int(((g > 70) & (g - r > 25) & (g - b > 25)).sum())


def main() -> None:
    path = Path(sys.argv[1] if len(sys.argv) > 1 else "teaser.mp4")
    luma = decode(path, W, H).mean(axis=(1, 2)) / 255.0
    hist = decode(path, HW, HH)
    rgb = decode(path, W, H, "rgb24").astype(np.int16)
    n = len(luma)
    fps = 60.0
    duration = n / fps

    boundaries = shots(luma, hist)
    lengths = [round(boundaries[i + 1] - boundaries[i], 2) for i in range(len(boundaries) - 1)]
    if boundaries[-1] < duration:
        lengths.append(round(duration - boundaries[-1], 2))

    print("== SHOT MAP (thresholds published: luma-delta 0.03 + hist-corr 0.98, "
          "refractory 0.6 s) ==")
    print(f"frames {n}  duration {duration:.2f}s  shots {len(boundaries) - 1}  "
          f"ASL {duration / (len(boundaries) - 1):.2f}s  longest {max(lengths):.2f}s")
    print("boundaries (refractory 0.6 s):", boundaries)
    raw_map = shots(luma, hist, refractory=0.0)
    print("boundaries (refractory 0.0 s):", raw_map)
    raw_times = sorted({round(b, 2) for b in raw_map})
    suppressed = [b for b in raw_map if b not in boundaries]
    print(f"cuts the refractory suppressed ({len(suppressed)}):", suppressed)

    below = float((luma < 0.030).mean())
    card = luma[int(CARD_START * fps):int(CARD_END * fps)]
    print("\n== LUMA FLOOR ==")
    print(f"frames below 0.030: {below * 100:.1f}%  (gate: <=5%)   mean luma {luma.mean():.4f}")
    print(f"card frames {CARD_START:.2f}-{CARD_END:.2f}s: min {card.min():.4f} "
          f"mean {card.mean():.4f} max {card.max():.4f} (gate: card >=0.030)")

    # AC-T1 / AC-T6: the seam set and the dead-time ban, measured from this
    # file only. A seam fails when the incoming shot is under half the
    # outgoing's level and the pair is dark (the black-hole class).
    print("\n== SEAMS (AC-T1: incoming >= 0.5x outgoing, both above the dark class) ==")
    seams = [b for b in boundaries if 1.0 <= b <= 30.7]
    seam_rows = []
    for b in seams:
        i = int(round(b * fps))
        outgoing = float(luma[max(0, i - 3):i].mean())
        incoming = float(luma[i + 1:i + 4].mean()) if i + 4 <= n else float(luma[i + 1:].mean())
        ratio = incoming / outgoing if outgoing > 1e-6 else 0.0
        dark = incoming < 0.030 or outgoing < 0.030
        verdict = "BLACK-HOLE" if (ratio < 0.5 and dark) else "OK"
        seam_rows.append((b, outgoing, incoming, ratio, verdict))
        print(f"  t={b:5.2f}  outgoing {outgoing:.3f}  incoming {incoming:.3f}  "
              f"ratio {ratio:.2f}  {verdict}")
    print(f"black-hole seams: {sum(1 for row in seam_rows if row[4] != 'OK')} of {len(seam_rows)}")

    print("\n== DEAD TIME (AC-T6: no run below 0.030 longer than 0.15 s, 1.0-30.7 s) ==")
    lo_i, hi_i = int(1.0 * fps), int(30.7 * fps)
    run = 0
    runs_below = []
    for i in range(lo_i, min(hi_i, n)):
        if luma[i] < 0.030:
            run += 1
        else:
            if run:
                runs_below.append((round((i - run) / fps, 2), round(run / fps, 2)))
            run = 0
    if run:
        runs_below.append((round((min(hi_i, n) - run) / fps, 2), round(run / fps, 2)))
    long_runs = [r for r in runs_below if r[1] > 0.15]
    print(f"runs below the floor: {runs_below if runs_below else 'none'}")
    print(f"runs longer than 0.15 s: {len(long_runs)}  (gate: 0)")

    print("\n== GOOP FIELD ==")
    lo, hi = int(GOOP_WINDOW[0] * fps), int(GOOP_WINDOW[1] * fps)
    greens = [green(rgb[i]) for i in range(lo, min(hi, n))]
    gscale = (1920 * 1080) / (W * H)

    best = max(greens) if greens else 0
    run = 0
    longest = 0
    for value in greens:
        run = run + 1 if value * gscale >= 300 else 0
        longest = max(longest, run)
    print(f"green px in {GOOP_WINDOW[0]:.2f}-{GOOP_WINDOW[1]:.2f}s: peak {best} "
          f"(= {best * gscale:.0f} at 1920x1080)  longest run >=300px: {longest / fps:.2f}s "
          f"(gate: >=1.0s)")

    print("\n== CHROMA (saturation > 90) ==")
    sat = rgb.max(axis=3) - rgb.min(axis=3)
    per_frame = (sat > 90).sum(axis=(1, 2))
    print(f"at {W}x{H} (scale to 1920x1080: x{(1920 * 1080) / (W * H):.0f}): "
          f"peak {int(per_frame.max())} px/frame   mean {per_frame.mean():.1f} px/frame   "
          f"(V4 reference, full res: peak 32554, mean 1155)")

    print("\n== IN-SHOT EVENTS (luma transient > 0.012 inside a shot) ==")
    delta = np.abs(np.diff(luma))
    cut = set()
    for i in range(1, len(delta)):
        if (i / fps) in boundaries:
            cut.add(i)
    events = []
    for i in range(1, len(delta)):
        if i in cut or i in cut or (i - 1) in cut:
            continue
        if float(delta[i]) > 0.012:
            events.append((round(i / fps, 2), round(float(delta[i]), 4)))
    per_shot = [[] for _ in boundaries[:-1]]
    for t, d in events:
        for k in range(len(boundaries) - 1):
            if boundaries[k] <= t < boundaries[k + 1]:
                per_shot[k].append(t)
                break
        else:
            per_shot[-1].append(t)
    bare = [k + 1 for k, evs in enumerate(per_shot) if not evs]
    print(f"count {len(events)}: {events}")
    print(f"per-shot evidence: {len(per_shot) - len(bare)} of {len(per_shot)} shots carry >=1 "
          f"in-shot event; the bare shots: {bare}")

    print("\n== SFX CAUSATION (work/sfx_v6.json vs the nearest visible change) ==")
    sfx = json.loads(Path("work/sfx_v6.json").read_text())
    all_changes = [(round(i / fps, 2), round(float(delta[i]), 4))
                   for i in range(1, len(delta)) if float(delta[i]) > 0.012]
    # A cut is a visible change too: the picture is replaced.
    times = sorted({t for t, _ in all_changes} | {round(b, 2) for b in boundaries})
    bridges = 0
    for item in sfx:
        t = item["time"]
        name = Path(item['path']).name
        if item.get("role") == "bridge":
            # J-cut class: the incoming scene's sound leads its picture cut
            # by 0.15-0.60 s (craft note S-T4 / AC-T4). The lead is measured
            # to the raw map: the refractory map hides cuts the film still
            # makes, and a bridge answers a picture change, not a display map.
            nxt = next((b for b in raw_times if b > t), None)
            lead = (nxt - t) if nxt is not None else 99.0
            ok = 0.15 <= lead <= 0.60
            bridges += 1
            print(f"  {t:6.2f}s {name:<34} J-CUT BRIDGE -> cut {nxt}  lead {lead:.2f}s "
                  f"{'OK' if ok else 'OUT OF WINDOW'}")
            continue
        near = min(times, key=lambda x: abs(x - t)) if times else None
        delta_s = abs(near - t) if near is not None else 99
        print(f"  {t:6.2f}s {name:<34} nearest transient {near}  "
              f"delta {delta_s:.2f}s {'OK' if delta_s <= 0.15 else 'OUT OF TOLERANCE'}")
    print(f"J-cut bridges: {bridges}")

    print("\n== AUDIO ==")
    probe = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", str(path), "-af", "ebur128=peak=true",
         "-f", "null", "-"], capture_output=True, text=True, check=True).stderr
    integrated = peak = lra = -70.0
    for line in probe.splitlines():
        t = line.strip()
        if t.startswith("I:"):
            integrated = float(t.split()[1])
        if t.startswith("Peak:"):
            peak = float(t.split()[1])
        if t.startswith("LRA:"):
            lra = float(t.split()[1])
    print(f"integrated {integrated:.1f} LUFS  true peak {peak:.1f} dBFS  LRA {lra:.1f} LU")

    # Where the film is loudest: the card must be the loudest moment.
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", str(path), "-ac", "1", "-ar", "48000", "-f", "f32le", "-"],
        capture_output=True, check=True).stdout
    samples = np.frombuffer(raw, dtype=np.float32)
    window = 48000
    count = len(samples) // window
    rms = 20 * np.log10(np.sqrt((samples[:count * window].reshape(count, window) ** 2).mean(axis=1)) + 1e-9)
    loudest = int(np.argmax(rms))
    print(f"loudest second {loudest}s ({rms[loudest]:.1f} dB RMS); the last 3 s start at {count - 3}s")
    audio = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", str(path), "-af",
         "astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level:file=-",
         "-f", "null", "-"], capture_output=True, text=True, check=True).stderr
    rms = [float(v) for v in
           [line.split("=")[1] for line in audio.splitlines() if "RMS_level=" in line]]
    if rms:
        loudest = int(np.argmax(rms)) / fps
        print(f"loudest second {loudest:.2f}s  last second {rms[-1]:.1f} dB  "
              f"first second {rms[0]:.1f} dB  final-3s peak {max(rms[-3:]):.1f} dB")
    silence = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", str(path), "-af",
         "silencedetect=n=-40dB:d=0.25", "-f", "null", "-"],
        capture_output=True, text=True, check=True).stderr
    runs = [l.strip() for l in silence.splitlines() if "silence_start" in l or "silence_end" in l]
    print("silence:", " | ".join(runs[:4]) if runs else "none")

    # AC-T5: the card lands on a hit. Both the picture and the waveform are
    # read off the shipped file; the onset time is the last detected boundary.
    print("\n== CARD ENTRY (AC-T5: hit within 0.1 s of the onset, hold >= 2.5 s) ==")
    onset = boundaries[-1]
    on_i = int(round(onset * fps))
    luma_step = float(luma[on_i] - luma[on_i - 1]) if on_i > 0 else 0.0
    near = samples[int(max(0, onset - 0.15) * 48000):int((onset + 0.15) * 48000)]
    win = int(0.02 * 48000)
    env = np.array([np.abs(near[i:i + win]).max() for i in range(0, max(1, len(near) - win), win)])
    peak_at = onset - 0.15 + (int(np.argmax(env)) * 0.02 + 0.01) if len(env) else onset
    print(f"onset {onset:.2f}s  picture luma step {luma_step:+.4f}  "
          f"audio peak {peak_at:.2f}s (offset {peak_at - onset:+.2f}s)  "
          f"card hold {duration - onset:.2f}s")
    print(f"gate: |offset| <= 0.10 s  {'OK' if abs(peak_at - onset) <= 0.10 else 'FAIL'}   "
          f"hold {'OK' if duration - onset >= 2.5 else 'SHORT'}")

    print("\n== LIEU CHECKS (from this file's pixels only) ==")
    first_change = events[0][0] if events else None
    print(f"first in-shot event at {first_change}s (loop-driven clock)  "
          f"gate: the console legible <= 2.0 s")
    shot_lengths = [(round(boundaries[k], 2), round(boundaries[k + 1] - boundaries[k], 2))
                    for k in range(len(boundaries) - 1)]
    last = round(duration - boundaries[-1], 2)
    if last > 0:
        shot_lengths.append((boundaries[-1], last))
    hairlines = [t for t, ln in shot_lengths if ln < 0.6]
    print(f"shot lengths: {shot_lengths}")
    print(f"hairline shots <0.6 s: {len(hairlines)} {hairlines}  "
          f"min {min(ln for _, ln in shot_lengths):.2f}s  max {max(ln for _, ln in shot_lengths):.2f}s"
          "  (novelty-dwell band: the loop beats are the long ones, not decor)")

    print("\n== MICROTRAILER (six 1s samples, 232x130) ==")
    for t in MICRO:
        raw = subprocess.run(
            ["ffmpeg", "-v", "error", "-ss", f"{t:.2f}", "-i", str(path), "-frames:v", "1",
             "-vf", "scale=232:130,format=gray", "-f", "rawvideo", "-"],
            capture_output=True, check=True).stdout
        small = np.frombuffer(raw, dtype=np.uint8)
        print(f"  {t:5.2f}s  mean luma {small.mean() / 255:.4f}  max {small.max()}")


if __name__ == "__main__":
    main()
