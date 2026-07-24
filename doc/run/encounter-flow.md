# Encounter flow and seed

## Layers (quiz correction)

| Path | Role |
|------|------|
| `src/units/` | Run-level **definitions** — `Instance`, `Stats`, enums (identity before a fight). |
| `src/run/` | **Encounter setup** — `Party`, `Encounter`, spawns, asteroid field generation. Not orchestrator/engine. |
| `src/battle/units/` | **Live battle** units — `State`, `Factory`, `Unit` shells on the board. |
| `src/core/engine/` | **`Engine`**, **`Simulation`**, timeline — generic planning/commit machinery. |
| `src/battle/BattleOrchestrator.cs` | Turn pipeline; **`FromEncounter`** builds the fight from an `Encounter`. |

## Seed

- `Session.StartNewRun()` sets `CurrentEncounter = Encounter.DevDefault(Random.Shared.Next())`.
- The **same int seed** is stored on `Encounter.Seed` and passed into `AsteroidFieldGenerator` so **asteroid layout is reproducible** for that run. Spawns are fixed in code today (not RNG’d from seed).
- Tests often pass an explicit seed (e.g. `42`) to `DevDefault(seed)` for deterministic boards.

## Who calls what

1. **Autoload `Session`** — `_Ready` → `StartNewRun()` → dev `Run` state + **`Encounter.DevDefault(seed)`** (builds spawns + board hazards in memory; no file load).
2. **`BattleController`** (battle scene) — `BattleOrchestrator.FromEncounter(Session.Instance.CurrentEncounter)` creates units, board, engine, and starts planning.

`Session` does **not** call `FromEncounter`; the battle scene does when it loads.

See also: [entry/run-entry.md](../entry/run-entry.md)
