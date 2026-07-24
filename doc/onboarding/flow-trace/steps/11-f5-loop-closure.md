# Step A.8 — F5 battle loop (closure)

## Tree (snapshot)

```
F5 battle loop — CLOSED
Session → main/battle → FromEncounter → BeginTurn (preview)
  → plan TryEnqueue → EndTurn → TryScheduleFromSimulation → Step → sink
  → EnemyPlanner → Step → BeginTurn …
```

## Bridge

**A.7** finished the commit edge. This step ties **A** into one loop you can re-walk from memory.

## Explanation

| Phase | Where rules run | Godot’s job |
|--------|-----------------|-------------|
| Hover / aim | Mostly read **preview** or options | **`_Process`**, input, UI |
| Planning | **`TryEnqueue`** on **preview** | Show ghosts, legal tiles |
| End Turn | **Live** timeline **`Schedule` + `Step`** | **`OnActionApplied`**, **`Refresh`** |
| Between turns | **`BeginTurn`** → new **preview** | Re-enable planning when **`!IsResolving`** |

Headless **`dotnet test`** exercises the middle columns without **`BattleController`**.

Entry files: **`project.godot`** (**`Session`**), **`scenes/main.tscn`**, **`BattleOrchestrator`**, **`src/core/engine/`**.

## Quiz (answered)

1. **Same seed** / deterministic replay — tests assert exact outcomes repeatedly ✓ (also: legality, timeline, no Godot)  
2. **No** — **`CanAct`** requires **`!IsResolving`** ✓

## Reference

**XCOM** — planning phase vs playback phase is the same split as preview vs live **`Step`**.
