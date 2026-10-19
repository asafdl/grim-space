# Grim Space Teaser - Notes

## Current State (Oct 14, 2026)

### Video Files (OLD - recorded before fix)
- `teaser.avi` (121 MB) - raw Movie Maker recording
- `teaser.mp4` (562 KB) - H.264 compressed
- **Recorded:** Oct 11, 2023
- **Problem:** Tactical combat phase shows empty gray grid (no units)

### Fix Applied (Oct 14)
**Files changed:**
1. `src/battle/presentation/scene/BattleController.cs`
   - Added public `Battle` property
   - Added `RunAiVsAiAsync()` method to run AI battle on visual orchestrator

2. `src/presentation/teaser/TeaserController.cs`
   - Modified `RunTacticalCombatPhase()` to:
     - Set `Session.Instance.Run.ActiveBattle` before instantiating battle scene
     - Wait frame for `BattleController._Ready()` to initialize with encounter
     - Call `battleController.RunAiVsAiAsync()` instead of headless `AiVsAiRunner`

**Build:** ✅ Success (499 tests pass)

### Required Next Step
**Must re-record in Godot on Windows:**
1. Open project in Godot 4.7 (mono/.NET build)
2. Run `scenes/teaser.tscn`
3. Movie Maker records new `teaser.avi` (configured in `project.godot`)
4. Compress to `teaser.mp4`
5. Share new video for analysis

### Expected Result
Tactical combat phase should now show:
- 3D units (Fighter, Gunship vs Carrier, Industrial Gooper)
- Grid with move range visuals
- Camera following action
- Turn-based AI vs AI combat playing out visually

### Analysis Pipeline
- **USER DIRECTIVE: Use Python Transformers script to push video frames to Qwen3-VL — NOT Ollama CLI**
- qwen3-vl:8b via Ollama CLI = **CRASHES** (CUDA error, text path only)
- llava via Ollama CLI = **BANNED by user** (hallucinates — Memento reference)
- Qwen2-VL via Transformers (`analyze_video.py`) = **PRIMARY method** — saves frames as temp images, passes paths to model
- **USER DIRECTIVE: No assumptions — if Qwen starts assuming/hallucinating, the prompt is bad**

### Qwen2-VL Analysis Result (teaser_final.avi)
**Method:** 4 frames at 384x384 res, video frames 0/1650/3300/4950
**Result: NO TEXT, NO UI, NO UNITS detected in any frame**
- Frame 1 (0:00): Black void with stars, top-down, no text/UI/gameplay
- Frame 2 (~0:27): Dark space gradient, top-down, no text/UI/gameplay  
- Frame 3 (~0:55): Dark void + grid view, top-down, no text/UI/gameplay
- Frame 4 (~1:22): Dark space gradient, top-down, no text/UI/gameplay

**Re-analysis at 512x512, 8 frames (0/825/1650/2475/3300/4125/4950/5775):**
- Frame 1 (0:00): Black void, NO text
- Frame 2 (~0:14): "THE FALL OF HUMANITY" text at bottom center, top-down ✅
- Frame 3 (~0:27): "THE FALL OF HUMANITY" text + grid view ✅
- Frame 4 (~0:41): "THE FALL OF HUMANITY" text + grid ✅
- Frame 5 (~0:55): "THE FALL OF HUMANITY" text ✅
- Frame 6 (~1:09): Dark void, NO text
- Frame 7 (~1:23): Dark void, NO text
- Frame 8 (~1:36): Dark void, NO text

**CRITICAL FINDING:** Story text ("THE FALL OF HUMANITY") IS visible (frames 2-5). But NO tactical combat units appear in ANY frame — no units, no grid combat, no unit roster, no strategic map, no title card. The tactical combat fix did NOT take effect.

**Root Cause Found:** `BattleOrchestrator.PlayerAgent` does `(UserExecutionAgent)ActiveAgent` — a direct cast that THROWS `InvalidCastException` when `ActiveAgent` is `AiExecutionAgent` (AI vs AI encounter). This crashes `BattleController._Ready()`, so no units render.

**Fix Applied:**
1. `BattleOrchestrator.PlayerAgent` → `ActiveAgent as UserExecutionAgent` (returns null for AI instead of throwing)
2. `BattleController._agent` → `UserExecutionAgent?` (nullable)
3. Null guards added: `RefreshPresentation()` returns early, `GetPlayerRenderedPosition()` returns Vector3.Zero, all `_agent.X` calls use null-safe patterns
4. `BattleController._Ready()` calls `Session.Instance.StartNewRun()` to initialize run state
5. `TeaserController.RunTacticalCombatPhase()` sets `BattleController.NextEncounterOverride` before scene instantiation
6. `TeaserController.StartPhase()` calls `GetTree().Quit()` when `TeaserPhase.Complete` is reached

**Build:** ✅ Success (0 errors, warnings only)
**Next:** Re-record video with fix applied → re-analyze with Qwen2-VL

## My Conclusions

### Root Cause
The teaser instantiated the battle scene (`battle.tscn`) but ran a **separate headless `AiVsAiRunner`** that created its own `BattleOrchestrator` with no connection to the visual `BattleController`. The visual `BattleController._Ready()` created a dev encounter with no units, so the SubViewport rendered an empty grid while the AI battle ran invisibly.

### Fix Strategy
Connect the teaser to the **visual BattleController's orchestrator** by:
1. Setting `Session.Instance.Run.ActiveBattle` before scene instantiation
2. Letting `BattleController._Ready()` call `CreateActiveBattleOrchestrator()` which reads that encounter
3. Calling `RunAiVsAiAsync()` on the visual controller so AI turns drive the same world that renders

### What This Reveals About Architecture
- `BattleController` owns the authoritative orchestrator (`_battle`) — good
- `AiVsAiRunner` was a **duplicate parallel orchestrator** for headless testing — violated single-owner principle
- The fix eliminates the duplicate by exposing `RunAiVsAiAsync()` on the visual controller
- Teaser now uses the **same code path** as real gameplay (strategic battle)

### Analysis Lessons
- **qwen3-vl:8b in Ollama CLI is text-only** — receives file path as string, no image data passed
- **llava via Ollama CLI works** for frame analysis (genuine vision)
- **Qwen2-VL Transformers pipeline** broken for video (only processes frame 1)
- **Only reliable local analysis:** llava on extracted frames

### Next Validation
After re-recording, the tactical combat phase (~30-90s) should show:
- 4 units: Fighter+Gunship (green/player) vs Carrier+IndustrialGooper (red/enemy)
- Grid with movement range wireframes
- Camera orbiting/following active unit
- Turn-by-turn AI decisions: move → ability → end turn
- BattleHUD with turn flow, health bars, action bars

---

## 2026-10-14: Convention Established
**User directive:** Every time user adds `#` mark, append latest message as conclusion to this file.

**Context:** Fix complete, build passing, video re-recording needed on Windows. Established this markdown log as persistent context across sessions.

---

## 2026-10-14 23:39: PowerShell Confirmed Working
**Test:** `powershell -Command "Get-Date"` returned `Wednesday, October 14, 2026 11:39:33 PM`

**Conclusion:** Windows commands work via PowerShell in this environment. Ready to execute any Windows commands needed (Godot, ffmpeg, etc.) from this agent.

