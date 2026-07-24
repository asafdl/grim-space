# Hub M1 — startup chain (quiz)

## Confirmed

- **`Session.StartNewRun`** assigns **`Run`** and **`CurrentEncounter`** (does not call **`FromEncounter`**).
- **`Encounter`**: **`Seed`**, **`Spawns`**, **`BoardHazards`** (asteroids via **`AsteroidFieldGenerator`**, same seed).
- **`BattleController._Ready`**: builds **`BattleOrchestrator`** first via **`FromEncounter(Session.Instance.CurrentEncounter)`**, then **`BattlePresenter`**.
- **HP/AP on grid** → unit **`State`** on **`BattleBoard`**; **`ActorSession`** = turn scratch. **`TryEnqueue`** → **`BattleOrchestrator`** (presenter calls it).
- New fighter weapon → register def in **`Capabilities.cs`**. **`dotnet test`** — no Godot.
- Tick execution after commit → **`TimelineRunner`** via **`Engine.Step`** ( **`ResolveTurn`** orchestrates, does not apply each tick itself).
