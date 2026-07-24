# Final onboarding exam

**Date:** 2026-07-24  
**Score:** 12 / 12 (pass)

## Answers (confirmed)

| # | Topic | Answer |
|---|--------|--------|
| 1 | Encounter setup | **`src/run/`** (Encounter, spawns, hazards) |
| 2 | Session boot | **`StartNewRun`** → **`Run`** + **`CurrentEncounter`** (not **`FromEncounter`** yet) |
| 3 | Planning mutations | **Preview** fork (**`PreviewWorld`** / **`TryEnqueue`**) |
| 4 | **`TryEnqueue` owner | **`BattleOrchestrator`** (presenter calls in) |
| 5 | Next planning round | **`BeginTurn`** → **`CreateSimulation()`** |
| 6 | Enemy turn | **`EnemyPlanner.PlanTurn`**, then live **`Step`** |
| 7 | Presentation sink | **Live** state after timeline ticks |
| 8 | Stale preview at commit | **Rebase** (refork + replay) |
| 9 | **`IsResolving`** | **No** new **`CanAct`** / planning |
| 10 | Rule advancement | **`Engine.Step`** → **`TimelineRunner`** |
| 11 | **`presentation/`** must not | **Own legality** (rules in actions/core) |
| 12 | CI / rules validation | **`dotnet test`** without Godot |

## Closure

Covers structure chunks **1–3**, hub **M1**, and F5 flow trace **A.1–A.8** (`doc/onboarding/flow-trace/`).

**Note:** First exam form listed every correct choice as the first option (AskQuestion “A”) — content valid; future exams must shuffle per `project-onboarding` skill.
