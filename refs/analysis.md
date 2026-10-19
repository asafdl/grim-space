# Sun look report — measured from NASA pixels

Everything below is measured from the downloaded reference frames by
`refs/tools/measure.py` (numpy/PIL/cv2). Raw diagnostics: `refs/measure_log.txt`.
Machine-readable values: `refs/palette.json`. Sources: `refs/SOURCES.md`.

**Scale conventions used throughout**

| Quantity | Value |
|---|---|
| Solar radius | R_sun = 696 000 km |
| AIA full frame | 0.6 arcsec/px → 1 R_sun = 800 px (2048 px images), 400 px (1024 px images) → 870 / 1740 km per px |
| HMI full frame | 0.5 arcsec/px → 1 R_sun = 960 px (2048 px images) → 725 km per px |
| Limb check | measured visible edge 831 px (AIA 304, 2048), 862 px (HMI), 416 px (AIA 304 movie, 1024) — the bright limb ring + prominences push the *detectable* edge 2–4 % outside the geometric limb |

---

## 1. Palette

### 1.1 Visible-light disk (the "white" sun)

`palette.disk` — HMI continuum (grayscale, unsaturated), median of each annulus:

| Region | Radius | RGB | Relative to centre |
|---|---|---|---|
| centre (`center_rgb`) | 0 – 0.08 R | **130, 130, 130** | 1.00 |
| mid (`mid_rgb`) | 0.45 – 0.55 R | **115, 115, 115** | 0.88 |
| limb (`limb_rgb`) | 0.90 – 0.98 R | **46, 46, 46** | **0.35** |

The photosphere is **neutral grey-to-white** — every channel is identical, there is
no yellow. A shader that wants a "white hot" star should saturate to (255,255,255)
in the core and fall to ~35 % of that at the limb, and should *not* tint the disk
orange. (The 4500 Å web product that looks yellow is a colour-mapped, clipped
rendering — unusable for this purpose.)

### 1.2 The 304 Å disk (chromosphere / transition region)

`palette.disk_0304` — AIA 304, 2048 px, colour-table rendering:

| Region | RGB | Note |
|---|---|---|
| quiet centre (`center_rgb`) | **168, 4, 0** | deep orange-red, G and B are almost zero |
| bright limb ring (`limb_rgb`) | **201, 34, 4** | top 15 % of the 0.94–1.00 R annulus |
| plain annulus median (not used) | 159, 2, 0 | the limb ring is *thin* (< 4 % of R) |

Limb/centre per channel: **R 1.20, G 8.5, B 4.0** → in 304 Å the limb is
dramatically brighter than the centre (an *inverse* limb, see §2).

### 1.3 Prominence core vs edge

`palette.prominence` — AIA 304 frame 2026-02-02 12:11:41 UT:

| | RGB | |
|---|---|---|
| core (`core_rgb`) | **212, 62, 1** | saturated orange, the hottest 10 % of the prominence |
| edge (`edge_rgb`) | **105, 11, 0** | the dimmer 30–55 % band of the same structure |

So a prominence is **not** a white-hot blob: it is the same orange as the disk but
~1.3–4× brighter in the green/blue channels, i.e. it reads as a brighter, more
saturated orange tongue against the darker disk behind it.

### 1.4 Corona

| Source | RGB | Meaning |
|---|---|---|
| `corona.rgb_at_1_05_r` (AIA 193, 1.05 R) | **175, 121, 57** | warm off-limb emission, still bright |
| `corona.rgb_at_1_15_r` (AIA 193, 1.15 R) | **97, 37, 6** | much dimmer, redder |
| `corona.rgb_at_1_30_r` (AIA 193, 1.30 R) | **0, 0, 0** | nothing left |
| white-light corona (ground-based eclipse, achromatic pixels) | **135, 129, 123** | **neutral grey-white** |
| LASCO C2 bright streamer (colour table) | 188, 18, 0 | the coronagraph's red colour table, not true colour |

**The corona is white/neutral in reality.** Every EUV or coronagraph product is
colour-mapped, so the orange in the 193/171/304 numbers is the instrument's
palette, not physics. If the shader paints the halo orange it will look like a
magnified 304 Å frame; a neutral white halo with a faint warm inner fringe is the
real look.

---

## 2. Limb darkening

`palette.limb_darkening` — median continuum luminance in 0.1-wide annuli,
normalised to the centre (HMI continuum, 1 R_sun = 960 px):

| r/R | 0.0 | 0.1 | 0.2 | 0.3 | 0.4 | 0.5 | 0.6 | 0.7 | 0.8 | 0.85 | 0.9 | 0.95 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| I/I(0) | 1.000 | 0.992 | 0.977 | 0.954 | 0.923 | 0.885 | 0.831 | 0.754 | 0.646 | 0.569 | 0.469 | 0.315 |

