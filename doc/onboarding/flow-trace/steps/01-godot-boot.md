# Step A — Godot boot (`project.godot`)

## Tree (snapshot)

```
F5 boot
-> A  Godot loads project (autoloads + main scene)     [now]
│   ├── A.1  Session autoload                          (next)
│   ...
```

## Bridge

Cold run (F5). Engine reads project config before any scene script.

## Explanation

F5 loads **`run/main_scene`** and **autoloads** first. **`Session`** is a singleton autoload; **`scenes/main.tscn`** is the scene root. Battle code waits until main instances **`battle.tscn`**. `dotnet test` skips this path.

```11:20:project.godot
[autoload]

Session="*res://src/core/Session.cs"

[application]
...
run/main_scene="res://scenes/main.tscn"
```

## Quiz

1. Full path of the **main scene** file?
2. Full path of the **autoload** script?
3. F5: autoload **`Session._Ready`** or main scene **`_Ready`** first? (autoload / main scene)

## Reference

**Minecraft** — loader manifest before any chunk renders.
