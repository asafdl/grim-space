# Chunk 1 — repo root

## Confirmed

- **`grim-space.Tests/`** — automated tests for battle/rules. **`dotnet test`** at repo root; **no Godot**.
- **Root workflows:** **Godot F5** (run/debug; open **`project.godot`**), **`dotnet build`**, **`dotnet test`**.
- **`main.tscn`** → instances **`battle.tscn`** only (no sector map yet).
- **Turn-based rules:** **`src/battle/`** + **`src/core/`**. **Encounter setup:** **`src/run/`** (party, spawns, asteroids). **`BattleOrchestrator.FromEncounter`** turns encounter data into live battle state and runs the turn pipeline.
