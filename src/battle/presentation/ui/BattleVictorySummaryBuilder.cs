using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Run;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Presentation.Facilities;

namespace GrimSpace.Battle.Presentation.Ui;

public static class BattleVictorySummaryBuilder
{
	public static BattleVictorySummary Build(
		BattleOutcome outcome,
		IReadOnlyList<BattleSpawn> spawns,
		IReadOnlyList<ITimelineEntry> timeline)
	{
		ArgumentNullException.ThrowIfNull(outcome);
		ArgumentNullException.ThrowIfNull(spawns);
		ArgumentNullException.ThrowIfNull(timeline);

		var handoffs = outcome.StateHandoffs.ToDictionary(h => h.Id, StringComparer.Ordinal);
		var destroyedEnemies = spawns
			.Where(spawn => spawn.Team == ETeam.Enemy)
			.Where(spawn =>
				handoffs.TryGetValue(spawn.Ship.Id, out var handoff)
				&& handoff.HullPoints <= 0)
			.OrderBy(spawn => PilotHireDisplay.ShipPackageLine(
				spawn.Ship.Spec.Chassis,
				handoffs[spawn.Ship.Id].GearTier))
			.ToArray();

		var damageRows = new List<BattleDamageReportRow>(destroyedEnemies.Length);
		var bundleCompletionWithSingleKill = destroyedEnemies.Length == 1;
		foreach (var enemy in destroyedEnemies)
		{
			var handoff = handoffs[enemy.Ship.Id];
			var label = PilotHireDisplay.ShipPackageLine(enemy.Ship.Spec.Chassis, handoff.GearTier);
			var xp = PilotProgression.KillXpPerKill;
			if (bundleCompletionWithSingleKill)
				xp += PilotProgression.CompletionXp;

			damageRows.Add(new BattleDamageReportRow(label, xp));
		}

		BattleDamageReportRow? completionRow = destroyedEnemies.Length > 1
			? new BattleDamageReportRow(
				BattleHudCopy.VictoryCompletionLabel,
				PilotProgression.CompletionXp)
			: null;

		return new BattleVictorySummary(damageRows, completionRow);
	}
}
