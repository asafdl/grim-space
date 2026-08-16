using Godot;

namespace GrimSpace.Battle.Presentation.Graphics;

/// <summary>
/// One-shot <see cref="GpuParticles3D"/> bursts — Godot's built-in particle path for explosions/sparks.
/// </summary>
internal static class OneShotParticles
{
	public static void Play(Node parent, Vector3 localPosition, Color color, float scale = 1f)
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
			Position = localPosition,
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
		parent.AddChild(particles);

		particles.Finished += () => particles.QueueFree();
		particles.Restart();
		particles.Emitting = true;
	}
}
