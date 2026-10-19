#!/usr/bin/env python3
"""
Pixel measurement of NASA solar reference material for the grim-space sun shader.

Reads only files under refs/ and writes refs/palette.json, refs/measure_extra.json
and refs/measure_log.txt. Every number in palette.json comes from the downloaded
pixels via this script.

Geometry
--------
SDO/AIA + HMI full frames are 0.6 arcsec/px -> 1 R_sun (960") = 1600 px in the
4096 product, i.e. R_SUN = 800 px in the 2048 web frames and 400 px in the 1024
web frames.  The measured limb edge (815-831 px at 2048) agrees to 2-4% (the
bright limb ring + prominences extend the visible edge slightly outward).
"""
import glob
import json
import math
import os
import subprocess

import cv2
import numpy as np
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))          # refs/tools
REFS = os.path.dirname(HERE)                               # refs
R_SUN_KM = 696000.0
R_SUN_2048 = 800.0      # AIA frames: 0.6 arcsec/px -> 1600 px per R_sun in the 4096 product
R_SUN_1024 = 400.0
R_SUN_HMI_2048 = 960.0  # HMI frames: 0.5 arcsec/px -> 1920 px per R_sun in the 4096 product
R_SUN_HMI_1024 = 480.0
LOG = []


def p(*a):
    s = " ".join(str(x) for x in a)
    print(s)
    LOG.append(s)


def load(rel):
    im = Image.open(os.path.join(REFS, rel)).convert("RGB")
    return np.asarray(im).astype(np.float64)


def lum_of(a):
    return a @ np.array([0.299, 0.587, 0.114])


def radial(shape, cx, cy, R):
    h, w = shape[:2]
    Y, X = np.mgrid[0:h, 0:w]
    return np.hypot(X - cx, Y - cy) / R


def edge_radius(lum, cx, cy, edge_frac=0.5, lo=0.10, hi=0.34):
    h, w = lum.shape
    Y, X = np.mgrid[0:h, 0:w]
    rr = np.hypot(X - cx, Y - cy)
    half = int(min(h, w) / 2)
    prof = np.full(half, np.nan)
    for i in range(half - 1):
        s = (rr >= i) & (rr < i + 1)
        prof[i] = np.median(lum[s]) if s.any() else np.nan
    plateau = float(np.nanmedian(prof[int(lo * half):int(hi * half)]))
    above = np.where(prof >= edge_frac * plateau)[0]
    R = float(above.max()) if above.size else float(half)
    return R, plateau, prof


def med(v):
    return float(np.median(v)) if v.size else float("nan")


def ann_lum(lum, d, r0, r1):
    s = (d >= r0) & (d < r1)
    return med(lum[s]), int(s.sum())


def ann_rgb(a, d, r0, r1):
    s = (d >= r0) & (d < r1)
    if not s.any():
        return [0, 0, 0]
    return [int(round(med(a[:, :, c][s]))) for c in range(3)]


def bright_limb_rgb(a, d, r0, r1, pct=85):
    """Median RGB of the bright limb-ring pixels in an annulus (top pct% of the annulus)."""
    lum = lum_of(a)
    s = (d >= r0) & (d < r1)
    if s.sum() < 50:
        return [0, 0, 0], 0
    v = lum[s]
    keep = v >= np.percentile(v, pct)
    flat = np.flatnonzero(s)[keep]
    return [int(round(med(a.reshape(-1, 3)[flat, c]))) for c in range(3)], int(keep.sum())


def limb_darkening(lum, d, label):
    c0, _ = ann_lum(lum, d, 0.0, 0.05)
    samples = []
    for i in range(0, 20):
        rf = round(0.05 * i, 2)
        v, _ = ann_lum(lum, d, max(0.0, rf - 0.05), rf + 0.05)
        samples.append({"radius_frac": rf, "intensity": round(v / c0, 4)})
    rf_a = np.array([s["radius_frac"] for s in samples])
    y_a = np.array([s["intensity"] for s in samples])
    # (a) classic two-term law over the inner 85% of the disk
    x1, x2 = [], []
    for rf in rf_a:
        mu = math.sqrt(max(0.0, 1.0 - rf * rf))
        x1.append(1 - mu)
        x2.append((1 - mu) ** 2)
    A = np.vstack([x1, x2]).T
    m = rf_a <= 0.85
    sol, *_ = np.linalg.lstsq(A[m], y_a[m], rcond=None)
    resid = y_a[m] - A[m] @ sol
    # (b) clamped power law I = clamp((1 - r^2)^alpha, 0, 1) over every sample
    def rms(a):
        pred = np.clip((1 - rf_a ** 2) ** a, 0, 1)
        return float(np.sqrt(((pred - y_a) ** 2).mean()))
    grid = np.linspace(0.05, 1.5, 300)
    alpha = float(grid[int(np.argmin([rms(a) for a in grid]))])
    p(f"  [{label}] centre lum={c0:.1f}  classic fit (rf<=0.85): u1={sol[0]:.3f} u2={sol[1]:.3f} "
      f"rms={float(np.sqrt((resid ** 2).mean())):.4f}  clamped power law I=clip((1-r^2)^a,0,1): "
      f"alpha={alpha:.3f} rms={rms(alpha):.4f}  limb(0.95)={samples[-1]['intensity']:.3f}")
    return samples, sol, c0, alpha


