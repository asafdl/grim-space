using System;
using System.Collections.Generic;
using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// Visual-only scrap drone swarm. All <see cref="Play"/> inputs are world space.
/// Instantiate once per shot, parent, then call <see cref="Play"/>.
/// </summary>
public partial class ScrapDroneAttack : Node3D
{
	[Signal]
	public delegate void TargetReachedEventHandler(int targetIndex, Vector3 worldPosition);

	[Signal]
	public delegate void CompletedEventHandler();

	[Export]
	public PackedScene DroneScene { get; set; } = null!;

	[Export]
	public float DroneSize { get; set; } = 0.18f;

	[Export]
	public Vector3 ModelRotationDegrees { get; set; } = Vector3.Zero;

	[Export]
	public uint RenderLayers { get; set; } = PresentationLayers.World;

	[Export]
	public float BubbleRadius { get; set; } = 0.28f;

	[Export]
	public float TeamRadius { get; set; } = 0.16f;

	[Export]
	public float SplitDistance { get; set; } = 1.0f;

	[Export]
	public float LaunchSeconds { get; set; } = 0.32f;

	[Export]
	public float FlightSeconds { get; set; } = 0.65f;

	[Export]
	public float FadeSeconds { get; set; } = 0.18f;

	[Export]
	public float WeaveAmount { get; set; } = 0.035f;

	[Export]
	public float PlaybackSpeed { get; set; } = 1f;

	public float ImpactSeconds => LaunchSeconds + FlightSeconds;

	public float DurationSeconds => ImpactSeconds + FadeSeconds;

	private sealed class Drone
	{
		public Node3D Root = null!;
		public Node3D Engine = null!;
		public readonly List<GeometryInstance3D> Geometry = new();
		public readonly List<GeometryInstance3D> EngineGeometry = new();
		public Vector3 BubbleOffset, Slot, LastPosition;
		public int Team;
		public float Phase;
	}

	private readonly List<Drone> _drones = new();
	private Vector3[] _targets = Array.Empty<Vector3>();
	private Vector3 _origin, _forward, _up, _right, _split, _missEnd;
	private float _time;
	private bool _played, _impactSent;

	public override void _Ready() => SetProcess(false);

	public void Play(
		Vector3 origin,
		Vector3 forward,
		Vector3 up,
		IReadOnlyList<Vector3> hitPositions,
		Vector3 missEnd)
	{
		if (_played)
			throw new InvalidOperationException("Instantiate a new effect for each attack.");
		if (!IsInsideTree())
			throw new InvalidOperationException("AddChild before Play.");
		if (DroneScene == null)
			throw new InvalidOperationException("Assign DroneScene.");
		if (hitPositions == null)
			throw new ArgumentNullException(nameof(hitPositions));
		if (forward.LengthSquared() < 0.0001f || LaunchSeconds <= 0 || FlightSeconds <= 0 ||
		    FadeSeconds <= 0 || DroneSize <= 0 || SplitDistance < 0 || BubbleRadius < 0 || TeamRadius < 0)
			throw new ArgumentException("Invalid direction, size, spacing or timing.");

		_played = true;
		TopLevel = true;
		GlobalTransform = Transform3D.Identity;
		_origin = origin;
		_forward = forward.Normalized();
		_up = up - _forward * up.Dot(_forward);
		if (_up.LengthSquared() < 0.0001f)
			_up = System.Math.Abs(_forward.Dot(Vector3.Up)) < 0.95f ? Vector3.Up : Vector3.Right;
		_right = _forward.Cross(_up).Normalized();
		_up = _right.Cross(_forward).Normalized();
		_targets = new Vector3[hitPositions.Count];
		float nearest = origin.DistanceTo(missEnd);
		if (_targets.Length > 0)
			nearest = float.MaxValue;
		for (int i = 0; i < _targets.Length; i++)
		{
			_targets[i] = hitPositions[i];
			nearest = System.Math.Min(nearest, origin.DistanceTo(_targets[i]));
		}

		_split = origin + _forward * System.Math.Min(SplitDistance, nearest * 0.3f);
		_missEnd = missEnd;
		int count = System.Math.Max(1, _targets.Length) * 4;
		for (int i = 0; i < count; i++)
		{
			float y = 1f - 2f * (i + 0.5f) / count;
			float r = Mathf.Sqrt(1f - y * y);
			float angle = i * 2.3999632f;
			int slot = i % 4;
			var drone = new Drone
			{
				Team = i / 4,
				Phase = i * 1.73f,
				BubbleOffset = (_right * (r * Mathf.Cos(angle)) + _up * y +
				                _forward * (r * Mathf.Sin(angle))) * BubbleRadius,
				Slot = (_right * (slot % 2 == 0 ? -1f : 1f) +
				        _up * (slot < 2 ? -1f : 1f)) * TeamRadius * 0.7071f,
				Root = new Node3D { Name = $"Drone{i}" },
			};
			AddChild(drone.Root);
			BuildVisual(drone);
			_drones.Add(drone);
			drone.LastPosition = PositionAt(drone, 0);
			drone.Root.Position = drone.LastPosition;
			drone.Root.Basis = Basis.LookingAt(_forward, _up);
		}

		SetProcess(true);
	}

