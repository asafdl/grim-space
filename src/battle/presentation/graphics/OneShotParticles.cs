using System.Linq;
using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// One-shot <see cref="GpuParticles3D"/> bursts — Godot's built-in particle path for explosions/sparks.
/// </summary>
internal static class OneShotParticles
{
	private const double ExplosionGrowSeconds = 0.6;
	private const double ExplosionSettleSeconds = 0.1;
	private const double ExplosionHoldSeconds = 0.02;
	private const double ExplosionFadeSeconds = 0.35;
	private const double ExplosionModelLifetimeSeconds = 1.1;
	private const double SmokeLifetimeSeconds = 2.0;
	private const int SmokeCardCount = 7;
	private const float SmokeSizeMultiplier = 2f;
	private const double BoomAppearDelay = 0.08;
	private const double BoomFadeInSeconds = 0.06;
	private const double BoomFadeStart = 0.62;
	private const double BoomFadeSeconds = 0.4;
	private const float BoomWidthScale = 4.43f;
	private const double WreckageLifetimeSeconds = 8.0;
	private const double WreckageFadeSeconds = 1.5;
	private const string HitSparksPath = "res://assets/vfx/hit_sparks.tscn";
	private const string HullHitSparksPath = "res://assets/vfx/hull_hit_sparks.tscn";
	private const string ExplosionModelPath = "res://assets/vfx/sparksexplosion_clean.glb";
	private const string SmokeTexturePath =
		"res://assets/textures/particles/smoke_02.png";
	private const string BoomTextPath = "res://assets/vfx/boom-godot.glb";
	private const string BoomTextNodeName = "BoomText";
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
	private static PackedScene? _hullHitSparks;
	private static PackedScene? _explosionModel;
	private static Texture2D? _smokeTexture;
	private static PackedScene? _boomText;
	private static PackedScene[]? _wreckageModels;

	public static void PlayHitSparks(Node parent, Vector3 localPosition, Basis? localBasis = null)
		=> PlaySparks(parent, localPosition, localBasis, ref _hitSparks, HitSparksPath);

	public static void PlayHullHitSparks(Node parent, Vector3 localPosition, Basis? localBasis = null)
		=> PlaySparks(parent, localPosition, localBasis, ref _hullHitSparks, HullHitSparksPath);

	private static void PlaySparks(
		Node parent,
		Vector3 localPosition,
		Basis? localBasis,
		ref PackedScene? packedScene,
		string scenePath)
	{
		packedScene ??= GD.Load<PackedScene>(scenePath)
			?? throw new InvalidOperationException($"Could not load hit sparks VFX '{scenePath}'.");

		var particles = packedScene.Instantiate<GpuParticles3D>();
		particles.Position = localPosition;
		if (localBasis is { } basis)
		{
			particles.LocalCoords = true;
			particles.Basis = basis;
		}

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

		PlaySmokeExplosion(host, worldPosition, scale);
		PlayWreckage(host, worldPosition, scale, debrisCount);
	}

	public static void PlaySmokeExplosion(
		Node host,
		Vector3 worldPosition,
		float scale)
	{
		PlaySmokeCloud(host, worldPosition, scale);
		PlayBoomText(host, worldPosition, scale * 3.3f);
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

		var explosionTween = effect.CreateTween();
		explosionTween.TweenProperty(
				pivot,
				"scale",
				Vector3.One * scale,
				ExplosionGrowSeconds)
			.SetTrans(Tween.TransitionType.Cubic)
			.SetEase(Tween.EaseType.In);
		explosionTween.TweenProperty(
				pivot,
				"scale",
				Vector3.One * (scale * 1.06f),
				ExplosionSettleSeconds)
			.SetTrans(Tween.TransitionType.Quad)
			.SetEase(Tween.EaseType.Out);
		explosionTween.TweenInterval(ExplosionHoldSeconds);
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

		var cleanupTween = effect.CreateTween();
		cleanupTween.TweenInterval(ExplosionModelLifetimeSeconds);
		cleanupTween.TweenCallback(Callable.From(effect.QueueFree));
	}

