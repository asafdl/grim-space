# Plan: cycle-hits-move-pick

| | |
|--|--|
| **Full plan** | [full.plan.md](full.plan.md) — goals, architecture, sections |
| **Changed files** | [full.plan.md § Changed files](full.plan.md#changed-files) |
| **Parent (Cursor)** | `%USERPROFILE%\.cursor\plans\cycle_hits_move_pick_3cd004dc.plan.md` |
| **Branch** | `feature/simple-preview` |
| **Edit root** | `side-branches/preview-debug-friendly/side-branches/simple-preview` |

Each **subplan** is self-contained: why the file matters, current behavior, step-by-step changes, edge cases, and verification. Read [full.plan.md](full.plan.md) for cross-cutting context.

## Changed files (links)

| Source | Subplan | Todos |
|--------|---------|-------|
| [`SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplan](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | blank-backdrop |
| [`RedDwarfSun.cs`](../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) | [subplan](subplans/src-battle-presentation-graphics-RedDwarfSun.cs.plan.md) | blank-backdrop |
| [`BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplan](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | blank-backdrop, controller-cycle |
| [`MovementSelection.cs`](../../../../../src/battle/presentation/ui/MovementSelection.cs) | [subplan](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md) | ray-hit-list |
| [`BattlePresenter.cs`](../../../../../src/battle/presentation/ui/BattlePresenter.cs) | [subplan](subplans/src-battle-presentation-ui-BattlePresenter.cs.plan.md) | hints-accessor |
| [`Combat.cs`](../../../../../src/battle/presentation/ui/Combat.cs) | [subplan](subplans/src-battle-presentation-ui-Combat.cs.plan.md) | hints-accessor |
| [`RayOptionCycleTests.cs`](../../../../../grim-space.Tests/Presentation/RayOptionCycleTests.cs) *(new)* | [subplan](subplans/grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md) | tests |

## Suggested implementation order

1. **Backdrop** — [SpaceBackdrop.cs](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) → [RedDwarfSun.cs](../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) → [BattleController._Ready](../../../../../src/battle/presentation/scene/BattleController.cs) (Godot check after step 3).
2. **Ray math** — [MovementSelection.cs](../../../../../src/battle/presentation/ui/MovementSelection.cs) (enables tests).
3. **Cycle UX** — [BattleController](../../../../../src/battle/presentation/scene/BattleController.cs) `_Process`, `_UnhandledInput`, `HandleLeftClick`.
4. **Discoverability** — [BattlePresenter.cs](../../../../../src/battle/presentation/ui/BattlePresenter.cs), [Combat.cs](../../../../../src/battle/presentation/ui/Combat.cs).
5. **Regression** — [RayOptionCycleTests.cs](../../../../../grim-space.Tests/Presentation/RayOptionCycleTests.cs) + `dotnet test`.

## Proposed commits

Commit when the step is **done and verifiable**. Unit of work: **manifest todo** (below), not one commit per subplan. [BattleController.cs](../../../../../src/battle/presentation/scene/BattleController.cs) appears in **commits 1 and 3** (backdrop wiring vs cycle UX).

| Step | Todo id | Scope (files / subplans) | Verify before commit | Suggested message |
|------|---------|---------------------------|----------------------|-------------------|
| 1 | `blank-backdrop` | [SpaceBackdrop](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md), [RedDwarfSun](subplans/src-battle-presentation-graphics-RedDwarfSun.cs.plan.md), [BattleController](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) — `_Ready` only (`BuildDiagram`, neutral light) | F5: flat dark void; no nebula/stars/sun blob | `feat(preview): diagram backdrop for planning readability` |
| 2 | `ray-hit-list` | [MovementSelection](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md) | Project builds; optional early test after step 5 | `feat(battle): list move options along view ray` |
| 3 | `controller-cycle` | [BattleController](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) — `_Process`, `_UnhandledInput` Tab, `HandleLeftClick` (uses [MovementSelection](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md); may need [HoveredMoveIndex](subplans/src-battle-presentation-ui-BattlePresenter.cs.plan.md) for click) | Tab / Shift+Tab cycles hover; click queues highlighted path | `feat(battle): Tab cycle move targets along sight ray` |
| 4 | `hints-accessor` | [BattlePresenter](subplans/src-battle-presentation-ui-BattlePresenter.cs.plan.md), [Combat](subplans/src-battle-presentation-ui-Combat.cs.plan.md) | Move-mode hint shows Tab line | `feat(battle): hint and hover accessor for move cycle` |
| 5 | `tests` | [RayOptionCycleTests](subplans/grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md) | `dotnet test` | `test(battle): ray option sort and pick radius` |

Plan docs under `doc/battle/preview-debug/plans/cycle-hits-move-pick/` may ship with commit 1 or a separate `docs:` commit if plans predate code.

## Subplans

| File | Subplan | Todos |
|------|---------|-------|
| [`src/battle/presentation/graphics/SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | blank-backdrop |
| [`src/battle/presentation/graphics/RedDwarfSun.cs`](../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) | [subplans/src-battle-presentation-graphics-RedDwarfSun.cs.plan.md](subplans/src-battle-presentation-graphics-RedDwarfSun.cs.plan.md) | blank-backdrop |
| [`src/battle/presentation/scene/BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplans/src-battle-presentation-scene-BattleController.cs.plan.md](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | blank-backdrop, controller-cycle |
| [`src/battle/presentation/ui/MovementSelection.cs`](../../../../../src/battle/presentation/ui/MovementSelection.cs) | [subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md) | ray-hit-list |
| [`src/battle/presentation/ui/BattlePresenter.cs`](../../../../../src/battle/presentation/ui/BattlePresenter.cs) | [subplans/src-battle-presentation-ui-BattlePresenter.cs.plan.md](subplans/src-battle-presentation-ui-BattlePresenter.cs.plan.md) | hints-accessor |
| [`src/battle/presentation/ui/Combat.cs`](../../../../../src/battle/presentation/ui/Combat.cs) | [subplans/src-battle-presentation-ui-Combat.cs.plan.md](subplans/src-battle-presentation-ui-Combat.cs.plan.md) | hints-accessor |
| [`grim-space.Tests/Presentation/RayOptionCycleTests.cs`](../../../../../grim-space.Tests/Presentation/RayOptionCycleTests.cs) *(new)* | [subplans/grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md](subplans/grim-space.Tests-Presentation-RayOptionCycleTests.cs.plan.md) | tests |

## Todo rollup

| Id | Summary | Status |
|----|---------|--------|
| blank-backdrop | Diagram backdrop + neutral light | pending |
| ray-hit-list | ListOptionIndicesAlongRay | pending |
| controller-cycle | Tab cycle + hover/click fix | pending |
| hints-accessor | Hint line + HoveredMoveIndex | pending |
| tests | RayOptionCycleTests | pending |
