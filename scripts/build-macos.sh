#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUTPUT="$ROOT/dist/macos/grim-space.zip"

# shellcheck source=godot-export-env.sh
source "$ROOT/scripts/godot-export-env.sh"
godot_export_env

cd "$ROOT"
mkdir -p "$(dirname "$OUTPUT")"

if [[ ! -f "$GODOT_MACOS_TEMPLATE" ]]; then
	echo "error: macOS export templates not installed" >&2
	echo "expected: $GODOT_MACOS_TEMPLATE" >&2
	echo "run: $ROOT/scripts/install-godot-export-templates.sh" >&2
	exit 1
fi

echo "Importing project..."
"$GODOT" --headless --path "$ROOT" --import || true

echo "Generating .NET bindings..."
"$GODOT" --headless --path "$ROOT" --build-solutions --quit || true

echo "Building C# project..."
dotnet build --configuration Release

echo "Exporting macOS release to $OUTPUT..."
"$GODOT" --headless --path "$ROOT" --export-release "macOS" "$OUTPUT"

echo "Done: $OUTPUT"
