# Preview-debug definitions (move planning)

Ubiquitous language for **presentation-only** move planning on `feature/simple-preview`. Grid and shared terms: [`CONTEXT.md`](../../../CONTEXT.md) at the worktree root.

---

## Ring and k

**Ring** and **k** are for **presentation only** — they improve move-planning UX (narrow choices, clearer mouse targeting). They do not change combat rules or legality.

Endpoints group by Manhattan **shell** from the planning actor **Position** (**A**) to each **EndPosition** (**E**): shell members satisfy `|dx|+|dy|+|dz| == k` (disjoint bands, not inclusive cubes). **Tab** cycles among sorted **k** values that have at least one legal **Option** after dedupe (skip empty shells).

---

## Snapshot cache

The ring table is built **once per planning snapshot** — when preview **Position** or legal **MoveOptions** change. While that snapshot is stable, the player should get the **same experience** (no hidden rebuild on **Depthkey** or pointer moves). **Depthkey** and hover only change **which band** or **which Option** is active, not the cached grouping.

---

## Depthkey (Tab)

**Depthkey** — **Tab** and **Shift+Tab** switch the active **ring** (which **k** band is selected). See **CONTEXT**.

---

## Pointer: Raywalk, fast click, long click

- **Raywalk** — **hold** and move the mouse to move the hovered **Option** **near → far** along the view on the **active ring**. See **CONTEXT**.
- **Fast click** — while **Raywalk**ing (or equivalent press on the active **ring**), a short press **selects** the **EndPosition** / **Option** under the scrub (pins presentation selection; does not queue by itself in mode **C**).
- **Long click** — queues the currently selected **Option** (after **Raywalk** / fast-click select on the active **ring**).

---

## Click (target vs today)

| | Behavior |
|--|----------|
| **Target (this doc)** | Left-click behavior is presentation-only and playtested via a **debug control** among: **(A)** queue the current selection (hover / **Raywalk**), **(B)** **re-pick** at the click pixel (today’s second `PickOptionIndex`), **(C)** **fast click** selects on **Raywalk** (short press while scrubbing the active **ring**); **long click** queues that selection. Ship one mode (or **C** alone) after playtesting. |
| **Today (`BattleController`)** | Hover: `MovementSelection.PickOptionIndex` each frame (ray distance, all **Options**, not ring-filtered). Click: **runs `PickOptionIndex` again** at the click pixel and queues that index — can disagree with hover. |

Grid ↔ scene boundary for picking: [`PointMapping.cs`](../../../src/battle/presentation/PointMapping.cs).

---

## Design reasoning

*Not written in your words yet.* Condensed Manhattan-vs-ray-list rationale lives in the rings UX plan; add a short section here when you want it, or defer to that plan.
