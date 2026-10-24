# FAIL-FIRST: why every sun change so far reads as "only less prominence"

Verdict first: **the user's runtime DOES run the tuned shader stack, and the
tuning was ratified at the wrong viewing pose.** At the sun_test closeup the
ratified palette (S1..S4 + repair round) is richly visible; at the map's real
poses the shader features collapse below visibility, and the only change that
survives above the visibility threshold is exactly the change we made
deliberately — the disc energy drop (0.70 → ~0.26) and the corona strand
dial-down. To a player that is one thing: "less prominent."

## 1. Runtime trace (what actually draws the starmap star)

`scenes/main.tscn` (start menu, `StartMenu.OnNewGame`) →
`Session.BeginMapFromMenuAsync()` → navigate to `res://scenes/map.tscn`.

Inside `map.tscn` the star is built **entirely in C#** — no `.tscn` ext_resource
references any sun shader:

- `MapController._Ready` → `_view.Build(...)` → `MapView:560`
  `StarVisual = MapCelestialVisuals.AddStar(root, seed, star.Radius, _atmosphere)`
- `MapCelestialVisuals.cs:40-44` → `SunVisual.Create(visualRadius, BuildStarPalette(seed, settings))`
  + `SunGlareOverlay` (intensity 0.55, streak 0.10, opening beat 1.1 s)
- Shaders used: `assets/shaders/sun_surface.gdshader`, `sun_corona.gdshader`,
  `sun_prominences.gdshader`, `sun_glare.gdshader`, `sun_dip.gdshader`
- Star size: `Star.DefaultRadius = 128` grid points × `1/32` = **4.0 world units**;
  `MapCamera` distance range 4..72, default 42 → boot pose subtense ≈ 0.55 of
  frame height (measured: ~550 px disc at 1920×1080, frame 8).

The same `_view.StarVisual` path is what renders with `--new-run` / the menu
boot; the movie asserts earlier (map_nrepair.avi) come from precisely this path.

## 2. Diff against the user's other two lines

- `origin/master` (35 commits ahead of the branch point, all UI/battle/rebalance
  — **none touch the map sun**): `MapCelestialVisuals.AddStar` there is the OLD
  look — `SunTexturePath` textured sphere + `StarCorona.Create` halo (only
  `star_corona.gdshader` exists on master). Porting our branch onto master keeps
  our sun; master never had the shader stack.
- `../grim-space-new` (branch `graphic/starmap-sun-improvement`, dirty): the SAME
  old look **plus their own `SunProminencePrototype` added to `AddStar`** — i.e.
  the user's own sun-improvement direction already in flight: prominences as the
  headline feature on the OLD star. Our branch neither merged nor matched this.
- The user's live build IS the branch: the reported NRE stacks map line-for-line
  to this repo at 5abe7939 (`MC._Ready:90`, `MC._Process:383`, `SH._Ready:58`,
  `SH:263`), not to ../grim-space-new (75/313/28/77) and not to master's older
  line numbers.

## 3. Verdict — named failure point

**The palette was ratified against the sun_test closeup, not the map's live
poses.** Evidence from the real-runtime render (320 frames, windowed movie,
`--new-run` board):

- Frame 8 (opening pose, disc ≈ 550 px): the sun is a **smooth gradient ball
  with a hard rim** — granulation (34 cells ≈ 16 px/cell) dapples only near the
  limb and the centre tones blow out to a flat gradient; plage (6 cells) and
  the pipeline-cast prominence loop (0.20 × radius ≈ 55 px arm — and it can be
  on the far side of the sphere) are not readable. There is **no corona glow
  beyond the rim** (CoronaStrength 0.12, halo reach 0.5 was tuned to be subtle).
- The whole 320-frame boot holds that pose; the sun never stops being
  "a plain orange ball."
- Pre-branch comparison (worktree at merge-base 65e38b93, same boot flow): the
  camera parks elsewhere and the sun is **off-screen the entire boot** — the
  old star look was only ever inspected by the user mid-play, where it had a
  visible rim glow (StarCorona). Subtracting that glow is why the user's
  compositional memory reads "only less prominence."

So the tuned change set split into (a) input the map poses can't display
(granulation detail, single-loop prominence, subtle corona) and (b) one change
every pose CAN display — the energy/rim reduction. The user is right.

## 4. Render proof (commands + paths)

Branch (current, the tuned runtime):
```
Godot --path . --write-movie /tmp/opencode/map_nrepair.avi --fixed-fps 30 \
  --quit-after 320 --scene res://scenes/map.tscn -- --new-run
# -> 0 NRE, storyboard frames at /tmp/opencode/storyboard/new_8.png,new_260.png,
#    contact sheet new_sheet.png (sun on-screen the whole boot, flat ball)
```

Pre-branch base (65e38b93, worktree render-base-65e38b93, throwaway boot
harness `scenes/dev/BaseRenderBoot.*` — disposable, game code untouched):
```
Godot --path <worktree> --write-movie /tmp/opencode/map_base.avi --fixed-fps 30 \
  --quit-after 320 --scene res://scenes/dev/BaseRenderBoot.tscn
# -> 0 NRE; contact sheet base_sheet.png: THE SUN NEVER ENTERS FRAME — the old
#    look cannot be photographed from a passive boot; the only old-look sun
#    reference is the user's mid-play memory.
```

(Note: rendering with `--load-save` to see the user's actual locked layout is
intentionally NOT done — a loaded run arms the autosave path against the
user's 30 MB save. `--new-run` only.)

## 5. Smallest redo plan (target the actual failure point)

1. **Re-ratify the palette at the map pose, not the closeup.** Add the map boot
   pose (and one mid-zoom pose) to the acceptance renders next to sun_test.
   Concretely raise what the map can actually show: CoronaStrength 0.12 →
   limb-visible rim glow, CoronaHaloReach up from 0.5, plage/limb darkening
   contrast, and a dominant-arc cast that must pass through the visible
   hemisphere (or cast the loop so its apex is guaranteed on camera at the
   default opening bearing).
2. **Prominence that survives 4.0-u scale.** ProminenceHeight 0.20 → ≥0.35 R and
   thicker arms (ProminenceStrength), still ONE role at a time (pipeline law),
   but apertured/readable at 550 px disc and ~250 px mid-zoom disc. Cross-check
   against the user's own `SunProminencePrototype` attempt in ../grim-space-new:
   that is the look they are trying to reach with their own branch.
3. **Own the energy signature.** The 0.70→0.26 disc drop was correct for the
   closeup blowout but it removed the "burning" read at distance — re-quote the
   ratified closeup value AT closeup and allow a distance-scaled (or pose-
   scaled) energy curve instead of one global number, if the shader exposes a
   distance uniform; otherwise re-balance limb darkening to keep the core hot
   and the rim alive.
4. **Probe per-commit.** One movie render per palette change at the boot pose +
   one mid-zoom frame; accept only when a side-by-side (base-boot, new-boot)
   shows a NEW feature (rim glow, arc, limb texture) — not just a dimmer ball.

Rendered evidence kept at `/tmp/opencode/storyboard/` (new_*.png, base_*.png,
both contact sheets, sun crops).
