# Go compute-kernel migration plan

Status: proposed implementation plan. The in-process Go library bridge is selected;
the rule-ownership migration and implementation phases below still require review.
Live enemy AI, void-bomb search, and player movement previews use the callback-backed
Go search described below. The proposed
rule-ownership migration remains separate and is not enabled by this integration.
The movement-first rule-ownership migration below is not approved. The callback
bridge runs mixed-action search in Go without moving the authoritative rules there;
it is not an implementation of that rule-ownership migration.

## Callback bridge and live enemy search

The bridge lives in the game project: [`src/core/dfs/SearchBridge.cs`](../src/core/dfs/SearchBridge.cs)
and [`SearchAdapter.cs`](../src/core/dfs/SearchAdapter.cs) call an in-process Go
shared library built from [`src/core/dfs/native`](../src/core/dfs/native/). The earlier
snapshot-based `MovementBridge` microbenchmark has been removed. `SearchBridge` runs
actual DFS and is generic over the simulation kernel. Go holds no world state: a search node is the path
of opaque action handles from the root, and Go retains those paths, results, and
the dominance frontier. Each C# worker owns one live `Simulation` and answers a
callback by moving it to the requested path (dequeue to the shared prefix, enqueue
the rest), then running the existing discovery, legality, effect, invariant, and
visit-key code. Undo, the timeline, runtimes, spawns, and action history therefore
behave exactly as in the C# search; no battle rule formulas are reimplemented in Go.

Any action definition is supported. Handles index a C# table of discovered
`IAction` instances, and visit keys are interned to integer ids, so neither actions
nor keys need per-type encoding. Spawned-unit ids are minted at discovery, so two
independent searches name the same spawn differently; tests normalize them.

Capability-search visit keys include every registered unit's hull and six face
shields, keyed by stable unit identity. The C# policy and Go adapter use the same
damage fingerprint. This fixes an outcome-pruning bug: advancing into range and
then firing must not be treated as equivalent to firing out of range and then
advancing. Regression tests cover hull damage, shield-only damage, miss-first
discovery in C#, and repeated serial/parallel Go searches. Heat/status and other
existing pruning approximations are not changed by this fix.

Worker count 0 selects the serial correctness reference. Positive counts select
a bounded pool of goroutines, each with its own C# simulation cursor and Go working
buffers. A task taken from another branch replays at most its path (depth <= 20). Root branches seed the workers; a shared task queue distributes the
remaining work. Workers synchronize the shared Pareto frontier, and a barrier
separates priority exploration from deferred branches. Consumer-controlled
pruning/stopping through `Run` is supported in serial mode only and rejected in
parallel mode. This compatibility API is not the live enemy-AI entry point.

[`AiController.Search`](../src/battle/ai/AiController.cs) calls `SearchBridge.FindBest`
with up to eight goroutine workers. [`best.go`](../src/core/dfs/native/best.go)
owns a shared atomic incumbent score, a synchronized winning path and Pareto frontier,
and a bounded work queue. Each node's children are ranked by score, then upper
bound; the most promising child's subtree is searched before its siblings are
offered to other workers. Workers recheck the shared incumbent before expanding.
An upper bound equal to the incumbent is sufficient to prune because only one
best-scoring proposal is required. Equal-score action choices may differ between runs.
Scores also participate in visit dominance, so a lower-scoring history cannot
dominate an otherwise equivalent higher-scoring state.

The bounded path scores the worker's existing simulation directly and retains only
the winner, not a copied world and result path for every scored node. The
order-dependent first-hit-prefix stop and the eight-finalist collection are removed.
Protection-drone preparation and carrier deployment remain outside the search.
Callback exceptions and cancellation fail the search instead of publishing a partial winner.

Void-bomb planning and fuel-layer reachability use `SearchBridge.Run` in serial Go
mode to preserve traversal order, rank tie-breaking, and frontier replacement.
Their existing C# rules, lexicographic ranking, and turn upkeep remain authoritative;
the scalar-score `FindBest` policy is not substituted for the void-bomb rank.

Player movement indexing uses `SearchBridge.Enumerate`. Its native cursor retains
an explicit traversal stack, the shared dominance frontier, and FIFO deferred
priority branches, rather than eagerly collecting every result. Each `MoveNext`
advances to one valid frame and then pauses; no callbacks or background goroutines
run while paused. The same cursor drives ordered eager `Run`, so their discovery
order, phases, depth limits, and pruning rules stay aligned. C# captures the current
simulation only for the yielded frame and forwards `PruneChildren` on resumption.
Disposing an enumerator releases its native handle through `SafeHandle`; exceptions,
cancellation, and completion also release it. Dispose a partially consumed
`MovePathIndex` when abandoning its search. Existing checkpoint projection, extension
ranking, and preview caching remain in the movement owner.

