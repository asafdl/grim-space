# Step A.5 — ResolveTurn → ExecuteTurn → Engine.Step

## Tree (snapshot)

```
│   -> A.5  ResolveTurn (commit)                         [now]
│       ├── schedule player → Step (player phase)
│       ├── EnemyPlanner → Step (enemy)
│       └── upkeep → BeginTurn (next round)
```

## Bridge

**A.4** queued actions on **preview**. **A.5** **`EndTurn`** → **`ResolveTurn`**: schedule plan on **live** timeline, **`Engine.Step`**, then AI + upkeep.

## Explanation

**`BattlePresenter.EndTurn`** calls **`ResolveTurn(_battle.Actions.ToList(), sink)`**. That runs **`ExecuteTurn`**: schedule player phase, **`Step`** ticks, **`EnemyPlanner`**, more **`Step`**, round upkeep, win check, then **`BeginTurn`** if fight continues.

```194:211:src/battle/BattleOrchestrator.cs
	public bool ResolveTurn(IReadOnlyList<IAction> playerActions, IPresentationEventSink? sink = null)
	{
		...
			var result = ExecuteTurn(playerActions, sink);
		...
			if (GetPlayer() is not null)
				BeginTurn();
```

```76:81:src/battle/presentation/ui/BattlePresenter.cs
	public bool EndTurn(IPresentationEventSink? sink = null)
	{
		...
		_battle.ResolveTurn(_battle.Actions.ToList(), sink);
```

Timeline stepping: **`src/core/engine/Engine.cs`** **`Step`** → **`TimelineRunner`**.

## Quiz (answered)

1. **`BeginTurn`** → fresh **preview** fork (live world unchanged until next commit) ✓  
2. **`EnemyPlanner`** ✓

## Reference

**XCOM** — End Turn plays out the turn; then a new planning phase.
