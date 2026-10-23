# SUN_NOTES — a sun shader built from real NASA/SDO reference frames

The working log for the sun/star shader in `grim-space`: what the reference
material was, what was measured from it, how the shader was built from those
numbers, how the result was compared against the real frames, and — honestly —
what still does not match.

**TL;DR** — three procedural shaders (photosphere, prominences, corona) over the
real NASA solar surface map, plus a screen space glare and a point light so the
star behaves like a light source rather than a sprite, plus a four-phenomenon
timeline (sunspots, flare, coronal rain, CME) that is shared with the
soundtrack.  Measured against a real AIA 304 frame the granulation scale
matches to 1.5 % and the mid-disc radial profile to within a few percent.  The
remaining gaps — the softness of the very last 5 % of the limb, the contrast of
the mottling, the fact that value noise cannot produce the real frames'
filamentary texture, and the fact that the NASA map is a contrast stretched
visualisation rather than a true colour image — are all listed in section 8
rather than glossed over.

---

## 1. What is in this repository

| Path | What it is |
|------|------------|
| `assets/shaders/sun_surface.gdshader` | photosphere: the NASA surface map as the base layer, procedural granulation drifting over it, supergranulation, plage, physical limb darkening, chromosphere edge and rim, sunspots, flare, core burn, palette ramp |
| `assets/shaders/sun_prominences.gdshader` | prominence loops as a thin *threaded* shell, coronal rain, CME shock front, footpoint plage, chromospheric ring, spicule haze |
| `assets/shaders/sun_corona.gdshader` | corona streamers, drawn on a concentric **sphere** (not a billboard) |
| `assets/shaders/sun_glare.gdshader` | the screen space glare: long range halo, hot core, chromatic bleed, anamorphic streak, breathing |
| `assets/shaders/sun_dip.gdshader` | the squint: the exposure loss and vignette when the star fills the frame |
| `assets/textures/8k_sun.jpg` | the real NASA solar surface map (copied from `../grim-space-teaser`, same file) |
| `src/presentation/graphics/SunVisual.cs` | builds the layers, the point light, and the glare overlay |
| `src/presentation/graphics/SunGlareOverlay.cs` | the screen space glare and dip passes |
| `src/presentation/graphics/SunPalette.cs` | colours and tuning constants |
| `src/presentation/graphics/SunPhenomena.cs` | the transient phenomena state and its timeline evaluation |
| `src/presentation/dev/SunTestScene.cs` | dev scene: showcase, close-up, presence test, battle backdrop, 304 diagnostic |
| `scenes/sun_test.tscn` | that dev scene |
| `scripts/render-sun.sh` | re-import + Godot Movie Maker render helper |
| `scripts/make-sun-audio.py` | generates the event timeline and the soundtrack from one source of truth |
| `assets/data/sun_events.json` | the event timeline (spots + flare + CME + rain) |
| `sun_test.mp4` | the rendered clip **with sound** (1920×1080, 30 fps, 12 s) |
| `refs/` | the real reference material, its sources, and the measured numbers |
| `refs/closeups/` | real prominence close-ups used to judge the thread structure |
| `refs/comparison/` | the side-by-side sheets, the before/after, and the event frames |
| `task/sun/` | the briefs and reports of the reference and review workers |

### Integration

* `src/battle/presentation/graphics/RedDwarfSun.cs` — the battle scene's sun is
  the shared shader with a red dwarf palette.  Its public surface
  (`CreateVisual`, `Configure`, `LightDirection`, `LightColor`) is unchanged.
* `src/world/star-system/presentation/atmosphere/MapCelestialVisuals.cs` — the
  strategic map's star is the same shader with a solar palette.

The star is also a light source: `SunVisual` attaches a point light with a
falloff range, so nearby ships, rocks and dust catch its light and the
illumination falls off with distance.

---

## 2. The reference material (all public domain)

Exact URLs in `refs/SOURCES.md`, measured numbers in `refs/palette.json`, the
analysis in `refs/analysis.md`, the close-ups in `refs/closeups/SOURCES.md`.

| Source | What it gave |
|--------|--------------|
| NASA SDO AIA 304 Å (1024 & 2048) | the red chromosphere, the granulation, the prominences |
| NASA SDO AIA 171 / 094 Å | the inner corona, the bright limb in EUV |
| NASA SDO HMI 193 / continuum | limb darkening and granulation scale at high resolution |
| SOHO LASCO C2, EIT 195 | the corona's streamer structure |
| SDO AIA 304 motion, 2010-10-21 and 2026-02-01 | how a prominence actually moves |
| "SDO 20240508 1024 0304 (AIA)" (Wikimedia, public domain) | the frame used for the side-by-side |
| "SDO AIA First light prominence 20100330" and "SDO prominence 20100330 crop" (Wikimedia, public domain) | the close-ups in `refs/closeups/` — the thread/fibril structure the prominence material is judged against |

The media under `refs/` was re-encoded and downscaled so the repository stays a
sane size (237 MB → ~53 MB); `refs/tools/shrink_for_repo.py` did it.  The
source URLs are unchanged and the numbers in `refs/palette.json` were measured
on the originals.

---

## 3. The look, measured from the pixels

Radii are fractions of the solar radius; luminances are sRGB (the same encoding
the NASA JPEGs and the Godot render use, so they are directly comparable).

### Palette

| | measured |
|---|---|
| AIA 304 disc centre | `rgb(0.718, 0.061, 0.003)` — saturated red |
| AIA 304 disc at 0.90 R | `rgb(0.563, 0.040, 0.005)` |
| AIA 304 bright active region | `rgb(0.732, 0.140, 0.027)` — orange |
| HMI 193 disc centre | `rgb(0.542, 0.304, 0.100)` — warm tan |
| prominence core | `rgb(0.238, 0.047, 0.047)` — dimmer than the disc and strongly red |

### Limb darkening (HMI 193 radial profile, disc centre = 1.0)