[`EnemySearchInput`](../src/battle/ai/EnemySearchInput.cs) supplies the score and bound.
The bound includes earned path score, every remaining offensive charge, AP refresh,
movement abilities, and potential goop-blockable threats. Offensive-hit and engagement
bonuses are mutually exclusive. Approaching enemies uses a relaxed, cached reach
envelope when a gravity well is reachable: free edges remain free, but a well is
not incorrectly treated as twenty cells of arbitrary free movement. Goop recoil
cannot bootstrap its own first useful shot, and Ram movement cannot inflate a
no-shot engagement bonus. The per-frame score no longer forks the world or replays
the unchanged search-start position.

The production log identifies `backend=Go`, actual/configured workers, evaluated
states, dominance/bound cuts, and elapsed search time. Strong early pruning can
finish on one worker even though a goroutine pool was configured.

The production C# DFS, its Pareto-frontier helper, and the unused AI runner have
been removed; there is no C# traversal fallback. The test-only unpruned
`ActionSearchExhaustive` remains an independent score/reachability oracle.
Ordered eager and incremental Go modes are compared for action paths, phases,
and complete serialized worlds, including queued prefixes, lethal damage,
scheduling, and the depth limit. Parallel tests exercise 2/4 workers, replay
every returned path through the existing simulation, and compare visit-key
coverage and final Pareto budgets. Complete-world result sets can differ because
dominance selection is order-sensitive; those differences are reported by the
tests, not hidden. The compatibility `Run` API is not a behaviorally identical production replacement.
The traversal measurements below compare this experimental candidate against
the former C# implementation, not Go against an equivalent compact C# redesign.

### Building

Every build of `grim-space.csproj` now requires Go (version from
[`go.mod`](../src/core/dfs/native/go.mod)) and a C compiler for cgo. The project
builds `-buildmode=c-shared` for the publish `RuntimeIdentifier` (Godot exports
publish once per architecture), or for the SDK host when there is none, under
`obj/native/<rid>/`. It then copies the library next to `grim-space.dll`. On macOS,
`/opt/homebrew/bin/go` and `/usr/local/go/bin/go` are probed before `PATH`, because
GUI-launched editors don't inherit the shell `PATH`. Override this with
`-p:GoExecutable=...`. Windows cross-builds default `CC` to `x86_64-w64-mingw32-gcc`.
macOS release exports run on a macOS runner, because darwin cgo needs the Apple
toolchain.

Godot loads the game assembly through `AssemblyDependencyResolver`, which resolves
P/Invoke libraries only from native assets listed in `grim-space.deps.json`. It does
not probe the assembly directory. The build therefore adds the library to the
project's entry in the build and publish deps files. This was verified in an
editor run and an arm64 macOS export; the x86_64 slice and Windows/Linux exports
were checked only for file placement, or not at all.

The search and parallel correctness checks are ordinary tests:

```sh
dotnet test --project grim-space.Tests/grim-space.Tests.csproj --filter-class 'GrimSpace.Tests.Dfs.*'
```

Prefix that command with `GOFLAGS=-race` to build and run against a race-instrumented
library, kept under a separate `obj/native/<rid>-race/` directory. Don't use it for
performance measurements; the benchmark rejects it. Disable tiered compilation
for timing runs, so background JIT promotion doesn't change the code being measured
partway through the samples.

### End-to-end search benchmark

The benchmark runs only the two 8 AP scenarios with 1, 4, and 8 workers; smaller
cases did not change any conclusion. Run the separately opt-in benchmark with:

```sh
DOTNET_TieredCompilation=0 dotnet test --project grim-space.Tests/grim-space.Tests.csproj -c Release -p:RunGoSearchBenchmark=true --filter-class GrimSpace.Tests.Benchmarks.SearchBenchmarkTests --output Detailed --show-stdout All
```

The serial and parallel Go modes return the action path after the queued prefix and the phase for
every retained frame; Go exports them in pages of 256. Results materialize into
full search frames by moving a simulation along the path.

Timed regions include adapter/native setup, search, real C# rule callbacks,
simulation cursor moves, result extraction, and native-handle disposal. AI scoring,
movement-index construction, and checkpoint replay are excluded.

The fixture uses a 24 x 24 x 24 grid, origin (12,12,12), 3 MP, and 4/6/8 AP.
Movement uses the actual movement definitions and translation-first priority;
the enemy and blocked terrain are at (0,0,0). Mixed search adds the actual
Lightning Cannon and movement-first priority, with the enemy at (12,12,15).
Its 1,000 hull and 5 shields per face allow actual shield/hull damage without
changing occupancy through death. Heat, generated status, and simulated
scheduling still use the real effects.

Historical measurements on macOS ARM64, 12 logical CPUs, .NET 10.0.401, Go 1.27.1:
C# search ran once per case as the untimed coverage reference; every goroutine
configuration is validated against it in a warm-up, then timed for three samples
in rotated order. Serial Go and C# are no longer timed: the decision to move
search to goroutines is made. Go used GOMAXPROCS=12 without race
instrumentation; .NET tiered compilation was disabled. Both runtimes were
collected before each timed sample. Allocation counters are process-wide
(`GC.GetTotalAllocatedBytes(precise: true)` and Go `TotalAlloc`), covering
callbacks on every thread. MiB means allocated bytes, not peak memory.
Correctness inspection, which replays every returned path and rebuilds its visit
frontier, is outside timing.

