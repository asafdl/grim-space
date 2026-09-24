using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Landmarks;
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
		if (contract.Objective is DeliveryObjective delivery)
			return BuildDeliverySummary(map, delivery);

		var searchIntel = contract.Objective switch
		{
			HuntObjective hunt when hunt.SpawnGroups.Count > 0 => hunt.SpawnGroups[0].SearchArea.Intel,
			WreckageObjective wreckage => wreckage.SearchArea.Intel,
			_ => null,
		};
		if (searchIntel is null)
			return PlainOrPreview(map, contract);

		return BuildSearchAreaSummary(map, searchIntel);
	}

	private static ObjectiveSummaryContent BuildSearchAreaSummary(StarMap map, AreaIntel intel)
	{
		if (!AreaIntelDisplay.TryParseLinkableSegments(intel, out var segments))
			return new ObjectiveSummaryContent.Plain("Search the indicated sector.");

		return segments switch
		{
			AreaIntelDisplay.ParsedSegments.ClosestOnly closest =>
				BuildClosestSummary(map, closest),
			AreaIntelDisplay.ParsedSegments.ClosestAndSecondary pair =>
				BuildClosestAndSecondarySummary(map, pair),
			AreaIntelDisplay.ParsedSegments.ClosestSecondaryAndAnchor triangle =>
				BuildTriangleSummary(map, triangle),
			_ => new ObjectiveSummaryContent.Plain("Search the indicated sector."),
		};
	}

	private static ObjectiveSummaryContent BuildClosestSummary(
		StarMap map,
		AreaIntelDisplay.ParsedSegments.ClosestOnly segments)
	{
		if (!TryResolveLandmarkForSummary(map, segments.LandmarkAId, out var landmarkId, out var displayName))
			return new ObjectiveSummaryContent.Plain("Search the indicated sector.");

		return new ObjectiveSummaryContent.NearLandmark(
			segments.Prefix,
			landmarkId,
			displayName,
			segments.Suffix);
	}

	private static ObjectiveSummaryContent BuildClosestAndSecondarySummary(
		StarMap map,
		AreaIntelDisplay.ParsedSegments.ClosestAndSecondary segments)
	{
		if (!TryResolveLandmarkForSummary(map, segments.LandmarkAId, out var landmarkAId, out var landmarkAName)
			|| !TryResolveLandmarkForSummary(map, segments.LandmarkBId, out var landmarkBId, out var landmarkBName))
			return new ObjectiveSummaryContent.Plain("Search the indicated sector.");

		return new ObjectiveSummaryContent.RouteBetweenLandmarks(
			segments.Prefix,
			landmarkAId,
			landmarkAName,
			segments.Connector,
			landmarkBId,
			landmarkBName,
			segments.Suffix);
	}

	private static ObjectiveSummaryContent BuildTriangleSummary(
		StarMap map,
		AreaIntelDisplay.ParsedSegments.ClosestSecondaryAndAnchor segments)
	{
		if (!TryResolveLandmarkForSummary(map, segments.LandmarkAId, out var landmarkAId, out var landmarkAName)
			|| !TryResolveLandmarkForSummary(map, segments.LandmarkBId, out var landmarkBId, out var landmarkBName)
			|| !TryResolveLandmarkForSummary(map, segments.LandmarkCId, out var landmarkCId, out var landmarkCName))
			return new ObjectiveSummaryContent.Plain("Search the indicated sector.");

		return new ObjectiveSummaryContent.RouteAmongLandmarks(
			segments.Prefix,
			landmarkAId,
			landmarkAName,
			segments.ConnectorAB,
			landmarkBId,
			landmarkBName,
			segments.ConnectorBC,
			landmarkCId,
			landmarkCName,
			segments.Suffix);
	}

	private static bool TryResolveLandmarkForSummary(
		StarMap map,
		string landmarkId,
		out string resolvedId,
		out string displayName)
	{
		resolvedId = landmarkId;
		if (AreaBorderAnchor.TryGetDisplayName(landmarkId) is { } rimName)
		{
			displayName = rimName;
			return true;
		}

		if (MapLandmarkQueries.TryGet(map, landmarkId, out var landmark))
		{
			displayName = landmark.DisplayName;
			return true;
		}

		displayName = "";
		return false;
	}

	private static ObjectiveSummaryContent BuildDeliverySummary(
		StarMap map,
		DeliveryObjective delivery)
	{
		var dropoff = map.PointsOfInterest.FirstOrDefault(poi => poi.Id == delivery.TurnInPoiId);
		if (dropoff is null)
			return new ObjectiveSummaryContent.Plain("Deliver cargo to the designated contact.");

		// Acceptance happens at the issuer; cargo is already aboard. Single-leg deliveries only for now.
		return new ObjectiveSummaryContent.NearLandmark(
			$"Deliver cargo to \"{delivery.TurnInOperatorName}\" at ",
			delivery.TurnInPoiId,
			dropoff.DisplayName,
			".");
	}

	private static ObjectiveSummaryContent PlainOrPreview(StarMap map, Contract contract) =>
		new ObjectiveSummaryContent.Plain(ContractDisplay.ObjectivePreview(contract, map));
}
