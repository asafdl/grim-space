using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation;

/// <summary>Maps destroyed enemy units to the last player unit that damaged them in battle history.</summary>
public static class BattleKillAttribution
{
	public static IReadOnlyDictionary<string, string> LastPlayerDamagerByTarget(
		IReadOnlyList<ITimelineEntry> timeline,
		IReadOnlySet<string> playerUnitIds)
	{
		ArgumentNullException.ThrowIfNull(timeline);
		ArgumentNullException.ThrowIfNull(playerUnitIds);

		var spawnOwner = BuildSpawnOwnerMap(timeline);
		var lastDamager = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var entry in timeline)
		{
			if (entry is not Record<ImpactFacts> { Value: var impact })
				continue;

			var resolvedSource = ResolveToPlayerUnit(impact.SourceId, playerUnitIds, spawnOwner);
			if (resolvedSource is null)
				continue;

			lastDamager[impact.TargetId] = resolvedSource;
		}

		return lastDamager;
	}

	private static Dictionary<string, string> BuildSpawnOwnerMap(IReadOnlyList<ITimelineEntry> timeline)
	{
		var map = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var entry in timeline)
		{
			if (entry is not Record<SpawnFacts> { Value: var spawn })
				continue;

			map[spawn.TargetId] = spawn.SourceId;
		}

		return map;
	}

	private static string? ResolveToPlayerUnit(
		string sourceId,
		IReadOnlySet<string> playerUnitIds,
		IReadOnlyDictionary<string, string> spawnOwner)
	{
		var current = sourceId;
		var visited = new HashSet<string>(StringComparer.Ordinal);
		while (visited.Add(current))
		{
			if (playerUnitIds.Contains(current))
				return current;

			if (!spawnOwner.TryGetValue(current, out var owner))
				return null;

			current = owner;
		}

		return null;
	}
}