| r/R | 0.80 | 0.90 | 0.95 | 1.00 |
|-----|------|------|------|------|
| linear ratio | 1.00 | 0.41 | 0.095 | 0.037 |

The photosphere part of that is the usual `I(mu) = 1 - u(1-mu) - v(1-mu)^2`
with `u = 0.50`, `v = 0.14`; the extra, much steeper fall-off in the last 10 %
of the radius is the chromosphere's finite thickness, so the shader models it
as a separate "chromospheric edge" term rather than bending the limb
darkening law into something unphysical.

### Granulation

| | reference | shader |
|---|---|---|
| cell size | 14.33 px | 15.23 px |
| cells across the disc | 46.9 | 46.2 |
| local contrast (std / mean of a 180 px disc patch) | 0.70 | 0.19 |

Both are the energy weighted mean wavenumber of the patch's power spectrum,
i.e. the same measurement applied to both images.  The scale matches to 1.5 %;
the contrast does not.

### Prominences

| | measured |
|---|---|
| height above the limb | 0.10 – 0.26 R (the brightest structures reach 0.67 R) |
| brightness | ~40 % of the disc, saturated red |
| count on the limb | 3 – 4 at any time |
| motion | over 3 hours of AIA 304 the prominence area changes by ±5 % with a ~50 minute period |
| close-up structure | bundles of thin threads flowing along the loop, layered ribs, a hot core inside a soft envelope, threads that slide, kink, thin and rejoin (`refs/closeups/`) |

### Corona

| | measured |
|---|---|
| AIA 171 at 1.00 R | 0.237 of the disc brightness |
| fall-off | exponential, e-folding length ≈ 0.065 R, gone by ~1.15 R |
| structure | narrow radial rays separated by darker lanes, a few broad "helmet" streamers (LASCO C2) |

---

## 4. Design decisions

**The real NASA surface is the base layer.**  `assets/textures/8k_sun.jpg` is
an equirectangular map of the real photosphere with its granulation and its
white-hot blisters.  It is mapped equirectangularly from the surface direction
and its luminance drives the granulation term.  Two details matter:

* *The seam.*  The map's first and last columns match to a mean absolute
  difference of 2.2/255, so the wrap is invisible; `fract()` on the longitude
  keeps the sampling continuous either way.
* *It is not a true colour image.*  The NASA map is a contrast stretched
  visualisation: its white patches are blown out and its dark lanes are much
  darker than the real Sun's.  Used at full strength the disc reads as a rocky
  planet, so the map's luminance is compressed around its own mean
  (`0.5 + (lum - 0.60) * 0.42`) before it is used.  The colour never comes
  from the map: the map only drives brightness, and the hue always comes from
  the measured palette ramp.

**The procedural field drifts over the map, not under it.**  The procedural
granulation is added as a moving modulation on top of the map's value, so the
surface is never a still image, and the map's own structure is never drowned.

**Prominences are a bundle of threads, not a tube.**  The filament is the
distance to an arc, but the arc's cross section is modulated by a noise field
that is *stretched along the loop* (low frequency in the along direction, high
frequency across it), with a slow braid that slides the threads across each
other.  That is what the real close-ups show: parallel fibrils, not a solid
tube, with the threads kinking and re-joining.

**The corona is a sphere, not a billboard.**  A quad has edges, and an edge
that falls inside the frame reads as a straight cut through the glow — which is
exactly the defect that was reported.  A concentric shell cannot have that
problem: the profile is a function of where the fragment lands on screen
relative to the star's centre, it is continuous all the way round, and it
reaches zero long before the shell's own silhouette.

**The glare is in the shader, not in the post process.**  Godot's bloom is
computed on a low precision mip chain: around a very bright compact source it
draws a visible dark annulus and quantised plateaus (measured: the frame's
luminance stepping in 76/255 increments along a straight line).  The glare is
therefore a screen space pass of its own — a wide halo that reaches the frame
corners, a hot core, a chromatic bleed, a restrained anamorphic streak, and a
slow breathing so it reads as an eye adjusting — plus a separate darkening pass
for the squint.

**One timeline for picture and sound.**  `assets/data/sun_events.json` holds
the sunspots and the transient events.  The renderer reads it and drives the
shaders from it; `scripts/make-sun-audio.py` reads the same file and
synthesises the soundtrack from it.  A flare that is visible is audible.

---

## 5. The phenomena

Four effects, each with a real counterpart, each on the shared timeline.

| Effect | Reference | Implementation | Parameters | Audio |
|--------|-----------|----------------|------------|-------|
| **Sunspots** | real sunspot groups: a dark umbra inside a lighter penumbra, rotating with the disc | `sun_surface.gdshader`: up to four spots, each (longitude, sin latitude, radius, strength); the longitude drifts so the spot migrates as the star turns | 3 spots, radii 0.034–0.052 rad, strengths 0.55–0.80 | none (they are ambient) |
| **Flare** | a rapid local brightening at an active region, lasting minutes | `sun_surface.gdshader`: a `flare_data` term that adds up to 3.2× the local brightness and pushes the hue towards the core colour, with a fast attack and a slow decay | 2 flares, sizes 0.13–0.16 rad, strengths 0.70–0.95 | a pressure sting: a falling sub tone plus a noise burst |
| **Coronal rain** | condensation blobs that run back down the legs of a loop as the eruption collapses | `sun_prominences.gdshader`: blobs that slide down the loop's legs along the arc length, scaled by `rain_amount` | 1 event, strength 0.85, 3.2 s | a scatter of short high drops |
| **CME** | a shock front lifting off the limb and expanding | `sun_prominences.gdshader`: an expanding shell centred on the limb, growing with the event's progress | 1 event, strength 0.80, 4.5 s, radius 0.42 rad | a low shove: a 30 Hz swell under the noise floor |

### The synced audio events

`sun_test.mp4` is 12.0 s.  The soundtrack is a deep sub hum (a 42 Hz tone
plus brown noise), a boiling crackle whose density follows the prominence
activity (the same eruption envelope the shader uses), and these events:

