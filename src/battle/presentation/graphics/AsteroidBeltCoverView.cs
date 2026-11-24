using Godot;
using GrimSpace.Battle.NonUnits;
using GrimSpace.Battle.Presentation;
using GrimSpace.Presentation.Graphics;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class AsteroidBeltCoverView : Node3D
{
	private const double HitDuration = 0.55;
	private const int ShatterChunks = 3;
	private const double DustLifetime = 0.35;
	private const string DustTexturePath = "res://assets/textures/particles/dust_smoke.jpg";
	private static Texture2D? _dustTexture;
	private static readonly Shader DustShader = new()
	{
		Code =
			"""
			shader_type spatial;
			render_mode unshaded, cull_disabled, depth_draw_never, shadows_disabled;

			uniform sampler2D smoke_texture : source_color, filter_linear, repeat_enable;
			uniform sampler2D cloud_texture : source_color, filter_linear;
			uniform vec4 dust_color : source_color;
			uniform float unit_scale;
			uniform vec2 smoke_offset;
			uniform float opacity = 0.0;
			varying vec3 local_position;
			varying vec3 local_normal;

			void vertex() {
				local_position = VERTEX * unit_scale;
				local_normal = NORMAL;
			}

			void fragment() {
				vec3 weights = abs(normalize(local_normal));
				weights /= weights.x + weights.y + weights.z;
				float smoke = texture(smoke_texture, smoke_offset + local_position.yz * 0.7).r * weights.x
					+ texture(smoke_texture, smoke_offset + local_position.xz * 0.7).r * weights.y
					+ texture(smoke_texture, smoke_offset + local_position.xy * 0.7).r * weights.z;
				ALPHA = texture(cloud_texture, UV).a * smoothstep(0.12, 0.75, smoke) * opacity;
				ALBEDO = dust_color.rgb * mix(0.35, 1.0, smoke);
			}
			""",
	};
	private static readonly Color LowAccent = new(0.84f, 0.46f, 0.34f);
	private static readonly Color HighAccent = new(0.63f, 0.50f, 0.45f);

	private Node3D? _coverVisual;
	private readonly List<Node3D> _rocks = [];
	private int? _resistancePercent;
	private int _seed;

	public int? ResistancePercent => _resistancePercent;

	public void Apply(int? resistancePercent, int seed)
	{
		if (resistancePercent is { } resistance
			&& resistance is not (
				AsteroidBelt.LowResistancePercent or AsteroidBelt.HighResistancePercent))
			throw new ArgumentOutOfRangeException(nameof(resistancePercent));

		if (_coverVisual is not null)
		{
			RemoveChild(_coverVisual);
			_coverVisual.Free();
			_coverVisual = null;
		}
		_rocks.Clear();
		_resistancePercent = resistancePercent;
		_seed = seed;

		if (resistancePercent is not { } activeResistance)
			return;

		var accent = activeResistance == AsteroidBelt.LowResistancePercent
			? LowAccent
			: HighAccent;
		var visual = new Node3D { Name = "Cover" };
		var placements = AsteroidBeltCoverLayout.Create(activeResistance, seed);
		for (var i = 0; i < placements.Count; i++)
		{
			var placement = placements[i];
			var random = new RandomNumberGenerator
			{
				Seed = unchecked((ulong)(uint)seed + (ulong)i),
			};
			var rock = AsteroidRockLibrary.CreateRock(
				random,
				placement.Diameter * WorldMapping.CellSize,
				mainMass: false);
			rock.Position = new Vector3(placement.X, placement.Y, placement.Z)
				* WorldMapping.CellSize;
			rock.Rotation = new Vector3(
				random.RandfRange(0f, Mathf.Tau),
				random.RandfRange(0f, Mathf.Tau),
				random.RandfRange(0f, Mathf.Tau));
			AsteroidRockLibrary.TintMeshes(rock, accent, roughness: 0.78f);
			visual.AddChild(rock);
			_rocks.Add(rock);
		}

		AddChild(visual);
		_coverVisual = visual;
	}

	/// <param name="hitDirection">Local travel direction of the incoming shot.</param>
	public void AnimateHit(int? nextResistancePercent, Vector3 hitDirection)
	{
		if (_coverVisual is not { } visual
			|| !(_resistancePercent == AsteroidBelt.HighResistancePercent
					&& nextResistancePercent == AsteroidBelt.LowResistancePercent
				|| _resistancePercent == AsteroidBelt.LowResistancePercent
					&& nextResistancePercent is null))
			throw new InvalidOperationException("Invalid asteroid belt cover hit transition.");
		if (hitDirection.IsZeroApprox())
			throw new ArgumentException("Hit direction must be non-zero.", nameof(hitDirection));

		var exit = hitDirection.Normalized();
		var accent = _resistancePercent == AsteroidBelt.LowResistancePercent ? LowAccent : HighAccent;
		var placements = AsteroidBeltCoverLayout.Create(_resistancePercent!.Value, _seed);
		var retainedCount = nextResistancePercent is { } resistance
			? AsteroidBeltCoverLayout.Create(resistance, _seed).Count
			: 0;
		for (var i = retainedCount; i < _rocks.Count; i++)
		{
			var rock = _rocks[i];
			var random = new RandomNumberGenerator
			{
				Seed = unchecked((ulong)(uint)_seed + (ulong)i),
			};
			var rockSize = placements[i].Diameter * WorldMapping.CellSize;
			for (var c = 0; c < ShatterChunks; c++)
				ShatterChunk(random, rock.Position, rockSize, exit, accent);
			rock.QueueFree();
		}
		_rocks.RemoveRange(retainedCount, _rocks.Count - retainedCount);
		DustCloud((_seed ^ retainedCount) & int.MaxValue, exit, accent);
		Shake(visual, exit);
		_resistancePercent = nextResistancePercent;

		if (nextResistancePercent is not null)
		{
			foreach (var rock in _rocks)
			{
				rock.GetChild<MeshInstance3D>(0).MaterialOverride = null;
				AsteroidRockLibrary.TintMeshes(rock, LowAccent, roughness: 0.78f);
			}
		}
		else
		{
			var cleanup = visual.CreateTween();
			cleanup.TweenInterval(HitDuration);
			cleanup.TweenCallback(Callable.From(() =>
			{
				_coverVisual = null;
				visual.QueueFree();
			}));
		}
	}

	private void ShatterChunk(
		RandomNumberGenerator random,
		Vector3 origin,
		float rockSize,
		Vector3 exit,
		Color accent)
	{
		var chunk = AsteroidRockLibrary.CreateRock(
			random,
			rockSize * random.RandfRange(0.35f, 0.55f),
			mainMass: false);
		AsteroidRockLibrary.TintMeshes(chunk, accent, roughness: 0.78f);
		var spread = new Vector3(
			random.RandfRange(-0.8f, 0.8f),
			random.RandfRange(-0.8f, 0.8f),
			random.RandfRange(-0.8f, 0.8f));
		var direction = (exit + spread).Normalized();
		chunk.Position = origin + direction * rockSize * 0.25f;
		chunk.Rotation = new Vector3(
			random.RandfRange(0f, Mathf.Tau),
			random.RandfRange(0f, Mathf.Tau),
			random.RandfRange(0f, Mathf.Tau));
		AddChild(chunk);

		var spin = new Vector3(
			random.RandfRange(-20f, 20f),
			random.RandfRange(-20f, 20f),
			random.RandfRange(-20f, 20f));
		var motion = chunk.CreateTween().SetParallel();
		motion.TweenProperty(chunk, "position",
			chunk.Position + direction * WorldMapping.CellSize * random.RandfRange(1.2f, 2.2f),
			HitDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		motion.TweenProperty(chunk, "rotation", chunk.Rotation + spin, HitDuration)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		motion.TweenProperty(chunk, "scale", Vector3.Zero, HitDuration * 0.5)
			.SetDelay(HitDuration * 0.5)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		motion.Chain().TweenCallback(Callable.From(chunk.QueueFree));
	}

	private void DustCloud(int seed, Vector3 exit, Color accent)
	{
		_dustTexture ??= GD.Load<Texture2D>(DustTexturePath)
			?? throw new InvalidOperationException($"Could not load dust texture '{DustTexturePath}'.");
		var random = new RandomNumberGenerator { Seed = (ulong)seed };
		var material = new ShaderMaterial { Shader = DustShader };
		material.SetShaderParameter("smoke_texture", _dustTexture);
		material.SetShaderParameter("dust_color", accent.Lightened(0.1f));
		material.SetShaderParameter("smoke_offset", new Vector2(random.Randf(), random.Randf()));
		material.SetShaderParameter("opacity", 0f);
		var cloud = CryoPlumeView.CreateCloud(seed, source =>
		{
			material.SetShaderParameter("cloud_texture", source.AlbedoTexture);
			return material;
		});
		material.SetShaderParameter("unit_scale", cloud.Scale.X / WorldMapping.CellSize);
		cloud.Name = "HitDust";
		cloud.Rotation = new Vector3(
			random.RandfRange(0f, Mathf.Tau),
			random.RandfRange(0f, Mathf.Tau),
			random.RandfRange(0f, Mathf.Tau));
		var baseScale = cloud.Scale;
		cloud.Scale = baseScale * 0.45f;
		cloud.Position = exit * WorldMapping.CellSize * 0.1f;
		AddChild(cloud);

		var motion = cloud.CreateTween().SetParallel();
		motion.TweenProperty(cloud, "position", exit * WorldMapping.CellSize * 0.6f, DustLifetime)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		motion.TweenProperty(cloud, "scale", baseScale * 1.15f, DustLifetime)
			.SetTrans(Tween.TransitionType.Expo)
			.SetEase(Tween.EaseType.Out);
		var fade = cloud.CreateTween();
		fade.TweenProperty(material, "shader_parameter/opacity", 0.6f, 0.06);
		fade.TweenProperty(material, "shader_parameter/opacity", 0f, DustLifetime - 0.06)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In);
		fade.TweenCallback(Callable.From(cloud.QueueFree));
	}

	private static void Shake(Node3D visual, Vector3 exit)
	{
		var amplitude = exit * WorldMapping.CellSize * 0.05f;
		var shake = visual.CreateTween();
		shake.TweenProperty(visual, "position", amplitude, 0.04);
		shake.TweenProperty(visual, "position", -amplitude * 0.7f, 0.06);
		shake.TweenProperty(visual, "position", amplitude * 0.4f, 0.06);
		shake.TweenProperty(visual, "position", -amplitude * 0.15f, 0.05);
		shake.TweenProperty(visual, "position", Vector3.Zero, 0.05);
	}
}
