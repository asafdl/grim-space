# Battle preview debug (`feature/simple-preview`)

Presentation-only experiments for **planning-friendly** battle UX: readable previews, overlays, and input—without changing combat rules.

## Documents

| Doc | Purpose |
|-----|---------|
| [CONTEXT.md](../../../CONTEXT.md) | Worktree ubiquitous language (grid, **Ring**, **Depthkey**, **Raywalk**, …) |
| [definitions.md](definitions.md) | Move-planning UX definitions (rings, snapshot cache, pointer) |
| [planning-ux-research.md](planning-ux-research.md) | Reference games, plane slicing, keyboard step-commit, hex vs square |
| [plans/cycle-hits-move-pick/index.md](plans/cycle-hits-move-pick/index.md) | Cycle hits + blank backdrop (split subplans) |

## Related spec (other worktree)

Full move-preview pipeline and direction-colored paths:

- Path: `side-branches/docs-project-guides/doc/battle/presentation-move-preview.md`
- Branch: `docs/project-guides`

Open that folder in the repo admin root (`grim-space`), not from this branch’s tree, unless the file has been copied here.

## Branch layout

- Integration: `feature/preview-debug-friendly` → `side-branches/preview-debug-friendly`
- This slice: `feature/simple-preview` → `side-branches/preview-debug-friendly/side-branches/simple-preview`

Run Godot from this worktree root (`project.godot`), or from repo root: `.\open-godot.ps1 -Branch simple-preview`
