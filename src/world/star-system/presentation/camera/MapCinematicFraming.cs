using Godot;
using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation.Camera;

/// <summary>
/// Third-person cinematic framing for the star-map orbit camera.
/// </summary>
public static class MapCinematicFraming
{
	private const float DefaultDistance = 6f;
	private const float DorsalOffset = 0.45f;

	public static OrbitPose BootstrapAtPlayer(PlayerTravelSample sample, in OrbitLimits limits)
	{
		var direction = sample.TravelDirection ?? Vector3.Forward;
		return BehindShip(sample.WorldPosition, direction, limits);
	}

	/// <summary>
	/// The star map's OPENING pose (S3 presence law, "the viewer sees he is
	/// looking at the sun"): on a fresh map the camera sits behind the ship
	/// looking out directly toward the star, so the opening frame contains
	/// the star and its glare reads as being in front of the viewer.  Same
	/// pose family (BehindShip) and the same distance limits — minimum
	/// intrusion; the travel-direction bootstrap stays as the fallback when
	/// the ship sits at the star's centre.
	/// </summary>
	public static OrbitPose OpenTowardStar(
		PlayerTravelSample sample,
		Vector3 starWorld,
		in OrbitLimits limits)
	{
		var towardStar = new Vector3(
			starWorld.X - sample.WorldPosition.X,
			0f,
			starWorld.Z - sample.WorldPosition.Z);
		if (towardStar.LengthSquared() < 0.001f)
			return BootstrapAtPlayer(sample, limits);
		return BehindShip(sample.WorldPosition, towardStar, limits);
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
