# Step A.2 — main.tscn → battle.tscn

## Tree (snapshot)

```
F5 boot
├── A   project.godot                                    [done]
├── A.1 Session hooks                                    [done]
├── A.1b lifecycle / frames                              [done]
│   -> A.2  main.tscn instances battle.tscn             [now]
│   └── A.3  BattleController._Ready                     (next)
```

## Bridge (A.1 → A.2)

**Session** hooks already ran; **`CurrentEncounter`** is on the singleton. **A.2** is Godot **loading the main scene file**—still no `BattleController` code until the instanced battle node’s tree is built.

## Explanation

**`scenes/main.tscn`** is only a **`Node3D`** that **instances** **`scenes/battle.tscn`**. Godot loads main as the scene root, creates the **Battle** child from the packed scene, and runs lifecycle hooks on that subtree (**children `_Ready` before parent** where applicable). **`Session` is not in this file**—battle code reaches back via **`Session.Instance`**.

```1:7:scenes/main.tscn
[ext_resource type="PackedScene" uid="uid://bqx8battle001" path="res://scenes/battle.tscn" id="1_battle"]

[node name="Main" type="Node3D"]

[node name="Battle" parent="." instance=ExtResource("1_battle")]
```

Battle scene attaches **`BattleController.cs`** to the root **Battle** node (`scenes/battle.tscn`).

## Quiz (answered)

1. **`scenes/main.tscn`** ✓  
2. **No** — Session is autoload (global), not under main ✓  
3. **A.3** — `BattleController._Ready` ✓

## Reference

**Factorio** — placing a **blueprint** on the map (instance scene); the factory rules start once entities exist (next hook).
