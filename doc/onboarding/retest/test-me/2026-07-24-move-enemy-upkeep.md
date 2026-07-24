# Test-me — move enqueue, enemy preview, stale sim, round upkeep

**Date:** 2026-07-24  
**Score:** 5 / 5

## Citation anchors (message 1)

- `src/battle/presentation/ui/BattlePresenter.cs` — `TryQueueMove` → orchestrator
- `src/battle/BattleOrchestrator.cs` — `TryEnqueueMovePath`, `ExecuteTurn` (enemySim, `ScheduleRoundUpkeep`)
- `src/battle/actions/MoveStepAction.cs` — `MoveDef.StepsFromPath`
- `src/core/engine/Simulation.cs` — `IsStale`
- `src/core/engine/Engine.cs` — `EnsureCurrent` / `Rebase`

## Confirmed

| # | Question | Answer |
|---|----------|--------|
| 1 | Path → `MoveStepAction` list | **`MoveDef.StepsFromPath`** |
| 2 | `EnemyPlanner` world | **Preview from `CreateSimulation` (enemySim)** |
| 3 | `IsStale` | **`Simulation.WorldVersion` ≠ engine current** |
| 4 | Round upkeep on live timeline | **`ScheduleRoundUpkeep` after enemy `Step` in `ExecuteTurn`** |
| 5 | Player move confirm | **`TryEnqueueMovePath(option)` on preview session** |
