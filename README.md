# grim-space

A 3D roguelike space game with tactical turn-based combat on a discrete 3D grid. This document describes the battle systems currently implemented.

## Architecture

Code is organized around **battle systems** with a slim external surface. Dependencies flow **presentation → battle → units/run**.

| Layer | Path | Role |
|-------|------|------|
| **Units** | `src/units/` | Unit definitions only — `Instance`, `Stats`, enums. No Godot. |
| **Run** | `src/run/` | Placeholder encounter/spawn/party data until roguelike sector map exists. No Godot. |
| **Battle** | `src/battle/` | Battle rules and orchestration — grid, movement, weapons, actions, turn, AI, runtime units. No Godot. |
| **Presentation** | `src/battle/presentation/` | All Godot code — scene, UI, camera, graphics, picking. |
| **Core** | `src/core/` | Session autoload bridging run data into battle entry. |

### Battle systems (`src/battle/`)

| System | Path | Responsibility |
|--------|------|----------------|
| **grid** | `grid/` | `Coord`, bounds, cell math |
| **movement** | `movement/` | Discrete steps, path search, momentum, orientation, step AP costs |
| **weapons** | `weapons/` | Missile mounts/targeting, railgun tuning, hazard zones |
| **actions** | `actions/` | Planned action types, queue, undo; `PlanExecutor` simulates and applies |
| **turn** | `turn/` | Turn counter, active unit |
| **ai** | `ai/` | `EnemyPlanner` — enemy move selection |
| **units** | `units/` | Runtime `State`, `Unit` shells, `Factory` |
| **manager** | `Manager.cs` | Thin orchestrator wiring systems and end-turn pipeline |

Presentation subfolders: `scene/` (`BattleController`), `ui/` (`BattlePresenter`, action bar, HUD), `camera/`, `graphics/`, `picking/`, plus `WorldMapping` for `Coord → Vector3`.

**`Manager`** owns battle orchestration. **`BattlePresenter`** holds player UI mode and builds a `PresentationFrame` snapshot. **`BattleController`** is the thin scene script — input in, frame applied to nodes.

`units/` and `run/` describe *what* a run contains; `battle/units/State` is mutable runtime state *during* a fight.

---

## Grid

### Coordinate space

Combat takes place on a **3D integer lattice**. Each cell is addressed by `(X, Y, Z)`. Distance between cells uses **Manhattan metric** (sum of axis deltas).

### Ship frame

Each unit carries an orthogonal basis: **forward**, **up**, and **right**. Movement directions and weapon arcs are expressed relative to this frame, not world axes.

### World mapping

Cells map to Godot world space at a fixed scale (2 units per cell). Cell centers sit at half-integer coordinates. The camera orbits a pivot at the grid's geometric center.

### Visualization

There is no permanent floor mesh. The grid is **abstract** until the player (or a mode) highlights cells:

- **Move** — reachable endpoints tinted by AP cost; selected/hovered path shown dimmer/brighter
- **Missile** — valid arc cells, blast preview on hover, existing hazard zones
- **Railgun** — valid target cells

Picking works by projecting screen rays into world space and snapping to the nearest relevant cell or unit.

---

## Movement

### Turn economy

Fighters receive **4 action points (AP) per turn**, refreshed when the turn ends. Only the **player unit** acts during the planning phase; the enemy moves when the player ends their turn.

**Movement is the AP sink.** Missiles and railgun do not cost AP.

### Momentum

Each ship has a **momentum level (0–3)** that shapes how moves feel:

| Effect | Behavior |
|--------|----------|
| **Free forward carry** | First N forward steps in a path cost no AP (N rises with momentum) |
| **Forward thrust** | Every 2 forward steps raise momentum by 1, up to +1 per turn from movement |
| **Braking (retro)** | Costs extra AP and lowers momentum |
| **Lateral drift** | Port/starboard/dorsal/ventral steps cost more AP at higher momentum |
| **Evasion** | Displayed per level; not yet rolled in combat |

Momentum updates when a move **resolves** at end of turn, not when the player previews or commits a path.

### Pathfinding

Movement is **discrete step-by-step** along six ship-relative directions. The engine enumerates all reachable paths within remaining AP using depth-first search.

**Path rules:**

- Steps must stay in bounds
- A path cannot reverse direction (e.g. forward then retro in the same path)
- Valid endpoints require **≥ 3 AP spent** on the path, **or** a **0 AP** path granted by momentum free steps

Each candidate path is an **option**: a sequence of cells ending at a reachable position, with total AP cost.

### Player flow

1. All reachable endpoints are highlighted
2. Player hovers/clicks to queue a move (single click)
3. Actions are **planned** — nothing commits until end turn; board preview updates from simulation
4. **Ctrl/Cmd+Z** undoes the last planned action
5. **Position, momentum, and combat effects apply** when the turn ends

### Enemy movement

Enemy AI scores all reachable options. It avoids ending in active hazard zones, prefers escaping hazards, and favors higher projected momentum and forward progress.

---

## Combat

The player toggles between three modes via the action bar: **Move**, **Fore Missile**, and **Railgun**.

### Missiles (mount-based targeting)

Missiles are limited to **2 per turn**. Each mount (currently **dorsal**) defines its own constraints:

- **Fixed range** — target must sit on a Manhattan-distance shell at that range
- **Arc limits** — forward, lateral, and vertical bands in ship-local space (e.g. can't fire too far off-boresight or too low)

This collapses targeting from a full 3D volume to a small arc of cells. Entering missile mode snaps the camera to a **third-person aim view** behind the ship; only valid arc cells are pickable. **Esc** cancels back to move mode.

Firing creates a **hazard zone** (a small cube around the chosen center). Damage does **not** apply immediately.

### Railgun

An instant, click-to-fire weapon. Requirements:

- Living enemy target
- Enemy at **momentum 0**
- Within Manhattan range

Damage is effectively lethal against current fighter HP. No AP cost.

### Hazards and turn resolution

Missile zones persist for the remainder of the player's turn. Multiple missiles can stack overlapping zones.

**End-of-turn sequence:**

1. Apply all planned actions via `PlanExecutor`
2. Run enemy AI movement
3. Resolve all hazards — enemy in a blast cell takes damage and loses momentum
4. Clear hazards, refresh AP, reset missile count, advance turn counter

### Combat ↔ movement interplay

- Missiles are **area denial** — the enemy actively avoids hazard cells when choosing moves
- Railgun is a **finisher** against stationary (momentum-0) targets — pairs with missiles (momentum loss) or forced braking
- Delayed hazard resolution creates a telegraph → react → punish loop within a single turn

### Win condition

Battle ends when either side reaches 0 HP.
