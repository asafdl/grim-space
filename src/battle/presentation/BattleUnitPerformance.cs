using GrimSpace.Battle.Effects;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Presentation;

public readonly record struct UnitDamageTotals(int Dealt, int Taken, int HealingDone);

public static class BattleUnitPerformance
{
	public static IReadOnlyDictionary<string, int> PerformancePointsByUnit(
		IReadOnlyList<ITimelineEntry> timeline,
		IReadOnlySet<string> playerUnitIds)
	{
		var totals = ForPlayerUnits(timeline, playerUnitIds);
		return playerUnitIds.ToDictionary(
			id => id,
			id =>
			{
				var unitTotals = totals.GetValueOrDefault(id);
				return BattlePerformanceScore.Points(
					unitTotals.Dealt,
					unitTotals.Taken,
					unitTotals.HealingDone);
			},
			StringComparer.Ordinal);
	}

	public static IReadOnlyDictionary<string, UnitDamageTotals> ForPlayerUnits(
		IReadOnlyList<ITimelineEntry> timeline,
		IReadOnlySet<string> playerUnitIds)
	{
		ArgumentNullException.ThrowIfNull(timeline);
		ArgumentNullException.ThrowIfNull(playerUnitIds);

		var spawnOwner = BuildSpawnOwnerMap(timeline);
		var dealt = playerUnitIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
		var taken = playerUnitIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);
		var healingDone = playerUnitIds.ToDictionary(id => id, _ => 0, StringComparer.Ordinal);

		foreach (var entry in timeline)
		{
			switch (entry)
			{
				case Record<ImpactFacts> { Value: var impact }:
				{
					var damage = impact.TotalDamage;
					if (damage <= 0)
						break;

					if (playerUnitIds.Contains(impact.TargetId))
						taken[impact.TargetId] += damage;

					var creditedSource = ResolveToPlayerUnit(impact.SourceId, playerUnitIds, spawnOwner);
					if (creditedSource is not null)
						dealt[creditedSource] += damage;
					break;
				}
				case Record<HealFacts> { Value: var heal }:
				{
					var healed = heal.ShieldRestored + heal.HullRestored;
					if (healed <= 0)
						break;

					var creditedHealer = ResolveToPlayerUnit(heal.SourceId, playerUnitIds, spawnOwner);
					if (creditedHealer is not null)
						healingDone[creditedHealer] += healed;
					break;
				}
			}
		}

		return playerUnitIds.ToDictionary(
			id => id,
			id => new UnitDamageTotals(dealt[id], taken[id], healingDone[id]),
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
