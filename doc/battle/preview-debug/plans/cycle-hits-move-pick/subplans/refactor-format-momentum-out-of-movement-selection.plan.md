---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/MovementSelection.cs
relatedFiles:
  - src/battle/presentation/ui/Combat.cs
todoIds: [format-momentum-home]
dependsOn: []
status: pending
---

# Subplan: move `FormatMomentum` out of `MovementSelection.cs`

**From:** [`src/battle/presentation/ui/MovementSelection.cs`](../../../../../../src/battle/presentation/ui/MovementSelection.cs)  
**To:** [`src/battle/presentation/ui/Combat.cs`](../../../../../../src/battle/presentation/ui/Combat.cs) (`CombatHints`)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why

[`MovementSelection`](../../../../../../src/battle/presentation/ui/MovementSelection.cs) should own **legacy ray pick** (`PickOptionIndex`) and optional **Raywalk** sort helpers only. **`FormatMomentum`** formats unit status for the hint bar — it has no relationship to ring tables or **`GridPick`**.

Keeping both in one static class blurs responsibilities and makes the ray-hit work in [MovementSelection subplan](src-battle-presentation-ui-MovementSelection.cs.plan.md) harder to read.

## Current behavior

**MovementSelection.cs** (~lines 9–14):

```csharp
public static string FormatMomentum(State unit)
{
	var config = MomentumConfig.ForLevel(unit.MomentumLevel);
	var evasion = (int)(config.Evasion * 100);
	return $"M{unit.MomentumLevel} ({evasion}% eva)";
}
```

**Single caller:** [`CombatHints.BuildHint`](../../../../../../src/battle/presentation/ui/Combat.cs) builds `status` with `MovementSelection.FormatMomentum(unit)` (~line 32) for every player mode.

## What to implement

### 1. Add formatter on `CombatHints` (Combat.cs)

- Add **`private static string FormatMomentum(State unit)`** with the **same body** as today (still uses `MomentumConfig.ForLevel`, same format string).
- In `BuildHint`, replace `MovementSelection.FormatMomentum(unit)` with **`FormatMomentum(unit)`**.

Do not change hint wording or layout beyond the call site.

### 2. Remove from MovementSelection.cs

- Delete **`FormatMomentum`** and drop **`using GrimSpace.Battle.Units`** if nothing else in the file needs `State` (ray math uses `Option` / Godot types only).

After this subplan, **MovementSelection** should contain only pick-radius constants and ray/list APIs.

## Edge cases

- **No other callers** — grep `FormatMomentum` and `MovementSelection.FormatMomentum` under `src/` before/after; expect zero references to momentum on `MovementSelection`.
- **Tests** — none today for this string; no new tests required unless you add one elsewhere for hint status (out of scope).

## Dependencies

- Independent of [ray-hit-list](src-battle-presentation-ui-MovementSelection.cs.plan.md); can land **before**, **with**, or **after** ray APIs.
- Safe to combine in the same commit as `ray-hit-list` if you touch `MovementSelection.cs` once.

## Verification

- Project builds.
- F5: hint bar still shows `M{n} ({ev}% eva)` in Move and other modes.
- `MovementSelection.cs` has no `State` / momentum formatting.

## Out of scope

- Changing evasion display rules or `MomentumConfig`.
- [Combat hints Tab line](src-battle-presentation-ui-Combat.cs.plan.md) — separate todo (`hints-accessor`).
