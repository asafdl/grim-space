---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/graphics/SpaceBackdrop.cs
todoIds: [blank-backdrop]
dependsOn: []
status: pending
---

# Subplan: `SpaceBackdrop.cs`

**Source:** [`src/battle/presentation/graphics/SpaceBackdrop.cs`](../../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

`BattleController._Ready` constructs a [`SpaceBackdrop`](../../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) and calls **`Build`**, which spawns the entire “space chamber” look: fog, nebula shells, starfield, and sun mesh. For **planning UX** we want a **diagram board** — empty void so [`GridView`](../../../../../../src/battle/presentation/graphics/GridView.cs) endpoint and path materials remain the primary visual signal.

## Current behavior

`Build(BoundedGrid grid)`:

1. `CreateWorldEnvironment` — dark purple background, **fog enabled**, filmic tonemap.
2. `CreateNebulaShell` — many additive sphere wisps + void membrane box.
3. `CreateStarfield` — 700-instance multimesh.
4. `RedDwarfSun.CreateVisual` — emissive sun at chamber edge.

None of this is required for rules; it is pure presentation.

## What to implement

Add a second entry point **`BuildDiagram(BoundedGrid grid)`** (name as in full plan):

1. Compute `center` / extent as today if needed for future use; diagram v1 may ignore chamber size except optional ambient tuning.
2. Add **only** a child `WorldEnvironment` from a new private helper, e.g. `CreateDiagramEnvironment()`:
   - `BackgroundMode = Color`
   - `BackgroundColor = new Color(0.07f, 0.07f, 0.09f, 1f)` (tweak slightly if grid blues clip)
   - **`FogEnabled = false`** (and do not set fog density/depth)
   - `AmbientLightSource = Color`, `AmbientLightColor` neutral gray (~0.45, 0.45, 0.48), `AmbientLightEnergy` ~0.4
3. **Do not** call nebula, stars, or sun visual helpers.

Leave **`Build` unchanged** so integration branch can keep cinematic backdrop via one call site change later (`[Export]` toggle in controller).

## Edge cases

- **Multiple calls to BuildDiagram** — same as Build today (controller calls once in `_Ready`); no hot reload requirement.
- **Grid size** — diagram does not need to scale decorations with `CombatConfig.DefaultGridSize` (64³); environment is infinite clear color.

## Dependencies

None. Land before `BattleController` switches call site.

## Verification

- After `BattleController` subplan: F5 shows flat dark gray-blue void, no particles/sun.
- `Build()` still compiles; optional manual test by temporarily calling `Build` in controller.

## Out of scope (this file)

- Grid line drawing, cell axes, or slice planes.
- Changing `GridView` materials.
