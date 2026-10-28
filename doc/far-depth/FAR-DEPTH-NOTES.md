# FAR-DEPTH — notes for the reviewer (plain words)

Task: the black hole and the far backgrounds "looked weird / paper-flat".
Branch `graphic/far-depth`, isolated to this worktree + `doc/far-depth` evidence.

## Sky-pass black hole v3 (the "match the target crop" round)

User ruling, six items, one commit each: (1) tip the accretion light into an
inclination ellipse, (2) bent light arcs crossing over / under / in front of
the silhouette like the target still, (3) bright side on the LEFT, (4) palette
sampled from the target crop, (5) keep every v2 verdict intact, (6) evidence +
notes. Targets were two stills in the ops folder (`target-overunder-arcs.png`,
`target-doppler.png`), not repo files.

1. **Inclination squash.** New shader knob `bh_disc_squash = 0.55`: the ring
   band, halo and grain are computed on an ellipse squashed to 0.55 of full
   height — a semi-edge-on disk — while the SHADOW stays a perfect circle,
   because the silhouette is the lensed image of the horizon, not of the disk,
   and the lens does not care which way the disk is tipped. The glow mask also
   stays keyed to the circular radius so light still never bleeds onto the
   black disc (v2 verdict B). Evidence:
   `evidence/skypass-v3/squad_42_before-after.png` (top row = v2 before,
   bottom = after).
2. **Over/under arcs, unclipped.** Three bent-light bands are now drawn AFTER
   the shadow composite, so the shadow mask can no longer cut them: an upper
   arc hugging the top of the silhouette, a lower arc running beneath it, and
   a front band computed in the squashed metric that crosses the lower half of
   the black disc — the "light passes in front of the hole" look from the
   target crop. First attempt had a bug found by render: an early-out on the
   new elliptical angle silently culled both arcs; it is back to the circular
   early-out with a separate elliptical envelope bounding the light.
   Evidence: same strips, bottom rows.
3. **Doppler repinned to screen-left.** One-sided-boost probe renders proved
   the sky-pass frame is LEFT-handed on screen: boost direction `(1,0)`
   brightens the LEFT side, `(0,1)` is screen-up. The repin is a plain axis
   flip, and the bright side now sits left like the target. Two rig facts
   worth keeping: the earlier "bright side was wrong" read was partly the
   `--load-save` boot dropping a fixed semi-transparent encounter panel over
   the ring's left half (it darkens exactly that half — brutal for doppler
   evidence), so every v3 evidence render uses `--new-run`, which is panel-free
   and pixel-deterministic; differing POI layouts between before/after stay
   acceptable per the v2 precedent (the compared subject is the
   layout-independent far layer). Evidence: bright side visible in both squad
   strips; probe stills `v3_doppler_probe*.png` under the ops folder.
4. **Palette from the target crop** — not NASA blue, not the old amber. Three
   sample-compare iterations landed: hot (1.0, 0.45, 0.12), warm
   (0.88, 0.16, 0.005), outer (0.62, 0, 0), with the old ring colour faded out
   mid-annulus so it cannot grey the falloff. Render measures vs the crop's:
   crest (245,209,65) vs (254,152,53) — the same orange-white class; mid
   (219,79,17) vs (226,76,9) — near-exact; falloff (100,20,6) vs (135,3,1) —
   deep red, no grey tail.
5. **v2 verdicts preserved.** Shadow still opaque pure black with glow masked
   off it; still one direction-locked sky-pass element with no far-clip
   ceiling; falloff still broad and soft, no knife ring — it now ends in the
   crop's pure red instead of grey. No double draw anywhere (still
   `map_sky.gdshader` only).
6. **Zero parallax re-proved after all changes.** Westward 4 u/s pan, frame
   170 → 470 of the 480-frame movie: the pure-black disc's centroid moved
   (1279.35, 457.39) → (1281.13, 458.17) px — ~1.9 px of drift while the
   camera translated ~57 world units; the playfield slides visibly on the
   same frames. Evidence:
   `evidence/skypass-v3/zeroparallax_panmove_f170-f470.png`.

Heavy movies/stills for this round live under `/d/ops-work/fardepth/skypass-v3/`.
Commits: `20cfa5e3` (item 1), `baf5f15d` (2), `d84f5564` (3), `6c78058b` (4).

## Sky-pass black hole v2 (skypass round, after realism kit 1)

User ruling: the far black hole moves from a world-space 3D object into the
SKY PASS (`map_sky.gdshader`) as one direction-locked feature — same semantics
as the nebula patch: it is a fixed direction on the sky dome, so it has zero
parallax under WASD pans and no far-clip ceiling (the camera's Far=400 never
clips the sky). One commit per item; evidence in `evidence/skypass/`, heavy
movies/stills under `/d/ops-work/fardepth/skypass/`.

