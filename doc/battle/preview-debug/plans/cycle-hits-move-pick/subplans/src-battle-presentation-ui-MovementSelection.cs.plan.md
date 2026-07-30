---
parentPlan: ../full.plan.md
targetFile: src/battle/presentation/ui/MovementSelection.cs
todoIds: [controller-cycle]
dependsOn: []
status: pending
---

# Subplan: `MovementSelection.cs`

**Source:** [`src/battle/presentation/ui/MovementSelection.cs`](../../../../../../src/battle/presentation/ui/MovementSelection.cs)

[Full plan](../full.plan.md) · [Index](../index.md)

## Why this file

Historically held move **pick** math. **Primary hover** moves to **`GridPick.PickFromSet`** on the active **ring** ([BattleController subplan](src-battle-presentation-scene-BattleController.cs.plan.md)). This file shrinks: keep **`PickOptionIndex`** for click debug mode **B**; optional **Raywalk** ray-sort helper; **`FormatMomentum`** until [format-momentum-home](refactor-format-momentum-out-of-movement-selection.plan.md).

## Current behavior

- **`PickOptionIndex`** — single best option by perpendicular ray distance within **`PickRadius`** (1.4f) over **all** options.
- **`FormatMomentum`** — hint bar copy (misplaced; refactor subplan).

## Do not implement

- **`ListOptionIndicesAlongRay`** as primary Tab / **Depthkey** model (superseded by Manhattan rings).

## Optional (Raywalk)

If **Raywalk** needs near?far ordering among endpoints on one **ring**, add a **small** helper, e.g. sort option indices on a ring by ray parameter **`t`** (same math as today’s **`DistanceRayToPoint`**). Used only from controller during hold-drag — not Tab bands.

## Keep for debug mode B

- **`PickOptionIndex`** unchanged until mode **B** removed after playtest.

## Behavior changes

- Move hover no longer calls **`PickOptionIndex`** in `_Process` once ring UX lands (controller subplan).

## Done when

- No **`ListOptionIndicesAlongRay`** in tree.
- **`PickOptionIndex`** still compiles for mode **B**.
- Optional Raywalk helper covered by manual test or thin unit test if extracted.

## Out of scope

- **`MovePreviewRings`** table build — [MovePreviewRings subplan](src-battle-presentation-ui-MovePreviewRings.cs.plan.md).
- Ring Tab — controller only.
