"""Before/after strips for every boundary (option C v8iv)."""
import subprocess
from pathlib import Path

VID = "teaser.mp4"
OUT = Path("work/v8iv/strips")
OUT.mkdir(parents=True, exist_ok=True)
# all internal boundaries (section dips kept at 7.40, 16.98, 27.38; plain elsewhere)
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
    extract(b, OUT / f"pre_{name}.png", off=-0.10)
    extract(b, OUT / f"post_{name}.png", off=+0.10)
    count += 2
print(f"Created {count} strips for {len(BOUNDS)} boundaries in {OUT}")
