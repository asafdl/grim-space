using Godot;

namespace GrimSpace.World.StarSystem.Presentation;

internal static class MapCameraOcclusion
{
	public const int PhysicsLayer = 20;
	public const uint CollisionMask = 1u << (PhysicsLayer - 1);

	public static void AddSphere(Node3D parent, float radius)
	{
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radius);

		var body = new StaticBody3D
		{
			Name = "CameraOccluder",
			CollisionLayer = CollisionMask,
			CollisionMask = 0u,
		};
		body.AddChild(new CollisionShape3D
		{
			Name = "Shape",
			Shape = new SphereShape3D { Radius = radius },
		});
		parent.AddChild(body);
	}
}
