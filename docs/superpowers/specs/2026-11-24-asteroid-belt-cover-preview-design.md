# Asteroid Belt Cover Preview Design

## Goal

Create an isolated Godot dev scene for evaluating the appearance of asteroid-belt cover before wiring it into battle presentation.

## Design

The scene follows the standalone `NebulaHazardPreview` pattern: a 3D camera, a space backdrop, orbit/zoom controls, and a small labeled gallery. It shows two one-cell cover clusters side by side, labeled `40%` and `80%`.

Both clusters use deterministic scatter from the existing small asteroid asset pack. The 40% sample contains fewer rocks with a brighter, more saturated tint; the denser 80% sample uses a more muted tint. A fixed seed keeps the preview reproducible. The rock renderer is a small battle-presentation component shared by the dev scene and the later battle view, and it has no collision or gameplay responsibilities.

The scene does not instantiate a battle, simulate damage, or add controls for changing resistance. Production planning/replay synchronization and tooltip behavior remain in the asteroid-belt implementation's later presentation task.

## Validation

- Build the C# project.
- Run the dev scene and visually confirm the two labeled clusters, relative density, and color contrast.
- Keep the scene isolated from combat state and ensure no collision or asteroid-occlusion behavior is introduced.
