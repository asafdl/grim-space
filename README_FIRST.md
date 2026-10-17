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
| Combat units during replay | ❌ Still not visible during replay phase — `RefreshPresentation()` added to replay loop but units still not rendering in sampled frames. Need to record new video with this fix and re-analyze. |

## Key code fixes applied (committed to `teaser-recording-fix`, NOT pushed)

- `BattleController._agent` → `as UserExecutionAgent` (nullable), null guards in `BattleController` and `GridView`
- `PresentationFrameBuilder.BuildFrame` → nullable `UserExecutionAgent?` agent with `CaptureLiveUnits` fallback when null, null guard for `sim.Actions`
- `RefreshPresentation()` — removed early return on null `_agent`; **added `RefreshPresentation()` call inside replay loop** (`while (Resolving || Replaying)`)
- `RunAiVsAiAsync()` — removed premature `NotifyReplayComplete()` that was cutting replay short
- `TeaserController._Ready()` — calls `Session.Instance.StartNewRun()` if run is null, added `_viewportDisplay` TextureRect to show SubViewport texture
- `TeaserController.StartPhase()` — calls `GetTree().Quit()` when `TeaserPhase.Complete` reached

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

1. Record new combat video to D:\grim-space-recordings\ with `RefreshPresentation()` replay-loop fix applied
2. Extract frames from combat replay window (~67-74s where Turn 1 replay runs)
3. Analyze frames with Nemotron (`opencode run -f frame.jpg "prompt"`) → confirm units visible
4. Append conclusion to `TEASER_NOTES.md` when user sends `#` marker
