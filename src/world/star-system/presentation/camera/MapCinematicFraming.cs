using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation.Camera;

/// <summary>
/// Third-person cinematic framing for the star-map orbit camera.
/// </summary>
public static class MapCinematicFraming
{
	private const float DefaultDistance = 12f;
	private const float DorsalOffset = 0.45f;

	public static OrbitPose BootstrapAtPlayer(PlayerTravelSample sample, in OrbitLimits limits)
	{
		var direction = sample.TravelDirection ?? Vector3.Forward;
		return BehindShip(sample.WorldPosition, direction, limits);
	}

	public static OrbitPose BehindShip(
		Vector3 playerWorld,
		Vector3 travelDirection,
		in OrbitLimits limits,
		float? distance = null)
	{
		var fore = new Vector3(travelDirection.X, 0f, travelDirection.Z);
		if (fore.LengthSquared() < 0.001f)
			fore = Vector3.Forward;
		else
			fore = fore.Normalized();

		var viewDirection = (-fore + Vector3.Up * DorsalOffset).Normalized();
		var (yaw, pitch) = OrbitPose.AnglesForDirection(viewDirection, limits);
		var resolvedDistance = Mathf.Clamp(
			distance ?? DefaultDistance,
			limits.MinDistance,
			limits.MaxDistance);

		return new OrbitPose
		{
			Pivot = playerWorld,
			Yaw = yaw,
			Pitch = pitch,
			Distance = resolvedDistance,
		};
	}
}
