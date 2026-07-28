---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/Combat.cs
todoIds: [hints-accessor]
dependsOn: []
status: pending
---

# Subplan: `Combat.cs`

**Source:** [`src/battle/presentation/ui/Combat.cs`](../../../../../../src/battle/presentation/ui/Combat.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

On-screen hints during battle come from **`CombatHints.BuildHint`** in [`Combat.cs`](../../../../../../src/battle/presentation/ui/Combat.cs). Move mode already tells the player to click a path to queue; **cycle hits** is invisible without a short keyboard hint — players will not discover Tab / Shift+Tab on their own.

This subplan is **copy-only** in the hint layer: no dependency on ray math or Godot input.

## Current behavior

In **`CombatHints.BuildHint`**, branch **`EPlayerMode.Move`** (~lines 39–40): builds a string along the lines of “click path to queue” plus optional **`planSuffix`** from committed planning steps.

Other modes (missile, flak, railgun, etc.) have separate strings — leave unchanged.

## What to implement

In the **Move** branch only, append a clause (exact punctuation can match existing hint style):

**`Tab / Shift+Tab: cycle target along sight`**

Suggested placement: **after** the click-to-queue phrase, **before** `planSuffix`, so committed-plan text stays at the end.

Example shape (illustrative):

```csharp
EPlayerMode.Move => $"… click path to queue. Tab / Shift+Tab: cycle target along sight{planSuffix}",
```

Use the project’s existing string concatenation / interpolation pattern; do not refactor unrelated hint lines.

## Optional v1.1 (explicitly not required for acceptance)

Extend `BuildHint` signature with `(int? rayHitIndex, int rayHitCount)` or similar:

- When **`rayHitCount > 1`**, show **`target {cursor+1}/{rayHitCount} along sight`** (wording TBD).
- Requires **BattleController** or **BattlePresenter** to pass live cycle state into whatever builds hints today (`BattlePresenter` → hint label refresh path).

Keep v1 to the static Tab line unless product asks for counts in the same slice.

## Edge cases

- **Single hit along ray** — static Tab hint is still accurate (cycle no-ops in controller); slightly redundant but acceptable.
- **Not in Move mode** — other branches unchanged; grep to ensure no accidental global append.

## Dependencies

- None — can land after cycle UX works.
- No reference to `MovementSelection` from this file.

## Verification

- F5, enter move planning: hint shows Tab / Shift+Tab line.
- Switch to missile/flak: previous hints unchanged.
- [full.plan.md § Manual test](../full.plan.md) item 3.

## Out of scope

- `HoveredMoveIndex` — [BattlePresenter subplan](src-battle-presentation-ui-BattlePresenter.cs.plan.md).
- Rebinding keys or InputMap changes.
- Tutorial overlay or first-run modal.
