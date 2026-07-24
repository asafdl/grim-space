# Flow trace

Call-order walk (F5 boot → …). Steps mirror chat **one message = one leaf** (append-only; fix only if something was unclear).

## Tree

```
F5 boot → battle loop (in progress)
-> A  Godot boot … BeginTurn           [done through A.3a.ii]
│   -> A.4–A.7  plan → resolve → present → schedule      [done]
│   -> A.8  F5 battle loop closure                         [done]
└── (later: run meta, hub deep-dives, TimelineRunner)
```

## Steps

- [01-godot-boot.md](steps/01-godot-boot.md) — Step A: `project.godot` / F5
- [02-session-autoload.md](steps/02-session-autoload.md) — Step A.1: Session autoload
- [02b-godot-lifecycle-hooks.md](steps/02b-godot-lifecycle-hooks.md) — Concept: hooks, frames vs ticks
- [03-main-instances-battle.md](steps/03-main-instances-battle.md) — Step A.2: main → battle
- [04-battle-controller-ready.md](steps/04-battle-controller-ready.md) — Step A.3: BattleController._Ready
- [05-from-encounter.md](steps/05-from-encounter.md) — Step A.3a: FromEncounter
- [06-begin-turn.md](steps/06-begin-turn.md) — Step A.3a.ii: BeginTurn
- [07-player-planning.md](steps/07-player-planning.md) — Step A.4: planning / TryEnqueue
- [08-resolve-turn.md](steps/08-resolve-turn.md) — Step A.5: ResolveTurn
- [09-presentation-sink.md](steps/09-presentation-sink.md) — Step A.6: presentation sink
- [10-schedule-from-simulation.md](steps/10-schedule-from-simulation.md) — Step A.7: preview → live schedule
- [11-f5-loop-closure.md](steps/11-f5-loop-closure.md) — Step A.8: loop closure
