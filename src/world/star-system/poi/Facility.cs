namespace GrimSpace.World.StarSystem.Poi;

public sealed record Facility(
	string Id,
	string DisplayName,
	EPresentationAnchor PresentationAnchor,
	IReadOnlyList<EServiceKind> ServiceKinds)
{
	public static string ScopedId(string poiId, string slug) => $"{poiId}-{slug}";
}
