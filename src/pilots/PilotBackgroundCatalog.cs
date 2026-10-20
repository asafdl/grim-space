namespace GrimSpace.Pilots;

public static class PilotBackgroundCatalog
{
	private static readonly IReadOnlyDictionary<EPilotBackgroundId, PilotBackgroundDefinition> Definitions =
		new Dictionary<EPilotBackgroundId, PilotBackgroundDefinition>
		{
			[EPilotBackgroundId.Unspecified] = new(
				EPilotBackgroundId.Unspecified,
				"Unspecified Background",
				"Temporary infrastructure placeholder. Real pilot backgrounds are not defined yet."),
		};

	public static PilotBackgroundDefinition Get(EPilotBackgroundId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot background '{id}'.");

	public static bool TryGet(EPilotBackgroundId id, out PilotBackgroundDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotBackgroundDefinition> All => Definitions.Values;
}
