---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/MovePreviewRings.cs
todoIds: [move-preview-rings]
dependsOn: []
status: pending
---

# Subplan: `MovePreviewRings.cs` *(new)*

**Source:** [`src/battle/presentation/ui/MovePreviewRings.cs`](../../../../../../src/battle/presentation/ui/MovePreviewRings.cs) *(create)*

[Full plan](../full.plan.md) · [Index](../index.md) · [definitions.md](../../../../definitions.md)

## Why this file

Pure presentation logic for the **snapshot cache**: group legal **Options** by Manhattan shell **k** from actor **Position** **A**, dedupe endpoints, expose stable **Depthkey** bands. Keeps Godot/controller code thin and testable without **`Camera3D`**.

## Target API

```csharp
readonly struct MovePreviewRingTable
{
  int RingCount { get; }
  int ShellK(int ringIndex);
  IReadOnlyList<int> OptionIndicesOnRing(int ringIndex);
}

MovePreviewRingTable BuildRingTable(Coord actor, IReadOnlyList<Option> options);
```

Static class **`MovePreviewRings`** (or namespace-consistent name) hosts **`BuildRingTable`**.

## Behavior

1. For each option index `i`, **`k = actor.ManhattanDistanceTo(options[i].EndPosition)`** (shell only, not inclusive cube).
2. Per **k**, keep **one index per `EndPosition`**: lowest **`ApCost`**, then lowest **`i`**.
3. **`ShellKValues`**: sorted distinct **k** that still have ≥1 option after dedupe (**skip empty shells** for Tab list).
4. **`OptionIndicesOnRing(ringIndex)`** returns deduped indices for that **k**, stable order (e.g. sort by **EndPosition** lexicographic or original index).

## Behavior changes

New file — no legacy behavior.

## Done when

- Table builds in O(n) over options; empty **`MoveOptions`** → **`RingCount == 0`**.
- [BattlePresenter subplan](src-battle-presentation-ui-BattlePresenter.cs.plan.md) attaches table to **`PresentationFrame`** on snapshot change only.

## Edge cases

- All options same **k** → single ring.
- Duplicate **EndPosition** at different **ApCost** → one winner per dedupe rule.
- Actor **A** equals an **EndPosition** → **k = 0** ring if legal.

## Verification

- [MovePreviewRingTests subplan](grim-space.Tests-Presentation-MovePreviewRingTests.cs.plan.md).

## Out of scope

- **`GridPick`**, camera rays, Tab input.
- Full-grid LUT.
