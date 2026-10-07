using System.Linq;
using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// One-shot <see cref="GpuParticles3D"/> bursts — Godot's built-in particle path for explosions/sparks.
/// </summary>
internal static class OneShotParticles
{
	private const double ExplosionFadeSeconds = 0.35;
	private const double ShockwaveDelaySeconds = 0.14;
	private const double ShockwaveExpandSeconds = 0.82;
	private const double WreckageLifetimeSeconds = 8.0;
	private const double WreckageFadeSeconds = 1.5;
	private const string HitSparksPath = "res://assets/vfx/hit_sparks.tscn";
	private const string ExplosionModelPath = "res://assets/vfx/sparksexplosion_clean.glb";
	private static readonly string[] WreckageModelPaths =
	[
		"res://assets/models/wreck_debris_pack/bent_armor.glb",
		"res://assets/models/wreck_debris_pack/broken_fuselage.glb",
		"res://assets/models/wreck_debris_pack/engine_fragment.glb",
		"res://assets/models/wreck_debris_pack/exposed_truss.glb",
		"res://assets/models/wreck_debris_pack/hull_panel.glb",
		"res://assets/models/wreck_debris_pack/severed_prow.glb",
	];
	private static PackedScene? _hitSparks;
	private static PackedScene? _explosionModel;
	private static PackedScene[]? _wreckageModels;

	public static void PlayHitSparks(Node parent, Vector3 localPosition)
	{
		_hitSparks ??= GD.Load<PackedScene>(HitSparksPath)
			?? throw new InvalidOperationException($"Could not load hit sparks VFX '{HitSparksPath}'.");

		var particles = _hitSparks.Instantiate<GpuParticles3D>();
		particles.Position = localPosition;
		parent.AddChild(particles);
		particles.Finished += particles.QueueFree;
		particles.Restart();
		particles.Emitting = true;
	}

	public static void PlayShipDestruction(
		Node host,
		Vector3 worldPosition,
		float scale,
		int debrisCount = 10)
	{
		if (debrisCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(debrisCount), debrisCount, "Debris count must be positive.");

		Play(
			host,
			worldPosition,
			new Color(1f, 0.42f, 0.12f, 0.9f),
			scale,
			worldSpace: true);
		PlayExplosionModel(host, worldPosition, scale * 3.3f);
		PlayWreckage(host, worldPosition, scale, debrisCount);
	}

	/// <param name="host">Scene node that outlives the dying unit (e.g. battle units root).</param>
	/// <param name="worldPosition">World-space burst origin.</param>
	public static void PlayExplosionModel(Node host, Vector3 worldPosition, float scale = 0.2f)
	{
		_explosionModel ??= GD.Load<PackedScene>(ExplosionModelPath)
			?? throw new InvalidOperationException($"Could not load explosion VFX '{ExplosionModelPath}'.");

		var effect = new Node3D { Name = "DeathExplosionModel" };
		host.AddChild(effect);
		effect.TopLevel = true;
		effect.GlobalPosition = worldPosition;

		var explosion = _explosionModel.Instantiate<Node3D>();
		PrepareWorldMeshes(explosion);

		var randomRoll = GD.Randf() * 360f;
		var pivot = new Node3D
		{
			Name = "ExplosionPivot",
			RotationDegrees = new Vector3(0f, 0f, randomRoll),
			Scale = Vector3.One * 0.02f,
		};
		effect.AddChild(pivot);
		pivot.AddChild(explosion);

		var visualMeshes = explosion.FindChildren("*", "MeshInstance3D", true, false)
			.OfType<MeshInstance3D>()
			.ToList();
		if (visualMeshes.Count > 0)
		{
			var center = visualMeshes
				.Select(mesh => explosion.ToLocal(mesh.GlobalTransform * mesh.GetAabb().GetCenter()))
				.Aggregate(Vector3.Zero, (sum, value) => sum + value) / visualMeshes.Count;
			explosion.Position = -center;
		}
		foreach (var mesh in visualMeshes)
		{
			mesh.Visible = true;
			mesh.Transparency = 0f;
		}

		var ringMaterial = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Add,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			NoDepthTest = true,
			AlbedoColor = new Color(1f, 0.92f, 0.72f, 0.85f),
			EmissionEnabled = true,
			Emission = new Color(1f, 0.88f, 0.55f),
			EmissionEnergyMultiplier = 5.5f,
		};
		var ringStart = scale * 0.22f;
		var ringEnd = scale * 3.6f;
		var ring = new MeshInstance3D
		{
			Name = "ExplosionBlastRing",
			RotationDegrees = new Vector3(90f, randomRoll, 0f),
			Scale = new Vector3(ringStart, ringStart * 0.12f, ringStart),
			Mesh = new TorusMesh
			{
				InnerRadius = 0.52f,
				OuterRadius = 0.62f,
			},
			MaterialOverride = ringMaterial,
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
			Transparency = 0f,
		};
		PresentationLayers.MarkWorld(ring);
		effect.AddChild(ring);

		var explosionTween = effect.CreateTween();
		explosionTween.TweenProperty(pivot, "scale", Vector3.One * scale, 0.6)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		explosionTween.TweenProperty(pivot, "scale", Vector3.One * (scale * 1.06f), 0.1)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		explosionTween.TweenInterval(0.02);
		if (visualMeshes.Count > 0)
		{
			explosionTween.TweenProperty(visualMeshes[0], "transparency", 1f, ExplosionFadeSeconds)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			foreach (var mesh in visualMeshes.Skip(1))
			{
				explosionTween.Parallel()
					.TweenProperty(mesh, "transparency", 1f, ExplosionFadeSeconds)
					.SetTrans(Tween.TransitionType.Quad)
					.SetEase(Tween.EaseType.Out);
			}
		}

