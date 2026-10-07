using Godot;
using GrimSpace.Battle.Presentation;

namespace GrimSpace.Battle.Presentation.Graphics;

public partial class MovementTrailView : Node3D
{
	private const float CoreParticleSpacing = 0.045f;
	private const float HaloParticleSpacing = 0.05f;

	private readonly List<TrailEmitter> _emitters = [];
	private readonly RandomNumberGenerator _random = new();
	private bool _emitting;

	public override void _Ready() => _random.Randomize();

	public override void _Process(double delta)
	{
		if (!_emitting)
			return;

		foreach (var trailEmitter in _emitters)
		{
			var emitter = trailEmitter.Particles;
			var current = emitter.GlobalPosition;
			var distance = trailEmitter.PreviousPosition.DistanceTo(current);
			if (distance < Mathf.Epsilon)
				continue;

			var count = Mathf.Max(1, Mathf.CeilToInt(distance / trailEmitter.Spacing));
			for (var sample = 1; sample <= count; sample++)
			{
				var worldPosition = trailEmitter.PreviousPosition.Lerp(current, sample / (float)count);
				var velocity = trailEmitter.Drifting
					? ExhaustVelocity((current - trailEmitter.PreviousPosition).Normalized())
					: Vector3.Zero;
				var flags = GpuParticles3D.EmitFlags.Position;
				if (trailEmitter.Drifting)
					flags |= GpuParticles3D.EmitFlags.Velocity;
				emitter.EmitParticle(
					new Transform3D(Basis.Identity, worldPosition),
					velocity,
					Colors.White,
					Colors.White,
					(uint)flags);
			}

			trailEmitter.PreviousPosition = current;
		}
	}

	private Vector3 ExhaustVelocity(Vector3 flightDirection)
	{
		Vector3 lateral;
		do
		{
			var random = new Vector3(
				_random.RandfRange(-1f, 1f),
				_random.RandfRange(-1f, 1f),
				_random.RandfRange(-1f, 1f));
			lateral = random - flightDirection * random.Dot(flightDirection);
		}
		while (lateral.LengthSquared() < Mathf.Epsilon);

		return -flightDirection * _random.RandfRange(0.08f, 0.18f)
			+ lateral.Normalized() * _random.RandfRange(0.01f, 0.05f);
	}

	public void Configure(
		Aabb bounds,
		bool singleEmitter,
		Color color)
	{
		foreach (var emitter in _emitters)
			emitter.Particles.QueueFree();
		_emitters.Clear();
		_emitting = false;

		var center = bounds.GetCenter();
		var aft = bounds.Position.Z + bounds.Size.Z * 0.18f;
		if (singleEmitter)
		{
			AddTrailAnchor(new Vector3(center.X, center.Y, aft), color);
			return;
		}

		var port = bounds.Position.X + bounds.Size.X * 0.08f;
		var starboard = bounds.End.X - bounds.Size.X * 0.08f;
		AddTrailAnchor(new Vector3(port, center.Y, aft), color);
		AddTrailAnchor(new Vector3(starboard, center.Y, aft), color);
	}

	public void SetEmitting(bool emitting)
	{
		if (_emitting == emitting)
			return;

		_emitting = emitting;
		if (!emitting)
			return;

		foreach (var emitter in _emitters)
		{
			emitter.PreviousPosition = emitter.Particles.GlobalPosition;
			emitter.HasPreviousPosition = true;
		}
	}

	public void Clear()
	{
		_emitting = false;
		foreach (var emitter in _emitters)
		{
			emitter.Particles.Restart();
			emitter.Particles.Emitting = false;
			emitter.HasPreviousPosition = false;
		}
	}

	private void AddTrailAnchor(Vector3 position, Color color)
	{
		AddEmitter(CreateCoreEmitter(position, color), CoreParticleSpacing, drifting: false);
		AddEmitter(CreateHaloEmitter(position, color), HaloParticleSpacing, drifting: true);
	}

