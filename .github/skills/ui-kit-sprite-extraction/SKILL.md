---
name: ui-kit-sprite-extraction
description: 'Extract a UI sprite from a raw PNG sprite sheet using a user-provided screenshot as the visual target. Use when asked to cut, crop, isolate, or create a runtime UI-kit asset from a larger PNG.'
argument-hint: 'Raw PNG path, reference screenshot, and desired runtime asset name'
---

# UI Kit Sprite Extraction

Extract one clean runtime sprite from a raw UI-kit sheet by matching a supplied screenshot to the source PNG. Preserve the source artwork; never edit the raw sheet.

## Inputs

Identify:

- The raw source PNG in the repository.
- The screenshot or crop showing the requested sprite.
- The intended output path and role: texture, palette-neutral base, NinePatch container, portrait frame, button, or other UI element.

If the screenshot contains multiple plausible sprites and the user has not identified one clearly, ask which sprite to use before extracting.

## Procedure

1. View the screenshot and the raw source PNG.
2. Read the raw image's actual pixel dimensions. The displayed preview may be scaled and must not be treated as source coordinates.
3. Locate the target in the raw sheet:
   - Crop a temporary search region around the estimated position.
   - View that search crop.
   - Refine the source-space bounds until the complete sprite is included without neighboring artwork.
4. Extract from the raw PNG with Pillow while preserving RGBA:

   ```bash
   python3 - <<'PY'
   from pathlib import Path
   from PIL import Image

   source = Image.open("RAW_PNG").convert("RGBA")
   target = source.crop((LEFT, TOP, RIGHT, BOTTOM))
   target.save(Path("OUTPUT_PNG"))
   PY
   ```

5. View the extracted PNG before wiring it into code. Check:
   - All corners, glows, shadows, and detached accent strokes are present.
   - No pixels from adjacent sprites remain.
   - Transparent margins are intentional and reasonably balanced.
   - The output has not acquired a white or black matte.
6. Keep a white or grayscale source variant when the runtime palette shader should recolor it. Do not pre-tint a palette-neutral asset.
7. If the sprite will be used as a NinePatch:
   - Downsample high-resolution source art before import when preserved corner pixels would be oversized at runtime.
   - Choose patch margins inside straight, repeatable edge sections.
   - Keep bevels, notches, detached accents, and corner decorations entirely outside the stretchable center.
   - Test both the smallest and widest expected control sizes.
8. Run Godot's importer:

   ```bash
   source scripts/godot-export-env.sh
   godot_export_env
   "$GODOT" --headless --path "$PWD" --import
   ```

9. Build with `dotnet build` after wiring the asset into C#.
10. Render the relevant dev preview scene and inspect the final UI at runtime size. A successful import or build does not prove that crop bounds, NinePatch margins, or palette mapping are correct.

## Runtime Asset Rules

- Put extracted production sprites under `assets/ui/components/runtime/` unless an existing feature-specific asset folder is more appropriate.
- Use descriptive lowercase snake_case names ending in `_neutral` for palette-remapped bases.
- Keep temporary search crops outside the repository, preferably in the session artifact directory.
- Do not commit Movie Writer frame sequences or generated WAV files.
- Preserve generated `.import` metadata for runtime assets.

## Failure Handling

- If Pillow is unavailable, report the missing dependency rather than silently using lossy screenshot tooling.
- If Godot reports `No loader found for resource`, run the importer before retrying the preview.
- If a NinePatch shows oversized or duplicated corners, fix source resolution and patch margins; do not compensate with unrelated container offsets.
- If the extracted sprite cannot be uniquely matched to the screenshot, stop and ask the user to identify its row, column, color, or neighboring sprites.
