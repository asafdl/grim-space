---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/graphics/Lighting.cs
todoIds: [blank-backdrop]
dependsOn: []
status: done
supersedes: src-battle-presentation-graphics-RedDwarfSun.cs.plan.md
---

# Subplan: `Lighting.cs`

**Source:** [`src/battle/presentation/graphics/Lighting.cs`](../../../../../../src/battle/presentation/graphics/Lighting.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Static helpers for battle presentation lighting and sun mesh. **Diagram mode** needs neutral **`DirectionalLight3D`** setup; cinematic mode uses warm **`ConfigureCinematic`** and **`AddSun`** (see SpaceBackdrop **`Build`**).

_Replaces plan entry **RedDwarfSun.cs** — type consolidated into **`Lighting`**._

## Implemented (diagram)

**`ConfigureDiagram(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)`**:

- Light at `gridCenter`, look at `gridCenter + diagramDirection` (`(-0.4, -0.7, -0.5)` normalized)
- `LightColor = Colors.White`, `LightEnergy = 1.0f`, `ShadowEnabled = false`

**`ConfigureCinematic`** — warm red tint + shadows (unchanged cinematic path).

## Wiring

[`BattleController._Ready`](../../../../../../src/battle/presentation/scene/BattleController.cs) calls **`Lighting.ConfigureDiagram`** with diagram backdrop.

## Verification

With diagram backdrop: ships and grid read neutral, not orange cast.
