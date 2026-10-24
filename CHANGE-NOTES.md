## verdict 8 - aura capped

The big additive cloud around the map sun is gone. `CoronaStrength` 0.40 → 0.18
and `CoronaHaloReach` 1.0 → 0.5, map star's palette only: the halo tail now ends
half a disc radius past the limb and is faint. The minutes law, prominence
height and every other sun parameter are untouched. Strips:
`refs/comparison/aura_boot_before_after.png`, `aura_zoom_before_after.png`;
the close-up regression pair is unchanged (99.92 % identical, noise-level).

# Sun redo — what changed

The map's sun was tuned against the close-up test scene, not the map; this
redo re-tunes it at the poses the player sees.

## The first thing you will feel

Prominences now **grow over minutes, not seconds**: bare limb to held loop takes
**5 minutes at 1x** — `SunProminenceCasting.GrowthSecondsAt1x = 300f`, the one
named constant, mid-way in the requested 3–6 minute band. The loop then holds
and dies to exactly zero; never a half-lit leftover. One loop at a time, seeded
cast and quiet gaps are unchanged. Higher game speed shortens growth
proportionally ("5 minutes" means at 1x).

The clock used to be the 14-second rumble loop, so every life restarted with
the soundtrack. It now runs on its own monotonic clock — that is what makes
minutes-scale lives possible.

## What the eye gets at the map poses

- **Corona halo** (`CoronaStrength` 0.12 → 0.40, `CoronaHaloReach` 0.5 → 1.0):
  the dark gap between disc and space fills with warm light, so the sun reads as
  burning instead of pasted on — at boot and zoomed.
- **Surface contrast** (`GranulationStrength` 0.65 → 0.85, `PlageStrength`
  0.45 → 0.75): cell texture and bright plage survive the ~550 px boot disc and
  the zoomed disc.
- **Prominence** (`ProminenceHeight` 0.20 → 0.38 R, `ProminenceStrength` 1.10 →
  1.9, span 0.42, filament width 0.030): taller, thicker arms that read as a
  loop, not a wire.
- **Guaranteed on the visible rim**: the shader measures which way its
  billboard's up axis points on screen each frame and pins the seeded site to the lower
  rim, so the arc cannot be born on the limb the opening camera crops.

## Evidence (`refs/comparison/`)

- `redo_map_boot_base.png` → `redo_map_boot_final.png` 
- `redo_map_zoom_base.png` → `redo_map_zoom_final.png` 
- `redo_mapstar_arc_closeup.png`
- `redo_closeup_before.png` → `redo_closeup_after.png`

Close-up regression measured: 99.06 % of the frame is pixel-identical (disc,
granulation, corona, colours); every changed pixel is the prominence's seeded
site, the change this redo mandates. The default close-up palette keeps the
shader's constants.

Timing proof (map boot log):

```
[ProminenceCasting] growth at 1x = 300s (user band 180..360s);
turn 0 = 0.00..8.85 design s, growth 1.24 design s x scale 242.2 =
life 2143 player s (growth 300 s).
```

One life spans 35.7 player minutes: 5 growing, ~26 held, 5 dying to zero. The compressed evidence render
(`--casting-fast 30`, 30 casting seconds per movie second) shows the same curve
in a 14-second movie.
