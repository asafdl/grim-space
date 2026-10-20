"""Mix the V6 teaser: three music states, the game's SFX on measured frames, -14 LUFS.

The music bed is cut into three states with a deliberate drop before the
ending, so the film has an arc:

    state A  0.00 - 14.0   a low bed, under the battle
    state B  14.0 - 26.0   the bed opens up, under the run layer
    state C  26.0 - 28.3   the peak
    the drop 28.3 - 30.2   near-silence, the inhale before the card
    state C  30.2 - end    the peak, and the card

Every SFX is the game's own file and is placed on a frame where the picture
actually changes, so no sound sits over a frozen frame.
"""
import argparse
import json
import subprocess
from pathlib import Path

MUSIC = Path("assets/music/terraforming-mars-tracks")
SFX = Path("assets/sfx")
TARGET_LUFS = -14.0


def run(args: list[str]) -> None:
    subprocess.run(args, check=True)


def measure_lufs(path: Path) -> tuple[float, float]:
    result = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-i", str(path),
         "-af", "ebur128=peak=true", "-f", "null", "-"],
        capture_output=True, text=True, check=True)
    integrated = peak = -70.0
    for line in result.stderr.splitlines():
        stripped = line.strip()
        if stripped.startswith("I:"):
            integrated = float(stripped.split()[1])
        if stripped.startswith("Peak:"):
            peak = float(stripped.split()[1])
    return integrated, peak


# The music states, in film time: the window, the level in dB.
MUSIC_STATES = [
    (0.0, 14.0, -8.0),
    (14.0, 26.0, -3.0),
    (26.0, 28.3, 0.0),
    (28.3, 30.2, -40.0),
    (30.2, None, 2.0),
]


def build_music(track: str, start: float, duration: float, out: Path) -> None:
    """Cuts the track into the film's states.

    The states are built by trimming each window out of the track and
    concatenating the windows, not by evaluating a volume expression over
    the timeline: a seek leaves the timestamps at the track's own time, and
    an expression would read the whole bed as "after the end".
    """
    chains = []
    labels = []
    for index, (from_s, to_s, gain) in enumerate(MUSIC_STATES):
        window_start = start + from_s
        window_end = start + (to_s if to_s is not None else duration)
        chains.append(
            f"[0:a]atrim=start={window_start:.3f}:end={window_end:.3f},"
            f"asetpts=PTS-STARTPTS,volume={gain:.1f}dB[a{index}]"
        )
        labels.append(f"[a{index}]")

    filter_complex = (
        ";".join(chains)
        + ";"
        + "".join(labels)
        + f"concat=n={len(MUSIC_STATES)}:v=0:a=1,afade=t=in:st=0:d=0.4[out]"
    )

    run([
        "ffmpeg", "-y", "-v", "error",
        "-i", str(MUSIC / track),
        "-filter_complex", filter_complex,
        "-map", "[out]",
        "-ar", "48000", "-ac", "2", str(out),
    ])


def build_sfx(events: list[dict], out: Path) -> None:
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


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--video", required=True)
    parser.add_argument("--out", required=True)
    parser.add_argument("--track", default="track-15.ogg")
    parser.add_argument("--music-start", type=float, required=True)
    parser.add_argument("--sfx", default="[]")
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
    raw = work / "raw.wav"
    run(["ffmpeg", "-y", "-v", "error", *inputs,
         "-filter_complex", mix_filter, "-map", "[out]",
         "-ar", "48000", "-ac", "2", str(raw)])

    integrated, _ = measure_lufs(raw)
    gain = TARGET_LUFS - integrated
    print(f"raw mix measured {integrated:.2f} LUFS -> applying {gain:+.2f} dB")

    # The correction and the limiter are iterated from the raw mix: the
    # limiter takes the level back down, so one pass is not enough to land on
    # the target. The limiter does not auto-level, so the ceiling holds.
    limited = work / "limited.wav"
    total_gain = gain
    for attempt in range(5):
        run([
            "ffmpeg", "-y", "-v", "error",
            "-i", str(raw),
            "-af", f"volume={total_gain:.2f}dB,alimiter=limit=0.62:level=disabled",
            "-ar", "48000", "-ac", "2", str(limited),
        ])

        limited_i, limited_p = measure_lufs(limited)
        correction = TARGET_LUFS - limited_i
        print(f"pass {attempt}: {limited_i:.2f} LUFS, peak {limited_p:.1f} dBFS -> "
              f"correcting {correction:+.2f} dB")
        if abs(correction) < 0.05:
            break

        total_gain += correction

    run([
        "ffmpeg", "-y", "-v", "error",
        "-i", str(video), "-i", str(limited),
        "-filter_complex",
        f"[1:a]alimiter=limit=0.62:level=disabled[a]",
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
        f"wrote {out}: {final_i:.1f} LUFS integrated, {final_p:.1f} dBFS true peak "
        f"({duration:.3f}s picture)")


if __name__ == "__main__":
    main()
