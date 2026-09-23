namespace GrimSpace.World.StarSystem.Areas;

public sealed record AreaPickerArgs(
	IReadOnlyList<IReadOnlyList<string>> LandmarkGroups,
	IReadOnlyList<EAreaDistance> Distances,
	int MinLandmarkSeparation = 2,
	long? DeterministicPickMix = null);
