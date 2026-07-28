---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/scene/BattleController.cs
todoIds: [blank-backdrop, controller-cycle]
dependsOn:
  - src/battle/presentation/graphics/SpaceBackdrop.cs
  - src/battle/presentation/graphics/RedDwarfSun.cs
  - src/battle/presentation/ui/MovementSelection.cs
status: pending
---

# Subplan: `BattleController.cs`

**Source:** [`src/battle/presentation/scene/BattleController.cs`](../../../../../../src/battle/presentation/scene/BattleController.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

`BattleController` is the Godot scene entry for battle presentation: it builds backdrop/grid views in `_Ready`, drives **per-frame move hover** in `_Process`, routes **input** in `_UnhandledInput`, and **commits** moves on left click in `HandleLeftClick`. All cycle-hits UX and diagram backdrop **call-site wiring** land here — not in simulation or pathfinding.

## Current behavior (relevant paths)

**Backdrop (`_Ready`, ~lines 39–46):**

- `backdrop.Build(battle.Grid)` — full space chamber.
- `RedDwarfSun.Configure(_directionalLight, ...)` — warm directional tint.

**Hover (`_Process`, ~78–97):**

- Early exit when resolving, not in `Move` mode, battle over, or no active actor; clears `_lastHoveredMoveIndex`.
- Builds `PresentationFrame`, calls **`MovementSelection.PickOptionIndex(camera, mouse, MoveOptions)`**.
- If index unchanged, skip refresh; else `SetMoveHover(index, count)` + `Refresh()`.

**Click (`HandleLeftClick`, Move branch ~255–260):**

- **Re-runs `PickOptionIndex` on click position** — can differ from hover after Tab cycle (bug this plan fixes).

**Other clears of `_lastHoveredMoveIndex`:** undo (Ctrl+Z), heading/roll success, successful move queue, early `_Process` exit.

## Part A — Blank backdrop (todo `blank-backdrop`)

### Steps

1. In `_Ready`, replace `backdrop.Build(battle.Grid)` with **`backdrop.BuildDiagram(battle.Grid)`**.
2. Replace **`RedDwarfSun.Configure(...)`** with **`RedDwarfSun.ConfigureNeutral(_directionalLight, gridCenter, chamberRadius)`** using same center/radius arguments as today.
3. Optional v1.1: `[Export] bool UseDiagramBackdrop = true` and branch `Build` vs `BuildDiagram` for A/B in editor without duplicating scenes.

### Verification

F5 on `feature/simple-preview`: flat void, no sun/nebula; grid highlights readable.

## Part B — Cycle hits (todo `controller-cycle`)

**Prerequisite:** [MovementSelection subplan](src-battle-presentation-ui-MovementSelection.cs.plan.md) — `ListOptionIndicesAlongRay`.

### New private state (move mode only)

| Field | Type | Role |
|--------|------|------|
| `_rayHitIndices` | `IReadOnlyList<int>` or cached `List<int>` | Sorted legal option indices along current aim ray |
| `_rayHitCursor` | `int` | Index into `_rayHitIndices` (0 = nearest along ray) |
| `_lastAimScreenPos` | `Vector2` | Screen position when hit list was last rebuilt |

Keep **`_lastHoveredMoveIndex`** only if still useful for “skip refresh when unchanged”; otherwise derive “changed” from `(hits[cursor], count)`.

### `_Process` — replace single pick

1. Same early exits as today; on exit, also clear **`_rayHitIndices` / `_rayHitCursor`** (or leave stale but invisible — prefer clear for consistency).
2. `mousePos = GetViewport().GetMousePosition()`.
3. `hits = MovementSelection.ListOptionIndicesAlongRay(_camera, mousePos, frame.MoveOptions)`.
4. **Stability rule:** If `mousePos` within ~**2 px** of `_lastAimScreenPos` **and** `hits` is sequence-equal to `_rayHitIndices`, **keep** `_rayHitCursor` clamped to `[0, hits.Count)`.
5. Else: assign `_rayHitIndices = hits`, `_lastAimScreenPos = mousePos`, **`_rayHitCursor = 0`**.
6. If `hits.Count == 0`: `SetMoveHover(null, frame.MoveOptions.Count)`; else `SetMoveHover(hits[_rayHitCursor], frame.MoveOptions.Count)`.
7. Refresh when hovered option index or count meaningfully changes (same optimization as today).

**Note:** Move mode does not use `HandleMouseMotion` for hover (that path returns immediately for `EPlayerMode.Move`); **`_Process` remains the hover driver**.

### `_UnhandledInput` — Tab cycle

Insert **after** resolving/battle-over guards, **before** mouse click handling, when:

- `_presenter.Mode == EPlayerMode.Move`
- Not resolving / not battle over

On `InputEventKey` Tab (Pressed, not Echo):

1. Rebuild `hits` from **current** mouse position (same API as `_Process`).
2. If `hits.Count <= 1`, no-op (optional: still `SetInputAsHandled` to swallow Tab in UI).
3. Shift held: `_rayHitCursor = (_rayHitCursor - 1 + hits.Count) % hits.Count`; else: `_rayHitCursor = (_rayHitCursor + 1) % hits.Count`.
4. Update `_rayHitIndices` / `_lastAimScreenPos` if list changed during rebuild; clamp cursor.
5. `SetMoveHover(hits[_rayHitCursor], count)`, `Refresh()`, **`SetInputAsHandled()`**.

Do **not** add Tab cycling for missile/flak/railgun in this subplan.

### `HandleLeftClick` — Move branch

Replace:

```csharp
if (MovementSelection.PickOptionIndex(...) is int index)
```

With:

- Use **[`BattlePresenter.HoveredMoveIndex`](src-battle-presentation-ui-BattlePresenter.cs.plan.md)** (or equivalent from `_presenter` after `SetMoveHover`).
- If non-null: `TryQueueMove(index, frame.MoveOptions)`, clear hover + **ray cycle state** (`_lastHoveredMoveIndex`, `_rayHitCursor`, empty hit list).

### Clear cycle state (same places as hover clear today)

- `_Process` early exit (resolving, wrong mode, battle over, no actor).
- Undo success.
- Heading / roll queued.
- Successful move queue.
- Consider **`ExitAimIfNeeded` / mode change** via action bar — clear ray state when leaving Move mode (mirror `_lastHoveredMoveIndex` behavior).

## Edge cases

- **Tab with mouse moved 1 px:** 2 px threshold avoids cursor reset on jitter; large move resets to nearest hit.
- **Options change mid-hover** (undo, AP, path commit): `MoveOptions.Count` may change; `SetMoveHover` already uses count — clamp cursor after rebuild.
- **Click with no hover:** no queue (same as today when pick misses).
- **Performance:** rebuilding hit list every frame is O(n options); acceptable for planning UX on typical option counts.

## Dependencies

- [SpaceBackdrop.cs](src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) — `BuildDiagram`
- [RedDwarfSun.cs](src-battle-presentation-graphics-RedDwarfSun.cs.plan.md) — `ConfigureNeutral`
- [MovementSelection.cs](src-battle-presentation-ui-MovementSelection.cs.plan.md) — ray list API
- [BattlePresenter.cs](src-battle-presentation-ui-BattlePresenter.cs.plan.md) — click reads hover (can implement in parallel with Part B)

## Verification

- Stacked endpoints along sight: Tab / Shift+Tab changes highlighted path tile; **click queues that path**, not a different one.
- Undo / mode change / end planning clears cycle.
- Manual checklist in [full.plan.md § Manual test](../full.plan.md).

## Out of scope (this file)

- Ray math implementation — MovementSelection subplan.
- Hint strings — Combat subplan.
- Camera defaults, depth bands, shell filtering.
