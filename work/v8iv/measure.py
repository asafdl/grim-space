"""Measure per-second luma + chroma of teaser.mp4 to find real scene/section boundaries."""
import subprocess
import numpy as np
from pathlib import Path

VID = "teaser.mp4"
OUT = Path("work/v8iv/measure.txt")

# Decode at 1fps, small, keep color for chroma
r = subprocess.run(
    ["ffmpeg", "-hide_banner", "-nostats", "-i", VID,
     "-vf", "fps=1,scale=40:24,format=gbrpf32le",
     "-f", "rawvideo", "-pix_fmt", "gbrpf32le", "-"],
    capture_output=True)
data = np.frombuffer(r.stdout, dtype=np.float32)
n = len(data)
w, h = 40, 24
frm = n // (w * h * 3)
frames = data[:frm * w * h * 3].reshape(frm, h, w, 3)
print(f"decoded {frm} frames")

lines = []
for i in range(frm):
    f = frames[i]
    rch, gch, bch = f[..., 0], f[..., 1], f[..., 2]
    luma = 0.2126 * rch + 0.7152 * gch + 0.0722 * bch
    # chroma: normalized rg
    s = rch + gch + bch + 1e-6
    cr = rch / s
    cg = gch / s
    mean_luma = float(luma.mean())
    mean_cr = float(cr.mean())
    mean_cg = float(cg.mean())
    lines.append(f"{i:2d}s luma={mean_luma:.3f} cr={mean_cr:.3f} cg={mean_cg:.3f}")

OUT.write_text("\n".join(lines) + "\n")
print("\n".join(lines))