	private static void PlaySmokeCloud(
		Node host,
		Vector3 worldPosition,
		float scale)
	{
		_smokeTexture ??= GD.Load<Texture2D>(SmokeTexturePath)
			?? throw new InvalidOperationException(
				$"Could not load ship death smoke '{SmokeTexturePath}'.");
		var cloud = new Node3D
		{
			Name = "ShipDeathSmoke",
		};
		host.AddChild(cloud);
		cloud.TopLevel = true;
		cloud.GlobalPosition = worldPosition;

		var material = new StandardMaterial3D
		{
			ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			BlendMode = BaseMaterial3D.BlendModeEnum.Mix,
			DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
			BillboardKeepScale = true,
			AlbedoTexture = _smokeTexture,
			AlbedoColor = new Color(0.34f, 0.35f, 0.37f, 0.72f),
		};
		var smokeScale = scale * SmokeSizeMultiplier;

		for (var i = 0; i < SmokeCardCount; i++)
		{
			var direction = new Vector3(
				RandomRange(-1f, 1f),
				RandomRange(-0.35f, 1f),
				RandomRange(-1f, 1f)).Normalized();
			var start = direction * (smokeScale * RandomRange(0.04f, 0.2f));
			var end = start + direction * (smokeScale * RandomRange(0.35f, 0.8f))
				+ Vector3.Up * (smokeScale * RandomRange(0.12f, 0.4f));
			var startSize = smokeScale * RandomRange(0.22f, 0.38f);
			var endSize = smokeScale * RandomRange(0.7f, 1.15f);
			var delay = i * 0.025;
			var card = new MeshInstance3D
			{
				Name = $"SmokeCard_{i}",
				Mesh = new QuadMesh { Size = Vector2.One },
				MaterialOverride = material,
				Position = start,
				RotationDegrees = new Vector3(
					0f,
					0f,
					RandomRange(0f, 360f)),
				Scale = Vector3.One * startSize,
				Transparency = 0.18f,
				CastShadow =
					GeometryInstance3D.ShadowCastingSetting.Off,
			};
			PresentationLayers.MarkWorld(card);
			cloud.AddChild(card);

			var motion = cloud.CreateTween();
			motion.TweenInterval(delay);
			motion.TweenProperty(
					card,
					"position",
					end,
					SmokeLifetimeSeconds - delay)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.Out);
			motion.Parallel().TweenProperty(
					card,
					"scale",
					Vector3.One * endSize,
					0.5)
				.SetTrans(Tween.TransitionType.Cubic)
				.SetEase(Tween.EaseType.Out);

			var fade = cloud.CreateTween();
			fade.TweenInterval(0.42 + delay);
			fade.TweenProperty(
					card,
					"transparency",
					1f,
					SmokeLifetimeSeconds - 0.42 - delay)
				.SetTrans(Tween.TransitionType.Quad)
				.SetEase(Tween.EaseType.In);
		}

		var cleanup = cloud.CreateTween();
		cleanup.TweenInterval(SmokeLifetimeSeconds);
		cleanup.TweenCallback(Callable.From(cloud.QueueFree));
	}

	private static void PlayBoomText(
		Node host,
		Vector3 worldPosition,
		float scale)
	{
		var effect = new Node3D
		{
			Name = "ShipDeathBoom",
		};
		host.AddChild(effect);
		effect.TopLevel = true;
		effect.GlobalPosition = worldPosition;
		var text = AddBoomText(effect, scale);
		AnimateBoomText(effect, text);

		var cleanup = effect.CreateTween();
		cleanup.TweenInterval(BoomFadeStart + BoomFadeSeconds);
		cleanup.TweenCallback(Callable.From(effect.QueueFree));
	}

	private static MeshInstance3D AddBoomText(Node3D effect, float scale)
	{
		_boomText ??= GD.Load<PackedScene>(BoomTextPath)
			?? throw new InvalidOperationException(
				$"Could not load ship death text '{BoomTextPath}'.");
		var root = _boomText.Instantiate<Node3D>();
		effect.AddChild(root);

		var text = root.FindChild(
			BoomTextNodeName,
			recursive: true,
			owned: false) as MeshInstance3D
			?? throw new InvalidOperationException(
				$"Ship death text '{BoomTextPath}' has no '{BoomTextNodeName}' mesh.");
		var bounds = text.Mesh?.GetAabb()
			?? throw new InvalidOperationException(
				$"Ship death text '{BoomTextNodeName}' has no mesh.");
		if (bounds.Size.X <= 0f)
			throw new InvalidOperationException(
				$"Ship death text '{BoomTextNodeName}' has no width.");

		var textScale = scale * BoomWidthScale / bounds.Size.X;
		text.Position = new Vector3(0f, scale * 0.22f, 0f);
		text.Scale = Vector3.One * textScale;
		text.Transparency = 1f;
		text.CastShadow =
			GeometryInstance3D.ShadowCastingSetting.Off;
		PresentationLayers.MarkWorld(text);

		for (var surface = 0; surface < text.Mesh.GetSurfaceCount(); surface++)
		{
			var source = text.GetActiveMaterial(surface) as BaseMaterial3D
				?? throw new InvalidOperationException(
					$"Ship death text surface {surface} has no 3D material.");
			var material = (BaseMaterial3D)source.Duplicate();
			material.BillboardMode =
				BaseMaterial3D.BillboardModeEnum.Enabled;
			material.BillboardKeepScale = true;
			text.SetSurfaceOverrideMaterial(surface, material);
		}

		return text;
	}

	private static void AnimateBoomText(
		Node3D effect,
		MeshInstance3D text)
	{
		var fullScale = text.Scale;
		text.Scale = fullScale * 0.82f;

		var tween = effect.CreateTween();
		tween.TweenInterval(BoomAppearDelay);
		tween.TweenProperty(
			text,
			"transparency",
			0f,
			BoomFadeInSeconds);
		tween.Parallel().TweenProperty(
				text,
				"scale",
				fullScale,
				BoomFadeInSeconds)
			.SetTrans(Tween.TransitionType.Back)
			.SetEase(Tween.EaseType.Out);
		tween.TweenInterval(
			BoomFadeStart
			- BoomAppearDelay
			- BoomFadeInSeconds);
		tween.TweenProperty(
			text,
			"transparency",
			1f,
			BoomFadeSeconds);
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
