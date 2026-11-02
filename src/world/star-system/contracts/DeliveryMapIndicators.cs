using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Math.Grid;

namespace GrimSpace.World.StarSystem.Contracts;

public static class DeliveryMapIndicators
{
	public readonly record struct MeetingMarker(string MeetingId, Coord Position);

	public static Dictionary<string, int> CountActiveByPresenterPoi(StarMap map)
	{
		var activeIds = ActiveContractIds(map);
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var poi in map.PointsOfInterest)
		{
			var count = poi.OperatorTemporaryRoles
				.Assignments(Poi.EFacilityOperatorRole.DeliveryTurnIn)
				.Sum(assignment => assignment.SourceIds.Count(activeIds.Contains));
			if (count > 0)
				counts[poi.Id] = count;
		}

		return counts;
	}

	public static bool HasActiveAtFacility(StarMap map, Poi.PointOfInterest poi, string facilityId)
	{
		var activeIds = ActiveContractIds(map);
		return poi.OperatorTemporaryRoles
			.Assignments(Poi.EFacilityOperatorRole.DeliveryTurnIn)
			.Where(assignment => assignment.FacilityId == facilityId)
			.SelectMany(assignment => assignment.SourceIds)
			.Any(activeIds.Contains);
	}

	public static string TooltipForPoi(StarMap map, string poiId) =>
		TooltipForCount(CountActiveByPresenterPoi(map).GetValueOrDefault(poiId));

	public static string TooltipForCount(int count) =>
		count == 1 ? "Delivery" : $"{count} deliveries";

	public static IReadOnlyList<MeetingMarker> ActiveMeetings(StarMap map)
	{
		var markers = new List<MeetingMarker>();
		foreach (var contract in map.ContractRegistry.All)
		{
			if (contract.Objective is not DeliveryObjective delivery
				|| !map.ContractRegistry.TryGetState(contract.Id, out var state)
				|| state is not DeliveryContractState deliveryState
				|| deliveryState.Status != EContractStatus.Active
				|| deliveryState.IsObjectiveMet()
				|| delivery.Route.Legs[deliveryState.Progress.CurrentLegIndex]
					is not SpaceMeetingDeliveryLeg meeting)
				continue;

			markers.Add(new MeetingMarker(meeting.MeetingId, meeting.Position));
		}

		return markers;
	}

	private static HashSet<string> ActiveContractIds(StarMap map)
	{
		var ids = new HashSet<string>(StringComparer.Ordinal);
		foreach (var contract in map.ContractRegistry.All)
		{
			if (contract.Objective is not DeliveryObjective)
				continue;
			if (map.ContractRegistry.TryGetState(contract.Id, out var state)
				&& state.Status == EContractStatus.Active
				&& !state.IsObjectiveMet())
				ids.Add(contract.Id);
		}

		return ids;
	}
}
