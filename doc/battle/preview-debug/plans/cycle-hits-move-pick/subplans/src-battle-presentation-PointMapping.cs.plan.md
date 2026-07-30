---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/PointMapping.cs
todoIds: [world-mapping]
dependsOn: []
status: done
supersedes: src-battle-presentation-WorldMapping.cs.plan.md
---

# Subplan: `PointMapping.cs`

**Source:** [`src/battle/presentation/PointMapping.cs`](../../../../../../src/battle/presentation/PointMapping.cs)

[Full plan](../full.plan.md) · [Index](../index.md) · [definitions.md](../../../../definitions.md)

_Renamed from **WorldMapping** — aligns with CONTEXT **Point**: grid **Position** (**Coord**) vs scene **Location** (**Vector3**)._

## Why this file

Single public boundary for grid ↔ Godot scene space. Ring hover uses **`GridPick.PickFromSet`** on **Coord** sets; **`ToWorld`** maps **cell** centers — the inverse lives here, not duplicated in pickers.

## Current behavior

- **`ToWorld(Coord)`** — cell center at `(coord + 0.5) * CellSize`.
- **`GridCenter(Grid)`** — chamber center.
- **`ToCoord(Vector3)`** — grid **Coord** from scene **Vector3** (see implementation).

## Target API

```csharp
public static Coord ToCoord(Vector3 point);
```

Inverse of **`ToWorld`** for picking; **`GridPick`** should delegate **`WorldToCell`** → **`ToCoord`**.

## Done when

- **`ToCoord`** matches picking expectations on representative points.
- [GridPick subplan](src-battle-presentation-picking-GridPick.cs.plan.md) delegates to **`PointMapping.ToCoord`**.

## Verification

- Build; manual missile/flak pick unchanged after GridPick subplan lands.

## Out of scope

- Changing **`CellSize`** or grid origin math.
