# COVENANT — SUN-VERIFY (independent visual + numeric review of the sun shader)

You are the **independent reviewer** for the grim-space sun-shader work.  The
shader author (another session) has already rendered and tuned.  Your job is
to check the result against the real NASA/SDO reference frames and to say
honestly whether it "looks like the real sun".  You did not write the shader
and you must not change it.

## Working directory

`C:\Users\User\dev\repos\grim-space`  (bash: `/c/Users/User/dev/repos/grim-space`)

## Inputs that already exist

| Path | What it is |
|------|------------|
| `refs/` | real NASA/SDO + SOHO reference material, `refs/SOURCES.md` (exact URLs), `refs/palette.json` (measured numbers), `refs/analysis.md` (the author's analysis) |
| `refs/frames/` | extracted reference frames (AIA 304/171/094/193, HMI, LASCO C2, eclipse composite) |
| `D:\grim-space-recordings\sbs_304.png` | **side by side**: real AIA 304 frame (left) vs shader render in the 304 palette (right) |
| `D:\grim-space-recordings\sbs_193.png` | side by side: HMI 193 (left) vs shader render in the visible-light palette (right) |
| `D:\grim-space-recordings\p304.png` | the shader render in the 304 palette, full frame |
| `D:\grim-space-recordings\pvl.png` | the shader render in the visible-light palette, full frame |
| `D:\grim-space-recordings\sun_p304.avi`, `sun_pvl.avi` | the same two renders as movies |

Reference material that the comparison was built from:
- `C:\Users\User\AppData\Local\Temp\opencode\sunref\frames\aia304_03.png` — real
  SDO/AIA 304 frame, 2024-05-08 13:28 UT, from the Wikimedia Commons public
  domain copy of "SDO 20240508 1024 0304 (AIA)".
- `C:\Users\User\AppData\Local\Temp\opencode\sunref\latest_1024_0193.jpg` — real
  SDO HMI 193 frame.

## EXACT deliverables (write ONLY these paths)

1. `task/sun/sun-verify-report.md` — your review. It MUST contain:
   - **Verdict**: one of `LOOKS LIKE THE REAL SUN`, `CREDIBLE BUT NOT CONVINCING`,
     `DOES NOT LOOK LIKE THE REAL SUN` — plus a one-paragraph justification.
   - **A table of the specific differences you can see**, each marked
     `BLOCKER` / `NOTABLE` / `MINOR`.  Be concrete ("the granulation cells are
     about 3x too large compared with the reference", "the limb darkens
     smoothly instead of ending in a sharp edge like the reference").
   - **Numbers**: independently measured from the images with Python
     (numpy + PIL are installed).  At minimum: the disc's mean/centre luminance,
     the radial brightness profile at 0.8/0.9/0.95/1.0 R for BOTH images, the
     granulation cell size in pixels and cells across the disc for BOTH images,
     the brightness just outside the limb, and the prominence height above the
     limb if any are visible.  Report the numbers side by side.
   - **What is right**: list the things that genuinely match, so the author does
     not "fix" them.
   - **The single highest-leverage change** you would make next.
   - **An explicit note on whether the prominence loops in the render resemble
     the real prominences in `refs/frames/`** (open and look at the real
     prominence frames, e.g. anything with `prom` in the name, and the LASCO /
     EIT frames).  This matters: the whole point of the shader is the
     prominences.

2. `task/sun/sun-verify-frames/` — optional extra frame grabs you made to
   support the report (at most 6 files).

## How to work

- Use `python` (numpy, PIL, cv2 available) for the numbers.  Write throwaway
  scripts under `C:\Users\User\AppData\Local\Temp\opencode\sunref\`, NOT in the
  repository.
- For the "by eye" judgement you must actually LOOK at the images.  You can
  read image files directly with your image-reading tool.  Do not describe an
  image you have not opened.
- Pay attention to: granulation scale and contrast, whether the disc reads as a
  flat disc or as a ball, the limb, the prominences, the corona, and the
  overall colour.
- The render is sRGB encoded, same as the NASA JPEGs, so the numbers are
  directly comparable.

## Constraints (verbatim)

- Do NOT modify, create or delete anything under `src/`, `assets/`, `scenes/`,
  `refs/`, or `project.godot`.  You are a reviewer; the only files you may
  write are the two paths listed above.
- Do NOT edit the shaders, do NOT "fix" anything, do NOT run git.
- Do NOT ask the harness questions - the harness does not answer.  If something
  is missing, write it in the report and continue.
- Do NOT claim a number you did not measure, and do NOT claim to have seen an
  image you did not open.  Fabrication is a hard fail.
- Be harsh but fair.  The point of this review is to catch anything a
  self-review would rationalise away.

## Budget

- ≤ 30 tool actions.
- ≤ 20 minutes of wall clock.

Record your final line as exactly `FINAL: DONE` or `FINAL: BLOCKED - reason`.
