---
name: godot-drawing-to-3d-lines
description: 'Turn user-provided 2D drawings or sketches into cartoon 3D line effects in grim-space. Use when tracing drawn speed lines, Ram trails, curved strokes, motion marks, or hand-drawn outlines into ship-aligned 3D curves or tube meshes with reveal animation, glow, and replay integration rather than a camera-facing PNG billboard.'
---

# 2D Drawings to 3D Line Effects

Interpret supplied artwork as separate spatial strokes in the Godot C# battle presentation.
Preserve the sketch's silhouette, curvature, spacing, and intentional irregularity.
This is a geometry workflow, not an image pasted onto a plane or an automatic 3D reconstruction.

## Choose the representation

- For exact painted colors, lettering, smoke, and flashes, prefer the existing
  [billboard skill](../godot-billboard-effect/SKILL.md).
- For strokes whose depth and ship-relative direction matter, use this skill.
- Both can coexist: Ram uses 3D trails during buildup and a BAAM billboard at impact.
- Do not replace existing GLB artwork or silently substitute generic procedural shapes.

## Inputs and decisions

Establish the source drawing, target ability/event, and these spatial choices:

- Which drawing edge faces forward, and which side attaches to the ship?
- Are lines behind the stern, alongside the hull, ahead of the nose, or surrounding it?
- Are strokes in one plane, mirrored across sides, or distributed in depth?
- Do they follow the ship, remain at emission positions, or stretch along a movement path?
- Approximate world-space length, spacing, thickness, color, and optional glow.
- Reveal order: entire strokes appearing individually, or each stroke drawing along its length.
- Buildup, hold, launch, impact, and cleanup timing; optional sound.

Ask for missing decisions that materially change the interpretation. A single sketch does not
define hidden geometry: recommend a simple depth arrangement and confirm it rather than inventing one.
For multi-file changes, present a plan and obtain approval under repository instructions.

## Procedure

### 1. Inspect and trace

1. View the original image, inspect its actual dimensions, and distinguish separate intended
   strokes from construction marks, borders, and lettering. Never edit a read-only attachment.
2. Read the architecture boundaries in `README.md`; trace the action, committed facts,
   replay clip, and presentation owner before changing animation behavior.
3. Use a small set of ordered control points per stroke, following the user's curves.
   Normalize drawing coordinates around an agreed anchor; account for image Y increasing
   downward when mapping to Godot coordinates.
4. Prefer straightforward polylines or sampled curves. Do not introduce image skeletonization,
   external conversion services, or a tracing framework for a handful of strokes.
5. If interpreting rather than faithfully tracing the sketch, explicitly identify the approximation.
   Do not claim a generic set of lines reproduces artwork exactly.

### 2. Establish space and placement

1. Verify the consuming view's local axes. In `UnitView`, local +X is starboard,
   +Y is dorsal, and +Z is forward; verify this against `BasisFrom` when implementing.
   Godot's named `Vector3.Forward` is -Z, so it points aft in that ship-local frame.
2. Use cached hull bounds or existing attachment locations. Do not recompute hull bounds
   after adding effects, where effect meshes could contaminate the measurement.
3. For rearward trails, start aft of the hull's minimum local Z and extend toward decreasing
   Z. Do not arrange the strokes around the ship merely because the effect is three-dimensional.
4. Preserve distinct lines and purposeful length variation. Add depth only as agreed;
   do not turn a flat sketch into a radial cage by default.
5. Use a ship-local effect parent for attached lines. For detached trails, sample world
   transforms at emission and do not inherit later ship movement or rotation.
6. Make a static or minimally animated preview first. Start from the user's requested
   camera angle; use a side view for rearward trails, then inspect oblique and rotated views.
   Review shape and placement before elaborate animation.

### 3. Build readable geometry

1. Search for prior art, particularly `RamChargeEffect`, before introducing mesh generation.
   Reuse appropriate geometry logic; extract a helper only if there are actual shared consumers.
