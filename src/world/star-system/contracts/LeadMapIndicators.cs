namespace GrimSpace.World.StarSystem.Contracts;

public static class LeadMapIndicators
{
	public static Dictionary<string, int> CountPendingByPresenterPoi(StarMap map)
	{
		var pendingIds = map.ContractRegistry.Pending
			.Where(contract => contract.Category == EContractCategory.Lead)
			.Select(contract => contract.Id)
			.ToHashSet(StringComparer.Ordinal);
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var poi in map.PointsOfInterest)
		{
			var count = poi.OperatorTemporaryRoles
				.Assignments(Poi.EFacilityOperatorRole.LeadContact)
				.Sum(assignment => assignment.SourceIds.Count(pendingIds.Contains));
			if (count > 0)
				counts[poi.Id] = count;
		}

		return counts;
	}

	public static string TooltipForPoi(StarMap map, string poiId)
	{
		var poi = map.GetPointOfInterest(poiId);
		var assignedIds = poi.OperatorTemporaryRoles
			.Assignments(Poi.EFacilityOperatorRole.LeadContact)
			.SelectMany(assignment => assignment.SourceIds)
			.ToHashSet(StringComparer.Ordinal);
		var count = map.ContractRegistry.Pending
			.Count(contract =>
				contract.Category == EContractCategory.Lead
				&& assignedIds.Contains(contract.Id));
		return count == 1 ? "1 lead available" : $"{count} leads available";
	}
}
