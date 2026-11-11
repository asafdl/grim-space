using Godot;
using GrimSpace.Battle.Presentation;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class RelationshipGuideView : Node3D
{
	private const float TetherMarkLength = 0.22f;
	private const float TetherMarkSpacing = 0.72f;
	public const float AlignmentDistanceMeters = 10_000f;

	public enum ReferencePlane { XZ, YZ, XY }

	public bool ShowReticle { get; init; } = true;

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
			Visible = ShowReticle,
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

	public bool Apply(Coord sourceCell, Coord targetCell, ReferencePlane plane, bool showAlignment = true)
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
		if (!showAlignment)
			projected = source;
		var aligned = Mathf.IsZeroApprox(offset);
		var connectionTint = new Color(0.16f, 0.56f, 0.58f, 0.46f);
		var projectionTint = new Color(0.72f, 0.5f, 0.18f, 0.46f);
		var markerTint = aligned
			? new Color(0.32f, 0.78f, 0.56f)
			: new Color(0.9f, 0.7f, 0.36f);
		UpdateDistance(0, source, projected, new Color(0.9f, 0.7f, 0.36f, 0.86f));
		UpdateDistance(1, projected, target, new Color(0.48f, 0.82f, 0.82f, 0.86f));
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

		void DashedLine(Vector3 from, Vector3 to, Color color)
		{
			var length = from.DistanceTo(to);
			if (Mathf.IsZeroApprox(length))
				return;
			var direction = (to - from) / length;
			for (var offset = 0f; offset < length; offset += TetherMarkSpacing)
			{
				Line(from + direction * offset,
					from + direction * Mathf.Min(offset + TetherMarkLength, length), color);
			}
		}

		DashedLine(source, projected, projectionTint);
		DashedLine(projected, target, connectionTint);
		if (!showAlignment)
		{
			_mesh.SurfaceEnd();
			return aligned;
		}
		var markerRadius = WorldMapping.CellSize * (aligned ? 0.28f : 0.18f);
		for (var i = 0; i < 32; i++)
		{
			var a = Mathf.Tau * i / 32f;
			var b = Mathf.Tau * (i + 1) / 32f;
			Line(projected + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * markerRadius,
				projected + (u * Mathf.Cos(b) + v * Mathf.Sin(b)) * markerRadius, markerTint);
		}
		if (aligned)
		{
			Line(projected - u * markerRadius, projected + u * markerRadius, markerTint);
			Line(projected - v * markerRadius, projected + v * markerRadius, markerTint);
		}
		else if (!projected.IsEqualApprox(target))
		{
			var cornerSize = Mathf.Min(WorldMapping.CellSize * 0.22f,
				Mathf.Min(Mathf.Abs(offset), projected.DistanceTo(target)) * 0.5f);
			var towardSource = normal * Mathf.Sign(offset) * cornerSize;
			var towardTarget = (target - projected).Normalized() * cornerSize;
			Line(projected + towardSource, projected + towardSource + towardTarget, markerTint);
			Line(projected + towardTarget, projected + towardSource + towardTarget, markerTint);
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
