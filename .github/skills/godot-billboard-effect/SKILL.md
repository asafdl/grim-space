---
name: godot-billboard-effect
description: 'Turn user-provided transparent PNG artwork into a camera-facing 3D billboard animation effect in grim-space. Use when adding hand-drawn cartoon VFX, steam, Heat Vents puffs, flashes, or buff art to an ability, integrating a PNG as a Sprite3D billboard, or prototyping 2D artwork in the 3D battle scene.'
---

# Transparent PNG Billboard Effects

Integrate supplied artwork as a visual-only effect in the Godot C# battle presentation.
A single drawing can move, expand, rotate, and fade without requiring a drawn frame sequence.
Preserve the user's cartoon artwork; do not replace it with procedural art or convert existing
3D weapon effects unless requested.

## Inputs and decisions

Establish these from the request and existing effect before editing:

- Source PNG and the ability/event it represents.
- Placement: ship-local attachment, impact point, or world-space position.
- Motion: follows the ship throughout, or emits from the ship then drifts independently.
- Lifetime: one-shot, loop while a buff exists, or persistent until an explicit event.
- Timing: start, peak/impact, and finish; optional supplied sound.
- Approximate world-space size and intended drawing orientation.

Ask only for missing decisions that materially change behavior. Recommend concrete defaults
instead of requiring the user to know Godot terminology. Do not invent gameplay mechanics,
new vent locations, impact timing, or sound assets.

For smoke/steam, recommend camera-facing puffs emitted from agreed ship-local positions,
then drifting independently in world space. A symbol attached to a buff usually follows
the ship instead. Explain this distinction before implementing if not already agreed.

## Procedure

### 1. Inspect the art and owning flow

1. View the actual PNG and inspect its pixel dimensions, alpha channel, transparent margins,
   and visible-art bounds. Do not derive source dimensions from a scaled image preview.
2. Confirm the background is genuinely transparent, not a painted checkerboard or white matte.
   If unsuitable, report it and request a corrected export; do not silently alter the drawing.
3. Read the architecture boundaries in `README.md`. Trace the relevant action, committed facts,
   replay clip, and existing effect/view lifetime. Search for reusable helpers before adding code.
4. Identify the authoritative event that starts/stops the effect. Do not mutate domain state
   from presentation, infer gameplay results from animation, or add a second buff timer.
5. Present a plan and obtain approval for multi-file work, following repository instructions.

### 2. Prove placement before polishing

1. Use one supplied drawing in a static or minimally animated dev preview first.
2. Put the art at the intended attachment/impact location at actual battle-camera scale.
3. Check frontal, oblique, elevated, and opposite-side camera views.
4. Show the rough result before investing in elaborate motion or asking for more drawings.
   If the effect needs a stable silhouette from multiple angles or must wrap around a hull,
   explain the limitation and propose 3D; do not silently change techniques.

### 3. Import and configure the billboard

1. Preserve the source. Copy the accepted PNG into an appropriately named folder under
   `assets/vfx/`, following adjacent asset conventions. Use lowercase snake_case names.
   Keep editable originals and temporary crops out of runtime assets.
2. Prefer `Sprite3D` with its texture assigned and full camera-facing billboard mode enabled.
   Use `AnimatedSprite3D` only when the user actually supplies multiple frames.
   Prefer standard engine behavior over a custom shader or framework.
3. Retain smooth alpha transparency and depth testing. Do not enable drawing through ships,
   fixed-screen-size scaling, additive blending, or glow by default.
4. Start unshaded to preserve the drawn colors; match the established effect's rendering
   layers and camera masks. Verify the effect is visible in the actual battle renderer.
5. Size using visible-art bounds and world-space units, not an unexplained pixel-size constant.
   Account for transparent padding when aligning the visual center or emission point.
   Do not crop, recolor, resample, or remove drawn glow without agreement.
6. Preserve smooth filtering for painted art; use nearest filtering only for intended pixel art.
   Evaluate mipmaps for artwork that becomes small at battle zoom.
7. Reuse an existing emitter offset if available. Otherwise agree on an attachment location;
   never treat one ship's hard-coded vent coordinates as valid for every chassis.

### 4. Animate and integrate

1. Use the existing presentation owner and replay paths. Keep C# domain rules Godot-free.
   If a required committed event is absent, extend its owning effect's facts and replay
   application rather than discovering the action through UI state or polling live state.
2. For a single PNG, use simple position/scale/opacity tweens:
   appear quickly, perform the agreed motion, then fade. Do not require a sprite sheet
   when transforms are enough.
3. Camera-facing art must remain camera-facing throughout movement. If rotating the art,
   verify rotation around its visible normal rather than tilting it out of view.
4. Distinguish an attached effect from emitted particles:
   attached art follows the rendered ship pose; detached puffs sample the emitter's world
   transform once and do not inherit later ship translation, turning, or scale.
5. Synchronize gameplay impact and optional audio with existing replay timing. Do not apply
   damage/healing when a tween finishes or add replay pauses just to accommodate a cosmetic tail.
6. Use per-instance properties/materials for fades and tinting. Never tween a shared resource
   that could change unrelated effects.
7. Handle completion, cancellation/reset, target removal/death, scene exit, and repeated
   activations explicitly. Kill owned tweens and free effect/audio nodes appropriately.
8. Restore persistent buff visuals silently from supplied state on load/reset; do not replay
   casting animations or SFX. Buff expiry must follow authoritative replay/status changes,
   not a separately guessed duration.

### 5. Validate and hand off

1. Run the Godot importer using `scripts/godot-export-env.sh` and `godot_export_env`;
   preserve generated `.import` metadata for the runtime art.
2. Run `dotnet build` after C# or script-binding changes. Stop on failure rather than
   previewing stale assemblies.
3. Invoke `godot-scene-debug` to run/capture the relevant dev scene with the real renderer.
   Headless import is fine; headless rendering is not a visual check.
4. Verify the exact effect: timing, readable scale, original colors, transparent edges,
   camera-facing orientation, ship occlusion, correct following/drift, and fade cleanup.
   Check overlapping instances and repeated activation for shared-resource interference.
5. For persistent effects, verify expiry, target death, reset, and loaded active state.
   Add pure C# tests for new fact/replay logic; do not unit-test Godot framework objects.
6. Update directly related documentation with asset location, timing, attachment behavior,
   and preview controls. Report what was visually inspected versus automatically tested.
7. Keep a useful preview screenshot if appropriate. Remove exact scoped temporary captures,
   frame sequences, generated recording audio, and smoke-test scripts. Do not commit them.

## Artist handoff

The user can provide just a transparent PNG plus a sentence such as:
"Heat Vents: two puffs from the side vents, drifting outward and expanding, then fading."
Resolve placement and timing together; do not require editor setup or atlas packing.
No mandatory resolution is imposed: 512x512 is a reasonable starting canvas, not a constraint.
If frames are later supplied, require consistent canvas size/alignment, frame order, and FPS,
then preserve this same placement and replay-lifecycle contract.