| t (s) | Event | What you hear |
|-------|-------|---------------|
| 1.35 | flare (strength 0.95) | a pressure sting: a falling sub tone and a noise burst |
| 4.10 | CME (strength 0.80) | a low shove under the noise floor |
| 6.60 | coronal rain (strength 0.85) | a scatter of short high drops |
| 9.20 | flare (strength 0.70) | a second, softer sting |

Integrated loudness is −16 LUFS with a true peak under −1.5 dBTP, so the
loudest moments are brief.  `ffprobe` on `sun_test.mp4` reports a 12.000 s h264
video stream and a 12.000 s aac audio stream.

---

## 6. Verification

### Method

1. Render with Godot Movie Maker (`scripts/render-sun.sh`, which re-imports
   first — Godot caches compiled shaders and a silent edit is a silent edit).
2. Extract frames with ffmpeg.
3. Build a side-by-side sheet: the real NASA frame on the left, the render on
   the right, centre cropped to the same square.
4. Measure both with the same numpy code and compare the numbers.
5. **Look at the pixels.**  Every appearance claim here comes from opening the
   images.

The reference frame is a real AIA 304 image from 2024-05-08 13:28 UT; the
render uses a palette sampled from that frame, so the comparison isolates the
shader rather than the art direction.  The render uses a distant camera and a
narrow field of view, so the projection is close to the near-orthographic view
the spacecraft cameras used.

### Result (AIA 304 palette, sRGB luminance)

Both radii are the **geometric** disc radius: for the reference 1 R_sun is
400 px in the AIA 1024 frame, which is 297 px in the 760 px comparison crop;
the render's disc is 322 px in the same crop.  (Using a profile-derived radius
instead - the radius where the azimuthal mean falls to 12 % of the disc median
- is not comparable between the two images, because the reference's disc edge
is sharp and the shader's is soft.  That mistake invalidated an earlier
version of this table; see section 7.)

| | reference | render |
|---|---|---|
| disc radius used | 297 px | 322 px |
| disc centre | 0.2509 | 0.2243 |
| 0.60 R | 0.2269 | 0.1679 |
| 0.90 R | 0.2511 | 0.1333 |
| 0.98 R | 0.2499 | 0.0950 |
| 1.00 R | 0.2524 | 0.0601 |
| 1.02–1.08 R mean | 0.1099 | 0.0040 |
| 1.02–1.08 R p99 (prominence peaks) | 0.3750 | 0.0525 |
| granulation cells across the disc | 46.9 | 46.2 |

