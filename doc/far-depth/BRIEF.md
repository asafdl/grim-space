# FAR-DEPTH: the black hole + far backgrounds must read FAR AWAY or 3D
USER VERDICT (raw, 2026-10-24): "make the black hole and all the far backgrounds, look either
far away or 3d - as of now it looks weird". Isolated task: this folder is its own worktree of
grim-space on branch graphic/far-depth; NOTHING else in the game may be touched.

PROBLEM SPACE (find, do not assume): black hole (if/where it exists in the starmap/deep-space
view) + far backgrounds (star field, nebulae, dust, any parallax layers). Suspected weirdness:
flat paper-cutout look = either (a) single unlayered texture ignoring camera parallax, or
(b) 2D sprites drawn at wrong camera-relative depth, or (c) no depth fog/atmospheric envelope,
or (d) incompatible sense of scale (near and far objects same size conventions).

MANDATE (evidence-first, matches repo AGENTS.md):
1. INVENTORY: find every far-background layer + the black hole in the presentation code
   (MapCelestialVisuals / MapView / shaders/assets) - name paths, one-line description of how
   each is drawn (2D texture? billboard? world mesh? Node3D z?).
2. BEFORE renders: 2-3 stills from the real boot flow (main.tscn path) holding the weird look,
   pasted commands + paths (use the BaseRenderBoot/--write-movie pattern; golden frames).
3. CHOOSE the depth solution per layer (may differ per object): true-3D placement vs parallax
   layer depth vs depth-fog+scale envelope. JUSTIFY in one line per layer.
4. IMPLEMENT in small commits, presentation-only (no new state, no parallel systems,
   MapCelestialVisuals/MapView ownership respected).
5. AFTER renders at the SAME poses + a camera-move proof (the parallax/depth response must
   show in motion: 2 frames at 2 camera distances - layers must separate correctly).
   Acceptance: side-by-side before/after + the camera-move strip = "reads far away or 3D",
   the weirdness gone. Black hole: verify it reads as DEEP suspension (accretion
   perspective, background lensing hint if cheap) not a 2D decal.
6. dotnet build + render pipeline + tests rules per AGENTS.md. Commit + push branch
   graphic/far-depth. Plain-words notes for the non-gamedev reviewer.
FINAL: FAR-DEPTH-DONE - <layers fixed + commit> or FINAL: BLOCKED - reason. No questions.
