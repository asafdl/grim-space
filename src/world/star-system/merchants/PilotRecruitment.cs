using GrimSpace.Math;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class PilotRecruitment
{
	public const int CandidatesPerLead = 3;

	private const string HireRollScope = "pilot-hire";

	private static readonly EPilotBackgroundId[] RecruitableBackgrounds =
	[
		EPilotBackgroundId.ScrapDrone,
		EPilotBackgroundId.OptimalityEnforcer,
		EPilotBackgroundId.CorporateSlave,
		EPilotBackgroundId.PodRacer,
		EPilotBackgroundId.AsteroidSurfer,
		EPilotBackgroundId.SynthSinger,
	];

	private static readonly EOperatorClassId[] RecruitableOperatorClasses =
	[
		EOperatorClassId.SpaceRanger,
		EOperatorClassId.Support,
	];

	public static IReadOnlyList<EPilotBackgroundId> RecruitableBackgroundIds { get; } =
		RecruitableBackgrounds;

	public static IReadOnlyList<EOperatorClassId> RecruitableOperatorClassIds { get; } =
		RecruitableOperatorClasses;

	public static bool IsRecruitableBackground(EPilotBackgroundId backgroundId) =>
		RecruitableBackgrounds.Contains(backgroundId);

	public static IReadOnlyList<PilotHireCandidate> RollCandidates(StarMap map, string contractId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		if (RecruitableBackgroundIds.Count == 0)
			throw new InvalidOperationException("No pilot backgrounds are configured for recruitment.");
		if (RecruitableOperatorClassIds.Count == 0)
			throw new InvalidOperationException("No operator classes are configured for recruitment.");

		var candidates = new PilotHireCandidate[CandidatesPerLead];
		for (var slot = 0; slot < CandidatesPerLead; slot++)
		{
			var name = Pilot.RollRecruitDisplayName(SlotRng(map.Seed, contractId, slot, "name"));
			var portraitId = Pilot.RollRecruitPortraitId(SlotRng(map.Seed, contractId, slot, "portrait"));
			var backgroundId = RecruitableBackgrounds[
				SlotRng(map.Seed, contractId, slot, "background").Next(RecruitableBackgrounds.Length)];
			var classId = RecruitableOperatorClasses[
				SlotRng(map.Seed, contractId, slot, "class").Next(RecruitableOperatorClasses.Length)];
			var baseSalary = StrategicEconomyConfig.RollBaseSalaryCredits(
				SlotRng(map.Seed, contractId, slot, "salary"));
			var profile = new PilotRecruitProfile(
				name,
				portraitId,
				backgroundId,
				experiencePoints: 0,
				claimedLevel: PilotProgression.LevelFor(0),
				baseSalaryCredits: baseSalary,
				classId: classId);

			candidates[slot] = new PilotHireCandidate(slot, profile);
		}

		return candidates;
	}

	public static bool TryGetHireCandidate(
		StarMap map,
		string contractId,
		int slot,
		out PilotHireCandidate candidate)
	{
		candidate = null!;
		if (slot < 0 || slot >= CandidatesPerLead)
			return false;

		try
		{
			var rolled = RollCandidates(map, contractId)
				.FirstOrDefault(row => row.Slot == slot);
			if (rolled is null)
				return false;

			candidate = rolled;
			return true;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	/// <summary>Display/hire-time price. Charismatic discount deferred.</summary>
	public static ResourceBundle Price(StarMap map, PilotHireCandidate candidate)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(candidate);

		return DefaultHirePrice(map);
	}

	/// <summary>Hire price until pilot-specific pricing exists.</summary>
	public static ResourceBundle DefaultHirePrice(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);
		return ResourceBundle.Of(ResourceId.Credits, StarMarketSeed.PilotHireCreditCost);
	}

	public static IReadOnlyList<PilotHireCandidate> RollAvailableCandidates(
		StarMap map,
		string contractId,
		IReadOnlyCollection<int> purchasedSlots)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		ArgumentNullException.ThrowIfNull(purchasedSlots);

		if (purchasedSlots.Count == 0)
			return RollCandidates(map, contractId);

		var purchased = purchasedSlots as HashSet<int> ?? purchasedSlots.ToHashSet();
		return RollCandidates(map, contractId)
			.Where(candidate => !purchased.Contains(candidate.Slot))
			.ToArray();
	}

	public sealed record PilotHireCandidate(int Slot, PilotRecruitProfile Profile);

	private static Random SlotRng(int mapSeed, string contractId, int slot, string stream) =>
		new(unchecked((int)SlotSeed(mapSeed, contractId, slot, stream)));

	private static ulong SlotSeed(int mapSeed, string contractId, int slot, string stream) =>
		StableSeedMixer.From(mapSeed)
			.Add(HireRollScope)
			.Add(contractId)
			.Add(slot)
			.Add(stream)
			.Value;
}
