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

## Current state (summary)

| Area | Status |
|------|--------|
| Build (`dotnet build`) | ✅ 0 errors, warnings only |
| Story phase video (`teaser_story.avi`) | ✅ 10s recorded, artwork renders, text visible |
| Combat phase video (`teaser_combat10s.avi`) | ✅ 10s recorded, UI renders (Turn 1, HP/Shields, Action Log, AP/MP, ability slots, End Turn, TACTICAL COMBAT text, space background). ❌ No ships/units visible — 10s captures AI thinking phase only (Turn 1 AI resolve takes ~67s, units animate during replay after ~110s). Need longer recording (~120s) to capture combat replay. |

## Key code fixes applied

- `BattleController._agent` → `as UserExecutionAgent` (nullable), null guards in `BattleController` and `GridView`
- `PresentationFrameBuilder.BuildFrame` → nullable `UserExecutionAgent?` agent with `CaptureLiveUnits` fallback when null, null guard for `sim.Actions`
- `RefreshPresentation()` — removed early return on null `_agent`
- `RunAiVsAiAsync()` — calls `RefreshPresentation()` after each AI turn, removed premature `NotifyReplayComplete()`
- `TeaserController._Ready()` — calls `Session.Instance.StartNewRun()` if run is null, added `_viewportDisplay` TextureRect to show SubViewport texture
- `TeaserController.StartPhase()` — calls `GetTree().Quit()` when `TeaserPhase.Complete` reached

## Disk management

**Record videos to D:\grim-space-recordings\ (1TB free)** to avoid C: drive filling up:

```powershell
# Record to D: drive
& 'C:\...\Godot_v4.7.2-stable_mono_win64_console.exe' --path 'C:\Users\User\dev\repos\grim-space-teaser' --scene 'res://scenes/teaser.tscn' --write-movie 'D:\grim-space-recordings\teaser_combat.avi' --quit-after 7800
```

Old C: drive videos were deleted to free space (~1.8 GB recovered).

```json
{
  "command": "powershell -Command \"<long command>\"",
  "background": true
}
```

## Next steps
1. Analyze `teaser_combat10s.avi` frames with Nemotron → confirm units visible in tactical combat phase
2. Append conclusion to `TEASER_NOTES.md` when user sends `#` marker
