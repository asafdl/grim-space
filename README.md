<div align="center">
  <img src="assets/icon.png" alt="grim-space logo" width="120" />
</div>

# grim-space

Early-stage 3D roguelike space game with strategic star-system navigation and tactical turn-based combat on a discrete 3D grid.

Gameplay systems in code come first — placeholder visuals, APIs that change, and design details still being proven out.

Reusable HUD visuals, variants, palettes, and adoption rules are documented in the [UI kit guide](docs/ui-kit.md).

## Game architecture and boundaries

The game uses one-way command flow into the simulation and derived presentation flow back out:

```text
Godot input
    → view / HUD
    → user-intent translator
    → IActionSink / player execution agent
    → simulation proposal
    → ActionBatch
    → subsystem orchestrator
    → Engine.Commit
    → live world + runtime + timeline
    → presentation frame / replay / synchronized views
    → view / HUD
```

| Layer | Owns | Boundary |
|-------|------|----------|
| **View / HUD** | Godot nodes, rendering, widgets, animation, and raw input events | Displays supplied state and emits interaction events; never changes game state |
| **User-intent translation** | Converting clicks, keys, picks, and UI choices into domain-level action requests | Targets `IActionSink` or another narrow execution-agent API; never decides final legality |
| **Execution agent** | One actor's planning lifecycle and proposed actions | Produces and publishes an action batch; simulation-backed agents validate on a fork; agents never commit |
| **Subsystem orchestrator** | Mode/phase, actor activation, batch ordering, tick advancement, and world-update notifications | The only layer allowed to move accepted proposals into the engine |
| **Domain rules** | World/runtime types, actions, effects, legality, objectives, and other game-specific policy | Must not depend on Godot or presentation |
| **Generic kernel** | Forking, preview, commit, timeline, listeners, and generic search | Must not depend on game-specific domains |

[`UserIntentTranslator`](src/battle/presentation/scene/UserIntentTranslator.cs) and its [star-map counterpart](src/world/star-system/presentation/scene/UserIntentTranslator.cs) are input boundaries, not rule owners. They may translate screen-space picks into domain coordinates or targets and request an action, but action definitions and the execution agent remain responsible for legality. A disabled button or missing highlight is never proof that an action is illegal.

Application controls such as pause, step, speed, scene navigation, and camera movement may call narrow presentation or orchestrator APIs directly because they do not represent an actor's domain action. They still must not mutate world objects.

### State authority

| State | Authority |
|-------|-----------|
| **Live world** | Authoritative current game state: units, positions, resources, contracts, objectives, hazards, and similar domain facts |
| **Live actor runtime** | Authoritative transient rule state owned beside the world by the engine |
| **Live timeline** | Authoritative committed order, delayed actions, and emitted facts; it is history, not a duplicate current-state model |
| **Simulation** | Non-authoritative proposal derived from a live-state fork |
| **Presentation frame / view model** | Disposable projection derived from live state, the active simulation, or replay data |
| **Godot node state** | Rendering and interaction state only |

UI state should be derived from the current world, active simulation, and committed timeline whenever practical. Acceptable local UI state includes focus, hover, selected tab, open overlays, camera pose, drag state, animation progress, and short-lived visual caches. Gameplay facts such as health, position, resources, action points, cooldowns, turn phase, contracts, objectives, and pending domain actions belong to the world/runtime or execution agent and must not be duplicated as independent UI truth.

Presentation may temporarily differ from the live world while showing a planning preview, interpolation, or replay. That state must remain explicitly derived and disposable: completing or canceling the presentation returns to a fresh projection of authoritative state. Rendered node transforms, visibility, labels, and animation state must never feed back into rule evaluation.

### Application seam (`Session`) and run coordination

Godot scenes come and go; tactical battle and the star-system map each own an orchestrator and a live world. [`Session`](src/application/Session.cs) is the **application seam**: a thin autoload that bootstraps logging and settings, holds the current run handle, preloads scenes, navigates between `map.tscn` and `battle.tscn`, and exposes dev tooling. It is **not** where cross-world gameplay rules or simulation commits live.

