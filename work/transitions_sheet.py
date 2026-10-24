"""Contact sheet: every transition row that changed, two frames each side.

    python work/transitions_sheet.py before.mp4 after.mp4 out.png t1 t2 ...

Each row is [label | before -2f | before +2f | after -2f | after +2f] at the
same timecode, so a fix is graded from the same two frames on both files.
"""
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

W, H = 480, 270
FPS = 60.0


def frames(path: str) -> np.ndarray:
    raw = subprocess.run(
        ["ffmpeg", "-v", "error", "-i", path, "-vf", f"scale={W}:{H},format=rgb24",
         "-f", "rawvideo", "-"], capture_output=True, check=True).stdout
    return np.frombuffer(raw, dtype=np.uint8).reshape(-1, H, W, 3)


def main() -> None:
    before, after, out = sys.argv[1], sys.argv[2], sys.argv[3]
    times = [float(t) for t in sys.argv[4:]]
    a = frames(before)
    b = frames(after)
    pad, label_h = 6, 22
    cols = 5
    sheet = Image.new("RGB", (cols * W + (cols + 1) * pad,
                              len(times) * (H + label_h + pad) + pad), (18, 18, 20))
    draw = ImageDraw.Draw(sheet)
    for row, t in enumerate(times):
        i = int(round(t * FPS))
        y = pad + row * (H + label_h + pad)
        for col, (img, idx) in enumerate(((a, i - 2), (a, i + 2), (b, i - 2), (b, i + 2))):
            idx = max(0, min(len(img) - 1, idx))
            x = pad + (col + 1) * (W + pad) - W
            sheet.paste(Image.fromarray(img[idx]), (x, y + label_h))
        draw.text((pad + 2, y + 4), f"t={t:.2f}s   BEFORE -2f / +2f            "
                                    f"AFTER -2f / +2f", fill=(235, 235, 235))
    Path(out).parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out)
    print(f"wrote {out} ({sheet.width}x{sheet.height}) rows {len(times)}")


if __name__ == "__main__":
    main()
