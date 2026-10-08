using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

public static class GoopSprayEffect
{
	private const int ParticleCount = 88;
	private const float LifetimeSeconds = 0.72f;
	private const float FlightLifetimeRatio = 0.94f;
	private const float MinDropletDiameter = 0.16f;
	private const float MaxDropletDiameter = 0.27f;
	private const float SpreadDegrees = 3.5f;
	private const float ArcHeightRatio = 0.09f;
	private const float MaxArcHeight = 0.65f;

	public const double SpraySeconds = LifetimeSeconds;
	public const double ImpactSeconds = LifetimeSeconds * FlightLifetimeRatio;

	private static StandardMaterial3D? _dropletMaterial;
	private static CapsuleMesh? _sprayMesh;

	public static void Play(
		Node host,
		Vector3 sourceWorldPosition,
		Vector3 targetWorldPosition,
		ulong seed)
	{
		ArgumentNullException.ThrowIfNull(host);
		var travel = targetWorldPosition - sourceWorldPosition;
		var distance = travel.Length();
		if (distance <= 0.001f)
			throw new ArgumentException("Goop spray source and target must be distinct positions.");

		var mesh = SprayMesh();
		var meshDiameter = mesh.GetAabb().Size.Length();
		var flightSeconds = (float)ImpactSeconds;
		var straightDirection = travel / distance;
		var arcAxis = Vector3.Up - straightDirection * straightDirection.Dot(Vector3.Up);
		if (arcAxis.LengthSquared() < 0.01f)
			arcAxis = Vector3.Right - straightDirection * straightDirection.Dot(Vector3.Right);
		arcAxis = arcAxis.Normalized();
		var arcHeight = Mathf.Min(distance * ArcHeightRatio, MaxArcHeight);
		var acceleration = arcAxis * (-8f * arcHeight / (flightSeconds * flightSeconds));
		var initialVelocity = travel / flightSeconds - acceleration * (flightSeconds * 0.5f);
		var speed = initialVelocity.Length();
		var effect = new Node3D { Name = "GoopSprayEffect" };
		host.AddChild(effect);
		effect.TopLevel = true;
		effect.GlobalPosition = sourceWorldPosition;

		var particles = new GpuParticles3D
		{
			Name = "Droplets",
			Amount = ParticleCount,
			Lifetime = LifetimeSeconds,
			OneShot = true,
			Explosiveness = 0.42f,
			Randomness = 0f,
			UseFixedSeed = true,
			Seed = unchecked((uint)(seed ^ (seed >> 32))),
			FixedFps = 60,
			Interpolate = true,
			FractDelta = true,
			LocalCoords = false,
			DrawOrder = GpuParticles3D.DrawOrderEnum.Lifetime,
			VisibilityAabb = new Aabb(
				Vector3.One * -(distance + 1f),
				Vector3.One * ((distance + 1f) * 2f)),
			ProcessMaterial = new ParticleProcessMaterial
			{
				EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Sphere,
				EmissionSphereRadius = 0.08f,
				Direction = initialVelocity / speed,
				Spread = SpreadDegrees,
				InitialVelocityMin = speed * 0.97f,
				InitialVelocityMax = speed,
				Gravity = acceleration,
				ParticleFlagAlignY = true,
				ScaleMin = MinDropletDiameter / meshDiameter,
				ScaleMax = MaxDropletDiameter / meshDiameter,
				ColorRamp = CreateColorRamp(),
			},
			DrawPass1 = mesh,
			MaterialOverride = DropletMaterial(),
			CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
		};
		PresentationLayers.MarkWorld(particles);
		effect.AddChild(particles);
		particles.Finished += effect.QueueFree;
		particles.Restart();
		particles.Emitting = true;
	}

	private static CapsuleMesh SprayMesh() =>
		_sprayMesh ??= new CapsuleMesh
		{
			Radius = 0.3f,
			Height = 1.4f,
			RadialSegments = 8,
			Rings = 4,
		};

	private static StandardMaterial3D DropletMaterial() =>
		_dropletMaterial ??= new StandardMaterial3D
		{
			Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
			CullMode = BaseMaterial3D.CullModeEnum.Disabled,
			VertexColorUseAsAlbedo = true,
			AlbedoColor = Colors.White,
			Metallic = 0f,
			Roughness = 0.2f,
			EmissionEnabled = true,
			Emission = new Color(0.18f, 0.62f, 0.1f),
			EmissionEnergyMultiplier = 0.12f,
		};

	private static GradientTexture1D CreateColorRamp() =>
		new()
		{
			Gradient = new Gradient
			{
				Offsets = [0f, 0.06f, 0.84f, 1f],
				Colors =
				[
					new Color(0.55f, 1f, 0.25f, 0.85f),
					new Color(0.32f, 0.88f, 0.16f, 1f),
					new Color(0.18f, 0.68f, 0.1f, 1f),
					new Color(0.12f, 0.48f, 0.08f, 0f),
				],
			},
		};
}
