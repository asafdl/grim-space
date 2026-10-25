using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation;

public readonly record struct UnitDamageTotals(int Dealt, int Taken);

public static class BattleUnitPerformance
{
	public static IReadOnlyDictionary<string, UnitDamageTotals> ForPlayerUnits(
		IReadOnlyList<ITimelineEntry> timeline,
		IReadOnlySet<string> playerUnitIds)
	{
		ArgumentNullException.ThrowIfNull(timeline);
		ArgumentNullException.ThrowIfNull(playerUnitIds);

		var spawnOwner = BuildSpawnOwnerMap(timeline);
		var dealt = playerUnitIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
		var taken = playerUnitIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

		foreach (var entry in timeline)
		{
			if (entry is not Record<ImpactFacts> { Value: var impact })
				continue;

			var damage = impact.TotalDamage;
			if (damage <= 0)
				continue;

			if (playerUnitIds.Contains(impact.TargetId))
				taken[impact.TargetId] += damage;

			var creditedSource = ResolveToPlayerUnit(impact.SourceId, playerUnitIds, spawnOwner);
			if (creditedSource is not null)
				dealt[creditedSource] += damage;
		}

		return playerUnitIds.ToDictionary(
			id => id,
			id => new UnitDamageTotals(dealt[id], taken[id]),
			StringComparer.Ordinal);
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