A. **Direction-locked fold.** The old pass (`MapBlackHoleVisual` + the two
   `black_hole_*.gdshader` files, horizon sphere + additive quad at world
   distance 260) is REMOVED, not dual-drawn — no double black hole anywhere.
   The hole now renders inside `map_sky.gdshader`: shadow + ring + glow are
   one sky-pass element at the fixed sky direction (`MapAtmosphereSettings.
   BlackHoleDirection`), sizes are angular radians now (0.10 rad shadow /
   0.19 rad glow — the same on-screen size the old 260-unit tuning produced).
   The closedup law holds in code: the new pass is reachable only through
   `MapBackdrop`, which is built only by `MapController` (the map scene);
   battle's `SpaceBackdrop` (`src/battle/presentation/graphics/SpaceBackdrop.cs`)
   uses its own `PanoramaSkyMaterial` and never touches `map_sky.gdshader`
   (`grep -rn map_sky src/` → MapBackdrop.cs only).
   Evidence: `itemA_skypass-fold.png` (world-space pass vs sky-pass, rig pose
   62,25,42).
B. **Solid black shadow, clean edge, glow never on it.** The shadow is an
   opaque pure-black disc; the glow's brightness mask is zero inside 1.02x of
   the shadow radius, so light cannot bleed onto the disc and the rim is drawn
   under the light inside this one element. Evidence:
   `itemB_shadow.png` (edge detail before/after).
C. **Ring + glow like the real references** (the EHT/GMVA class strips in
   `real-vs-ours-v4.png`): a broad soft photon-ring band — gaussian sigma
   0.105 rad, visible thickness ~0.17 rad, which is more than 1/3 of the glow
   diameter (0.38 rad) — over a wide diffuse falloff that runs all the way to
   the glow edge. No thin knife line, no single-pixel cut, shadow small
   inside generous light (shadow diameter ~ half the glow diameter, like the
   reference stills). Evidence: `itemC_ring-glow.png`.
D. **One side brighter.** A doppler crescent brightens one side of the ring
   (amount 0.85); its side was tuned against the rig so the bright band sits
   lower-left at the evidence pose — the same placement the kit-1 item-4
   world-space doppler produced at that pose. Honest tradeoff under
   direction-lock: in a sky pass the image is pinned to the ring the way a
   real photograph pins it, so the "bright side follows the view" mechanism of
   the world-space pass is gone; what is kept is the calibrated bright-side
   placement. Evidence: `itemD_doppler.png`.
E. **Zero parallax under a westward camera move.** The dev rig gained an
   optional pan on `--mappose` (fields 5/6 = west/north units per second;
   dev-only, no argument → no effect). At pose 62,25,42 with a 4 u/s westward
   pivot slide, the pure-black disc's centroid moved (1343.49, 533.71) →
   (1344.63, 532.57) px from frame 170 to frame 470 — ~1.1 px of drift while
   the camera translated ~57 world units; the playfield elements on the same
   frames slide visibly. Evidence: `itemE_zeroparallax.png` (f170 vs f470,
   same still-pair crop), `itemE_no-clip.png` (d72 stare: old world-space pass
   vs sky-pass — no clip ceiling involved anymore).
   Build/test before the renders: `dotnet build` clean; `dotnet test` 1601
   succeeded, the same 2 pre-existing `BattleSpawnPilotTests` failures as on
   the untouched base commit.

## Realism kit 1 (the review round after the 3D rebuild)

The geek review of the first 3D pass said: still reads like a decal because the
disk is a frozen picture, the rotation is rigid, the rim is a fat blur, the
bright side is painted on, depth does not interact, and there is no air/distance
envelope at all. Six small items, one commit each, evidence under
`evidence/kit1/` and the heavy movie files under `/d/ops-work/fardepth/`.

Evidence rig for every still in this round: the already-committed `--mappose`
capture arg, pose `62,25,42` (the pose where the hole is in frame, the star is
not on top of it), `--load-save` so the system layout is identical between
before/after renders (a fresh `--new-run` is randomly laid out, so a pixel
comparison would be apples-to-oranges), frame 180 = 6.0 s settled.

1. **The disk breathes (alive, not a photo).** The shader now adds a slow plus
   a fast layer of TIME-driven noise to the disk energy, so its brightness
   drifts instead of holding one value. Measured on the disk crop: before, the
   mean luma moved 0.1 units over frames 60-210 (dead flat); after, it moves
   2.7 units across the same window. Evidence:
   `evidence/kit1/item1_alive-energy.png` (left: old shader; middle/right: new
   shader at two times — the middle is the "swell", the right the "trough").

