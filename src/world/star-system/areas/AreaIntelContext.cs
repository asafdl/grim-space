namespace GrimSpace.World.StarSystem.Areas;

/// <param name="LandmarkAId">Closest reference to the search center.</param>
/// <param name="LandmarkBId">Second-closest reference.</param>
/// <param name="LandmarkCId">Farthest reference (distant corner of the fix).</param>
public sealed record AreaIntelContext(
	string LandmarkAId,
	string LandmarkBId,
	string LandmarkCId);