Run-scoped coordination lives in [`Run.State`](src/run/State.cs). That type is the shared “glue” for one roguelike run: pilot roster and fielded squad, [`ItemInventory`](src/run/items/Inventory.cs), tutorial progress, the active [`StarSystemOrchestrator`](src/world/star-system/StarSystemOrchestrator.cs), optional strategic [`ActiveBattle`](src/battle/encounter/BattleEncounter.cs), pending outcomes, map regeneration, and [`RunTransitionInbox`](src/run/RunTransitionInbox.cs) (buffered star-system resource notifications that must survive scene changes). `CreateNewRun` returns a fully wired instance—subscriptions included—so background-prepared runs do not need extra binding after adoption.

**Run ships and fielding.** All chassis instances live in `State.ItemInventory` with an `OwnerId` (player fleet id or the strategic fleet unit that registered the ship). The fielded battle/strategic squad is not a separate party list: up to four [`RunPilotRegistry.Active`](src/run/RunPilotRegistry.cs) pilots, in order, each with an `AssignedShipId`. [`State.GetFieldedShipIds()`](src/run/State.cs) projects that roster for star-map fleet members, battle deployment, and merchant targeting.

| Component | Owns | Must not |
|-----------|------|----------|
| **`Session`** | Current `Run` adoption/disposal, `BattleReady` → deferred scene change, menu startup, scene preload, dev menu | Query engagement state, build encounters, subscribe to battle/star-system engine listeners, or mutate strategic worlds |
| **`Run.State`** | Orchestrator lifecycle, engine listener subscriptions, run facts (`ActiveBattle`, `PendingBattleOutcome`), inbox binding | Godot nodes, input, or rendering |

#### Records and listeners into shared run state

Subsystem boundaries are crossed with **committed facts**, not with action types. Actions express actor intent (`EngageAction`, `CommitBattleOutcomeAction`, …). Effects apply mutations and may emit [`Record<T>`](src/core/timeline/IRecord.cs) values that describe what **definitively happened** after commit—engagement identity, battle outcome, loot transactions, and similar. [`Engine.Subscribe<TEntry>`](src/core/engine/Engine.cs) delivers those entries only from **live** commits; simulation preview does not notify subscribers.

`Run.State` subscribes to orchestrators and updates **run-level** state in small handlers. It does not replace effects or duplicate world authority: handlers call existing orchestrator APIs (for example `StarSystem.ResolveEngagement`) and set run fields such as `ActiveBattle` or clear them after success.

```text
Star map: EngageAction committed
    → CommitEngagementEffect
    → Record<EngagementCommitted> on live timeline
    → Run.State listener (player participant, dedupe by engagement id)
    → EngagementBattleFactory → ActiveBattle
    → BattleReady event
    → Session → battle.tscn

Battle: terminal outcome committed
    → CommitBattleOutcomeEffect
    → Record<BattleOutcome> on live timeline
    → Run.State listener (via battle orchestrator subscription)
    → StarSystem.ResolveEngagement → ActiveBattle cleared
    → presentation may return to map when ActiveBattle is null
```

Prefer **records over actions** when the consumer needs resolved identity or outcome payload. Example: do not subscribe to `EngageAction` to start a battle—the action does not carry the committed engagement id; subscribe to `Record<EngagementCommitted>` emitted by the effect instead.

Presentation scenes stay thin. [`MapController`](src/world/star-system/presentation/scene/MapController.cs) enqueues domain actions and binds HUD feeds to `Run.Transitions`; it does not construct battles or drive scene transitions. [`BattleController`](src/battle/presentation/scene/BattleController.cs) asks `Run.State` for `CreateActiveBattleOrchestrator()` on the strategic path (outcome subscription is already on the run) or builds a dev-only encounter with no `ActiveBattle`. Dev duel explicitly bypasses strategic run state.

When replacing or regenerating a star-system, `Run.State` disposes old engine subscriptions and rebinds the inbox so listeners do not leak across maps. `Session` only unsubscribes from `BattleReady`, disposes the previous run, assigns the new run, and resubscribes—no special-case wiring for prepared runs.

### Engagement costs

Engaging charges hazard pay for fielded pilots (active pilots with assigned ships). Fleeing instead costs fuel for **all owned pilots**, including inactive and unassigned pilots. Each pilot costs `ceil(10 * 1.5^(level - 1))` fuel, using their XP-derived level, then the individual costs are summed: levels 1–6 cost 10, 15, 23, 34, 51, and 76 fuel respectively.

