# Go compute-kernel migration plan

Status: proposed implementation plan. The in-process Go library bridge is selected;
the rule-ownership migration and implementation phases below still require review.
This document does not change the current architecture or enable a Go backend.

## Objective and evidence

Move expensive planning computation into Go without moving Godot, presentation,
or live commit authority. Optimize the representation and output contract, not
just the implementation language.

The experiment reproduced the same 8 AP traversal counts in C# and Go. Warmed
Release C# search took a median 527 ms without probes; the compact Go experiment
took about 100 ms in a separate run. These are not equivalent-output benchmarks:
Go omitted gameplay side effects and usable path/state results. Neither measured
the complete movement-index consumer. They justify a migration experiment, not a
promised speedup.

The C# experiment diagnostics and diagnostic tests have been removed. Keep
performance measurement in a separate benchmark harness, not production probes.
The existing [Go benchmark](../bench/actionsearch/main.go) remains experimental;
do not promote its hardcoded fighter rules or byte payloads into production.

## 1. Scope and ownership

Keep the boundaries in [the architecture guide](../README.md#generic-simulation-kernel).
The generic .NET kernel must remain usable by plain .NET consumers without a
Godot or battle dependency. Do not make its generic APIs require a Go library.

| Area | Planned owner |
|---|---|
| Live world/runtime, commit ordering, scheduling, timeline history, listeners | Existing C# engine and subsystem orchestrators |
| Godot, input, presentation, replay, execution-agent activation and publication | C# |
| Snapshot extraction, request identity, native-library lifetime, action/result adaptation | C# consuming-system adapter |
| DFS traversal, typed visit keys, Pareto frontiers, priority/deferred work, compact path storage | Generic Go compute package |
| Movement action discovery, legality, state changes, and resulting branch state | Go battle-domain package, separate from generic traversal |
| Other action families and non-battle consumers | Existing C# implementations until individually migrated |

Go receives disposable planning state, never live managed objects. Its output is
a proposal/projection, not permission to mutate a C# world or commit actions.

### Initial deliverable: movement only

Migrate the rule calculations behind
[MoveDef](../src/battle/actions/MoveStepAction.cs),
[HeadingDef](../src/battle/actions/HeadingTurnAction.cs), and
[RollDef](../src/battle/actions/RollAction.cs), together with movement-only search.
This includes discovery, legality, and all state changes those actions trigger,
not just position/orientation or DFS bookkeeping.

C# supplies the current simulation snapshot and movement parameters. Go discovers
and applies one action at a time to its own branch state, explores/prunes branches,
and returns compact results in batches. There are no per-node callbacks to C#
for discovery, legality, effect application, or undo.

The existing C# movement action types remain public entry points. They delegate
migrated rule calculations to the canonical Go evaluator when used by ordinary
simulation or live execution; the C# engine still owns applying changes to its
world, undo, records, and commits.

Non-movement action definitions and mixed movement/ability searches remain in C#.
Such searches may use the migrated movement evaluator through those entry points,
but their traversal is not moved to Go in this deliverable. Do not claim the same
speedup for them: they still retain C# simulation machinery and bridge calls.

Movement dominates the measured movement-only workload; the experiment does not
establish its share of every enemy search or the entire kernel. The later
workloads below are separate proposals, not prerequisites for shipping movement.

### Migration order

1. **Movement index:** replace the expensive traversal and per-prefix checkpoint
   replay in [MovePathIndex](../src/battle/movement/MovePathIndex.cs). Preserve
   extension queries, ranking, priority completion, and incremental completion.
2. **Void-bomb reach envelope (separate follow-up):** compute fuel-indexed reachable cells in Go,
   returning layers rather than worlds. Preserve
   [VoidBombReachEnvelope](../src/battle/units/voidbomb/VoidBombReachEnvelope.cs)
   semantics, including upkeep between layers.
3. **Enemy capability search (separate follow-up):** migrate only after its complete action/rule
   dependency set and scoring policy can execute locally in Go.
   [AiController](../src/battle/ai/AiController.cs) currently prunes and terminates
   search based on scores; [EnemySearchInput](../src/battle/ai/EnemySearchInput.cs)
   also forks/replays worlds. Moving DFS alone would not eliminate that cost.

Do not initially port the live engine, generic effect log, committed timeline,
strategic world, persistence, or every action family. Do not add a goroutine pool
as a prerequisite; establish a deterministic single-search implementation first.

## 2. Rule ownership: prerequisite, not follow-up cleanup

A handwritten Go copy of C# movement rules would violate the single-owner
boundary. Revalidating only the winning path would not fix incorrect reachability,
pruning, preview state, or ranking produced by such a copy.

The proposed boundary change is to move migrated pure transition calculations
into the Go battle package and have both Go planning and C# action execution use
that same evaluator:

- Go search invokes it directly, with no cross-language calls inside traversal.
- C# action definitions adapt inputs to it for migrated actions. Existing effects
  retain responsibility for applying/undoing changes to C# state and emitting
  records, using the computed transition rather than independently recalculating it.
- Shared calculations such as heat changes must have one owner across movement
  and non-movement callers; do not retain a second formula in another C# effect.
- Configuration values come from the owning chassis/loadout/status data.
  Do not hardcode the benchmark's AP costs, MP pool, or grid dimensions.
- Preserve resolution, rewrite, application, and event ordering. Explicitly decide
  where migrated status rewriting occurs so it runs exactly once. Never bypass
  the existing rewrite pass globally or apply an already-resolved bonus twice.

This requires a reviewed change to the domain action/effect boundary before any
production consumer switches. It does not move battle rules into the generic Go
search package.

### Required movement-state closure

Audit all reads/writes before defining the snapshot. At minimum, account for:

- Pose, supported translations/turns/rolls, AP/MP costs and remaining pools.
- Grid limits, terrain, live-unit occupancy, and gravity-assisted translations.
- Heat participation, gain scaling, capacity, overdrive/overheat, and pending
  overheat scheduling at the correct tick.
- Cryo-plume cooling and the per-turn set of plumes already consumed by the actor.
- Shield Dynamo shields and its granted-per-face counter.
- Defensive Maneuvers MP-used state.
- Hull/shields and any other state used by preview equivalence or future actions.
- Status ordering, action tags, and any relevant effect rewrites.
- The pre-existing simulation queue and its projected state at the search root.

These are real behaviors in [movement effects](../src/battle/effects/MoveEffect.cs),
[heat](../src/battle/effects/HeatChangeEffect.cs),
[AP changes](../src/battle/effects/ApChangeEffect.cs), and
[MP changes](../src/battle/effects/MpChangeEffect.cs), not optional payload bytes.

[BattleStatus](../src/battle/statuses/BattleStatus.cs) contains C# delegates;
they cannot be serialized as executable Go rules. Migrate the needed semantics
to explicit data/evaluators with a single owner. Do not translate arbitrary
delegates, build a general-purpose rule language, or call them back per node.

Use explicit backend eligibility during rollout: an unsupported rule combination
selects the existing C# implementation *before* starting the search, with a
diagnosable reason. Unknown relevant status behavior is unsupported, not ignored.
Once a query is accepted by Go, native failures must surface as failures rather
than silently retrying in C# or returning an empty result.

## 3. Bridge: native Go library and C# P/Invoke

Build a Go `c-shared` library with a small versioned C ABI. Call it from a C#
interop adapter; do not expose Go types or C# delegates across the ABI.

This is one game process containing both runtimes. The Go library stays loaded
for process lifetime. It is not a worker service and needs no network protocol.
A fatal native/runtime failure can terminate the game; process isolation is not
provided.

### Proposed API responsibilities

Names below describe the contract, not finalized function signatures.

| Operation | Contract |
|---|---|
| `kernel_get_abi_info` | Return ABI/schema version and supported feature identifiers; validate before any query |
| `kernel_evaluate_transitions` | Evaluate a batch of migrated actions using the same domain evaluator as search |
| `kernel_search_create` | Validate/copy a snapshot and options; return an opaque numeric session handle |
| `kernel_search_advance` | Perform bounded work and copy a page of compact results into caller-owned buffers |
| `kernel_search_cancel` | Mark a session canceled; subsequent advancement reports cancellation distinctly |
| `kernel_search_destroy` | Release session state; serialize against advancement/cancellation through the managed wrapper |

Return explicit status codes and bounded error details: success, more work,
priority complete, complete, canceled, insufficient output capacity, unsupported
rules, invalid input/handle, version mismatch, and internal failure. An empty
successful result must mean a valid search with no matching outputs.

Validate malformed lengths, offsets, coordinates, budgets, enums, and arithmetic
overflow before allocating or executing. Recoverable Go panics at exported
boundaries become an explicit internal-failure result; fatal runtime conditions
are not recoverable. Do not allow exceptions/panics to cross the ABI.

### Data format and memory

- Start with a flat, explicitly versioned binary layout: fixed-width integers,
  tagged records, and array offsets/counts. Specify byte order and every field.
  Use 32-bit coordinates, not the benchmark's 8-bit coordinates.
- No strings as inner-loop keys. Map actor/hazard identifiers to stable
  request-local integer indices and keep the inverse mapping in C#.
- Avoid native `bool`, platform-sized integers, implicit struct padding, Go
  slices/maps/interfaces, and managed object references in the wire contract.
- C# allocates input/output buffers. Go copies input into Go-owned state during
  creation and copies results into output buffers during advancement.
- No pointer to Go-managed memory escapes. Go never retains a pointer to a
  managed input/output buffer after a call. Managed buffers are pinned only for
  the duration of a call; alternatively use explicitly owned unmanaged buffers.
- Session handles are integer tokens for a Go-owned registry, not Go pointers.
  The managed adapter wraps ownership in a disposable safe-handle lifetime.
- Insufficient output capacity reports the required size without losing results,
  skipping nodes, or advancing the externally visible page cursor.
- Reject stale/disposed handles. Completion, cancellation, exceptions, cache
  invalidation, and owner disposal must all release session state.

Prefer these explicit copies to a zero-copy protocol for the first implementation.
One snapshot copy and batched result copies are the intended cost; per-node
serialization is not.

### Request identity and invalidation

Include actor identity, world version, anchor tick, rules/schema version, and a
planning-request generation. World version alone is insufficient: queued actions
can change the preview without a live-world commit. Capture the root projected
state and prefix identity once, without replaying that prefix twice.

Results carry the same identity. The C# owner discards stale results and cancels/
disposes the corresponding session after world updates, queue edits, actor
changes, activation changes, or scene/owner teardown.

## 4. Search and output contract

Do not replicate `IEnumerable<SearchFrame<...>>` by transferring a full world for
every frame. Introduce a compact result boundary at the consuming system instead.
Keep existing generic C# search available for consumers that need its contracts.

### Movement representation

- Immutable request-wide geometry and rule parameters.
- Compact branch-local state containing the audited mutable state closure.
- Typed pose/state keys and fixed budget values instead of boxed keys and arrays.
- Parent-linked path nodes storing an action code, parent index, projected
  movement checkpoint/state delta, and phase. Avoid full path copies per visit.
- Export a retained prefix graph plus endpoint records; endpoint-only best paths
  are insufficient for `GetExtensions(prefix)`.
- Keep result indices stable across pages. Prefix nodes must remain available
  while dependent paths are queryable.

Construct [MovePathSession](../src/battle/movement/MovePathSession.cs) action arrays
and complete C# result state only when required by the existing consumer contract.
Generate checkpoints from retained nodes; do not fork/replay every path in
`MovePathIndex.Add` after Go already computed it.

Maintain exact action ordering, ranking/tie breaks, priority-phase semantics,
depth limits, and budget-frontier behavior for the compatibility migration.
Trace counts are a useful check, not the correctness oracle.

Do not silently expand or shrink the existing visit key. If the state-closure
audit exposes unsound existing pruning, document it as a separate behavior change
with explicit approval and tests.

### Incrementality and cancellation

`search_create` only validates and captures state; it must not compute the whole
search before returning. `search_advance` takes an expansion budget, output
capacity, and a stop-at-priority-boundary option. It preserves traversal state
between calls. This supports the current priority/remaining APIs without one
interop call per yielded frame.

Use bounded calls as cooperative cancellation points. Initially no Go background
goroutine is required for a query. Serialize operations per handle; different
sessions must have independent state. C# retains scheduling responsibility and
must not block Godot's frame loop with an unbounded native call.

Preserve `CompletePriority`, `AdvanceRemaining`, and `Complete` behavior through
managed page buffering. Make session ownership/disposal explicit at the movement
index and each caching caller before enabling the native implementation.

Arbitrary C# `PruneChildren` or `ShouldExploreAction` delegates are not part of the
ABI. A migrated consumer supplies an explicit Go-local policy. In particular,
enemy search must migrate its scoring, bounds, early-stop ordering, and relevant
action semantics together; do not transfer every candidate back to C# for scoring.

## 5. Intended source boundaries and packaging

Proposed locations, to be created only during implementation:

| Location | Responsibility |
|---|---|
| `native/go.mod` | Production Go module; separate from the experiment |
| `native/core/search/` | Domain-independent traversal/frontier/path machinery |
| `native/battle/` | Migrated domain state, transitions, and query policies |
| `native/cmd/grimkernel/` | C exports, validation, buffers, and session ownership |
| `src/core/interop/` | Native loading and ABI plumbing only, if genuinely shared |
| `src/battle/interop/` | Battle snapshot/result and action/effect adapters |

Keep this small: do not create a service, registry, or wrapper layer per function.
Do not serialize the entire C# class hierarchy or create a duplicate live world.

Build/package requirements:

- A Go toolchain plus a C toolchain for `c-shared`; document and pin supported
  build inputs when implemented.
- macOS arm64 for local development and both arm64/x86_64 for the existing
  universal export. Prove per-architecture loadability and bundle/signing
  integration; do not assume one arm64 library satisfies a universal app.
- Linux architecture targets must be agreed and built/tested natively or with a
  configured cross-C toolchain. Do not claim plain `GOOS`/`GOARCH` is sufficient.
- Deterministic native-library resolution from the development/build output or
  exported application, with a startup ABI/schema compatibility check.
- Integrate with [the macOS build script](../scripts/build-macos.sh) and
  [export configuration](../export_presets.cfg); verify the library is actually
  inside the exported app, not only loadable from the repository checkout.
- Generic managed tests remain native-library independent. Native integration
  tests require the built library and report missing prerequisites explicitly.

No separate worker process, hot unloading, automatic downloading of binaries,
or additional platform support is in the initial scope.

## 6. Phased implementation and review gates

Implement one phase at a time and review its results before proceeding.

### Phase 1: freeze behavior, ownership, and performance baselines

- Specify movement snapshot/result schemas and the transition-state closure.
- Review the rule ownership changes in section 2, including shared effects and
  status dispatch. Resolve this before writing a parallel Go rule implementation.
- Capture C# reference fixtures and representative complete-consumer benchmarks
  without adding diagnostic hooks to production code.
- Freeze public ordering, extension, cancellation, and invalidation expectations.
- Agree numeric performance thresholds; the proposals below are not promises.

Exit: approved contracts, recorded baseline distributions, and fixture outputs.

### Phase 2: prove the native boundary

- Build/load the shared library from a plain .NET test host.
- Implement version negotiation, buffer round-trip, handle lifetime, bounded
  advancement, and explicit error behavior.
- Validate repeated create/advance/cancel/destroy cycles and malformed inputs.
- Smoke-test loading from an exported macOS application early.

Exit: functioning bridge with measured marshaling/call costs and no gameplay
backend enabled.

### Phase 3: implement compact movement and canonical transitions

- Implement generic Go search and the separately owned battle evaluator.
- Adapt the C# migrated rule entry points to the same evaluator; keep production
  routing unchanged until parity and boundary review pass.
- Cover heat, plume cooling, shields, MP-derived state, and gravity assistance.
- Compare full projected outputs and prefix behavior with frozen C# fixtures.
- Exercise the evaluator through C# enqueue, undo, peek, replay, and live commit.

Exit: equivalent transition/search results; no independent C# formulas remain for
the migrated rule calculations.

### Phase 4: integrate movement consumers

- Replace the movement index's search source and checkpoint replay with compact
  pages/prefix storage, preserving its external behavior.
- Add generation-aware invalidation, cancellation, and deterministic disposal.
- Validate eligible native queries against the existing implementation during
  development. Keep backend selection explicit; never hide native errors.
- Measure full movement-index creation, priority availability, extension queries,
  result adaptation, and selected-path validation.

Exit: correctness and performance gates pass in the actual consumer, including
an exported application.

### Phase 5: migrate void-bomb reachability (separate approval)

- Extend the audited rule closure to projectile movement, fuel, turn upkeep,
  occupancy, and relevant scheduled behavior.
- Move layered reach computation, not full-world copying, into Go.
- Return compact per-turn position sets and preserve existing classifications.

Exit: reach-envelope and target-selection parity plus an end-to-end improvement.

### Phase 6: evaluate enemy capability search (separate approval)

- Inventory supported abilities, effect/status dependencies, scoring inputs,
  pruning, and first-hit-prefix termination semantics.
- Migrate one explicitly supported capability set with its full Go-local policy.
- Return the selected path or bounded finalists, not every world snapshot.
- Do not enable a hybrid per-node C# scoring callback as an intermediate release.

Exit: accepted action/score/ordering equivalence and measured improvement; only
then decide whether more simulation/kernel functionality warrants migration.

## 7. Validation and acceptance

### Correctness

Compare reachable states, non-dominated budgets, phases, action sequences,
extension ranking, checkpoints, and full affected actor state. Replay returned
paths through the authoritative simulation and compare relevant pending timeline
actions and records, not just AP/MP or aggregate traversal counts.

Fixtures should include AP 0/1/4/8 and higher supported budgets, differing ship
profiles, reachable blockers, grid edges, occupied cells, prequeued actions,
heat threshold crossings, overheat, cryo-plume revisits, Shield Dynamo, Defensive
Maneuvers, gravity-assisted zero-cost moves/cycles, and unsupported status behavior.

Preserve existing search behavior and movement tests, including
[ActionSearchBehaviorTests](../grim-space.Tests/Dfs/ActionSearchBehaviorTests.cs),
[MovePathSearchTests](../grim-space.Tests/Movement/MovePathSearchTests.cs), and
[MovePreviewSearchTests](../grim-space.Tests/Movement/MovePreviewSearchTests.cs).
Later phases include projectile reach/scoring/target-selection tests. Test plain
C# domain code and fixtures, not Godot framework behavior.

Test ABI mismatch, buffer growth/retry, cancellation, stale generations, multiple
independent sessions, repeated disposal, and handle/resource cleanup. Run Go race
checks for the handle registry and any supported concurrent operations.

### Performance

Use warmed optimized builds, repeated runs, and the same required outputs.
Measure:

- Complete query latency and time to priority results.
- Snapshot extraction/encoding, native boundary overhead, Go computation, result
  copying/materialization, and selected-path validation separately.
- C# and Go allocation/GC behavior, process peak memory, and retained memory
  after repeated queries. C# allocation counters do not include Go allocations.
- Bounded-call duration and cancellation response.

Compare a Go-only invocation using the *production* request/results with the
C#-through-Go path to isolate boundary cost. Also compare complete consumer
latency against C#: faster traversal must not conceal expensive result rebuilding.

Proposed gates to confirm in Phase 1:

- At least 2x lower median full movement-query latency for the agreed 8 AP case.
- No more than 5% median regression on the agreed common AP <= 4 cases.
- Bridge-only overhead below 5% of the heavy-query end-to-end latency; report
  snapshot/result adaptation separately rather than hiding it in Go computation.
- No monotonic retained-memory growth across repeated completed/canceled queries.
- Identical required outputs and explicitly approved timing/incremental behavior.

If a gate fails, investigate representations, batching, and result materialization
before adding concurrency or expanding the migration scope.

## Next approval

Approve Phase 1, especially the canonical-rule ownership change and proposed
acceptance thresholds, before implementing the native backend. The existing
C# engine remains the live-state authority throughout this plan.
