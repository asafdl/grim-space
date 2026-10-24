#!/usr/bin/env bash
# Compose a labelled evidence strip from stills (crop -> panel -> label).
# Usage: compose-strip.sh <out.png> <crop w:h:x:y> <label> <still> [<label> <still> ...]
set -euo pipefail
FF=$(ls /c/Users/User/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe | head -1)
FONT="C\\:/Windows/Fonts/consola.ttf"
OUT="$1"
CROP="$2"
shift 2
i=0
inputs=(-y -hide_banner)
filter=""
labels=""
while [ $# -gt 1 ]; do
	label="$1"; still="$2"; shift 2
	inputs+=(-i "$still")
	filter="${filter}[${i}:v]crop=${CROP},drawtext=fontfile='${FONT}':text='${label}':x=10:y=10:fontsize=24:fontcolor=white:box=1:boxcolor=black@0.6:boxborderw=8[v${i}];"
	labels="${labels}[v${i}]"
	i=$((i+1))
done
filter="${filter}${labels}hstack=inputs=${i}[out]"
"$FF" "${inputs[@]}" -filter_complex "$filter" -map "[out]" -frames:v 1 "$OUT" 2>&1 | grep -Ei "error|invalid" | head -3 || true
echo "strip: $OUT"
