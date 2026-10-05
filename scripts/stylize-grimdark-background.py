#!/usr/bin/env python3
"""Apply the project's grimdark, faceted, hard-edge treatment to one image.

Requires ffmpeg and ffprobe on PATH. The source image is never overwritten.

Usage:
  python3 scripts/stylize-grimdark-background.py assets/backgrounds/Hangar.png
  python3 scripts/stylize-grimdark-background.py input.jpg output.png
  python3 scripts/stylize-grimdark-background.py input.jpg output.png --force
"""

from __future__ import annotations

import argparse
import json
import shutil
import subprocess
import sys
from pathlib import Path


def find_tool(name: str) -> str:
    path = shutil.which(name)
    if not path:
        sys.exit(f"error: {name} not found on PATH")
    return path


def probe_dimensions(ffprobe: str, source: Path) -> tuple[int, int]:
    command = [
        ffprobe,
        "-v",
        "error",
        "-select_streams",
        "v:0",
        "-show_entries",
        "stream=width,height",
        "-of",
        "json",
        str(source),
    ]
    try:
        result = subprocess.run(
            command,
            check=True,
            capture_output=True,
            text=True,
        )
        stream = json.loads(result.stdout)["streams"][0]
        width = int(stream["width"])
        height = int(stream["height"])
    except (subprocess.CalledProcessError, KeyError, IndexError, ValueError, json.JSONDecodeError) as error:
        detail = error.stderr.strip() if isinstance(error, subprocess.CalledProcessError) else str(error)
        sys.exit(f"error: could not read image dimensions for {source}: {detail}")

    if width < 2 or height < 2:
        sys.exit(f"error: image is too small to stylize: {width}x{height}")
    return width, height


def build_filter(width: int, height: int) -> str:
    half_width = max(1, width // 2)
    half_height = max(1, height // 2)
    edge_width = max(1, round(width * 5 / 12))
    edge_height = max(1, round(height * 5 / 12))

    return (
        "[0:v]format=gbrp,"
        "colorbalance="
        "rs=-0.025:gs=0.005:bs=0.04:"
        "rm=-0.012:gm=-0.004:bm=0.015:"
        "rh=0.02:gh=0.0:bh=-0.018,"
        "eq=contrast=1.08:brightness=-0.018:saturation=0.72:"
        "gamma=0.98:gamma_weight=0.88,"
        "curves="
        "all='0/0 0.12/0.085 0.45/0.40 0.78/0.74 1/0.96':"
        "red='0/0 0.4/0.39 0.72/0.75 1/1',"
        "vignette=PI/7:eval=frame,"
        "noise=alls=1.4:allf=t+u,"
        f"scale={half_width}:{half_height}:flags=lanczos,"
        "bilateral=sigmaS=2.4:sigmaR=0.075:planes=7,"
        "elbg=codebook_length=128:nb_steps=3:seed=47,"
        "unsharp=5:5:0.65:3:3:0.2,"
        f"scale={width}:{height}:flags=lanczos,"
        "format=gbrp,split=2[base][edge];"
        f"[edge]scale={edge_width}:{edge_height}:flags=lanczos,"
        "format=gray,"
        "edgedetect=low=0.045:high=0.12:mode=wires,"
        "negate,"
        f"scale={width}:{height}:flags=lanczos,"
        "format=gbrp[ink];"
        "[base][ink]blend=all_mode=multiply:all_opacity=0.62,"
        "unsharp=5:5:1.15:3:3:0.4,"
        "format=rgb24[out]"
    )


def stylize(ffmpeg: str, source: Path, destination: Path, width: int, height: int) -> None:
    command = [
        ffmpeg,
        "-hide_banner",
        "-loglevel",
        "error",
        "-y",
        "-i",
        str(source),
        "-filter_complex",
        build_filter(width, height),
        "-map",
        "[out]",
        "-frames:v",
        "1",
        "-c:v",
        "png",
        "-pred",
        "mixed",
        str(destination),
    ]
    subprocess.run(command, check=True)


def main() -> None:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument("input", type=Path, help="Source image")
    parser.add_argument(
        "output",
        type=Path,
        nargs="?",
        help="Output PNG (default: <input>-grimdark-hard-edges.png)",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Replace the output if it already exists",
    )
    args = parser.parse_args()

    source = args.input.expanduser().resolve()
    if not source.is_file():
        sys.exit(f"error: input image does not exist: {source}")

    destination = (
        args.output.expanduser().resolve()
        if args.output
        else source.with_name(f"{source.stem}-grimdark-hard-edges.png")
    )
    if destination == source:
        sys.exit("error: output must not overwrite the source image")
    if destination.suffix.lower() != ".png":
        sys.exit("error: output must use the .png extension")
    if not destination.parent.is_dir():
        sys.exit(f"error: output directory does not exist: {destination.parent}")
    if destination.exists() and not args.force:
        sys.exit(f"error: output already exists (pass --force to replace it): {destination}")

    ffmpeg = find_tool("ffmpeg")
    ffprobe = find_tool("ffprobe")
    width, height = probe_dimensions(ffprobe, source)

    try:
        stylize(ffmpeg, source, destination, width, height)
    except subprocess.CalledProcessError as error:
        sys.exit(f"error: ffmpeg failed with exit code {error.returncode}")

    print(f"created {destination} ({width}x{height})")


if __name__ == "__main__":
    main()
