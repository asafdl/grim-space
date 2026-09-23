using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Landmarks;
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

	public static string DetailsBody(Contract contract, StarMap map)
	{
		var parts = new List<string>();
		var briefing = Narrative(contract);
		if (!string.IsNullOrWhiteSpace(briefing))
			parts.Add(briefing.Trim());

		var searchArea = SearchArea(contract, map);
		if (searchArea != "—")
			parts.Add(searchArea.Trim());

		var reward = Reward(contract);
		if (reward != "—")
			parts.Add($"Compensation is {reward}.");

		var danger = Danger(contract);
		if (danger != "—")
			parts.Add($"Threat assessment: {danger}.");

		return parts.Count == 0 ? "—" : string.Join("\n\n", parts);
	}

	internal static string ObjectivePreview(Contract contract, StarMap map) =>
		contract.Objective switch
		{
			HuntObjective hunt => FormatHuntObjective(hunt),
			DeliveryObjective delivery => FormatDeliveryObjective(contract, delivery, map),
			_ => "—",
		};

	public static string SearchArea(Contract contract, StarMap map) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				FormatSearchAreaIntel(hunt.SpawnGroups[0].SearchArea.Intel, map),
			DeliveryObjective delivery => FormatDeliveryRoute(contract, delivery, map),
			_ => "—",
		};

	internal static string FormatSearchAreaIntel(AreaIntel intel, StarMap map) =>
		AreaIntelDisplay.FormatPlain(
			intel,
			id => AreaBorderAnchor.TryGetDisplayName(id) ?? MapLandmarkQueries.GetDisplayName(map, id));

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

	private static string FormatDeliveryObjective(Contract contract, DeliveryObjective delivery, StarMap map)
	{
		var issuerName = ResolvePoiDisplayName(map, contract.IssuerPoiId);
		var dropoffName = ResolvePoiDisplayName(map, delivery.TurnInPoiId);
		return $"Pick up cargo at {issuerName}, then deliver it to {dropoffName}.";
	}

	private static string FormatDeliveryRoute(Contract contract, DeliveryObjective delivery, StarMap map)
	{
		var issuerName = ResolvePoiDisplayName(map, contract.IssuerPoiId);
		var dropoffName = ResolvePoiDisplayName(map, delivery.TurnInPoiId);
		return $"From {issuerName} to {dropoffName}.";
	}

	private static string ResolvePoiDisplayName(StarMap map, string? poiId) =>
		poiId is null
			? "—"
			: map.PointsOfInterest.FirstOrDefault(poi => poi.Id == poiId)?.DisplayName ?? poiId;

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

	private static string DangerDisplayName(EDangerLevel danger) =>
		danger switch
		{
			EDangerLevel.VeryLow => "minimal",
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

	private static string FormatCount(int count) => count == 1 ? "one" : count.ToString();

	private static string Pluralize(int count, string singular, string plural) =>
		count == 1 ? singular : plural;
}
