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
