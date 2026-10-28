"""Before/after strips for every boundary (v8vii: slow fades at all boundaries).

Small PNGs (320x180) into work/v8vii/strips/; heavy copies into
/d/ops-work/trans/v8vii/strips/. One pre (-0.10s) and one post (+0.10s) per
internal boundary, plus a bottom frame at each boundary (mid-fade).
"""
import subprocess
import shutil
from pathlib import Path

VID = "teaser.mp4"
LOCAL = Path("work/v8vii/strips")
HEAVY = Path("/d/ops-work/trans/v8vii/strips")
LOCAL.mkdir(parents=True, exist_ok=True)
HEAVY.mkdir(parents=True, exist_ok=True)
# all internal boundaries
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
    # bottom frame at the boundary itself (mid-fade, near-black)
    extract(b, LOCAL / f"bottom_{name}.png", off=0.0)
    count += 3
# mirror to heavy ops area
for p in LOCAL.iterdir():
    if p.is_file():
        shutil.copy2(p, HEAVY / p.name)
print(f"Created {count} strips for {len(BOUNDS)} boundaries in {LOCAL}")
print(f"Mirrored {len(list(HEAVY.iterdir()))} files to {HEAVY}")
