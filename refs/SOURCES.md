# Reference sources — grim-space sun shader

All material below is NASA / NASA+ESA mission imagery. **License: NASA — public domain**
(SDO/AIA and SDO/HMI are NASA; SOHO/LASCO and SOHO/EIT are NASA+ESA; the eclipse
composite is published by NASA SVS with the credit lines listed per item).

Capture times are taken from the HTTP `Last-Modified` header and/or the timestamp
burned into the image itself (both are recorded). All files were downloaded
2026-10-19 from the URLs below and verified on disk.

---

## refs/sdo/ — NASA Solar Dynamics Observatory (SDO)

SDO flies in geosynchronous orbit and stares continuously at the Sun. Plate scale of
the full-frame products is 0.6 arcsec/px for AIA and 0.5 arcsec/px for HMI, i.e.
1 R_sun = 1600 px (AIA) / 1920 px (HMI) in the 4096-px product.

| File | Source URL | Page URL | Instrument / λ | Capture date | Resolution | License |
|---|---|---|---|---|---|---|
| `sdo_aia304_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0304.jpg | https://sdo.gsfc.nasa.gov/data | AIA 304 Å (He II, chromosphere/transition region) | 2026-10-19 17:14:08 UTC (HTTP Last-Modified) | 1024×1024 | NASA — public domain |
| `sdo_aia304_2048.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0304.jpg | https://sdo.gsfc.nasa.gov/data | AIA 304 Å | 2026-10-19 17:14 UTC | 2048×2048 | NASA — public domain |
| `sdo_aia171_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0171.jpg | https://sdo.gsfc.nasa.gov/data | AIA 171 Å (Fe IX) | 2026-10-19 17:22:06 UTC | 1024×1024 | NASA — public domain |
| `sdo_aia171_2048.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0171.jpg | https://sdo.gsfc.nasa.gov/data | AIA 171 Å | 2026-10-19 | 2048×2048 | NASA — public domain |
| `sdo_aia094_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_094.jpg | https://sdo.gsfc.nasa.gov/data | AIA 094 Å (Fe XVII, corona) | 2026-10-19 17:20:06 UTC | 1024×1024 | NASA — public domain |
| `sdo_aia094_2048.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_094.jpg | https://sdo.gsfc.nasa.gov/data | AIA 094 Å | 2026-10-19 | 2048×2048 | NASA — public domain |
| `sdo_aia193_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_0193.jpg | https://sdo.gsfc.nasa.gov/data | **AIA 193 Å (Fe XII) — note: the file is named `0193` but it is AIA 193, NOT HMI continuum** | 2026-10-19 17:18:05 UTC (burned into image) | 1024×1024 | NASA — public domain |
| `sdo_aia193_2048.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_0193.jpg | https://sdo.gsfc.nasa.gov/data | AIA 193 Å | 2026-10-19 | 2048×2048 | NASA — public domain |
| `sdo_hmii_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMII.jpg | https://sdo.gsfc.nasa.gov/data | HMI intensitygram (continuum, Fe I 6173 Å), **grayscale, unsaturated — this is the visible-light reference** | 2026-10-19 | 1024×1024 | NASA — public domain |
| `sdo_hmii_2048.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_2048_HMII.jpg | https://sdo.gsfc.nasa.gov/data | HMI intensitygram (continuum), grayscale | 2026-10-19 | 2048×2048 | NASA — public domain |
| `sdo_hmi_intensitygram_coloured_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMIIC.jpg | https://sdo.gsfc.nasa.gov/data | HMI intensitygram, colour-mapped | 2026-10-19 | 1024×1024 | NASA — public domain |
| `sdo_hmi_4500_saturated_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_4500.jpg | https://sdo.gsfc.nasa.gov/data | HMI 4500 Å continuum, colour-mapped — **saturated (R,G clipped), unusable for photometry** | 2026-10-19 17:00:06 UTC (burned into image) | 1024×1024 | NASA — public domain |
| `sdo_hmiihi_1024.jpg` | https://sdo.gsfc.nasa.gov/assets/img/latest/latest_1024_HMIIHI.jpg | https://sdo.gsfc.nasa.gov/data | HMI intensitygram, high-contrast colour map | 2026-10-19 | 1024×1024 | NASA — public domain |
| `sdo_svs3828_aia304_20101021_1080p30.mp4` | http://svs.gsfc.nasa.gov/vis/a000000/a003800/a003828/SDOAIA304A_20101021_HD1080.mp4 | https://svs.gsfc.nasa.gov/3828/ | AIA 304 Å, **12 s cadence** (1 movie frame = 12 s of real time, played at 30 fps) | 2010-10-21 18:00 → 2010-10-22 04:00 UTC | 1920×1080, 3001 frames, 100 s playback (10.0 h of real time) | NASA — public domain |
| `sdo_svs5649_aia304_20260201_t360.mp4` | http://svs.gsfc.nasa.gov/vis/a000000/a005600/a005649/NOVA20260201_AIA.0304A_PSF_stamped_1024p30.mp4 | https://svs.gsfc.nasa.gov/5649/ | AIA 304 Å, PSF-corrected, timestamp-stamped, **12 s cadence** (verified against the burned-in clock) | 2026-02-02 12:11 → 15:11 UTC (30 s excerpt of the 4-day 16-min movie; the full asset is 1.2 GB and was range-fetched, not fully downloaded) | 1024×1024, 907 frames | NASA — public domain |
| `eclipse_composite_2017_nasa_svs.jpg` | http://svs.gsfc.nasa.gov/vis/a030000/a030800/a030893/eclipse_composite_2017_without_credit.jpg | https://svs.gsfc.nasa.gov/30893/ | Composite: centre = NASA/SDO AIA 193, middle ring = ground-based total-eclipse corona (Williams College Eclipse Expedition / NSF / National Geographic), outer ring = ESA/NASA/SOHO LASCO C2 | 2017-08-21 | 1041×1041 | NASA-published composite; inner NASA/SDO, outer ESA/NASA/SOHO (public domain), ground-based ring credited as listed |

