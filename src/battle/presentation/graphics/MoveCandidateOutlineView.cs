using Godot;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class MoveCandidateOutlineView : MeshInstance3D
{
	private readonly ImmediateMesh _mesh = new();
	private IReadOnlySet<Coord>? _cells;

	public override void _Ready()
	{
		Mesh = _mesh;
		var material = CellGridGeometry.CreateMaterial(new Color(0.35f, 0.9f, 1f));
		material.NoDepthTest = true;
		MaterialOverride = material;
		CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		PresentationLayers.MarkUx(this);
		Visible = false;
	}

	public void Apply(IReadOnlySet<Coord>? cells, bool enabled)
	{
		Visible = enabled && cells is { Count: > 0 };
		if (ReferenceEquals(cells, _cells))
			return;
		_cells = cells;
		_mesh.ClearSurfaces();
		if (cells is not { Count: > 0 })
			return;

		_mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		foreach (var cell in cells)
		{
			var center = WorldMapping.ToWorld(cell);
			var half = WorldMapping.CellSize * 0.47f;
			for (var axis = 0; axis < 3; axis++)
			for (var a = -1; a <= 1; a += 2)
			for (var b = -1; b <= 1; b += 2)
			{
				var from = Vector3.Zero;
				from[axis] = -half;
				from[(axis + 1) % 3] = a * half;
				from[(axis + 2) % 3] = b * half;
				var to = from;
				to[axis] = half;
				_mesh.SurfaceAddVertex(center + from);
				_mesh.SurfaceAddVertex(center + to);
			}
		}
		_mesh.SurfaceEnd();
	}
}