* **How much darker is the limb?** The limb (0.90–0.98 R) sits at **0.35** of the
  centre brightness; at 0.95 R it is 0.32. The published HMI rendering is
  *steeper* than the textbook visible-light law (u₁ = 0.6, u₂ = 0.2).
* **Best-fit shader curve** (rms 0.011 over all 20 samples), and the reason the
  schema value is `"curve": "clamp"`:

  ```
  I(r) = clamp( (1 - (r/R)²) ^ 0.45 , 0, 1 )
  ```

  The classic two-term law I(µ) = 1 − u₁(1−µ) − u₂(1−µ)² cannot fit this data
  (fitted u₁ = 8.8, u₂ = −17.8, rms 0.57) — the real curve collapses faster near
  the limb than a polynomial in µ allows.

### Inverse limb (bright limb) in the EUV

Yes, and it is strong:

| Channel | limb/centre | R | G | B |
|---|---|---|---|---|
| **AIA 193** | ratio | 1.14 | 1.28 | **1.70** |
| **AIA 304** (bright ring vs centre) | ratio | 1.20 | **8.5** | **4.0** |

AIA 193 is the textbook case: the coronal temperature/emissivity distribution makes
the *limb* brighter than the centre (a bright ring). 304 Å shows the same effect
even more extremely in the green/blue channels.

**Shader consequence:** one limb-darkening curve cannot serve both. A shader that
wants "a white sun with a dark limb" (continuum) and "an orange chromosphere with
a bright limb" (304) needs two different radial falloffs, or one falloff plus an
additive limb ring.

---

## 3. Granulation

`palette.granulation` — measured on the AIA 304 2048 frame (quietest of six
candidate patches, high-pass filtered, radial FFT of the power spectrum):

| Quantity | Value |
|---|---|
| `cell_size_px` | **31.4 px** (2048 px image) |
| `dominant_scale` | **27 291 km** feature |
| `cell_count_across_disk` | **51** across the 1 600 px disk diameter |
| `contrast_std` | **9.93 DN** of 255 → ≈ 20 % of the 49 DN quiet-disk level |

HMI continuum for comparison: 19.2 px cells (13 939 km, 100 across the disk),
contrast 5.46 DN.

**The 27 000 km scale is supergranulation, not granulation.** True photospheric
granulation is ~1 000 km and is *unresolved* at these resolutions (1 px = 870 km);
what a shader can legitimately texture is supergranulation/network scale
(20 000–30 000 km, i.e. 30–40 cells across the disk) with ~10–20 % contrast
against a slowly varying background. A shader that uses 1–2 px cells is claiming
a resolution the reference imagery does not contain.

---

## 4. Prominence geometry

`palette.prominence`, measured on the AIA 304 frame 2026-02-02 12:11:41 UT
(1024 px, 1 R_sun = 400 px = 1 740 km/px), by detecting off-limb blobs against a
per-radius local background (6σ, excluding the bright limb ring below 1.02 R and
the burned-in timestamp strip). Visual check: `refs/frames/ref_sdo5649_prominence_overlay_001.png`
(detected pixels painted magenta — they sit exactly on the two limb arcs).

| Structure | Area | Height above limb | Radial width | Position |
|---|---|---|---|---|
| largest | 4 214 px | **0.206 R** diffuse, **0.068 R** bright (143 000 / 118 000 km) | 59.7 px (104 000 km) | right (east) limb, 945,640 |
| #2 | 229 px | 0.128 R (89 000 km) | 27.8 px (48 000 km) | left (west) limb, 99,649 |
| #3 | 111 px | 0.133 R (93 000 km) | 14.7 px (26 000 km) | left limb, 82,631 |
| #4 | 105 px | 0.107 R (74 000 km) | 11.4 px (20 000 km) | left limb, 78,463 |

* `count_on_limb` = **4** prominences simultaneously visible on the limb in this
  frame (1 major + 3 minor), all on the *same* two limb arcs, none elsewhere.
* `filament_width_px` = **28.4 px** mean radial width = **49 000 km**, i.e. the
  loops are *thin* — 0.07 R thick — but *tall*: the biggest reaches 0.2 R
  (143 000 km ≈ 1/5 of the solar radius) above the limb.
* They **connect to the limb**: every detected blob starts at 1.02 R (the mask
  floor) and grows outward, so the loops are rooted on the limb and arch away from
  the disk. Nothing floats free of the disk.

**Shader consequence:** a recognisable sun needs a few (2–5) *tall, narrow,
limb-rooted* arcs, not round bubbles scattered over the disk. Height 0.07–0.2 R,
thickness ~0.05–0.07 R, brightness 1.3–4× the surrounding disk in G/B.

---

## 5. Prominence motion cadence

This is real motion, not a still series: the NASA SVS 5649 movie is AIA 304 at the
instrument's native **12 s cadence** (each movie frame = 12 s of real time, played
at 30 fps). The cadence is independently verified by the burned-in clock: the ten
extracted frames are 18 min apart in real time and their stamps read
12:11:41 → 14:35:41 on 2026-02-02.

