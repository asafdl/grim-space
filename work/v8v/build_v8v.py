"""Build teaser v8v: user-law slow dark dips at section changes.

User-eye verdict on v8iv: dips bottom at luma 0.278 (gray band reads as a
screen flash) and run too fast (4-6 frames). USER LAW REFUTES the
exposure-matched-gray-floor law: gray dip floors are BANNED.

Dips ONLY at section-change boundaries [7.40, 16.98, 27.38] -- slow dip
~12 frames total (6 frames each side @60fps) fading to NEAR-BLACK (luma 0),
a natural eye-blink, NOT a gray floor. Smooth ease-in/ease-out (V-shape),
no rectangle look, no flat bottom.
ALL other boundaries: plain hard cuts with per-shot chroma matching (lutrgb).
Keeps room tone 28.31-30.21 (-15 dB class) by copying audio from teaser.mp4.
"""
import subprocess
import numpy as np
from pathlib import Path

SRC = "work/v8i_cut.mp4"        # hard cuts at all 16 boundaries (matches v8iv)
TEASER = "teaser.mp4"           # has room-tone audio already
OUT_DIR = Path("work/v8v")
OUT_DIR.mkdir(parents=True, exist_ok=True)
OUT_CUT = OUT_DIR / "teaser_v8v_cut.mp4"
OUT_MP4 = OUT_DIR / "teaser_v8v.mp4"

DURATION = 34.63
FPS = 60.0
BOUNDS = [0.0, 1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32,
          14.17, 16.98, 20.47, 23.52, 25.98, 27.38, 30.68, 34.63]
DIPS = {7.40, 16.98, 27.38}        # section-change boundaries -> slow dark dip
DIP_HALF = 0.10                    # dip half-width (6 frames @60fps each side),
                                   # total dip ~12 frames, bottom at boundary = black
DIP_FLOOR = 0.00                   # fade-to-black bottom (near-black, <= 0.10)

# plain cuts needing chroma match (dChroma > 0.12 from midpoint measurement)
MATCH_CUTS = {1.92, 3.90, 14.17, 20.47, 23.52, 25.98}
GAIN_CLIP = (0.6, 1.6)


def run(cmd, label):
    print(f"=== {label} ===")
    r = subprocess.run(cmd, capture_output=True, text=True)
    print("rc:", r.returncode)
    if r.returncode != 0:
        print(r.stderr[-2000:])
        return False
    return True


