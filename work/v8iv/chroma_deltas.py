"""Measure chroma/luma just before and after each internal boundary to find real section changes."""
import subprocess
import numpy as np

VID = "teaser.mp4"
# internal beat boundaries (excluding start fade 0.0)
BOUNDS = [1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32, 14.17, 16.98, 20.47, 23.52, 25.98, 27.38, 30.68]


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


def chroma_delta(b, eps=0.04):
    before = chroma_at(b - eps)
    after = chroma_at(b + eps)
    if before is None or after is None:
        return None
    bl, bcr, bcg = before
    al, acr, acg = after
    dc = abs(acr - bcr) + abs(acg - bcg)
    dl = abs(al - bl)
    return dl, dc, bl, al, bcr, bcg, acr, acg


lines = []
for b in BOUNDS:
    m = chroma_delta(b)
    if m is None:
        lines.append(f"{b:6.2f}: N/A")
        continue
    dl, dc, bl, al, bcr, bcg, acr, acg = m
    flag = "SECTION?" if dc > 0.15 or dl > 0.10 else "within"
    lines.append(f"{b:6.2f}: dLuma={dl:.3f} dChroma={dc:.3f} L({bl:.2f}->{al:.2f}) Cr({bcr:.2f}->{acr:.2f}) Cg({bcg:.2f}->{acg:.2f})  {flag}")

Path = __import__("pathlib").Path
out = "\n".join(lines)
Path("work/v8iv/chroma_deltas.txt").write_text(out + "\n")
print(out)
