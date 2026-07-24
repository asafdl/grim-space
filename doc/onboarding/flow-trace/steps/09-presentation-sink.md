# Step A.6 — CollectTick → presentation sink

## Tree (snapshot)

```
│   -> A.6  CollectTick / OnActionApplied               [now]
│       └── meshes move; rules already applied on live world
```

## Bridge

**A.5** advanced **live** timeline in **`Engine.Step`**. **A.6** is how Godot *shows* each applied action without re-running rules in **`_Process`**.

## Explanation

Each **`TickResult`** from **`Step`** lists **`AppliedActions`**. **`CollectTick`** forwards each to an optional **`IPresentationEventSink`**:

```300:306:src/battle/BattleOrchestrator.cs
	private static void CollectTick(TickResult tick, IPresentationEventSink? sink, List<IAction> applied)
	{
		foreach (var action in tick.AppliedActions)
			sink?.OnActionApplied(new PresentationEvent(action));

		applied.AddRange(tick.AppliedActions);
	}
```

**`BattlePresenter.EndTurn(this)`** passes **`BattleController`** as the sink. The controller implements **`OnActionApplied`** to drive visuals (movement, shots, etc.) while **`IsResolving`** blocks new planning.

```17:17:src/battle/presentation/scene/BattleController.cs
public partial class BattleController : Node3D, IPresentationEventSink
```

```142:142:src/battle/presentation/scene/BattleController.cs
		_actionBar.EndTurnRequested += () => { if (_presenter.EndTurn(this)) { ExitAimIfNeeded(); Refresh(); } };
```

Rules live in action **`Apply`** / timeline execution; presentation only reacts to events.

## Quiz (answered)

1. **Live** results (sync views from committed state) ✓  
2. **`BattleController`** ✓

## Reference

**Slay the Spire** — card effects resolve in logic first; VFX is layered on top, not the source of truth.
