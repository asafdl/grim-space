using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.World.StarSystem.Objectives;

public static class ContractObjectiveProjection
{
	public static ActiveObjective Project(StarMap map, Contract contract)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(contract);

		var title = FormatTitle(contract);
		var summary = BuildSummary(map, contract);
		return new ActiveObjective(
			contract.Id,
			title,
			summary,
			contract.Terms.Payment,
			EObjectiveSource.Contract);
	}

	private static string FormatTitle(Contract contract)
	{
		var difficulty = ContractDisplay.Difficulty(contract);
		if (difficulty == 0)
			return ContractDisplay.Title(contract);

		return $"{ContractDisplay.Title(contract)} {new string('★', difficulty)}";
	}

	private static ObjectiveSummaryContent BuildSummary(StarMap map, Contract contract)
	{
		if (contract.Objective is DeliveryObjective delivery
			&& contract.IssuerPoiId is { } issuerPoiId)
			return BuildDeliverySummary(map, issuerPoiId, delivery.TurnInPoiId);

		if (contract.Objective is not HuntObjective hunt
			|| hunt.SpawnGroups.Count == 0)
			return PlainOrPreview(map, contract);

		var intel = hunt.SpawnGroups[0].SearchArea.Intel;
		if (!AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments))
			return PlainOrPreview(map, contract);

		var poiA = map.PointsOfInterest.FirstOrDefault(poi => poi.Id == segments.LandmarkAId);
		var poiB = map.PointsOfInterest.FirstOrDefault(poi => poi.Id == segments.LandmarkBId);
		if (poiA is null || poiB is null)
			return PlainOrPreview(map, contract);

		return new ObjectiveSummaryContent.RouteBetweenLandmarks(
			segments.Prefix,
			poiA.Id,
			poiA.DisplayName,
			segments.Connector,
			poiB.Id,
			poiB.DisplayName,
			segments.Suffix);
	}

	private static ObjectiveSummaryContent BuildDeliverySummary(
		StarMap map,
		string issuerPoiId,
		string dropoffPoiId)
	{
		var issuer = map.PointsOfInterest.FirstOrDefault(poi => poi.Id == issuerPoiId);
		var dropoff = map.PointsOfInterest.FirstOrDefault(poi => poi.Id == dropoffPoiId);
		if (issuer is null || dropoff is null)
			return new ObjectiveSummaryContent.Plain("Deliver cargo to the designated contact.");

		return new ObjectiveSummaryContent.RouteBetweenLandmarks(
			"Pick up at ",
			issuerPoiId,
			issuer.DisplayName,
			", deliver to ",
			dropoffPoiId,
			dropoff.DisplayName,
			".");
	}

	private static ObjectiveSummaryContent PlainOrPreview(StarMap map, Contract contract) =>
		new ObjectiveSummaryContent.Plain(ContractDisplay.ObjectivePreview(contract, map));
}
