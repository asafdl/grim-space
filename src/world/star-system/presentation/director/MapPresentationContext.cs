using Godot;
using GrimSpace.Math.Camera;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Presentation.Camera;
using GrimSpace.World.StarSystem.Presentation.Map;

namespace GrimSpace.World.StarSystem.Presentation.Director;

/// <summary>
/// Narrow read-only queries and camera hooks for presentation modes.
/// Does not expose simulation orchestrators.
/// </summary>
public sealed class MapPresentationContext
{
	public required Func<StarMap> Map { get; init; }
	public required Func<PlayerTravelSample> ResolvePlayerTravelSample { get; init; }
	public required Func<string?> ResolveDockedPoiId { get; init; }
	public required Func<bool> CanAccessFacilities { get; init; }

	/// <summary>
	/// The star's world position, for the opening pose (S3 presence: the
	/// star map's first frame faces the star) and anything that needs to
	/// point at the sun.
	/// </summary>
	public required Func<Vector3> StarWorldPosition { get; init; }
	public required Func<(float Width, float Height)> ViewportSize { get; init; }
	public required MapCamera Camera { get; init; }
	public required Func<OrbitPose> ResolveCameraPose { get; init; }
	public required Func<bool> IsCameraAnimating { get; init; }
	public required Action<float> ApplyCameraDistanceDelta { get; init; }
	public required MapView View { get; init; }
	public required float BoundsHalfX { get; init; }
	public required float BoundsHalfZ { get; init; }

	public required Action<OrbitLimits> ApplyLimits { get; init; }
	public required Action<bool> SetOcclusionEnabled { get; init; }
	public required Action<OrbitPose, OrbitLimits> SnapToPose { get; init; }
	public required Action<OrbitPose, OrbitLimits, Action?> TweenToPose { get; init; }
	public required Action<bool> SetPlayerBeaconHidden { get; init; }
}
