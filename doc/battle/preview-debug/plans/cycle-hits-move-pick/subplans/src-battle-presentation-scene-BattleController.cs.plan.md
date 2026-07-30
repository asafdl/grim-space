---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/scene/BattleController.cs
todoIds: [blank-backdrop, controller-cycle]
dependsOn:
  - src/battle/presentation/graphics/SpaceBackdrop.cs
  - src/battle/presentation/graphics/Lighting.cs
  - src/battle/presentation/ui/MovePreviewRings.cs
  - src/battle/presentation/picking/GridPick.cs
  - src/battle/presentation/ui/BattlePresenter.cs
status: pending
---

# Subplan: `BattleController.cs`

**Source:** [`src/battle/presentation/scene/BattleController.cs`](../../../../../../src/battle/presentation/scene/BattleController.cs)

[Full plan](../full.plan.md) · [Index](../index.md) · [definitions.md](../../../../definitions.md)

## Why this file

Scene entry: backdrop **`_Ready`**, move hover **`_Process`**, **Depthkey** / **Raywalk** input, move **click** modes. Wires ring table from **`PresentationFrame`** to **`GridPick`** and **`SetMoveHover`**.

## Part A — Blank backdrop (todo `blank-backdrop`)

Unchanged: **`BuildDiagram`**, **`Lighting.ConfigureDiagram`** in **`_Ready`**. See SpaceBackdrop / Lighting subplans.

## Part B — Ring planning UX (todo `controller-cycle`)

**Prerequisites:** [MovePreviewRings](src-battle-presentation-ui-MovePreviewRings.cs.plan.md) on frame, [GridPick](src-battle-presentation-picking-GridPick.cs.plan.md), [BattlePresenter](src-battle-presentation-ui-BattlePresenter.cs.plan.md) accessors.

### State (move mode)

| Field | Role |
|--------|------|
| `_activeRingIndex` | Index into frame **`ShellKValues`** / ring table |
| `_ringSnapshotId` or compare frame table ref | Detect snapshot change ? clamp/reset ring index |
| Raywalk / press timing | Hold scrub, fast vs long click for mode **C** |
| Debug export | Switch click modes **A / B / C** ([definitions § Click](../../../../definitions.md#click-target-vs-today)) |

Clear ring + hover state on: resolving, battle over, wrong mode, undo, heading/roll, successful queue, leaving Move.

### Snapshot change

When preview **Position** or **`MoveOptions`** change (new frame ring table identity):

- Rebuild not done here — table from **`BuildFrame`**
- Reset or clamp **`_activeRingIndex`**
- Do **not** rebuild ring table on Tab or mouse move

### `_Process` (Move)

1. Early exits as today.
2. Read **`frame.MovePreviewRings`** (name TBD) and **`_activeRingIndex`**.
3. Build **`HashSet<Coord>`** (or reuse cached set) of deduped **EndPosition**s for active ring from **`OptionIndicesOnRing`**.
4. **`GridPick.PickFromSet(_camera, mouse, set)`** ? **Coord**? map to **Option** index (unique per ring after dedupe) ? **`SetMoveHover`**.
5. **Raywalk**: while mouse held, update selection near?far on active ring (optional helper from [MovementSelection](src-battle-presentation-ui-MovementSelection.cs.plan.md)).
6. Refresh when hovered index changes.

### `_UnhandledInput` — **Depthkey**

On Tab (Move mode, not echo):

- **`_activeRingIndex = (_activeRingIndex ± 1) mod RingCount`** (Shift = backward)
- No-op if **`RingCount <= 1`**
- Update hover for same mouse position on new ring; **`SetInputAsHandled`**

### `HandleLeftClick` — Move

Per debug mode:

- **A** — **`HoveredMoveIndex`** ? **`TryQueueMove`**
- **B** — **`MovementSelection.PickOptionIndex`** at click (today)
- **C** — **fast click** pins scrub selection; **long click** queues pinned/hovered index

Never mix modes without explicit toggle.

### Done when

- Tab changes **ring** band, not ray-depth stack across all options.
- Hover only considers endpoints on active **ring**.
- Playtest toggle switches **A/B/C** without recompile (export or dev menu).

## Verification

- F5: **Depthkey** narrows endpoints; **Raywalk** on one band; click modes per toggle.
- Manual checklist in [full.plan.md](../full.plan.md).

## Out of scope

- Hint strings — [Combat subplan](src-battle-presentation-ui-Combat.cs.plan.md).
- **`BuildRingTable`** implementation — MovePreviewRings subplan.
