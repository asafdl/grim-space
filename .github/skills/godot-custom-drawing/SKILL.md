---
name: godot-custom-drawing
description: Design and implement precise filled HUD silhouettes, curves, and outlines with Godot CanvasItem custom drawing.
---

# Godot Custom Drawing

Use this skill when changing a Godot `Control._Draw()` or `Node2D._Draw()` implementation, especially for HUD shells whose filled silhouette and border include curves.

## Start From Explicit Geometry

Translate the requested shape into exact local-space endpoints before choosing an equation. Godot canvas coordinates use positive Y downward.

For example, an elliptical left edge running from `(-40, bottom)` to `(0, top)` has:

- Center: `(-40, top)`
- Horizontal radius: `40`
- Vertical radius: `bottom - top`
- Bottom angle: `PI / 2`
- Top angle: `0`

Do not introduce a generalized sine, easing, or curve system when the shape is one fixed arc. Expose only meaningful design constants such as width, height, and point count.

## Filled Curves Need Shared Geometry

`DrawEllipseArc()` draws only an outline. A filled shell still needs a polygon whose boundary contains points sampled from the same ellipse.

1. Generate arc points in the polygon's winding order.
2. Add the remaining straight-edge vertices.
3. Call `DrawPolygon()` for the fill.
4. Draw the straight outline separately.
5. Call `DrawEllipseArc()` with the same center and radii for the curved outline.

The arc's `pointCount` is tessellation resolution, not multiple visual segments. Name it `arcPointCount`, not `segments`, when that distinction matters.

```csharp
var angle = Mathf.Lerp(Mathf.Pi / 2f, 0f, progress);
var point = center + new Vector2(
	radiusX * Mathf.Cos(angle),
	radiusY * Mathf.Sin(angle));
```

For `DrawPolygon()`, the color array must contain exactly one color per polygon point. Preserve a consistent clockwise or counterclockwise vertex order to avoid self-intersections.

## Keep Related Lines Consistent

Derive inner outlines, glow rails, and edge marks from the same geometry rather than inventing independent offsets. For an inset ellipse, adjust its center, radii, and endpoints together so it remains visually parallel to the outer edge.

If a portrait or other child overlaps negative local coordinates, confirm that neither the custom control nor its ancestors clip children or canvas drawing. Keep visual overlap separate from container sizing and input ownership.

## Validate Correctly

A successful C# build proves API and type correctness, not visual correctness. After building:

1. Run the relevant scene.
2. Inspect both fill and outline for gaps or doubled borders.
3. Confirm the curve bends in the requested direction.
4. Verify the shape at the actual control size and with overlapping HUD elements.

Prefer adjusting explicit endpoints or radii from visual feedback instead of replacing the geometry model.
