# Presentation: move preview, paths, and planned visuals

How Godot turns planning state into grid highlights—and where to extend **direction-colored preview** and **fading turn trails**. Rules stay in `src/battle/actions/` and `src/core/`; this layer only displays and routes input.

See also: [momentum.md](momentum.md) (move AP and evasion affect path costs shown on endpoints).

---

## Presentation vs rules

**Presentation** (`src/battle/presentation/`) renders and input. It may call **`BattleOrchestrator`** for legality and enqueue; it must **not** invent AP costs, legal tiles, or damage.

**Rules** = truth on **`BattleBoard`**, timeline, **`MoveDef`**, etc.

---

## Godot scene roles

### BattleController

`Node3D` on the battle scene. **`_Process`**: mouse hover (move option pick). Clicks queue actions. **`Refresh()`**: rebuild frame → apply to views. Implements **`IPresentationEventSink`**: during **`ResolveTurn`**, **`OnActionApplied`** syncs unit meshes from **live** state after each timeline tick.

### BattlePresenter

Non-node C# class: mode (move / missile / flak), hovers, selection index. **`BuildFrame()`** produces one **`PresentationFrame`** per refresh. Calls orchestrator; does not own grid meshes.

### GridView

Draws **cell highlights** (`MeshInstance3D` boxes). **`SetMoveHighlights`**: endpoint AP colors + single **`_pathMaterial`** for path cells + hover target. **`ClearHighlights()`** frees all highlight meshes—nothing persists unless you add a separate trail layer.

### UnitView

Ship mesh; **`SyncFromState(State)`** from preview or live display state.

---

## Per-frame pipeline

1. **`BattlePresenter.BuildFrame()`** — legal **`Option`s**, **`MovePath`**, preview board, hazards, HUD fields → **`PresentationFrame`**.
2. **`BattleController.ApplyGrid(frame)`** — in Move mode, **`GridView.SetMoveHighlights(...)`**.
3. **`SetMoveHighlights`** — clear meshes; paint endpoints by **`Option.ApCost`**; paint path coords; paint hover target.

Extending direction colors or trails means **new data on the frame** (or parallel args) and **GridView APIs** that do not wipe trail state when clearing interactive highlights.

---

## PresentationFrame (move-related)

| Field | Meaning |
|--------|--------|
| **`Mode`** | **`EPlayerMode`**: which **`ApplyGrid`** branch runs. |
| **`MoveOptions`** | Legal complete paths (**`Option`**: **`Path`**, **`ApCost`**). |
| **`MovePath`** | Grid cells to draw as current path (hover or committed). **Coords only today.** |
| **`MoveTarget`** | Hovered endpoint cell. |
| **`PreviewBoard`** | Board used to display unit positions (preview). |
| **`ActorState`** | Player **`State`** on preview board (position, facing, AP, **momentum**, …). |
| **`CanAct`** | False if battle over or **`IsResolving`**. |

---

## Orchestrator, engine, simulation

### BattleOrchestrator

Battle façade: **`TryEnqueue`**, **`TryEnqueueMovePath`**, **`ResolveTurn`**, **`BeginTurn`**, **`TurnNumber`**, **`IsResolving`**.

### Engine

**Live** world and **`Step`** (timeline after commit).

### BattleSimulation (`Simulation<BattleBoard, ActorSession>`)

Planning workspace for the current player turn:

- **`AnchorWorld` / `AnchorActorRuntimes`** — snapshot at **turn start** (before queued actions).
- **`PreviewWorld` / `PreviewActorRuntimes`** — fork + replay of **`Actions`** (**`Reevaluate`**).
- **`Actions`** — queued **`IAction`s** not yet on live timeline.

**`BeginTurn()`** → new simulation from live world. Planning mutates **preview**.

**`BattleOrchestrator.Board`** → **`_session.PreviewWorld`** (preview, not live).

### Preview vs live vs anchor

| Copy | Role |
|------|------|
| **Live** | **`Engine.World`**; committed timeline. |
| **Preview** | Queued plan replayed on fork. |
| **Anchor** | Turn-start snapshot inside simulation. |

**`View.GetTurnGhost(battle)`** returns **`battle.Board`** (preview) for unit display positions.

**`Reevaluate`**: fork anchor, reapply every queued action in order. Display logic must match replay or shared helpers (**`RebuildMovePath`**, **`StepsFromPath`**).

---

## Grid vocabulary

- **`Coord`** — integer cell.
- **`WorldMapping.ToWorld(coord)`** — Godot position for meshes.
- **`SetCellMaterial(coord, material)`** — ensure highlight box on cell.

Flak already uses **different materials per side** (port vs starboard)—same pattern as per-**`EStepDirection`** colors.

