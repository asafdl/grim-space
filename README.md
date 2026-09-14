<div align="center">
  <img src="assets/icon.png" alt="react-conditional-ui logo" width="120" />
</div>

# grim-space

Early-stage 3D roguelike space game with tactical turn-based combat. The current focus is a combat prototype on a discrete 3D grid; run and progression layers are thin placeholders.

Gameplay systems in code come first — placeholder visuals, APIs that change, and design details still being proven out.

## Code layout

Dependencies flow **presentation → battle → data**. Battle rules are plain C# (testable without Godot); Godot handles rendering, input, and scene wiring.

| Area | Intent |
|------|--------|
| `src/battle/` | Combat rules — grid, movement, weapons, AI, turn orchestration |
| `src/core/actions/` | Shared action / effect / timeline primitives; battle simulation and commit |
| `src/units/`, `src/run/` | Unit definitions; encounter and run scaffolding |
| `src/battle/presentation/` | Godot layer — scene, UI, camera, graphics, picking |

## Generic simulation kernel

`src/core/` is a Godot-free kernel shared by tactical battle and the strategic star-system. It defines execution mechanics, not game rules.

### Canonical ownership

| Owner | Responsibility | Must not |
|-------|----------------|----------|
| [`Engine<TWorld, TRuntime>`](src/core/engine/Engine.cs) | Owns the live world, live actor runtimes, `WorldVersion`, commits, scheduling, timeline advancement, and listeners | Plan actions or contain game-specific rules |
| [`Simulation<TWorld, TRuntime>`](src/core/engine/Simulation.cs) | Owns a forked planning world/runtime, proposed actions, preview effects, and undo state | Mutate the engine or append to its live timeline |
| [`ExecutionAgent<TWorld, TRuntime>`](src/core/engine/ExecutionAgent.cs) | Produces one actor's proposed `ActionBatch` when the orchestrator allows it to work | Commit actions or choose global execution order |
| Subsystem orchestrator | Activates agents, consumes batches, orders commits, advances ticks, and reports world updates | Reimplement action rules or mutate domain state directly |
| [`Timeline`](src/core/timeline/Timeline.cs) | Stores committed entries by tick and actions scheduled for future ticks | Act as a second world-state model |

Each rules system supplies:

- a durable `IWorld<TWorld>` containing its domain state and timeline;
- a resettable, forkable `IRuntimeContext<TRuntime>` for per-actor working state.

### Required execution flow

```text
live Engine
    → CreateSimulation() forks world + actor runtimes at Tick and WorldVersion
    → SimulationExecutionAgent plans against that fork
    → agent publishes ActionBatch through ActionBatchSink
    → subsystem orchestrator accepts and orders the batch
    → Engine.Commit resolves the actions against live state
    → effects mutate live world/runtime
    → actions + records are appended to the live timeline
    → WorldVersion increments and typed listeners run
    → orchestrator reports the world update; simulation-backed agents refork
```

The simulation action queue is a proposal, **not** timeline history. `Simulation.TryCommit` only returns that proposal; despite its name, it does not cross into live state. Only the orchestrator's call to `Engine.Commit` does that.

### Actions, effects, records, and listeners

| Type | Meaning | Authority |
|------|---------|-----------|
| **Action** ([`IAction`](src/core/actions/IAction.cs)) | An actor's intent, such as moving, firing, or accepting a contract | Identifies the actor and its definition; never mutates state |
| **Action definition** ([`IActionDef`](src/core/actions/IActionDef.cs)) | Rules for one action family | Discovers candidates, checks possibility/legality, and resolves an action into effects |
| **Effect** ([`IEffect`](src/core/actions/IEffect.cs)) | One atomic state transition | Mutates only the supplied world/runtime; supports undo when used in undoable planning |
| **Record** ([`IRecord`](src/core/timeline/IRecord.cs)) | A fact emitted by an effect, such as an impact, spawn, or transaction | Describes an outcome; never mutates state and is not a replay command |

One action may resolve to many effects; one effect may emit zero or more records:

```text
Action (what was attempted)
    → ActionDef.Resolve
    → Effects (what changes)
    → Records (what happened)
```

Simulation runs the same definitions and effects against forked state. Preview records stay inside that simulation and never enter the live timeline. During `Engine.Commit`, the engine resolves again against live state, then appends each action followed by its emitted records. Actions and records both implement `ITimelineEntry`.

### Typed listeners

