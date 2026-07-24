# High level

**Product (intent):** 3D space roguelike with turn-based tactical combat on a 3D grid. Most code is a combat prototype; run/map/meta are thin placeholders.

**Stack:** Godot 4.7 .NET, C#, Jolt. Primitive visuals; rules in plain C# (`dotnet test` without Godot).

**Layers:** presentation (scenes, UI, camera) → battle (grid, actions, timeline, AI) → core engine (simulation, planning fork) → run/units (encounter, party, spawns).

**Loop:** plan turn with preview/undo → commit → timeline runs player → enemy → upkeep.

**Start:** F5 → `main.tscn` → battle; autoload `Session` builds dev run + encounter.

See also: [entry/run-entry.md](../entry/run-entry.md)
