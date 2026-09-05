using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.World.StarSystem.Contracts;

// TODO: fix this shit
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
}