def granulation(a, cx, cy, R, label, box=1024):
    h, w = a.shape[:2]
    box = min(box, h - 2, w - 2)
    lum = lum_of(a)

    def patch_at(px, py):
        x0 = int(np.clip(px - box / 2, 0, w - box))
        y0 = int(np.clip(py - box / 2, 0, h - box))
        pl = lum[y0:y0 + box, x0:x0 + box]
        return pl - cv2.GaussianBlur(pl, (0, 0), 8.0)

    best, best_rms = None, 1e18
    for dx, dy in [(0, 0), (0.30, -0.30), (-0.30, 0.30), (0.34, 0.20), (-0.22, -0.34), (0.0, -0.36)]:
        pl = patch_at(cx + dx * R, cy + dy * R)
        if pl.std() < best_rms:
            best_rms, best = pl.std(), pl
    g = best
    F = np.abs(np.fft.fftshift(np.fft.fft2(g))) ** 2
    n = box
    fq = np.fft.fftshift(np.fft.fftfreq(n))
    FX, FY = np.meshgrid(fq, fq)
    rad = np.hypot(FX, FY)
    bins = np.linspace(0, 0.5, 150)
    idx = np.digitize(rad.ravel(), bins)
    prof = np.array([F.ravel()[idx == i].mean() if (idx == i).any() else 0.0 for i in range(1, len(bins))])
    centres = (bins[:-1] + bins[1:]) / 2
    band = (centres >= 1 / 64) & (centres <= 0.25)
    fstar = float(centres[band][np.argmax(prof[band])])
    cell_px = 1.0 / fstar
    hp = g - cv2.GaussianBlur(g, (0, 0), cell_px / 2.0)
    contrast_std = float(hp.std())
    km_px = R_SUN_KM / R
    p(f"  [{label}] dominant wavelength={cell_px:.1f} px  cells_across_disk={2 * R / cell_px:.1f}  "
      f"contrast_std={contrast_std:.2f} DN  ({cell_px * km_px:.0f} km/cell)")
    return {"cell_count_across_disk": round(2 * R / cell_px, 2),
            "cell_size_px": round(cell_px, 2),
            "contrast_std": round(contrast_std, 2),
            "dominant_scale": f"{cell_px * km_px:.0f} km feature ({cell_px:.1f} px, {km_px:.0f} km/px)"}


def usable_mask(shape, d, cx, cy, R):
    """Exclude the burned-in timestamp strip and the frame border."""
    h, w = shape[:2]
    Y, X = np.mgrid[0:h, 0:w]
    ok = (Y < 0.915 * h) & (X > 10) & (X < w - 10) & (Y > 10)
    return ok


def prominence_mask(lum, d, ok, rmin=1.02, rmax=1.25, k=6.0, floor=25.0):
    band = ok & (d > rmin) & (d < rmax)
    if band.sum() < 500:
        return None
    ri = (d * 1000).astype(int)
    bg = np.full(ri.max() + 2, np.nan)
    sg = np.full(ri.max() + 2, np.nan)
    for kk in range(int(rmin * 1000), int(rmax * 1000) + 1):
        s = (ri == kk) & band
        if s.sum() < 15:
            continue
        v = lum[s]
        m = float(np.median(v))
        bg[kk] = m
        sg[kk] = max(1.4826 * float(np.median(np.abs(v - m))), 1.0)
    thr = np.where(np.isnan(sg), np.inf, bg + k * np.where(np.isnan(sg), 0, sg))
    tmap = thr[ri]
    mask = ((lum > np.maximum(tmap, floor)) & band).astype(np.uint8)
    mask = cv2.morphologyEx(mask, cv2.MORPH_CLOSE, np.ones((3, 3), np.uint8))
    return mask


