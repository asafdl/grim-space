---
name: godot-scene-debug
description: 'Launch or capture a screenshot of a Godot .tscn scene for visual debugging. Use when asked to view, open, run, preview, screenshot, or debug a scene in the grim-space Godot project.'
argument-hint: 'Scene path and optional screenshot request, for example scenes/battle.tscn screenshot'
---

# Godot Scene Debug

Launch a requested scene in the Godot .NET runtime so the user can inspect it, or render a short deterministic PNG sequence and return its final frame as a screenshot. This skill does not validate gameplay rules or replace automated tests.

## Procedure

1. Resolve the requested scene to a repository-relative `.tscn` path.
2. Confirm the file exists and remains inside the repository. If no scene was identified, ask the user which scene to launch.
3. Build the project with `dotnet build` when C# source or scene script bindings may have changed. Stop and report build failures rather than launching stale code.
4. Discover the installed Godot executable by sourcing `scripts/godot-export-env.sh` and calling `godot_export_env`.
5. Choose one of the following modes.

### Interactive View

Launch the scene from the repository root:

   ```bash
   source scripts/godot-export-env.sh
   godot_export_env
   "$GODOT" --path "$PWD" --scene "res://scenes/example.tscn"
   ```

Replace the example with the resolved scene path. Run this as a detached process when the user needs the window to remain open after the agent finishes. Verify that the process remains running long enough for the scene window to initialize.

### Screenshot Capture

Use Godot's Movie Writer to render enough frames for the scene to initialize, then use the final generated PNG as the screenshot:

```bash
source scripts/godot-export-env.sh
godot_export_env
"$GODOT" \
  --path "$PWD" \
  --scene "res://scenes/example.tscn" \
  --write-movie "/absolute/artifact/path/frame.png" \
  --fixed-fps 60 \
  --quit-after 30 \
  --disable-vsync
```

Store output outside the repository, preferably in the session artifact directory. PNG Movie Writer output is a numbered sequence such as `frame00000000.png`; inspect and return the highest-numbered frame. Remove the intermediate numbered frames and generated WAV by their exact resolved paths after preserving the selected screenshot.

This capture is non-interactive and auto-quits, but it is not Godot `--headless`: the headless display driver cannot render this project reliably and may crash without producing a PNG. The real renderer may create a window briefly while capturing.

6. Report the launched scene or captured screenshot path and any startup errors. A non-zero exit or missing PNG is a failed capture, even if Godot printed that Movie Maker mode started.

## Guardrails

- Use the .NET Godot build selected by `godot_export_env`; do not assume the standard Godot build can load C# scripts.
- Launch only one new scene instance unless the user explicitly asks for multiple instances.
- Do not modify the project's configured main scene merely to preview another scene.
- Never combine `--headless` with screenshot capture. Use Movie Writer with the platform renderer.
- Do not treat a running process as proof that the scene rendered correctly. The user performs the visual inspection.
- Do not commit generated screenshots, frame sequences, or WAV files unless the user explicitly requests it.
- If an existing game instance would conflict with the requested launch, ask before stopping it. Terminate only the specific process started by this workflow.
