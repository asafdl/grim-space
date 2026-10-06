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
		var state = map.ContractRegistry.TryGetState(contract.Id, out var activeState)
			? activeState
			: null;
		var summary = BuildSummary(map, contract, state);
		return new ActiveObjective(
			contract.Id,
			title,
			summary,
			contract.Terms.Payment,
			EObjectiveSource.Contract);
	}

	public static ActiveObjective Project(
		StarMap map,
		Contract contract,
		ContractState state)
	{
		ArgumentNullException.ThrowIfNull(map);
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentNullException.ThrowIfNull(state);
		if (!string.Equals(contract.Id, state.ContractId, StringComparison.Ordinal))
			throw new ArgumentException("Contract and state IDs must match.", nameof(state));

		return new ActiveObjective(
			contract.Id,
			FormatTitle(contract),
			BuildSummary(map, contract, state),
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

	private static ObjectiveSummaryContent BuildSummary(
		StarMap map,
		Contract contract,
		ContractState? state)
	{
		if (contract.Objective is DeliveryObjective delivery)
		{
			var leg = CurrentDeliveryLeg(delivery, state);
			if (leg is FacilityDeliveryLeg facility)
				return BuildFacilityDeliverySummary(map, facility);
		}

		if (TryGetAreaIntel(contract, state, out var intel))
			return BuildSearchAreaSummary(map, intel);

		return PlainOrPreview(map, contract);
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

	private static DeliveryLeg CurrentDeliveryLeg(
		DeliveryObjective delivery,
		ContractState? state) =>
		delivery.Route.Legs[
			state is DeliveryContractState deliveryState
				? deliveryState.Progress.CurrentLegIndex
				: 0];

	private static ObjectiveSummaryContent BuildFacilityDeliverySummary(
		StarMap map,
		FacilityDeliveryLeg facility)
	{
		var dropoff = map.GetPointOfInterest(facility.PoiId);
		return new ObjectiveSummaryContent.NearLandmark(
			$"Deliver cargo to \"{facility.OperatorName}\" at {dropoff.GetFacility(facility.FacilityId).DisplayName} in ",
			facility.PoiId,
			dropoff.DisplayName,
			".");
	}

	private static bool TryGetAreaIntel(
		Contract contract,
		ContractState? state,
		out AreaIntel intel)
	{
		intel = null!;

		switch (contract.Objective)
		{
			case DeliveryObjective delivery
				when CurrentDeliveryLeg(delivery, state) is SpaceMeetingDeliveryLeg meeting:
				intel = meeting.SearchArea.Intel;
				return true;
			case HuntObjective { SpawnGroups.Count: > 0 } hunt:
				intel = hunt.SpawnGroups[0].SearchArea.Intel;
				return true;
			case WreckageObjective wreckage:
				intel = wreckage.SearchArea.Intel;
				return true;
			default:
				return false;
		}
	}

	private static ObjectiveSummaryContent PlainOrPreview(StarMap map, Contract contract) =>
		new ObjectiveSummaryContent.Plain(ContractDisplay.ObjectivePreview(contract, map));
}
