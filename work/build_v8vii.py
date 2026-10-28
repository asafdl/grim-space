"""Build teaser v8vii: user-ruled slow film-speed fades at ALL section boundaries.

USER EYE VERDICT (v8vi, fd72b294): 'not all transition are slow, the one that
are, are good' = the 3 slow fades (7.40/16.98/27.38) APPROVED as the shape;
the remaining boundaries read as too-fast because they are still hard cuts.

ORDER: apply the APPROVED fade shape to ALL 16 internal boundaries - every cut
returns a slow film-speed fade (ramp 45-60 frames / 0.75-1.0s @60fps, smooth
ease in+out, bottom near-black <=0.10 luma, never gray).

Method: segment+concat on the v8vi film base (work/v8i_cut.mp4 = clean hard cuts).
Every internal boundary gets a dim-out piece (outgoing segment fades to
near-black over the last ~26f) + a dim-in piece (incoming segment fades from
near-black over the first ~26f). Where two boundaries are closer than 52f
apart, the per-segment fade half-width is capped at half the segment duration
so dim-in and dim-out never overlap within a segment.

Keeps room tone / motion / loudness by copying audio from teaser.mp4.
"""
import subprocess
import numpy as np
from pathlib import Path

SRC = "work/v8i_cut.mp4"        # hard cuts at all 16 internal boundaries
TEASER = "teaser.mp4"           # current committed mix (room tone already in it)
OUT_DIR = Path("work/v8vii")
OUT_DIR.mkdir(parents=True, exist_ok=True)
OUT_CUT = OUT_DIR / "teaser_v8vii_cut.mp4"
OUT_MP4 = OUT_DIR / "teaser_v8vii.mp4"

DURATION = 34.63
FPS = 60.0
BOUNDS = [0.0, 1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32,
          14.17, 16.98, 20.47, 23.52, 25.98, 27.38, 30.68, 34.63]
# All internal boundaries are now slow fades (was only {7.40, 16.98, 27.38} in v8vi)
# total ramp 52 frames = 26 frames each side @60fps = ~0.87 s (45-60 frame law)
DIP_HALF = 26 / FPS             # fade half-width each side of a boundary
DIP_FLOOR = 0.00                # fade-to-black bottom (near-black, <= 0.10)


def run(cmd, label):
    print(f"=== {label} ===")
    r = subprocess.run(cmd, capture_output=True, text=True)
    print("rc:", r.returncode)
    if r.returncode != 0:
        print(r.stderr[-2000:])
        return False
    return True


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def main():
    # 1) build pieces: every internal boundary gets dim-out (left) + dim-in (right)
    pieces = []
    n_seg = len(BOUNDS) - 1
    for i in range(n_seg):
        s, e = BOUNDS[i], BOUNDS[i + 1]
        seg_dur = e - s
        has_left = i > 0      # dip-in at start (incoming from previous boundary)
        has_right = i < n_seg - 1  # dip-out at end (outgoing to next boundary)

        if has_left and has_right:
            max_half = seg_dur / 2.0
            di_dur = min(DIP_HALF, max_half)
            do_dur = min(DIP_HALF, max_half)
            if di_dur + do_dur > seg_dur:
                scale = seg_dur / (di_dur + do_dur)
                di_dur *= scale
                do_dur *= scale
            if di_dur > 0:
                pieces.append((s, s + di_dur, "dim-in", i))
            mid_s, mid_e = s + di_dur, e - do_dur
            if mid_e > mid_s:
                pieces.append((mid_s, mid_e, "normal", i))
            if do_dur > 0:
                pieces.append((e - do_dur, e, "dim-out", i))
        elif has_left:  # last segment: only dim-in at start
            di_dur = min(DIP_HALF, seg_dur)
            pieces.append((s, s + di_dur, "dim-in", i))
            if e > s + di_dur:
                pieces.append((s + di_dur, e, "normal", i))
        elif has_right:  # first segment: only dim-out at end
            do_dur = min(DIP_HALF, seg_dur)
            if e - do_dur > s:
                pieces.append((s, e - do_dur, "normal", i))
            pieces.append((e - do_dur, e, "dim-out", i))
        else:
            pieces.append((s, e, "normal", i))

    # 2) build filter_complex
    chains = []
    labels = []
    for pi, (ps, pe, kind, si) in enumerate(pieces):
        label = f"p{pi}"
        labels.append(label)
        parts = [f"[0:v]trim=start={ps:.6f}:end={pe:.6f}", "setpts=PTS-STARTPTS"]
        if kind == "dim-out":
            parts.append(f"fade=out:start_time=0:duration={pe - ps:.6f}")
        elif kind == "dim-in":
            parts.append(f"fade=in:start_time=0:duration={pe - ps:.6f}")
        chains.append(",".join(parts) + f"[{label}]")

    concat_inputs = "".join(f"[{l}]" for l in labels)
    filter_complex = ";".join(chains) + f";{concat_inputs}concat=n={len(labels)}:v=1:a=0[outv]"
    print(f"\nFilter complex ({len(filter_complex)} chars), {len(labels)} pieces, "
          f"{n_seg} segments, {n_seg - 1} internal boundaries")

    # Step 1: video
    cmd = ["ffmpeg", "-hide_banner", "-nostats", "-y", "-i", SRC,
           "-filter_complex", filter_complex, "-map", "[outv]",
           "-c:v", "libx264", "-crf", "20", "-preset", "medium",
           "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-r", "60",
           str(OUT_CUT)]
    if not run(cmd, "Step 1: Render video (slow fades at ALL boundaries)"):
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