[`FleeDef`](src/world/star-system/actions/FleeAction.cs) owns the total and affordability check; [`StrategicEconomyConfig`](src/world/star-system/economy/StrategicEconomyConfig.cs) owns the tuning values. Fleeing requires the full amount and records a `flee-fuel` resource transaction. Insufficient fuel leaves engagement pending and requires engaging instead. Escape cannot bypass this restriction. Flee payment precedes settlement of any outstanding travel fuel.

The engagement HUD displays both costs inside their respective buttons, after the label, with hover tooltips. Its cost and available-fuel values are projections from [`EngagementQueries`](src/world/star-system/contact/EngagementQueries.cs), not UI-owned gameplay state.

### Rules for contributors and agents

- **MUST** send user-originated gameplay actions through intent translation and an execution agent; never mutate world objects from UI code.
- **MUST** implement legality in action definitions. UI may explain or pre-filter choices but must not own a parallel legality rule.
- **MUST** rebuild or synchronize presentation after authoritative state changes instead of incrementally maintaining a second game model.
- **MUST** key derived caches by the relevant tick, `WorldVersion`, or source identity and invalidate them when that source changes.
- **MUST** keep UI-only state semantically irrelevant to simulation outcomes.
- **MUST** use committed actions/records or explicit world-update notifications to trigger presentation refreshes; listeners must not perform domain mutation.
- **MUST NOT** read gameplay truth from Godot nodes, rendered transforms, labels, button state, or animation progress.
- **MUST NOT** let domain, engine, action, or effect code depend on presentation types.
- Temporary exceptions must be marked as boundary leaks and must not become dependencies for new code.

## Generic simulation kernel

`src/core/actions/`, `src/core/engine/`, `src/core/timeline/`, and `src/core/dfs/` form a domain-agnostic, Godot-free kernel. Tactical battle and the strategic star-system are two consumers, not concepts built into the kernel.

Domain independence is an architectural requirement. The kernel must remain usable by plain .NET code for non-game workflows that fit its action/effect/timeline model. It must not reference Godot, scenes, presentation, or grim-space domains such as battles, fleets, contracts, and units. Consumers provide their world, runtime, actions, effects, and policies through the generic contracts; integration with Godot belongs outside the kernel.

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

### Actions, effects, and records

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

- **MUST** keep kernel packages free of Godot and grim-space domain dependencies.
- **MUST** express consumer-specific behavior through generic types, kernel interfaces, or code in the consuming system.
- **MUST NOT** place scene lifecycle, autoload, input, rendering, audio, or persisted Godot settings in the kernel.
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

## Tactical battle (current intent)

### Grid & positioning

Combat happens on a 3D cell lattice. Each ship has a facing; movement and weapons are expressed in ship-local directions. Range, arcs, cover, and hazards are all grid-based — positioning is meant to matter.

Strategic engagements deploy as seeded 3D interceptions. The initiating fleet starts outside turn-one direct-fire reach but close enough for contact to develop after one approach turn, and faces the intercepted formation; intercepted ships share one randomized fore/dorsal basis so the fleet reads as traveling together when caught.

Movement uses two independently refilled resources. Action points (AP) pay for translation, while maneuver points (MP) pay for yaw, pitch, and roll. Each chassis owns its AP/MP allotments, supported translations and rotations, and their costs through [`ManeuverabilitySpec`](src/units/maneuvering/ManeuverabilitySpec.cs); battle [`State`](src/battle/units/State.cs) holds the effective profile and current pools. Void bombs derive a zero-MP profile from their launcher snapshot. Path selection ranks exact destination poses by AP cost first, then MP cost.

### Heat, overdrive, and overheat

Chassis that participate in heat ([`HeatSpec`](src/units/heat/HeatSpec.cs) on [`ShipSpec.Heat`](src/units/specs/ShipSpec.cs), copied to `State.HeatProfile`) track a heat pool capped at capacity (`Standard` is 100, clear 20/turn, gain multiplier 1). Opt-out with `HeatSpec.None` (e.g. void bombs)—gate with `Participates` only, never type branches.

Actions emit [`HeatChangeEffect`](src/battle/effects/HeatChangeEffect.cs) deltas (`+10` per AP spent, `+5` per MP spent, `+20` per ability). Positive gains scale by `GainMultiplier` and no-op while overheated. Hitting capacity sets **overdrive** and schedules [`EnterOverheatAction`](src/battle/actions/EnterOverheatAction.cs) for the next tick; that clears overdrive, sets **overheat**, and schedules [`ClearOverheatAction`](src/battle/actions/ClearOverheatAction.cs) (heat to 0). [`TurnUpkeepEffect`](src/battle/effects/TurnUpkeepEffect.cs) cools by `ClearPerTurn` when neither flag is set.