		var shockwaveTween = effect.CreateTween();
		shockwaveTween.TweenInterval(ShockwaveDelaySeconds);
		shockwaveTween.TweenProperty(
				ring,
				"scale",
				new Vector3(ringEnd, ringEnd * 0.1f, ringEnd),
				ShockwaveExpandSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.Out);
		shockwaveTween.Parallel()
			.TweenProperty(ring, "transparency", 1f, ExplosionFadeSeconds + 0.12)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.In)
			.SetDelay(ShockwaveExpandSeconds * 0.35);
		shockwaveTween.TweenCallback(Callable.From(effect.QueueFree));
	}

	private static void PlayWreckage(
		Node host,
		Vector3 worldPosition,
		float scale,
		int debrisCount)
	{
		_wreckageModels ??= WreckageModelPaths
			.Select(path => GD.Load<PackedScene>(path)
				?? throw new InvalidOperationException($"Could not load wreckage model '{path}'."))
			.ToArray();

		var wreckage = new Node3D { Name = "WreckageBurst" };
		host.AddChild(wreckage);
		wreckage.TopLevel = true;
		wreckage.GlobalPosition = worldPosition;

		for (var i = 0; i < debrisCount; i++)
		{
			var modelIndex = (int)(GD.Randi() % (uint)_wreckageModels.Length);
			var fragment = _wreckageModels[modelIndex].Instantiate<Node3D>();
			fragment.Name = $"WreckageFragment_{i}";
			PrepareWorldMeshes(fragment);
			wreckage.AddChild(fragment);

			var direction = RandomDirection();
			var start = direction * RandomRange(0.08f, 0.3f) * scale;
			var duration = RandomRange(6.5f, (float)WreckageLifetimeSeconds);
			fragment.Position = start;
			fragment.Rotation = new Vector3(
				RandomRange(0f, Mathf.Tau),
				RandomRange(0f, Mathf.Tau),
				RandomRange(0f, Mathf.Tau));
			fragment.Scale = Vector3.One * RandomRange(0.12f, 0.26f) * scale;

			var drift = direction * RandomRange(4.5f, 7.5f) * scale
				+ RandomDirection() * RandomRange(0.4f, 1.1f) * scale;
			var rotation = fragment.Rotation + RandomDirection() * RandomRange(6f, 13f);
			var movement = wreckage.CreateTween();
			movement.TweenProperty(fragment, "position", start + drift, duration)
				.SetTrans(Tween.TransitionType.Quart)
				.SetEase(Tween.EaseType.Out);
			movement.Parallel()
				.TweenProperty(fragment, "rotation", rotation, duration)
				.SetTrans(Tween.TransitionType.Linear);

			var meshes = fragment.FindChildren("*", "MeshInstance3D", true, false)
				.OfType<MeshInstance3D>()
				.ToList();
			if (meshes.Count == 0)
				continue;

			var fade = wreckage.CreateTween();
			fade.TweenInterval(duration - WreckageFadeSeconds);
			fade.TweenProperty(meshes[0], "transparency", 1f, WreckageFadeSeconds);
			foreach (var mesh in meshes.Skip(1))
				fade.Parallel().TweenProperty(mesh, "transparency", 1f, WreckageFadeSeconds);
		}

		var cleanup = wreckage.CreateTween();
		cleanup.TweenInterval(WreckageLifetimeSeconds);
		cleanup.TweenCallback(Callable.From(wreckage.QueueFree));
	}

	private static float RandomRange(float min, float max) =>
		Mathf.Lerp(min, max, GD.Randf());

	private static Vector3 RandomDirection()
	{
		var y = RandomRange(-1f, 1f);
		var angle = RandomRange(0f, Mathf.Tau);
		var radius = Mathf.Sqrt(1f - y * y);
		return new Vector3(radius * Mathf.Cos(angle), y, radius * Mathf.Sin(angle));
	}

	static void PrepareWorldMeshes(Node node)
	{
		if (node is MeshInstance3D mesh)
		{
			PresentationLayers.MarkWorld(mesh);
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}

		foreach (var child in node.GetChildren())
			PrepareWorldMeshes(child);
	}

	public static void Play(Node parent, Vector3 localPosition, Color color, float scale = 1f) =>
		Play(parent, localPosition, color, scale, worldSpace: false);

	public static void Play(
		Node host,
		Vector3 position,
		Color color,
		float scale,
		bool worldSpace)
	{
		var material = new ParticleProcessMaterial
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
			EmissionSphereRadius = 0.12f * scale,
			Direction = Vector3.Up,
			Spread = 180f,
			InitialVelocityMin = 1.8f * scale,
			InitialVelocityMax = 4.5f * scale,
			Gravity = Vector3.Zero,
			ScaleMin = 0.12f * scale,
			ScaleMax = 0.32f * scale,
			Color = color,
		};

		var particles = new GpuParticles3D
		{
			Amount = 18,
			Lifetime = 0.36f,
			OneShot = true,
			Explosiveness = 1f,
			ProcessMaterial = material,
			DrawPass1 = new SphereMesh
			{
				Radius = 0.07f * scale,
				Height = 0.14f * scale,
			},
		};
		PresentationLayers.MarkUx(particles);
		if (worldSpace)
		{
			var burst = new Node3D { Name = "DeathExplosionBurst" };
			host.AddChild(burst);
			burst.TopLevel = true;
			burst.GlobalPosition = position;
			burst.AddChild(particles);
			particles.Finished += () => burst.QueueFree();
		}
		else
		{
			particles.Position = position;
			host.AddChild(particles);
			particles.Finished += particles.QueueFree;
		}

		particles.Restart();
		particles.Emitting = true;
	}
}
