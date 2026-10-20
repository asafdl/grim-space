namespace GrimSpace.Pilots;

public static class PilotQuirkCatalog
{
	private static readonly IReadOnlyDictionary<EPilotQuirkId, PilotQuirkDefinition> Definitions =
		new Dictionary<EPilotQuirkId, PilotQuirkDefinition>
		{
			[EPilotQuirkId.PlaceholderPositive] = new(
				EPilotQuirkId.PlaceholderPositive,
				EPilotQuirkPolarity.Positive,
				[],
				"[Dev] No positive quirk",
				"Placeholder until positive quirks are designed."),
			[EPilotQuirkId.PlaceholderNegative] = new(
				EPilotQuirkId.PlaceholderNegative,
				EPilotQuirkPolarity.Negative,
				[],
				"[Dev] No negative quirk",
				"Placeholder until negative quirks are designed."),
			[EPilotQuirkId.GrantProbePositive] = new(
				EPilotQuirkId.GrantProbePositive,
				EPilotQuirkPolarity.Positive,
				[EPilotCapabilityId.LightningShieldDamageBonus],
				"Grant Probe Positive",
				"Test fixture quirk granting a battle capability."),
			[EPilotQuirkId.GrantProbeNegative] = new(
				EPilotQuirkId.GrantProbeNegative,
				EPilotQuirkPolarity.Negative,
				[EPilotCapabilityId.ScrapRewardBonus],
				"Grant Probe Negative",
				"Test fixture quirk granting a strategic capability."),
		};

	public static PilotQuirkDefinition Get(EPilotQuirkId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot quirk '{id}'.");

	public static bool TryGet(EPilotQuirkId id, out PilotQuirkDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotQuirkDefinition> All => Definitions.Values;
}
