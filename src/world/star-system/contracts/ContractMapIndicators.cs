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

	public static string TooltipForCount(int count) =>
		count == 1 ? "1 available" : $"{count} available";
}