def rgb_mean(t):
    """Mean R,G,B (0-255) at time t."""
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-y", "-ss", f"{t:.4f}", "-i", SRC,
         "-frames:v", "1", "-vf", "scale=60:34,format=rgb24", "-f", "rawvideo",
         "-pix_fmt", "rgb24", "-"],
        capture_output=True)
    d = np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)
    if d.size == 0:
        print(f"  WARN rgb_mean({t}): empty stdout")
        return None
    f = d.reshape(34, 60, 3)
    return (float(f[..., 0].mean()), float(f[..., 1].mean()), float(f[..., 2].mean()))


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def main():
    # 1) measure mean RGB per segment midpoint
    segs = []
    for i in range(len(BOUNDS) - 1):
        s, e = BOUNDS[i], BOUNDS[i + 1]
        mid = (s + e) / 2
        segs.append({"s": s, "e": e, "mid": mid, "rgb": rgb_mean(mid)})
        print(f"seg {i}: {s:6.2f}-{e:6.2f} mid {mid:5.2f} RGB={segs[-1]['rgb']}")

    # 2) build pieces
    # piece: (start, end, kind) kind in {'normal','dim-out','dim-in'}
    pieces = []
    for i, sg in enumerate(segs):
        s, e = sg["s"], sg["e"]
        if e in DIPS:
            pieces.append((s, e - DIP_HALF, "normal", i))
            pieces.append((e - DIP_HALF, e, "dim-out", i))
        elif s in DIPS:
            pieces.append((s, s + DIP_HALF, "dim-in", i))
            pieces.append((s + DIP_HALF, e, "normal", i))
        else:
            pieces.append((s, e, "normal", i))

    # 3) chroma-match gains for plain-cut incoming segments.
    gains = {0: (1.0, 1.0, 1.0)}
    for i, sg in enumerate(segs):
        if i == 0:
            continue
        s = sg["s"]
        if s in MATCH_CUTS and s not in DIPS:
            prev = segs[i - 1]["rgb"]
            curr = sg["rgb"]
            if prev and curr:
                ps = sum(prev); cs = sum(curr)
                chroma_p = [c / ps for c in prev]
                chroma_c = [c / cs for c in curr]
                raw = [chroma_p[c] / (chroma_c[c] + 1e-6) for c in range(3)]
                luma_after = sum(curr[c] * raw[c] for c in range(3))
                scale = cs / (luma_after + 1e-6)
                gains[i] = tuple(clamp(raw[c] * scale, *GAIN_CLIP) for c in range(3))
            else:
                gains[i] = (1.0, 1.0, 1.0)
        else:
            gains[i] = (1.0, 1.0, 1.0)

    print("\nGains:", {k: tuple(round(v, 3) for v in gains[k]) for k in gains if gains[k] != (1.0, 1.0, 1.0)})

    # 4) build filter_complex
    chains = []
    labels = []
    for pi, (ps, pe, kind, si) in enumerate(pieces):
        label = f"p{pi}"
        labels.append(label)
        dur = pe - ps
        parts = [f"[0:v]trim=start={ps:.6f}:end={pe:.6f}", "setpts=PTS-STARTPTS"]
        gr, gg, gb = gains[si]
        if kind == "dim-out":
            # fade full -> black over the whole piece, ends black at the boundary
            parts.append(f"fade=out:start_time=0:duration={DIP_HALF:.6f}")
        elif kind == "dim-in":
            # fade black -> full over the whole piece, starts black at the boundary
            parts.append(f"fade=in:start_time=0:duration={DIP_HALF:.6f}")
        else:
            if gr != 1.0 or gg != 1.0 or gb != 1.0:
                parts.append(f"lutrgb=r='val*{gr:.4f}':g='val*{gg:.4f}':b='val*{gb:.4f}'")
        chains.append(",".join(parts) + f"[{label}]")

    concat_inputs = "".join(f"[{l}]" for l in labels)
    filter_complex = ";".join(chains) + f";{concat_inputs}concat=n={len(labels)}:v=1:a=0[outv]"
    print(f"\nFilter complex ({len(filter_complex)} chars), {len(labels)} pieces")

    # Step 1: video
    cmd = ["ffmpeg", "-hide_banner", "-nostats", "-y", "-i", SRC,
           "-filter_complex", filter_complex, "-map", "[outv]",
           "-c:v", "libx264", "-crf", "20", "-preset", "medium",
           "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-r", "60",
           str(OUT_CUT)]
    if not run(cmd, "Step 1: Render video (slow dark dips at section boundaries)"):
        return 1

    pr = subprocess.run(["ffprobe", "-v", "error", "-show_entries", "format=duration",
                         "-of", "default=noprint_wrappers=1", str(OUT_CUT)],
                        capture_output=True, text=True)
    print("Video duration:", pr.stdout.strip())

    # Step 2: mux with original room-tone audio (copy, untouched)
    cmd = ["ffmpeg", "-hide_banner", "-nostats", "-y",
           "-i", str(OUT_CUT), "-i", TEASER,
           "-map", "0:v:0", "-map", "1:a:0?",
           "-c:v", "copy", "-c:a", "copy",
           "-movflags", "+faststart", "-shortest",
           str(OUT_MP4)]
    if not run(cmd, "Step 2: Mux video + room-tone audio (copy)"):
        return 1

    # Step 3: promote to repo-root teaser.mp4
    cmd = ["ffmpeg", "-hide_banner", "-nostats", "-y",
           "-i", str(OUT_MP4), "-c", "copy", "-movflags", "+faststart",
           "teaser.mp4"]
    if not run(cmd, "Step 3: Promote to repo-root teaser.mp4"):
        return 1

    pr = subprocess.run(["ffprobe", "-v", "error", "-show_entries",
                         "stream=codec_type,codec_name:format=duration,size",
                         "-of", "default=noprint_wrappers=1", str(OUT_MP4)],
                        capture_output=True, text=True)
    print("\n=== FINAL FILE ===")
    print(pr.stdout)
    print(f"Outputs: {OUT_CUT}\n        {OUT_MP4}\n        teaser.mp4")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
