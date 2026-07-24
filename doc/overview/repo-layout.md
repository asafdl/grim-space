# Repo layout

High-level tree of grim-space (not every file).

```
grim-space/
├── project.godot, grim-space.csproj, grim-space.sln
├── scenes/              # main.tscn → battle.tscn
├── assets/              # models (asteroids)
├── doc/
├── grim-space.Tests/
└── src/
    ├── core/            # Session, Engine, Simulation, Timeline, IAction/IEffect
    ├── math/grid/
    ├── units/
    ├── run/
    └── battle/
        ├── actions/, effects/, board/, movement/, weapons/
        ├── runtime/, ai/, turn/, environment/
        ├── BattleOrchestrator.cs
        └── presentation/
```

Root **README.md** / **AGENTS.md** for humans and agents.

See also: [high-level.md](high-level.md), [entry/run-entry.md](../entry/run-entry.md)