**Overdrive:** Attack-tagged damage through [`OverdriveDamageBonusModifier`](src/battle/pilot-capabilities/OverdriveDamageBonusModifier.cs) gains +80% (including the threshold-crossing attack). Reads the acting actor only. **Overheat:** ship abilities are illegal; movement remains legal and heat gain is frozen.

### Turn loop

Battle vocabulary:

| Term | Meaning |
|------|---------|
| **Turn** | One battle **tick** — `Engine.Tick` / orchestrator `TurnNumber`; full initiative pass, then per-unit [`TurnUpkeepAction`](src/battle/actions/TurnUpkeepAction.cs), then `AdvanceTick`. |
| **Round** (activation-round) | Per-unit scheduled work on [`BattleWorld`](src/battle/world/BattleWorld.cs) — [`ActivationRoundEntry`](src/battle/activation-round/ActivationRoundEntry.cs) rows executed by [`RoundExecutionAgent`](src/battle/round/RoundExecutionAgent.cs) at activation **Start** / **End** (`EUnitActivationPhase`). Not the tick-end upkeep. |
| **Activation** | One unit’s slice in the initiative pass; the orchestrator runs the round agent twice per activation (Start → player/AI batch → End). |

List rows are declarative: **`RunAt`** (`Start` / `End`), optional **`RoundsRemaining`** (countdown in unit activations), **`Sticky`**, and the **`Action`** to publish.

[`BattleOrchestrator`](src/battle/BattleOrchestrator.cs) walks [`UnitRegistry.ActivationOrder`](src/battle/units/UnitRegistry.cs), ordered by descending chassis initiative with stable enlistment-order ties. Each player-controlled activation enters `PlayerTurn`; ending it commits that actor, resolves following AI activations up to the next player-controlled actor, and packages that segment as a [`TurnReplay`](src/battle/TurnReplay.cs). The next player activation starts only after the segment replay completes. Newly spawned actors join the remaining activation order. After the final activation on a tick, turn upkeep runs for every unit, then the tick advances and a new initiative pass begins.

### Battle state

| Bucket | Holds |
|--------|--------|
| **World** (`BattleWorld`) | Durable battlefield snapshot — units, grid occupancy, hazards, timeline |
| **Runtime** (`ActorRuntimes<ActorRuntime>`) | Per-actor battle context — pilot snapshot (`PilotId`, resolved battle capability grants), installed action-effect pipeline, plus turn scratch as needed. Built from encounter spawns (or empty for spawned children / rules actor). |

[`Capabilities`](src/battle/units/Capabilities.cs) maps each actor's installed abilities and **pilot runtime capabilities** to action definitions. Skills are progression sources that grant capabilities; battle and strategic runtimes consume resolved capability grants without depending on how they were acquired. AI and UI start from the installed action-definition set and ask each definition what is legal or discoverable; they do not maintain separate rule tables. Type-based overloads exist only for previews and tests that do not have live actor state.

[`IAction`](src/core/actions/IAction.cs) exposes opaque string **`Tags`** for committed timeline entries; the kernel stores and forwards them only. Pilot capability pipeline registrations declare the action tags they match, and [`ActionTagEffectModifier`](src/core/actions/ActionTagEffectModifier.cs) applies the concrete modifier only when the action contains every required tag. Concrete modifiers transform compatible effect types without naming actions or repeating tag checks. Encounter creation installs the battle pipeline from the deployed pilots' frozen capability snapshots.

Target-side passives publish their current combat condition from the owning actor runtime into cloneable battle unit state when the condition is evaluated. Damage effects read that state directly because effects receive the acting runtime only; they must not reach through the world to the engine's peer-runtime registry.

`BattleOrchestrator` owns phase transitions, activation order, outcome evaluation, and the engine commit boundary. Godot-facing [`BattleController`](src/battle/presentation/scene/BattleController.cs), [`BattleHud`](src/battle/presentation/ui/BattleHud.cs), and [`PresentationFrameBuilder`](src/battle/presentation/PresentationFrameBuilder.cs) consume previews and committed history but do not implement battle rules.

