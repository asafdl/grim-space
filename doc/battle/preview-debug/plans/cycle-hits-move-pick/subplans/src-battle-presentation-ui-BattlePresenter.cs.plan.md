---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/BattlePresenter.cs
todoIds: [hints-accessor]
dependsOn: []
status: pending
---

# Subplan: `BattlePresenter.cs`

**Source:** [`src/battle/presentation/ui/BattlePresenter.cs`](../../../../../../src/battle/presentation/ui/BattlePresenter.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

`BattlePresenter` owns presentation **selection state** for move hover (`MovementSelection` / internal `_selection`). [`SetMoveHover`](../../../../../../src/battle/presentation/ui/BattlePresenter.cs) updates which move option index drives path and endpoint highlights in `BuildFrame` → `GridView`.

[`BattleController`](../../../../../../src/battle/presentation/scene/BattleController.cs) must **commit the same index on click that the user saw while hovering**, including after Tab cycles. Today the controller re-picks on click; the fix needs a **stable, read-only view** of the hovered move index without duplicating pick logic in the controller.

## Current behavior

- Private **`MovementSelection _selection`** (or equivalent) holds hover index and option count.
- **`SetMoveHover(int? index, int optionCount)`** (~lines 98–99) sets hover; **`ClampToCount`** (~195) keeps index valid when option list shrinks.
- No public property exposes hovered move index to the scene controller.

## What to implement

Add a **read-only** accessor, e.g.:

```csharp
public int? HoveredMoveIndex => _selection.HoveredIndex;
```

(Exact member name on `_selection` — match existing `MovementSelection` API; goal is **`int?`** when nothing hovered.)

### Contract

- **Set only via** existing `SetMoveHover` / presenter methods — not settable from outside.
- Value reflects **last** hover set by controller `_Process` or Tab handler.
- When hover cleared (`SetMoveHover(null, count)`), accessor returns **`null`**.

### Consumers

[`BattleController.HandleLeftClick`](../../../../../../src/battle/presentation/scene/BattleController.cs) Move branch:

```csharp
if (_presenter.HoveredMoveIndex is int index)
    _presenter.TryQueueMove(index, frame.MoveOptions);
```

No second call to `PickOptionIndex`.

## Edge cases

- **Hover null at click** — no move queued (user clicked empty space or left pick radius).
- **Index stale after frame rebuild** — `TryQueueMove` should validate index against `frame.MoveOptions`; if invalid, no-op (existing behavior).
- **Option count drops** — existing clamp in presenter should run before click; document that controller refreshes frame before click handling (already builds `frame` in `_UnhandledInput`).

## Optional follow-up (not v1)

- Expose **`RayHitCount`** / cursor for hints (`target 2/5 along sight`) — requires controller to pass counts into presenter or hint builder; see [Combat subplan](src-battle-presentation-ui-Combat.cs.plan.md) optional v1.1.

## Dependencies

- None for adding the property.
- **BattleController** click path depends on this accessor.

## Verification

- Breakpoint or log: after Tab, `HoveredMoveIndex` matches highlighted option; click queues that index.
- No new public setters on selection state.

## Out of scope

- Changing highlight colors, path mesh logic, or `BuildFrame` structure beyond what clamp already does.
- Hint string composition — Combat.cs.
