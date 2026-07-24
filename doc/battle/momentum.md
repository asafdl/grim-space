# Momentum (battle rules)

Discrete **momentum level** **0–3** on each unit (**`State.MomentumLevel`**) affects **evasion**, **move AP economy**, and **weapon targeting**. Stored on the board; copied on fork; updated by move effects, yaw, and hazards.

Related: [presentation-move-preview.md](presentation-move-preview.md) (path highlights and AP endpoint colors depend on step costs at each momentum).

---

## What momentum does (player-facing)

| Level | Evasion | Free forward steps (per move path, when step is priced) | Forward AP (after free) | Lateral AP | Retro (brake) AP |
|-------|---------|--------------------------------------------------------|-------------------------|------------|------------------|
| **0** | 0% | 0 | 1 | 1 | 1 |
| **1** | 30% | 1 | 1 | 2 | 1 |
| **2** | 70% | 2 | 1 | 3 | 2 |
| **3** | 90% | 3 | 1 | 4 | 2 |

Constants in **`MomentumConfig`**: **`MaxLevel = 3`**, **`ForwardStepsPerMomentumGain = 2`** (two forwards in buildup → +1 level, capped), **`MaxGainFromMovementPerTurn = 1`** (from movement alone, at most **+1 level above path start** per path via **`CapMovementGain`**).

Higher momentum = harder to hit, more free forward tiles, **more expensive strafe and brakes**.

UI: **`MovementSelection.FormatMomentum`**, combat hint line, **`UnitView`** label (`M{n} (evasion%)`).

---

## Where state lives

### On the unit (`State`)

**`MomentumLevel`** — authoritative on **`BattleBoard`**. Pathfinder, legality, hazards, and effects read/write here.

Spawn default **0** in **`State.FromSpawn`**. Encounters set **`Spawn.InitialMomentum`** (e.g. dev default: player 0, enemy 2 in **`Encounter.DevDefault`**).

### On the session (`ActorSession`) — one move path

Scratch for the **current continuous move path** (chain of **`MoveStepAction`**):

| Field | Role |
|--------|------|
| **`MoveStartMomentumLevel`** | **`MomentumLevel` when path started** (`BeginMovePathEffect`). Caps movement gain this path. |
| **`MovementBuildupLevel` / `MovementBuildupForwardSteps`** | Internal **`MomentumConfig.Buildup`** while stepping. |
| **`PathForwardSteps`** | Forward steps this path → **`MoveStepContext.ForwardStepsInPath`** for AP. |
| **`UsedDirectionsMask`** | Blocks opposite direction on same path. |
| **`MomentumPaid`** | Momentum “spent” by yaw; refundable when unwinding yaw (`YawMomentumEffect`). |
| **`MomentumGainedFromMovement`** | Passed to **`CapMovementGain`**. **Currently reset but not incremented elsewhere** — cap behaves as **`gained == 0`** (still limited by **`MoveStartMomentumLevel + 1`**). |

**`IsMovePathStarted`** — path in progress; **`View.GetLegalMoves`** returns no new **`Option`s** until path rules reset.

---

## MomentumConfig buildup rules

**`ApplyStep(Buildup, direction)`:**

- **Forward:** progress +1 toward gain; every **2** forwards → level `min(level+1, 3)`, reset progress.
- **Retro:** level `max(level-1, 0)`, reset progress.
- **Lateral:** no level change in **`ApplyStep`** (lateral AP still uses **current level** at pricing time).

**`ApplyMovementStep`** = **`ApplyStep`** + **`CapMovementGain`** (respect path start + max gain per turn).

---

## One move step: effect order

**`MoveDef.Resolve`** (simplified):

1. **`BeginMovePathEffect`** (first step of path) — reset path counters; set **`MoveStartMomentumLevel`** from actor.
2. **`MoveStepMomentumEffect(direction)`** — update buildup; write **`actor.MomentumLevel`**.
3. **`MoveEffect`** — move cell.
4. **`ApChangeEffect(-stepCost)`**.
5. **`RecordMovePathStepEffect`** — mask + forward count.
6. **`MarkSpinBrakedEffect`** on retro (spin discount for yaw).
7. **`HazardCellEntryEffect`** — may reduce momentum / HP.

**Pricing:** **`StepCosts.GetMoveStepApCost`** uses **`MoveStepContext(PathForwardSteps, actor.MomentumLevel)`** **before** effects run on that step. Next step sees updated momentum from step 2.

**Forward:** if `ForwardStepsInPath < FreeForwardSteps` for config at pricing → **0 AP**; else **1 AP**. **Lateral** / **retro** use row **`LateralCost`** / **`BrakeCost`**.

Code: `src/battle/actions/MoveStepAction.cs`, `src/battle/effects/MoveStepMomentumEffect.cs`, `src/battle/movement/StepCosts.cs`.

---

## Pathfinding and preview

**`MovePathFinder.Find`** simulates steps with undo; search nodes include **momentum**. **`Option.ApCost`** totals match this simulation.

**`RebuildMovePath`** (presentation) applies real effects on forked anchor — **momentum changes along the path**, so later legal options and AP highlights can change after partial commits.

**EnemyPlanner** scores with **`MomentumLevel * 1000`** (prefers high momentum when planning).

---

## Momentum outside movement

### Heading / yaw

**`HeadingTurnAction`**: yaw uses **`Orientation.MomentumLossForNetYaw`** (currently equals yaw AP cost). **`YawMomentumEffect`**: positive delta reduces **`MomentumLevel`** and adds to **`MomentumPaid`**; negative delta refunds up to **`MomentumPaid`**. **Spin discount** after retro can zero momentum loss on a yaw.

### Hazards (`HazardResolution`)

- **Missile zone:** damage + **`MissileMomentumLoss`** (1).
- **Flak:** **`FlakMomentumLoss`** (1); if momentum **< `FlakApPenaltyThreshold` (2)** → **`ApPenaltyNextTurn`**.

### Round upkeep

**`RoundUpkeepEffect`**: refills AP (minus 1 if flak penalty flag), missiles, flak. **Does not** reset momentum.

### Railgun

Target must have **`MomentumLevel == RailgunRequiredTargetMomentum` (0)** (`CombatConfig`).

---

## Design intent

Momentum is a **risk/reward gear**: coast forward for evasion and free tiles; strafe and brake cost more at high tiers; hazards and railgun push you toward low momentum. Move UI endpoint AP colors compress path cost computed under this system.

---

## Key files

- `src/battle/movement/MomentumConfig.cs`
- `src/battle/effects/MoveStepMomentumEffect.cs`
- `src/battle/effects/BeginMovePathEffect.cs`
- `src/battle/movement/StepCosts.cs`
- `src/battle/actions/MovePathFinder.cs`
- `src/battle/effects/YawMomentumEffect.cs`
- `src/battle/effects/HazardCellEntryEffect.cs`
- `src/battle/units/State.cs`
- `src/battle/runtime/ActorSession.cs`
- `src/battle/weapons/CombatConfig.cs`
