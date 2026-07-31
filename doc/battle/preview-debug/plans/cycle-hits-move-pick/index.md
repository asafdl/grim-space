# Plan: Manhattan rings + blank backdrop (slug: cycle-hits-move-pick)

**Glossary:** [CONTEXT.md](../../../../../CONTEXT.md) ? [definitions.md](../../definitions.md)

| | |
|--|--|
| **Full plan** | [full.plan.md](full.plan.md) ? goals, architecture, sections |
| **Changed files** | [full.plan.md ? Changed files](full.plan.md#changed-files) |
| **Parent (Cursor)** | `%USERPROFILE%\.cursor\plans\rings_ux_full_plan_3068da5d.plan.md` |
| **Legacy parent** | `%USERPROFILE%\.cursor\plans\cycle_hits_move_pick_3cd004dc.plan.md` (ray-hit-list ? superseded) |
| **Branch** | `feature/simple-preview` |
| **Edit root** | `side-branches/preview-debug-friendly/side-branches/simple-preview` |

Each **subplan** is self-contained: why the file matters, current behavior, step-by-step changes, edge cases, and verification. Read [full.plan.md](full.plan.md) for cross-cutting context.

## Changed files (links)

| Source | Subplan | Todos |
|--------|---------|-------|
| [`SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplan](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | blank-backdrop |
| [`Lighting.cs`](../../../../../src/battle/presentation/graphics/Lighting.cs) | [subplan](subplans/src-battle-presentation-graphics-Lighting.cs.plan.md) | blank-backdrop |
| [`PointMapping.cs`](../../../../../src/battle/presentation/PointMapping.cs) | [subplan](subplans/src-battle-presentation-PointMapping.cs.plan.md) | world-mapping |
| [`GridPick.cs`](../../../../../src/battle/presentation/picking/GridPick.cs) | [subplan](subplans/src-battle-presentation-picking-GridPick.cs.plan.md) | world-mapping |
| [`MovePreviewRings.cs`](../../../../../src/battle/presentation/ui/MovePreviewRings.cs) *(new)* | [subplan](subplans/src-battle-presentation-ui-MovePreviewRings.cs.plan.md) | move-preview-rings |
| [`BattleFrameBuilder.cs`](../../../../../src/battle/presentation/BattleFrameBuilder.cs) | [subplan](subplans/src-battle-presentation-BattleFrameBuilder.cs.plan.md) | move-preview-rings |
| [`InteractionState.cs`](../../../../../src/battle/presentation/Interaction/InteractionState.cs) | [subplan](subplans/src-battle-presentation-Interaction-InteractionState.cs.plan.md) | controller-cycle, hints-accessor |
| [`BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplan](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | blank-backdrop, controller-cycle |
| [`MovementSelection.cs`](../../../../../src/battle/presentation/ui/MovementSelection.cs) | [subplan](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md) | controller-cycle |
| [`Combat.cs`](../../../../../src/battle/presentation/ui/Combat.cs) | [subplan](subplans/src-battle-presentation-ui-Combat.cs.plan.md) | hints-accessor |
| [`MovePreviewRingTests.cs`](../../../../../grim-space.Tests/Presentation/MovePreviewRingTests.cs) *(new)* | [subplan](subplans/grim-space.Tests-Presentation-MovePreviewRingTests.cs.plan.md) | tests |

**Optional cleanup:** [refactor FormatMomentum](subplans/refactor-format-momentum-out-of-movement-selection.plan.md) (`format-momentum-home`).

## Suggested implementation order

1. **Backdrop** ? [SpaceBackdrop.cs](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) ? [Lighting.cs](../../../../../src/battle/presentation/graphics/Lighting.cs) (`ConfigureDiagram`) ? [BattleController._Ready](../../../../../src/battle/presentation/scene/BattleController.cs) (Godot F5 after wiring).
2. **World ? grid** ? [PointMapping.ToCoord](../../../../../src/battle/presentation/PointMapping.cs) ? [GridPick](../../../../../src/battle/presentation/picking/GridPick.cs) delegates.
3. **Ring table** — [MovePreviewRings.cs](../../../../../src/battle/presentation/ui/MovePreviewRings.cs) *(done)* + [BattleFrameBuilder.Build](../../../../../src/battle/presentation/BattleFrameBuilder.cs) / [PresentationFrame](../../../../../src/battle/presentation/Ui/PresentationFrame.cs) snapshot cache *(done)*.
4. **Controller UX** ? [BattleController](../../../../../src/battle/presentation/scene/BattleController.cs): **Depthkey**, **Raywalk**, `GridPick.PickFromSet` on active ring; click per [definitions.md](../../definitions.md) debug modes **A / B / C**.
5. **Discoverability** — [InteractionState](../../../../../src/battle/presentation/Interaction/InteractionState.cs) / [BattleUi](../../../../../src/battle/presentation/BattleUi.cs) ring accessors, [Combat hints](../../../../../src/battle/presentation/ui/Combat.cs).
6. **Regression** ? [MovePreviewRingTests.cs](../../../../../grim-space.Tests/Presentation/MovePreviewRingTests.cs) + `dotnet test`.

## Proposed commits

Commit when the step is **done and verifiable**. Unit of work: **manifest todo** (below), not one commit per subplan.

| Step | Todo id | Scope (files / subplans) | Verify before commit | Suggested message |
|------|---------|---------------------------|----------------------|-------------------|
| 0 | ? | CONTEXT + definitions + plan pivot (optional `docs:`) | Links resolve | `docs(preview): rings glossary and plan pivot` |
| 1 | `blank-backdrop` | SpaceBackdrop, Lighting, BattleController `_Ready` | F5 flat void | `feat(preview): diagram backdrop for planning readability` |
| 2 | `world-mapping` | PointMapping, GridPick | build | `feat(battle): PointMapping.ToCoord for picking` |
| 3 | `move-preview-rings` | MovePreviewRings, BattleFrameBuilder + PresentationFrame | unit tests pass | `feat(battle): Manhattan ring snapshot cache for move preview` |
| 4 | `controller-cycle` | BattleController ring Tab, Raywalk, hover | F5 Depthkey + scrub | `feat(battle): ring hover and Depthkey move planning` |
| 5 | `hints-accessor` | InteractionState / BattleUi + Combat + click contract | hint shows ring line | `feat(battle): ring hints and hovered move accessor` |
| 6 | `tests` | MovePreviewRingTests | `dotnet test` | `test(battle): move preview ring table` |

Plan docs may ship with commit 1 or a separate `docs:` commit.

## Subplans

| File | Subplan | Todos |
|------|---------|-------|
| [`src/battle/presentation/graphics/SpaceBackdrop.cs`](../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) | [subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md](subplans/src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md) | blank-backdrop |
| [`src/battle/presentation/graphics/Lighting.cs`](../../../../../src/battle/presentation/graphics/Lighting.cs) | [subplans/src-battle-presentation-graphics-Lighting.cs.plan.md](subplans/src-battle-presentation-graphics-Lighting.cs.plan.md) | blank-backdrop |
| [`src/battle/presentation/PointMapping.cs`](../../../../../src/battle/presentation/PointMapping.cs) | [subplans/src-battle-presentation-PointMapping.cs.plan.md](subplans/src-battle-presentation-PointMapping.cs.plan.md) | world-mapping |
| [`src/battle/presentation/picking/GridPick.cs`](../../../../../src/battle/presentation/picking/GridPick.cs) | [subplans/src-battle-presentation-picking-GridPick.cs.plan.md](subplans/src-battle-presentation-picking-GridPick.cs.plan.md) | world-mapping |
| [`src/battle/presentation/ui/MovePreviewRings.cs`](../../../../../src/battle/presentation/ui/MovePreviewRings.cs) | [subplans/src-battle-presentation-ui-MovePreviewRings.cs.plan.md](subplans/src-battle-presentation-ui-MovePreviewRings.cs.plan.md) | move-preview-rings |
| [`src/battle/presentation/BattleFrameBuilder.cs`](../../../../../src/battle/presentation/BattleFrameBuilder.cs) | [subplans/src-battle-presentation-BattleFrameBuilder.cs.plan.md](subplans/src-battle-presentation-BattleFrameBuilder.cs.plan.md) | move-preview-rings |
| [`src/battle/presentation/Interaction/InteractionState.cs`](../../../../../src/battle/presentation/Interaction/InteractionState.cs) | [subplans/src-battle-presentation-Interaction-InteractionState.cs.plan.md](subplans/src-battle-presentation-Interaction-InteractionState.cs.plan.md) | controller-cycle, hints-accessor |
| [`src/battle/presentation/scene/BattleController.cs`](../../../../../src/battle/presentation/scene/BattleController.cs) | [subplans/src-battle-presentation-scene-BattleController.cs.plan.md](subplans/src-battle-presentation-scene-BattleController.cs.plan.md) | blank-backdrop, controller-cycle |
| [`src/battle/presentation/ui/MovementSelection.cs`](../../../../../src/battle/presentation/ui/MovementSelection.cs) | [subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md](subplans/src-battle-presentation-ui-MovementSelection.cs.plan.md) | controller-cycle |
| [`src/battle/presentation/ui/Combat.cs`](../../../../../src/battle/presentation/ui/Combat.cs) | [subplans/src-battle-presentation-ui-Combat.cs.plan.md](subplans/src-battle-presentation-ui-Combat.cs.plan.md) | hints-accessor |
| [`grim-space.Tests/Presentation/MovePreviewRingTests.cs`](../../../../../grim-space.Tests/Presentation/MovePreviewRingTests.cs) | [subplans/grim-space.Tests-Presentation-MovePreviewRingTests.cs.plan.md](subplans/grim-space.Tests-Presentation-MovePreviewRingTests.cs.plan.md) | tests |

## Todo rollup

| Id | Summary | Status |
|----|---------|--------|
| blank-backdrop | Diagram backdrop + neutral light | done |
| world-mapping | ToCoord + GridPick | done |
| move-preview-rings | BuildRingTable on frame | done |
| controller-cycle | Depthkey, Raywalk, ring pick, click modes | pending |
| hints-accessor | Ring hints + HoveredMoveIndex | pending |
| tests | MovePreviewRingTests | done |
| format-momentum-home | FormatMomentum ? CombatHints (optional) | pending |
