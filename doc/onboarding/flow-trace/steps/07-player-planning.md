# Step A.4 — Player planning (TryEnqueue)

## Tree (snapshot)

```
│   -> A.4  Player input → TryEnqueue / undo               [now]
│       └── ResolveTurn → timeline Step                   (next)
```

## Bridge

After **`BeginTurn`**, **`BattlePresenter`** / **`BattleController`** call **`BattleOrchestrator.TryEnqueue`** (or move path helpers). Live timeline unchanged until commit.

## Explanation

Input handlers (action bar, grid pick, heading HUD) call presenter methods → **`TryEnqueue`** / **`TryEnqueueMovePath`** on the orchestrator. Each success replays on **`Simulation`** preview; **`TryUndoLast`** rolls back. **`IsLegal`** asks action defs against preview board + session.

Planning stays on **`_session`** until player commits **`ResolveTurn`**.

### Code chain (player)

**Input** — `src/battle/presentation/scene/BattleController.cs` (`_UnhandledInput`, action bar):

```141:142:src/battle/presentation/scene/BattleController.cs
		_actionBar.EndTurnRequested += () => { if (_presenter.EndTurn(this)) { ExitAimIfNeeded(); Refresh(); } };
```

```253:258:src/battle/presentation/scene/BattleController.cs
		switch (_presenter.Mode)
		{
			case EPlayerMode.Move:
				...
					_presenter.TryQueueMove(index, frame.MoveOptions);
```

**Presenter** — `src/battle/presentation/ui/BattlePresenter.cs`:

```122:128:src/battle/presentation/ui/BattlePresenter.cs
	public bool TryQueueMove(int optionIndex, IReadOnlyList<Option> options)
	{
		...
		if (!_battle.TryEnqueueMovePath(options[optionIndex]))
```

**Orchestrator** — `src/battle/BattleOrchestrator.cs`:

```143:159:src/battle/BattleOrchestrator.cs
	public bool TryEnqueue(IAction action)
	{
		...
		if (!_session.TryEnqueue(action))
			return false;

		ApplyEndOfPhase(_session.PreviewWorld, _session.PreviewActorRuntimes.For(PlayerId), PlayerId);
		return true;
	}
```

**Simulation queue** — `src/core/engine/Simulation.cs` (`TryEnqueue` replays on preview).

## Quiz (answered)

1. **Preview** ✓  
2. **`BattleOrchestrator`** (`TryEnqueue`); presenter calls it ✓

## Reference

**XCOM** — move waypoints in planning; nothing final until End Turn.
