using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Contracts.Generation;

public sealed class ContractNarrativePicker
{
	private readonly ContractNarrativePickerConfig _config;

	public ContractNarrativePicker(ContractNarrativePickerConfig? config = null) =>
		_config = config ?? new ContractNarrativePickerConfig();

	public ContractNarrative Pick(
		StarMap map,
		string issuerPoiId,
		EContractKind kind,
		int tick,
		int slotIndex)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(issuerPoiId);

		var eligible = EligibleEntries(map, issuerPoiId, kind);
		if (eligible.Count == 0)
			return Fallback(kind);

		var random = CreateRandom(map.Seed, tick, slotIndex, issuerPoiId, kind);
		var index = (int)(random.NextDouble() * eligible.Count);
		return eligible[index].ToNarrative();
	}

	private IReadOnlyList<ContractNarrativePoolEntry> EligibleEntries(
		StarMap map,
		string issuerPoiId,
		EContractKind kind)
	{
		var byKind = _config.Entries.Where(entry => entry.Kind == kind).ToArray();
		if (byKind.Length == 0)
			return [];

		var facilitySpecific = byKind
			.Where(entry => entry.FacilitySlug is not null
				&& IssuerHasFacility(map, issuerPoiId, entry.FacilitySlug))
			.ToArray();
		if (facilitySpecific.Length > 0)
			return facilitySpecific;

		return byKind.Where(entry => entry.FacilitySlug is null).ToArray();
	}

	private static bool IssuerHasFacility(StarMap map, string issuerPoiId, string facilitySlug)
	{
		var poi = map.PointsOfInterest.FirstOrDefault(candidate =>
			string.Equals(candidate.Id, issuerPoiId, StringComparison.Ordinal));
		if (poi is null)
			return false;

		var scopedId = Facility.ScopedId(issuerPoiId, facilitySlug);
		return poi.Facilities.Any(facility =>
			string.Equals(facility.Id, scopedId, StringComparison.Ordinal));
	}

	private static ContractNarrative Fallback(EContractKind kind) =>
		kind switch
		{
			EContractKind.Hunt => ContractNarrative.ForHunt("Pirate Hunt"),
			EContractKind.Delivery => ContractNarrative.ForDelivery(
				"Supply Run",
				"Take this package to the marked facility. The recipient knows what it is. We have made a deliberate choice not to.",
				"Put it down gently. No, not there. There. No—fine. Payment is already someone else's problem."),
			_ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
		};

	private static StableRandom CreateRandom(
		int mapSeed,
		int tick,
		int slotIndex,
		string issuerPoiId,
		EContractKind kind) =>
		new(StableSeedMixer.From(mapSeed)
			.Add(tick)
			.Add(slotIndex)
			.Add(issuerPoiId)
			.Add(kind.ToString())
			.Add("contract-narrative")
			.Value);
}