## refs/soho/ — NASA/ESA Solar and Heliospheric Observatory (SOHO)

| File | Source URL | Page URL | Instrument | Capture dates | Resolution | License |
|---|---|---|---|---|---|---|
| `soho_lasco_c2.gif` | https://soho.nascom.nasa.gov/data/LATEST/current_c2.gif | https://soho.nascom.nasa.gov/data/realtime-images.html | LASCO C2 coronagraph, white-light K-corona | 2026-10-15 15:24 → 2026-10-17 04:00 UTC (14 distinct observations, timestamps burned into each frame) | 512×512, 340 frames, 17 s playback | NASA/ESA — public domain |
| `soho_eit_195.gif` | https://soho.nascom.nasa.gov/data/LATEST/current_eit_195.gif | https://soho.nascom.nasa.gov/data/realtime-images.html | EIT 195 Å (Fe XII) full disk | 2026-10-15 → 2026-10-17 UTC (14 distinct observations) | 512×512, 134 frames, 6.7 s playback | NASA/ESA — public domain |

---

## refs/frames/ — extracted reference frames (PNG)

All extracted by `ffmpeg`/`PIL` from the files above; nothing here is a new
observation. Naming: `ref_<source>_<nnn>.png`.

| Pattern | Origin | Count | Notes |
|---|---|---|---|
| `ref_sdo5649_0304_001..010.png` | `sdo_svs5649_aia304_20260201_t360.mp4` | 10 | 3 s of playback apart = **18 min of real time apart**; each frame carries the burned-in UTC stamp (12:11:41 → 14:35:41 on 2026-02-02) |
| `ref_sdo3828_0304_001..008.png` | `sdo_svs3828_aia304_20101021_1080p30.mp4` | 8 | 13 s of playback apart = **1.3 h of real time apart**; 2010-10-21/22 |
| `ref_soho_c2_001..014.png` | `soho_lasco_c2.gif` | 14 | one PNG per *distinct* C2 observation (de-duplicated from 340 GIF frames) |
| `ref_soho_eit195_001..014.png` | `soho_eit_195.gif` | 14 | one PNG per *distinct* EIT observation (de-duplicated from 134 GIF frames) |
| `ref_sdo_hmi_continuum_1024_000.png` | `sdo_hmii_1024.jpg` | 1 | HMI continuum (grayscale) |
| `ref_sdo_hmi_4500_saturated_1024_000.png` | `sdo_hmi_4500_saturated_1024.jpg` | 1 | 4500 Å continuum, saturated |
| `ref_sdo_aia193_1024_000.png` | `sdo_aia193_1024.jpg` | 1 | AIA 193 |
| `ref_sdo_aia193_2048_000.png` | `sdo_aia193_2048.jpg` | 1 | AIA 193 (used for the off-limb corona) |
| `ref_sdo_aia171_1024_000.png` / `ref_sdo_aia171_2048_000.png` | `sdo_aia171_*.jpg` | 2 | AIA 171 |
| `ref_sdo_aia094_1024_000.png` / `ref_sdo_aia094_2048_000.png` | `sdo_aia094_*.jpg` | 2 | AIA 094 |
| `ref_sdo_aia304_2048_000.png` | `sdo_aia304_2048.jpg` | 1 | AIA 304 |
| `ref_eclipse_composite_2017_000.png` | `eclipse_composite_2017_nasa_svs.jpg` | 1 | white-light eclipse composite |
| `ref_sdo5649_prominence_overlay_001.png` | `ref_sdo5649_0304_001.png` | 1 | **machine-generated**: detected off-limb prominence pixels painted magenta, for visual verification of the prominence geometry |
| `ref_montage_5649_001.png`, `ref_montage_3828_001.png`, `ref_montage_c2_001.png`, `ref_montage_eit_001.png` | the above | 4 | contact sheets for quick side-by-side review |

