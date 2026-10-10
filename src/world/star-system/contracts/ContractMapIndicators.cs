namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractMapIndicators
{
	public static Dictionary<string, int> CountPendingByPresenterPoi(StarMap map)
	{
		var pendingIds = map.ContractRegistry.Pending
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var poi in map.PointsOfInterest)
		{
			var count = ContractAssignments(poi)
				.Sum(assignment => assignment.SourceIds.Count(pendingIds.Contains));
			if (count > 0)
				counts[poi.Id] = count;
		}

		return counts;
	}

	public static string TooltipForPoi(StarMap map, string poiId)
	{
		var poi = map.GetPointOfInterest(poiId);
		var assignedIds = ContractAssignments(poi)
			.SelectMany(assignment => assignment.SourceIds)
			.ToHashSet(StringComparer.Ordinal);
		var counts = map.ContractRegistry.Pending
			.Where(contract => assignedIds.Contains(contract.Id))
			.GroupBy(ContractDisplay.Kind)
			.OrderBy(group => group.Key)
			.Select(group => $"{group.Count()}x {ContractDisplay.KindDisplayName(group.Key)}");

		return string.Join("\n", counts);
	}

	public static string TooltipForCount(int count) =>
		count == 1 ? "1 available" : $"{count} available";

	private static IEnumerable<Poi.FacilityOperatorTemporaryRoles.Assignment> ContractAssignments(
		Poi.PointOfInterest poi) =>
		poi.OperatorTemporaryRoles.Assignments(Poi.EFacilityOperatorRole.Contracts)
			.Concat(poi.OperatorTemporaryRoles.Assignments(Poi.EFacilityOperatorRole.StoryContact));
}
