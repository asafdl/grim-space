# grim-space

Early-stage 3D roguelike space game with tactical turn-based combat. The current focus is a combat prototype on a discrete 3D grid; run and progression layers are thin placeholders.

Gameplay systems in code come first — placeholder visuals, APIs that change, and design details still being proven out.

## Code layout

Dependencies flow **presentation → battle → data**. Battle rules are plain C# (testable without Godot); Godot handles rendering, input, and scene wiring.

| Area | Intent |
|------|--------|
| `src/battle/` | Combat rules — grid, movement, weapons, AI, turn orchestration |
| `src/core/actions/` | Shared action / effect / timeline primitives; battle planning and commit |
| `src/units/`, `src/run/` | Unit definitions; encounter and run scaffolding |
| `src/battle/presentation/` | Godot layer — scene, UI, camera, graphics, picking |

## Battle (current intent)

### Grid & positioning

Combat happens on a 3D cell lattice. Each ship has a facing; movement and weapons are expressed in ship-local directions. Range, arcs, cover, and hazards are all grid-based — positioning is meant to matter.

### Turn loop

The player plans a full turn up front, previews the outcome, then commits. The enemy acts when the turn resolves. During planning, actions can be queued and undone; permanent state changes happen on commit and resolution.

### Actions, effects, and timeline

Battle logic is split into three cooperating ideas:

| Concept | Role |
|---------|------|
| **Actions** | Declarative intent — move, fire, resolve a delayed hit, etc. |
| **Effects** | Atomic state changes — damage, movement, AP, hazards, scheduling future work |
| **Timeline** | When things happen — discrete ticks ordering player, enemy, and delayed events |

Typical flow: **plan → stage → execute → upkeep**. Presentation observes results; it does not own rules.

### Architecture (rules layer)

Combat state is split into two buckets, passed together through actions and effects:

| Bucket | Holds |
|--------|--------|
| **World** (`BattleBoard`) | Durable battlefield snapshot — units, grid occupancy, hazards, timeline |
| **Runtime** (`ActorSession`) | Per-actor turn scratch — queued path state, yaw tags, weapon-use flags, etc. |

**Actions** answer “is this legal?” and “what effects does it produce?” against `(world, runtime)`. **Effects** apply the actual mutations. There is no separate context object or slice layer — callers pass world and runtime directly.

**Action defs** (`MoveDef`, `HeadingDef`, …) own discovery and legality for a family of actions. **Capabilities** maps unit type → which defs that ship has; AI and UI start there, then ask each def what is possible. Movement paths are discovered through the move def; other actions come from each def’s `Discover`.

**Planning** uses `Simulation<BattleBoard, ActorSession>`: an anchor world (live state at turn start), a preview fork (replayed queue), and an action list with undo groups. **Commit** runs the timeline — player queue, enemy plan, delayed resolves, round upkeep — via `BattleOrchestrator`.

Presentation (`View`, `BattlePresenter`, scene) reads preview state and highlights legal options; it does not implement rules. Tests hit the same orchestrator and defs as the game, without Godot.

### Movement & momentum

Movement is discrete and ship-relative, with action points as the primary turn budget. Momentum gives ships inertia — forward flight gets cheaper at speed, braking and lateral moves cost more. The feel target is thrust and drift, not free grid teleportation.

### Combat

Weapons are being shaped around complementary roles: area denial and delayed threats (missiles, flak) versus direct finishers (railgun). Hazards telegraph danger during the turn so both sides can react before damage lands. Tuning, weapon count, and exact constraints are still in flux.

---

## Local development

### Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Godot 4.7 .NET build](https://godotengine.org/download) — pick the **.NET** download for your OS (not the standard build; C# requires the .NET edition)

`dotnet build` and `dotnet test` work on all platforms without launching Godot.

### Linux

- Install the .NET SDK via your distro packages or [Microsoft's Linux install docs](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
- Download the Godot **.NET** Linux binary (x86_64). If you run it from an extracted archive, `chmod +x` the executable.
- Open the repo root in Godot, or add the binary to your `PATH` and run it against `project.godot`.

### Windows

- Install the .NET SDK, then the Godot **.NET** Windows build.
- Open the repo root in Godot (`Godot_v4.x-stable_mono_win64.exe` → import/open `project.godot`).
- `dotnet build` and `dotnet test` work from PowerShell or cmd in the repo root.

### Setup & run

1. Open the repo root in your editor and in Godot (`project.godot`).
2. Build: Godot editor **Build** button, or `dotnet build`.
3. Run in Godot (F5). Main scene: `scenes/main.tscn`.

Rebuild after changing exported properties, signals, or tool scripts.

### Tests

Battle logic tests live in `grim-space.Tests/` and run without Godot:

```bash
dotnet test
```

Use tests for rules and orchestration; use Godot for presentation and full battle flow.