## Reproducing the measurements

- `refs/tools/measure.py` — the whole measurement pipeline (numpy/PIL/cv2/ffmpeg).
  Run with `python refs/tools/measure.py` from the repo root. It writes
  `refs/palette.json`, `refs/measure_extra.json` and `refs/measure_log.txt`.
- `refs/measure_log.txt` — full diagnostic log of the last run (every measured
  annulus, every detected prominence blob, every radial profile).
- `refs/measure_extra.json` — per-sample series (302-sample prominence motion
  series, corona annuli, LASCO geometry, plate scales).

## Notes on known traps (for whoever re-runs this)

1. `latest_1024_0193.jpg` on the SDO site is **AIA 193 Å**, not HMI continuum.
2. HMI is **0.5 arcsec/px** (1 R_sun = 960 px in the 2048 web frame) while AIA is
   **0.6 arcsec/px** (1 R_sun = 800 px at 2048, 400 px at 1024). Using one scale
   for both breaks every radius-based measurement.
3. The SDO "4500 Å" web product is colour-mapped and **saturated** (R and G clipped
   at 255) — it cannot be used for photometry. Use `latest_*_HMII.jpg` instead.
4. LASCO C2 hides the corona inside ~2.75 R_sun behind its occulting disc, so the
   inner corona (1.0–1.3 R_sun) can only be seen in EUV images or from the ground
   during a total eclipse.
5. EIA 304 has **no off-limb corona** — it is a sharp limb edge that is already
   zero by 1.30 R_sun. Do not use it to calibrate a corona falloff.

---

## Repository copies

The files checked into this repository are **re-encoded / downscaled copies** of
the originals, so that the repository does not have to carry 237 MB of media.
`refs/tools/shrink_for_repo.py` performs the conversion (videos to 512 px
H.264, the animated GIFs to a reduced frame count at 384 px, the large PNG
frames to 1024 px). The file names, the source URLs above and the capture
dates are unchanged, and every number in `refs/palette.json`,
`refs/measure_extra.json` and `refs/measure_log.txt` was measured on the
**originals** before the conversion, so the measurements are unaffected.