### Ship spec, loadout, and ability spec (`src/units/`)

Combat configuration is split in the units package (no Godot, no battle imports):

| Layer | Type | Meaning |
|-------|------|---------|
| **Ship spec** | [`ShipSpec`](src/units/specs/ShipSpec.cs) (+ [`FighterSpec`](src/units/specs/FighterSpec.cs), etc.) | **Chassis definition only**: [`EType`](src/units/Enums/EType.cs), permissible ability mounts (**kind + facet**), default hull/shield layout. Immutable after spawn. |
| **Ship loadout** | [`ShipLoadout`](src/units/ShipLoadout.cs) | **Configured instance**: installed abilities, max hull/shields, upgrade tiers. Mutates via dock / [`ShipInstance`](src/units/ShipInstance.cs) APIs. |
| **Ability spec** | [`AbilitySpec`](src/units/loadouts/abilities/AbilitySpec.cs) (+ sealed records) | What an ability *does*: canonical baseline values, independent per-stat upgrade caps, per-turn uses or cooldown, and behavior via small interfaces. |

Each [`InstalledAbility`](src/units/loadouts/abilities/InstalledAbility.cs) is one physical mount (**`EAbilityKind` + facet**) with damage/range upgrade deltas. Its effective spec is always derived as canonical baseline + validated deltas; chassis specs do not redefine weapon numbers. A kind may appear on multiple facets; each mount has independent upgrades and uses/cooldown in battle.

Capability interfaces (metadata + helpers, still in units):

- [`IAreaDamage`](src/units/loadouts/abilities/IAreaDamage.cs) — `Damage` and `GetArea(origin, direction, fore, dorsal)` in world grid steps. Battle builds origin/direction from [`BodyFrame`](src/battle/spatial/BodyFrame.cs) and the chosen facet ([`AbilityArea`](src/battle/spatial/AbilityArea.cs) clips to the grid).
- [`ISpawnable`](src/units/loadouts/abilities/ISpawnable.cs) — **`ChildSpec`** is a nested **`ShipSpec`** (the spawned repurposedMiner/torpedo ship spec), plus `MaxLivingChildren` where relevant.

#### Who creates ships vs who consumes them

| Layer | Role |
|-------|------|
| **[`ShipCatalog`](src/units/ShipCatalog.cs)** | **Spawn factory only** — pairs chassis [`ShipSpec`](src/units/specs/ShipSpec.cs) with a new [`ShipLoadout`](src/units/ShipLoadout.cs) ([`ShipInstance.FromCatalog`](src/units/ShipInstance.cs)). Not consulted during combat or merchant quoting (merchants use the instance’s `Spec` + `Loadout`). |
| **Star map / run** ([`RunShipRegistry`](src/run/RunShipRegistry.cs), [`Party`](src/run/Party.cs), [`RunPilotRegistry`](src/run/RunPilotRegistry.cs), engagement setup) | **Owns ships** ([`ShipInstance`](src/units/ShipInstance.cs)), **fleet order** (party ship ids), and **pilots** (name, portrait, `AssignedShipId`, persisted XP/level/learned skills via [`PilotProgression`](src/run/PilotProgression.cs)). Survivors earn XP on **wins** (flat completion + per-enemy-kill bonus). Pilot-owned sources resolve to capability grants: battle grants are snapshotted into encounters, while active strategic grants are installed as the player star-runtime pipeline. The map and runtime do not persist a second authoritative source list. [`State`](src/run/State.cs) refreshes that pipeline when strategic capabilities or party membership change. |
| **Battle** ([`State.FromShipInstance`](src/battle/units/State.cs), action defs) | **Copies `Loadout` from the spawned `ShipInstance`** — installed abilities, caps, upgrades. Does **not** re-query the catalog or validate against chassis templates (run/map setup owns that). |

[`BattleSpawn.Ship`](src/battle/encounter/BattleSpawn.cs) carries a **cloned** `ShipInstance` at layout time; strategic player spawns also snapshot **`PilotId`** on the spawn (encounter save/load), so battle portraits use deployment identity rather than live ship assignment. Battle [`State`](src/battle/units/State.cs) holds [`ShipLoadout`](src/units/ShipLoadout.cs) and creates one runtime counter set per mount. Spawns use [`ISpawnable.ChildSpec`](src/units/loadouts/abilities/ISpawnable.cs) (chassis spec for the child) via [`Factory.ChildFromSpawnableMount`](src/battle/units/Factory.cs). Battle reads each mount's effective [`AbilitySpec`](src/units/loadouts/abilities/AbilitySpec.cs); spawned torpedoes snapshot those effective values onto battle [`State.Projectile`](src/battle/units/VoidBombProjectile.cs).

