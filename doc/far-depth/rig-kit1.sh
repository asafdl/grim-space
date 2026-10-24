#!/usr/bin/env bash
# FAR-DEPTH realism kit 1 evidence rig.
# Usage: rig-kit1.sh <name> <pose> [extra godot user args...]
#   pose = yawDeg,pitchDeg,dist[,driftDegPerSec]
# Renders a movie with the committed --mappose rig, extracts the settled
# frame, and keeps the heavy intermediates on /d/ops-work/fardepth.
# Layout is deterministic via --load-save so before/after frames at one pose
# are pixel-comparable (boot layouts are random under --new-run).
set -euo pipefail

GODOT="/c/Users/User/AppData/Local/Microsoft/WinGet/Packages/GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"
FF=$(ls /c/Users/User/AppData/Local/Microsoft/WinGet/Packages/Gyan.FFmpeg*/ffmpeg-*/bin/ffmpeg.exe | head -1)
REPO="/c/Users/User/dev/repos/grim-space-fardepth"
WORK="/d/ops-work/fardepth"
NAME="$1"
POSE="$2"
shift 2
FRAMES=${KIT_FRAMES:-240}
SETTLE=${KIT_SETTLE:-170}
mkdir -p "$WORK/movies" "$WORK/stills"

"$GODOT" --path "$REPO" \
	--write-movie "$WORK/movies/${NAME}.avi" \
	--quit-after "$FRAMES" \
	--fixed-fps 30 \
	--scene "res://scenes/map.tscn" \
	-- "$@" --mappose="$POSE" 2>&1 \
	| grep -E "Done recording|SHADER ERROR|Parser Error|\[MapController\] bad" | head -5 || true

"$FF" -y -i "$WORK/movies/${NAME}.avi" \
	-vf "select=eq(n\,${SETTLE})" -vframes 1 "$WORK/stills/${NAME}.png" 2>&1 | tail -1
echo "still: $WORK/stills/${NAME}.png"
