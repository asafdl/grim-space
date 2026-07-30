# Grim Space (simple-preview worktree)

Battle planning and presentation experiments on `feature/simple-preview` without changing combat rules.

## Language

**Cell**:
One slot on the 3D battle **grid**; identified by a **Coord**, not a **Vector3** or scene **Location**.
_Avoid_: cube, voxel (in player-facing copy unless discussing rendering); **Vector3** or **Location** when you mean a **cell**.

**Coord**:
Grid-side coordinate type — integer `(X, Y, Z)` for one **cell** (`GrimSpace.Math.Grid.Coord`). Values for grid **Position** and **EndPosition**; aligned with **Vector3** via `PointMapping` (`ToCoord`, `ToWorld`).
_Avoid_: world position, scenepoint; **Coord** when you mean the **Point** role (say **Position** or **EndPosition**).

**Grid**:
The bounded `Width × Height × Depth` lattice of **cells**.

**Point**:
Where something is — umbrella term; name the **environment**. Grid → **Position**; scene → **Location**.
_Avoid_: “point” alone when grid vs scene must be explicit; scenepoint, scene point as headings.

**Position**:
Grid-side **Point** — which **cell** an actor occupies, as a **Coord** (preview/planning uses the **Coord** after queued steps).
_Avoid_: **Position** for scene **Location** or **Vector3**; **Position** as a synonym for the **Coord** type in API copy.

**Location**:
Scene-side **Point** in Godot — rendering and picking, as **Vector3**.
_Avoid_: **Location** for a **cell**, **Coord**, or grid **Position**; vague “location” without grid vs scene; **LocationMapping** (renamed **`PointMapping`**).

**Vector3**:
Scene-side coordinate type; aligned with **Coord** via `PointMapping` (`ToCoord`, `ToWorld`).
_Avoid_: **Vector3** for a **cell** or grid **Position**; unpinned “position” without **Position** / **Location** when the environment matters.

### Grid ↔ scene

Umbrella: **Point**. Twin roles and types by environment:

| Environment | Type (mapping) | Twin role |
|-------------|----------------|-----------|
| **Grid** | **Coord** | **Position** — actor **Point** on a **cell** (also **EndPosition** for an **Option** endpoint) |
| **Scene** | **Vector3** | **Location** — **Point** in Godot world/scene space (rendering, picking) |

Mapping: `PointMapping` — `ToWorld(Coord)` → **Vector3**, `ToCoord(Vector3)` → **Coord**.

**PointMapping**:
Grid ↔ scene conversion for **Point** — **`Coord`** to scene **Location** (**Vector3**) and back; owns **`CellSize`** and chamber **`GridCenter`**.
_Avoid_: **LocationMapping** (old name); treating **`PointMapping`** as a **Point** value instead of the mapper type.

**Option**:
A legal move path the player may queue; ends at an **EndPosition** on the **grid**.
_Avoid_: move target as a loose phrase without tying to **EndPosition**.

**EndPosition**:
Grid-side **Point** — the **cell** where an **Option**’s path ends (last step), as a **Coord**; not the ship’s **Position** until the path is committed.
_Avoid_: **EndPosition** for scene **Location** or **Vector3**; treating it as the ship’s current **Position** before commit.

**Ring** (preview UX):
A Manhattan-distance **shell** band around the planning actor’s **Position**; identified by **k** in move-planning copy.
_Avoid_: layer, shell, cube as interchangeable UX synonyms for **ring** — say **ring** and **k**.

**k**:
Manhattan distance from planning actor **Position** **A** to **EndPosition** **E** on the active **ring** (`|dx|+|dy|+|dz|` for that shell only).
_Avoid_: AP cost, path step count, or Chebyshev “box” distance when describing Tab bands.

**Depthkey**:
Presentation-only keyboard control — **Tab** and **Shift+Tab** switch the active **ring** (which **k** band is selected for hover).
_Avoid_: “depth key” as a spaced heading; calling **k** a **Depthkey** (**k** is the shell index; **Depthkey** is the input).

**Raywalk**:
Presentation-only pointer control — **hold** and move the mouse to move the hovered target **near → far** along the view on the **active ring** (scene **Location** / **Vector3** along the ray, not a grid **Position** commit).
_Avoid_: using **Raywalk** for **Depthkey** (Tab) or for changing grid **Position** / **Coord**; “ray walk” as a spaced heading.