[`Engine.Subscribe<TEntry>`](src/core/engine/Engine.cs) observes newly committed timeline entries:

- Matching uses the entry's **exact runtime type**; subscribing to a base type or interface does not receive derived entries.
- Both action types and record types can be observed.
- Callbacks run synchronously, in entry order, after the entire batch has mutated live state, entered history, and incremented `WorldVersion`.
- A callback sees the batch's final world state, not the intermediate state after its matching entry.
- Preview execution never notifies live listeners.
- `Schedule` does not notify. The action becomes observable when `AdvanceTick` commits it.
- There is no history replay for late subscribers.
- The returned subscription must be disposed with its owner.

Listeners must not become a second mutation path. They may update external presentation state or enqueue follow-up actions for the orchestrator, but should not edit the world directly or recursively call `Engine.Commit`. Listener exceptions propagate after the live commit has already occurred, so callbacks should remain small and reliable.

### Reusable kernel tools

These APIs are generic capabilities. Their current consumers do not limit where they may be reused.

#### Planning and queue tools

| Tool | Contract |
|------|----------|
| `Simulation.Peek(action)` | Applies one legal action to copies of the current preview world/runtime and returns a `PeekFrame` with projected state and records. It does not change the simulation or enqueue the action. |
| `Simulation.Fork()` | Creates an independent branch preserving the current proposed queue. Use for speculative branches. |
| `Simulation.ForkFromAnchor()` | Creates a clean branch from the simulation's original engine snapshot. |
| `Simulation.ReplayWorld(depth)` | Reconstructs world state from the anchor through a queue prefix without changing the simulation. |
| `Simulation.RecordsFor(index)` | Returns preview records retained for an action enqueued with `keepRecords: true`. |
| [`IActionInvariants`](src/core/actions/IActionInvariants.cs) | Evaluates whether the whole proposed sequence is `Ok`, `Incomplete`, or `Impossible`. This is separate from per-action legality. `TryCommit` accepts only `Ok`; planners may continue an `Incomplete` sequence and should prune an `Impossible` one. |
| [`IActionStreamline`](src/core/actions/IActionStreamline.cs) | Rewrites or compacts an action queue through a caller-provided legality predicate. It is a generic queue-normalization hook, not a battle rule. |
| [`IActionSink`](src/core/actions/IActionSink.cs) | Minimal enqueue/undo/commit interface for callers that should control planning without depending on a concrete execution agent. |

#### Actor and action-production tools

| Tool | Contract |
|------|----------|
| [`ActorRuntimes<TRuntime>`](src/core/engine/ActorRuntimes.cs) | Lazily owns runtime state by actor id and can fork or reset all registered runtimes. |
| [`EmptyRuntime`](src/core/engine/EmptyRuntime.cs) | Runtime implementation for systems that need no per-actor scratch state. |
| [`IActorStateWorld<TState, TWorld>`](src/core/engine/IActorStateWorld.cs) | Optional world capability enabling typed `Simulation.StateOf<TState>(actorId)` lookup. |
| [`ActionBatchSink`](src/core/engine/ActionBatchSink.cs) | Per-actor handoff between producers and orchestrators. Supports polling or async waiting and carries either a batch or a production failure. Each actor has one bounded latest-result slot, so a newer unread result replaces the older one. |
| `ExecutionAgent.SetCanWork` | Activation gate controlled by the orchestrator. At most one batch should be in flight for an activation. |
| `ExecutionAgent.OnWorldUpdated` | Refresh signal from the orchestrator. A `SimulationExecutionAgent` reforks and replans from current live state. |

#### Timeline tools

| Tool | Contract |
|------|----------|
| `Timeline.History(tick)` | Non-destructive read of committed entries for one tick. |
| `Timeline.HistoryByActor(tick)` | Non-destructive action-only view grouped by actor. Records are excluded. |
| `Timeline.DrainUntil(tick)` | Destructively removes and returns committed history through the supplied tick as `TimelineBatch` values. |
| `Timeline.Clone()` | Copies clock, pending actions, and history. |
| `Timeline.CloneSnapshot()` | Copies clock and pending actions but omits committed history; useful for simulation snapshots. |
| Timeline GC | `Engine` periodically removes old committed history according to `TimelineGcOptions`. It does not remove pending actions or mutate domain state. |

### Rules for contributors and agents