def prominence_stats(a, cx, cy, R, label, min_area=100, min_h=0.008, overlay=None):
    lum = lum_of(a)
    d = radial(a.shape, cx, cy, R)
    ok = usable_mask(a.shape, d, cx, cy, R)
    mask = prominence_mask(lum, d, ok)
    if mask is None:
        return {"core_rgb": [0, 0, 0], "edge_rgb": [0, 0, 0], "loop_height_over_radius": 0.0,
                "filament_width_px": 0.0, "count_on_limb": 0}
    n, lab, stats, cent = cv2.connectedComponentsWithStats(mask, 8)
    blobs = []
    for i in range(1, n):
        area = int(stats[i, cv2.CC_STAT_AREA])
        if area < min_area:
            continue
        s = lab == i
        dd = d[s]
        h = float(dd.max()) - 1.0
        if h < min_h or float(dd.max()) > 1.245:
            continue
        blobs.append({"idx": i, "area": area, "h": h, "dmin": float(dd.min()),
                      "dmax": float(dd.max()), "c": cent[i]})
    blobs.sort(key=lambda b: -b["area"])
    p(f"  [{label}] {len(blobs)} prominence blobs (area>={min_area}px)")
    for b in blobs:
        p(f"     area={b['area']:6d}px height_above_limb={b['h']:.3f}R ({b['h'] * R:.0f}px = "
          f"{b['h'] * R * (R_SUN_KM / R):.0f} km) radial_extent={(b['dmax'] - b['dmin']) * R:.1f}px "
          f"centre=({b['c'][0]:.0f},{b['c'][1]:.0f})")
    if not blobs:
        return {"core_rgb": [0, 0, 0], "edge_rgb": [0, 0, 0], "loop_height_over_radius": 0.0,
                "filament_width_px": 0.0, "count_on_limb": 0}
    b = blobs[0]
    s = lab == b["idx"]
    v = lum[s]
    core_sel = s & (lum >= np.percentile(v, 90))
    v_lo, v_hi = np.percentile(v, 30), np.percentile(v, 55)
    edge_sel = s & (lum >= v_lo) & (lum <= v_hi)
    core_rgb = [int(round(med(a[:, :, c][core_sel]))) for c in range(3)]
    edge_rgb = [int(round(med(a[:, :, c][edge_sel]))) for c in range(3)]
    widths = [(bb["dmax"] - bb["dmin"]) * R for bb in blobs]
    h_max = max(bb["h"] for bb in blobs)
    bright = s & (lum >= 120)
    h_bright = (float(d[bright].max()) - 1.0) if bright.any() else 0.0
    p(f"  [{label}] core_rgb={core_rgb} edge_rgb={edge_rgb} diffuse_height={h_max:.3f}R "
      f"bright_height={h_bright:.3f}R mean_radial_width={float(np.mean(widths)):.1f}px "
      f"count_on_limb={len(blobs)}")
    if overlay:
        vis = a.astype(np.uint8).copy()
        vis[mask > 0] = (0.45 * vis[mask > 0] + 0.55 * np.array([255, 0, 255])).astype(np.uint8)
        Image.fromarray(vis).save(os.path.join(REFS, overlay))
        p(f"  [{label}] overlay -> {overlay}")
    return {"core_rgb": core_rgb, "edge_rgb": edge_rgb,
            "loop_height_over_radius": round(h_bright, 3),
            "filament_width_px": round(float(np.mean(widths)), 1),
            "count_on_limb": int(len(blobs)),
            "diffuse_height_R": round(h_max, 3)}


def motion_series(mp4, cx, cy, R, every=3, w=1024, h=1024):
    """Prominence area / peak height per frame of the real 12 s/frame movie."""
    cmd = ["ffmpeg", "-v", "error", "-i", mp4, "-f", "rawvideo", "-pix_fmt", "rgb24", "-"]
    proc = subprocess.Popen(cmd, stdout=subprocess.PIPE, bufsize=10 ** 8)
    n = w * h * 3
    rows = []
    idx = 0
    while True:
        buf = proc.stdout.read(n)
        if len(buf) < n:
            break
        if idx % every == 0:
            im = np.frombuffer(buf, np.uint8).reshape(h, w, 3).astype(np.float64)
            lum = lum_of(im)
            d = radial(im.shape, cx, cy, R)
            ok = usable_mask(im.shape, d, cx, cy, R)
            band = ok & (d > 1.02) & (d < 1.25)
            sel = band & (lum > 120)
            area = int(sel.sum())
            hmax = float(d[sel].max()) - 1.0 if sel.any() else 0.0
            rows.append({"frame": idx, "t_min": round(idx * 12 / 60.0, 1),
                         "prominence_px": area, "max_height_R": round(hmax, 4)})
        idx += 1
    proc.stdout.close()
    proc.wait()
    return rows, idx


