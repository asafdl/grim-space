# README FIRST — Grim Space Teaser Context

> **Updated 2026-10-24 (V8.1: the transitions pass, one item at a time).** The
> V8 film re-cut against the film-geek's transition craft note (`AC-T1..AC-T7`):
> every seam graded, two fixed (the opening fade no longer reveals an empty
> grey viewport; the claim map no longer enters through a 0.012-luma black
> frame), seven world seams carried by J-cut sound bridges (leads 0.18-0.25 s),
> and the in-shot SFX re-timed to this render's own transients. The deliverable
> is `teaser.mp4` — **34.62 s, 1080p60, stereo at -14.1 LUFS** (LRA 11.7 LU,
> true peak -3.2 dBFS), 22 shots, ASL 1.57 s, no shot >3.94 s, luma floor
> 1.3 % below 0.030, **0 black-hole seams of 21** and **0 dead-time runs**.
> The cut list, the KEEP/FIX sheet, the beat table and the before/after strip
> are in `TEASER_NOTES.md` §V8.1 and `work/v8_trans/`.
>
> **The promise, in one sentence — the thing a viewer can say after watching:**
>
> > **"We are running a lost-registration survey out to the wreck along the
> > sector edge, and it is costing us a hull at a time."**
>
> The film now shows *how the game is played* from the first second and *what
> the run is about* by 2.50 s (7.2 % of the runtime, gate ≤12 %): the fight
> console — turn strip, ability cards, EXECUTE TURN — is shot 1, the claim
> board names the wreck run with the game's own `NEW` badge, and the map
> draws the route to it. The card names it again at the end — promised, not
> granted: `RECOVER WHAT WAS LOST. PAY IN HULLS.`
>
> The cut still runs as a chain, loop-first: the fight → so now the claim
> names it → and then the order goes in → but then it takes a hull → and then
> the goop gun turns the space green → so now the ship is further along the
> route → and then three hulls close in → but then the wreck is in sight →
> and then the money goes into the hull → but then their plan is drawn across
> it → and the promise.
>
> **The pointer is gone** (the user's ruling). **Every battle is the real
> `battle.tscn`**, built by the game's own engagement factory from fleets
> registered in the run's own inventory. The sun shader session is
> mid-repair in the main repo, so per the sun clause the reach slot bakes
> **no sun** — it is a named placeholder band in the beat plan. The comic
> marker-scrawl billboard is hidden for the recording: the destruction reads
> as the hull breaking up.
>
> The m-sheet evidence lives in `work/v8_sheet/v8_vs_v7_sheet.png`
> (before/after, paired with `work/geek_v7/` stills); the verifier output on
> the shipped file is `work/verify_v8.txt`; the chroma comparison is
> `work/chroma_v8.txt` (mean saturation 0.67× V4 at a same-frame basis; the
> peak remains the residual).
>
> Every claim in the write-up carries the command that produces it and the
> output that command printed on the shipped file:
>
> ```
> python scripts/verify_teaser.py teaser.mp4
> ```
>
> Full write-up — the m-sheet first, the beat table regenerated from the
> shipped file, the gate table with pasted outputs, and the honest residuals —
> is in [TEASER_NOTES.md](TEASER_NOTES.md#v8-the-film-opens-on-the-loop).
>
Earlier notes:
> The teaser **has been recorded successfully in this environment**.
> The "Godot 4.7.2 / .NET 10 compatibility issue" described below is **not** real: the
> blocker was the CLI flag. Godot's `--script/-s` cannot run a `PackedScene`; pass the scene
> as a **positional argument** instead:
>
> ```
> godot --path <repo> --write-movie <out.avi> --fixed-fps 60 --quit-after <frames> "res://scenes/teaser.tscn"
> ```
>
> The empty-gray-grid combat phase is **fixed** (root cause: the teaser camera used a
> negative pitch and overrode the shared camera rig, so it aimed at empty space).
> The sections below are the **older** Oct 14-19 notes and are kept for history.

## What this project is
**grim-space** is a 3D roguelike space game with tactical turn-based combat. This repo (`grim-space-teaser`) is focused on recording a teaser video via Godot Movie Maker and analyzing it with local vision models.

## Navigation (start here)

| File | Purpose |
|------|---------|
| `INDEX.md` / `INDEX.csv` | File index — all key source files and their roles |
| `ASSUMPTIONS.md` | Assumptions about code design + test results |
| `TEASER_NOTES.md` | Primary teaser log — old vs new videos, code fixes, Godot runtime errors, Qwen2-VL results |
| `README.md` | Full game architecture — command flow, state authority, simulation kernel, battle spec |
| `AGENTS.md` | Agent operating manual — coding conventions, design pillars |
| `ANALYSIS.md` (if created) | Frame-by-frame analysis results |

## Stupidity log

See `C:\Users\User\stupidity.log` — recorded agent mistakes from prior sessions.

## Git status

**Branch:** `teaser-recording-fix`, **rebased onto `origin/master` (65e38b93)**
**Commit:** the 16 teaser commits were replayed onto master and rewritten
(`16a831f3..9ca443ed`); the V4 pass is the commits on top of that.
**Remote:** NOT pushed to origin/master (`origin/master` still at `65e38b93`)

## Current state (summary)

| Area | Status |
|------|--------|
| Build (`dotnet build`) | ✅ 0 errors, warnings only |
| Story phase video (`teaser_story.avi`) | ✅ 10s recorded, artwork renders, text visible |
| Combat phase video (`teaser_combat10s.avi`) | ✅ 10s recorded, UI renders (Turn 1, HP/Shields, Action Log, AP/MP, ability slots, End Turn, TACTICAL COMBAT text, space background). ❌ No ships/units visible — 10s captures AI thinking phase only. |
| Combat units during replay | ✅ **ROOT CAUSE FIXED & VERIFIED** — Unit hulls were on default render layer 0, but camera only renders layers 1 (World) and 2 (Ux). Fix: Added `PresentationLayers.MarkWorld(_hull)` to all 6 `Bind*()` methods in `UnitView.cs`. Camera edge case also fixed: when `Fore`/`Dorsal` parallel, fallback to world Up and project offset perpendicular to fore. **Nemotron confirms: 4 blue unit markers visible during replay (2s frame)**. |
| **Minimal test scene** (`unit_test.tscn`) | ✅ **UNIT NOW VISIBLE** — Simple test scene with static camera at (0, 5, -20) shows unit at origin with `VisualState = Live`, hull on World layer (1), camera cull mask = 3 (World|Ux). Nemotron confirms: "Yes — there is ONE ship/unit visible" with "two bright yellow glowing lights". |
| **Overview camera** (`SetupOverviewCamera`) | ✅ **ALL 4 UNITS FRAMED** — Camera now frames all 4 units (fighter, gunship, carrier, gooper) at Center=(67,65,73), Distance=76. Nemotron confirms "4 small blue capsule/oval-shaped unit markers" visible during replay (2s frame). Static camera mode prevents replay from moving camera. |
| **Combat visualization** | ✅ **AI REPORTS ON FIGHT** — Nemotron confirms units visible during replay: "3-4 ship icons" at 2s frame, space background, asteroids, tactical combat UI. Units animate during replay (7s duration). |

## Key code fixes applied (committed to `teaser-recording-fix`, NOT pushed)

- `BattleController._agent` → `as UserExecutionAgent` (nullable), null guards in `BattleController` and `GridView`
- `PresentationFrameBuilder.BuildFrame` → nullable `UserExecutionAgent?` agent with `CaptureLiveUnits` fallback when null, null guard for `sim.Actions`
- `RefreshPresentation()` — removed early return on null `_agent`; **added `RefreshPresentation()` call inside replay loop** (`while (Resolving || Replaying)`)
- `RunAiVsAiAsync()` — removed premature `NotifyReplayComplete()` that was cutting replay short
- `TeaserController._Ready()` — calls `Session.Instance.StartNewRun()` if run is null, added `_viewportDisplay` TextureRect to show SubViewport texture
- `TeaserController.StartPhase()` — calls `GetTree().Quit()` when `TeaserPhase.Complete` reached
- `BattleController._Ready()` — **`SyncAuthoritativeUnitViewsFromWorld()` now called unconditionally** (was guarded by `if (_battle.Phase == EBattlePhase.PlayerTurn)`), so unit views are created even during AI vs AI battles when `_agent` is null
- `UnitView.Bind*()` methods (6) — **added `PresentationLayers.MarkWorld(_hull)`** so unit hulls render on camera-visible World layer (was default layer 0)
- `BattleCameraPoses.PlayerAft()` — **fixed edge case**: when `Fore` and `Dorsal` are parallel (dot > 0.99), fall back to world Up; project dorsal offset perpendicular to fore so camera is always behind ship
- Added `CameraUnitVisibilityTests.cs` (4 tests) — validates camera behind ship for all 6 cardinal orientations, pivot match, and look-at-pivot

## Disk management

**Record videos to D:\grim-space-recordings\ (1TB free)** to avoid C: drive filling up:

```powershell
# Record to D: drive
& 'C:\Users\User\AppData\Local\Microsoft\WinGet\Packages\GodotEngine.GodotEngine.Mono_Microsoft.Winget.Source_8wekyb3d8bbwe\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'C:\Users\User\dev\repos\grim-space-teaser' --scene 'res://scenes/teaser.tscn' --write-movie 'D:\grim-space-recordings\teaser_combat.avi' --quit-after 7800
```

Old C: drive videos deleted to free space (~1.8 GB recovered).

## Running long commands

**Always use `background: true`** for commands >30 seconds (Godot recording, Nemotron analysis loops, dotnet build):

```json
{
  "command": "powershell -Command \"<long command>\"",
  "background": true
}
```

Completion is signaled via a shell notification; read the `.out` file from `C:\Users\User\.local\share\opencode\shell/...` to get results.

## Next steps

1. ~~Record new combat video to D:\grim-space-recordings\ with `RefreshPresentation()` replay-loop fix applied~~ ✅ Done
2. ~~Extract frames from combat replay window (~67-74s where Turn 1 replay runs)~~ ✅ Done  
3. ~~Analyze frames with Nemotron (`opencode run -f frame.jpg "prompt"`) → confirm units visible~~ ✅ Done — **root cause found & fixed** (render layer + camera edge case)
4. Append conclusion to `TEASER_NOTES.md` when user sends `#` marker
5. ~~Optional: Record new video with all fixes to visually confirm units appear during replay~~ ✅ **Confirmed: 4 units visible during replay (2s frame)**
6. ~~Investigate why units don't appear during replay in full battle scene~~ ✅ **Fixed**: Overview camera + static mode shows all 4 units during replay
7. **Optional**: Add cinematic camera transitions (pan between units, zoom on actions)