- **MUST** mutate live domain objects, after engine construction, only through effects applied during `Engine.Commit`.
- **MUST** route post-construction timeline changes through `Engine.Commit`, `Engine.Schedule`, `Engine.AdvanceTick`, or an effect currently being applied by the engine. Direct timeline setup is reserved for world construction.
- **MUST NOT** let an execution agent or simulation call `Engine.Commit`.
- **MUST** keep action definitions deterministic for the supplied world/runtime.
- **MUST NOT** mutate live domain objects directly from an action, record, presentation listener, or orchestrator.
- **MUST** keep every effect's mutations inside the supplied world/runtime.
- **MUST** implement complete `Undo` for effects reachable from undoable planning or [`ActionSearch`](src/core/dfs/ActionSearch.cs). A no-op `Undo` is valid only when that effect can never be dequeued.
- **MUST** treat listener callbacks as post-commit notifications. Queue follow-up actions for the orchestrator instead of committing recursively.
- **MUST** notify simulation-backed agents after live state changes so they refork before producing another batch.
- **MUST NOT** assume `Engine.Commit` revalidates legality or rejects stale simulations. The orchestrator must consume only the expected actor's current batch.
- **MUST NOT** treat timeline records as authoritative state. Query the live world for current state.

The base `ExecutionAgent` may support deterministic producers that read live state instead of using a simulation. They still publish an `ActionBatch` and have no commit authority.

`ActionSearch` forks the supplied simulation, explores by enqueueing and undoing on that fork, and returns copied search frames. It must never mutate the caller's simulation or the live engine.

## Battle (current intent)

### Grid & positioning

Combat happens on a 3D cell lattice. Each ship has a facing; movement and weapons are expressed in ship-local directions. Range, arcs, cover, and hazards are all grid-based — positioning is meant to matter.

### Turn loop

The player queues a full turn up front, previews the outcome, then commits. The enemy acts when the turn resolves. During simulation, actions can be queued and undone; permanent state changes happen on commit and resolution.

### Actions, effects, and timeline

Battle logic is split into three cooperating ideas:

| Concept | Role |
|---------|------|
| **Actions** | Declarative intent — move, fire, resolve a delayed hit, etc. |
| **Effects** | Atomic state changes — damage, movement, AP, hazards, scheduling future work |
| **Timeline** | When things happen — discrete ticks ordering player, enemy, and delayed events |

Typical flow: **queue → commit to timeline → step → repeat**. Player input uses a throwaway `Simulation` fork (undoable preview). On commit, actions are scheduled on the live timeline and stepped — that becomes world truth. Enemy AI then `CreateSimulation()` from that live state, previews with `StepPreview` to peek ahead, commits its queue, and the orchestrator steps through the rest of the turn. Presentation observes `TickResult`; it does not own rules.

### Architecture (rules layer)

Combat state is split into two buckets, passed together through actions and effects:

| Bucket | Holds |
|--------|--------|
| **World** (`BattleWorld`) | Durable battlefield snapshot — units, grid occupancy, hazards, timeline |
| **Runtime** (`ActorRuntimes<ActorRuntime>`) | Per-actor turn scratch — queued path state, yaw tags, weapon-use flags, etc. |

**Actions** answer “is this legal?” and “what effects does it produce?” against `(world, runtime)`. **Effects** apply the actual mutations. There is no separate context object or slice layer — callers pass world and runtime directly.

**Action defs** (`MoveDef`, `HeadingDef`, …) own discovery and legality for a family of actions. **Capabilities** maps unit type → which defs that ship has; AI and UI start there, then ask each def what is possible. Movement paths are discovered through the move def; other actions come from each def’s `Discover`.

**Engine** owns the live `World`, `ActorRuntimes`, and a monotonic `WorldVersion` (incremented on every schedule/step). `CreateSimulation()` stamps the current version on the fork. `TryScheduleFromSimulation` rebases stale sims (save actions → refork → replay) before committing; failed replay is rejected.

**Simulation** uses `Simulation<BattleWorld, ActorRuntime>`: anchor world + anchor runtimes, preview forks replayed on each enqueue, action list with undo groups. **Commit** returns the queued `IReadOnlyList<IAction>` for scheduling; only the orchestrator writes to the live timeline.

**BattleOrchestrator** builds the encounter, owns turn flow (sequential commit: player → step → AI → step → upkeep), win rules, and presentation hooks. Presentation (`BattleUi`, `BattleController`, `BattleFrameBuilder`) reads preview state and highlights legal options; it does not implement rules. Tests hit the same orchestrator and defs as the game, without Godot.



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
