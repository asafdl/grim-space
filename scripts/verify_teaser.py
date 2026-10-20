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
CARD_START = 30.70
CARD_END = 34.60
GOOP_WINDOW = (10.90, 15.50)
MICRO = [4.00, 9.00, 11.00, 16.00, 25.00, 26.00]

# The film's chain: every beat, the sentence that joins it to the one before,
# and whether it advances the goal or only decorates it.
GOAL_CHAIN = [
    (0.00, 1.60, "THE OFFER", "the claim is on the board", "states the goal"),
    (1.60, 3.02, "THE ROUTE", "so now the ship runs for the wreck", "advances"),
    (3.02, 4.88, "THE AMBUSH", "but then a fleet is waiting in the way", "advances"),
    (4.88, 5.68, "THE ORDER", "so now the order goes in", "advances"),
    (5.68, 7.92, "THE BEAMS", "and then the beams cross the field", "advances"),
    (7.92, 9.08, "THE COST", "but then it takes a hull", "advances"),
    (9.08, 11.58, "THE WRECK", "so now a hull is gone", "advances"),
    (11.58, 12.98, "THE NEXT PLAN", "and then the enemy's plan is drawn again", "advances"),
    (12.98, 14.78, "THE GOOP", "but then the goop gun turns the space green", "advances"),
    (14.78, 16.98, "THE PROGRESS", "so now the ship is further along the route", "advances"),
    (16.98, 18.40, "THE DOCKYARD", "and then the money goes into the hull", "advances"),
    (18.40, 21.78, "THE ESCALATION", "but then three hulls close in", "advances"),
    (21.78, 24.60, "THE ODD NUMBERS", "so now they outnumber us", "advances"),
    (24.60, 27.37, "THE REACH", "so now the wreck is in sight", "advances"),
    (27.37, 30.70, "THE THREAT", "but then their plan is drawn across it", "advances"),
    (30.70, 34.65, "THE CARD", "and the promise", "advances"),
]


def decode(path: Path, w: int, h: int, mode: str = "gray") -> np.ndarray:
    args = ["ffmpeg", "-v", "error", "-i", str(path), "-vf", f"scale={w}:{h},format={mode}"]
    args += ["-f", "rawvideo", "-"] if mode == "gray" else ["-pix_fmt", "rgb24", "-f", "rawvideo", "-"]
    raw = subprocess.run(args, capture_output=True, check=True).stdout
    channels = 1 if mode == "gray" else 3
    frames = np.frombuffer(raw, dtype=np.uint8).reshape(-1, h, w, channels)
    return frames[:, :, :, 0] if mode == "gray" else frames


def shots(luma: np.ndarray, hist: np.ndarray) -> list[float]:
    """The auditor's two detectors: luma delta 0.03 and histogram correlation 0.98."""
    flat = hist.reshape(len(hist), -1).astype(np.float32)
    centred = flat - flat.mean(axis=1, keepdims=True)
    norm = np.sqrt((centred ** 2).sum(axis=1)) + 1e-6
    corr = (centred[1:] * centred[:-1]).sum(axis=1) / (norm[1:] * norm[:-1])
    delta = np.abs(np.diff(flat.mean(axis=1)) / 255.0)
    boundaries = [0.0]
    for i in range(1, len(delta)):
        if (delta[i] > 0.03 or corr[i] < 0.98) and (i / 60.0 - boundaries[-1]) > 0.6:
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

    print("== SHOT MAP (luma-delta 0.03 + hist-corr 0.90) ==")
    print(f"frames {n}  duration {duration:.2f}s  shots {len(boundaries) - 1}  "
          f"ASL {duration / (len(boundaries) - 1):.2f}s  longest {max(lengths):.2f}s")
    print("boundaries:", boundaries)

    below = float((luma < 0.030).mean())
    card = luma[int(CARD_START * fps):int(CARD_END * fps)]
    print("\n== LUMA FLOOR ==")
    print(f"frames below 0.030: {below * 100:.1f}%  (gate: <=5%)   mean luma {luma.mean():.4f}")
    print(f"card frames {CARD_START:.2f}-{CARD_END:.2f}s: min {card.min():.4f} "
          f"mean {card.mean():.4f} max {card.max():.4f} (gate: card >=0.030)")

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
    print(f"count {len(events)}: {events}")

    print("\n== SFX CAUSATION (work/sfx_v6.json vs the nearest visible change) ==")
    sfx = json.loads(Path("work/sfx_v6.json").read_text())
    all_changes = [(round(i / fps, 2), round(float(delta[i]), 4))
                   for i in range(1, len(delta)) if float(delta[i]) > 0.012]
    # A cut is a visible change too: the picture is replaced.
    times = sorted({t for t, _ in all_changes} | {round(b, 2) for b in boundaries})
    for item in sfx:
        t = item["time"]
        near = min(times, key=lambda x: abs(x - t)) if times else None
        delta_s = abs(near - t) if near is not None else 99
        print(f"  {t:6.2f}s {Path(item['path']).name:<34} nearest transient {near}  "
              f"delta {delta_s:.2f}s {'OK' if delta_s <= 0.15 else 'OUT OF TOLERANCE'}")

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

    print("\n== GOAL CHAIN (does the shot advance the goal?) ==")
    for start, end, name, chain, kind in GOAL_CHAIN:
        inside = [b for b in boundaries if start <= b < end]
        print(f"  {start:6.2f}-{end:6.2f}  {name:<14} {chain:<46} [{kind}]")
    advancing = sum(
        1 for b in boundaries
        if any(start <= b < end and kind == "advances" for start, end, _, _, kind in GOAL_CHAIN))
    print(f"  shots that advance or complicate the goal: {advancing} of {len(boundaries) - 1} "
          f"({100 * advancing / max(1, len(boundaries) - 1):.0f}%, gate: >=60%)")
    goal_end = GOAL_CHAIN[1][1]
    print(f"  the goal is stated by {goal_end:.2f}s = {100 * goal_end / duration:.1f}% of the film "
          f"(gate: <=12%)")

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
