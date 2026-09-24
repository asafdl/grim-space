#!/usr/bin/env python3
"""Re-encode terraforming-mars BGM to Ogg Vorbis and point Music.cs at .ogg files.

Requires ffmpeg on PATH. After running, open the project in Godot once so it
imports the new .ogg files (or run a headless import if you have that wired up).

Usage:
  python3 scripts/reencode-music-tracks.py              # encode + update Music.cs
  python3 scripts/reencode-music-tracks.py --dry-run
  python3 scripts/reencode-music-tracks.py --remove-mp3 # delete sources after success
"""

from __future__ import annotations

import argparse
import re
import shutil
import subprocess
import sys
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
TRACKS_DIR = REPO_ROOT / "assets" / "music" / "terraforming-mars-tracks"
MUSIC_CS = REPO_ROOT / "src" / "application" / "Music.cs"

TRACK_GLOB = "track-*.mp3"
VORBIS_QUALITY_DEFAULT = 5


def find_ffmpeg() -> str:
    path = shutil.which("ffmpeg")
    if not path:
        sys.exit("error: ffmpeg not found on PATH")
    return path


def list_mp3_tracks() -> list[Path]:
    tracks = sorted(TRACKS_DIR.glob(TRACK_GLOB))
    if not tracks:
        sys.exit(f"error: no files matching {TRACK_GLOB} in {TRACKS_DIR}")
    return tracks


def encode_track(ffmpeg: str, src: Path, dst: Path, quality: int, dry_run: bool) -> None:
    if dst.exists() and dst.stat().st_mtime >= src.stat().st_mtime and not dry_run:
        print(f"skip (up to date): {dst.name}")
        return

    cmd = [
        ffmpeg,
        "-hide_banner",
        "-loglevel",
        "error",
        "-y",
        "-i",
        str(src),
        "-vn",
        "-c:a",
        "vorbis",
        "-strict",
        "-2",
        "-q:a",
        str(quality),
        str(dst),
    ]
    if dry_run:
        print("would run:", " ".join(cmd))
        return

    subprocess.run(cmd, check=True)
    before = src.stat().st_size
    after = dst.stat().st_size
    pct = (1 - after / before) * 100 if before else 0
    print(f"encoded {src.name} -> {dst.name} ({before // 1024} KiB -> {after // 1024} KiB, {pct:.0f}% smaller)")


def update_music_cs(extension: str, dry_run: bool) -> None:
    text = MUSIC_CS.read_text(encoding="utf-8")
    original = text

    if "TrackExtension" not in text:
        text, n = re.subn(
            r'(private const string TrackPathPrefix = "res://assets/music/terraforming-mars-tracks/track-";)\n',
            rf'\1\n\tprivate const string TrackExtension = "{extension}";\n',
            text,
            count=1,
        )
        if n != 1:
            sys.exit("error: could not insert TrackExtension constant in Music.cs")

    text = re.sub(
        r'private const string TrackExtension = "[^"]+";',
        f'private const string TrackExtension = "{extension}";',
        text,
        count=1,
    )
    text = re.sub(
        r'(\$"\{TrackPathPrefix\}[^"]+)\.mp3',
        r"\1{TrackExtension}",
        text,
    )

    remaining = [
        line
        for line in text.splitlines()
        if "TrackPathPrefix" in line and ".mp3" in line
    ]
    if remaining:
        sys.exit(
            "error: Music.cs still contains .mp3 track paths:\n" + "\n".join(remaining)
        )

    if text == original:
        print("Music.cs already points at TrackExtension (no changes)")
        return

    if dry_run:
        print(
            f"would update {MUSIC_CS.relative_to(REPO_ROOT)} "
            f'(TrackExtension = "{extension}")'
        )
        return

    MUSIC_CS.write_text(text, encoding="utf-8")
    print(f"updated {MUSIC_CS.relative_to(REPO_ROOT)}")


def remove_mp3_sources(mp3_files: list[Path], dry_run: bool) -> None:
    for mp3 in mp3_files:
        import_sidecar = mp3.with_suffix(mp3.suffix + ".import")
        for path in (mp3, import_sidecar):
            if not path.exists():
                continue
            if dry_run:
                print(f"would remove {path.relative_to(REPO_ROOT)}")
            else:
                path.unlink()
                print(f"removed {path.relative_to(REPO_ROOT)}")


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--quality",
        "-q",
        type=int,
        default=VORBIS_QUALITY_DEFAULT,
        help=f"Vorbis quality 0–10 (default {VORBIS_QUALITY_DEFAULT}, ~160 kbps at 5)",
    )
    parser.add_argument("--dry-run", action="store_true", help="Print actions without writing files")
    parser.add_argument(
        "--remove-mp3",
        action="store_true",
        help="Delete .mp3 and .mp3.import after successful encode (irreversible)",
    )
    parser.add_argument(
        "--extension",
        default=".ogg",
        help="Target extension written into Music.cs (default .ogg)",
    )
    args = parser.parse_args()

    if not MUSIC_CS.is_file():
        sys.exit(f"error: missing {MUSIC_CS}")

    ffmpeg = find_ffmpeg()
    mp3_files = list_mp3_tracks()
    ext = args.extension if args.extension.startswith(".") else f".{args.extension}"

    print(f"tracks: {len(mp3_files)} in {TRACKS_DIR.relative_to(REPO_ROOT)}")

    for mp3 in mp3_files:
        ogg = mp3.with_suffix(ext)
        encode_track(ffmpeg, mp3, ogg, args.quality, args.dry_run)

    update_music_cs(ext, args.dry_run)

    if args.remove_mp3:
        remove_mp3_sources(mp3_files, dry_run=args.dry_run)

    if not args.dry_run:
        print("done. Re-import in Godot (open editor or headless) before exporting.")


if __name__ == "__main__":
    main()