	private void BuildVisual(Drone drone)
	{
		var correction = new Node3D { RotationDegrees = ModelRotationDegrees };
		var normalized = new Node3D();
		drone.Root.AddChild(correction);
		correction.AddChild(normalized);
		var model = DroneScene.Instantiate<Node3D>();
		normalized.AddChild(model);
		Collect(model, drone.Geometry);
		var body = new List<GeometryInstance3D>();
		foreach (var g in drone.Geometry)
		{
			if (g is MeshInstance3D && g.Name.ToString().StartsWith("droid", StringComparison.OrdinalIgnoreCase))
				body.Add(g);
		}

		var measured = body.Count > 0 ? body : drone.Geometry;
		bool first = true;
		Vector3 min = Vector3.Zero, max = Vector3.Zero;
		foreach (var g in measured)
		{
			if (g is not MeshInstance3D mesh || mesh.Mesh == null)
				continue;
			var bounds = mesh.GetAabb();
			var transform = normalized.GlobalTransform.AffineInverse() * mesh.GlobalTransform;
			for (int k = 0; k < 8; k++)
			{
				var p = transform * (bounds.Position + new Vector3(
					(k & 1) == 0 ? 0 : bounds.Size.X,
					(k & 2) == 0 ? 0 : bounds.Size.Y,
					(k & 4) == 0 ? 0 : bounds.Size.Z));
				if (first)
				{
					min = max = p;
					first = false;
				}
				else
				{
					min = min.Min(p);
					max = max.Max(p);
				}
			}
		}

		if (!first)
		{
			var size = max - min;
			float scale = DroneSize / System.Math.Max(0.0001f, System.Math.Max(size.X, System.Math.Max(size.Y, size.Z)));
			normalized.Scale = Vector3.One * scale;
			normalized.Position = -(min + max) * 0.5f * scale;
		}

		AddEngineGlow(drone);
	}

	private void AddEngineGlow(Drone drone)
	{
		float size = DroneSize;
		float exhaustLength = size * 0.30f;

		var engine = new Node3D
		{
			Name = "Engine",
			Position = new Vector3(0f, 0f, size * 0.48f),
		};

		drone.Root.AddChild(engine);
		drone.Engine = engine;

		var haloTexture = new GradientTexture2D
		{
			Width = 64,
			Height = 64,
			Fill = GradientTexture2D.FillEnum.Radial,
			FillFrom = new Vector2(0.5f, 0.5f),
			FillTo = new Vector2(1f, 0.5f),
			Gradient = new Gradient
			{
				Offsets = new[] { 0f, 0.2f, 1f },
				Colors = new[]
				{
					new Color(1f, 0.85f, 0.45f, 0.45f),
					new Color(1f, 0.55f, 0.12f, 0.18f),
					new Color(0f, 0f, 0f, 0f),
				},
			},
		};

		var exhaustMaterial = new ShaderMaterial
		{
			Shader = new Shader
			{
				Code = """
					shader_type spatial;
					render_mode unshaded, blend_add, depth_draw_never, cull_disabled;

					uniform float plume_length = 1.0;
					varying float along_plume;

					void vertex() {
						along_plume = clamp(
							VERTEX.y / plume_length + 0.5,
							0.0,
							1.0
						);
					}

					void fragment() {
						float t = clamp(along_plume, 0.0, 1.0);

						vec3 hot = vec3(1.0, 0.94, 0.65);
						vec3 warm = vec3(1.0, 0.45, 0.08);

						float tip_fade = 1.0 - smoothstep(0.05, 1.0, t);
						float edge_fade = smoothstep(
							0.0,
							0.6,
							abs(dot(normalize(NORMAL), normalize(VIEW)))
						);

						ALBEDO = mix(hot, warm, t);
						ALPHA = tip_fade * edge_fade * 0.55;
					}
					""",
			},
		};

		exhaustMaterial.SetShaderParameter("plume_length", exhaustLength);

		AddPart(
			"Core",
			new SphereMesh
			{
				Radius = size * 0.025f,
				Height = size * 0.05f,
				RadialSegments = 12,
				Rings = 6,
			},
			new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				AlbedoColor = new Color(1f, 0.94f, 0.72f),
				EmissionEnabled = true,
				Emission = new Color(1f, 0.75f, 0.35f),
				EmissionEnergyMultiplier = 1.5f,
			},
			Vector3.Zero,
			Vector3.Zero);