---

## Movement vocabulary

### EStepDirection

Ship-relative step: **Forward, Retro, Dorsal, Ventral, Port, Starboard** (see **`BodyFrame`**, not world axes).

### BodyFrame

From **`State`**: origin + **Fore / Dorsal / Starboard**. **`DirectionOfStep(from, to)`** returns step direction between adjacent cells **at current orientation**. Orientation can change mid-path → per-step **`MoveStepAction.Direction`** is the reliable color key.

### MoveStepAction

One grid step: **`ActorId` + Direction**. Many queued for one path click.

### Option

One legal full path: **`Path`** (destination cells per step), **`ApCost`** (total AP → endpoint highlight color in **`GridView`**).

### MovePathFinder / DiscoverPaths

Search on preview board + **`ActorSession`**; produces **`Option`** list (**`View.GetLegalMoves`**).

### ActorSession

Turn scratch during a move path: direction mask, forward step count, path AP, **movement buildup**, **`IsMovePathStarted`**. When path started, **`GetLegalMoves`** returns empty (no new endpoint options until path rules say otherwise).

### MovementSelection

- **`GetHighlights`** — hovered **`Option.Path`**.
- **`WithCommittedMove`** — if not hovering, rebuild path from queued **`MoveStepAction`**s via **`RebuildMovePath`** (anchor board + apply effects).
- **`RebuildMovePath`** — fork anchor, apply each step’s effects, collect positions after each step.

### Input chain

**`PickOptionIndex`** (ray vs endpoints) → **`SetMoveHover`** → **`TryQueueMove`** → **`TryEnqueueMovePath`** → **`MoveDef.StepsFromPath`** → enqueue steps.

---

## Commit and presentation events

- **`ResolveTurn`** / **`IsResolving`** — live **`Step`**; no new planning while true.
- **`PresentationEvent`** — wraps applied **`IAction`**.
- **`OnActionApplied`** — presentation hook during playback (today: sync units from live state).

---

## Feature: direction-colored preview

**Goal:** each path cell colored by **`EStepDirection`** of the step **into** that cell.

**Data:**

- Hover: **`Option.Path`** + derive steps via **`MoveDef.StepsFromPath`** or replay like **`RebuildMovePath`**.
- Committed (no hover): **`Actions.OfType<MoveStepAction>()`** — each action has **`Direction`**; map to cells via replay.

**Touch points:**

1. **`MomentumConfig`-style map** — **`Dictionary<EStepDirection, StandardMaterial3D>`** in **`GridView.Build`**.
2. Extend **`SetMoveHighlights`** (or overload) with per-cell direction.
3. **`BattlePresenter.BuildFrame`** — compute direction list; add to **`PresentationFrame`** if needed.
4. Paint order: direction on path cells → endpoint AP tiles → hover target on top.

**Do not** reimplement legality in **`GridView`**; use paths/options the rules already produced.

---

## Feature: fading previous-turn trail

**Goal:** show last turn(s) player path with decreasing alpha.

**Today:** **`ClearHighlightMeshes`** removes all transient highlights each update.

**Design:**

- **Presentation-only history** e.g. `{ RecordedTurn, Cells, optional Directions, Alpha }`.
- **Capture:** before **`ResolveTurn`**, snapshot from **`Actions`** + **`RebuildMovePath`**; and/or append on **`OnActionApplied`** for **`MoveStepAction`** (synced with playback).
- **Fade:** on each **`Refresh`**, age = **`TurnNumber - RecordedTurn`**; reduce alpha; drop at 0.
- **Separate layer:** **`_trailMeshes`** + **`SetTrailOverlays`**, not cleared by **`SetMoveHighlights`**.
- **Reset** on new run / scene reload.

---

## Key files

| Area | Path |
|------|------|
| Frame | `src/battle/presentation/ui/PresentationFrame.cs`, `BattlePresenter.cs` |
| Apply | `src/battle/presentation/scene/BattleController.cs` → `ApplyGrid` |
| Grid | `src/battle/presentation/graphics/GridView.cs` |
| Path UI | `src/battle/presentation/ui/MovementSelection.cs` |
| Planning queries | `src/battle/presentation/planning/View.cs` |
| Steps | `src/battle/actions/MoveStepAction.cs`, `MovePathFinder.cs` |
| Spatial | `src/battle/spatial/BodyFrame.cs`, `movement/enums/EStepDirection.cs` |

---

## Mental model

Plan on **preview** → **frame** each refresh → **GridView** paints coords → **End Turn** runs **live** **Step** → sink updates meshes. Direction colors enrich path paint; fading trail is a **second draw layer** with memory keyed by **`TurnNumber`**.