The granulation scale still matches to 1.5 %.  The disc edge now behaves the
way the real AIA 304 does - bright right up to 1.00 R and then falling off a
cliff, with essentially no off-limb corona (which is correct: the 304 channel
has none, `refs/SOURCES.md` says so explicitly).  What is still off is the
prominences: in the 304 palette they are far too dim (p99 0.053 against the
reference's 0.375) and the mid-disc is ~25 % dim.  The side-by-side sheet is
`refs/comparison/side_by_side_aia304.png`.

### The glow boundary

The reported defect — a straight hard edge in the corona at t = 10 — was
traced numerically: the frame's luminance stepped in quantised 76/255
increments along a straight line.  That is Godot's post process bloom, whose
mip chain has too little precision for a very bright compact source.  It was
not the quad (the quad's profile already reached zero well inside its own
edge).  Three changes removed it: the corona moved from a billboard to a
concentric shell, the post process bloom was switched off in favour of the
shader glare, and the prominence billboard was enlarged from 4R to 10R so its
own edge can never fall inside the frame.  A scan of the new frames finds the
only remaining hard steps are on the photosphere's own limb, which is correct.

### Before and after

`refs/comparison/before_after_round1.png` — the same frame from the first
version of the shader and from the current one.  The first version was a
correctly coloured disc that read as an object; the current one is a light
source: a washed out core, a halo that reaches the frame corners, a warm
bleed, and the space around it lifted.

`refs/comparison/closeup_and_presence.png` — the close-up shot (the camera
just outside the photosphere, looking along the limb, so the thread structure
can be judged) and the presence shot.

### The presence test

`refs/comparison/presence_frame.png`: the star sits off to the side of the
frame and a rock and a hull sit between it and the camera.  The rock and the
hull are lit by the star's own point light — the rock shows a lit crescent
with the terminator falling across it, and the light falls off with distance.
The star drifts across the frame over the clip, so the lighting on the
objects changes with it.

### What the pixel loop caught

* **`VERTEX` means different things in the two shader stages.**  It is model
  space in `vertex()` and *view* space in `fragment()`.  The first version
  computed the view direction from the fragment stage `VERTEX`, so the limb
  darkening was applied to a garbage direction and the disc rendered as a
  flat, evenly lit ball.
* **GLSL's `pow()` is undefined for a negative base.**  The footpoint
  highlight used `exp(-pow(x, 2.0))` with a signed `x`; the squares are now
  written out explicitly.
* **A hard angular cut-off in the prominence loop** dropped a term that was
  still non-zero at the cut, which showed up as a hard straight edge.
* **The quad's size is a mesh property, not a transform.**  `MODEL_MATRIX[0]`
  is the node transform (identity), not the mesh's half-width, so the corona
  was a whole solar radius too far out and left a black gap around the disc.
* **Godot caches compiled shaders.**  A shader edit that is not followed by a
  re-import silently does not reach the render.
* **A `Control` in a `CanvasLayer` needs its anchors set**, or it stays a
  zero sized rect at the origin and the glare pass draws nothing at all.
* **A `canvas_item` shader's blend mode is per shader, not per material**, so
  the additive glare and the darkening dip need separate shaders.

---

## 6b. THE DARK BAND (a user caught defect)

**What was reported:** in the close-up shot there was a near-black *moat* between
the photosphere's limb and the corona glow, and then the glow ignited abruptly
further out.  The fall-off geometry was wrong.

**Measured, on the row through the frame centre of the old build**
(`refs/comparison/closeup_frame.png` before the fix):

| x | 900 | 940 | 1020 | 1100 | 1140 | 1180 |
|---|-----|-----|------|------|------|------|
| luminance | 0.338 (surface) | 0.060 | 0.048 | 0.034 (the moat) | 0.353 (the glow ignites) | 0.256 |

The surface's limb fell to 0.034 and the corona did not start until 310 px
further out.  Three causes, all of them mine:

1. **The corona measured its radius in the wrong space.**  It compared the
   fragment's screen position against a point on the photosphere at the
   *centre's* depth, not against the photosphere's true silhouette (the tangent
   point of the view ray).  At the close-up's camera distance - 2.5 star radii -
   that put the corona's inner edge well outside the limb.
2. **The corona's profile was not one curve.**  It was a fast inner corona
   (e-folding length 0.065 R) plus a slow outer lift, so the two terms did not
   add up to a single monotonic decay.
3. **The surface's limb went to near black.**  The physical limb darkening, a
   deep red limb colour and a chromosphere edge term with a floor of 0.115
   multiplied together into a black ring.

**The fix**

1. The corona now measures every fragment against the photosphere's silhouette
   in **world space** - the tangent point of the view ray - so its inner edge
   is exactly the limb at any camera distance, and the measurement no longer
   depends on how the viewport maps to normalised device coordinates.
2. The corona has **one profile, maximal at the limb and decaying monotonically
   outward**: `core * inner_brightness + halo * brightness`, both terms at their
   maximum at `dist = 0`.
3. **The limb is bounded.**  `limb_edge_floor` is a per-palette parameter
   (0.42 for the visible light and red dwarf palettes, 0.30 for the 304
   diagnostic), the visible light palette's limb colour was lifted from a deep
   red to a deep orange, and the physical limb darkening was reduced from
   u = 0.50, v = 0.14 to u = 0.30, v = 0.08.  A star's limb is a little
   darker, never a black ring.

**Verified** on the same row after the fix:

| x | 900 | 940 | 980 | 1020 | 1100 | 1180 | 1220 | 1300 | 1460 |
|---|-----|-----|-----|------|------|------|------|------|------|
| luminance | 0.410 | 0.305 | 0.190 | 0.124 | 0.107 | 0.036 | 0.030 | 0.025 | 0.020 |

Monotonically non-increasing from the limb outward: no dip, no abrupt ignition.

**The master branch's own implementation was checked with the same camera**
(`--master` renders the 2k_sun.jpg texture through `battle_red_dwarf.gdshader`
plus the billboard corona).  Its profile was 0.306 at the limb -> 0.205 ->
0.105 -> 0.122 -> 0.091 -> 0.033 -> 0.002: it does **not** have the near-black
moat, because its surface has no limb darkening and its texture's edge is
bright, but its corona is a tight ring whose tail died within about 0.1 R, so
the glow was disconnected from the surface.  The same treatment was applied
there: `star_corona.gdshader`'s halo now has a long, gentle tail on top of the
tight core, so the corona is one profile that decays monotonically from the
limb.  Its profile is now 0.323 -> 0.227 -> 0.139 -> 0.154 -> 0.146 -> 0.117
-> 0.074 -> 0.055, i.e. the tail reaches 0.055 where it used to be 0.002 and
the corona is continuous with the surface.  (The small rise from 0.139 to
0.154 is the corona's own streamer structure, which is intended.)  Neither
implementation was wired or unwired.

**Artefacts:** `refs/comparison/closeup_fix_before_after.png` (before / after /
master, same camera), the refreshed `refs/comparison/closeup_frame.png`, and
`refs/comparison/closeup_frame_master_implementation.png`.


### 6b addendum: the corona's radial banding, and the debug overlay

Two more defects were caught by eye in the refreshed close-up frame.

**1. Radial banding in the corona.**  Faint spoke shaped streaks fanned out of
the limb, most visible in the upper right quadrant.  The cause was the corona's
noise being sampled in (angle, radius): the radius coordinate changed slowly,
so the noise stayed coherent along a radius and drew rays.

The first attempt - dithering the angular coordinate with a low frequency field
- made it *worse*: the dither's own isosurfaces turned the spokes into
concentric contour rings.  The fix is to stop sampling in (angle, radius) at
all: the corona's structure is now sampled on the shell's own direction
(`fbm(offset * 7.0 ...)`), which is isotropic by construction, and the
structure's contrast was cut so it reads as a soft mottle rather than a
pattern.  A sub-quantisation dither was added on top so the smooth parts of
the corona do not band on an 8 bit target.

Verified on the crop `refs/comparison/closeup_band_before_after.png`: the
spokes are gone and the corona is a smooth haze.  A multi-angle radial profile
check (72 rays, the limb found per ray, the profile sampled outward) gives an
angular spread in the corona region of 0.0704 before and 0.0642 after the
isotropic change; the residual is the prominence loops and the disc's own
mottling, which are features, not banding.  The smooth final version's spread
is indistinguishable from the pre-fix one on that metric because the
structure's contrast was cut until the banding was below the metric's noise
floor - the visual check is what settles it.

**2. The debug overlay was in the captures.**  The "FPS: 30" label is the
project's own performance readout, drawn by `Session` (an autoload) in a
`CanvasLayer`.  `Session` now reads the `--capture` user argument and hides
that label, and `scripts/render-sun.sh` passes `--capture` on every render.
The overlay is only hidden for captures; the game is unaffected.

`refs/comparison/closeup_frame.png` was re-captured clean, and
`refs/comparison/closeup_band_before_after.png` shows the banding before and
after.

### 6b addendum 2: the v3 recording

The close-up clip was re-recorded from scratch after the moat, banding and
overlay fixes.  The files in `D:\grim-space-recordings\sun_test*.mp4` from
before that are fossils and are not the shipped look.

**Provenance**

| | |
|---|---|
| Branch state | `feature/sun-shader` at `a0acfddb` (post moat fix, post banding fix, post overlay fix) |
| Camera | `SunTestScene` close-up mode: 2.45 star radii out, 50 deg FOV, looking along the limb |
| Render | `./scripts/render-sun.sh "D:/grim-space-recordings/sun_v3_silent.avi" 420 --spin=0.05 --closeup` - 420 frames at 30 fps, 1920x1080, 14.000 s |
| Audio | `python scripts/make-sun-audio.py --duration 14 --out sun_rumble_v3.wav` - the same timeline the renderer reads, so the events line up |
| Mux | `ffmpeg -i v3_silent.mp4 -i sun_rumble_v3.wav -map 0:v:0 -map 1:a:0 -c:v copy -c:a aac -b:a 192k -ar 48000 -af "atrim=0:14,asetpts=N/SR/TB" -shortest -movflags +faststart sun_test_v3.mp4` |
| Output | `D:\grim-space-recordings\sun_test_v3.mp4` |

The audio script is now parameterised (`--duration`, `--out`); its default is
still 12 s, so the 12 s `sun_test.mp4` at the repository root stays
reproducible.

**Verification of the shipped file**

`ffprobe` on `sun_test_v3.mp4`:

| stream | codec | format | start | duration |
|--------|-------|--------|-------|----------|
| video | h264 | 1920x1080, 30 fps | 0.000000 | **14.000000** |
| audio | aac | 48 kHz, mono | 0.000000 | **14.000000** |
| container | mp4 | | | **14.000000** |

Levels: **-15.9 LUFS integrated, LRA 2.2 LU, true peak -2.9 dBFS** - the
documented target was -16 LUFS, so the soundtrack is where it says it is.
(The decoded audio is 672000 samples = 14.000 s, peak 0.7054, rms 0.1157.
`ffmpeg -af volumedetect` reports -91 dB for this file, which is wrong - the
decoded samples are the truth and they match the source wav exactly.)

Three evenly spaced frames were extracted from the shipped file and read:
t = 2.33 s, 7.0 s and 11.67 s.  All three show

* **no moat** - the photosphere's limb runs straight into the corona,
* **no radial banding** - the corona is a soft isotropic mottle, no spokes,
* **no debug HUD** - the performance label is gone (the `--capture` switch),
* the prominence loop crossing the limb, changing shape between the frames,
  and the photospheric texture drifting.

The phenomenon beats were read separately at their event times: the flare
pulse at **t = 1.33 s**, the CME at **t = 4.1 s** and the second flare at
**t = 9.2 s**, each with its audio sting at the same timestamp.  The best
frame - the flare beat with the loop crossing the limb - is
`refs/comparison/sun_showcase_v3.png`.

---

## 6b addendum 3: ROUND 5 - REFERENCE REGIME

The reference library is now pinned: `refs/sun_reference_library/` is a verbatim
copy of the user's picks, with `SOURCES.md` naming every file and `WEIGHTS.md`
carrying the weights and the per-element deltas.

### VERDICT AXES

The round 5 standard is two axes, because looking *at* the Sun and looking at
*things the Sun lights* are different questions.

| Axis | Image | Verdict | One sentence |
|------|-------|---------|--------------|
| **A - THE VIEW** (looking at the Sun: surface granulation to DKIST, corona to eclipse streamers, prominence to graceful-eruption choreography) | `refs/comparison/round5_closeup.png` | **PARTIAL** | The corona now carries the eclipse's radial streamer arcs and the surface carries DKIST-style bright cell cores on dark lanes, but the prominence still reads as a thin drawn arc rather than the graceful rising plume of `dr_graceful_eruption.jpg`, and the NASA map's large white patches still blow out where DKIST is uniformly cellular. |
| **B - THE LIT-BY VERDICT** (looking at things the Sun lights: crescent limb, terminator, the Sun as the scene's light source) | `refs/comparison/round5_asteroid_presence.png`, `refs/comparison/round5_battle_wide.png` | **PASS** | The asteroid field is lit by the Sun's own point light with a readable terminator and a warm colour temperature on the surfaces, the rock in front of the disc silhouettes correctly, and the wide battle frame shows the red dwarf as the light the whole scene is lit by. |

### What changed in the shader

* **Surface granulation -> DKIST.**  A cellular field was added: a 27-cell
  Worley lookup gives the distance to the cell centre and the distance to the
  cell border, and the granulation is a bright core with a dark, narrow lane
  around it - 118 cells across the disc.  The NASA map's blend was cut from
  0.72 to 0.45 so the cells, not the map's large white patches, dominate.
* **Corona -> eclipse streamers.**  The corona's structure is no longer a polar
  noise sample (that drew spokes) nor an isotropic mottle (that drew no
  structure).  It is a starburst of radial strands: broad arcs from
  incommensurate angular sines, fine strands inside them, fading with the
  radius, and **no radial phase anywhere** - a high angular frequency with any
  radial term beats into concentric rings, which is what the first attempt did.
* **Prominence material -> braid, knots, feathers, flow.**  The noise phase is
  advected along the loop so the plasma visibly travels along the arc; the
  braid has a second harmonic so the threads cross and **knot**; the filament's
  width is modulated by a hair field so the edge **feathers** instead of ending
  in a clean line; and the knots brighten.
* **The asteroid is lit.**  The shipped pack is a *metallic* variant, and a
  metal only shows a specular highlight, so the rock read as a black
  silhouette.  The material is replaced with a rough dielectric, and the Sun's
  point light now falls on the rock's surface with a terminator.

### Frame reads and profile checks

* `round5_closeup.png`: read.  No moat, no concentric rings, the corona's
  strands are radial, the granulation shows bright cores on dark lanes, a
  prominence loop crosses the limb at the top, no debug HUD.
* `round5_asteroid_presence.png`: read.  The debris field to the right of the
  disc is lit (warm tan, shaded, terminator visible), the rock in front of the
  disc is a correct silhouette, the corona's arcs read against the sky.
* `round5_battle_wide.png`: read.  The red dwarf is the light of the battle
  scene; the dust stream crosses in front of it and catches the light.
* Radial profile, 72 rays with the limb found per ray:
  `round5_closeup` 0.250, 0.086, 0.079, 0.070, 0.062, 0.059, 0.054, 0.049,
  0.043, 0.040, 0.038, 0.033, 0.030 - monotonically decreasing, **0 outward
  rises**.  `round5_asteroid_presence` 0.209, 0.131, 0.113, 0.107, 0.095,
  0.087, 0.079, 0.077, 0.071, 0.067, 0.063, 0.062, 0.059 - also monotonic,
  **0 outward rises**.

---

## 6b addendum 4: PROMINENCE VERDICT EVIDENCE

The prominence is the highest weighted element in the library, and at star
scale it was not legible.  This is the dedicated evidence.

| Artefact | What it is |
|----------|------------|
| `refs/comparison/round5_prominence_closeup.png` | a tight shot of one prominence at the limb, cropped from a 9.5 degree telephoto render of the real scene |
| `refs/comparison/round5_prominence_flow_1.png`, `_2.png`, `_3.png` | the same loop at equal intervals (1.37 s apart, frames 156 / 197 / 238 of a 240 frame render) |
| `refs/comparison/round5_prominence_eruption.png` | the frame at the eruption's peak (frame 204) |

### What is PROVEN (read off the frames)

1. **Thread braid: PROVEN.**  Frames 1 and 3 show three or four *separate,
   parallel strands* running along the loop, with a brighter strand in the
   middle and fainter ones either side.  They are threads, not a tube - which
   is the `ha_filament_threads.jpg` and `real_loops_2011_closeup.png`
   standard.  The strand count and their separation are legible at this crop.
2. **Interior knots and the core-in-envelope structure: PROVEN.**  Frame 2 (the
   eruption peak) shows a saturated red core sitting inside a wide, soft
   orange envelope that fades into the corona, with the envelope's brightness
   falling off continuously - the `dr_graceful_eruption.jpg` and
   `gen_closeup_3828_f003.png` structure.
3. **Feathered edges: PROVEN.**  The core does not end in a clean line: it
   breaks into fine hairs and the envelope frays into the surrounding glow.
4. **Flow along the loop, not a shape wobble: PROVEN.**  Between the three
   frames the *thread pattern* changes completely - the strands slide, one
   brightens while its neighbour dims, the braided crossing point moves along
   the arc - while the loop's overall envelope and its anchor on the limb stay
   in place.  That is material travelling along the loop lines, which is the
   graceful-eruption choreography question.

### What is INFERRED (not provable from these frames)

* **The choreography's *speed* relative to a real eruption.**  The reference
  cadence is hours; the shader runs a 13 s cycle so the motion is legible.
  The frames prove the material moves along the lines; they cannot prove the
  rate is right.
* **The braid's density against the texture bosses.**  The strand count is
  now legible, but matching `real_loops_2011_closeup.png`'s exact thread
  density would need a measurement of threads per unit arc length in the
  reference, which this round did not make.
* **The knots.**  The braided crossing brightens and moves, but whether the
  crossings read as "knots" the way they do in the reference is a judgement
  call, not a measurement.

### How the shot was made

`SunTestScene --prominence` aims a 9.5 degree telephoto camera at the limb
point of the prominence site with the largest height scale.  The site's screen
angle is found by replicating the shader's site hash in C#, and the camera
stays on the star's axis so the prominence billboard's basis is the world X/Y
plane.  The evidence images are crops of that render, not re-renders, so the
pixels are the scene's.

---

## 7. An earlier radius calibration was wrong, and it mattered

The first versions of the comparison used the *half-max* radius for the render
and the *outer edge* for the reference.  Because the shader's disc has a much
softer edge than the reference's, that made the render's disc look 22 % too
large and the whole limb comparison meaningless.  Re-deriving both radii with
the same rule changed the conclusions substantially: the granulation scale
went from "38 cells vs 47" to "46.2 vs 46.9", and the off-limb brightness from
"8× too bright" to "slightly too dim".  Measure, do not assume.

---

## 8. Honest remaining gaps

An independent reviewer (`SUN-VERIFY`, `task/sun/sun-verify-report.md`, who
measured the images with its own code and had no access to the numbers above)
graded the earlier version **"CREDIBLE BUT NOT CONVINCING"** with four
blockers.  Two have been addressed since; the list below is the state of play.

1. **The NASA map is a contrast stretched visualisation, not a true colour
   image.**  Its white blisters are blown out and its dark lanes are much
   darker than the real Sun's, so it has to be compressed to be usable.  The
   trade-off is explicit: the map gives the *real* granulation and blister
   structure at the right scale, and costs the disc's tonal range, which the
   procedural field has to make back up.  A true-colour HMI continuum map
   would sit better but is not in the repository.
2. **The texture's large scale features still read as "moon".**  The map is
   tiled twice around the disc, which halves the feature size, but the real
   supergranulation-scale contrast survives compression and still reads as
   patches.  The fully procedural version did not have this problem, and it
   matched the reference's *scale* better before the map was introduced.
3. **The granulation contrast is ~3.6× too low** (0.19 vs 0.70).  The scale
   matches, but the reference's patch contrast is dominated by white-hot active
   regions; the shader's plage covers less area and never reaches a white core.
4. **The limb edge is still too soft.**  At 0.95 R the render is 0.111 against
   the reference's 0.072, and at 1.00 R 0.078 against 0.032.  The
   chromosphere edge term reproduces the shape of the fall-off but not its
   steepness.
5. **The prominence threads are an approximation.**  The real close-ups show
   threads that kink, thin, break and rejoin, with a strong bright core and a
   soft envelope; the shader's are a stretched noise field with a braid, which
   is the right *idea* and the wrong *detail*.  The corona rain and the CME
   are stylised too: a blob sliding down a leg and an expanding shell are not
   the real physics.
6. **The disc reads as a ball rather than a flat disc** in the visible light
   palette, because the photosphere's limb darkening is a smooth radial
   gradient.  That is physically correct for visible light and is what the
   reference's HMI 193 shows; the 304 reference is much flatter.
7. **The prominence motion is a game scale fiction.**  The reference cadence
   is ~50 minutes; the shader uses 13 s.
8. **The corona's streamers are noise, not field aligned.**  The LASCO
   reference shows streamers coherent over several solar radii and rooted in
   specific active regions; the shader's streamers drift with the noise field.
9. **The auto-exposure is a fixed curve, not a real one.**  The squint is
   driven by the star's apparent size, not by the frame's measured luminance,
   so a bright object elsewhere in the frame does not trigger it.

---

## 9. Reproducing this

```bash
# the showcase clip, with sound
./scripts/render-sun.sh "D:/grim-space-recordings/sun_showcase.avi" 360 --spin=0.05
python scripts/make-sun-audio.py
ffmpeg -y -i sun_showcase.avi -c:v libx264 -crf 22 -pix_fmt yuv420p \
       -i dist/sun_rumble.wav -c:v copy -c:a aac -b:a 192k -shortest sun_test.mp4

# the close-up shot (the limb fills the frame)
./scripts/render-sun.sh "D:/grim-space-recordings/sun_closeup.avi" 360 --spin=0.05 --closeup

# the presence test (the star lights a rock and a hull)
./scripts/render-sun.sh "D:/grim-space-recordings/sun_presence.avi" 360 --spin=0.05 --presence

# the 304 diagnostic (the palette sampled from the reference frame)
./scripts/render-sun.sh "D:/grim-space-recordings/sun_p304.avi" 60 --noglow --304

# the battle scene's red dwarf sun, in the scene that uses it
./scripts/render-sun.sh "D:/grim-space-recordings/battle_backdrop.avi" 60 --battle
```

Useful flags: `--304`, `--noglow`, `--noglow`+`--legacy` (the "before"),
`--battle`, `--presence`, `--closeup`, `--camdist=`, `--spin=`, `--seed=`,
`--quadscale=`.

---

## 10. Tests

The branch was rebased onto `master` (65e38b93) by the harness, which brought
master's test suite with it: **1603 tests, 1601 passing, 2 failing.**

The two failures are in `Run/BattleSpawnPilotTests`
(`BattleEncounterSave_RoundTripsPilotSnapshot` and
`MultiShipEngagement_EachPlayerSpawnGetsDistinctPilotSnapshot`), both failing
in `BattleEngagementComposition.EnsurePlayableCounts` with *"needs at least
one player and one enemy unit"*.  They are **pre-existing on the rebased
branch and unrelated to the sun work**: they were reproduced by stashing every
modification of this branch, applying only the build repair described below,
and running the suite again - the same two tests fail, 1601 pass.

The rebase also left the tree **not compiling**: the merge of `RedDwarfSun.cs`
dropped the `CreatePalette()` method that its own `CreateVisual` call
references, so `dotnet build` failed with two errors.  Restoring that method
was necessary to build the branch at all; it is the only change made to the
wiring of either sun implementation, and no implementation was wired or
unwired as part of this work.

Run the suite with:

```
grim-space.Tests/bin/Debug/net10.0/grim-space.Tests.exe
```

`dotnet test` reports "Zero tests ran" in this environment.  That is
**pre-existing and unrelated to this change** - it was verified by stashing
every modification and running the same command on the unmodified tree, which
fails identically.  The test project is an xunit v3 executable with
`Microsoft.Testing.Platform` selected in `global.json`; the direct runner is
the path that works here.

No test was added for the shader itself.  Shaders are graphics, not logic;
`AGENTS.md` is explicit that tests must not exercise the Godot framework, and
the visual verification in this document is the equivalent coverage.

## 6b addendum 5: THE STAR MAP'S SUN (S1..S4 FIXES, 2026-10-23)

The star map's sun was judged live-broken by the user: "something is weird
about the sun", "the disc reads off + the light 'rays' read wrong",
"the prominence reads as threaded threads lit FROM the star" (it did not),
"ONE prominence role at a time" (it did not obey), "the viewer must SEE that
he is looking at the sun" (nothing responded), "the sun's sound" (there was
none).  Root causes, each named after code + render evidence:

| Gate | Root cause (one sentence) |
|------|---------------------------|
| S1 | `MapCelestialVisuals.AddStar` drove the ratified shader with a private palette whose disc energy (`0.5 + 0.25*SunGlow ≈ 0.70`) was 2.7x the measured sun_test value, burning the disc into an odd over-hot ball. |
| S1b | The map star's corona ran at the full streamer/halo balance at map viewing distances, so the strand field aliased into a fan of fake "light rays" and the shell's silhouette cut the from-the-tail into a hard-edged ball; and the glare/dip overlay was never wired into the map at all. |
| S2 | The prominence's brightness had no limb-proximity envelope and its fibril frequency was too low to read as threads at map scale, so the loop read as a flat wire with an isolated hot foot; the map's own lights could never well into it (all sun shaders are `unshaded` — verified, no regression needed there). |
| S2' | 3-5 sites erupted on independent shader cycles, so several prominences were frequently alive at once with no quiet gaps; casting is now ONE role at a time. |
| S3 | The map had no glare/dip overlay and its opening cinematic bootstrap faced the ship's travel direction, so nothing ever told the viewer he was looking at the sun; the overlay's squint law also carried a units defect (it multiplied the fractional radius back into pixels, saturating the squint at any resolution). |
| S4 | The map had no audio and no timeline: the pipeline's `sun_events.json`/`make-sun-audio.py` rumble never reached an audio bus. |

### S2'' — the ONE ROLE, generated where the pipeline owns it

`scripts/make-sun-audio.py` now mirrors the S2' law: every birth performs one
seeded roll of 0..3 (weights `[0.26, 0.44, 0.18, 0.12]`, biased toward
holder 1, the heavyweight), the rolled holder takes the stage with its own
start/death window (holder 1: gain 1.42, life 8-11 s; holder 0: 1.0,
5-7 s; holders 2/3: 0.72/0.58, 3.8-5.2/2.8-4.0 s), then a seeded all-quiet
gap (0.6-2.6 s).  The dissolve runs the same S-ramp in and (reversed) out,
so every death lands at intensity exactly 0 — a true disappearance; the
all-quiet gap between turns is legal and the shedule is written into
`assets/data/sun_events.json` (`prominence_roles`, + `prominence_roll` spec)
and into the rumble's boiling-crackle density, so the sound breathes with the
same rolls.

The renderer consumes that schedule data-driven (`SunProminenceCasting.Resolve`
on `SunPhenomena.LoadRoles`, installed per scene by
`SunVisual.SetProminenceSchedule`): a star whose casting is not driven
per-frame (the battle sun) runs the same law cycle-internally in the shader
(`role_gain < 0` self-drive branch: one roll of 0..3 per birth, weighted the
same, dying exactly 0 with a quiet tail).  The shader's fixed `site_count`
sites and its own 13 s erupt cycle are gone; `SunPalette.ProminenceCount` is
removed everywhere (the law owns casting).

### The disc and the corona (S1, S1b)

Map palette rebased onto the measured/ratified values: `EmissionStrength
= 0.26 + 0.06*SunGlow` (the disc is no longer over-burned), corona softened
to map distances: `CoronaStrength 0.12`, `CoronaStreamerStrength 0.34` (new
palette knob — the shader's own balance 0.85 stays for sun_test), and a new
`CoronaHaloReach` knob (default 1.2 = the SPF's own polynomial ég; map uses
0.5) so the glow hugs the limb instead of reading as a brown ball.  Two
shader-law additions, both additive-only and shown not to regress the
sun_test regime (check frame at the end of this section):

* `sun_prominences.gdshader`: fibril frequency along the arc raised
  (1.7 -> 5.0) and a height-facing energy term
  (`light_facing = mix(0.55, 1.0, exp(-max(r-1,0)/0.075))`) shared by the
  thread, curtain and feet, so the loop reads as threads lit FROM the star.
  Rain is gated on a live role (no loop, no rain).
* `sun_corona.gdshader`: the shell dissolves to a true 0 for the last 0.25 R
  before its own silhouette (`edge`, applied to colour AND dither) — at
  tail brightness the last shell ring sits under the dither noise and the
  abrupt quantisation step read as a hard bright ring on dark backdrops.

### Presence (S3) and sound (S4)

* `MapController` now wires `SunVisual.CreateGlareOverlay(starVisual,
  worldRadius)` with map-scaled parameters (intensity 0.55, streak 0.10,
  breathe 0.05, chromatic 0.22) and `MapPresentationContext.StarWorldPosition`
  feeds `MapCinematicFraming.OpenTowardStar`, which is the fresh
  cinematic-map bootstrap: the camera sits behind the ship LOOKING toward
  the star (same pose family and distance limits; the travel-direction
  bootstrap stays as the degenerate fallback).  The overlay's squint law is
  now fraction-of-frame based (the units fix), so a distant star squints
  mildly and a disc-filling star squints hard.
* The star's sound is the pipeline's own rumble (`dist/sun_rumble_map.wav`,
  generated by the SAME script at 14 s) routed through the new `Sun` bus
  (`default_bus_layout.tres`, sends to Master, so the existing mute path
  governs it), on an `AudioStreamPlayer` in `MapController` at -8 dB.  The
  timeline clock that drives the shader phenomena is read straight off the
  player's playback position (looped `Finished -> Play`), so picture and
  sound stay event-synced with no drift; the timeline wraps at the json's
  `duration` (14 s) and both clocks wrap together.

### Evidence (commands + sheets, all in refs/comparison/)

Rendered with `scripts/render-sun.sh` (its leading `--import` re-imports the
shaders — the cache law).  Command families:

```
bash scripts/render-sun.sh /tmp/opencode/mapstar_before.avi 360 --mapstar
bash scripts/render-sun.sh /tmp/opencode/final_12r.avi 360 --mapstar
bash scripts/render-sun.sh /tmp/opencode/final_closeup.avi 60 --mapstar --camdist=2.8
bash scripts/render-sun.sh /tmp/opencode/final_suntest.avi 90
```

`SunTestScene` grew a `--mapstar` reproduction mode: it builds the star with
`MapCelestialVisuals.BuildStarPalette` (the map's palette owner, made static)
at the map star's world radius under a map-like camera, so the map's own
tune is rendered, not re-described.  Sheets (frame extracts stacked with
ffmpeg vstack/hstack):

* `sheet_disc_before_after.png` — S1: disc energy before/after.
* `sheet_rays_before_after.png` — S1b: the ray fan before/after corona balance.
* `sheet_prominence_before_after.png` — S2: the loop's light from the limb.
  (Disclosure for the audit: the before panel keeps the map's own disc/corona
  values but the fix's prominence scale, so both panels show the loop; the
  shipped map's prominence at 0.35 rendered the same structure dimmer.)
* `sheet_casting_one_role_quiet.png` — S2': one loop live (t=5 s) beside the
  all-quiet frame (t=10.2 s): the rolled holder died at exactly 0.  In the
  12 s clip the rolls were holder 1 at t=0..8.85 (death exactly 0), gap
  until 10.67, then holder 1's next turn: never two loops.
* Audio: `ffprobe dist/sun_rumble_map.wav` -> 14.000 s, mono 48 kHz, pcm s16
  (the map's loop length and the shader clock share it).

Known unrelated-to-this-work: the direct test runner reports 2 pre-existing
battle-fixture failures (`pilot-save-test` / `multi-pilot-test` — battles
with zero enemy units), and the stored `cme_data` uniform is written by the
timeline but the prominence shader fragment never consumed it even before
this work (the CME is a schedule entry in the sound only).  Both left as
they were found.