		AddPart(
			"Halo",
			new QuadMesh
			{
				Size = Vector2.One * size * 0.14f,
			},
			new StandardMaterial3D
			{
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				CullMode = BaseMaterial3D.CullModeEnum.Disabled,
				BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
				BillboardKeepScale = true,
				AlbedoColor = Colors.White,
				AlbedoTexture = haloTexture,
			},
			Vector3.Zero,
			Vector3.Zero);

		AddPart(
			"Exhaust",
			new CylinderMesh
			{
				TopRadius = 0f,
				BottomRadius = size * 0.035f,
				Height = exhaustLength,
				RadialSegments = 16,
				Rings = 4,
				CapTop = false,
				CapBottom = false,
			},
			exhaustMaterial,
			new Vector3(0f, 0f, exhaustLength * 0.5f),
			new Vector3(90f, 0f, 0f));

		void AddPart(
			string name,
			Mesh mesh,
			Material material,
			Vector3 position,
			Vector3 rotationDegrees)
		{
			var part = new MeshInstance3D
			{
				Name = name,
				Mesh = mesh,
				MaterialOverride = material,
				Position = position,
				RotationDegrees = rotationDegrees,
				Layers = RenderLayers,
				CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			};

			engine.AddChild(part);
			drone.EngineGeometry.Add(part);
		}
	}

	private void Collect(Node node, List<GeometryInstance3D> geometry)
	{
		if (node is AnimationPlayer animation)
			animation.Stop();

		if (node is GeometryInstance3D g)
		{
			g.Layers = RenderLayers;
			g.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
			geometry.Add(g);
		}

		foreach (Node child in node.GetChildren())
			Collect(child, geometry);
	}

	private Vector3 PositionAt(Drone d, float time)
	{
		if (time <= LaunchSeconds)
		{
			float u = Mathf.Clamp(time / LaunchSeconds, 0, 1);
			return _origin.Lerp(_split, u * u) + d.BubbleOffset * Mathf.Lerp(0.15f, 1f, Smooth(u));
		}

		float t = Mathf.Clamp((time - LaunchSeconds) / FlightSeconds, 0, 1);
		bool miss = _targets.Length == 0;
		Vector3 start = _split + d.BubbleOffset;
		Vector3 destination = miss ? _missEnd + d.Slot * 1.8f : _targets[d.Team] + d.Slot * 0.3f;
		Vector3 approach = destination - _split;
		float distance = approach.Length();
		Vector3 heading = distance > 0.0001f ? approach / distance : _forward;
		Vector3 p1 = start + _forward * distance * 0.32f;
		Vector3 p2 = destination - heading * distance * 0.3f + d.Slot;
		Vector3 p = Bezier(start, p1, p2, destination, Smooth(t));
		float envelope = Mathf.Sin(Mathf.Pi * t);
		p += (_right * Mathf.Sin(time * 11f + d.Phase) +
		      _up * Mathf.Cos(time * 9f + d.Phase)) * WeaveAmount * envelope;
		return p;
	}

	public override void _Process(double delta)
	{
		_time += (float)delta * System.Math.Max(0, PlaybackSpeed);
		bool miss = _targets.Length == 0;
		foreach (var d in _drones)
		{
			Vector3 p = PositionAt(d, _time);
			Vector3 velocity = p - d.LastPosition;
			d.Root.Position = p;
			if (velocity.LengthSquared() > 0.0000001f)
			{
				var direction = velocity.Normalized();
				var up = System.Math.Abs(direction.Dot(_up)) > 0.97f ? _right : _up;
				d.Root.Basis = Basis.LookingAt(direction, up);
			}

			d.LastPosition = p;
			float pulse =
				1f
				+ 0.06f * Mathf.Sin(_time * 37f + d.Phase)
				+ 0.03f * Mathf.Sin(_time * 61f + d.Phase * 1.7f);
			d.Engine.Scale = new Vector3(1f, 1f, pulse);
			float fadeStart = miss ? LaunchSeconds + FlightSeconds * 0.72f : ImpactSeconds;
			float fadeEnd = miss ? ImpactSeconds : DurationSeconds;
			float fade = Mathf.Clamp((_time - fadeStart) / (fadeEnd - fadeStart), 0, 1);
			foreach (var g in d.Geometry)
				g.Transparency = fade;
			// Additive engine parts blow out under the same Transparency value long before the hull.
			foreach (var g in d.EngineGeometry)
				g.Transparency = 0f;
			d.Engine.Visible = fade < 1f;
		}

		if (!_impactSent && _time >= ImpactSeconds)
		{
			_impactSent = true;
			for (int i = 0; i < _targets.Length; i++)
				EmitSignal(SignalName.TargetReached, i, _targets[i]);
		}

		if (_time >= DurationSeconds)
		{
			SetProcess(false);
			EmitSignal(SignalName.Completed);
			QueueFree();
		}
	}

	private static float Smooth(float t) => t * t * (3f - 2f * t);

	private static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
	{
		float s = 1 - t;
		return a * (s * s * s) + b * (3 * s * s * t) + c * (3 * s * t * t) + d * (t * t * t);
	}
}
