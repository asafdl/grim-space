# Step A.1b — Godot lifecycle hooks (concept)

Parent: [02-session-autoload.md](02-session-autoload.md). Explains *how* Session runs before main scene.

## Explanation

Godot drives **nodes** (things in the scene tree). C# scripts attach to nodes; the engine calls **virtual hooks** on a fixed schedule—you do not call `_Ready()` yourself.

Typical order for **one** node entering the tree:

1. **`_EnterTree`** — node was added to the tree; children may not be ready yet.
2. **`_Ready`** — node and its **children** are in the tree; safe to `GetNode`, wire peers.
3. **`_Process` / `_PhysicsProcess`** — every frame (if enabled).
4. **`_ExitTree`** — removed from tree.

**Autoload vs main scene (F5):**

- All **autoload** nodes are added first → each gets **`_EnterTree` → `_Ready`** (among others).
- Then the **main scene** is instantiated → same hooks on root and descendants **bottom-up** (`_Ready` on children before parent in Godot 4? Actually Godot _Ready is bottom-up for children first then parent - I'll verify: In Godot, _Ready is called in reverse order - children _Ready before parent. Standard doc: parent _EnterTree, children _EnterTree, children _Ready, parent _Ready.)

Godot 4 docs: `_Ready` is called when node and children are ready; order is depth-first, children before parent.

For grim-space boot:

1. **`Session`**: `_EnterTree` sets singleton → `_Ready` → `StartNewRun()`.
2. **`Main` / `Battle`**: later, `BattleController._Ready` runs after battle node tree exists.

So “lifecycle hook” = **engine callback**, not a method you invoke from `project.godot`.

## Citations

`Session`:

```16:24:src/core/Session.cs
	public override void _EnterTree() => _instance = this;
	...
	public override void _Ready() => StartNewRun();
```

`BattleController` (later in boot):

```32:37:src/battle/presentation/scene/BattleController.cs
	public override void _Ready()
	{
		GameLog.Configure(GD.Print);

		var battle = BattleOrchestrator.FromEncounter(Session.Instance.CurrentEncounter);
		_presenter = new BattlePresenter(battle);
```

## Quiz (answered)

1. **`_Process`** (~every screen refresh) ✓  
2. Timeline **`Step`** (not `_Process`) ✓  
3. **Move-path hover** in move mode ✓

## Frames vs timeline ticks

**Frame (render loop):** Godot calls **`_Process`** ~every screen refresh (~60×/sec). Good for **continuous** things: mouse hover, animations, camera.

**Timeline tick (rules):** grim-space combat advances on **`Timeline` / `TickClock`** when you **`ResolveTurn`** → **`Engine.Step`** — discrete steps (player phase, enemy phase, delayed missile), **not** once per frame.

| | Frame (`_Process`) | Tick (timeline) |
|--|------------------|-----------------|
| Drives | Presentation | Combat rules |
| When | Every frame while game runs | On commit / step |
| Example | Move path hover | Flak resolves N ticks later |

We **do** use frames in **`BattleController._Process`** for move hover only. We **don’t** use frames to spend AP or move ships— that would tie tactics to reflex speed.

**Naming:** `PresentationFrame.BuildFrame()` = planning UI snapshot, **not** a GPU frame.
