---
parentPlan: ../full.plan.md
targetFile: grim-space.Tests/Presentation/MovePreviewRingTests.cs
todoIds: [tests]
dependsOn:
  - src/battle/presentation/ui/MovePreviewRings.cs
status: pending
---

# Subplan: `MovePreviewRingTests.cs` *(new)*

**Source:** [`grim-space.Tests/Presentation/MovePreviewRingTests.cs`](../../../../../../grim-space.Tests/Presentation/MovePreviewRingTests.cs) *(create)*

[Full plan](../full.plan.md) · [Index](../index.md)

**Supersedes:** [`grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md`](grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md) (ray-list — do not implement).

## Why this file

**Snapshot ring table** grouping and dedupe are easy to regress; tests run without Godot.

## Cases

1. **Two shells** — options at **k=1** and **k=2** → **`RingCount == 2`**, sorted **k**.
2. **Dedupe** — two options same **EndPosition**, different **ApCost** → one index kept (lower cost).
3. **Skip empty k** — no options at **k=2** but options at **k=1** and **k=3** → Tab list is **1, 3** only.
4. **Empty options** — empty table / zero rings.
5. **`OptionIndicesOnRing`** — returns expected indices for a given ring index.

Use synthetic **`Coord`** / **`Option`** fixtures consistent with existing **`grim-space.Tests`** patterns.

## Done when

- **`dotnet test`** passes from simple-preview worktree root.

## Out of scope

- Camera ray sort tests (retired primary UX).