Catalog loadouts are baseline ship configurations and may leave permissible mounts empty. DevDefault/DevDuel derive separate scenario loadouts by installing every permissible mount and applying one damage/range upgrade wherever that ability supports it; these dev upgrades never change the catalog baseline.

**Upgrade target identity:** run-level **`shipId`** plus either ability kind + facet or a shield face. Merchant packs set an explicit target level on that target; they do not blindly increment it. [`RunShipRegistry.Register`](src/run/RunShipRegistry.cs) is insert-only and idempotent; loadout changes use [`Update`](src/run/RunShipRegistry.cs).

#### Star-map merchant commerce (boundaries)

Trade-hub commerce is split so market configuration lives on [`StarMap.Market`](src/world/star-system/StarMap.cs), offer construction stays in [`merchants/`](src/world/star-system/merchants/), ship mutations in [`units`](src/units/ShipInstance.cs), commits in [`actions`](src/world/star-system/actions/), and run persistence via a single [`MerchantShipPurchase`](src/world/star-system/merchants/MerchantShipPurchase.cs) fact in [`State`](src/run/State.cs).

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Market inventory | [`StarMarket`](src/world/star-system/merchants/StarMarket.cs) + [`MerchantCatalog`](src/world/star-system/merchants/MerchantCatalog.cs) | The map owns persisted merchant and recruitment listings. Catalog construction derives applicable offers for a ship without applying pilot discounts. |
| Damage / range packs | [`MerchantShipChanges`](src/world/star-system/merchants/MerchantShipChanges.cs) | Pack level X sets the selected weapon's upgrade level to X, so its effective stat is its ability baseline plus X. Targets already at X or above are omitted. |
| Shield packs | [`MerchantShipChanges`](src/world/star-system/merchants/MerchantShipChanges.cs) | Pack level X sets the selected face's raw maximum shields to X. Faces already at X or above are omitted. |
| Repair / recharge | [`MerchantCatalog`](src/world/star-system/merchants/MerchantCatalog.cs) | Untiered market listings produce services only when the selected ship needs them. |
| Ship recruitment | [`MerchantSubtypeStock`](src/world/star-system/merchants/MerchantSubtypeStock.cs) | Recruitment stock derives from the map market's tiered recruitment listings. |
| Catalog fetch | [`FetchMerchantCatalogAction`](src/world/star-system/actions/FetchMerchantCatalogAction.cs) | The immediate player action builds base offers from `StarMap.Market`, applies the player's runtime pipeline to a [`PublishMerchantCatalogEffect`](src/world/star-system/effects/PublishMerchantCatalogEffect.cs), and emits `Record<MerchantCatalogQuote>` without writing world state. Scene bindings route quotes to HUDs by endpoint, catalog, and ship. |
| Purchase commit | [`PurchaseAction`](src/world/star-system/actions/PurchaseAction.cs) | The action carries the selected offering, target, ship snapshot, and quoted cost. `IsLegal` re-runs the same pipeline-backed catalog pricing against live state and rejects stale prices or ship snapshots before resolving resource and ship changes. Quotes themselves are not persisted. |
| Who sells what | [`MerchantEndpoint`](src/world/star-system/merchants/MerchantEndpoint.cs) + [`MerchantPurchaseValidation`](src/world/star-system/poi/MerchantPurchaseValidation.cs) | Structural trade-hub merchants are gone; **lead contracts** materialize temporary `EFacilityOperatorRole.Merchant` assignments or a space meeting fleet. Purchases use [`LeadSaleMerchantEndpoint`](src/world/star-system/merchants/MerchantEndpoint.cs) while the sale is **active** through its deadline. Healing stations remain structural. Legacy v3 saves restore grace hub merchants via [`PoiRestore`](src/run/persistence/PoiRestore.cs). |

**Lead black-market sales:** accept a pending lead at a [`LeadContact`](src/world/star-system/poi/EFacilityOperatorRole.cs) dialog, shop at the granted facility merchant or pursue the meeting contact (`EContactIntent.BlackMarketMeeting`) until the scheduled [`ExpireLeadSaleAction`](src/world/star-system/actions/ExpireLeadSaleAction.cs) completes the contract—there is no visit-to-complete step.

