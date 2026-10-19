using Godot;
using GrimSpace.Battle.Player;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Presentation.Graphics;

public sealed partial class EnemyRangeTetherView : Node3D
{
	private const float GridRadiusCells = 1.8f;
	private const float TetherMarkLength = 0.22f;
	private const float TetherMarkSpacing = 0.72f;
	private const float EndpointChangeThresholdSquared = 0.000001f;

	private readonly ImmediateMesh _lineMesh = new();
	private readonly ArrayMesh _gridMesh = new();
	private BoundedGrid _grid = null!;
	private ShaderMaterial _gridMaterial = null!;
	private MeshInstance3D _gridVisual = null!;
	private MeshInstance3D _line = null!;
	private Label3D _distanceLabel = null!;
	private EnemyTargetInfoOverlay _targetInfo = null!;
	private Sprite3D _targetInfoVisual = null!;
	private Node3D? _source;
	private Node3D? _target;
	private Vector3? _lastWorldFrom;
	private Vector3? _lastWorldTo;
	private LatticeBounds? _latticeBounds;

	public void Configure(BoundedGrid grid) =>
		_grid = grid;

	public override void _Ready()
	{
		if (_grid is null)
			throw new InvalidOperationException("Enemy range tether must be configured before entering the scene tree.");

		_gridMaterial = CreateGridMaterial();
		_gridVisual = new MeshInstance3D
		{
			Name = "LocalGrid",
			Mesh = _gridMesh,
			MaterialOverride = _gridMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkUx(_gridVisual);
		AddChild(_gridVisual);

		_line = new MeshInstance3D
		{
			Name = "Line",
			Mesh = _lineMesh,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			MaterialOverride = new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(0.16f, 0.56f, 0.58f, 0.46f),
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			},
		};
		PresentationLayers.MarkUx(_line);
		AddChild(_line);

		_distanceLabel = new Label3D
		{
			Name = "Distance",
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FixedSize = true,
			PixelSize = 0.0012f,
			FontSize = 18,
			OutlineSize = 4,
			Modulate = new Color(0.48f, 0.82f, 0.82f, 0.86f),
			OutlineModulate = new Color(0.02f, 0.05f, 0.07f, 0.9f),
		};
		AddChild(_distanceLabel);

		var targetInfoViewport = new SubViewport
		{
			Name = "TargetInfoViewport",
			Size = new Vector2I(240, 160),
			TransparentBg = true,
			Disable3D = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
		};
		_targetInfo = new EnemyTargetInfoOverlay { Name = "TargetInfo" };
		targetInfoViewport.AddChild(_targetInfo);
		AddChild(targetInfoViewport);

		_targetInfoVisual = new Sprite3D
		{
			Name = "TargetInfoVisual",
			Texture = targetInfoViewport.GetTexture(),
			Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
			FixedSize = false,
			PixelSize = 0.023f,
			TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
			Shaded = false,
		};
		AddChild(_targetInfoVisual);
		Hide();
	}

	public void Apply(
		Node3D? source,
		Node3D? target,
		float separationMeters,
		UnitDisplayState? sourceState = null,
		UnitDisplayState? targetState = null)
	{
		_source = source;
		_target = target;
		if (source is null || target is null || sourceState is null || targetState is null)
		{
			Hide();
			return;
		}

		_distanceLabel.Text = BattleDistance.Format(separationMeters);
		var shieldFace = targetState.ToBodyFrame().HitFaceFrom(sourceState.Position);
		var maxShield = targetState.Loadout.MaxShieldPoints[shieldFace];
		_targetInfo.Set(
			$"{DisplayName(targetState.Type).ToUpperInvariant()} "
				+ $"({ShipLoadoutGearTier.FromLoadout(targetState.Loadout, targetState.Type)})",
			targetState.MaxHullPoints > 0
				? targetState.HullPoints / (float)targetState.MaxHullPoints
				: 0f,
			maxShield > 0
				? targetState.ShieldPoints[shieldFace] / (float)maxShield
				: 0f);
		Show();
		UpdateVisual();
	}

	public override void _Process(double delta)
	{
		if (!Visible)
			return;
		if (!IsInstanceValid(_source) || !IsInstanceValid(_target))
		{
			Hide();
			return;
		}
		UpdateVisual();
	}

	private void UpdateVisual()
	{
		var worldFrom = _source!.GlobalPosition;
		var worldTo = _target!.GlobalPosition;
		if (_lastWorldFrom is Vector3 previousFrom
			&& _lastWorldTo is Vector3 previousTo
			&& previousFrom.DistanceSquaredTo(worldFrom) <= EndpointChangeThresholdSquared
			&& previousTo.DistanceSquaredTo(worldTo) <= EndpointChangeThresholdSquared)
			return;

		_lastWorldFrom = worldFrom;
		_lastWorldTo = worldTo;
		var from = ToLocal(worldFrom);
		var to = ToLocal(worldTo);
		var tether = to - from;
		var length = tether.Length();
		var direction = length > 0f ? tether / length : Vector3.Zero;
		_lineMesh.ClearSurfaces();
		_lineMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
		for (var offset = 0f; offset < length; offset += TetherMarkSpacing)
		{
			_lineMesh.SurfaceAddVertex(from + direction * offset);
			_lineMesh.SurfaceAddVertex(
				from + direction * Mathf.Min(offset + TetherMarkLength, length));
		}
		_lineMesh.SurfaceEnd();
		_distanceLabel.Position = from.Lerp(to, 0.5f);
		_targetInfoVisual.Position = to;

		_gridMaterial.SetShaderParameter("tether_from", worldFrom);
		_gridMaterial.SetShaderParameter("tether_to", worldTo);
		RefreshGridMesh(worldFrom, worldTo);
	}

