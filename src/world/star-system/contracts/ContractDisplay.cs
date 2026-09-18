using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractDisplay
{
	public static string Title(Contract contract) => contract.Narrative.Title;

	public static string Narrative(Contract contract) => contract.Narrative.Briefing;

	public static string Issuer(Contract contract, StarMap map)
	{
		var faction = FactionCatalog.DisplayName(contract.IssuerFaction);
		if (contract.IssuerPoiId is not { } poiId)
			return faction;

		var poiName = map.PointsOfInterest.First(poi => poi.Id == poiId).DisplayName;
		return $"{faction} · {poiName}";
	}

	public static string ObjectiveSummary(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt =>
				$"{FormatHuntObjective(hunt)} Expected force: {FormatForceEstimate(hunt)}.",
			_ => "—",
		};

	public static string ObjectivePreview(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt => FormatHuntObjective(hunt),
			_ => "—",
		};

	public static string ForceEstimate(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt => FormatForceEstimate(hunt),
			_ => "—",
		};

	public static string SearchArea(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				hunt.SpawnGroups[0].SearchArea.Description,
			_ => "—",
		};

	public static string Reward(Contract contract) =>
		contract.Terms.Payment.IsEmpty
			? "—"
			: string.Join(", ", contract.Terms.Payment.Select(FormatResource));

	private static string FormatResource(KeyValuePair<ResourceId, int> entry) =>
		entry.Key switch
		{
			ResourceId.Credits => $"{entry.Value} {Pluralize(entry.Value, "credit", "credits")}",
			ResourceId.ScrapAlloy => $"{entry.Value} scrap alloy",
			ResourceId.IndustrialCore =>
				$"{entry.Value} {Pluralize(entry.Value, "industrial core", "industrial cores")}",
			_ => throw new ArgumentOutOfRangeException(nameof(entry), entry.Key, null),
		};

	public static string Danger(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				DangerDisplayName(hunt.SpawnGroups.Max(group => group.Spawn.Danger)),
			_ => "—",
		};

	public static int Difficulty(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				Difficulty(hunt.SpawnGroups.Max(group => group.Spawn.Danger)),
			_ => 0,
		};

	public static string DifficultyStars(Contract contract)
	{
		const int maxDifficulty = 5;
		var difficulty = Difficulty(contract);
		return new string('★', difficulty) + new string('☆', maxDifficulty - difficulty);
	}

	public static bool TryGetDangerLevel(Contract contract, out EDangerLevel danger)
	{
		if (contract.Objective is HuntObjective hunt && hunt.SpawnGroups.Count > 0)
		{
			danger = hunt.SpawnGroups[0].Spawn.Danger;
			return true;
		}

		danger = default;
		return false;
	}

	private static string FormatHuntObjective(HuntObjective hunt)
	{
		if (hunt.SpawnGroups.Count == 0)
			return "No target information available.";

		var targets = hunt.SpawnGroups
			.GroupBy(group => group.Spawn.Faction)
			.Select(group =>
			{
				var count = group.Sum(spawn => spawn.RequiredCount);
				return $"{FormatCount(count)} {TargetName(group.Key, count)}";
			});
		return $"Locate and eliminate {string.Join(" and ", targets)}.";
	}

	private static string FormatForceEstimate(HuntObjective hunt)
	{
		var craft = hunt.SpawnGroups
			.SelectMany(group => group.Spawn.MemberTypes.Select(type => (type, group.RequiredCount)))
			.GroupBy(entry => entry.type)
			.Select(group => (Type: group.Key, Count: group.Sum(entry => entry.RequiredCount)))
			.ToArray();
		if (craft.Length == 0)
			return "unavailable";

		return string.Join(
			", ",
			craft.Select(entry => $"{entry.Count} {CraftName(entry.Type, entry.Count)}"));
	}

	private static string DangerDisplayName(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => "Minimal",
			_ => throw new ArgumentOutOfRangeException(nameof(danger), danger, null),
		};

	private static int Difficulty(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(danger), danger, null),
		};

	private static string TargetName(EFaction faction, int count) =>
		faction switch
		{
			EFaction.Pirates => Pluralize(count, "pirate fleet", "pirate fleets"),
			EFaction.TheOptimality => Pluralize(count, "Optimality fleet", "Optimality fleets"),
			_ => throw new ArgumentOutOfRangeException(nameof(faction), faction, null),
		};

	private static string CraftName(GrimSpace.Units.Enums.EType type, int count) =>
		type switch
		{
			GrimSpace.Units.Enums.EType.Fighter => Pluralize(count, "fighter", "fighters"),
			GrimSpace.Units.Enums.EType.Carrier => Pluralize(count, "carrier", "carriers"),
			GrimSpace.Units.Enums.EType.Patrol => "patrol craft",
			GrimSpace.Units.Enums.EType.Torpedo => "torpedo craft",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
		};

	private static string FormatCount(int count) => count == 1 ? "one" : count.ToString();

	private static string Pluralize(int count, string singular, string plural) =>
		count == 1 ? singular : plural;
}
