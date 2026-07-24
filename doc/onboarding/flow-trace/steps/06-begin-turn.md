# Step A.3a.ii — BeginTurn (end of FromEncounter)

## Tree (snapshot)

```
│   -> A.3a  FromEncounter                               [done]
│   -> A.3a.ii  BeginTurn → CreateSimulation             [now]
│       └── player planning (TryEnqueue)                 (next)
```

## Bridge

Last call inside **`FromEncounter`** before return: **`orchestrator.BeginTurn()`**. Opens the **planning** simulation for the player.

## Explanation

**`BeginTurn`** replaces **`_session`** with **`_engine.CreateSimulation()`** (preview fork), then **`ApplyEndOfPhase`** on the player so preview matches end-of-phase counters/tags.

```118:122:src/battle/BattleOrchestrator.cs
	public void BeginTurn()
	{
		_session = _engine.CreateSimulation();
		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
	}
```

After this, UI **`TryEnqueue`** / undo operate on **`_session`**, not live timeline until **`ResolveTurn`**.

## Quiz (answered)

1. **Preview** fork ✓  
2. Fight **build** (`FromEncounter`) at **`_Ready`**; **`BeginTurn`** starts planning sim ✓

## Reference

**XCOM** — start of **planning phase** before you press End Turn.
