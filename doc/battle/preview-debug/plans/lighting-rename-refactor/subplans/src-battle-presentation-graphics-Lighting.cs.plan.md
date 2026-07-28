---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/graphics/Lighting.cs
todoIds: [lighting-file]
dependsOn: []
status: pending
---

# Subplan: `Lighting.cs`

**Source:** [`src/battle/presentation/graphics/Lighting.cs`](../../../../../../src/battle/presentation/graphics/Lighting.cs) *(new; replaces [`RedDwarfSun.cs`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs))*

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Presentation needs one place to tune the scene **`DirectionalLight3D`** for **diagram** vs **cinematic** presets and to factory **sun props** (emissive spheres) aligned with the key light direction. Today that logic lives in misnamed [`RedDwarfSun`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) with `Configure` + `CreateVisual`. Renaming clarifies dual presets and replaces “create visual” with **`AddSun`**, callable N times without a registry.

## Current behavior

[`RedDwarfSun.cs`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) (static class):

- **`LightDirection`**, warm **`LightColor`**, **`SunCoreColor`**, **`SunHaloColor`** constants.
- **`Configure(light, gridCenter, chamberRadius)`** — light at grid center, look at `gridCenter + LightDirection`, red tint, shadows on (lines 52–64).
- **`CreateVisual(gridCenter, chamberRadius)`** — returns `Node3D` with core + halo meshes placed along `-LightDirection` (lines 12–49).

References: [`SpaceBackdrop.Build`](../../../../../../src/battle/presentation/graphics/SpaceBackdrop.cs) calls `CreateVisual`; [`BattleController._Ready`](../../../../../../src/battle/presentation/scene/BattleController.cs) calls `Configure`.

## What to implement

1. Add **`Lighting.cs`** with `public static class Lighting` in `GrimSpace.Battle.Presentation.Graphics`.
2. Move cinematic constants and behavior from `RedDwarfSun` (keep same numeric values unless noted).
3. **`ConfigureCinematic(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)`** — rename of current `Configure` (same behavior).
4. **`ConfigureDiagram(DirectionalLight3D light, Vector3 gridCenter, float chamberRadius)`**:

| Property | Value | Rationale |
|----------|-------|-----------|
| Position / look target | Same as cinematic: at `gridCenter`, look at `gridCenter + direction` | Consistent rig |
| Direction | e.g. `new Vector3(-0.4f, -0.7f, -0.5f).Normalized()` | Readable ships on diagram |
| `LightColor` | `Colors.White` | Neutral placeholders |
| `LightEnergy` | ~1.0f | Key without warm cast |
| `ShadowEnabled` | `false` v1 | Avoid muddy grid cells |

5. **`AddSun(Node3D parent, Vector3 gridCenter, float chamberRadius)`** — body of `CreateVisual`, but **`parent.AddChild(root)`** instead of only returning (may return `Node3D` for optional caller use). Node name e.g. `"Sun"` or `"SunProp"` (not `RedDwarfSun`).
6. Expose **`KeyDirection`** (or keep `LightDirection` name) as shared constant for cinematic diagram alignment when adding sun props.
7. **Delete** [`RedDwarfSun.cs`](../../../../../../src/battle/presentation/graphics/RedDwarfSun.cs) and its `.uid` (Godot regenerates on next open).
8. **`dotnet build`** — fix any remaining `RedDwarfSun` references in `src/` (subplans update SpaceBackdrop + BattleController).

## Edge cases

- **Multiple suns:** call **`AddSun`** twice with different positions only if caller computes positions; v1 keeps single sun at `-KeyDirection * chamberRadius * 2.4f` like today.
- **Diagram:** never call **`AddSun`** from diagram backdrop path.
- **Missile aim camera:** global directional light still applies (acceptable v1).

## Dependencies

None — implement first.

## Verification

- `rg RedDwarfSun src/` returns no matches after delete.
- `dotnet build` succeeds.
- Unit tests unchanged (no tests reference `RedDwarfSun` today).

## Out of scope (this file)

- `SpaceBackdrop` renames — [SpaceBackdrop subplan](src-battle-presentation-graphics-SpaceBackdrop.cs.plan.md).
- Controller call site — [BattleController subplan](src-battle-presentation-scene-BattleController.cs.plan.md).
- Starfield / nebula — stay in `SpaceBackdrop`.

## See also

- [full.plan.md § Naming notes](../full.plan.md#naming-notes)
- Prior neutral-light values in [cycle-hits RedDwarfSun subplan](../../cycle-hits-move-pick/subplans/src-battle-presentation-graphics-RedDwarfSun.cs.plan.md) (use **`ConfigureDiagram`** name here)
