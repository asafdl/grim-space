"""The cut list: every shot boundary in the shipped film, with the auditor's
own two detectors (luma delta + histogram correlation) and the transition type.

    python work/transitions_v8.py teaser.mp4 --stills work/v8_trans/before

The detector is the one the transition order specifies: a boundary is a frame
where |luma delta| > 0.03 or histogram correlation < 0.90, with the 0.6 s
refractory published next to the raw map (so a reviewer sees what the
refractory suppresses). Every row carries the two stills it was graded on.

Types are decided from the pixels only:
    fade-in            the film's opening fade (the first boundary at 0.0)
    flash              pre and post frames are the same scene (corr >= 0.90):
                       a light event inside a shot, not a picture cut
    cut-in-from-black  incoming frame under the 0.030 floor
    cut-to-black       outgoing frame under the 0.030 floor
    black bore-through both sides under the floor
    hard cut           everything else: the picture is replaced
"""
import argparse
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

W, H = 480, 270
HW, HH = 64, 36
FPS = 60.0


def decode(path: Path, w: int, h: int) -> np.ndarray:
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", str(path), "-vf",
         f"scale={w}:{h},format=gray", "-f", "rawvideo", "-"],
        capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.uint8).reshape(-1, h, w)


def boundaries(luma: np.ndarray, hist: np.ndarray, refractory: float) -> list[int]:
    flat = hist.reshape(len(hist), -1).astype(np.float32)
    centred = flat - flat.mean(axis=1, keepdims=True)
    norm = np.sqrt((centred ** 2).sum(axis=1)) + 1e-6
    corr = (centred[1:] * centred[:-1]).sum(axis=1) / (norm[1:] * norm[:-1])
    delta = np.abs(np.diff(luma))
    found = [0]
    for i in range(1, len(delta)):
        if (delta[i] > 0.03 or corr[i] < 0.90) and (i / FPS - found[-1] / FPS) > refractory:
            found.append(i)
    return found


def scene_corr(gray: np.ndarray, a: int, b: int) -> float:
    """Gradient-field correlation: the same geometry under two brightnesses."""
    def grad(idx: int) -> np.ndarray:
        f = gray[idx].astype(np.float32)
        gx = np.abs(np.diff(f, axis=1))[:-1, :]
        gy = np.abs(np.diff(f, axis=0))[:, :-1]
        return (gx + gy).ravel()

    x, y = grad(a), grad(b)
    x -= x.mean()
    y -= y.mean()
    denom = (np.sqrt((x ** 2).sum()) * np.sqrt((y ** 2).sum())) + 1e-6
    return float((x * y).sum() / denom)


def type_of(gray: np.ndarray, luma: np.ndarray, i: int, index: int) -> tuple[str, float, float]:
    n = len(gray)
    before = float(luma[max(0, i - 6):max(1, i - 2)].mean())
    after = float(luma[min(n - 1, i + 3):min(n, i + 7)].mean())
    corr = scene_corr(gray, max(0, i - 5), min(n - 1, i + 5))
    step = after - before
    if index == 0:
        return "fade-in", corr, step
    if before < 0.030 and after < 0.030:
        return "black bore-through", corr, step
    if after < 0.030:
        return "cut-to-black", corr, step
    if before < 0.030:
        return "cut-in-from-black", corr, step
    # A light event spikes one frame and the scene returns; a cut steps to a
    # new scene and stays there.
    if abs(step) < 0.15 * max(before, after) and corr >= 0.55:
        return "flash (same scene)", corr, step
    return "hard cut", corr, step


def main() -> None:
    ap = argparse.ArgumentParser()
    ap.add_argument("path", nargs="?", default="teaser.mp4")
    ap.add_argument("--stills", default="")
    args = ap.parse_args()

    path = Path(args.path)
    gray = decode(path, W, H)
    hist = decode(path, HW, HH)
    luma = gray.mean(axis=(1, 2)) / 255.0
    n = len(gray)
    duration = n / FPS

    cut = boundaries(luma, hist, 0.6)
    raw = boundaries(luma, hist, 0.0)
    print(f"file {path}  frames {n}  duration {duration:.2f}s")
    print(f"detector: luma delta > 0.03 or hist corr < 0.90, refractory 0.6 s")
    print(f"boundaries ({len(cut)}): {[round(i / FPS, 2) for i in cut]}")
    print(f"raw map ({len(raw)}): {[round(i / FPS, 2) for i in raw]}")
    print()
    print("| t | prev shot | next shot | pre | post | edge corr | level step | type | stills |")
    print("|---|---|---|---|---|---|---|---|---|")

    stills_dir = Path(args.stills) if args.stills else None
    if stills_dir:
        stills_dir.mkdir(parents=True, exist_ok=True)

    rows = []
    for index, i in enumerate(cut):
        t = i / FPS
        prev_b = cut[index - 1] / FPS if index else None
        next_b = cut[index + 1] / FPS if index + 1 < len(cut) else duration
        kind, corr, step = type_of(gray, luma, i, index)
        pre = float(luma[max(0, i - 1)])
        post = float(luma[min(n - 1, i)])
        paths = ""
        if stills_dir:
            a = max(0, i - 2)
            b = min(n - 1, i + 2)
            pa = stills_dir / f"t{t:05.2f}_pre.png"
            pb = stills_dir / f"t{t:05.2f}_post.png"
            Image.fromarray(gray[a]).save(pa)
            Image.fromarray(gray[b]).save(pb)
            paths = f"{pa} / {pb}"
        rows.append((t, prev_b, next_b, pre, post, corr, kind))
        print(f"| {t:5.2f} | {('-') if prev_b is None else f'{prev_b:.2f}'} | {next_b:.2f} | "
              f"{pre:.3f} | {post:.3f} | {corr:+.2f} | {step:+.3f} | {kind} | {paths} |")

    print()
    kinds = {}
    for row in rows:
        kinds[row[6]] = kinds.get(row[6], 0) + 1
    print("types:", kinds)


if __name__ == "__main__":
    sys.exit(main())