	private void AddEmitter(GpuParticles3D particles, float spacing, bool drifting)
	{
		PresentationLayers.MarkUx(particles);
		AddChild(particles);
		_emitters.Add(new TrailEmitter(particles, spacing, drifting));
	}

	private static GpuParticles3D CreateCoreEmitter(Vector3 position, Color color) =>
		CreateEmitter(
			"MovementTrailCore",
			position,
			amount: 4096,
			lifetime: 600f,
			CreateCoreProcessMaterial(color),
			CreateParticleMesh(radius: 0.035f));

	private static GpuParticles3D CreateHaloEmitter(Vector3 position, Color color) =>
		CreateEmitter(
			"MovementTrailHalo",
			position,
			amount: 1536,
			lifetime: 1.4f,
			CreateHaloProcessMaterial(color.Lightened(0.35f)),
			CreateParticleMesh(radius: 0.03f));

	private static GpuParticles3D CreateEmitter(
		string name,
		Vector3 position,
		int amount,
		float lifetime,
		ParticleProcessMaterial processMaterial,
		Mesh drawMesh) =>
		new()
		{
			Name = name,
			Position = position,
			Amount = amount,
			Lifetime = lifetime,
			FixedFps = 60,
			Interpolate = true,
			FractDelta = true,
			Randomness = 0.08f,
			LocalCoords = false,
			DrawOrder = GpuParticles3D.DrawOrderEnum.Lifetime,
			VisibilityAabb = new Aabb(new Vector3(-32f, -32f, -32f), new Vector3(64f, 64f, 64f)),
			ProcessMaterial = processMaterial,
			DrawPass1 = drawMesh,
			Emitting = false,
		};

	private static ParticleProcessMaterial CreateCoreProcessMaterial(Color color) =>
		new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Point,
			InitialVelocityMin = 0f,
			InitialVelocityMax = 0f,
			Gravity = Vector3.Zero,
			ScaleMin = 0.65f,
			ScaleMax = 1.1f,
			ColorRamp = ColorRamp(
				[0f, 0.002f, 0.0075f, 0.01f, 1f],
				[0.9f, 0.72f, 0.24f, 0.12f, 0.12f],
				color),
		};

	private static ParticleProcessMaterial CreateHaloProcessMaterial(Color color) =>
		new()
		{
			EmissionShape = ParticleProcessMaterial.EmissionShapeEnum.Point,
			Direction = Vector3.Up,
			Spread = 180f,
			InitialVelocityMin = 0f,
			InitialVelocityMax = 0f,
			Gravity = Vector3.Zero,
			ScaleMin = 0.6f,
			ScaleMax = 1.25f,
			ColorRamp = ColorRamp(
				[0f, 0.22f, 0.7f, 1f],
				[0.95f, 0.7f, 0.24f, 0f],
				color),
		};

	private static GradientTexture1D ColorRamp(float[] offsets, float[] alphas, Color color) =>
		new()
		{
			Gradient = new Gradient
			{
				Offsets = offsets,
				Colors = alphas
					.Select(alpha => new Color(color.R, color.G, color.B, alpha))
					.ToArray(),
			},
		};

	private static SphereMesh CreateParticleMesh(float radius) =>
		new()
		{
			Radius = radius,
			Height = radius * 2f,
			RadialSegments = 6,
			Rings = 3,
			Material = new StandardMaterial3D
			{
				Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
				ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
				BlendMode = BaseMaterial3D.BlendModeEnum.Add,
				VertexColorUseAsAlbedo = true,
				AlbedoColor = Colors.White,
				DepthDrawMode = BaseMaterial3D.DepthDrawModeEnum.Disabled,
			},
		};

	private sealed class TrailEmitter(GpuParticles3D particles, float spacing, bool drifting)
	{
		public GpuParticles3D Particles { get; } = particles;
		public float Spacing { get; } = spacing;
		public bool Drifting { get; } = drifting;
		public Vector3 PreviousPosition { get; set; }
		public bool HasPreviousPosition { get; set; }
	}
}
