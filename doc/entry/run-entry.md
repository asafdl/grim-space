# Run entry (Godot)

**Run:** Godot F5 or `dotnet build` then play; main scene is set in `project.godot`.

**Chain**

1. `run/main_scene` → `scenes/main.tscn` (root `Node3D` only instances `scenes/battle.tscn`).
2. Autoload `Session` (`src/core/Session.cs`) runs before scenes: `_Ready` calls `StartNewRun()`.
3. That builds placeholder run data (`Run.State.CreateDevDefault()`) and a fight setup (`Encounter.DevDefault(seed)`).
4. `BattleController` (in battle scene) reads `Session.Instance.CurrentEncounter` and builds `BattleOrchestrator.FromEncounter(...)`.

**CLI without Godot:** `dotnet test` exercises battle rules only; no scene tree.

**Commands (repo root):** `dotnet build`, `dotnet test`.
