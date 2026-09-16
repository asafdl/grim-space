using GrimSpace.Math.Camera;

namespace GrimSpace.World.StarSystem.Presentation;

/// <summary>
/// Narrow read-only queries and camera hooks for presentation modes.
/// Does not expose simulation orchestrators.
/// </summary>
public sealed class MapPresentationContext
{
	public required Func<StarMap> Map { get; init; }
	public required Func<string?> ResolveDockedPoiId { get; init; }
	public required Func<bool> CanAccessFacilities { get; init; }
	public required Func<(float Width, float Height)> ViewportSize { get; init; }
	public required MapCamera Camera { get; init; }
	public required MapView View { get; init; }
	public required float BoundsHalfX { get; init; }
	public required float BoundsHalfZ { get; init; }

	public required Action<OrbitLimits> ApplyLimits { get; init; }
	public required Action<bool> SetOcclusionEnabled { get; init; }
	public required Action<OrbitPose, OrbitLimits> SnapToPose { get; init; }
	public required Action<OrbitPose, OrbitLimits, Action?> TweenToPose { get; init; }
}
