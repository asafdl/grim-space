# SUN-REF report — reference acquisition for the grim-space sun shader

**Status:** complete. 4 NASA/ESA sources downloaded (2 still series + 2 real motion
clips + 1 coronagraph series), 55+ reference frames extracted, all numbers in
`refs/palette.json` measured from the downloaded pixels by `refs/tools/measure.py`.

Deliverables: `refs/SOURCES.md`, `refs/palette.json`, `refs/analysis.md`,
`refs/frames/`, `refs/sdo/`, `refs/soho/`, `refs/measure_log.txt`,
`refs/measure_extra.json`, `refs/tools/measure.py`.

---

## 1. The 10 numbers the shader author needs

| # | Number | Where it comes from |
|---|---|---|
| 1 | **Disk centre = (130, 130, 130)** — neutral grey/white, *not* orange; equal channels | HMI continuum, `palette.disk.center_rgb` |
| 2 | **Disk limb = (46, 46, 46) = 0.35 × centre**; I(0.95 R) = 0.32 | HMI continuum, `palette.disk.limb_rgb` |
| 3 | **Limb-darkening curve: `I = clamp((1-(r/R)²)^0.45, 0, 1)`** (rms 0.011 over 20 samples) | `palette.limb_darkening` |
| 4 | **AIA 304: centre (168, 4, 0), bright limb ring (201, 34, 4)** → limb/centre = **1.20 R / 8.5 G / 4.0 B** = *inverse* limb | `palette.disk_0304` |
| 5 | **AIA 193: limb/centre = 1.14 R / 1.28 G / 1.70 B** — a clear bright limb in EUV | AIA 193 2048 |
| 6 | **Granulation/supergranulation: 31.4 px @2048 = 27 291 km, 51 cells across the disk, contrast 9.93 DN (~20 %)** | `palette.granulation` |
| 7 | **Prominence: 4 on the limb; core (212, 62, 1) → edge (105, 11, 0); bright height 0.068 R (118 000 km), diffuse extent 0.206 R (143 000 km); width 28.4 px (49 000 km)** | `palette.prominence` |
| 8 | **Motion cadence: real 12 s/frame AIA 304; over 3.01 h the prominence area moves 3 965 → 4 382 px (±5 %) with a ~50 min slow period, height 0.069–0.084 R** | `refs/measure_extra.json` → `prominence_motion` |
| 9 | **Corona is neutral white (135, 129, 123)**; the EUV off-limb limb edge falls as **r^−19.6** and is **zero by 1.30 R**; LASCO C2's occulting disc hides the corona inside **2.75 R** | `palette.corona` + LASCO C2 |
| 10 | **Scale: 1 R_sun = 800 px (AIA 2048) / 400 px (AIA 1024) / 960 px (HMI 2048) → 870 / 1 740 / 725 km per px; R_sun = 696 000 km** | `refs/measure_extra.json` → `plate_scales_km_per_px` |

---

## 2. Exact source URLs used

