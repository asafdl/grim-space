# Step A.7 — TryScheduleFromSimulation (preview → live timeline)

## Tree (snapshot)

```
│   -> A.7  TryScheduleFromSimulation at commit          [now]
│       └── F5 battle loop (boot → plan → resolve → show)  [wrap next]
```

## Bridge

**A.4–A.6** kept the plan on **preview**. At **End Turn**, **`TrySchedulePlayerPhase`** must attach that plan to the **live** timeline without corrupting state if the world moved on.

## Explanation

**`TrySchedulePlayerPhase`** delegates to the engine:

```308:314:src/battle/BattleOrchestrator.cs
	private bool TrySchedulePlayerPhase(string actorId, IReadOnlyList<IAction> actions, int delayTicks)
	{
		if (!_engine.TryScheduleFromSimulation(_session, out _session, actions, delayTicks))
			return false;

		_engine.ScheduleToWorldTimeline(new EndOfPhaseAction(actorId), delayTicks);
		return true;
	}
```

**`TryScheduleFromSimulation`** ensures the **`Simulation`** is current (rebase if stale), then enqueues actions on **`World.Timeline`**:

```32:47:src/core/engine/Engine.cs
	public bool TryScheduleFromSimulation(
		Simulation<TWorld, TRuntime> simulation,
		out Simulation<TWorld, TRuntime> current,
		IReadOnlyList<IAction> actions,
		int delayTicks = 0)
	{
		var resolved = EnsureCurrent(simulation);
		if (resolved is null)
		{
			current = simulation;
			return false;
		}

		current = resolved;
		ScheduleToWorldTimeline(actions, delayTicks);
		return true;
	}
```

Stale preview → **refork from live** and **replay** queued actions; if replay fails, commit aborts (`ResolveTurn` returns false).

## Quiz (answered)

1. **Rebase** (refork + replay) ✓  
2. **Live** **`World`** ✓

## Reference

**Factorio** — blueprint ghost vs built entities: planning is a copy; placement commits into the real simulation.
