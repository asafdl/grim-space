---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/MovementSelection.cs
todoIds: [ray-hit-list]
dependsOn: []
status: pending
---

# Subplan: `MovementSelection.cs`

**Source:** [`src/battle/presentation/ui/MovementSelection.cs`](../../../../../../src/battle/presentation/ui/MovementSelection.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

All move **hover picking** math for endpoints lives here. [`PickOptionIndex`](../../../../../../src/battle/presentation/ui/MovementSelection.cs) projects a camera ray and picks the **single** legal `Option` whose endpoint world position is within **`PickRadius`** (1.4f) of the ray in perpendicular distance.

When several endpoints stack along the **same view direction** (common on a 64³ grid), they share similar perpendicular distance; sort order is unstable and **does not expose “depth along sight”** for cycling.

## Current algorithm (reference)

```csharp
// For each option i:
//   world = ToWorld(EndPosition)
//   distance = DistanceRayToPoint(origin, direction, world)
//   keep i if distance < bestDistance (starts at PickRadius)
```

`DistanceRayToPoint` computes closest point on ray with `t = clamp(dot(toPoint, direction), 0, 200)`.

## What to implement

### 1. `ListOptionIndicesAlongRay(Vector3 origin, Vector3 direction, IReadOnlyList<Option> options, float pickRadius = PickRadius)`

- For each option index `i`, compute perpendicular distance as today.
- If `distance >= pickRadius`, skip.
- Else record `(i, t, distance)` where **`t = dot(toPoint, direction)`** using same clamp as `DistanceRayToPoint` (0..200 or share helper).
- Sort ascending by **`t`**, then by **`distance`**, then by **`i`** (stable tie-break).
- Return `IReadOnlyList<int>` of indices only.

### 2. `ListOptionIndicesAlongRay(Camera3D camera, Vector2 screenPos, IReadOnlyList<Option> options)`

- `ProjectRayOrigin` / `ProjectRayNormal` → delegate to vector overload.

### 3. Refactor `PickOptionIndex`

- Build list via `ListOptionIndicesAlongRay`.
- Return **`list.Count > 0 ? list[0] : null`** so default hover remains “nearest along ray” when controller cursor is 0.

Optional (not v1): expose `RayOptionHit` struct with `Index`, `T`, `PerpDistance` for hint “2/5 along sight”.

## Edge cases

- **Empty options** — return empty list (move path in progress may yield zero endpoint options from `GetLegalMoves`; list empty is ok).
- **Options behind camera** — `t` clamped to 0 minimum; endpoints behind eye may still appear if within radius (same as today).
- **Duplicate endpoints** — search should not produce duplicate `EndPosition`; if it does, both indices may appear (acceptable).

## Dependencies

None for coding order; **BattleController** and **tests** depend on this API.

## Verification

- **Unit tests** (see RayOptionCycleTests subplan): three synthetic endpoints on a line along +Z, origin at origin, direction +Z → order nearest-first.
- Option far from ray (> PickRadius) excluded.

## Out of scope

- Changing `PickRadius` or path highlight logic (`GetHighlights`, `WithCommittedMove`).
- Grid cell picking (`GridPick`).
