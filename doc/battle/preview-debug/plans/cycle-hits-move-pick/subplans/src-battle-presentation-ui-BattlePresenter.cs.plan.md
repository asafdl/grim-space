---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/BattlePresenter.cs
todoIds: [move-preview-rings, hints-accessor]
dependsOn:
  - src/battle/presentation/ui/MovePreviewRings.cs
status: pending
---

# Subplan: `BattlePresenter.cs`

**Source:** [`src/battle/presentation/ui/BattlePresenter.cs`](../../../../../../src/battle/presentation/ui/BattlePresenter.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Builds **`PresentationFrame`** and owns move hover selection. Ring UX needs the **snapshot ring table** on the frame and read-only accessors for controller click + hints.

## Part 1 — Frame ring table (todo `move-preview-rings`)

In **`BuildFrame`** (when preview actor **Coord** **A** or **`MoveOptions`** change):

- Call **`MovePreviewRings.BuildRingTable(A, moveOptions)`**
- Store on **`PresentationFrame`** (new field, e.g. **`MoveRingTable`**)

Invalidate only on snapshot change — not on Tab or mouse.

## Part 2 — Accessors (todo `hints-accessor`)

Add read-only surface, names illustrative:

```csharp
public int? HoveredMoveIndex => ...;
public int ActiveRingIndex { get; }  // set via presenter method from controller
public int RingCount => frame.MoveRingTable.RingCount;
public int ActiveShellK => ...;
```

- **`HoveredMoveIndex`** — set only through existing **`SetMoveHover`**; **`null`** when cleared.
- **`ActiveRingIndex`** — update via **`SetActiveRingIndex(int)`** (or fold into hover API) when controller handles **Depthkey**; clamp to **`RingCount`**.

### Consumers

- [BattleController](src-battle-presentation-scene-BattleController.cs.plan.md) click mode **A** / **C**
- [Combat hints](src-battle-presentation-ui-Combat.cs.plan.md) **`Ring i/n (k=…)`**

## Done when

- Frame carries stable **`MovePreviewRingTable`** for the planning snapshot.
- Controller can read ring count / **k** without recomputing table.

## Out of scope

- GridView dimming off-ring cells.
- Building table inside controller.
