"""Sample each beat's interior to cluster shots into sections by chroma/luma."""
import subprocess
import numpy as np
from pathlib import Path

VID = "teaser.mp4"
# beat intervals from beat_table_v8ii (boundaries)
BEATS = [
    (0.00, 1.88, "FIGHT OPENS"),
    (1.88, 3.90, "CLAIM"),
    (3.90, 7.40, "LOOP"),
    (7.40, 9.68, "COST"),
    (9.68, 13.32, "WRECK/PLAN"),
    (13.32, 14.63, "GOOP FIELD"),
    (14.63, 17.00, "PROGRESS"),
    (17.00, 20.50, "THE MAP"),
    (20.50, 23.52, "ESCALATION"),
    (23.52, 25.98, "ODD NUMBERS"),
    (25.98, 27.40, "REACH/DOCK"),
    (27.40, 30.70, "THREAT"),
    (30.70, 34.63, "CARD"),
]

def sample(t):
    # decode 1 frame at time t, small color
    r = subprocess.run(
        ["ffmpeg", "-hide_banner", "-nostats", "-y", "-ss", f"{t:.3f}", "-i", VID,
         "-frames:v", "1", "-vf", "scale=60:34,format=rgb24", "-f", "rawvideo",
         "-pix_fmt", "rgb24", "-"],
        capture_output=True)
    data = np.frombuffer(r.stdout, dtype=np.uint8).astype(np.float64)
    if data.size == 0:
        return None
    f = data.reshape(34, 60, 3)
    rch, gch, bch = f[..., 0], f[..., 1], f[..., 2]
    luma = 0.2126 * rch + 0.7152 * gch + 0.0722 * bch
    s = rch + gch + bch + 1e-6
    return float(luma.mean()), float((rch/s).mean()), float((gch/s).mean())

lines = []
prev = None
for s, e, name in BEATS:
    mid = (s + e) / 2
    m = sample(mid)
    if m is None:
        continue
    l, cr, cg = m
    delta = ""
    if prev is not None:
        pl, pcr, pcg = prev
        dc = abs(cr - pcr) + abs(cg - pcg)
        dl = abs(l - pl)
        delta = f"  dLuma={dl:.3f} dChroma={dc:.3f}"
    lines.append(f"{mid:5.2f}s [{name:12s}] luma={l:.3f} cr={cr:.3f} cg={cg:.3f}{delta}")
    prev = (l, cr, cg)

Path("work/v8iv/sections.txt").write_text("\n".join(lines) + "\n")
print("\n".join(lines))
