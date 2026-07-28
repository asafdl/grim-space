---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/scene/BattleController.cs
todoIds: [controller]
dependsOn:
  - src/battle/presentation/graphics/Lighting.cs
  - src/battle/presentation/graphics/SpaceBackdrop.cs
status: pending
---

# Subplan: `BattleController.cs`

**Source:** [`src/battle/presentation/scene/BattleController.cs`](../../../../../../src/battle/presentation/scene/BattleController.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

`BattleController._Ready` is the wiring point for backdrop preset and scene **`DirectionalLight3D`**. Today it calls **`backdrop.BuildDiagram`** but still **`RedDwarfSun.Configure`** (warm cinematic light), which tints the diagram backdrop orange. This subplan pairs diagram backdrop with **`Lighting.ConfigureDiagram`** and documents cinematic pairing for toggle/merge.

## Current behavior

In **`_Ready`** (~lines 39–51):

1. `new SpaceBackdrop()` → **`BuildDiagram(battle.Grid)`** → add as child.
2. After grid/camera setup: **`RedDwarfSun.Configure(GetNode<DirectionalLight3D>("DirectionalLight3D"), gridCenter, chamberRadius)`**.

No `[Export]` backdrop toggle yet.

## What to implement

1. Replace **`RedDwarfSun.Configure(...)`** with **`Lighting.ConfigureDiagram(...)`** using same `gridCenter` and `chamberRadius` as today.
2. Ensure `using` / namespace resolves `Lighting` in `GrimSpace.Battle.Presentation.Graphics`.
3. **Optional v1.1:** add `[Export] bool UseDiagramBackdrop = true` and branch:
   - diagram: `BuildDiagram` + `ConfigureDiagram`
   - cinematic: `BuildCinematic` + `ConfigureCinematic`
4. Do **not** call **`AddSun`** here — sun props are added inside **`SpaceBackdrop.BuildCinematic`** only.

## Edge cases

- **`IsResolving` / input** — unchanged; lighting is setup-only in `_Ready`.
- **Integration branch** — cinematic branch must call **`BuildCinematic`** + **`ConfigureCinematic`** together (see [SpaceBackdrop subplan](src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md)).

## Dependencies

- [Lighting.cs](src-battle-presentation-graphics-Lighting.cs.plan.md) — `ConfigureDiagram` / `ConfigureCinematic`.
- [SpaceBackdrop.cs](src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) — `BuildCinematic` name if using export toggle.

## Verification

- Godot F5 on default (diagram): no sun mesh, grid/ships read neutral (not orange wash).
- With export false or temporary cinematic calls: warm light + sun visible from backdrop.

## Out of scope (this file)

- Tab ray cycle, `_Process` move hover — [cycle-hits-move-pick](../../cycle-hits-move-pick/index.md).
- Creating `Lighting.cs` — [Lighting subplan](src-battle-presentation-graphics-Lighting.cs.plan.md).

## See also

- [full.plan.md § Verification](../full.plan.md#verification-full)
