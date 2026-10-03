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
	private const string HitSparksPath = "res://assets/vfx/hit_sparks.tscn";
	private const string ExplosionModelPath = "res://assets/vfx/sparksexplosion_clean.glb";
	private static PackedScene? _hitSparks;
	private static PackedScene? _explosionModel;

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
		PrepareExplosionMeshes(explosion);

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

	static void PrepareExplosionMeshes(Node node)
	{
		if (node is MeshInstance3D mesh)
		{
			PresentationLayers.MarkWorld(mesh);
			mesh.CastShadow = GeometryInstance3D.ShadowCastingSetting.Off;
		}

		foreach (var child in node.GetChildren())
			PrepareExplosionMeshes(child);
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