2. Use thin tube meshes when consistent thickness and visibility from multiple angles matter.
   Primitive line meshes are acceptable for a rough prototype but can become subpixel at battle zoom.
3. Sample curves sufficiently to avoid visible corners without excessive subdivisions.
   For general 3D curves, orient tube cross-sections perpendicular to the local tangent using
   a stable frame; avoid flips near parallel reference vectors and handle coincident points.
   A fixed cross-section plane is only suitable for curves whose direction supports it.
4. Verify triangle winding/culling, finite vertices, thickness, and joins. A successful build
   does not establish that the generated mesh is visible or correctly oriented.
5. Keep materials unshaded if appropriate for the drawn style. Preserve depth testing,
   world-space sizing, and established presentation render layers.
6. Add glow only when requested. Use HDR brightness/emission and the existing environment's
   bloom settings; verify a visible halo in the actual renderer. Do not globally increase bloom
   to compensate for an incorrect material. Emission on an unshaded material alone may not
   visibly glow; inspect rendered output rather than assuming it does.
7. Static materials may be shared. Animated tint/emission must use per-effect or per-stroke
   material instances so unrelated activations remain unaffected.

### 4. Animate through the owning replay flow

1. Use the existing action clip or committed facts. Keep Godot geometry out of domain rules,
   and never derive gameplay results from node transforms or tween completion.
2. Implement the agreed reveal semantics: toggling stroke visibility does not animate a line
   drawing itself. Use progressive geometry only when a draw-on effect is requested.
3. Separate buildup, hold, dash, and impact explicitly. A Ram-style sequence can spend most
   time charging and then move rapidly to the resolved destination. Do not change legality,
   collision rules, damage, or destination to make the visual look better.
4. Keep visual vibration on the presentation child, not authoritative position. Restore its
   baseline on completion and cancellation; shake in ship-local axes for rotated ships.
5. Do not overlap charge and impact cues accidentally. If a billboard belongs at the hit,
   show it at arrival, not at launch. Cosmetic tails must not add replay pauses.
6. Agree on each sound's phase. Inspect source onset and isolate requested hits, preserving
   the original outside runtime processing. Do not mask an impact with a loud engine tail.
7. For an audible fade, tween linear gain and convert to decibels with a safe positive floor,
   rather than dropping rapidly from 0 to -60 dB. Verify the sound remains audible during the
   intended phase and is freed only after its fade.
8. Wire cancellation, reset, synchronization, death/hiding, and scene exit. Kill owned tweens,
   free geometry/audio, restore pose offsets, and stop trail emitters. Track the dash separately
   if charge geometry is freed before the movement tween ends; cancelling must still stop motion.

### 5. Validate and hand off

1. Build with `dotnet build`; stop rather than previewing stale assemblies on failure.
2. Run the smallest related domain/replay regression tests. Do not unit-test Godot framework
   objects; use the actual engine preview for mesh/tween behavior.
3. Invoke `godot-scene-debug` for rendered inspection. Verify the requested side/front view
   plus oblique and rotated-ship cases, at actual battle-camera scale.
4. Check measurable phase timing, reveal order, stationary buildup, resolved dash endpoint,
   impact ordering, depth occlusion, glow, audio fade, and repeated activations.
5. Exercise cancellation during both buildup and dash. Verify no remaining shake offset,
   moving ship, geometry, trails, billboard, or playing sound after reset.
6. Update related documentation with runtime owner, timing, and dev-preview controls.
   State which checks actually ran; do not describe an untested angle or path as validated.
7. Remove exact scoped temporary scripts, Movie Writer frames, and recording audio.
   Retain useful preview screenshots outside the repository when appropriate.

## Artist handoff

A sketch plus "these lines extend behind the stern, appear one by one during charge,
then disappear when the ship dashes" is enough to start. The user need not author a GLB
or draw unseen sides. Resolve depth and orientation with a rough preview before polishing.
