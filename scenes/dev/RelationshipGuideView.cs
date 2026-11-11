using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Graphics;
using GrimSpace.Math.Grid;

namespace GrimSpace.Scenes.Dev;

public partial class RelationshipGuideView : Node3D
{
	public enum ReferencePlane { XZ, YZ, XY }

	private readonly ImmediateMesh _mesh = new();
	private readonly Label3D[] _distances = new Label3D[2];
	private Sprite3D _reticle = null!;

	public override void _Ready()
	{
		var visual = new MeshInstance3D
		{
			Mesh = _mesh,
			MaterialOverride = CellGridGeometry.CreateMaterial(Colors.White),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(visual);
		AddChild(visual);
		for (var i = 0; i < _distances.Length; i++)
		{
			var label = new Label3D
			{
				Name = $"SegmentDistance{i}",
				Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
				FixedSize = true,
				PixelSize = 0.0012f,
				FontSize = 18,
				OutlineSize = 4,
				OutlineModulate = new Color(0.02f, 0.05f, 0.07f, 0.9f),
				Offset = new Vector2(0, -14),
			};
			PresentationLayers.MarkUx(label);
			AddChild(label);
			_distances[i] = label;
		}
		const string reticlePath = "res://assets/ui/battle/enemy_target_reticle.svg";
		_reticle = new Sprite3D
		{
			Texture = GD.Load<Texture2D>(reticlePath)
				?? throw new InvalidOperationException($"Could not load enemy target reticle '{reticlePath}'."),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			PixelSize = 0.0115f,
			Modulate = new Color(0.9f, 0.25f, 0.3f),
			Shaded = false,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
		};
		PresentationLayers.MarkUx(_reticle);
		AddChild(_reticle);
	}

	public bool Apply(Coord sourceCell, Coord targetCell, ReferencePlane plane)
	{
		var target = WorldMapping.ToWorld(targetCell);
		var source = WorldMapping.ToWorld(sourceCell);
		var (u, v, normal) = plane switch
		{
			ReferencePlane.XZ => (Vector3.Right, Vector3.Back, Vector3.Up),
			ReferencePlane.YZ => (Vector3.Up, Vector3.Back, Vector3.Right),
			ReferencePlane.XY => (Vector3.Right, Vector3.Up, Vector3.Back),
			_ => throw new ArgumentOutOfRangeException(nameof(plane), plane, "Unknown reference plane."),
		};
		var offset = (source - target).Dot(normal);
		var projected = source - normal * offset;
		var aligned = Mathf.IsZeroApprox(offset);
		var connectionTint = new Color(0.6f, 0.85f, 1f);
		var projectionTint = aligned ? new Color(0.32f, 0.78f, 0.56f) : Colors.Gold;
		UpdateDistance(0, source, projected, projectionTint);
		UpdateDistance(1, projected, target, connectionTint);
		_reticle.Position = target;

		_mesh.ClearSurfaces();
		_mesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		void Line(Vector3 from, Vector3 to, Color color)
		{
			if (from.IsEqualApprox(to))
				return;
			_mesh.SurfaceSetColor(color);
			_mesh.SurfaceAddVertex(from);
			_mesh.SurfaceAddVertex(to);
		}

		Line(source, projected, projectionTint);
		Line(projected, target, connectionTint);
		var markerRadius = WorldMapping.CellSize * (aligned ? 0.28f : 0.18f);
		for (var i = 0; i < 32; i++)
		{
			var a = Mathf.Tau * i / 32f;
			var b = Mathf.Tau * (i + 1) / 32f;
			Line(projected + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * markerRadius,
				projected + (u * Mathf.Cos(b) + v * Mathf.Sin(b)) * markerRadius, projectionTint);
		}
		if (aligned)
		{
			Line(projected - u * markerRadius, projected + u * markerRadius, projectionTint);
			Line(projected - v * markerRadius, projected + v * markerRadius, projectionTint);
		}
		else if (!projected.IsEqualApprox(target))
		{
			var cornerSize = Mathf.Min(WorldMapping.CellSize * 0.22f,
				Mathf.Min(Mathf.Abs(offset), projected.DistanceTo(target)) * 0.5f);
			var towardSource = normal * Mathf.Sign(offset) * cornerSize;
			var towardTarget = (target - projected).Normalized() * cornerSize;
			Line(projected + towardSource, projected + towardSource + towardTarget, projectionTint);
			Line(projected + towardTarget, projected + towardSource + towardTarget, projectionTint);
		}
		_mesh.SurfaceEnd();
		return aligned;
	}

	private void UpdateDistance(int index, Vector3 from, Vector3 to, Color tint)
	{
		var label = _distances[index];
		label.Visible = !from.IsEqualApprox(to);
		label.Position = (from + to) * 0.5f;
		label.Modulate = tint;
		label.Text = BattleDistance.Format(
			from.DistanceTo(to) / WorldMapping.CellSize * BattleDistance.MetersPerCell);
	}
}
