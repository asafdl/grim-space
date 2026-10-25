using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Presentation;
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
		IReadOnlyList<ITimelineEntry> timeline,
		Func<string, Pilot?> resolvePilot)
	{
		ArgumentNullException.ThrowIfNull(outcome);
		ArgumentNullException.ThrowIfNull(spawns);
		ArgumentNullException.ThrowIfNull(timeline);
		ArgumentNullException.ThrowIfNull(resolvePilot);

		var handoffs = outcome.StateHandoffs.ToDictionary(h => h.Id, StringComparer.Ordinal);
		var playerUnitIds = spawns
			.Where(spawn => spawn.Team == ETeam.Player)
			.Select(spawn => spawn.Ship.Id)
			.ToHashSet(StringComparer.Ordinal);
		var performance = BattleUnitPerformance.ForPlayerUnits(timeline, playerUnitIds);
		var xpGrants = PilotProgression.RewardsFor(spawns, outcome);

		var pilotRows = new List<BattleVictoryPilotRow>();
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Player || string.IsNullOrEmpty(spawn.PilotId))
				continue;

			handoffs.TryGetValue(spawn.Ship.Id, out var handoff);
			var totals = performance.GetValueOrDefault(spawn.Ship.Id);
			xpGrants.TryGetValue(spawn.PilotId, out var xp);

			var pilot = resolvePilot(spawn.PilotId);
			var gearTier = handoff?.GearTier ?? EShipGearTier.T0;
			pilotRows.Add(new BattleVictoryPilotRow(
				pilot?.Name ?? spawn.PilotId,
				pilot?.PortraitId ?? Pilot.PortraitPool[0],
				PilotHireDisplay.ShipPackageLine(spawn.Ship.Spec.Chassis, gearTier),
				totals.Dealt,
				totals.Taken,
				xp,
				BattlePerformanceScore.Grade(totals.Dealt, totals.Taken)));
		}

		pilotRows.Sort((left, right) =>
		{
			var byScore = BattlePerformanceScore
				.Points(right.DamageDealt, right.DamageTaken)
				.CompareTo(BattlePerformanceScore.Points(left.DamageDealt, left.DamageTaken));
			return byScore != 0
				? byScore
				: string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
		});

		return new BattleVictorySummary(pilotRows);
	}
}