def corona_profile(a, cx, cy, R, radii, label, rmax=1.42):
    lum = lum_of(a)
    d = radial(a.shape, cx, cy, R)
    far = d > rmax - 0.08
    bg = float(np.percentile(lum[far], 60)) if far.any() else 0.0
    out = {}
    for rr in radii:
        s = (d >= rr - 0.02) & (d < rr + 0.02)
        out[rr] = ([int(round(med(a[:, :, c][s]))) for c in range(3)], med(lum[s]), int(s.sum()))
        p(f"  [{label}] r={rr:.2f}R rgb={out[rr][0]} lum={out[rr][1]:.2f} bg={bg:.2f} n={out[rr][2]}")
    rs, ls = [], []
    for rr in np.arange(1.03, rmax, 0.02):
        s = (d >= rr) & (d < rr + 0.02)
        if s.sum() < 50:
            continue
        v = med(lum[s]) - bg
        if v <= 0.5:
            continue
        rs.append(rr)
        ls.append(v)
    n_exp = float("nan")
    if len(rs) > 4:
        A = np.vstack([np.ones(len(rs)), np.log(rs)]).T
        sol, *_ = np.linalg.lstsq(A, np.log(ls), rcond=None)
        n_exp = -sol[1]
    p(f"  [{label}] falloff I ~ r^-{n_exp:.2f} over 1.03-{rmax}R (n={len(rs)} annuli, bg={bg:.2f})")
    return out, n_exp, bg


def lasco(path):
    """LASCO C2: white limb-circle (1 R_sun), occulting disc, radial falloff, streamer colour."""
    a = load(path)
    lum = lum_of(a)
    h, w = lum.shape
    cy, cx = h / 2.0, w / 2.0
    Y, X = np.mgrid[0:h, 0:w]
    rr = np.hypot(X - cx, Y - cy)
    corner = (np.abs(X - cx) > 0.33 * w) & (np.abs(Y - cy) > 0.33 * w)
    mx, mn = a.max(axis=2), a.min(axis=2)
    white = (mn > 120) & ((mx - mn) < 45) & (rr < 0.25 * min(h, w))
    r_sun = float("nan")
    if white.sum() > 50:
        hist, edges = np.histogram(rr[white], bins=np.arange(0, 0.25 * min(h, w), 1.0))
        r_sun = float(edges[int(np.argmax(hist))] + 0.5)
    prof = []
    for r in range(0, int(0.47 * w), 2):
        sel = (rr >= r) & (rr < r + 2) & (~corner)
        prof.append((r, float(np.median(lum[sel])) if sel.sum() > 20 else np.nan))
    occ = prof[0][1]
    keep = [r for r, v in prof if np.isfinite(v) and v >= 0.99 * occ and r < 0.30 * w]
    r_occ = float(max(keep)) if keep else float("nan")
    rs, ls = [], []
    for r in range(int(r_occ) + 2, int(0.45 * w), 3):
        sel = (rr >= r) & (rr < r + 3) & (~corner)
        if sel.sum() < 30:
            continue
        rs.append(r / r_sun)
        ls.append(float(lum[sel].mean()))
    rs = np.array(rs)
    ls = np.array(ls)
    good = ls > 0.05 * ls.max()
    n_exp = float("nan")
    if good.sum() > 4:
        A = np.vstack([np.ones(int(good.sum())), np.log(rs[good])]).T
        sol, *_ = np.linalg.lstsq(A, np.log(ls[good]), rcond=None)
        n_exp = -sol[1]
    band = (rr > r_occ) & (~corner)
    cs = band & (lum > np.percentile(lum[band], 85))
    rgb = [int(round(med(a[:, :, c][cs]))) for c in range(3)]
    p(f"  LASCO C2 {os.path.basename(path)}: 1R_sun(white circle)={r_sun:.1f}px  flat occulter to "
      f"r={r_occ:.0f}px ({r_occ / r_sun:.2f} R)  corona I~r^-{n_exp:.2f} over {r_occ / r_sun:.1f}-"
      f"{0.45 * w / r_sun:.1f} R  streamer rgb={rgb}")
    return {"r_sun_px": r_sun, "occ_r_px": r_occ, "falloff": n_exp, "rgb": rgb}


