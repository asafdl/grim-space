# Chunk 2 — `src/` top level

## Confirmed

- **`Engine`** + **`Simulation`** live in **`src/core/engine/`**.
- **`Encounter.DevDefault`** **builds** a placeholder encounter (seed, spawns, asteroids) — does not load from disk.
- **`Session.StartNewRun`** calls **`DevDefault`** and stores **`CurrentEncounter`**.
- **`BattleOrchestrator.FromEncounter`** consumes that encounter (from **`BattleController`** when the battle scene loads).
- **Three folders:** `src/units/` = identity + type stats; `src/run/` = encounter setup (spawns, seed, asteroids); `src/battle/units/` = live `Unit` + `State` (position, AP, HP, momentum, facing).

## Still open

_(none)_
