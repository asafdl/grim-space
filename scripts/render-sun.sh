#!/usr/bin/env bash
# Render the sun test scene with Godot Movie Maker and extract frames.
#
# Godot caches compiled shaders in .godot/, so the project is re-imported
# first; without that a shader edit silently does not reach the render.
#
# usage: render-sun.sh <output-avi> <frames> <extra godot args...>
set -euo pipefail

GODOT="/c/Users/User/AppData/Local/Microsoft/WinGet/Packages/GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"
REPO="/c/Users/User/dev/repos/grim-space"
OUT="${1:-D:/grim-space-recordings/sun_test.avi}"
FRAMES="${2:-300}"
shift 2 || true

"$GODOT" --path "$REPO" --headless --import >/dev/null 2>&1 || true
"$GODOT" --path "$REPO" \
	--write-movie "$OUT" \
	--quit-after "$FRAMES" \
	--fixed-fps 30 \
	--scene "res://scenes/sun_test.tscn" \
	-- "$@" 2>&1 | grep -E "Done recording|SHADER ERROR|ERROR:" || true

echo "wrote $OUT"
