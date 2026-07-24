# Step A.3a — BattleOrchestrator.FromEncounter

## Tree (snapshot)

```
│   -> A.3a  FromEncounter                               [now]
│       ├── build grid / timeline / hazards
│       ├── Factory → units
│       ├── BattleBoard + Engine
│       └── BeginTurn
```

## Bridge

`BattleController._Ready` passes **`Session.Instance.CurrentEncounter`** into this static method.

## Explanation

Turns **`Encounter`** (run data) into live **`BattleOrchestrator`**: 3D grid, timeline, hazard registration, units from spawns, **`BattleBoard`**, **`Engine`**, active player, **`BeginTurn()`** (planning sim).

See `src/battle/BattleOrchestrator.cs` lines 70–105.

## Quiz (answered)

1. **`BattleOrchestrator`** ✓  
2. **`Encounter.DevDefault`** (method **`DevDefault`**) ✓

## Reference

**XCOM** — mission data → tactical map spawn.
