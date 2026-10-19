using GrimSpace.Battle.Units;
using GrimSpace.Core.Log;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Encounter;

public static class BattleEngagementComposition
{
	public static void EnsurePlayable(IReadOnlyList<BattleSpawn> spawns, string engagementId)
	{
		ArgumentNullException.ThrowIfNull(spawns);
		CountTeams(
			spawns.Select(spawn => spawn.Team),
			out var playerUnits,
			out var enemyUnits);
		EnsurePlayableCounts(engagementId, playerUnits, enemyUnits, spawns.Count);
	}

	public static void EnsurePlayableUnits(IEnumerable<Unit> units, string battleId)
	{
		ArgumentNullException.ThrowIfNull(units);
		CountTeams(
			units.Select(unit => unit.Team),
			out var playerUnits,
			out var enemyUnits);
		EnsurePlayableCounts(battleId, playerUnits, enemyUnits, playerUnits + enemyUnits);
	}

	private static void CountTeams(
		IEnumerable<ETeam> teams,
		out int playerUnits,
		out int enemyUnits)
	{
		playerUnits = 0;
		enemyUnits = 0;
		foreach (var team in teams)
		{
			if (team == ETeam.Player)
				playerUnits++;
			else if (team == ETeam.Enemy)
				enemyUnits++;
		}
	}

	private static void EnsurePlayableCounts(
		string engagementId,
		int playerUnits,
		int enemyUnits,
		int totalUnits)
	{
		if (playerUnits >= 1 && enemyUnits >= 1)
			return;

		var ex = new InvalidOperationException(
			$"Battle '{engagementId}' needs at least one player and one enemy unit "
			+ $"(player={playerUnits}, enemy={enemyUnits}, total={totalUnits}).");
		GameLog.LogException(ex, "Refusing to start battle with invalid team composition.");
		throw ex;
	}
}
