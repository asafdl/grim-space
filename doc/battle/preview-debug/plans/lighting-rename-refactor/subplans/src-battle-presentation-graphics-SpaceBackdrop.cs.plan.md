---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/graphics/SpaceBackdrop.cs
todoIds: [space-backdrop]
dependsOn: [src/battle/presentation/graphics/Lighting.cs]
status: pending
---

# Subplan: `SpaceBackdrop.cs`

**Source:** [`src/battle/presentation/graphics/SpaceBackdrop.cs`](../../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

`SpaceBackdrop` composes the 3D **environment** around the grid: `WorldEnvironment`, nebula, starfield, and (cinematic only) sun props. It already has **`BuildDiagram`** for a minimal void. This subplan renames the full preset entry point to **`BuildCinematic`** and routes sun mesh creation through **`Lighting.AddSun`** instead of `RedDwarfSun.CreateVisual`.

## Current behavior

- **`BuildDiagram(grid)`** — adds diagram `WorldEnvironment` only (fog off, flat background).
- **`Build(grid)`** — cinematic stack: `CreateWorldEnvironment`, `CreateNebulaShell`, `CreateStarfield`, then **`RedDwarfSun.CreateVisual(center, half.Length())`** as child (line ~40).
- **`CreateStarfield`** — 700-instance multimesh (unrelated to sun prop; do not merge into `Lighting`).

## What to implement

1. Rename **`Build`** → **`BuildCinematic`** (update any callers; primary caller is future toggle or integration branch — grep `SpaceBackdrop` + `.Build(`).
2. In **`BuildCinematic`**, replace:
   - `AddChild(RedDwarfSun.CreateVisual(...))`
   - with **`Lighting.AddSun(this, center, half.Length())`** (or equivalent after [Lighting subplan](src-battle-presentation-graphics-Lighting.cs.plan.md)).
3. Leave **`BuildDiagram`** unchanged except doc comments if needed (pair names: diagram / cinematic).
4. Do **not** move nebula/starfield/fog helpers into `Lighting`.

## Edge cases

- **`BuildCinematic` called twice** — same as today for `Build` (controller should call once in `_Ready`).
- **Naming:** do not add `AddStar` API here; starfield stays **`CreateStarfield`**.

## Dependencies

- [Lighting.cs subplan](src-battle-presentation-graphics-Lighting.cs.plan.md) must land first (`Lighting.AddSun` exists).

## Verification

- `dotnet build`.
- Temporary swap in controller to `BuildCinematic`: Godot shows nebula + starfield + sun blob; diagram path still no sun.

## Out of scope (this file)

- Directional light tuning — [BattleController](src-battle-presentation-scene-BattleController.cs.plan.md) + `Lighting.Configure*`.
- Export toggle on controller — optional v1.1 in [full.plan.md](../full.plan.md).

## See also

- [full.plan.md § Architecture](../full.plan.md#architecture)
