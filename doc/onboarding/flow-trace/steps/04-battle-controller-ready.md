# Step A.3 — BattleController._Ready

## Tree (snapshot)

```
F5 boot
├── A … A.2 main → battle instance                        [done]
│   -> A.3  BattleController._Ready                      [now]
│       ├── A.3a  FromEncounter
│       ├── A.3b  BattlePresenter + views
│       └── A.3c  _Process (hover) / input
```

## Bridge (A.2 → A.3)

**A.2** added the **Battle** node tree from **`battle.tscn`**. **A.3** is Godot calling **`_Ready`** on the root script **`BattleController`**: first battle code that reads **`Session.Instance.CurrentEncounter`**.

## Explanation

**`_Ready`** configures logging, builds **`BattleOrchestrator.FromEncounter(...)`**, then **`BattlePresenter`**, then child views (grid, camera, units, UI). Rules live in orchestrator; this file wires Godot nodes to it.

```32:37:src/battle/presentation/scene/BattleController.cs
	public override void _Ready()
	{
		GameLog.Configure(GD.Print);

		var battle = BattleOrchestrator.FromEncounter(Session.Instance.CurrentEncounter);
		_presenter = new BattlePresenter(battle);
```

Script binding on battle root:

```7:8:scenes/battle.tscn
[node name="Battle" type="Node3D"]
script = ExtResource("1_battle")
```

## Quiz (answered)

1. **`BattleOrchestrator`** (via **`FromEncounter`**) ✓  
2. **`src/battle/presentation/scene/BattleController.cs`** ✓  
3. **`Session.StartNewRun` → `Encounter.DevDefault`** on **`CurrentEncounter`** ✓  
_(corrected after partial)_

## Reference

**The Matrix** — construct loads (orchestrator); UI wraps around it (presenter/views).
