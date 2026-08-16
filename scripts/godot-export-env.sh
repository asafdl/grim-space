#!/usr/bin/env bash
# Shared Godot export paths derived from the installed editor binary.

godot_export_env() {
	GODOT="${GODOT:-godot-mono}"

	if ! command -v "$GODOT" &>/dev/null; then
		if command -v godot &>/dev/null; then
			GODOT=godot
		else
			echo "error: godot-mono (or godot) not found in PATH" >&2
			return 1
		fi
	fi

	local version_line
	version_line="$("$GODOT" --version)"

	GODOT_TEMPLATE_DIR_NAME="${version_line%%.official*}"

	local major minor _stability _flavor
	IFS='.' read -r major minor _stability _flavor _ <<<"$GODOT_TEMPLATE_DIR_NAME"
	GODOT_RELEASE_TAG="${major}.${minor}-stable"

	if [[ "$GODOT_TEMPLATE_DIR_NAME" == *mono* ]]; then
		GODOT_TEMPLATES_TPZ="Godot_v${major}.${minor}-stable_mono_export_templates.tpz"
	else
		GODOT_TEMPLATES_TPZ="Godot_v${major}.${minor}-stable_export_templates.tpz"
	fi
	GODOT_TEMPLATES_URL="https://github.com/godotengine/godot-builds/releases/download/${GODOT_RELEASE_TAG}/${GODOT_TEMPLATES_TPZ}"

	if [[ "$(uname -s)" == "Darwin" ]]; then
		GODOT_EXPORT_TEMPLATES_DIR="$HOME/Library/Application Support/Godot/export_templates"
	else
		GODOT_EXPORT_TEMPLATES_DIR="${XDG_DATA_HOME:-$HOME/.local/share}/godot/export_templates"
	fi

	GODOT_MACOS_TEMPLATE="${GODOT_EXPORT_TEMPLATES_DIR}/${GODOT_TEMPLATE_DIR_NAME}/macos.zip"
	export GODOT GODOT_TEMPLATE_DIR_NAME GODOT_RELEASE_TAG GODOT_TEMPLATES_TPZ GODOT_TEMPLATES_URL
	export GODOT_EXPORT_TEMPLATES_DIR GODOT_MACOS_TEMPLATE
}
