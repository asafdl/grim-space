# grim-space — Agent Guide

## IMPORTANT
This file is manually edited!

## What this is

**grim-space** is a 3D roguelike space game with **tactical RPG (TRPG)** combat — turn-based squad fights on grid-like battle spaces, wrapped in a procedural run structure (rooms/sectors, permadeath or run-based progression, meta unlocks TBD).

The near-term goal is **gameplay systems in code**, not polish. Use primitive/placeholder visuals (colored meshes, CSG, debug draws, UI labels) and skip bespoke art and sound until core loops are fun.

## Tech stack

| Layer | Choice |
|-------|--------|
| Engine | Godot **4.7.1** (Forward Plus renderer) |
| Language | **C#** (`net1.0`, `Godot.NET.Sdk/4.7.1`) |
| Physics | Jolt (3D) |
| Dev OS | macOS primary; Linux secondary |
| Editor | Godot .NET build (`godot-mono`); Cursor/VS Code + C# extension for scripting |

### Prerequisites (local machine)

- `dotnet` SDK 10+
- `godot-mono` (not the standard Godot build — C# requires the .NET edition)
- Open this repo root in Cursor; Godot generates `grim-space.sln` / `grim-space.csproj`

### Build & run

1. Open repo root in Godot (`project.godot` lives at top level).
2. After changing exported properties, signals, or tool scripts: **Build** (top-right) or `dotnet build`.
3. Set **Project → Project Settings → Application → Run → Main Scene** once `main.tscn` exists.

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
- Rebuild after adding `[Export]` fields or new signals so the editor sees them.

### Placeholder assets

- Meshes: `MeshInstance3D` + `BoxMesh` / `SphereMesh` / `CylinderMesh`.
- Materials: solid `StandardMaterial3D` colors per faction/role.
- UI: `Control` nodes + labels for HP, AP, turn order, logs.
- Audio: none for now unless needed to test a timing mechanic.

## References

- [Godot 4 C# basics](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Godot 4 C# API differences from GDScript](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_differences.html)
