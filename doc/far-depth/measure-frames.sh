#!/usr/bin/env bash
# Extract requested frames from a kit movie and measure the disk-crop mean luma.
# Usage: measure-frames.sh <movie> <crop x:y:w:h> <frame> [frame...]
set -euo pipefail
FF=$(ls /c/Users/User/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe | head -1)
MOVIE="$1"
CROP="$2"
shift 2
WORK="/d/ops-work/fardepth"
NAME=$(basename "$MOVIE" .avi)
OUT="$WORK/frames/$NAME"
mkdir -p "$OUT"
for frame in "$@"; do
	"$FF" -y -i "$MOVIE" -vf "select=eq(n\,${frame})" -vframes 1 "$OUT/f${frame}.png" >/dev/null 2>&1
	avg=$("$FF" -hide_banner -i "$OUT/f${frame}.png" -vf "crop=${CROP},signalstats,metadata=print:key=lavfi.signalstats.YAVG" -f null - 2>&1 \
		| grep -o "YAVG=[0-9.]*" | head -1)
	echo "frame ${frame}  ${avg}"
done
