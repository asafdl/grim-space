---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/picking/GridPick.cs
todoIds: [world-mapping]
dependsOn:
  - src/battle/presentation/PointMapping.cs
status: done
---

# Subplan: `GridPick.cs`

**Source:** [`src/battle/presentation/picking/GridPick.cs`](../../../../../../src/battle/presentation/picking/GridPick.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Move ring hover picks the closest **EndPosition** **cell** on the active **ring** via **`PickFromSet`**. That path must use the same world→**Coord** rule as **`PickCell`** and missile/flak modes.

## Current behavior

- Private **`WorldToCell(Vector3)`** — floor division by **`PointMapping.CellSize`**.
- **`PickFromSet`**, **`PickCell`**, **`PickUnit`** use **`PointMapping.ToWorld`** + ray distance helpers.

## Target API

No new public methods. Replace **`WorldToCell`** body with **`PointMapping.ToCoord(world)`** (delete private **`WorldToCell`** or make it a one-line forwarder removed after refactor).

## Behavior changes

None intended — mechanical delegate only.

## Done when

- No duplicate floor math in **`GridPick`**; all world→grid goes through **`PointMapping.ToCoord`**.

## Verification

- Build; smoke-test missile/flak pick in Godot if already used on branch.

## Out of scope

- Changing **`PickFromSet`** radius thresholds (used by ring hover as-is).