Measurement: 904 movie frames = **3.01 h of real time**, sampled every 36 s
(302 samples). Metric: number of off-limb pixels brighter than 120 DN, and the
maximum height above the limb.

| Quantity | Value |
|---|---|
| prominence area | 3 965 – 4 382 px (≈ 4 100 ± 5 %) |
| maximum height above limb | 0.069 – 0.084 R (±10 %) |
| slow trend | rises from ~4 000 px at t = 0 to ~4 380 px at t ≈ 50–55 min, then relaxes to ~4 200 px by t ≈ 80 min |

**Rhythm:** over three hours the structures are *quasi-steady* — a slow brighten-
and-decay with a ~50 min period, plus ±10 % flicker on a ~20–30 min timescale.
There is no eruption, no loop launch and no collapse in this window: prominence
loops at this scale change on a timescale of **tens of minutes**, not seconds.
The visible loop height oscillates by only ~0.015 R, so a shader should animate
prominences *slowly* (a full breathe cycle ≈ 1–3 minutes of screen time is
already faster than reality) and keep the motion small-amplitude and confined to
the limb arcs.

---

## 6. Corona

### Shape

| Measurement | Value | Source |
|---|---|---|
| EUV off-limb brightness 1.05 R | 130 DN | AIA 193 |
| 1.15 R | 51 DN | AIA 193 |
| 1.30 R | 0 DN | AIA 193 |
| `corona.falloff_exponent` | **19.6** (I ∝ r^−19.6 over 1.03–1.42 R) | AIA 193 |
| AIA 171 equivalent | 18.8 | AIA 171 |
| LASCO C2: 1 R_sun marker | 38.5 px in a 512 px frame | SOHO/LASCO |
| LASCO C2 occulting disc | hides everything inside **2.75 R_sun** | SOHO/LASCO |
| LASCO C2 outer corona | I ∝ r^−0.17 in *display* space over 2.8–6.0 R | SOHO/LASCO |

Two very different things are being measured here and they must not be confused:

1. **The EUV off-limb "corona" is not a corona.** It is the sharp limb edge of
   the emission shell. It is bright at 1.05 R, half gone by 1.15 R, and *exactly
   zero* by 1.30 R. The steep exponent (≈ 19) is a real property of those pixels
   but it is the falloff of a limb, not of a halo. **A shader that paints the
   visible halo out to 1.3 R is painting something no EUV frame contains.**
2. **The white-light corona is a different object.** SOHO/LASCO C2 cannot see
   inside 2.75 R_sun (occulting disc) and shows streamers from there outward. Its
   image is log-stretched, so the fitted exponent (0.17) is a *display-space*
   number, not the physical K-corona falloff, which is steep (≈ r^−2 to r^−3) in
   that range.

### Colour shift with distance

* 1.05 R: warm, R ≫ G ≫ B (175, 121, 57) — mostly emission still coming from the
  limb shell.
* 1.15 R: (97, 37, 6) — dimmer and *redder* (G/R drops from 0.69 to 0.38).
* ≥ 1.30 R: nothing in EUV.
* True white light: **neutral** (135, 129, 123) at all radii — the blue-rich
  component of the real corona is small, and the human-visible corona is
  white/grey, only slightly warm.

---

## 7. Checklist — what a shader must reproduce to be recognisable

1. **Neutral, not orange, core.** Photosphere = (130,130,130) in the reference
   rendering, i.e. white-hot with a *dark* limb: I(0.95 R) = 0.32 of centre.
2. **Use the clamped power law** `I = clamp((1-(r/R)²)^0.45, 0, 1)` for the
   continuum disk, not the textbook polynomial (rms 0.011 vs 0.57).
3. **Two different edges.** If you also render the chromosphere/304 look, its limb
   is *brighter* than its centre (1.2× R, 8.5× G, 4× B) — the inverse limb.
4. **Texture at supergranulation scale**, 27 000 km ≈ 31 px at 2048, ~50 cells
   across the disk, 10–20 % contrast. Do not claim 1 000 km granulation.
5. **Prominences: 2–5, limb-rooted, tall and thin** — 0.07–0.2 R above the limb,
   ~0.07 R thick, 1.3–4× the disk brightness in G/B, colour (212,62,1) core
   fading to (105,11,0).
6. **Animate prominences over tens of minutes**, not seconds; amplitude ±10 % in
   area, ±0.015 R in height.
7. **The halo is white/neutral** (135,129,123), not orange, and if you tint it,
   tint it warm only very close in (the 1.05 R emission) and let it go neutral
   outward. Its visible extent is short in EUV (nothing by 1.3 R) — a long
   orange halo is a 304 Å look, not a corona.
8. **Sunspot-free disk is not the norm**: at solar maximum the reference frames
   carry several bright active regions (white-hot in 304) — the Feb 2026 frame
   has 4–5 of them, occupying a few percent of the disk area.
