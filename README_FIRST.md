# README FIRST — Grim Space Teaser Context

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

**Branch:** `teaser-recording-fix` (created from `master` after local commit `479f5eb3`)
**Commit:** `479f5eb3` — "fix: battle presentation null guards + teaser viewport display + replay refresh" (23 files, 1621 insertions, 49 deletions)
**Remote:** NOT pushed to origin/master (`origin/master` still at `3693dece`)

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
