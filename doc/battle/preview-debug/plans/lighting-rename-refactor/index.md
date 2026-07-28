# Plan: lighting-rename-refactor

| | |
|--|--|
| **Full plan** | [full.plan.md](full.plan.md) — goals, architecture, changed files |
| **Changed files** | [full.plan.md § Changed files](full.plan.md#changed-files) |
| **Parent (Cursor)** | `%USERPROFILE%\.cursor\plans\lighting_rename_refactor_c056e9dc.plan.md` |
| **Branch** | `feature/simple-preview` |
| **Edit root** | `side-branches/preview-debug-friendly/side-branches/simple-preview` |
| **Created** | 2026-07-26 |

Each **subplan** is self-contained: why the file matters, current behavior, step-by-step changes, edge cases, and verification.

## Changed files (links)

| Source | Subplan | Todos | Depends |
|--------|---------|-------|---------|
| [`Lighting.cs`](../../../../../src/battle/presentation/graphics/Lighting.cs) *(replaces RedDwarfSun.cs)* | [subplan](subplans/src-battle-presentation-graphics-Lighting.cs.plan.md) | lighting-file | — |
| [`SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplan](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | space-backdrop | Lighting.cs |
| [`BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplan](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | controller | Lighting, SpaceBackdrop |

## Suggested implementation order

1. **[Lighting.cs](../../../../../src/battle/presentation/graphics/Lighting.cs)** — new file; delete [RedDwarfSun.cs](../../../../../src/battle/presentation/graphics/RedDwarfSun.cs).
2. **[SpaceBackdrop.cs](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs)** — `BuildCinematic` + `Lighting.AddSun`.
3. **[BattleController.cs](../../../../../src/battle/presentation/scene/BattleController.cs)** — `ConfigureDiagram` with diagram backdrop.
4. **Verify** — `dotnet build`; Godot F5 diagram + optional cinematic toggle check ([full.plan.md § Verification](full.plan.md#verification-full)).

## Subplans

| File | Subplan | Todos |
|------|---------|-------|
| [`src/battle/presentation/graphics/Lighting.cs`](../../../../../src/battle/presentation/graphics/Lighting.cs) | [subplans/src-battle-presentation-graphics-Lighting.cs.plan.md](subplans/src-battle-presentation-graphics-Lighting.cs.plan.md) | lighting-file |
| [`src/battle/presentation/graphics/SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | space-backdrop |
| [`src/battle/presentation/scene/BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplans/src-battle-presentation-scene-BattleController.cs.plan.md](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | controller |

## Todo rollup

| Id | Summary | Status |
|----|---------|--------|
| lighting-file | Lighting.cs + remove RedDwarfSun | pending |
| space-backdrop | BuildCinematic + AddSun | pending |
| controller | ConfigureDiagram wiring | pending |
| verify | build + Godot smoke | pending |
