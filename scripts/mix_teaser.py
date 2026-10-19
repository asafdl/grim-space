"""Mix the teaser: music bed plus game SFX placed on measured frames.

The music is a window of one track; the SFX are placed at the times measured
from the picture, never guessed. The mix is normalised to the web target of
-14 LUFS with a true-peak limiter.
"""
import argparse
import json
import re
import subprocess
from pathlib import Path

MUSIC = Path("assets/music/terraforming-mars-tracks")
SFX = Path("assets/sfx")

TARGET_LUFS = -14.0


def run(args: list[str]) -> None:
    subprocess.run(args, check=True)


def build_music(track: str, start: float, duration: float, out: Path) -> None:
    run([
        "ffmpeg", "-y", "-v", "error",
        "-ss", f"{start:.3f}", "-t", f"{duration:.3f}",
        "-i", str(MUSIC / track),
        "-af", "afade=t=in:st=0:d=0.35,afade=t=out:st={:.3f}:d=1.2".format(max(duration - 1.2, 0)),
        "-ar", "48000", "-ac", "2", str(out),
    ])


def build_sfx(events: list[dict], out: Path) -> None:
    """One input per event, delayed to its time, then mixed without normalising."""
    if not events:
        out.write_bytes(b"")
        return

    args = ["ffmpeg", "-y", "-v", "error"]
    for event in events:
        args += ["-i", str(event["path"])]

    chains = []
    labels = []
    for index, event in enumerate(events):
        delay = int(round(event["time"] * 1000))
        gain = event.get("gain_db", 0.0)
        volume = f"volume={gain:.1f}dB" if gain else "anull"
        chains.append(f"[{index}:a]{volume},adelay={delay}|{delay}[a{index}]")
        labels.append(f"[a{index}]")

    filter_complex = (
        ";".join(chains)
        + ";"
        + "".join(labels)
        + f"amix=inputs={len(events)}:normalize=0:dropout_transition=0[out]"
    )

    args += [
        "-filter_complex", filter_complex,
        "-map", "[out]",
        "-ar", "48000", "-ac", "2", str(out),
    ]
    run(args)


def measure_lufs(path: Path) -> tuple[float, float]:
    """Integrated loudness and true peak of a file, measured with EBU R128."""
    result = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", str(path),
         "-af", "ebur128=peak=true", "-f", "null", "-"],
        capture_output=True, text=True, check=True)
    text = result.stderr
    integrated = -70.0
    peak = -70.0
    lines = text.splitlines()
    for index, line in enumerate(lines):
        stripped = line.strip()
        if stripped.startswith("I:"):
            for key, value in (("integrated", integrated), ("peak", peak)):
                del key, value
            try:
                integrated = float(stripped.split()[1])
            except (IndexError, ValueError):
                pass
        if stripped.startswith("Peak:"):
            try:
                peak = float(stripped.split()[1])
            except (IndexError, ValueError):
                pass
    return integrated, peak


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--video", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--track", default="track-15.ogg")
    parser.add_argument("--music-start", type=float, required=True)
    parser.add_argument("--sfx", default="[]", help="JSON list of {path,time,gain_db}")
    args = parser.parse_args()

    video = Path(args.video)
    out = Path(args.out)
    work = out.parent / (out.stem + "_work")
    work.mkdir(parents=True, exist_ok=True)

    duration = float(subprocess.run(
        ["ffprobe", "-v", "error", "-show_entries", "format=duration",
         "-of", "default=nw=1:nk=1", str(video)],
        capture_output=True, check=True, text=True).stdout.strip())

    music = work / "music.wav"
    build_music(args.track, args.music_start, duration, music)

    events = json.loads(args.sfx)
    sfx_file = work / "sfx.wav"
    build_sfx(events, sfx_file)

    # Pass 1: the raw mix, measured for loudness.
    raw = work / "raw.wav"
    inputs = ["-i", str(music)]
    if sfx_file.exists() and sfx_file.stat().st_size > 0:
        inputs += ["-i", str(sfx_file)]
    else:
        events = []

    mix_filter = (
        "[0:a]volume=-3dB[m];"
        + (f"[1:a]volume=-3dB[s];" if events else "")
        + ("[m][s]amix=inputs=2:normalize=0:dropout_transition=0[out]"
           if events else "[m]anull[out]")
    )
    run(["ffmpeg", "-y", "-v", "error", *inputs,
         "-filter_complex", mix_filter, "-map", "[out]",
         "-ar", "48000", "-ac", "2", str(raw)])

    # Pass 2: the measured gain that lands the mix on the web target. The limiter
    # lifts the integrated loudness a little, so the gain is applied once, the
    # limited result is measured, and the difference is corrected on the audio
    # stem - the picture is encoded once.
    integrated, _ = measure_lufs(raw)
    gain = TARGET_LUFS - integrated
    print(f"raw mix measured {integrated:.2f} LUFS -> applying {gain:+.2f} dB")

    limited = work / "limited.wav"
    run([
        "ffmpeg", "-y", "-v", "error",
        "-i", str(raw),
        "-af", f"volume={gain:.2f}dB,alimiter=limit=0.89",
        "-ar", "48000", "-ac", "2", str(limited),
    ])

    limited_i, limited_p = measure_lufs(limited)
    correction = TARGET_LUFS - limited_i
    print(f"limited mix measured {limited_i:.2f} LUFS -> correcting {correction:+.2f} dB")

    run([
        "ffmpeg", "-y", "-v", "error",
        "-i", str(video), "-i", str(limited),
        "-filter_complex",
        f"[1:a]volume={correction:.2f}dB[a]",
        "-map", "0:v", "-map", "[a]",
        "-c:v", "libx264", "-preset", "medium", "-crf", "21",
        "-pix_fmt", "yuv420p", "-profile:v", "high",
        "-color_primaries", "bt709", "-color_trc", "bt709", "-colorspace", "bt709",
        "-color_range", "tv",
        "-c:a", "aac", "-b:a", "192k", "-ar", "48000", "-ac", "2",
        "-shortest", str(out),
    ])

    final_i, final_p = measure_lufs(out)
    print(
        f"wrote {out}: {final_i:.1f} LUFS integrated, {final_p:.1f} dBFS true peak"
        f" ({duration:.3f}s picture)")


if __name__ == "__main__":
    main()
