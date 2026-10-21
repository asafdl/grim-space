namespace GrimSpace.Pilots;

public static class PilotQuirkCatalog
{
	public static IReadOnlyList<EPilotQuirkId> RecruitablePositive { get; } = Array.Empty<EPilotQuirkId>();

	public static IReadOnlyList<EPilotQuirkId> RecruitableNegative { get; } = Array.Empty<EPilotQuirkId>();

	private static readonly IReadOnlyDictionary<EPilotQuirkId, PilotQuirkDefinition> Definitions =
		new Dictionary<EPilotQuirkId, PilotQuirkDefinition>
		{
			[EPilotQuirkId.Charismatic] = new(
				EPilotQuirkId.Charismatic,
				EPilotQuirkPolarity.Positive,
				[],
				"Charismatic",
				"Pilot hires cost 5% less. Effect is not implemented yet."),
			[EPilotQuirkId.NeuroDrugAddict] = new(
				EPilotQuirkId.NeuroDrugAddict,
				EPilotQuirkPolarity.Negative,
				[],
				"Neuro-drug Addict",
				"Prone to accidents and may steal credits. Effects are not implemented yet."),
		};

	public static PilotQuirkDefinition Get(EPilotQuirkId id) =>
		Definitions.TryGetValue(id, out var definition)
			? definition
			: throw new KeyNotFoundException($"Unknown pilot quirk '{id}'.");

	public static bool TryGet(EPilotQuirkId id, out PilotQuirkDefinition definition) =>
		Definitions.TryGetValue(id, out definition!);

	public static IEnumerable<PilotQuirkDefinition> All => Definitions.Values;

	public static EPilotQuirkId Roll(EPilotQuirkPolarity polarity, Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var options = Definitions.Values
			.Where(definition => definition.Polarity == polarity)
			.Select(definition => definition.Id)
			.ToArray();
		if (options.Length == 0)
			throw new InvalidOperationException($"No {polarity} quirks are registered.");

		return options[rng.Next(options.Length)];
	}

	public static EPilotQuirkId? RollRecruitableQuirk(EPilotQuirkPolarity polarity, Random rng)
	{
		ArgumentNullException.ThrowIfNull(rng);
		var pool = polarity == EPilotQuirkPolarity.Positive
			? RecruitablePositive
			: RecruitableNegative;
		if (pool.Count == 0)
			return null;

		return pool[rng.Next(pool.Count)];
	}
}
