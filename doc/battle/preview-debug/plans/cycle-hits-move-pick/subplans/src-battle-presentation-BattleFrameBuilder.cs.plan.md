---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/BattleFrameBuilder.cs
todoIds: [move-preview-rings]
dependsOn:
  - src/battle/presentation/ui/MovePreviewRings.cs
status: done
---

# Subplan: `BattleFrameBuilder.cs`

**Source:** [`src/battle/presentation/BattleFrameBuilder.cs`](../../../../../../src/battle/presentation/BattleFrameBuilder.cs)

**Also touches:** [`PresentationFrame`](../../../../../../src/battle/presentation/Ui/PresentationFrame.cs) (new field on snapshot).

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Builds **`PresentationFrame`** each refresh via **`BattleUi.BuildFrame()` → `BattleFrameBuilder.Build`**. Ring UX needs **`MovePreviewRings.BuildRingTable`** attached once per snapshot (preview actor **A** + **`MoveOptions`**), not recomputed on Tab or mouse.

## Part 1 — Frame ring table (todo `move-preview-rings`)

In **`Build`**:

- After **`actorState`** and **`moveOptions`** are known:
  - **`MovePreviewRings.BuildRingTable(actorState.Position, moveOptions)`**
- Store on **`PresentationFrame`** (e.g. **`MoveRingTable`**).

Invalidate only when **`Build`** runs for a new snapshot — do **not** read **`InteractionState.MoveHoveredIndex`** or ring Tab state when building the table.

## Done when

- Frame carries stable **`MovePreviewRingTable`** for the planning snapshot.
- Controller can read **`RingCount`** / **`ShellK`** from the frame without recomputing the table.

## Out of scope

- **`ActiveRingIndex`** — [InteractionState subplan](src-battle-presentation-Interaction-InteractionState.cs.plan.md).
- GridView dimming, controller input.

## Verification

- `dotnet build`; [MovePreviewRingTests](grim-space.Tests-Presentation-MovePreviewRingTests.cs.plan.md) still pass.
