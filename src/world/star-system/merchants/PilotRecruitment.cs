using GrimSpace.Core.Ids;
using GrimSpace.Math;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Merchants;

public static class PilotRecruitment
{
	public const int CandidatesPerLead = 3;

	public static EType DefaultHireChassis => EType.RepurposedMiner;

	public static EShipGearTier DefaultHireGearTier => EShipGearTier.T0;

	private const string HireRollScope = "pilot-hire";

	private static readonly (EPilotBackgroundId BackgroundId, EMerchantAvailabilityBand MinimumBand)[]
		BackgroundBands =
		[
			(EPilotBackgroundId.ScrapDrone, EMerchantAvailabilityBand.T0),
			(EPilotBackgroundId.OptimalityEnforcer, EMerchantAvailabilityBand.T0),
			(EPilotBackgroundId.CorporateSlave, EMerchantAvailabilityBand.T0),
			(EPilotBackgroundId.PodRacer, EMerchantAvailabilityBand.T0),
			(EPilotBackgroundId.AsteroidSurfer, EMerchantAvailabilityBand.T0),
			(EPilotBackgroundId.SynthSinger, EMerchantAvailabilityBand.T0),
		];

	public static IReadOnlyList<EPilotBackgroundId> RecruitableBackgroundIds { get; } =
		BackgroundBands.Select(entry => entry.BackgroundId).ToArray();

	public static bool IsRecruitableBackground(EPilotBackgroundId backgroundId) =>
		BackgroundBands.Any(entry => entry.BackgroundId == backgroundId);

	public static IReadOnlyList<EPilotBackgroundId> ListUnlockedBackgrounds(
		EMerchantAvailabilityBand marketTier)
	{
		var results = new List<EPilotBackgroundId>(BackgroundBands.Length);
		foreach (var (backgroundId, minimumBand) in BackgroundBands)
		{
			if (MerchantCatalog.IncludesSheet(marketTier, minimumBand))
				results.Add(backgroundId);
		}

		return results;
	}

	public static IReadOnlyList<PilotHireCandidate> RollCandidates(StarMap map, string contractId)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentException.ThrowIfNullOrEmpty(contractId);

		var tier = MerchantCatalog.Tier(map);
		var backgrounds = ListUnlockedBackgrounds(tier);
		if (backgrounds.Count == 0)
			throw new InvalidOperationException(
				"No pilot backgrounds are unlocked for recruitment at the current market tier.");

		var candidates = new PilotHireCandidate[CandidatesPerLead];
		for (var slot = 0; slot < CandidatesPerLead; slot++)
		{
			var name = Pilot.RollRecruitDisplayName(SlotRng(map.Seed, contractId, slot, "name"));
			var portraitId = Pilot.RollRecruitPortraitId(SlotRng(map.Seed, contractId, slot, "portrait"));
			var backgroundId = backgrounds[
				SlotRng(map.Seed, contractId, slot, "background").Next(backgrounds.Count)];
			var profile = new PilotRecruitProfile(
				name,
				portraitId,
				backgroundId,
				experiencePoints: 0,
				claimedLevel: PilotProgression.LevelFor(0));

			var shipId = TypedIdGenerator.Format(
				UnitTypeSlug.For(DefaultHireChassis),
				$"hire-{SlotSeed(map.Seed, contractId, slot, "ship-id"):x}");
			var ship = new ShipSpawnDeclaration(shipId, DefaultHireChassis, DefaultHireGearTier);

			candidates[slot] = new PilotHireCandidate(slot, profile, ship);
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

	/// <summary>Hire bundle price until pilot-specific pricing exists.</summary>
	public static ResourceBundle DefaultHirePrice(StarMap map)
	{
		ArgumentNullException.ThrowIfNull(map);

		var tier = MerchantCatalog.Tier(map);
		if (map.Market.TryFindRecruitmentOffer(
			    DefaultHireChassis,
			    DefaultHireGearTier,
			    tier,
			    out var offer))
			return offer.Cost;

		return ResourceBundle.Of(ResourceId.Credits, StarMarketSeed.RepurposedMinerCreditCost);
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

	public sealed record PilotHireCandidate(
		int Slot,
		PilotRecruitProfile Profile,
		ShipSpawnDeclaration Ship);

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
