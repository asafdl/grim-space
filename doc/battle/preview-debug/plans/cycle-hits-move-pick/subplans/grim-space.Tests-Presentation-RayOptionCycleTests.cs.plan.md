---
parentPlan: ../full.plan.md
targetFile: grim-space.Tests/Presentation/RayOptionCycleTests.cs
todoIds: [tests]
dependsOn:
  - src/battle/presentation/ui/MovementSelection.cs
status: pending
---

# Subplan: `RayOptionCycleTests.cs`

**Source:** [`grim-space.Tests/Presentation/RayOptionCycleTests.cs`](../../../../../../grim-space.Tests/Presentation/RayOptionCycleTests.cs) *(new file)*

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Ray hit ordering is **pure geometry** on legal move options — no Godot scene, no camera matrices required if tests call the **`Vector3 origin + direction`** overload of `ListOptionIndicesAlongRay`. Without tests, sort order and radius filtering regress silently when someone refactors `DistanceRayToPoint` or `PickRadius`.

This file locks the **contract** the BattleController cycle UX depends on: nearest along ray first, respect `PickRadius`, stable tie-breaks.

## Current state

File **does not exist**. Create under **`grim-space.Tests/Presentation/`** (folder if missing).

Follow existing test project conventions: xUnit **`[Fact]`**, naming like other presentation/move tests (e.g. browse `grim-space.Tests/Movement/` for patterns).

## What to implement

### API under test

- **`MovementSelection.ListOptionIndicesAlongRay(Vector3 origin, Vector3 direction, IReadOnlyList<Option> options, float pickRadius = PickRadius)`**
- Optionally assert **`PickOptionIndex`** (vector or camera-less path) equals **`list[0]`** when list non-empty — only if camera overload is hard to test; prefer testing list + document that `PickOptionIndex` delegates to first element.

### Test case 1 — Sort order along ray

**Setup:**

- Ray: `origin = Vector3.Zero`, `direction = Vector3.Forward` (or +Z normalized — match Godot/world convention used in `WorldMapping`).
- Three **`Option`** instances with **`EndPosition`** grid coords that map to world positions **on the same ray**, increasing **`t = dot(toPoint, direction)`** (e.g. same X/Y in world, increasing Z).

**Assert:**

- Returned index list order is **nearest → farthest** along `direction`.
- Count == 3.

Use **`WorldMapping.ToWorld`** with known **`WorldMapping.CellSize`** when constructing expected positions — read current mapping from production code; do not guess cell size.

### Test case 2 — Radius exclusion

**Setup:**

- One endpoint **on** ray within radius (should appear).
- One endpoint with **perpendicular distance ≥ pickRadius`** (use **`PickRadius`** constant from production, **1.4f**, or `InternalsVisibleTo` / public const if tests need explicit value).

**Assert:**

- List contains only the in-radius index.
- Excluded option’s index absent.

### Test case 3 — Tie-break

**Setup (if constructible):**

- Two endpoints with **equal `t`** but different perpendicular distances to ray.

**Assert:**

- Closer perpendicular distance **before** farther.
- If still tied, **lower option index** first (matches subplan MovementSelection spec).

If equal-`t` setup is awkward with grid snapping, document skip with comment and cover in case 1 ordering only — prefer at least one tie-break test.

### Test case 4 (optional) — Empty input

- Empty `options` → empty list.
- All options outside radius → empty list.

## Building minimal `Option` instances

Inspect **`Option`** type in presentation/move layer: required fields for list iteration (at minimum **`EndPosition`**). Use same constructors/factories as other tests if present; avoid pulling full battle simulation into tests.

## Dependencies

- [MovementSelection.cs](src-battle-presentation-ui-MovementSelection.cs.plan.md) must implement `ListOptionIndicesAlongRay` first.

## Run / CI

From simple-preview worktree root (folder with `grim-space.sln` / `.csproj`):

```powershell
dotnet test
```

Failures should distinguish **sort regression** vs **radius regression** via distinct test method names and assertion messages.

## Out of scope

- Godot headless or scene tree tests.
- Simulating Tab key in BattleController.
- Testing backdrop or presenter hover (manual Godot only).

## Verification

- All new facts green locally.
- Deliberately break sort in MovementSelection — at least one test fails.

## See also

- [full.plan.md § 5 Tests](../full.plan.md)
- [MovementSelection subplan](src-battle-presentation-ui-MovementSelection.cs.plan.md) for sort definition