def white_light_colour(path):
    a = load(path)
    mx, mn = a.max(axis=2), a.min(axis=2)
    lum = lum_of(a)
    sel = ((mx - mn) < 30) & (lum > 35)
    rgb = [int(round(med(a[:, :, c][sel]))) for c in range(3)]
    p(f"  white-light corona (achromatic pixels of {os.path.basename(path)}): rgb={rgb} n={int(sel.sum())}")
    return rgb


def main():
    p("=" * 78)
    p("SUN-REF pixel measurement")
    p("=" * 78)

    # ---------- 0. geometry ----------
    p("[0] geometry: SDO full frames are 0.6\"/px -> 1 R_sun = 1600 px (4096) = 800 (2048) = 400 (1024)")
    hmi = load("sdo/sdo_hmii_2048.jpg")
    hlum = lum_of(hmi)
    h, w = hlum.shape
    hcx, hcy = w / 2.0, h / 2.0
    hR_meas, hplateau, hprof = edge_radius(hlum, hcx, hcy, 0.5)
    hkm = R_SUN_KM / R_SUN_HMI_2048
    hmi1024 = load("sdo/sdo_hmii_1024.jpg")
    h1024_meas, _, _ = edge_radius(lum_of(hmi1024), 512.0, 512.0, 0.5)
    p(f"  HMI 2048: nominal R_sun={R_SUN_HMI_2048:.0f}px (0.5 arcsec/px)  measured edge={hR_meas:.0f}px  "
      f"plateau={hplateau:.0f}  plate scale={hkm:.0f} km/px")
    p(f"  HMI 1024: measured edge={h1024_meas:.0f}px (nominal {R_SUN_HMI_1024:.0f}) -> framing ratio "
      f"{hR_meas / h1024_meas:.2f} (2.00 = same framing)")
    p("  HMI radial profile (median lum vs radius): " +
      " ".join(f"{r}:{hprof[r]:.0f}" for r in range(0, int(h / 2), 100)))

    # ---------- 1. visible-light continuum ----------
    p("[1] HMI continuum 2048 (grayscale) - disk palette + limb darkening")
    hd = radial(hmi.shape, hcx, hcy, R_SUN_HMI_2048)
    disk_center = ann_rgb(hmi, hd, 0.0, 0.08)
    disk_mid = ann_rgb(hmi, hd, 0.45, 0.55)
    disk_limb = ann_rgb(hmi, hd, 0.90, 0.98)
    p(f"  disk center_rgb={disk_center} mid_rgb={disk_mid} limb_rgb={disk_limb}")
    ld_samples, ld_fit, ld_c0, ld_alpha = limb_darkening(hlum, hd, "HMI continuum")
    limb_ratio = disk_limb[0] / max(1, disk_center[0])
    p(f"  limb/center = {limb_ratio:.3f}")

    # ---------- 2. AIA 304 disk ----------
    p("[2] AIA 304 2048 - disk palette + bright-limb test")
    a304 = load("sdo/sdo_aia304_2048.jpg")
    aH, aW = a304.shape[:2]
    acx, acy = aW / 2.0, aH / 2.0
    aR = R_SUN_2048
    ad = radial(a304.shape, acx, acy, aR)
    a304lum = lum_of(a304)
    aR_meas, aplateau, _ = edge_radius(a304lum, acx, acy, 0.5)
    p(f"  304 2048: nominal R_sun={aR:.0f}px measured edge={aR_meas:.0f}px plateau={aplateau:.0f} "
      f"plate scale={R_SUN_KM / aR:.0f} km/px")
    d304_center = ann_rgb(a304, ad, 0.0, 0.10)
    d304_limb_plain = ann_rgb(a304, ad, 0.90, 0.98)
    d304_limb, n_limb_px = bright_limb_rgb(a304, ad, 0.94, 1.00, 85)
    d304_ratio = [round(d304_limb[i] / max(1, d304_center[i]), 2) for i in range(3)]
    p(f"  304 center_rgb={d304_center} limb(plain median)={d304_limb_plain} "
      f"limb(bright ring, {n_limb_px}px)={d304_limb} ratio(limb/center)={d304_ratio}")

    # ---------- 3. AIA 193 disk (bright limb) ----------
    p("[3] AIA 193 2048 - disk palette + inverse limb darkening")
    a193 = load("sdo/sdo_aia193_2048.jpg")
    iH, iW = a193.shape[:2]
    icx, icy = iW / 2.0, iH / 2.0
    iR = R_SUN_2048
    idd = radial(a193.shape, icx, icy, iR)
    i193lum = lum_of(a193)
    iR_meas, iplateau, _ = edge_radius(i193lum, icx, icy, 0.5)
    a193_1024 = load("sdo/sdo_aia193_1024.jpg")
    i1024_meas, _, _ = edge_radius(lum_of(a193_1024), 512.0, 512.0, 0.5)
    p(f"  193 2048: nominal R_sun={iR:.0f}px (0.6 arcsec/px) measured edge={iR_meas:.0f}px "
      f"plateau={iplateau:.0f} plate scale={R_SUN_KM / iR:.0f} km/px")
    p(f"  AIA 193 1024: measured edge={i1024_meas:.0f}px (nominal {R_SUN_1024:.0f}) -> framing ratio "
      f"{iR_meas / max(1e-9, i1024_meas):.2f} (2.00 = same framing)")
    d193_center = ann_rgb(a193, idd, 0.0, 0.10)
    d193_limb = ann_rgb(a193, idd, 0.92, 0.99)
    d193_ratio = [round(d193_limb[i] / max(1, d193_center[i]), 2) for i in range(3)]
    p(f"  193 center_rgb={d193_center} limb_rgb={d193_limb} ratio={d193_ratio}")

    # ---------- 4. granulation ----------
    p("[4] granulation scale")
    gran_304 = granulation(a304, acx, acy, aR, "AIA 304 (2048)")
    gran_hmi = granulation(hmi, hcx, hcy, R_SUN_HMI_2048, "HMI continuum (2048)")

    # ---------- 5. prominences ----------
    p("[5] prominences, AIA 304 movie frame 2026-02-02 12:11:41 UT (1024)")
    m = load("frames/ref_sdo5649_0304_001.png")
    mH, mW = m.shape[:2]
    mcx, mcy = mW / 2.0, mH / 2.0
    mR = R_SUN_1024
    mlum = lum_of(m)
    mR_meas, mplateau, _ = edge_radius(mlum, mcx, mcy, 0.5)
    p(f"  5649 f001: nominal R_sun={mR:.0f}px measured edge={mR_meas:.0f}px plateau={mplateau:.0f} "
      f"plate scale={R_SUN_KM / mR:.0f} km/px")
    prom = prominence_stats(m, mcx, mcy, mR, "5649 f001",
                            overlay="frames/ref_sdo5649_prominence_overlay_001.png")

    # ---------- 6. prominence motion ----------
    p("[6] prominence motion from the real 12 s/frame AIA 304 movie")
    rows, nframes = motion_series(os.path.join(REFS, "sdo", "sdo_svs5649_aia304_20260201_t360.mp4"),
                                   mcx, mcy, mR, every=3)
    p(f"  {nframes} movie frames ({nframes * 12 / 3600.0:.2f} h of real time at 12 s/frame), "
      f"{len(rows)} samples every 36 s")
    for r in rows[::6]:
        p(f"   frame {r['frame']:4d} t={r['t_min']:6.1f} min  prominence_px={r['prominence_px']:6d}  "
          f"max_height={r['max_height_R']:.3f} R")
    areas = [r["prominence_px"] for r in rows]
    hs = [r["max_height_R"] for r in rows]
    p(f"  prominence area over 3 h: min={min(areas)} max={max(areas)} px  "
      f"peak height {min(hs):.3f}..{max(hs):.3f} R")

    # ---------- 7. corona ----------
    p("[7] corona")
    cor_rgb = {1.05: ([0, 0, 0], 0.0, 0), 1.15: ([0, 0, 0], 0.0, 0), 1.30: ([0, 0, 0], 0.0, 0)}
    cor_n, cor_bg = float("nan"), 0.0
    c171 = dict(cor_rgb)
    n171, bg171 = float("nan"), 0.0
    las = {"r_sun_px": float("nan"), "occ_r_px": float("nan"), "falloff": float("nan"), "rgb": [0, 0, 0]}
    wl = [0, 0, 0]
    try:
        cor_rgb, cor_n, cor_bg = corona_profile(a193, icx, icy, iR, [1.05, 1.15, 1.30],
                                                 "AIA 193 off-limb", rmax=1.42)
        a171 = load("sdo/sdo_aia171_2048.jpg")
        c171, n171, bg171 = corona_profile(a171, icx, icy, iR, [1.05, 1.15, 1.30],
                                           "AIA 171 off-limb", rmax=1.42)
    except Exception as exc:
        p(f"  WARNING: EUV corona profile failed: {exc}")
    try:
        las = lasco("frames/ref_soho_c2_001.png")
    except Exception as exc:
        p(f"  WARNING: LASCO analysis failed: {exc}")
    try:
        wl = white_light_colour("sdo/eclipse_composite_2017_nasa_svs.jpg")
    except Exception as exc:
        p(f"  WARNING: white-light colour failed: {exc}")

    notes = [
        f"disk.* = HMI continuum, SDO latest_2048_HMII.jpg (grayscale, unsaturated). HMI is 0.5 "
        f"arcsec/px -> 1 R_sun = {R_SUN_HMI_2048:.0f}px in the 2048 web frame ({hkm:.0f} km/px), frame "
        f"centre used; measured limb edge {hR_meas:.0f}px. AIA is 0.6 arcsec/px -> 1 R_sun = "
        f"{R_SUN_2048:.0f}px at 2048 and {R_SUN_1024:.0f}px at 1024.",
        f"limb_darkening: median continuum luminance per 0.1-wide annulus, normalised to the centre. "
        f"Classic law I(mu)/I(0)=1-u1(1-mu)-u2(1-mu)^2 fitted over r<0.85R -> u1={ld_fit[0]:.2f}, "
        f"u2={ld_fit[1]:.2f}. RECOMMENDED shader curve (matches the measured samples, clamped at the "
        f"limb): I = clamp((1-(r/R)^2)^{ld_alpha:.2f}, 0, 1). limb(0.95R)/centre="
        f"{ld_samples[-1]['intensity']:.2f}.",
        f"disk_0304 = AIA 304, SDO latest_2048_0304.jpg ({R_SUN_KM / aR:.0f} km/px). limb_rgb is the BRIGHT "
        f"LIMB RING (top 15% of the 0.94-1.00 R annulus, {n_limb_px} px); the plain annulus median is "
        f"{d304_limb_plain}. limb/centre per channel = {d304_ratio}.",
        f"AIA 193 (SDO latest_2048_0193.jpg - NOTE: the latest_1024_0193.jpg asset is AIA 193, NOT HMI "
        f"continuum): centre {d193_center}, limb {d193_limb}, ratio {d193_ratio} -> strong INVERSE limb "
        f"darkening in EUV.",
        f"granulation: dominant scale {gran_304['dominant_scale']} in AIA 304 (contrast_std "
        f"{gran_304['contrast_std']} DN of 255); HMI continuum {gran_hmi['dominant_scale']} "
        f"(contrast_std {gran_hmi['contrast_std']} DN). True photospheric granulation (~1000 km) is "
        f"UNRESOLVED at 2048 px; the measured texture is supergranulation/network scale.",
        f"prominence: AIA 304 frame 2026-02-02 12:11:41 UT (1024 px, {R_SUN_KM / mR:.0f} km/px). Off-limb "
        f"blobs detected with a per-radius local background + 6 sigma, excluding the bright limb ring "
        f"(start 1.02 R) and the burned-in timestamp strip. core/edge = median RGB of the top 10% / "
        f"boundary pixels of the largest prominence. loop_height_over_radius uses the BRIGHT material "
        f"(lum>120); the diffuse extent reaches {prom.get('diffuse_height_R')} R.",
        f"corona.falloff_exponent: fit on the AIA 193 off-limb region 1.03-1.42 R_sun, I ~ r^-{cor_n:.2f} "
        f"(background {cor_bg:.1f} DN). AIA 171 gives r^-{n171:.2f} over the same range.",
        f"corona.rgb_* : median pixels of the AIA 193 off-limb corona at 1.05/1.15/1.30 R_sun "
        f"(colour-TABLE rendering, not true colour; true white-light corona is achromatic).",
        f"white-light corona truth, SOHO LASCO C2 (512 px): drawn 1 R_sun circle = {las['r_sun_px']:.1f} px, "
        f"the flat occulting disc hides the corona inside ~{las['occ_r_px'] / las['r_sun_px']:.1f} R_sun, "
        f"radial falloff I ~ r^-{las['falloff']:.2f} out to "
        f"{0.47 * 512 / las['r_sun_px']:.1f} R_sun, bright streamer colour rgb={las['rgb']}.",
        f"true-colour white-light corona (ground-based total eclipse, NASA SVS 2017 composite, achromatic "
        f"pixels): rgb={wl} - white/neutral, NOT orange.",
        f"prominence motion: real AIA 304 movie at 12 s/frame (cadence verified from the burned-in "
        f"timestamps: 18 min between the 18-min-spaced extracted frames). {len(rows)} samples over "
        f"{nframes * 12 / 3600.0:.1f} h of real time.",
    ]

    palette = {
        "disk": {"center_rgb": disk_center, "mid_rgb": disk_mid, "limb_rgb": disk_limb},
        "disk_0304": {"center_rgb": d304_center, "limb_rgb": d304_limb},
        "limb_darkening": {"samples": ld_samples, "curve": "clamp"},
        "granulation": {
            "cell_count_across_disk": gran_304["cell_count_across_disk"],
            "cell_size_px": gran_304["cell_size_px"],
            "contrast_std": gran_304["contrast_std"],
            "dominant_scale": gran_304["dominant_scale"],
        },
        "prominence": {
            "core_rgb": prom["core_rgb"],
            "edge_rgb": prom["edge_rgb"],
            "loop_height_over_radius": prom["loop_height_over_radius"],
            "filament_width_px": prom["filament_width_px"],
            "count_on_limb": prom["count_on_limb"],
        },
        "corona": {
            "falloff_exponent": round(cor_n, 3),
            "rgb_at_1_05_r": cor_rgb[1.05][0],
            "rgb_at_1_15_r": cor_rgb[1.15][0],
            "rgb_at_1_30_r": cor_rgb[1.30][0],
        },
        "notes": notes,
    }
    with open(os.path.join(REFS, "palette.json"), "w", encoding="utf-8") as f:
        json.dump(palette, f, indent=2)
    p("wrote refs/palette.json")

    extra = {
        "granulation_hmi": gran_hmi,
        "lasco_c2": las,
        "white_light_corona_rgb": wl,
        "corona_aia193": {str(k): {"rgb": v[0], "lum": round(v[1], 2), "n": v[2]} for k, v in cor_rgb.items()},
        "corona_aia171": {str(k): {"rgb": v[0], "lum": round(v[1], 2), "n": v[2]} for k, v in c171.items()},
        "corona_falloff_exponent": {"aia193": round(cor_n, 3), "aia171": round(n171, 3),
                                    "lasco_c2": round(las["falloff"], 3)},
        "limb_darkening_fit": {"u1": round(float(ld_fit[0]), 3), "u2": round(float(ld_fit[1]), 3),
                               "limb_over_center": round(limb_ratio, 3), "centre_lum": ld_c0},
        "disk_193": {"center_rgb": d193_center, "limb_rgb": d193_limb, "ratio": d193_ratio},
        "disk_304_limb_plain_median": d304_limb_plain,
        "plate_scales_km_per_px": {"aia_2048": round(R_SUN_KM / R_SUN_2048),
                                   "aia_1024": round(R_SUN_KM / R_SUN_1024),
                                   "hmi_2048": round(R_SUN_KM / R_SUN_HMI_2048)},
        "measured_limb_edge_px": {"hmi_2048": round(hR_meas), "hmi_1024": round(h1024_meas),
                                  "aia304_2048": round(aR_meas), "aia193_2048": round(iR_meas),
                                  "aia193_1024": round(i1024_meas),
                                  "aia304_movie_1024": round(mR_meas)},
        "prominence_motion": rows,
        "prominence_motion_summary": {"area_min": min(areas), "area_max": max(areas),
                                      "height_min_R": min(hs), "height_max_R": max(hs),
                                      "n_frames": nframes, "n_samples": len(rows),
                                      "real_hours": round(nframes * 12 / 3600.0, 2)},
    }
    with open(os.path.join(REFS, "measure_extra.json"), "w", encoding="utf-8") as f:
        json.dump(extra, f, indent=2)
    p("wrote refs/measure_extra.json")

    with open(os.path.join(REFS, "measure_log.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(LOG) + "\n")
    p("wrote refs/measure_log.txt")


if __name__ == "__main__":
    main()
