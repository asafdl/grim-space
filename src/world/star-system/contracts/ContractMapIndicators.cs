namespace GrimSpace.World.StarSystem.Contracts;

public static class ContractMapIndicators
{
	public static Dictionary<string, int> CountPendingByIssuerPoi(StarMap map)
	{
		var counts = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var contract in map.ContractRegistry.Pending)
		{
			if (contract.IssuerPoiId is not { } poiId)
				continue;

			counts.TryGetValue(poiId, out var count);
			counts[poiId] = count + 1;
		}

		return counts;
	}

	public static string TooltipForPoi(StarMap map, string poiId)
	{
		var counts = map.ContractRegistry.Pending
			.Where(contract => contract.IssuerPoiId == poiId)
			.GroupBy(ContractDisplay.Kind)
			.OrderBy(group => group.Key)
			.Select(group => $"{group.Count()}x {ContractDisplay.KindDisplayName(group.Key)}");

		return string.Join("\n", counts);
	}

	public static string TooltipForCount(int count) =>
		count == 1 ? "1 available" : $"{count} available";
}
