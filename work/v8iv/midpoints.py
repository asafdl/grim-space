"""Sample chroma at the midpoint of each shot (clean, away from dip edges) to
cluster into 4 sections and find which plain cuts need chroma matching."""
import subprocess
import numpy as np
from pathlib import Path

VID = "teaser.mp4"
BOUNDS = [0.0, 1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32, 14.17, 16.98, 20.47, 23.52, 25.98, 27.38, 30.68, 34.63]
SECTION_DIPS = {7.40, 16.98, 27.38}  # hypothesized section-change boundaries


def chroma_at(t):
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-y", "-ss", f"{t:.4f}", "-i", VID,
         "-frames:v", "1", "-vf", "scale=60:34,format=rgb24", "-f", "rawvideo",
         "-pix_fmt", "rgb24", "-"],
        capture_output=True)
    data = np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)
    if data.size == 0:
        return None
    f = data.reshape(34, 60, 3)
    rc, gc, bc = f[..., 0], f[..., 1], f[..., 2]
    luma = 0.2126 * rc + 0.7152 * gc + 0.0722 * bc
    s = rc + gc + bc + 1e-6
    return float(luma.mean()), float((rc / s).mean()), float((gc / s).mean())


shots = []
for i in range(len(BOUNDS) - 1):
    s, e = BOUNDS[i], BOUNDS[i + 1]
    mid = (s + e) / 2
    m = chroma_at(mid)
    if m is None:
        continue
    shots.append((s, e, mid, m))

lines = []
prev = None
for s, e, mid, (l, cr, cg) in shots:
    delta = ""
    if prev is not None:
        ps, pe, pmid, (pl, pcr, pcg) = prev
        dc = abs(cr - pcr) + abs(cg - pcg)
        dl = abs(l - pl)
        need = "MATCH" if dc > 0.12 else "ok"
        b_is_dip = pe in SECTION_DIPS
        lines.append(f"{ps:6.2f}-{pe:6.2f} | mid {pmid:5.2f} | L {pl:5.1f}->{l:5.1f} (dL={dl:4.1f}) Cr {pcr:.2f}->{cr:.2f} Cg {pcg:.2f}->{cg:.2f} dChroma={dc:.3f} {'DIP-BOUNDARY' if b_is_dip else need}")
    else:
        lines.append(f"{s:6.2f}-{e:6.2f} | mid {mid:5.2f} | L {l:5.1f} Cr {cr:.2f} Cg {cg:.2f}  (first shot)")
    prev = (s, e, mid, (l, cr, cg))

out = "\n".join(lines)
Path("work/v8iv/midpoints.txt").write_text(out + "\n")
print(out)
