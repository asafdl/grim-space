# Grim Space modular wreck debris

Six original procedural GLBs. No third-party asset data or textures.

Import individual GLBs into Godot, place them under a Node3D, then rotate, scale and overlap to compose wrecks. Each file contains one mesh with material surfaces. Shared material definitions use scorched gray armor, exposed steel, dark interior, faded ochre paint. No animation, collision, lights or external texture dependencies.

Y is up; Z is the length axis. Pivots are close to fragment centers for easy random rotation, not attachment sockets. Pieces are roughly 1–2.4 arbitrary units; scale to your ships. No need for vertices to align to gameplay's integer grid: keep logical wreck placement separate from visual offsets.

Suggested compositions:
- Small wreck: broken_fuselage + engine_fragment behind it, two hull panels nearby.
- Skeletal wreck: exposed_truss + severed_prow at one end + bent_armor partially overlapping.
- Debris field: duplicate panels and engine pieces, vary rotations and sizes; avoid perfectly even spacing.

These are simple prototypes, with geometry detail and flat PBR materials rather than authored wear textures. The preview uses basic shading; Godot lighting will differ. GLB headers/buffer bounds checked; not tested in the game.

Triangle counts:
- hull_panel: 228
- bent_armor: 210
- broken_fuselage: 336
- engine_fragment: 648
- exposed_truss: 228
- severed_prow: 146

Rebuild with Python, numpy, scipy and matplotlib: python build_wreck_debris.py
