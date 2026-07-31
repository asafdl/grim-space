---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/Combat.cs
todoIds: [hints-accessor]
dependsOn: []
status: pending
---

# Subplan: `Combat.cs`

**Source:** [`src/battle/presentation/ui/Combat.cs`](../../../../../../src/battle/presentation/ui/Combat.cs)

[Full plan](../full.plan.md)  [Index](../index.md)  [definitions.md](../../../../definitions.md)

## Why this file

**`CombatHints.BuildHint`**  discoverability for **Depthkey**, **Raywalk**, and optional ring position copy.

## Current behavior

Move branch: click-to-queue + planning suffix. No **Depthkey** line.

## What to implement

**Move** branch only:

1. Append **`Tab / Shift+Tab: cycle ring`** (**Depthkey**  match [CONTEXT.md](../../../../../../CONTEXT.md) wording in player copy).
2. Optional when **`RingCount > 1`**: **`Ring {i+1}/{n} (k={k})`** from **`PresentationFrame.MoveRingTable`** + **`ActiveRingIndex`** (pass into hint builder or read from **`BattleUi`** / frame refresh).
3. Optional dev line when click debug toggle visible: mention **Raywalk** / **long click** queue per mode **C** (short; exact string TBD in implementation).

Do not change missile/flak/railgun strings.

## Done when

- F5 move mode shows **Depthkey** hint.
- Ring position line appears when multiple rings exist (if v1 includes optional clause).

## Out of scope

- InputMap / rebinding.
- **`HoveredMoveIndex`** implementation — [InteractionState subplan](src-battle-presentation-Interaction-InteractionState.cs.plan.md).