2. **The inner band laps the outer (differential rotation).** The swirl is now
   driven per-radius: drift rate = `swirl_speed * (1.45 - 0.95*t)`, i.e. the
   inner edge drifts ~2.9x faster than the outer edge, both on the same TIME
   clock. Before (old shader) every radius turned as one rigid stencil; after,
   the arm structure shears (wind-up) as time passes. Evidence:
   `evidence/kit1/item2_inner-laps-outer.png` — same rig pose, same fixed
   layout, before/after at t=2s and t=29s. Honest note: tracking a scrolling
   noise texture with the eye is hard on stills, so the strip mostly
   shows the late-time grain field is animated and wound deeper at the rim
   (see `item2_inner-rim.png` for the inner-rim detail strip); the differential
   shape itself is in the shader line above and its plain-words statement here.
   (Two numeric probes attempted on the recording — angular correlation and
   rotation fitting — both hit the noise pattern's too-few-repetitions aliasing
   at these cell sizes, so the visual strip is the accepted evidence.)

3. **The rim is a knife edge, not a fat blur.** The hot-edge falloff across
   the annulus is now a tunable `rim_power` (set 14, was an implicit 5.5), so
   the pale glow band wrapped around the hole is much thinner and sharper and
   the warm annulus behind it shows through. Evidence:
   `evidence/kit1/item3_thinner-rim.png` (left: fat rim; right: thinner rim,
   same rig pose and t=6s on both sides).

4. **The bright side follows the view, not a painted patch.** Doppler beaming
   is now computed per fragment from the camera ray of the disk material's
   orbital direction (`dot(orbital_dir, to_camera)`), not from a fixed
   quad-local edge painted at build time. At the pose the map was tuned for
   (yaw 62) both arrangements agree
   (`item4_same-pose-tuned.png`); orbit the camera (rig drift 4 deg/s,
   yaw 45 -> 69) and the bright band re-places on the ring
   (`item4_doppler-follows-view.png`), while the baked version keeps the same
   patch of ring lit at every yaw.

5. **Depth interacts (or at least is allowed to).** The accretion disk said
   `depth_draw_never`, i.e. the material formally refused to participate in the
   depth buffer. The flag was removed so the disk/horizon read and respect
   depth like other world geometry. Honest result: at every tested pose
   (near/far/different yaw, `item5_depth-write.png`) the frames are identical —
   the transparent pass was already depth-testing against the star sphere, so
   the flag was latent, not load-bearing. Kept the removal anyway: it costs
   nothing and enforces that the hole can occlude things going forward, with
   zero regression. The additive glow itself can't be pixel-occluded (that's
   what blend_add means), which is a later-round question, not a kit-1 one.

6. **A distance envelope (scene fog refused, the disk got one instead).**
   First attempt: turn the environment's fog on. Honest result: regression —
   every transparent surface gets fogged per fragment, so the whole disk quad
   got a grey slab instead of an envelope (`item6_fog-regression.png`, left).
   The envelope lives on the disk itself now: the additive disk dims from
   the hole's own distance (260 world units) away, fully recessed 120 units
   later, so pulling the camera back recedes it into the dark and zooming in
   restores it (`item6_env-far-d72.png` vs `item6_env-close-d14.png`).
   Same-pose disk-crop luma, before -> envelope: close view -1.1%, mid -3.3%,
   far view -9.8% — monotonic with viewing distance.
   Two rig facts found by rendering, so future kits don't rediscover them:
   - In this Godot build, `VERTEX` in a spatial shader's `fragment()` is
     model-space, not view-space; the world-space geometry terms (and the item
     4 doppler ray) now come from `vertex()` varyings, which fixed the
     envelope math (the fragment version computed phantom near-camera points).
     Item 4's committed strip evidence stands on its own (the bright-band
     response was filmed, whatever the underlying precision was).
   - `--mappose` distances were silently clamped to the cinematic mode's
     distance cap of 14 in every movie render up to now — so all kit-1
     "same pose" comparisons are still valid side-by-side (both sides shared
     the applied pose), but pose distance labels above 14 were inert. The
     dev-only capture path now snaps against the camera's own zoom band
     (4..72), so pose numbers mean what they say; the final camera-move strip
     and item 6's distance sweep are the first renders with real distances.

## Final proof: occlusion + depth respond after this round

`final_camera-move_depth-respond.png` — same pose family rendered at true
camera distances 14 / 42 / 72 while pulling back: the black horizon keeps
occluding (starlight and disk grain disappear behind it; the asteroid, moon
and station read in front of the disk), and the far side recedes — the disk
dims with distance like a thing that is 260 units away, not a sticker.

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
| Black hole | ~~sky decal~~ ~~world-space horizon sphere + additive disk~~ → sky-pass feature inside `map_sky.gdshader` | Direction-locked sky-pass pixels: zero parallax, no far-clip ceiling (skypass v2 round above) |
| Dust cards | world-space billboards | Keep — they already respond to camera and sit on the playfield scale |
| Sun / planets / stations | world-space meshes | Untouched (near/mid layers, already 3D) |
