using GrimSpace.Battle.Combat;
using GrimSpace.Battle.Units;
using GrimSpace.Domain.Combat;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Planning;

public sealed class SimulatedTurn
{
	public required State Player { get; init; }
	public required State Enemy { get; init; }
	public required IReadOnlyList<Hazard> Hazards { get; init; }
	public int MissilesPlanned { get; init; }
	public bool RailgunPlanned { get; init; }
}

public static class PlanSimulator
{
	public static SimulatedTurn Simulate(
		Unit player,
		Unit enemy,
		BattleGrid grid,
		IReadOnlyList<PlannedAction> actions)
	{
		var playerState = StateSnapshot.Clone(player.State);
		var enemyState = StateSnapshot.Clone(enemy.State);
		var hazards = new List<Hazard>();
		var missilesPlanned = 0;
		var railgunPlanned = false;

		foreach (var action in actions)
		{
			switch (action)
			{
				case PlannedMove move:
					player.Movement.ApplyMove(playerState, move.Option);
					playerState.ActionPoints -= move.Option.ApCost;
					break;

				case PlannedRoll roll:
					Orientation.ApplyRoll(playerState, roll.Direction);
					playerState.ActionPoints -= CombatConfig.RollApCost;
					break;

				case PlannedHeadingTurn headingTurn:
					var turnCost = CombatConfig.HeadingTurnBaseApCost + playerState.MomentumLevel;
					Orientation.ApplyHeadingTurn(playerState, headingTurn.Turn);
					playerState.ActionPoints -= turnCost;
					break;

				case PlannedMissile missile:
					hazards.Add(Hazard.MissileZone(
						missile.Center,
						grid,
						CombatConfig.MissileRadius,
						CombatConfig.MissileDamage,
						CombatConfig.MissileMomentumLoss));
					missilesPlanned++;
					break;

				case PlannedRailgun railgun when railgun.TargetUnitId == enemyState.Id:
					enemyState.Hp = Math.Max(enemyState.Hp - CombatConfig.RailgunDamage, 0);
					railgunPlanned = true;
					break;
			}
		}

		return new SimulatedTurn
		{
			Player = playerState,
			Enemy = enemyState,
			Hazards = hazards,
			MissilesPlanned = missilesPlanned,
			RailgunPlanned = railgunPlanned,
		};
	}
}
