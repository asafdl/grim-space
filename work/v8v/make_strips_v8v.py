"""Before/after strips for every boundary (v8v: slow dark dips, gray floors banned).

Small PNGs (320x180) into work/v8v/strips/; heavy copies into
/d/ops-work/trans/v8v/strips/. One pre (-0.10s) and one post (+0.10s) per
internal boundary, plus a bottom frame at each section dip.
"""
import subprocess
from pathlib import Path

VID = "teaser.mp4"
LOCAL = Path("work/v8v/strips")
HEAVY = Path("/d/ops-work/trans/v8v/strips")
LOCAL.mkdir(parents=True, exist_ok=True)
HEAVY.mkdir(parents=True, exist_ok=True)
# all internal boundaries (section dips at 7.40, 16.98, 27.38; plain elsewhere)
BOUNDS = [1.92, 3.90, 4.72, 5.65, 7.40, 8.03, 10.30, 12.10, 13.32, 14.17,
          16.98, 20.47, 23.52, 25.98, 27.38, 30.68]
W, H = 320, 180


def extract(t, outpath, off=0.0):
    tt = max(0.0, t + off)
    cmd = ["ffmpeg", "-hide_banner", "-nostats", "-y",
           "-ss", f"{tt:.4f}", "-i", VID, "-frames:v", "1",
           "-vf", f"scale={W}:{H}", str(outpath)]
    subprocess.run(cmd, capture_output=True, check=True)


count = 0
for b in BOUNDS:
    name = f"{b:.2f}"
    extract(b, LOCAL / f"pre_{name}.png", off=-0.10)
    extract(b, LOCAL / f"post_{name}.png", off=+0.10)
    # bottom frame at the dip boundary itself (section dips)
    if b in (7.40, 16.98, 27.38):
        extract(b, LOCAL / f"bottom_{name}.png", off=0.0)
    count += 2
    if b in (7.40, 16.98, 27.38):
        count += 1
# mirror to heavy ops area
import shutil
for p in LOCAL.iterdir():
    if p.is_file():
        shutil.copy2(p, HEAVY / p.name)
print(f"Created {count} strips for {len(BOUNDS)} boundaries in {LOCAL}")
print(f"Mirrored {len(list(HEAVY.iterdir()))} files to {HEAVY}")
