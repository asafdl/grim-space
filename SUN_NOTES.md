# SUN_NOTES — a sun shader built from real NASA/SDO reference frames

This is the working log for the sun/star shader in `grim-space`: what the
reference material was, what was measured from it, how the shader was built
from those numbers, how the result was compared against the real frames, and
— honestly — what still does not match.

**TL;DR** — three procedural shaders (photosphere, prominences, corona) driven
by a palette object.  The photosphere reproduces the measured granulation scale
(~41 cells across the disc against the reference's ~47) and the measured limb
darkening; the prominences are thin shell-model arcs that erupt, hang and fall
back; the corona is a noise-driven streamer halo with the measured e-folding
length.  Compared side by side with a real AIA 304 frame the disc centre
matches within 20 % and the 0.90 R limb brightness matches within 1 %.  The
remaining gaps are the limb edge (my disc ends ~2–3× too bright), the local
contrast of the mottling (~3.5× too low) and the filamentary, flowing quality
of the real granulation, which value noise does not reproduce.

---

## 1. What is in this repository

| Path | What it is |
|------|------------|
| `assets/shaders/sun_surface.gdshader` | photosphere: granulation, supergranulation, plage, limb darkening, chromospheric edge and rim, palette ramp |
| `assets/shaders/sun_prominences.gdshader` | prominence loops, footpoint plage, chromospheric ring, spicule haze |
| `assets/shaders/sun_corona.gdshader` | corona streamers |
| `src/presentation/graphics/SunVisual.cs` | builds the three layers into one node |
| `src/presentation/graphics/SunPalette.cs` | the colours and tuning constants |
| `src/presentation/dev/SunTestScene.cs` | dev scene used to render and compare the shader |
| `scenes/sun_test.tscn` | that dev scene |
| `scripts/render-sun.sh` | re-import + Godot Movie Maker render helper |
| `sun_test.mp4` | the rendered clip (1920×1080, 30 fps, 12 s, star rotating) |
| `refs/` | the real reference material, its sources, and the measured numbers |
| `refs/comparison/` | the side-by-side comparison sheets and render stills |
| `task/sun/` | the briefs and reports of the reference and review workers |

### Integration

* `src/battle/presentation/graphics/RedDwarfSun.cs` — the battle scene's sun is
  now the shared shader driven by a **red dwarf** palette (cooler, redder core,
  weaker prominences).  The public surface of the class (`CreateVisual`,
  `Configure`, `LightDirection`, `LightColor`) is unchanged, so
  `SpaceBackdrop` and `BattleController` were not touched.
* `src/world/star-system/presentation/atmosphere/MapCelestialVisuals.cs` — the
  strategic map's star is now the same shader with a solar palette, with a
  coarser granulation and weaker prominences because it is usually only tens of
  pixels across and would otherwise alias.  The old texture-plus-corona path
  (`2k_sun.jpg` + `star_corona.gdshader`) is gone from that call site.

`assets/textures/2k_sun.jpg` is now unused by the code but was left in place
rather than deleted — removing a shared asset is not this task's business.

---

## 2. The reference material (all public domain)

Acquired by the `SUN-REF` worker; exact URLs in `refs/SOURCES.md`, measured
numbers in `refs/palette.json`, the analysis in `refs/analysis.md`.

| Source | URL | What it gave |
|--------|-----|--------------|
| NASA SDO AIA 304 Å (1024 & 2048) | `https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0304.jpg` | the red chromosphere, the granulation, the prominences |
| NASA SDO AIA 171 / 094 Å | `.../latest_1024_0171.jpg`, `.../latest_1024_0094.jpg` | the inner corona, the bright limb in EUV |
| NASA SDO HMI 193 / continuum | `.../latest_1024_0193.jpg` | limb darkening and granulation scale at high resolution |
| SOHO LASCO C2, EIT 195 | `https://soho.nascom.nasa.gov/data/LATEST/current_c2.gif` | the corona's streamer structure |
| SDO AIA 304 motion, 2010-10-21 | `https://svs.gsfc.nasa.gov/` (SVS3828) | how a prominence actually moves |
| SDO AIA 304 motion, 2026-02-01 | SVS5649 | the same, a different epoch |
| "SDO 20240508 1024 0304 (AIA)" | Wikimedia Commons (public domain, NASA/SDO) | the frame used for the side-by-side comparison |
| "SDO AIA First light prominence 20100330" | Wikimedia Commons (public domain) | a full disc in 304 Å with clear prominence loops |

**A note on the copies in this repository.**  The media under `refs/` was
re-encoded and downscaled so the repository stays a sane size (237 MB → 51 MB);
`refs/tools/shrink_for_repo.py` did it.  The source URLs in `refs/SOURCES.md`
are the originals, and the numbers in `refs/palette.json` were measured on the
originals, not on these copies.

---

## 3. The look, measured from the pixels

These are the numbers the shader constants come from.  Radii are expressed as
a fraction of the solar radius; luminances are sRGB (the same encoding the
NASA JPEGs and the Godot render use, so they are directly comparable).

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
with `u ≈ 0.50`, `v ≈ 0.14`; the extra, much steeper fall-off in the last 10 %
of the radius is the chromosphere's finite thickness, so the shader models it
as a separate "chromospheric edge" term rather than bending the limb
darkening law into something unphysical.

### Granulation

| | reference | shader |
|---|---|---|
| cell size | 14.3 px | 15.7 px |
| cells across the disc | 46.6 | 40.8 |
| local contrast (std / mean of a 180 px disc patch) | 0.70 | 0.20 |

Both numbers are the energy-weighted mean wavenumber of the patch's power
spectrum, i.e. the same measurement applied to both images.

### Prominences

| | measured |
|---|---|
| height above the limb | 0.10 – 0.26 R (the brightest structures reach 0.67 R) |
| brightness | ~40 % of the disc, saturated red |
| count on the limb | 3 – 4 at any time |
| motion | over 3 hours of AIA 304 the prominence area changes by ±5 % with a ~50 minute period |

### Corona

| | measured |
|---|---|
| AIA 171 at 1.00 R | 0.237 of the disc brightness |
| fall-off | exponential, e-folding length ≈ 0.065 R, gone by ~1.15 R |
| structure | narrow radial rays separated by darker lanes, a few broad "helmet" streamers (LASCO C2) |

---

## 4. Design decisions

**Three layers, not one.**  The photosphere is an opaque sphere; the
prominences and the corona are camera-facing additive quads.  Keeping the
opaque surface separate is what lets the prominence loops be rooted exactly on
the silhouette and extend *outside* it, which is what a prominence is.

**Prominences as a shell, not a blob.**  A real prominence reads as a thin
bright filament with a darker interior, because it is a loop of plasma seen
against the corona.  The shader renders the *distance to an arc* as a thin
Gaussian shell, so the interior of the loop stays dark.  The arc is a
semicircle whose height grows and shrinks with the eruption envelope, so the
loop rises, hangs and falls back onto the limb.

**Curl noise for the turbulence.**  The loop surface is displaced by the curl
of a scalar noise potential, which is divergence-free, so the plasma writhes
without the loop losing its shape.  The noise uses the quintic fade
(`f*f*f*(f*(f*6-15)+10)`) rather than the cubic one, because the curl
differentiates the field and the cheaper fade leaves visible creases along the
lattice boundaries.

**True image radius, not `sin(beta)`.**  The limb terms are keyed on the
fragment's *image* radius as a fraction of the disc's apparent radius, which
the shader computes from the camera geometry (`v_tangent` and `v_distance`
are captured in the vertex stage).  `sin(beta)` is not the same thing under
perspective, and using it made the limb fade appear in the wrong place.

**Granulation is hue, not brightness.**  The real 304 mottling is mostly an
orange-on-red *hue* variation with only a modest luminance swing; the big
luminance contrast in the reference comes from the plage (the white-hot active
regions).  The shader therefore drives the colour ramp hard from the
granulation and keeps the brightness swing small, with a separate strong
plage term.

**One shader, several palettes.**  The physics (limb darkening, granulation
scale, edge profile) is shared; the colours and the relative strengths are
per-palette.  The red dwarf in the battle scene and the star on the strategic
map use the same code with different palettes.

**Deliberate departures from the reference.**  The real prominence cycle is
~50 minutes; in a game that is invisible, so the shader uses a 13 s eruption
period.  The reference 304 is a chromosphere, and the shader is also used for
the game's visible-light look, so the palette separates the two.

---

## 5. Verification

### Method

1. Render the sun with Godot Movie Maker (`scripts/render-sun.sh`).
2. Extract frames with ffmpeg.
3. Build a side-by-side sheet: the real NASA frame on the left, the render on
   the right, both centre-cropped to the same square and the same disc size.
4. Measure both images with the same numpy code (radial profile, granulation
   spectrum) and compare the numbers.
5. **Look at the pixels.**  Every claim in this document that is about
   appearance comes from opening the images, not from reading a log.

The reference frame is a genuine AIA 304 image from 2024-05-08 13:28 UT; the
render is the same shader driven with a palette sampled from that frame, so
the comparison isolates the shader rather than the art direction.

The render uses a distant camera with a narrow field of view
(`CameraDistance = 12 R`, FOV derived from the distance) so the projection is
close to the near-orthographic view the spacecraft cameras used, which is what
makes the image-radius comparison valid.

### Result (AIA 304 palette, sRGB luminance)

| | reference | render |
|---|---|---|
| disc centre | 0.2509 | 0.3036 |
| 0.80 R | 0.2511 | 0.1818 |
| 0.90 R | 0.1924 | 0.1935 |
| 0.95 R | 0.0793 | 0.1727 |
| 1.00 R | 0.0355 | 0.1053 |
| 1.02–1.08 R mean | 0.0176 | 0.0737 |
| 1.02–1.08 R p99 (prominence peaks) | 0.1184 | 0.1249 |

The disc centre is 21 % bright, the 0.90 R limb matches within 1 %, and the
prominence peak brightness matches within 6 %.  The limb edge and the mean
brightness just outside the limb do **not** match — see below.

The side-by-side sheet is `refs/comparison/side_by_side_aia304.png`; the
visible-light render is `refs/comparison/render_visible_light_palette.png`
and `sun_test.mp4`.

### What the pixel loop caught

These were all invisible in the code and obvious in the pixels, which is the
argument for doing the comparison at all:

* **`VERTEX` means different things in the two shader stages.**  It is model
  space in `vertex()` and *view* space in `fragment()`.  The first version
  computed the view direction from the fragment-stage `VERTEX`, so the limb
  darkening was applied to a garbage direction and the disc rendered as a
  flat, evenly lit ball.
* **`pow()` is undefined for a negative base in GLSL.**  The footpoint
  highlight used `exp(-pow(x, 2.0))` with a signed `x`; the squares are now
  written out explicitly.
* **A hard angular cut-off in the prominence loop** (`if (abs(u) > 1.35)
  continue;`) dropped a term that was still non-zero at the cut, which showed
  up as a hard straight edge in the image.  Every site contribution is now
  windowed to zero before the cut.
* **The quad's size is a mesh property, not a transform.**  `MODEL_MATRIX[0]`
  is the node transform (identity), not the mesh's half-width, so the first
  version of the billboard maths put the corona a whole solar radius too far
  out and left a black gap around the disc.
* **Godot caches compiled shaders.**  A shader edit that is not followed by a
  re-import silently does not reach the render.  `scripts/render-sun.sh`
  re-imports first for exactly this reason; several "the change did nothing"
  moments turned out to be this.

---

## 6. Honest remaining gaps

1. **The limb edge is too soft and the disc is too bright at the edge.**  At
   0.95 R the render is 0.173 against the reference's 0.079, and at 1.00 R
   0.105 against 0.036.  The chromospheric edge term reproduces the shape of
   the fall-off but not its steepness; the real AIA 304 limb is much sharper
   than any smooth function of the image radius that was tried, which suggests
   the instrument's limb darkening function contributes as well as the
   physics.
2. **The mean brightness just outside the limb is ~4× too high** (0.074 vs
   0.018 at 1.02–1.08 R).  The prominence *peaks* are right, so the residual
   is the low-level background: the curtain under the loops, the spicule haze
   and the corona's inner fall-off together are too strong.
3. **The granulation contrast is ~3.5× too low** (0.20 vs 0.70).  The scale
   matches, but the reference's patch contrast is dominated by its white-hot
   active regions; the shader's plage covers less area and never reaches a
   white core.
4. **The texture is blotchy, not filamentary.**  Value noise gives cellular
   blobs; the real granulation is a flowing network of lanes.  This is the
   single biggest visual difference and it is a limitation of the noise
   function, not of the tuning.
5. **The disc reads as a ball rather than a flat disc** in the visible-light
   palette, because the photosphere's limb darkening is a smooth radial
   gradient.  That is physically correct for visible light and is what the
   reference's HMI 193 shows, but the 304 reference is much flatter.
6. **The prominence motion is a game-scale fiction.**  The reference cadence
   is ~50 minutes; the shader uses 13 s.
7. **The corona's streamers are noise, not field-aligned.**  The LASCO
   reference shows streamers that are coherent over several solar radii and
   rooted in specific active regions; the shader's streamers drift with the
   noise field.

---

## 7. Reproducing this

```bash
# render the comparison still (real reference on the left, shader on the right)
./scripts/render-sun.sh "D:/grim-space-recordings/sun_p304.avi" 30 --noglow --304
ffmpeg -y -i sun_p304.avi -vf "select='eq(n\,0)'" -fps_mode passthrough p304.png

# render the showcase clip
./scripts/render-sun.sh "D:/grim-space-recordings/sun_test.avi" 360 --spin=0.05
ffmpeg -y -i sun_test.avi -c:v libx264 -crf 24 -pix_fmt yuv420p sun_test.mp4

# the battle scene's red dwarf sun, in the scene that uses it
./scripts/render-sun.sh "D:/grim-space-recordings/battle_backdrop.avi" 60 --battle
```

Useful flags for the dev scene: `--304` (chromosphere palette), `--noglow`,
`--battle` (the real battle backdrop), `--camdist=`, `--spin=`, `--seed=`,
`--quadscale=`.

---

## 8. Tests

`grim-space.Tests` runs 770 tests, all passing, through the xunit v3
in-process runner:

```
grim-space.Tests/bin/Debug/net10.0/grim-space.Tests.exe
  Total: 770, Errors: 0, Failed: 0, Skipped: 0, Not Run: 0
```

`dotnet test` reports "Zero tests ran" in this environment.  That is
**pre-existing and unrelated to this change** — it was verified by stashing
every modification and running the same command on the unmodified tree, which
fails identically.  The test project is an xunit v3 executable with
`Microsoft.Testing.Platform` selected in `global.json`; the direct runner is
the path that works here.

No test was added for the shader itself.  Shaders are graphics, not logic;
`AGENTS.md` is explicit that tests must not exercise the Godot framework, and
the visual verification in this document is the equivalent coverage.
