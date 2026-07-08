# grim-space — Agent Guide

## What this is

**grim-space** is a 3D roguelike space game with **tactical RPG (TRPG)** combat — turn-based squad fights on grid-like battle spaces, wrapped in a procedural run structure (rooms/sectors, permadeath or run-based progression, meta unlocks TBD).

The near-term goal is **gameplay systems in code**, not polish. Use primitive/placeholder visuals (colored meshes, CSG, debug draws, UI labels) and skip bespoke art and sound until core loops are fun.

## Tech stack

| Layer | Choice |
|-------|--------|
| Engine | Godot **4.7** (Forward Plus renderer) |
| Language | **C#** (`net8.0`, `Godot.NET.Sdk/4.7.0`) |
| Physics | Jolt (3D) |
| Dev OS | macOS primary; Linux secondary |
| Editor | Godot .NET build (`godot-mono`); Cursor/VS Code + C# extension for scripting |

### Prerequisites (local machine)

- `dotnet` SDK 8+
- `godot-mono` (not the standard Godot build — C# requires the .NET edition)
- Open this repo root in Cursor; Godot generates `grim-space.sln` / `grim-space.csproj`

### Build & run

1. Open repo root in Godot (`project.godot` lives at top level).
2. After changing exported properties, signals, or tool scripts: **Build** (top-right) or `dotnet build`.
3. Set **Project → Project Settings → Application → Run → Main Scene** once `main.tscn` exists.

## Repo layout

```
grim-space/
├── project.godot          # Godot project config
├── grim-space.sln         # C# solution (commit)
├── grim-space.csproj      # C# project (commit)
├── AGENTS.md              # This file
├── scenes/                # .tscn scene files (create as needed)
├── scripts/               # C# gameplay code (create as needed)
├── resources/             # .tres data (stats, abilities, loot tables)
└── .godot/                # Editor cache — gitignored, do not commit
```

Current state is early bootstrap: starter `Node3d.cs`, no main scene yet. Prefer the structure above as files are added.

## Game design pillars (for implementation decisions)

1. **Roguelike run** — procedural sectors/rooms, escalating difficulty, run-scoped resources.
2. **TRPG combat** — turn order, action points or move+action economy, positioning matters, LoS/cover TBD.
3. **Space setting** — ships, stations, sectors, vacuum/terrain as mechanics (not just theme).
4. **Code-first prototyping** — if a system can be proven with primitives and logs, do that before assets.

Defer until later: final art, animation, VFX, music, SFX, narrative writing, balancing pass.

## Coding conventions

### Godot + C#

- Scripts: `public partial class Foo : Node` (or appropriate base); **class name must match `.cs` filename**.
- Godot API is **PascalCase** in C# (`GetNode`, `Position`, `GD.Print`).
- Stringly calls to engine APIs use **snake_case** names: `CallDeferred(MethodName.AddChild)` or `PropertyName` constants — not C# PascalCase strings.
- Prefer **composition** (scene tree of small nodes) over deep inheritance.
- Put data in **Resources** (`.tres` + C# `Resource` subclasses) where designers/agents will tune stats; keep nodes thin.
- Rebuild after adding `[Export]` fields or new signals so the editor sees them.

### Scope discipline

- Smallest change that proves the loop. No speculative frameworks.
- One concern per class/scene when possible.
- Comments only for non-obvious game rules or engine quirks.

### Placeholder assets

- Meshes: `MeshInstance3D` + `BoxMesh` / `SphereMesh` / `CylinderMesh`.
- Materials: solid `StandardMaterial3D` colors per faction/role.
- UI: `Control` nodes + labels for HP, AP, turn order, logs.
- Audio: none for now unless needed to test a timing mechanic.

## Architecture sketch (target, not all built yet)

```
Run (roguelike meta)
 └── Sector map (nodes: combat, shop, event, boss)
      └── Tactical battle scene
           ├── Grid / hex board
           ├── Unit actors (player squad + enemies)
           ├── Turn manager (initiative, phases)
           └── Action resolver (move, attack, abilities)
```

Keep battle logic **deterministic** where possible (seeded RNG) to simplify debugging and future replay/netcode.

## First TODOs

Ordered for a code-first vertical slice — ship one playable loop before breadth.

1. **Project scaffold** — `scenes/main.tscn` as main scene; folder layout (`scenes/`, `scripts/`, `resources/`); retire or relocate bootstrap `Node3d.cs`.
2. **Tactical grid** — data structure for cell coords, occupancy, walkability; debug draw (lines/quads) for grid in 3D.
3. **Unit prototype** — `Unit` resource + scene (HP, position on grid, team); spawn player + enemy placeholders on grid.
4. **Turn manager** — round/initiative queue, active unit, end-turn input; combat log via `GD.Print` or on-screen label.
5. **Core actions** — move (pathfinding on grid), basic attack (range, LoS stub, damage); win/lose when one side is eliminated.
6. **Battle scene wiring** — camera (orbit or fixed isometric), click-to-select cell/unit, highlight valid moves/targets.
7. **Run shell (minimal)** — start run → single combat encounter → win/lose screen → restart; no meta progression yet.

**Explicitly later:** art pass, audio, animation, full sector map, inventory/loot, narrative events, save/meta progression, export/packaging.

## What agents should ask before large changes

- Does this serve the **first vertical slice** (one grid battle end-to-end)?
- Can it be tested with **placeholders** only?
- Is the change **deterministic / debuggable** (seed, log, visible state)?

## References

- [Godot 4 C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Godot 4 C# API differences from GDScript](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_differences.html)