Facility controllers only render authoritative references supplied by [`FacilitySceneBinding`](src/world/star-system/presentation/facilities/FacilitySceneBinding.cs) and emit intents through [`UserIntentTranslator`](src/world/star-system/presentation/scene/UserIntentTranslator.cs). Facility intents use the player's immediate action lane: the orchestrator commits that lane at the current tick, resolves reactions, and reports `WorldUpdated` without advancing strategic time or consuming autonomous-agent batches. Map intents remain on the temporal player lane and are consumed only by the strategic tick cycle.

Merchant UI is pack-first: select an upgrade pack, then select one of the eligible targets returned by its authoritative quote. Repair and recharge services purchase directly. Shop tier is displayed separately from the pack's target level.

Strategic reward passives use the same runtime pipeline. Wreckage investigation and engagement resolution tag salvage rewards, build unmodified award effects, and then apply the player runtime pipeline. `DronesYearnForMinesModifier` replaces only positive Scrap awards and rebuilds engagement roll totals; no action, orchestrator, merchant, or presentation code reads pilot skill ids to reproduce those formulas.

---

## Local development

### Prerequisites

- [.NET SDK 10+](https://dotnet.microsoft.com/download)
- [Godot 4.7 .NET build](https://godotengine.org/download) — pick the **.NET** download for your OS (not the standard build; C# requires the .NET edition)

`dotnet build` and the test suite work on all platforms without launching Godot.

### Linux

- Install the .NET SDK via your distro packages or [Microsoft's Linux install docs](https://learn.microsoft.com/en-us/dotnet/core/install/linux).
- Download the Godot **.NET** Linux binary (x86_64). If you run it from an extracted archive, `chmod +x` the executable.
- Open the repo root in Godot, or add the binary to your `PATH` and run it against `project.godot`.

### Windows

- Install the .NET SDK, then the Godot **.NET** Windows build.
- Open the repo root in Godot (`Godot_v4.x-stable_mono_win64.exe` → import/open `project.godot`).
- `dotnet build` and the test command below work from PowerShell or cmd in the repo root.

### Setup & run

1. Open the repo root in your editor and in Godot (`project.godot`).
2. Build: Godot editor **Build** button, or `dotnet build`.
3. Run in Godot (F5). Main scene: `scenes/main.tscn`.

Rebuild after changing exported properties, signals, or tool scripts.

### Tests

Rules and orchestration tests live in `grim-space.Tests/` and run without Godot:

```bash
dotnet test grim-space.Tests/grim-space.Tests.csproj
```

CI runs the full suite (no filter).

#### Suites

Test classes are tagged with xUnit traits (`Suite=Battle`, `Suite=StarSystem`, or `Suite=Integration`) via `[BattleTestSuite]`, `[StarSystemTestSuite]`, and `[IntegrationTestSuite]` in `grim-space.Tests/`. Filter with `dotnet test --filter`:

| Suite | When to run |
|-------|-------------|
| **Battle** | Tactical combat, movement, actions, presentation tied to battle |
| **StarSystem** | Map, contracts, traffic, facilities, engagement on the star map |
| **Integration** | Run/tutorials and cross-layer flows (e.g. session assembly, contract generation integration) |

```bash
dotnet test grim-space.Tests/grim-space.Tests.csproj --filter "Suite=Battle"
dotnet test grim-space.Tests/grim-space.Tests.csproj --filter "Suite=StarSystem"
dotnet test grim-space.Tests/grim-space.Tests.csproj --filter "Suite=Integration"
```

For work on the application seam ([`Session`](src/application/Session.cs), [`Run.State`](src/run/State.cs)), run battle and star-system suites (and integration if you touched those tests)—either two filtered runs or one combined filter:

```bash
dotnet test grim-space.Tests/grim-space.Tests.csproj --filter "(Suite=Battle)|(Suite=StarSystem)|(Suite=Integration)"
```

New `*Tests.cs` files should declare a suite attribute: `[BattleTestSuite]` by default, `[StarSystemTestSuite]` under `World/StarSystem/`, and `[IntegrationTestSuite]` (optionally alongside StarSystem) for run/tutorials, `*IntegrationTests.cs`, and similar cross-layer tests.
