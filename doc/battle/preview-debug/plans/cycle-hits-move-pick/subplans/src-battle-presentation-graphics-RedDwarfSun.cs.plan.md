---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/graphics/RedDwarfSun.cs
todoIds: [blank-backdrop]
dependsOn: []
status: pending
---

# Subplan: `RedDwarfSun.cs`

**Source:** [`src/battle/presentation/graphics/RedDwarfSun.cs`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Despite the name, this type configures **presentation lighting** used in battle setup. [`Configure`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) aims the scene **`DirectionalLight3D`** with **orange/red** `LightColor` and strong energy — appropriate for a red dwarf sun, but it **tints ships and grid** toward warm hues and fights the “neutral diagram” goal.

`CreateVisual` builds the sun mesh; diagram mode **does not call** it (see SpaceBackdrop subplan).

## Current behavior

- **`Configure(light, gridCenter, chamberRadius)`** — positions light at grid center, looks along fixed `LightDirection`, red tint, shadows on with tuned bias/blur.
- **`CreateVisual`** — emissive spheres (unchanged in this subplan).

## What to implement

Add **`ConfigureNeutral(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)`**:

| Property | Suggested value | Rationale |
|----------|-----------------|-----------|
| Position / target | Same pattern as `Configure`: light at `gridCenter`, look at `gridCenter + direction` | Consistent shadow direction |
| Direction | e.g. `new Vector3(-0.4f, -0.7f, -0.5f).Normalized()` | Top-front key; ships readable |
| `LightColor` | `Colors.White` | Neutral albedo on placeholders |
| `LightEnergy` | ~1.0f | Brighter than ambient-only |
| `ShadowEnabled` | `false` for v1 | Reduces muddy cells; enable later if needed |

Do **not** remove or alter **`Configure`** / **`CreateVisual`** — cinematic path stays for other branches.

## Wiring

[`BattleController._Ready`](../../../../../../src/battle/presentation/scene/BattleController.cs) will call **`ConfigureNeutral`** instead of **`Configure`** when using diagram backdrop (same subplan as controller).

## Edge cases

- **Missile aim camera** — uses separate camera path; neutral light still affects scene globally (acceptable for v1).
- **Chamber radius** — reuse shadow max distance formula from `Configure` if shadows enabled later.

## Verification

With diagram backdrop: ships and grid read **gray/white/blue**, not orange cast.

## Out of scope

- Second fill light, HDRI, or environment reflections.
