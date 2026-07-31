---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/Interaction/InteractionState.cs
todoIds: [controller-cycle, hints-accessor]
dependsOn:
  - src/battle/presentation/BattleFrameBuilder.cs
status: pending
---

# Subplan: `InteractionState.cs`

**Source:** [`src/battle/presentation/Interaction/InteractionState.cs`](../../../../../../src/battle/presentation/Interaction/InteractionState.cs)

**Facade:** [`BattleUi`](../../../../../../src/battle/presentation/BattleUi.cs) forwards hover/queue; expose ring index through **`BattleUi`**.

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Move hover and mode live here today (**`MoveHoveredIndex`**, **`SetMoveHover`**). Ring UX adds **active ring band** state separate from the snapshot table on **`PresentationFrame`**.

## Part 1 — Ring index + snapshot sync (todo `controller-cycle`)

- **`ActiveRingIndex`** — which Manhattan band (0 … `RingCount - 1`).
- **`SyncActiveRingForSnapshot(PresentationFrame)`** — when preview **Position** or **`MoveOptions`** count changes, reset index to **0**; clamp to **`MovePreviewRingTable.RingCount`**.
- **`CycleActiveRingIndex(delta, ringCount)`** — Depthkey; no-op if **`ringCount <= 1`**.
- Reset in **`ClearInteraction`** / mode changes (with hover clear).

**`BattleUi.BuildFrame`** calls **`SyncActiveRingForSnapshot`** after **`BattleFrameBuilder.Build`**.

## Part 2 — Hints consumers (todo `hints-accessor`)

- **`BattleUi.ActiveRingIndex`** for [Combat hints](src-battle-presentation-ui-Combat.cs.plan.md) — **`Ring i/n (k=…)`** with frame table.
- **`MoveHoveredIndex`** unchanged — set only through **`SetMoveHover`**.

### Consumers

- [BattleController](src-battle-presentation-scene-BattleController.cs.plan.md) Tab → **`CycleActiveRingIndex`**; hover/click reads **`ActiveRingIndex`** + frame table.

## Done when

- Controller and hints can read ring band index without recomputing **`BuildRingTable`**.

## Out of scope

- Building ring table — [BattleFrameBuilder subplan](src-battle-presentation-BattleFrameBuilder.cs.plan.md).
- Hint string copy — Combat subplan.
