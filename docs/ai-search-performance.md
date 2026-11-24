# Enemy DFS: budgets and performance investigation

Session findings recorded on 2026-11-24. This document distinguishes implemented behavior, measured observations, and unfinished work. The [README architecture contract](../README.md#game-architecture-and-boundaries) remains authoritative.

## Outcome and current limits

- Enemy search has a **two-second cooperative wall-clock budget per decision**, with a warning when exhausted.
- Search-local void-bomb identities no longer prevent equivalent states from being merged.
- A controlled synthetic stress test fell from **171,884 to 73,320 evaluations**, with the same best score of **3700**. That run took **3651.6 ms versus 1483.7 ms**.
- This is not a reproduction or a complete fix of the originally reported 49-second turn. Repeated scoring replay remains expensive, and an observed upper-bound correctness problem remains unresolved.
- A returned plan is the best eligible plan evaluated, not a guarantee of globally optimal play.

## Original symptom and reproduction

The reported log was:

```text
Enemy DFS (fighter-enemy-dev-1018876669): backend=Go workers=8/8 scored=2632207 dominancePruned=911479 boundPruned=1275480 best=4000 dfs=49300.0ms
```

Rebuilding the initial deployment from seed `1018876669` alone was fast. The exact slow battle snapshot was unavailable; the seed does not capture subsequent movement, damage, resource use, or status changes.

The committed stress fixture in [EnemySearchPerformanceTests.cs](../grim-space.Tests/Ai/EnemySearchPerformanceTests.cs), `CanonicalSpawnStatesReduceFullySkilledFighterSearch`, uses that seed with:

- The fully skilled development pilots and original world hazards.
- A 64-cell grid.
- The entire player formation translated so its first ship is 14 cells forward of the enemy fighter.
- Identical search capabilities, scoring, and action filtering for both variants.
- A 15-second diagnostic budget for each variant, asserting neither exhausts it.

The control uses the same rich canonical state key **plus raw newly spawned unit IDs**. It does not compare the new key against the weaker legacy key, which would confound identity deduplication with additional state distinctions.

For a future slow-turn report, preserve the search anchor: world, actor, queued-action prefix, AP/MP, pose, installed abilities and counters, pilot capabilities, heat, statuses, spawned units, and terrain. A seed and final score are insufficient.

## Execution and ownership

| Component | Responsibility |
|-----------|----------------|
| [AiController.cs](../src/battle/ai/AiController.cs) | Selects capabilities, budget, worker count, evaluator, and visit policy; logs results and publishes actions. |
| [SearchBridge.cs](../src/core/dfs/SearchBridge.cs) | Generic managed/native boundary, worker adapters, action/key tables, result paths, statistics, and native-handle lifetime. |
| [SearchAdapter.cs](../src/core/dfs/SearchAdapter.cs) | Worker-local simulation cursor; discovery, legality, effects, undo/replay, and C# scoring callbacks. |
| [best.go](../src/core/dfs/native/best.go) | Bounded parallel traversal, shared incumbent, dominance, branch-and-bound, deadline checks, and winning path retention. |
| [EnemySearchInput.cs](../src/battle/ai/EnemySearchInput.cs) | Battle-specific scoring, optimistic bounds, and action filtering. |
| [BattleSearchVisit.cs](../src/battle/ai/BattleSearchVisit.cs) | Battle-specific equivalence keys and resource dimensions for dominance. |

Go owns traversal, not battle rules. The generic kernel must remain Godot-free and battle-domain-free. Simulations and AI producers do not commit; the orchestrator owns live commits.

Important traversal details:

- Maximum search depth is 20. A depth limit alone does not bound the number of branches.
- Enemy search requests between one and eight workers, based on processor count.
- Each adapter forks the source once and moves its cursor by undoing to the shared path prefix and replaying the suffix. Complete effect undo is essential.
- Candidates are scored before dominance pruning. Avoiding descendant expansion does not avoid the current candidate's evaluation cost.
- The dominance frontier includes current score in addition to the visit policy's resource dimensions.
- Children are ordered by score, then bound. The promising first child is explored recursively before siblings are offered to other workers.
- The shared queue is bounded to four times the worker count. This limits queued parallel work, not total search effort.
- `FindBest` retains the winning path rather than every result frame. `Run` retains frames; `Enumerate` supports lazy results and must be disposed when abandoned.
- Void-bomb planning and fuel reachability still use serial `Run`; player movement previews use `Enumerate`. The enemy budget is not automatically applied to these consumers.

### Reading the log

- `workers=used/requested` counts workers that handled callbacks, not CPU utilization.
- `scored` counts evaluator calls, including states later pruned. It is not a unique-state count.
- `dominancePruned` counts states rejected by visit dominance.
- `boundPruned` counts bound-based pruning, including checks during expansion. Do not sum these counters to infer total discovered actions.
- `best` is a heuristic utility, not damage dealt.
- `dfs` measures the controller's search call, not the complete enemy activation, path replay, animation, or enemy-team turn.

## Budget contract

The default in [AiController.Search](../src/battle/ai/AiController.cs) was initially one second and was explicitly raised to **two seconds** later in the session. Internal callers/tests can override it.

The generic `SearchBridge.FindBest` accepts an optional `TimeSpan? timeBudget`. Omitting it leaves that search unlimited; supplied budgets must be positive. Adapter setup time inside the bridge is deducted before entering native traversal. Controller argument preparation occurs before bridge timing.

At the native boundary:

- The remaining budget is passed as nanoseconds; zero means unlimited only at this boundary.
- Workers share a deadline and atomic exhaustion flag.
- Stop checks surround discovery, candidate processing, expansion, and queue operations.
- Root evaluation still runs, even if the budget has already expired, so an eligible root can provide an empty additional-action plan.
- Workers finish in-flight callbacks and exit before the result is returned. The source simulation and live world remain untouched.
- The statistics buffer has five entries: evaluated, dominance-pruned, bound-pruned, best score, and exhaustion. Managed and native signatures must remain synchronized.

On exhaustion, the controller returns the incumbent and appends:

```text
warning: time budget exhausted (budget=2000.0ms); using best plan found.
```

Warnings are emitted only for actual exhaustion. Cancellation and callback errors propagate instead of being disguised as a successful partial result; native result handles are released.

This is **not a hard real-time cutoff**: an expensive or stuck rules callback cannot be interrupted by these deadline checks. The budget is per enemy search, not a total budget shared across all enemies. Protection-drone preparation, post-search deployment, replay, and presentation are outside the traversal budget.

The oversized-search regression returned in **2000.7 ms** after the default changed to two seconds. Its elapsed-time assertion allows two to three seconds, with a separate four-second cancellation guard; that tolerance is a test allowance, not an extra production budget.

## Causes found

### 1. Random spawned IDs fragmented equivalent states

[VoidBombDef](../src/battle/actions/VoidBombAction.cs) generates a fresh spawned-unit ID during discovery. Repeatedly discovering equivalent launches therefore produced different raw identity keys, even when the resulting gameplay states were equivalent.

The legacy damage fingerprint included each unit's ID, hull, and shields. It also omitted dynamic distinctions needed for safe broader equivalence. Simply deleting IDs from that string would not have been a sufficient fix.

### 2. Scoring repeatedly reconstructs historical worlds

`DamageAdjustment` in [EnemySearchInput.cs](../src/battle/ai/EnemySearchInput.cs) revisits previous attacks in the candidate path. `WorldAtAction` reconstructs their preceding worlds by replaying the anchor prefix, forking runtimes, and applying earlier actions.

That work repeats across candidate evaluations, including candidates subsequently rejected by dominance. Improving traversal alone cannot remove this per-candidate allocation and replay cost.

A temporary synthetic diagnostic reported approximately **155k evaluations, 2.69 seconds, and 8.3 GiB of managed allocations**, with the final best score first found after approximately **68 ms**. Allocated bytes are cumulative allocation traffic, not peak resident memory; that diagnostic was not the final committed benchmark.

### 3. AP spending can reward cycles

Scoring includes a penalty of 100 per unused AP. A diagnostic out-and-back sequence at saturated heat returned to the same pose and legacy visit key with heat 100, but AP fell from four to one and score rose from 400 to 700.

This can make resource-wasting cycles look attractive and prevents a simplistic "more AP always dominates" argument when score is also considered. However, removing the AP penalty barely changed evaluation counts in the stress fixture. It was **not established as the main cause**, and scoring was not changed.

### 4. Bounds can underestimate descendants

A diagnostic observed an ancestor bound of **2000** with a descendant score of **4000**, involving future void-bomb alignment. A valid maximization bound must cover every reachable descendant, not merely the current score.

The adapter checks `UpperBound >= Score` for the current candidate; that check cannot detect this ancestor/descendant failure. Existing branch-and-bound can therefore discard a better continuation. Neither the timeout nor identity deduplication fixes this correctness issue.

Do not strengthen pruning until bound admissibility is regression-tested and corrected.

## Implemented state equivalence

`BattleSearchVisit.ForCapabilitySearch(anchor)` captures the action-prefix length, original unit IDs, and immutable terrain references. Only new void bombs launched after that prefix receive search-local aliases, in spawn-action order.

Actual actions, unit IDs, and replay targets are never rewritten. Units already present at the anchor retain their identity. Original identities and aliases use separate namespaces. Unit/spawn enumeration order is preserved rather than assuming arbitrary permutations are equivalent.

The fingerprint includes:

- Identity and parent, team/faction/type/initiative, position and orientation basis.
- Hull, directional shields and base capacities; fuel and projectile parameters.
- Exact heat, Overdrive, and Overheat.
- Ability uses/cooldowns and activation flags, including shields, vents, brace, engine overcharge, and weakspot.
- Defensive-maneuver use, shield dynamo, scrap/reactive stacks, and cooled cryo-plume IDs.
- Other units' AP/MP; the acting unit's AP, MP, and negative actor action count remain separate dominance dimensions.
- Ordered statuses, priority, polarity, action tags, callback semantics, and payload state.
- Added, replaced, and removed terrain, including type, frame, cells, and asteroid-belt damage resistance. Unchanged immutable anchor terrain is skipped.

Status handling is deliberately conservative:

- Without an explicit `RuleKey`, callback delegates use normal delegate equality.
- [BattleStatus.RuleKey](../src/battle/statuses/BattleStatus.cs) is an owner-declared promise of equivalent callbacks for the owning unit, independent of closure identity. `DeepCopy` preserves it.
- Independently created Overdrive statuses need that stable key because their closures otherwise differ despite equivalent behavior.
- Empty, temporary-shield, and shield-brace payloads have explicit handling. Unknown payloads are deep-copied into equality data rather than assumed equivalent.
- Temporary-shield grant order and remaining amounts matter; opaque grant IDs do not during this activation search, because expiry runs outside it.
- The rules list uses structural equality and hashing, not list-reference equality.

This equivalence is **activation-scoped**, not a universal world equality or persistence format. Revisit it when adding stateful rules, terrain properties, payloads, or expiry work inside the search. New `RuleKey` values must come from the rule owner, not guesses about closure contents.

The older `ForCapabilities` policy still exists for other consumers/tests; this session migrated the enemy controller, not every search consumer. The existing status-monolith TODO was not refactored.

### Heat is not a monotone resource

The existing heat-key TODO cites approximately 9.6 times more frames on DevGooper; that figure was not remeasured here. Exact heat remains in the key because cooler states cannot always reproduce hotter Overdrive attacks, and hotter is not uniformly better. Collapsing heat or treating it like AP requires a separate correctness argument.

## Measurements and interpretation

| Experiment | Observation | Qualification |
|------------|-------------|---------------|
| Original report | 2,632,207 scored; 49,300 ms; best 4000 | Exact snapshot unavailable. |
| Earlier diagnostic canonicalization | About 154k to 65k evaluations; warm 1.82 s to 0.77 s; best 3700 | Temporary prototype, not final richer key. |
| Final controlled stress comparison | 171,884 to 73,320 evaluations; 3651.6 to 1483.7 ms; best 3700 | Same rich key/scoring, differing only in raw spawned-ID discrimination. |
| Repeated final-key runs | Roughly 73k-74k canonical evaluations; about 1.3-2.1 s | Scheduling, warmup, GC, and machine contention affect results. |
| Default-budget regression after increase | 2000.7 ms | Deliberately oversized 100-AP search; verifies exhaustion and replayability. |

The controlled comparison removed approximately **57% of evaluations**. Its timing is a sample, not a latency guarantee or a direct before/after measurement of the original deployment. Parallel incumbent timing can also change evaluation counts.

The original small fixtures exercised only 29 and 124 evaluations. Passing them did not demonstrate resistance to fully skilled, combinatorial encounters.

## Performance and correctness testing

Committed coverage:

- [BattleSearchVisitTests.cs](../grim-space.Tests/Ai/BattleSearchVisitTests.cs): equivalent spawned identities, unchanged action IDs, distinct gameplay state, anchor identities, Overdrive closure equivalence through status copying, conservative custom callbacks, and comparison with a small exhaustive traversal.
- [EnemySearchPerformanceTests.cs](../grim-space.Tests/Ai/EnemySearchPerformanceTests.cs): controlled stress comparison, equal best score, at least one third fewer evaluations, completed searches, replayable winner, default deadline, and warning/non-warning cases.
- [SearchBridgeTests.cs](../grim-space.Tests/Dfs/SearchBridgeTests.cs): partial incumbent, legal root on immediate expiry, source/prefix preservation, replay, and cancellation/error propagation.
- [ActionSearchExhaustive.cs](../grim-space.Tests/Dfs/ActionSearchExhaustive.cs): small unpruned C# oracle. Use tiny fixtures; exhaustive traversal is not a production fallback.
- Status persistence, temporary shields, heat effects, and save persistence were included in the broader targeted validation because status copying changed.

Testing principles:

1. Separate deadline compliance from algorithmic improvement. A fast timeout does not prove a smaller search space.
2. Compare identical fixtures, scoring, capabilities, and budgets; require both searches to finish for complete-result comparisons.
3. Check score and replay legality, not counts alone. Equal stress scores do not prove global optimality, especially with the known bound problem.
4. Prefer evaluation-reduction thresholds for regression stability; retain elapsed-time output and explicit budget tests.
5. Collect warm/repeated timings, managed allocations, and time to the last incumbent improvement when profiling. Distinguish managed allocations from native allocations and process memory.
6. Avoid timing-test contention. The performance collection disables parallelization; global log scopes also require isolation.
7. Test domain C# and fixtures, not Godot framework behavior; framework-dependent tests have previously caused segmentation faults.

### Reliable commands

Run from the repository root. Build first; direct DLL execution otherwise runs stale tests.

```sh
dotnet build grim-space.Tests/grim-space.Tests.csproj --no-restore --verbosity quiet -consoleLoggerParameters:ErrorsOnly
dotnet grim-space.Tests/bin/Debug/net10.0/grim-space.Tests.dll -class 'GrimSpace.Tests.Ai.*' -class 'GrimSpace.Tests.Dfs.*' -class 'GrimSpace.Tests.Battle.BattleStatus*' -class 'GrimSpace.Tests.Units.TemporaryShieldsTests' -class 'GrimSpace.Tests.Effects.HeatChangeEffectTests' -class 'GrimSpace.Tests.Run.SaveGamePersistenceTests' -parallelMode none -showLiveOutput
```

Focused budget/warning validation:

```sh
dotnet grim-space.Tests/bin/Debug/net10.0/grim-space.Tests.dll -method '*OversizedSearchStopsAtTheDefaultTwoSecondBudget' -method '*DecisionBudgetWarnsOnlyWhenExhausted' -showLiveOutput
```

[global.json](../global.json) selects Microsoft.Testing.Platform. During this session, the attempted `dotnet test --filter FullyQualifiedName~...` selection ran zero tests, and an attempted `dotnet test ... --no-build -- --help` crashed the CLI. The direct xUnit runner above was reliable; always check the reported test count.

Native checks must run inside the Go module:

```sh
(cd src/core/dfs/native && go test ./... && go vet ./... && test -z "$(gofmt -l best.go)")
git diff --check
```

Running `go test ./...` at repository root fails because it is not the module root. The native module currently reports no Go test files; these commands check compilation, static analysis, and formatting, while managed integration tests exercise traversal behavior.

At the end of the deduplication step, 197 targeted managed tests passed. After increasing the default to two seconds, the three focused budget/warning cases passed. These were separate runs, not a claimed full-suite result.

## Remaining work, not implemented

1. Add an ancestor/descendant bound regression and correct future bomb-alignment bounds before relying on stronger pruning.
2. Replace repeated historical scoring replay with incremental earned-score accounting, preserving existing scoring, undo, fork, and branch semantics. Keep battle scoring out of Go.
3. Profile again after scoring changes; do not assume further traversal changes are necessary.
4. If movement permutations remain dominant, evaluate movement closure or macro-actions: search reachable tactical poses at decision points instead of every ordering of single-cell moves. This requires preserving heat, orientation, terrain effects, resources, and intermediate triggers.

[MovePathIndex.cs](../src/battle/movement/MovePathIndex.cs) is not already a cheap movement-closure replacement: it still enumerates/stores paths and clones worlds. Neither an AP-scoring change, heat collapse, a beam search, nor a hierarchical planner was implemented in this session.