**SDO stills** (page: https://sdo.gsfc.nasa.gov/data)
```
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0304.jpg   (AIA 304, 1024)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0304.jpg   (AIA 304, 2048)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0171.jpg   (AIA 171, 1024)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0171.jpg   (AIA 171, 2048)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0094.jpg   (AIA 094, 1024)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0094.jpg   (AIA 094, 2048)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0193.jpg   (AIA 193, 1024)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0193.jpg   (AIA 193, 2048)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMII.jpg   (HMI continuum, 1024)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_HMII.jpg   (HMI continuum, 2048)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMIIC.jpg  (HMI intensitygram, colour)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_4500.jpg   (HMI 4500 A, saturated)
https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMIIHI.jpg (HMI intensitygram, high contrast)
```

**Real motion clips** (NASA Scientific Visualization Studio)
```
http://svs.gsfc.nasa.gov/vis/a000000/a003800/a003828/SDOAIA304A_20101021_HD1080.mp4
   page https://svs.gsfc.nasa.gov/3828/  — AIA 304, 12 s cadence, 2010-10-21/22, 10.0 h, 1920x1080, 49.7 MB
http://svs.gsfc.nasa.gov/vis/a000000/a005600/a005649/NOVA20260201_AIA.0304A_PSF_stamped_1024p30.mp4
   page https://svs.gsfc.nasa.gov/5649/  — AIA 304, 12 s cadence, Feb 2026 (solar max), 4 days total.
   Only a 30 s / 3.01 h excerpt was range-fetched (37.6 MB) instead of the full 1.20 GB asset.
```

**SOHO** (page: https://soho.nascom.nasa.gov/data/realtime-images.html)
```
https://soho.nascom.nasa.gov/data/LATEST/current_c2.gif      (LASCO C2, 512x512, 14 dated observations, 60.2 MB)
https://soho.nascom.nasa.gov/data/LATEST/current_eit_195.gif  (EIT 195, 512x512, 14 dated observations, 27.0 MB)
```

**White-light eclipse composite**
```
http://svs.gsfc.nasa.gov/vis/a030000/a030800/a030893/eclipse_composite_2017_without_credit.jpg
   page https://svs.gsfc.nasa.gov/30893/  — 1041x1041, 2017-08-21
```

Total downloaded: **~231 MB** (budget was 3 GB / "<100 MB aim" — over the soft aim
because the two real motion clips and the two animated coronagraph GIFs are the
irreplaceable parts; well under the hard cap).

---

## 3. What failed / what I had to work around

| Problem | Resolution |
|---|---|
| The brief says `latest_1024_0193.jpg` is **HMI continuum** — it is actually **AIA 193 Å** (the image is stamped "SDO/AIA 193") | Used `latest_2048_0193.jpg` for AIA 193 and added `latest_*_HMII.jpg` for the real grayscale continuum |
| The SDO **4500 Å** web product is colour-mapped and **saturated** (R and G clipped at 255) — useless for photometry | Used the HMI intensitygram (grayscale, max 145/169) for all continuum photometry |
| `latest_512_0304.gif` and `latest_512_0193.gif` → **404**; no animated GIF exists on the SDO "latest" service | Got real 12 s-cadence footage from NASA SVS mp4s instead (see above) |
| `current_c2_512.gif`, `current_eit_195_1024.jpg`, `current_c2_1024.jpg` → **404** | Used the 512 px `current_c2.gif` / `current_eit_195.gif` |
| The SVS 5649 4-day movie is **1.20 GB** (and the 4096 px version is 163 GB) | Range-fetched only a 30 s segment (3.01 h of real time) — 37.6 MB |
| **HMI is 0.5″/px and AIA is 0.6″/px** — my first measurement pass used one scale for both and produced a garbage limb-darkening fit (u₁ = 6.1) | Re-derived: 1 R_sun = 960 px (HMI) / 800 px (AIA) at 2048, 400 px at 1024; verified by the 1024↔2048 framing ratio (2.00/2.01) and the measured limb edge |
| First prominence detection swallowed the bright limb ring and the **burned-in timestamp strip** | Mask starts at 1.02 R, +6σ local background, 25 DN floor, timestamp strip and 10 px border excluded; verified visually with the magenta overlay |
| **AIA 304 has no off-limb corona** — it is a sharp limb edge, already zero at 1.30 R | Corona falloff taken from AIA 193; the LASCO C2 white-light corona used for the outer region; the *true colour* taken from the eclipse composite (achromatic 135,129,123) |
| The eclipse composite's ground-based ring **could not be auto-calibrated** (its achromatic radial profile is not a clean lunar limb + corona) | Did not force it: the corona block uses the AIA 193 EUV measurement, and the white-light truth is quoted from the composite's achromatic pixels |
| The classic visible-light limb-darkening law does **not** fit the measured SDO HMI profile | Reported the measured samples plus the clamped power law that does fit (rms 0.011) |

**Honest caveats for the shader author**

* Every EUV number (304/193/171) is a **colour-table rendering**, so its RGB is
  NASA palette, not solar colour. The only true-colour measurement here is the
  white-light corona: **neutral grey-white**.
* `corona.falloff_exponent` (19.6) is the falloff of the **EUV limb edge**,
  because that is the only 1.05–1.30 R corona we could measure at these
  resolutions. It is *not* the physical K-corona falloff, which is far gentler.
  The LASCO C2 fit (r^−0.17 in display space, log-stretched image) is in
  `refs/measure_extra.json` for reference.
* The prominence geometry comes from **one frame** of one 3 h window; the counts
  (4 on the limb) are typical of solar maximum but vary day to day.
* The 3 h motion window contains **no eruption** — it shows the quasi-steady
  baseline (slow ~50 min brighten/decay, ±10 % area, ±0.015 R height). Loop
  launch/collapse is a rarer, faster event not captured here.

---

## 4. Where to look

| Want | File |
|---|---|
| machine-readable numbers | `refs/palette.json` |
| the look, in prose, with all the reasoning | `refs/analysis.md` |
| where every byte came from | `refs/SOURCES.md` |
| raw diagnostics (annuli, blobs, radial profiles) | `refs/measure_log.txt` |
| long-form series (302-sample motion series, corona annuli) | `refs/measure_extra.json` |
| re-run the measurement | `python refs/tools/measure.py` |
| side-by-side visuals | `refs/frames/ref_montage_*.png` |
| prominence detection overlay (visual proof) | `refs/frames/ref_sdo5649_prominence_overlay_001.png` |
