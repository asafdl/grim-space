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
	public bool AnimateAlignment { get; init; }

	private readonly ImmediateMesh _mesh = new();
	private (Coord Source, Coord Target, ReferencePlane Plane)? _alignment;
	private float _dashOffset;
	private float _ringPhase;
	private bool _lineAligned;
	private bool _planeAligned;
	private readonly Label3D[] _distances = new Label3D[2];
	private Sprite3D _reticle = null!;

	public override void _Ready()
	{
		var material = CellGridGeometry.CreateMaterial(Colors.White);
		if (AnimateAlignment)
			material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
		var visual = new MeshInstance3D
		{
			Mesh = _mesh,
			MaterialOverride = material,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(visual);
		AddChild(visual);
		SetProcess(AnimateAlignment);
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

	public override void _Process(double delta)
	{
		if (!IsVisibleInTree() || _alignment is not { } alignment)
			return;
		if (_lineAligned)
			_dashOffset = (_dashOffset + (float)delta * 1.4f) % TetherMarkSpacing;
		if (_planeAligned)
			_ringPhase = (_ringPhase + (float)delta / 2.4f) % 1f;
		Apply(alignment.Source, alignment.Target, alignment.Plane);
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
		_lineAligned = AnimateAlignment && showAlignment
			&& !projected.IsEqualApprox(target)
			&& (Mathf.IsZeroApprox((target - projected).Dot(u))
				|| Mathf.IsZeroApprox((target - projected).Dot(v)));
		var wasPlaneAligned = _planeAligned;
		_planeAligned = AnimateAlignment && showAlignment && aligned;
		if (_planeAligned && !wasPlaneAligned)
			_ringPhase = 0f;
		_alignment = _lineAligned || _planeAligned ? (sourceCell, targetCell, plane) : null;
		var projectionTint = new Color(0.72f, 0.5f, 0.18f, 0.46f);
		var markerTint = aligned
			? new Color(0.32f, 0.78f, 0.56f)
			: new Color(0.9f, 0.7f, 0.36f);
		if (_planeAligned)
			markerTint = new Color(0.9f, 0.7f, 0.36f);
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

		void DashedLine(Vector3 from, Vector3 to, Color color, float dashOffset = 0f)
		{
			var length = from.DistanceTo(to);
			if (Mathf.IsZeroApprox(length))
				return;
			var direction = (to - from) / length;
			for (var offset = dashOffset - TetherMarkSpacing; offset < length; offset += TetherMarkSpacing)
			{
				var end = Mathf.Min(offset + TetherMarkLength, length);
				if (end > 0f)
					Line(from + direction * Mathf.Max(offset, 0f),
						from + direction * end, color);
			}
		}

		DashedLine(source, projected, projectionTint);
		if (!_lineAligned)
			DashedLine(projected, target, connectionTint);
		if (!showAlignment)
		{
			_mesh.SurfaceEnd();
			return aligned;
		}
		var markerRadius = WorldMapping.CellSize * (aligned ? 0.28f : 0.18f);
		void Ring(float radius, Color tint)
		{
			for (var i = 0; i < 32; i++)
			{
				var a = Mathf.Tau * i / 32f;
				var b = Mathf.Tau * (i + 1) / 32f;
				Line(projected + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * radius,
					projected + (u * Mathf.Cos(b) + v * Mathf.Sin(b)) * radius, tint);
			}
		}
		Ring(markerRadius, markerTint);
		if (_planeAligned)
			Ring(markerRadius + WorldMapping.CellSize * 0.2f * _ringPhase,
				new Color(markerTint.R, markerTint.G, markerTint.B, 1f - _ringPhase));
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
		if (_lineAligned)
		{
			var direction = (target - projected).Normalized();
			var camera = GetViewport().GetCamera3D();
			var viewDirection = camera is null ? normal : camera.GlobalPosition - (projected + target) * 0.5f;
			var side = direction.Cross(viewDirection).Normalized();
			if (side.IsZeroApprox())
				side = direction.Cross(normal).Normalized();
			var edge = side * 0.02f;
			var length = projected.DistanceTo(target);
			_mesh.SurfaceBegin(Mesh.PrimitiveType.Triangles);
			_mesh.SurfaceSetColor(connectionTint);
			for (var start = _dashOffset - TetherMarkSpacing; start < length; start += TetherMarkSpacing)
			{
				var end = Mathf.Min(start + TetherMarkLength, length);
				if (end <= 0f)
					continue;
				var from = projected + direction * Mathf.Max(start, 0f);
				var to = projected + direction * end;
				_mesh.SurfaceAddVertex(from - edge);
				_mesh.SurfaceAddVertex(from + edge);
				_mesh.SurfaceAddVertex(to + edge);
				_mesh.SurfaceAddVertex(from - edge);
				_mesh.SurfaceAddVertex(to + edge);
				_mesh.SurfaceAddVertex(to - edge);
			}
			_mesh.SurfaceEnd();
		}
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
