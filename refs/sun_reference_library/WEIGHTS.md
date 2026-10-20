# Weights

The user pinned this library and its weights.  Where two references disagree,
the heavier one wins.  `ours_*` has weight zero: it is the defendant, kept only
for comparison.

| Weight | File | Role |
|--------|------|------|
| **HIGHEST (north star)** | `dr_graceful_eruption.jpg` | the motion and choreography lead: a graceful rise, white hot at the base, red wisps streaming away |
| HIGH | `dr_burst_eruption_composite.jpg` | the eruptive burst envelope |
| HIGH | `dr_giant_prominence_erupted.jpg` | prominence scale and thickness |
| HIGH | `dr_solar_prominence_1_2000px.jpg` | prominence brightness against the disc |
| HIGH | `dr_stereo_enormous_prominence.jpg` | prominence colour and contrast |
| HIGH | `real_eruption_2011_6080px.jpg` | the eruption choreography at high resolution |
| HIGH | `real_xflare_2024_closeup.jpg` | the flare's brightness and speed |
| HIGH | `real_solarorbiter_closeup.png` | surface and prominence detail |
| HIGH | `ha_prishtina.jpg` | H-alpha fine fibre structure |
| HIGH | `solo_moss.ultracloseup.webp` | the micro-detail ambition |
| **TEXTURE BOSS (braid density)** | `real_loops_2011_closeup.png` | how many threads a loop carries |
| **TEXTURE BOSS (braid density)** | `gen_closeup_3828_f003.png`, `gen_closeup_3828_f006.png` | how many threads a loop carries |
| **TARGET (surface granulation)** | `dkist_granulation_realclosest.jpg` | white hot cell cores, dark narrow lanes, high contrast |
| **TARGET (corona truth)** | `eclipse_2024_hdr.jpg`, `eclipse_2019_esa.jpg` | streamer arcs, anisotropic decay, starting at the limb |
| MEDIUM | `ha_filament_threads.jpg` | threads, not tubes; fibres flowing in one direction |
| MEDIUM | `gen_closeup_5649_f005.png`, `gen_closeup_5649_f010.png` | surface granulation |
| MEDIUM | `closeup_sdo304_firstlight_01.png`, `_02.png` | prominence placement and the limb |
| MEDIUM | `closeup_sdo304_prominence_crop_01.png`, `_05.png` | prominence thread structure |
| ZERO (defendant) | `ours_current_render.png`, `ours_presence.png` | comparison only |

## Per-element deltas: what the weights changed

| Element | Before round 5 | After round 5 | Driven by |
|---------|----------------|---------------|-----------|
| Surface granulation | a soft mottled fbm plus the NASA map, contrast 0.19 against the reference's 0.70 | a **cellular** field: bright cell cores separated by dark narrow lanes, 118 cells across the disc | `dkist_granulation_realclosest.jpg` (target) |
| Corona | an isotropic mottle (a polar noise sample was removed in round 4 because it drew spokes) | **radial streamer arcs**: broad arcs from incommensurate angular sines, fine strands inside them, fading with the radius, starting at the limb | `eclipse_2024_hdr.jpg`, `eclipse_2019_esa.jpg` (truth) |
| Prominence material | a bundle of threads stretched along the loop, with a braid | the same, plus: the noise phase is **advected along the loop** so the plasma visibly flows, the braid has a second harmonic so the threads **knot**, the filament's width is modulated so the edge **feathers** into hairs, and the knot points brighten | `dr_graceful_eruption.jpg` (north star), `ha_filament_threads.jpg`, `real_loops_2011_closeup.png` |
| Surface / map blend | the NASA map at 0.72, so its large white patches dominated | 0.45, so the cellular granulation is the dominant texture and the map is a modulation under it | `dkist_granulation_realclosest.jpg` |
| Asteroid material | the shipped pack is a **metallic** variant, so a lit rock only shows a specular highlight and reads as a black silhouette | the material is replaced with a rough dielectric so the star's light falls on it and the terminator reads | the axis B requirement |
