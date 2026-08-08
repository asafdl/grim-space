using GrimSpace.Battle.Units;
using GrimSpace.Battle.World;
using GrimSpace.Run;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Objectives;

public sealed class Manager(EObjective objective)
{
	public BattleOutcome Evaluate(BattleWorld world, string perspectiveUnitId) =>
		objective switch
		{
			EObjective.EliminateOpponents => EliminateOpponents(world, perspectiveUnitId),
			_ => throw new ArgumentOutOfRangeException(nameof(objective), objective, null),
		};

	private static BattleOutcome EliminateOpponents(BattleWorld world, string perspectiveUnitId)
	{
		var units = UnitRegistry.For(world);
		if (!units.TryGet(perspectiveUnitId, out var perspective))
			return BattleOutcome.Ongoing;

		var livingTeams = new HashSet<ETeam>();
		foreach (var unit in units.All)
		{
			if (unit.State.IsAlive)
				livingTeams.Add(unit.Alliance.Team);
		}

		if (livingTeams.Count == 0)
			return BattleOutcome.Tie;

		var alliance = perspective.Alliance;
		var anyFriendly = false;
		var anyOpponent = false;
		foreach (var team in livingTeams)
		{
			if (alliance.IsAlliedWith(team))
				anyFriendly = true;
			else
				anyOpponent = true;

			if (anyFriendly && anyOpponent)
				return BattleOutcome.Ongoing;
		}

		return anyFriendly ? BattleOutcome.Win : BattleOutcome.Lose;
	}
}
