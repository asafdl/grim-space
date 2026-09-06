using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;

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
			HuntObjective hunt => string.Join(
				"; ",
				hunt.SpawnGroups.Select(group =>
					$"{group.RequiredCount}x {FactionCatalog.DisplayName(group.Spawn.Faction)} ({group.Spawn.Danger})")),
			_ => "—",
		};

	public static string ObjectivePreview(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				$"Hunt {hunt.SpawnGroups[0].RequiredCount}× {FactionCatalog.DisplayName(hunt.SpawnGroups[0].Spawn.Faction)}",
			_ => "—",
		};

	public static string SearchArea(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				hunt.SpawnGroups[0].SearchArea.Description,
			_ => "—",
		};

	public static string Reward(Contract contract) => $"{contract.Terms.RewardCredits} cr";

	public static string Danger(Contract contract) =>
		contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 =>
				hunt.SpawnGroups[0].Spawn.Danger.ToString(),
			_ => "—",
		};

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
}
