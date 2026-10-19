#!/usr/bin/env python3
"""Shrink the reference media in refs/ so the repository stays reasonable.

The files are re-encoded / downscaled in place; the source URLs and the
measured numbers in refs/palette.json are unaffected because they were taken
from the originals.
"""
import os
import subprocess
import sys
from PIL import Image

REFS = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))

def shrink_video(path, width=512, crf=30):
    tmp = path + '.tmp.mp4'
    subprocess.run([
        'ffmpeg', '-y', '-v', 'error', '-i', path,
        '-vf', f'scale={width}:-2', '-c:v', 'libx264', '-crf', str(crf),
        '-preset', 'slow', '-pix_fmt', 'yuv420p', '-an', tmp,
    ], check=True)
    os.replace(tmp, path)

def shrink_gif(path, frames=24, width=512):
    tmp = path + '.tmp.gif'
    subprocess.run([
        'ffmpeg', '-y', '-v', 'error', '-i', path,
        '-vf', f'fps={frames/6.0},scale={width}:-2:flags=lanczos',
        '-loop', '0', tmp,
    ], check=True)
    os.replace(tmp, path)

def shrink_png(path, max_size=1024):
    im = Image.open(path)
    if max(im.size) <= max_size:
        return False
    im = im.convert('RGB')
    im.thumbnail((max_size, max_size), Image.LANCZOS)
    im.save(path, optimize=True)
    return True

def main():
    total_before = total_after = 0
    for root, _dirs, files in os.walk(REFS):
        for name in sorted(files):
            path = os.path.join(root, name)
            before = os.path.getsize(path)
            if name.endswith('.mp4'):
                shrink_video(path)
            elif name.endswith('.gif'):
                shrink_gif(path)
            elif name.endswith('.png') and 'frames' in root:
                shrink_png(path)
            after = os.path.getsize(path)
            total_before += before
            total_after += after
            if before != after:
                print(f'{name}: {before/1e6:.1f}MB -> {after/1e6:.1f}MB')
    print(f'total {total_before/1e6:.1f}MB -> {total_after/1e6:.1f}MB')

if __name__ == '__main__':
    sys.exit(main())
