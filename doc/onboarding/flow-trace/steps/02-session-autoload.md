# Step A.1 — Session autoload

## Tree (snapshot)

```
F5 boot
├── A  Godot loads project                             [done]
│   -> A.1  Session autoload (_Ready → StartNewRun)   [now]
│   ├── A.2  main.tscn → battle.tscn                  (next)
│   ...
```

## Bridge (A → A.1)

**Step A** = *configuration only* — `project.godot` tells Godot: “register autoload `Session`” and “main scene is `main.tscn`.” **No C# from grim-space has run yet** (only the engine reading a file).

**Step A.1** = *first grim-space code* — Godot **creates** the autoload node and runs **`Session._Ready()`**. That happens **before** `main.tscn` is built, because autoloads always initialize first.

**Not a function call:** `project.godot` does not “call” `Session`. Godot’s startup is:

1. Read `project.godot` (step **A** — labels only)  
2. Create autoloads → **`Session._EnterTree` / `_Ready`** (step **A.1** — writes `Run` + `CurrentEncounter`)  
3. Load `main.tscn` → instance `battle.tscn` (step **A.2**, later)

**Analogy:** A is the **program listing** (“play Session, then Main”). A.1 is **Session actually starting** while the audience is still seating—not Main calling Session.

## Explanation

**`Session`** (`src/core/Session.cs`) sets the static **`Instance`**, then **`_Ready`** calls **`StartNewRun()`**: dev **`Run`** plus **`Encounter.DevDefault(seed)`** on **`CurrentEncounter`**. Battle has not started; data sits on the singleton until **`BattleController`** reads **`Session.Instance.CurrentEncounter`**.

```16:30:src/core/Session.cs
	public override void _EnterTree() => _instance = this;
	...
	public override void _Ready() => StartNewRun();

	public void StartNewRun()
	{
		Run = State.CreateDevDefault();
		CurrentEncounter = Encounter.DevDefault(Random.Shared.Next());
	}
```

## Quiz

1. Which two properties does **`StartNewRun`** assign?
2. Does **`Session`** call **`FromEncounter`**? (yes / no)

## Reference

**Slay the Spire** — run seed and first encounter chosen before the combat UI loads.
