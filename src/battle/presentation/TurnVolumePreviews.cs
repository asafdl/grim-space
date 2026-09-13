using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation;

public sealed record TurnVolumePreview(
	Coord Origin,
	IReadOnlyList<IReadOnlySet<Coord>> TurnBands)
{
	public static TurnVolumePreview FromCumulativeReach(
		Coord origin,
		IReadOnlyList<IReadOnlySet<Coord>> layers)
	{
		var seen = new HashSet<Coord>();
		var bands = new List<IReadOnlySet<Coord>>(layers.Count);
		foreach (var layer in layers)
			bands.Add(layer.Where(seen.Add).ToHashSet());

		return new TurnVolumePreview(origin, bands);
	}
}

public sealed record TurnVolumePreviews(
	TurnVolumePreview? Aim,
	TurnVolumePreview? Queued)
{
	public static TurnVolumePreviews Empty { get; } = new(null, null);
}