After removal of the production C# DFS, the benchmark uses serial Go as its
untimed coverage reference. This compares Go worker configurations, not an
independent implementation; the separate exhaustive-oracle tests verify search
correctness on bounded fixtures.

Median elapsed milliseconds over 3 samples. "Snapshot" is the previous adapter
(commit `a50bfdc2`, four action families), rerun on the same machine:

| Search (8 AP) | Go 1 | Go 4 | Go 8 | Snapshot Go 1 | Snapshot Go 4 | Snapshot Go 8 | GC pause (Go 8) | Keys |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| Movement | 573.70 | **216.44** | 251.11 | 540.61 | 201.87 | 196.78 | 36.84 | 19,788 |
| Mixed | 6,162.73 | 2,263.31 | **1,793.44** | 6,112.27 | 2,361.79 | 1,954.08 | 509.32 | 153,674 |

The first path-based version was 2-2.6x slower at 8 workers. Three adapter costs
explained it: locks on the shared action table and visit-key ids (fixed with an
append-only chunked table and a concurrent dictionary), resolving and comparing
the whole action path on every callback (fixed by diffing integer handles against
the worker's current queue), and retaining every discovered action, which tripled
GC pauses (fixed by interning actions by record value).

Every validated run (each warm-up and the last timed sample) matched C# visit-key
coverage and final Pareto budgets. Retained world sets can still differ where visit
keys do not encode every effect-dependent field; the benchmark no longer counts
those differences. These are local measurements on a shared
machine, not guarantees. Multi-worker timings move by 10-20% between runs under
background load. Compare builds back-to-back, not against this table. Moving the
bridge into the game project was compared that way and showed no regression.

The earlier 8 AP mixed coverage failure was a pruning-policy bug shared by both
backends, not a Go or adapter defect: every Go frame matched a fresh C# replay.
The capability visit key omitted heat, so a cooler pre-shot state with more AP/MP
dominated a hotter one that would cross into Overdrive (+80% damage) when firing.
Which state survived depended on visit order, so goroutine scheduling exposed it.
Heat, Overdrive, and Overheat are now part of the capability key. That raised
mixed-fixture frames by roughly 30% but multiplies production DevGooper search
frames about 9.6x (26,434 to 252,870); a cheaper heat heuristic is a tracked TODO.

This experiment demonstrates that Go-owned search with goroutines and real C#
action/effect callbacks substantially beats the current search in all six
fixtures without moving rule ownership. These results do not establish that Go
is necessary for the gains, cover unsupported battle features, or approve the
movement-first migration below.

## Objective and evidence

Move expensive planning computation into Go without moving Godot, presentation,
or live commit authority. Optimize the representation and output contract, not
just the implementation language.

The earlier counter-only prototype reproduced the same 8 AP traversal counts in
C# and Go. Warmed Release C# search took a median 527 ms without probes; that Go experiment
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
2. **Void-bomb reach envelope:** traversal already runs through the callback-backed
   Go bridge. A separate follow-up can compute compact fuel-indexed reachable
   layers entirely in Go rather than adapting frames. Preserve
   [VoidBombReachEnvelope](../src/battle/units/voidbomb/VoidBombReachEnvelope.cs)
   semantics, including upkeep between layers.
3. **Enemy capability search (separate follow-up):** migrate only after its complete action/rule
   dependency set and scoring policy can execute locally in Go.
   The callback-backed goroutine path is now enabled in
   [AiController](../src/battle/ai/AiController.cs), including shared-score pruning.
   Moving all scoring and action rules into Go remains a separate migration;
   path-dependent damage scoring can still replay earlier actions in C#.

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

Use explicit rule-ownership eligibility during rollout: an unsupported rule combination
selects the existing callback-backed Go traversal *before* starting the search, with a
diagnosable reason. Unknown relevant status behavior is unsupported, not ignored.
Once a query is accepted by Go, native failures must surface as failures rather
than silently retrying with another backend or returning an empty result.

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
Preserve the generic search contracts through the Go bridge while migrating
domain consumers to compact outputs.

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

Callback-backed enemy traversal is already live as described above; this phase
concerns moving its rule and scoring ownership into Go.

- Inventory supported abilities, effect/status dependencies, scoring inputs,
  pruning, and admissible upper bounds. First-hit-prefix termination was removed.
- Migrate one explicitly supported capability set with its full Go-local policy.
- Return the selected path or bounded finalists, not every world snapshot.
- Replace the existing C# callbacks only when the complete supported policy can
  run locally in Go without duplicating authoritative rules.

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
[SearchBridgeBehaviorTests](../grim-space.Tests/Dfs/SearchBridgeBehaviorTests.cs),
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