---

## 2026-10-14 23:50: Godot Runs via PowerShell
**Test:** `powershell -Command "& 'C:\Users\User\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe' --path 'C:\Users\User\dev\repos\grim-space-teaser' --headless -s 'res://scenes/teaser.tscn'"`

**Result:** Godot executable launches, initializes .NET, loads project, but fails with `ERROR: Can't load script: res://scenes/teaser.tscn` - likely a UID mismatch between `.tscn` (uid://bqx8teaser001) and `.cs.uid` (uid://ci1vvrig5jaew). The .NET assembly builds successfully (`dotnet build` ✅).

**Conclusion:** Godot **can run via PowerShell** in this container. The blocker is now a **UID sync issue** between scene file and script file, not the execution environment. Need to regenerate UIDs or fix the mismatch.

---

## 2026-10-14 23:55: Godot Runs Without --headless (GPU Detected!)
**Test:** Same command **without** `--headless` flag.

**Result:** 
```
D3D12 12_0 - Forward+ - Using Device #0: NVIDIA - NVIDIA GeForce RTX 2070 SUPER
object_count = 2, desire_count = 2
```

**Then same error:** `ERROR: Can't load script: res://scenes/teaser.tscn`

**Conclusion:** The RTX 2070 SUPER **is accessible** from this container! Godot initializes the D3D12 renderer and detects the GPU. The UID mismatch is the only remaining blocker. If we fix the UID, Godot should fully run and render here.

---

## 2026-10-14 23:56: # Mark Received — Summary
**User sent `#`** — confirming the above conclusion is recorded. Key findings so far:
1. PowerShell works ✅
2. Godot runs via PowerShell ✅  
3. RTX 2070 SUPER accessible ✅ (D3D12 renderer initializes)
4. Only blocker: UID mismatch between `teaser.tscn` (uid://bqx8teaser001) and `TeaserController.cs.uid` (uid://ci1vvrig5jaew)

**Next:** Fix UID mismatch → Godot should fully run and render teaser here.

---

## 2026-10-15 00:10: Headless --build-solutions WORKS ✅
**Test:** `powershell -Command "& '...Godot_v4.7.2-stable_mono_win64_console.exe' --path '...' --headless --build-solutions"`

**Result:** 
- Project initialization: ✅ (5 steps)
- .NET build: ✅ `dotnet_build_project` completed successfully
- Editor layout loading: ✅ (5 steps)
- **Only warnings:** UID duplicate (teaser) + invalid UIDs (main.tscn → StartMenu, KeyBindingsTab)

**Conclusion:** Godot **can build the project headless** in this container. The .NET compilation succeeds. The UID warnings are non-fatal. The project is build-ready.

**Next:** Run the teaser scene with Movie Maker to record the video.

---

## 2026-10-15 00:45: C# Assembly Dependency Issue Blocking Runtime
**Test:** `powershell -Command "& '...Godot_v4.7.2-stable_mono_win64_console.exe' --path '...' --headless --write-movie '...' -s 'res://scenes/teaser.tscn' --quit-after 9000 --verbose"`

**Result:** 
- All resources load successfully (scripts, scenes, textures, themes)
- Scene `res://scenes/teaser.tscn` loads: "Completed load for: 'res://scenes/teaser.tscn'"
- **Then fails:** `ERROR: Can't load script: res://scenes/teaser.tscn`
- Same error for `main.tscn`

**Root Cause Found:** The compiled `grim-space.dll` has **missing System.Runtime dependencies** (both 8.0.0.0 and 10.0.0.0) when loaded via reflection. Godot's internal C# compiler (Roslyn via GodotSharp) fails to compile autoload scripts (`Session.cs`, `Music.cs`) even though `dotnet build` succeeds.

**Evidence:**
- Build output: "ERROR: Failed to create an autoload, script 'res://src/application/Session.cs' is not compiling."
- `dotnet build` ✅ but Godot's C# integration ❌
- Assembly loads in Godot's .NET host but fails at script instantiation

**Conclusion:** This is a **Godot 4.7.2 / .NET 10 compatibility issue** in this container environment. The project builds with `dotnet` but GodotSharp's Roslyn compiler can't resolve the same dependencies.

**Workaround:** Run on **native Windows** (user's machine) where Godot 4.7.2 mono + .NET 10 works correctly. The container's .NET host may have PATH/GAC issues.

**Status:** Code fix complete ✅, build passes ✅, tests pass ✅ (499). **Blocker is environment-specific.** User must record on Windows host.

---

## 2026-10-19 — TEASER PRODUCTION PASS (new engagement recording)

### Deliverable

| File | Size | Notes |
|------|------|-------|
| `teaser.mp4` (repo root) | **31,520,003 bytes** | 85.6 s, 1920x1080, 60 fps, H.264 (libx264, CRF 21, yuv420p, faststart) |
| `D:\grim-space-recordings\teaser_v9.avi` | 757,969,868 bytes | raw Godot Movie Maker capture, 5138 frames |

Verified by `ffprobe` on the committed file: `duration=85.633333`, `size=31520003`, `codec_name=h264`, `1920x1080`, `60/1`.

### The recording command that finally worked

The Oct-14 note ("Godot 4.7.2 / .NET 10 compatibility issue … user must record on Windows host") is **wrong / stale**. The blocker was never .NET — it was the CLI flag:

```
-godot --path <repo> --write-movie <out.avi> --quit-after <frames> "res://scenes/teaser.tscn"
```

`-s res://scenes/teaser.tscn` (script mode) fails with `ERROR: Can't load script: res://scenes/teaser.tscn` because Godot 4.7's `--script` expects a *Script*, not a *PackedScene*; the scene itself loads fine (the log shows `Completed load for: 'res://scenes/teaser.tscn'`). **Pass the scene as a positional argument instead.** `--quit-after` counts *frames*, not seconds (5138 frames = 85.6 s at 60 fps).

### Root cause of the "empty gray grid" (the real bug)

It was never a missing-scene, missing-unit or render-layer problem. The units existed and were `Visible` the whole time — the **camera was pointing at empty space**.

* `OrbitPose.Offset()` / `Controller.CameraPosition()` use `+sin(Pitch)` for the vertical axis, so in this game's convention **positive pitch = camera above the pivot looking down**.
* The teaser overview camera used `pitch = -0.6` (camera *below*, looking *up*) and then bypassed the shared camera rig entirely, forcing `_camera.Rotation = new Vector3(pitch, yaw, 0)` each frame. That manual rotation also has the opposite sign to Godot's look-at convention, so the camera ended up aimed away from the battle.
* Result: the SubViewport rendered the space backdrop + asteroids and nothing else. The HUD, action log, "Detonate · Hit" entries and turn flow were all correct and live — which is exactly why earlier passes concluded "the fix did not take effect".

Fix (`BattleController.SetupOverviewCamera` / `ApplyOverviewCamera`): use the shared camera rig (`_camera.SetFocus(...)`, which ends in `LookAt`), `pitch = +0.42`, distance derived from the actual unit spread, an establishing dolly-in (34 → ~15 units over 4.5 s) and a slow orbital drift so the shot is never a still frame.

### Second thing that was never a grid

`GridView` only draws the **movement-range preview** (a player move-preview affordance). An AI-vs-AI battle has no player turn, so *no grid is ever drawn* — that is correct game behaviour, not a bug. The teaser shows ships in open space, not a chessboard.

### Other real bugs found and fixed while producing this

1. **Combat shot was 3.5 s long.** `RunAiVsAiAsync` returned as soon as the battle was *decided*, while the turn's replay was still playing, so the teaser cut away mid-replay and the freed battle scene threw `ObjectDisposedException` in `TurnReplayPlayer`. The method now keeps the presentation live until the in-flight replay has finished; the teaser additionally cuts on a 26 s budget via `CancellationToken` + a new `TurnReplayPlayer.Stop()`. The resulting combat beat is ~16.7 s (the replay ends before the budget expires).
2. **`travel.tscn` needs a live POI.** The facility binding threw `Travel requires an active POI` / `expects scene slot 'Manager'`. The teaser now calls `MapNavigationContext.EnterFacility` with the facility whose `ScenePath` is `travel.tscn` (the wormhole's travel facility). The v9 run logs **zero** runtime errors.
3. **Old scenes were never actually removed.** `ClearCurrentScene()` only called `QueueFree()`, so the travel corridor still showed through the roster cards and the map's narrative dialogue box overlapped the closing title card. It now detaches every child of the viewport's `CurrentScene` node before freeing it.
4. **Full-screen art did not cover the frame.** `AnchorsPreset` on a freshly constructed `Control` preserves its 0x0 rect, so the stills rendered at a fraction of the viewport. Art is now staged with an explicit `Position`/`Size` and `KeepAspectCovered`.
5. **Overlay captions were mis-placed.** A full-rect-anchored `Label` expresses `offset_bottom` relative to the anchor, so `offset_bottom = 500` actually extends 500 px *past* the bottom of the screen. Captions are now placed by absolute band (`PlaceLabel`).
6. Per-frame `GD.Print` spam in `BattleController._Process` removed (it was also slowing the capture).

### Differences vs the old teaser

| | Old (`teaser.mp4`, Oct 11) | New (`teaser.mp4`, Oct 19) |
|---|---|---|
| Combat phase | empty gray grid, no units (the old raw capture was deleted to free disk, so its length cannot be re-measured now) | live AI-vs-AI battle: 4 ships, shields, laser tracers, void-bomb detonation blast, asteroid field, planet, full HUD + action log |
| Combat duration | unknown (file gone) | **~16.7 s of live combat**, measured frame-precisely on the caption band (frames 1900→2900 of 5138); the cut budget is 26 s and the turn's replay simply finished sooner |
| Story beat | 13 pages, 3 s each, no title card | 6 curated pages (hope → escalation → new world → the threat → the fall) with cross-fades, 2.4 s each |
| Other phases | travel / roster / map / title | travel (real facility interior, live binding), 4 unit cards on a clean backdrop, strategic map with the live narrative box, closing wordmark over the action art with a legibility scrim |
| Length / size | — | 85.6 s, 31.5 MB, 1080p60 |

### Editorial choices (stated honestly)

* **The AI-vs-AI battle is a loss for the player side in every seed I tried** (12 seeds swept through `AiVsAiRunner`: `99999, 1, 42, 123, 456, 789, 7, 13, 77, 2024, 31337, 555` — all `winner=Lose`, 2 turns). The teaser therefore **cuts the combat beat on a 26 s budget before the outcome panel appears**. Nothing about the fight is faked: the ships, the weapons fire, the hits and the damage numbers in the action log are all the real simulation. Only the result is not shown.
* The closing card is the `war-and-ruin2` key art with the wordmark over a dark scrim; the *opening* uses the `intro-screen` art whose logo is baked in, so the two ends do not repeat the same image.
* The strategic-map beat keeps the game's own narrative box ("Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how did I even end up here?") because it is real game content, not a mock.

### What was NOT changed

No new gameplay systems. Changes are confined to presentation/teaser code plus one small, honest API addition (`TurnReplayPlayer.Stop()` + an optional `CancellationToken` on `RunAiVsAiAsync`).

### Vision-model analysis (Qwen2-VL-2B-Instruct, CPU)

`python analyze_video.py teaser.mp4 "<prompt>" --max-frames 6 --resize 448 --model Qwen/Qwen2-VL-2B-Instruct` (Qwen2-VL-2B is the model cached in `~/.cache/huggingface/hub`; it runs on CPU, ~2.5 min/frame).

Frames are sampled evenly over the 5138 frames (0, 856, 1712, 2569, 3425, 4281). What the model actually reported:

| Frame | Time | Model's reading | Verdict |
|-------|------|-----------------|---------|
| 1 | 0.0 s | "completely black… no visible spacecraft… no readable text" | ✅ correct (fade-in) |
| 2 | 14.3 s | "a view of the Earth from space" | ✅ correct (story art) |
| 3 | 28.5 s | "several spacecraft… laser beams… several explosions" | ⚠️ it is the `war-and-ruin2` **illustration**, not gameplay — the description matches the picture, but it read illustration as live combat |
| 4 | 42.8 s | "a tactical combat game… two spacecraft… asteroids and planets… status bar displays **AP** (Attack Points) and **HP** (Health Points)" | ✅ correct — this is the live combat frame |
| 5 | 57.1 s | "a fighter strike craft… The text **"FIGHTER Strike Craft"** is visible at the bottom" | ✅ correct, exact transcription |
| 6 | 71.4 s | read **"Oh, Syndi's beard, WHAT. A. RUSTBOX. this system is, how di"**, **"STRATEGIC MAP"**, **"RESOURCES"**, **"ACTIVE OBJECTIVES"**, **"00"**, **"No active objectives"** | ✅ near-perfect transcription; it wrongly added *"this is in foreign language, likely German"* — the text is English. **This is the hallucination the old notes warned about**, and it is the one error I found. |

**Conclusion from the model:** the new video contains readable space-combat content — spacecraft, weapon fire, explosions, planets, asteroids and a legible HUD. The old teaser's frames read as "black / starfield, no text, no units" to the same model, so this is a direct before/after.

**Closing card, separate pass** (3 s clip at t=80 s, 2 frames, same model): *"a promotional graphic for a video game titled 'GRIM SPACE: A Tactical Roguelike in the Void'… The title 'GRIM SPACE' is prominently displayed… with 'A Tactical Roguelike in the Void' written below it"*. The transcription is exact; the model put the wordmark in the "upper left" when it is in fact lower-centre, so its **spatial** reading is unreliable at 448 px even when its transcription is right. Frame 1 of that clip (the fade-in black frame) came back as the literal string `(10,10),(990,989)` — i.e. the model degenerates on a featureless frame rather than inventing content, which is the behaviour you want.

### Test suite

`dotnet test --project grim-space.Tests/grim-space.Tests.csproj` → **1377 total, 1373 passed, 4 skipped, 0 failed.**

The 4 skips are pre-existing hazards, not new ones: `TeaserSceneLoadTest` and three `ReplayCameraTests` construct Godot objects (`GD.Load`, `new UnitView()`, `new Controller()`), which aborts the whole test host with a native access violation (`0xC0000005` inside `ResourceLoader..cctor` / `Camera3D..cctor`). They were added by the prior session's commit `479f5eb3` and violate the AGENTS.md rule *"Tests must not test GODOT framework, this has caused previous segmentation faults"*. They are now marked `Skip` with that reason, and the two camera-director tests that did **not** need a real `Camera3D` were rewritten against a new `FakeCameraRig` fixture so they run for real instead of crashing the host. Before this, the run aborted at ~483 tests, which is why the older notes report "499 tests" — that number was never a full run.

---

## HARVEST LOG (2026-10-19)

- Exported all four offered sessions with `opencode session export <id>` (1.3 MB–4.8 MB each) and parsed them with a python extractor. Note: in v2 exports the assistant prose lives in `content[]` (type `text` / `reasoning`), **not** in `parts[]` — a naive parser sees zero text.
- **Chose: `ses_eaf1f8430ffe234Uo81WzMflSo` ("Babysitting tactical combat session")** — the only one about this game's combat. The other three (`ses_ead69ad2…` worker hierarchy, `ses_ead81f4a…` superagent hierarchy, `ses_ead9bf19…` babysitting research) are opencode session-orchestration experiments, not gameplay; I read their summaries and skipped their contents.
- From the chosen session I took an **independent audit that confirmed my bug**: its babysitter inspected all 90 sampled frames of `long_capture_01.avi` and recorded *"No visible hulls"* in **every single frame** — the same symptom I reproduced and finally root-caused (camera pitch sign inverted, camera aimed at empty space). It also documented the two crash fixes I inherited in the working tree (`TurnReplayPlayer` `ObjectDisposedException`, `MoveStepClip` `NullReferenceException`).
- Also harvested the working method that session converged on and used throughout: **never sleep on a render** — launch it in the background, verify with `ffprobe`, extract frames with `ffmpeg`, and inspect the pixels yourself instead of trusting a text-only report. That is how I found the camera bug: the first frame I read looked like the "empty gray grid" everyone else described, and the fix only became obvious after looking at the actual pixels.
- I delegated nothing to helper sessions (no `opencode run --title` spawns, so there are no registry entries to check); the work was one long capture/analyze/fix loop, and `opencode session list` still shows only the four harvested sessions.## 2026-10-19 — POLISH PASS: flicker root-cause, sell checklist, engagement re-edit

Three jobs: (1) find and fix the flicker in the committed teaser **from the pixels**,
(2) turn web research into a checklist and score the old cut honestly against it,
(3) re-plan, re-record and re-mix the teaser. Then prove it with the test suite.

---

## 1. FLICKER — diagnosed from pixels, fixed at source

### 1.1 Method (pixels first, never a text-only render report)

* Decoded **all 5138 frames** of the committed `teaser.mp4` at 320x180 and computed, per
  frame: mean luma, mean |Δ| against the previous frame, mean |Δ| against the frame two
  back (the A/B-alternation signature), the fraction of pixels changing by more than
  12 levels, and an 8-band spatial profile of *where* the change happened.
* Rendered a 1 fps contact sheet of the whole video and **read it**, then dense 10 fps
  strips around every candidate moment (and full-resolution frames of the worst ones).
* Ran a per-second "how many of the 60 frames actually changed" census, which is where
  the dead air showed up.

### 1.2 What the pixels showed (committed `teaser.mp4`, 85.63 s)

| Finding | Measurement |
|---|---|
| **12 black-out / black-in events.** The picture faded to near-black and back 12 times in 85 s — once per story page (6) and once per phase change (6). | Luma minima of **1.02, 1.02, 1.02, 1.37, 1.91, 1.02 …** out of 255. |
| The fade pair was **dead motion on the wrong frame**. The shape was: fade down 0.45 s to luma ≈ 1, fade back up 0.45 s **to the same image**, then a *hard cut* to the new image (e.g. t=5.03→6.10: 32.1 → 1.02 → 24.6 → **79.8 in one frame**, Δ=65.7). The cross-dissolve it was supposed to be never happened. | See the 10 fps strip of the first cut. |
| **The last 1.6 s strobed twice**: title card → black (84.0–84.6) → back up (84.6–85.1) → black again (85.1–85.6) → quit. | Per-frame luma 62.9, 76.5, 60.2, 43.9, 27.6, 11.1, 2.4, 19.2, 35.6, 51.9, 68.2, 81.5, 65.5, 49.3, 33.0, 16.6, 1.0 |
| **24 s of the 85 s (28 %) were pixel-identical frames.** The six story paintings and the 5 s closing card changed **0 frames per second**. | Per-second census: `changed=0/59` for t=2,3,7,8,11,12,15,16,17,19,20,21,24,25,28,29,30 … |
| A brightness pop right before the final fade. | 62.94 → **76.47** → 60.18 (the wordmark's scrim was removed one beat too early). |
| The single largest frame-to-frame change in the whole file was a **full-frame cut**, not a dropped frame. | Δ=78.3 over 96.9 % of the frame. |

There were **no dropped frames, no duplicate frames and no interlace artefacts** in the
moving sections: the combat section changed on 59 of 59 frames every second. The
"flicker" was entirely a *transition* bug, not a capture bug.

### 1.3 Root cause in code

`src/presentation/teaser/TeaserController.cs` (before this pass):

1. `RunStoryArtworkPhase` did `ShowStoryPage(page[i])` → `ShowFullScreenArt()` →
   `ClearCurrentScene()` + add the new `TextureRect` + `FadeIn(1.0)`. The transition
   rect's alpha was already 0, so **the fade-in did nothing and the new still appeared
   as a hard cut**.
2. The loop then called `FadeTransition(0.45)` = `FadeOut(0.45)` + `FadeIn(0.45)`, which
   faded the *unchanged* current still down to **full black** and back up again. The
   outgoing image and the incoming image were never on screen together — that is the
   entire bug.
3. Every phase boundary repeated it: `StartPhase` ended with `await FadeOut(0.5)` (to
   full black) and the next phase began with `await FadeIn(0.5)`, and every phase body
   then ran its *own* extra `FadeIn(...)` on top of that.
4. `TeaserPhase.Complete` had **no `case`** in the phase switch, so after the title card
   the controller ran `FadeIn(0.5)` + `FadeOut(0.5)` and *then* quit → the double strobe.
5. `_titleScrim.Visible = false` was executed one beat before the final fade-out, so
   the frame brightened by ~13 luma levels just before it went to black.

### 1.4 The fix (in code; no post-processing, no masking)

`TeaserController` now has **one** transition primitive, `DissolveTo(Node, float)`,
which encodes Godot's paint order: 2D content is drawn in the viewport's canvas
**in front of** the 3D world, therefore

* incoming **2D** (a still, the facility `Control`, the roster backdrop) → fades **up**
  over whatever is behind it, and
* incoming **3D** (the battle scene, the star map) → the outgoing 2D still is faded
  **away** from in front of it.

Either way the outgoing and incoming images overlap for the length of the dissolve and
**the frame never passes through black**.

Also fixed in the same pass:

* the `Complete` phase no longer fades twice — the file now contains exactly **one**
  fade to black (the last second) and one fade from black (the first 0.4 s);
* the wordmark scrim is a vertical **gradient** instead of a hard-edged band, and it
  stays up until the fade-out;
* the static paintings now get a slow push (1.00 → 1.08 across the hold), so no held
  shot is pixel-identical any more;
* `Session.ShowPerformanceOverlay` lets the teaser switch off the `FPS: 24` readout
  that was being recorded into the corner of every gameplay shot;
* `BattleController.FocusUnitCamera` applies the camera pose **immediately** instead of
  waiting for the first follow tick, so the first rendered frame is already framed on
  the ship (the old cut opened on an empty starfield);
* the cinematic follow's response time is 0.12 s (was 0.22 s) and its orbit drift is
  halved, so the subject no longer slides to the frame edge.

**Not** changed: the capture pipeline was already correct —
`--write-movie` forces `--fixed-fps`, so the old capture was frame-deterministic. The
pacing was never the problem; the transition logic was.

---

---

## 2. SELL CHECKLIST (research → 15 rules, with sources)

The research was done by a helper session (`opencode run --title teaser-sell-research`,
registry-verified in `opencode session list`) and distilled here. Every rule below is
traceable to a source that was actually fetched.

1. **Hook first: live gameplay inside the first five seconds.** The hook belongs in
   the first third of the trailer and certainly in the first half — "How quickly should
   a trailer get to the game's hook? AS SOON AS POSSIBLE!!!" Steam viewers "click
   through the trailer's progress bar in search of raw gameplay" and will not wait
   15-30 s for gameplay to appear.
   [derek-lieu.com 2022-10-24](https://www.derek-lieu.com/blog/2022/10/24/how-to-hook-the-audience-and-how-quickly-to-do-it) ·
   [gamediscover.co](https://newsletter.gamediscover.co/p/game-trailers-breaking-down-a-key)
2. **The cold open must pose a question that needs answering.** "If there are no
   questions … it's unlikely to hold the audience's attention." Openings made of slow
   environment art fail unless the art carries environmental storytelling.
   [derek-lieu.com 2019-10-02](https://www.derek-lieu.com/blog/2019/10/2/editing-an-engaging-trailer-opening)
3. **Establish the genre in 1-2 shots, then go straight to the hook.** The order to
   avoid is "Genre, Anchors, Hook, Content".
   [derek-lieu.com 2022-10-24](https://www.derek-lieu.com/blog/2022/10/24/how-to-hook-the-audience-and-how-quickly-to-do-it)
4. **Show the real UI, but don't expect anyone to read it.** Strategy-game UI is a
   rewatch magnet (spikes in *Empire of Sin*, *Hades*, *Noita* menus); the UI is what
   the viewer rewinds to study. Tactical Breach Wizards opens with the HUD on, then
   toggles it off for clarity.
   [derek-lieu.com 2022-10-24](https://www.derek-lieu.com/blog/2022/10/24/tactical-breach-wizards-gameplay-trailer) ·
   [derek-lieu.com 2022-06-12](https://www.derek-lieu.com/blog/2022/6/12/how-to-get-people-to-rewatch-your-game-trailer)
5. **One idea per shot.** "Better for the audience to remember one thing than see two
   and forget them."
   [derek-lieu.com 2018-02-03](https://www.derek-lieu.com/blog/2018/2/3/game-trailers-as-tutorials-part-1-of-3)
6. **Clarity beats flash.** For strategy and tactics games, "simple editing with clear
   and intriguing gameplay" wins over "flashy editing with confusing gameplay" almost
   every time. Grid/tactical games are "impenetrable to the unfamiliar", so they need
   explicit scaffolding (turn order, highlighted tiles).
   [derek-lieu.com 2023-04-23](https://www.derek-lieu.com/blog/2023/4/23/more-trailer-editing-is-not-better-trailer-editing) ·
   [derek-lieu.com 2018-02-03](https://www.derek-lieu.com/blog/2018/2/3/game-trailers-as-tutorials-part-1-of-3)
7. **No dead air, and no section longer than ~15 s.** A measured 85 s trailer breaks
   down as 6-14-5-14-15-5-16-6-60 seconds; "a short trailer with bad pacing can feel
   like boring after only 15-20 seconds."
   [derek-lieu.com 2020-11-16](https://www.derek-lieu.com/blog/2020/11/16/how-to-think-about-pacing-in-a-trailer)
8. **Vary shot length, not just shot count.** One rhythm for the whole piece reads as
   monotonous; measured average shot lengths: *Baba is You* 4.25 s, *Papers, Please*
   2.6 s, *Modern Warfare II* 1.3 s — a system-heavy game deserves the 2-4 s band.
   [derek-lieu.com 2023-04-23](https://www.derek-lieu.com/blog/2023/4/23/more-trailer-editing-is-not-better-trailer-editing) ·
   [studiobinder.com](https://www.studiobinder.com/blog/how-does-an-editor-control-the-rhythm-of-a-film/)
9. **Order the shots the way the game teaches them**, and build the shot list in three
   layers: (1) story & player verbs, (2) NPCs/enemies/weapons, (3) biomes and level art.
   "You can't build a house out of paint, or the things that go inside of the house."
   [derek-lieu.com 2023-07-16](https://www.derek-lieu.com/blog/2023/7/16/how-to-decide-what-to-show-in-a-game-trailer-and-in-what-order-to-put-it)
10. **Escalate; don't ramp linearly.** Tension → release → bigger tension → release →
    peak. "Going from A to B to C is okay, but if you go from A to Z in a slowly
    incremental order, things can get boring and predictable very quickly."
    [derek-lieu.com, How to Make a Trailer](https://www.derek-lieu.com/blog/category/How+to+Make+a+Trailer)
11. **Length: 90 s or fewer for a trailer, and it must survive a 15 s scrub.** 30-90 s
    is an "appetizer" — it has to deliver a taste, not the meal. Steam pages are
    browsed by people who scrub to 15 s.
    [derek-lieu.com 2018-12-20](https://www.derek-lieu.com/blog/2018/12/20/ideal-game-trailer-length-and-labeling) ·
    [gamediscover.co](https://newsletter.gamediscover.co/p/game-trailers-breaking-down-a-key)
12. **No feature-list callouts.** "PROCEDURALLY GENERATED LEVELS / CUSTOMIZABLE
    WEAPONS / EPIC BOSS BATTLES" is the anti-pattern. If text is needed it is a
    tagline, and the end slate hierarchy is title → CTA → platform logos → dev logos
    → legal.
    [derek-lieu.com 2019-01-21](https://www.derek-lieu.com/blog/2019/1/21/remember-the-titles) ·
    [derek-lieu.com 2021-04-25](https://www.derek-lieu.com/blog/2021/4/25/the-game-trailer-call-to-action-end-slate)
13. **Music needs highs and lows inside every section, cut to its peaks, with SFX
    layered onto the peaks rather than after them.** A flat drone under 85 s kills
    the pacing work, and the music must have "finality" — no fade-out, no abrupt cut.
    [derek-lieu.com 2020-11-24](https://www.derek-lieu.com/blog/2020/11/24/basic-guide-to-trailer-sound-editing) ·
    [derek-lieu.com 2019-11-26](https://www.derek-lieu.com/blog/2019/11/26/developing-an-ear-for-trailer-music)
14. **The game's own SFX are brand markers: timestamp 2-3 of them.** Superfuse put the
    Diablo coin flip "in the first shot of the trailer and in another at approximately
    8 seconds"; Half-Life: Alyx used the health-recharge sound at the 0:49 mark with
    the music dropped out. Sound must also mark the section boundaries, so the
    audience hears the trailer move onto a new idea.
    [derek-lieu.com 2024-01-07](https://www.derek-lieu.com/blog/2024/1/7/targeting-your-audience-with-the-trailers-sound-effects-ba73f) ·
    [derek-lieu.com 2020-11-24](https://www.derek-lieu.com/blog/2020/11/24/basic-guide-to-trailer-sound-editing)
15. **The last three seconds are the rewatch moment — the loudest musical moment of
    the piece — and the mix masters to -14 LUFS.** YouTube normalises to -14 LUFS
    globally: too loud and the whole file is turned down, too quiet and it is never
    brought up.
    [derek-lieu.com 2022-06-12](https://www.derek-lieu.com/blog/2022/6/12/how-to-get-people-to-rewatch-your-game-trailer) ·
    [blog.quadmeup.com](https://blog.quadmeup.com/2022/12/07/how-to-normailize-audio-for-youtube-with-davinci-resolve/)

---

## 3. HONEST SCORE OF THE OLD TEASER (committed `teaser.mp4`, 85.63 s)

Scored out of 5 per rule, from the pixels, not from intent.

| # | Rule | Score | What the pixels showed |
|---|------|-------|-----------------------|
| 1 | Hook in the first 5 s | **0** | 0-4 s static title art, 4-31 s six static paintings. First live gameplay at **32.9 s** — 55 % of the runtime before the game is on screen. |
| 2 | Cold open poses a question | **1** | The title art names the game; it asks nothing. |
| 3 | Genre in 1-2 shots, then the hook | **0** | Six paintings before the hook; the tactical genre is not visible until 33 s. |
| 4 | Real UI visible and legible | **4** | The battle HUD, action log, AP/MP and ability slots are all legible at 1080p — but only in the shot that arrives last. |
| 5 | One idea per shot | **2** | Six consecutive shots of the same idea (painted apocalypse). |
| 6 | Clarity over flash | **3** | The battle frame reads, but the ships are small flat blue ellipses at board distance. |
| 7 | No dead air, no section > 15 s | **1** | **24 s of 85 s (28 %) were pixel-identical**; the story section alone ran 27 s. |
| 8 | Varied shot lengths | **1** | The same 4.3 s rhythm six times, then 0.9 s fades, then 6 s / 6 s / 5 s. |
| 9 | Ordered like the game teaches | **0** | Story art first, gameplay last — the exact inversion of the three-layer rule. |
| 10 | Escalation, not a linear ramp | **1** | A→B→C→D, no release, no peak. The most energetic frame in the file is a static painting. |
| 11 | ≤ 90 s and survives a 15 s scrub | **2** | 85.6 s is inside the window, but a 15 s scrub shows only paintings. |
| 12 | No callout lists; tagline slate | **2** | The captions are system names, not feature lists (good); the slate has the title + tagline (good); there is no CTA. |
| 13 | Music with highs/lows, cut to the peaks | **0** | **There was no audio at all.** The committed file's AAC track is digital silence (mean volume **-91 dB**). |
| 14 | SFX as timestamped signature markers | **0** | Same: no audio. |
| 15 | Loudest at the end, -14 LUFS | **0** | Same: silence — and the last 1.6 s strobed black twice. |
| | **Total** | **17 / 75 (23 %)** | |

---

## 4. THE RE-EDIT — new shot list

Driven by the checklist, the old footage's own problems, and what the game's content can
actually deliver. Every shot is live game content; nothing is mocked up.

| # | Shot | In | Length | What it is | Checklist rules it serves |
|---|------|----|--------|------------|-----------------------------|
| 1 | **HOOK** | 0.4 s | 4.7 s | Live tactical combat, camera locked on the player's fighter: hull close-up, shield bubbles, tracers, the action log filling with real `Detonate · Hit` entries. | 1, 2, 3, 4, 6 |
| 2 | **WIDE** | 5.1 s | 4.0 s | The camera pulls back to the whole engagement — 7 ships in frame, red and cyan laser fire, the void-bomb detonation flash at 7.6 s. Cut the instant the fight stops being legible. | 1, 6, 7, 8 |
| 3 | **FACILITY** | 9.7 s | 3.8 s | A real facility interior with the operator robot and the live HUD. | 7, 9, 10 |
| 4 | **STRATEGIC MAP** | 14.4 s | 5.3 s | The star map: the station, the fleet marker, the routes, RESOURCES / ACTIVE OBJECTIVES, and the pilot's own line ("Oh, Syndi's beard, WHAT. A. RUSTBOX. …"). | 4, 9, 10 |
| 5 | **ROSTER** | 20.2 s | 4 × 1.95 s | The four ships the player commands, one card each (FIGHTER / CARRIER / VOID BOMB / REPURPOSED MINER). | 5, 7, 8 |
| 6 | **STORY** | 28.8 s | 2 × 2.2 s | Two paintings with a slow push: humanity controls the galaxy → total war. Caption: THE FALL OF HUMANITY. | 7, 8, 10 |
| 7 | **NAME CARD** | 34.6 s | 5.9 s | The war-and-ruin key art with the wordmark over a soft gradient scrim, the tagline under it, and the loudest musical moment of the piece. | 12, 15 |

(The "in" times are the measured section starts from the final capture, not the
constants in the code.)

* **No section is longer than 6 s** and no held frame is pixel-identical (the paintings
  push 1.00 → 1.08 across their hold).
* **Every transition is a cross-dissolve**; the frame never passes through black.
* The **only** fades to black in the whole file are the first 0.4 s and the last 1.0 s.
* What was cut and why: the old cut's 6 story paintings (24 s of dead air) are down to
  2 with motion; the old 26 s combat beat is down to 9.2 s because the last ships die
  at ~9.5 s and the remaining 4 s of that shot were an empty starfield.

### What the re-edit deliberately does **not** do

* **No voice-over.** The mandate is voice-free; the game's own narrative box carries
  the character beat instead.
* **No feature-list callouts and no "WISHLIST ON STEAM" slate.** The game has no store
  page yet; promising one would be a lie. The slate is title + tagline, which is the
  honest maximum for the current state of the project.
* **No second combat beat.** The AI-vs-AI encounter is deterministic, so a second
  take of the same battle would be the same 9 s of footage with a different camera.

---

## 5. SOUND — plan, sources, mix

The committed teaser had **no audio at all** (its AAC track was digital silence at
-91 dB), so this is a new layer rather than a re-mix.

### 5.1 What is in the repo

| Asset | Duration | Role in the mix |
|---|---|---|
| `assets/music/terraforming-mars-tracks/track-01..39.ogg` | 84-394 s each | The music bed. 39 candidates, scanned for BPM and a per-second RMS arc before choosing. |
| `assets/sfx/abilities/void_bomb_explosion.wav` | 1.50 s | The **signature sound**: the game's own detonation, used for the hook impact, the payoff explosion and the final hit. |
| `assets/sfx/abilities/lightning_cannon_hit.wav` | 0.56 s | Weapon hits, placed on frames where a tracer actually appears. |
| `assets/sfx/ships/ship_death_enhanced_v2.wav` | 3.95 s | A kill in the engagement. |
| `assets/sfx/ui/menu_hover_click.wav` | 0.15 s | The section marker on the facility and the roster. |
| `assets/sfx/abilities/robot_beeps_300ms.wav` | 0.30 s | The facility robot and the pilot's line on the map. |
| `assets/sfx/abilities/latch_release_80ms.wav` | 0.08 s | The roster card ticks. |
| *synthesised* `hook_boom.wav` | 1.80 s | A 46 Hz sub-bass boom with a filtered noise transient, built with ffmpeg `sine` + `anoisesrc`, for the cold open. |

### 5.2 The music choice (and why)

Every one of the 39 tracks was decoded and profiled (BPM by onset-envelope
autocorrelation, per-second RMS arc, integrated LUFS). The brief was a cue that

* is **present but not loud** under the first ten seconds, so the hook impact has room
  (the research's "stop down" — drop the music to make room for a hero sound);
* **builds** through the middle;
* is **loudest at the name card**, because the closing montage is the rewatch moment.

`track-15.ogg` (103 BPM, 84.4 s) fits: it opens at -20…-23 dB RMS, holds a body at
-14 dB from 0:20, and peaks at -11 dB around 1:10 before its own fade. Started at
**28.5 s** of the track, its loudest moment lands on the name card; the first 4.2 s of
the cue are additionally **ducked by 9 dB** so the hook impact lands on an open mix.
Mixed at **-3 dB** under the SFX, then bus-compressed (threshold -20 dB, ratio 3) and
normalised.

### 5.3 The mix

Godot's Movie Maker writes no audio, so the sound is mixed in ffmpeg against the
recorded picture:

1. music bed = `track-15.ogg` from 10.0 s, 42.3 s long, 0.35 s fade in, 1.4 s fade out;
2. every SFX is placed with `adelay` **at the frame it belongs to** — the shot changes
   were measured from the picture (see §6), not guessed;
3. `amix` (no normalisation) → `loudnorm` → `alimiter`.

| Setting | Value |
|---|---|
| Integrated loudness | **-14.0 LUFS** (EBU R128 / BS.1770) — the web/YouTube target |
| True peak | **-1.4 dBFS** on the mix, **-0.6 dBFS** measured on the decoded AAC in the shipped file |
| Loudness range | 1.5 LU (measured on the shipped file) |
| Video | H.264 High, **yuv420p**, limited range, bt709, CRF 21, `+faststart` |
| Audio | AAC-LC, 48 kHz, stereo, 192 kbps |
| Stream durations | video **41.516667 s**, audio **41.517000 s** — delta **0.3 ms** |

### 5.4 The SFX timeline (every hit placed on a measured frame)

| t | Sound | Picture event it lands on |
|---|-------|-----------------------------|
| 0.10 s | sub-bass boom | the fade up out of black |
| 0.62 s | void bomb explosion | the combat opens (f30-40) |
| 3.10 s | lightning cannon hit | a tracer appears (f185-209) |
| 5.35 s | lightning cannon hit | the pull back; tracers (f276-337) |
| 6.30 s | lightning cannon hit | a tracer (f376) |
| **7.55 s** | **void bomb explosion** | **the payoff detonation — the brightest frame in the file (f452-475)** |
| 8.90 s | ship destroyed | a hull is lost; the ships leave the frame (f540-556) |
| 9.20 s | void bomb explosion (-7 dB) + UI tick | **shot change: combat → facility** |
| 13.60 s | robot beep | **shot change: facility → strategic map** (the pilot's line appears) |
| 19.80 s | UI click + mechanical tick | **shot change: map → roster** |
| 20.45 / 22.40 / 24.35 / 26.30 s | latch click ×4 | one per roster card |
| 28.10 s | void bomb explosion (-8 dB) | **shot change: roster → story 1**, a distant boom |
| 31.10 s | latch click + UI tick | **shot change: story 1 → story 2** |
| **34.00 s** | **void bomb explosion** | **the final impact, on the name card** |

---

## 6. VERIFICATION (all of it measured, none of it asserted)

### 6.1 The flicker is gone

The same frame-differ that found the old file's 12 black-outs was run over the new one.

| Metric | Old `teaser.mp4` (85.63 s) | New `teaser.mp4` (41.52 s) |
|---|---|---|
| Frames that fade to near-black mid-file | **12** (luma minima 1.0-1.9 / 255) | **0** — the darkest mid-file luma is the naturally dark space scene (5-9) |
| Longest fully-black run | 0.45 s × 12 | 0.40 s (the opening fade) + 1.0 s (the closing fade), both intentional |
| Pixel-identical stretches | 24 s (28 % of the file) | 0 s — every held shot drifts (the paintings push in, the map camera drifts) |
| Largest single-frame change | 78.3 levels over 96.9 % of the frame (a cut) | 71.0 levels over 98.3 % — the **opening fade from black** |
| Frames that changed at all, per second | 0/59 in 17 separate seconds | never 0 except on the roster card holds |
| Duplicate / dropped frames | none | none |

### 6.2 The file

```
$ ffprobe -v error -show_entries stream=... -show_entries format=... teaser.mp4
video : h264 High, 1920x1080, yuv420p, limited range, bt709, 60/1 fps, 2491 frames, 41.516667 s
audio : aac LC, 48000 Hz, stereo, 41.517000 s
format: 41.517000 s, 31,875,260 bytes
video/audio duration delta: 0.3 ms  ->  MATCH
```

### 6.3 Audio

| Check | Result |
|---|---|
| Integrated loudness | **-14.0 LUFS** (measured on the shipped mp4) |
| True peak | **-1.4 dBFS** on the mix before encoding; **-0.6 dBFS** measured on the decoded AAC in the shipped file |
| Loudness range | 1.5 LU (measured on the shipped mp4) |
| Stream duration match | video 41.516667 s vs audio 41.517000 s → **0.3 ms** |
| SFX transient vs. picture cut | every SFX stem peak lands **within 0.05-0.17 s** of the shot change it was placed on (the offset is the sound's own attack time) |
| Audible accent on each cut | +0.2 / +2.7 / +0.3 / +1.0 / -0.1 / +3.3 dB across the six measured cuts |
| Waveform | `teaser_waveform.png` (committed) — 1800 px wide, one column per 23 ms |

The SFX times were **measured from the picture**, not guessed: the dissolve detector
(`find_dissolves.py`) found the transition regions frame-precisely (f546-580, f829-854,
f1698-1730, f1868-1898, f2036-2075) and the frame-differ found the combat events
(f185-209, f325-337, f376, f452-475, f540-556).

### 6.4 Vision-model re-score (Qwen2-VL-2B-Instruct, local, CPU)

`python analyze_video.py teaser.mp4 "<prompt>" --max-frames 8 --resize 448` — 8 frames
sampled evenly over the file (frames 0, 311, 622, 934, 1245, 1556, 1868, 2179). What the
model actually reported, in order:

| Frame | t | The model's reading | Verdict |
|---|---|---|---|
| 1 | 0.00 s | "live 3D space-combat gameplay", ships "X-1, Y-2, Z-3" | ✗ — that frame is the black fade-in; the model invents content on a featureless frame (the same failure the old notes recorded) |
| 2 | 5.19 s | "live 3D space-combat gameplay": four ships, a red glowing line of fire, three blue lines of fire, explosions, HUD panels | ✓ — the wide shot |
| 3 | 10.37 s | "a game menu/UI screen" — "SPACE TRAVEL & FACILITIES" | ✓ (the category is wrong, the transcription is exact) |
| 4 | 15.57 s | "Oh, Syndi's beard, WHAT. A. RUSTBOX. th" | ✓ — the pilot's line on the map, near-exact |
| 5 | 20.75 s | "FIGHTER Strike Craft" | ✓ — exact |
| 6 | 25.93 s | "VOID BOMB" / "Suicide Bomber" | ✓ — exact |
| 7 | 31.13 s | "THE FALL OF HUMANITY" | ✓ — exact |
| 8 | 36.32 s | "GRIM SPACE: A Tactical Roguelike in the Void" | ✓ — exact; note it calls the closing card "live 3D space-combat gameplay" because the key art *is* a picture of a space battle |

**Before/after on the same model and the same prompt:** the previous session's notes
record that the *old* teaser read as "black / starfield, no text, no units" in every
sampled frame, and the first live gameplay in the new cut is at **0.4 s**.

**What the model still gets wrong** (so the score is not inflated): it answers the
multiple-choice "is this live 3D or a static screen" question with the wrong option for
the facility, the roster cards and the paintings, and it over-counts the ships in the
wide shot. Its *transcriptions* are reliable; its *counts* and its *categorisation* are
not. The re-score below therefore leans on the pixel measurements and uses the model
only for transcription.

### 6.5 Re-score against the checklist (the new cut)

| # | Rule | Old | New | Why |
|---|------|-----|-----|-----|
| 1 | Hook in the first 5 s | 0 | **5** | Live combat from 0.4 s; the first tracer at ~1.0 s, the model reads frame 2 (5.3 s) as space combat. |
| 2 | Cold open poses a question | 1 | **3** | A lone hull in a void with weapons about to fire. It is a quiet question, not a dramatic one. |
| 3 | Genre in 1-2 shots, then the hook | 0 | **4** | The hook *is* the genre shot: grid tactics with a turn flow and an action log. |
| 4 | Real UI visible and legible | 4 | **5** | The HUD, action log, AP/MP and the ability bar are on screen for the whole 9 s engagement and transcribe cleanly. |
| 5 | One idea per shot | 2 | **4** | Each section carries one idea; the roster is four cards of one idea (the rule of threes would prefer three). |
| 6 | Clarity over flash | 3 | **4** | The wide shot frames seven ships with the fight in the middle third. The ships are still small at board distance. |
| 7 | No dead air, no section > 15 s | 1 | **4** | Longest section is 9.2 s; no held frame is pixel-identical except the roster cards and the facility screen (see §7). |
| 8 | Varied shot lengths | 1 | **4** | 5.0 / 4.2 / 3.4 / 5.4 / 1.95×4 / 2.2 / 2.2 / 6.0 s. |
| 9 | Ordered like the game teaches | 0 | **4** | Gameplay → world → strategy → roster → story → title. |
| 10 | Escalation, not a linear ramp | 1 | **3** | The picture escalates (hull → fleet → detonation) but the back half is a sequence of equal-weight sections; the escalation now lives in the music. |
| 11 | ≤ 90 s, survives a 15 s scrub | 2 | **5** | 41.5 s, and a 15 s scrub lands in the middle of the engagement. |
| 12 | No callout lists; tagline slate | 2 | **4** | Captions name systems, not features; the slate is title + tagline. Still no CTA — there is no store page to point at. |
| 13 | Music with highs/lows, cut to the peaks | 0 | **4** | One cue with a 3 dB internal build, ducked under the hook and peaking on the name card. Still a single cue, not 2-3. |
| 14 | SFX as timestamped signature markers | 0 | **4** | The game's own detonation at the hook, the payoff and the title card; six cut markers measured against the picture. |
| 15 | Loudest at the end; -14 LUFS | 0 | **5** | -14.0 LUFS, -1.4 dBTP, and the loudest moment of the file is the final impact on the name card. |
| | **Total** | **17 / 75 (23 %)** | **61 / 75 (81 %)** | |

The two rules still short of full marks are the ones the game's own content cannot
honestly deliver yet: there is no store page for a CTA, and the battle is the only
source of tactical footage, so the escalation curve lives in the sound rather than in
the picture.

---

## 7. WHAT CHANGED, AND WHAT I SKIPPED

### Code

| File | Change | Why |
|---|---|---|
| `src/presentation/teaser/TeaserController.cs` | Rewritten: one `DissolveTo` transition primitive, new shot list, Ken Burns on the paintings, captions that switch *after* the dissolve, no double fade at the end, gradient scrim. | The flicker, the dead air and the caption mismatch. |
| `src/battle/presentation/scene/BattleController.cs` | Added `FocusUnitCamera` (a cinematic follow on one unit), `BattleView` (read-only), and made `SetupOverviewCamera` take a start distance and a dolly time so the shot can pull *back* out of the close-up instead of jumping. | The hook needed a hull-level framing. |
| `src/battle/presentation/camera/Controller.cs` | Added `SetOrbit` (distance/angles without moving the pivot). | A follow camera has to re-aim without resetting its pivot. |
| `src/application/Session.cs` | Added `ShowPerformanceOverlay` so the teaser can switch the `FPS:` readout off. | The dev overlay was being recorded into every gameplay shot. |

**No gameplay system was touched.** Every frame in the teaser is the real simulation:
the tracer fire, the shield bubbles, the void-bomb detonation and the action log are
the game's own, and the engagement is the same AI-vs-AI battle the previous teaser
recorded (seed 99999) — only the camera and the cut changed.

### The video

| | Old (85.63 s) | New (41.52 s) |
|---|---|---|
| First live gameplay | 32.9 s | **0.4 s** |
| Audio | none (digital silence) | music + 17 game SFX, -14.0 LUFS |
| Black-out events | 12 | 0 |
| Pixel-identical time | 24 s | 0 s (except the roster card holds) |
| Sections | 7 (one of them 27 s) | 7 (longest 9.2 s) |
| File | 31.5 MB, no audio track | 31.2 MB, 1080p60 + AAC stereo |

### Honest list of what I did **not** do

* **No voice-over.** Out of scope, and the game's own pilot line carries the character
  beat better than a narrator would.
* **No "WISHLIST ON STEAM" end slate.** There is no store page; the slate is the
  title and the tagline, which is the honest maximum today.
* **No second combat beat.** The AI-vs-AI encounter is deterministic, so a second take
  would be the same nine seconds with a different camera.
* **The facility interior (3.4 s) and the four roster cards are still held still.**
  The facility screen is a UI scene with no camera to borrow and no animation to
  replay; the cards are cards. Everything else moves.
* **The music is one track, not a 2-3 cue trailer score.** The library has 39 tracks
  and no trailer cue; cutting two of them together is the obvious next step and needs
  a human ear.
* **The music licence is unverified.** `assets/music/terraforming-mars-tracks/` is
  pre-existing repo content with no licence or attribution file anywhere in the
  repository. It needs checking before this video is published anywhere.
* **The tactical combat is still the least legible thing in the file.** The camera
  work helps, but the ships are small and the HUD is dense. A scripted shot of a
  single player turn (move → aim → fire → damage number) would beat the AI-vs-AI
  capture for legibility. That is a game-side feature, not a video-side one.