	private void RefreshGridMesh(Vector3 from, Vector3 to)
	{
		var radius = GridRadiusCells * WorldMapping.CellSize;
		var minimum = from.Min(to) - Vector3.One * radius;
		var maximum = from.Max(to) + Vector3.One * radius;
		var bounds = new LatticeBounds(
			Mathf.Clamp(Mathf.FloorToInt(minimum.X / WorldMapping.CellSize), 0, _grid.Width),
			Mathf.Clamp(Mathf.CeilToInt(maximum.X / WorldMapping.CellSize), 0, _grid.Width),
			Mathf.Clamp(Mathf.FloorToInt(minimum.Y / WorldMapping.CellSize), 0, _grid.Height),
			Mathf.Clamp(Mathf.CeilToInt(maximum.Y / WorldMapping.CellSize), 0, _grid.Height),
			Mathf.Clamp(Mathf.FloorToInt(minimum.Z / WorldMapping.CellSize), 0, _grid.Depth),
			Mathf.Clamp(Mathf.CeilToInt(maximum.Z / WorldMapping.CellSize), 0, _grid.Depth));
		if (_latticeBounds == bounds)
			return;

		_latticeBounds = bounds;
		var vertices = new List<Vector3>();
		for (var y = bounds.MinY; y <= bounds.MaxY; y++)
		{
			for (var z = bounds.MinZ; z <= bounds.MaxZ; z++)
			{
				AddGridLine(
					vertices,
					new Vector3(bounds.MinX, y, z),
					new Vector3(bounds.MaxX, y, z));
			}
		}
		for (var x = bounds.MinX; x <= bounds.MaxX; x++)
		{
			for (var z = bounds.MinZ; z <= bounds.MaxZ; z++)
			{
				AddGridLine(
					vertices,
					new Vector3(x, bounds.MinY, z),
					new Vector3(x, bounds.MaxY, z));
			}
		}
		for (var x = bounds.MinX; x <= bounds.MaxX; x++)
		{
			for (var y = bounds.MinY; y <= bounds.MaxY; y++)
			{
				AddGridLine(
					vertices,
					new Vector3(x, y, bounds.MinZ),
					new Vector3(x, y, bounds.MaxZ));
			}
		}

		var arrays = new Godot.Collections.Array();
		arrays.Resize((int)Mesh.ArrayType.Max);
		arrays[(int)Mesh.ArrayType.Vertex] = vertices.ToArray();
		_gridMesh.ClearSurfaces();
		_gridMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
	}

	private void AddGridLine(List<Vector3> vertices, Vector3 from, Vector3 to)
	{
		vertices.Add(ToLocal(from * WorldMapping.CellSize));
		vertices.Add(ToLocal(to * WorldMapping.CellSize));
	}

	private static ShaderMaterial CreateGridMaterial() =>
		new()
		{
			Shader = new Shader
			{
				Code =
					"""
					shader_type spatial;
					render_mode unshaded, blend_mix, depth_draw_never, cull_disabled;

					uniform vec3 tether_from;
					uniform vec3 tether_to;
					uniform float radius = 3.6;
					uniform vec3 tint : source_color = vec3(0.10, 0.46, 0.48);

					varying vec3 world_position;

					void vertex() {
						world_position = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
					}

					void fragment() {
						vec3 segment = tether_to - tether_from;
						float segment_length_squared = dot(segment, segment);
						float along = segment_length_squared > 0.0001
							? clamp(dot(world_position - tether_from, segment) / segment_length_squared, 0.0, 1.0)
							: 0.0;
						float distance_to_tether = distance(
							world_position,
							tether_from + segment * along);
						float proximity = 1.0 - smoothstep(0.0, radius, distance_to_tether);
						ALBEDO = tint;
						EMISSION = tint * proximity * 0.55;
						ALPHA = pow(proximity, 1.2) * 0.42;
					}
					""",
			},
		};

	private static string DisplayName(EType type) =>
		type switch
		{
			EType.IndustrialGooper => "Industrial Gooper",
			EType.RepurposedMiner => "Repurposed Miner",
			EType.VoidBomb => "Void Bomb",
			_ => type.ToString(),
		};

	private readonly record struct LatticeBounds(
		int MinX,
		int MaxX,
		int MinY,
		int MaxY,
		int MinZ,
		int MaxZ);
}
