# FAR-DEPTH — notes for the reviewer (plain words)

Task: the black hole and the far backgrounds "looked weird / paper-flat".
Branch `graphic/far-depth`, isolated to this worktree + `doc/far-depth` evidence.

## What was actually wrong (found by rendering, not guessing)

The "black hole" was **not an object at all**. It was a photo collage:
`assets/textures/event-horizon.jpg` was spliced into the sky shader
(`map_sky.gdshader` `panorama_patch`) — a flat picture pasted onto the
dome behind everything. Proof in `evidence/before_*.png`:

- It sat at the same visual depth as the playfield (its glow overlapped the
  map plane with no separation), so the eye read it as "a sticker hovering
  right next to the stations".
- Zoom in (`evidence/compare_zoom-in_before-after.png`, left half): at camera
  distance 42 vs 14 the decal **did not grow at all** — nothing that far away
  behaves that way; real things get bigger when you approach.

## What changed (one thing, small)

1. The sky decal was removed from `map_sky.gdshader` (the nebula splice stayed).
2. A real 3D black hole was built in its place: black event-horizon sphere +
   additive accretion-disk ring with a spiral grain and a thin photon ring at
   the limb (the cheap "lensing hint"). New files:
   `assets/shaders/black_hole_accretion.gdshader`,
   `assets/shaders/black_hole_horizon.gdshader`,
   `src/world/star-system/presentation/map/MapBlackHoleVisual.cs`.
   `MapBackdrop` (the existing far-background owner) mounts it along the old
   decal's sky direction at distance 260 world units — far beyond the
   playfield, inside every camera's far plane.
   Sizing knobs live where the old sky-decal knobs lived:
   `MapAtmosphereSettings` (`BlackHoleDistance/HorizonRadius/DiskRadius`).
   `event-horizon.jpg` is no longer referenced by code.
3. A tiny dev-only camera arg (`--mappose=yaw,pitch,dist[,driftDegPerSec]`)
   in `MapController` was added so evidence renders are repeatable. No
   argument → no effect on the game.

## Why this is presentation-only

No new gameplay state, no parallel system: `MapBackdrop` already owns the far
background (it built the sky); the black hole is added inside it, fed from the
same `MapAtmosphereSettings` tuning surface. Picking, POIs, camera limits,
facade modes — untouched.

## Proof (all in `evidence/`, same camera poses rendered before and after)

Boot layouts are procedurally random (no seed flag exists), so nearby POIs
differ between shots; the compared subject is the far layer + camera geometry.

| file | shows |
|---|---|
| `compare_stare_before-after.png` | Same stare pose: paper decal vs 3D black hole with perspective shadow + disk wrapping |
| `compare_zoom-in_before-after.png` | THE proof: before — decal stays a fixed-size sticker when the camera dives to distance 14; after — the black hole looms huge exactly like the sun POI does |
| `camera-move_far-to-near_after.png` | 72 → 42 → 14 camera distance strip: the black hole scales with distance like proper world geometry |
| `camera-drift_parallax_before-after.png` | 2-frame orbit drift: before — decal locked to the sky (0 parallax); after — black hole slides against the starfield while swinging around the playfield = true world-space depth |
| `final_boot_f180.png` | Normal boot: flat decal gone, starfield layers read clean |

Also verified: `dotnet build` clean; `dotnet test` 1601/1603 — the 2 failures
(`BattleSpawnPilotTests`) were confirmed failing on the untouched base commit
`0512d4bd` (battle encounter logic, pre-existing, unrelated). Headless
`--no-sky` audit renders still pass (exit 0, no `texture_2d_get` crash).

## Per-layer decision summary

| layer | drawn as | decision |
|---|---|---|
| Stars | sky-pass panorama | Keep — sky pass is "infinitely far" by construction, correct semantics |
| Nebula | sky-pass splice | Keep — same far semantics; its only flat-look neighbour (the decal) is gone |
| Black hole | ~~sky decal~~ → world-space horizon sphere + additive accretion disk | Replaced with real 3D at distance 260; photon-ring lensing hint at the limb |
| Dust cards | world-space billboards | Keep — they already respond to camera and sit on the playfield scale |
| Sun / planets / stations | world-space meshes | Untouched (near/mid layers, already 3D) |
