using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

/// <summary>Pilot XP curve and battle-win rewards. Levels are derived from <see cref="Pilot.ExperiencePoints"/> only.</summary>
public static class PilotProgression
{
	public const int CompletionXp = 50;
	public const int KillXpPerKill = 25;

	/// <summary>Cumulative XP required to reach each level (index 0 = level 1).</summary>
	private static readonly int[] LevelThresholds = [0, 100, 250, 450, 700, 1000];

	public static int MaxLevel => LevelThresholds.Length;

	public static int LevelFor(int experiencePoints)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		var level = 1;
		for (var next = 1; next < LevelThresholds.Length; next++)
		{
			if (experiencePoints < LevelThresholds[next])
				return level;
			level++;
		}

		return level;
	}

	public static (int CurrentInLevel, int RequiredForNext) ProgressWithinLevel(int experiencePoints)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		var level = LevelFor(experiencePoints);
		if (level >= LevelThresholds.Length)
			return (0, 0);

		var levelStart = LevelThresholds[level - 1];
		var nextThreshold = LevelThresholds[level];
		return (experiencePoints - levelStart, nextThreshold - levelStart);
	}

	public static Pilot AddExperience(Pilot pilot, int amount)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (amount <= 0)
			return pilot;

		return pilot.WithExperience(pilot.ExperiencePoints + amount);
	}

	public static IReadOnlyDictionary<string, int> RewardsFor(
		IReadOnlyList<BattleSpawn> spawns,
		BattleOutcome outcome)
	{
		ArgumentNullException.ThrowIfNull(spawns);
		ArgumentNullException.ThrowIfNull(outcome);
		if (outcome.Result != EBattleResult.Win)
			return new Dictionary<string, int>();

		var handoffs = BuildHandoffLookup(outcome.StateHandoffs);
		var killCount = CountEnemyKills(spawns, handoffs);
		var xpPerSurvivor = CompletionXp + killCount * KillXpPerKill;
		if (xpPerSurvivor <= 0)
			return new Dictionary<string, int>();

		var grants = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Player || string.IsNullOrEmpty(spawn.PilotId))
				continue;
			if (!handoffs.TryGetValue(spawn.Ship.Id, out var handoff) || handoff.HullPoints <= 0)
				continue;

			grants[spawn.PilotId] = xpPerSurvivor;
		}

		return grants;
	}

	private static Dictionary<string, UnitStateHandoff> BuildHandoffLookup(
		IReadOnlyList<UnitStateHandoff> handoffs)
	{
		var lookup = new Dictionary<string, UnitStateHandoff>(StringComparer.Ordinal);
		foreach (var handoff in handoffs)
			lookup[handoff.Id] = handoff;
		return lookup;
	}

	private static int CountEnemyKills(
		IReadOnlyList<BattleSpawn> spawns,
		Dictionary<string, UnitStateHandoff> handoffs)
	{
		var kills = 0;
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Enemy)
				continue;
			if (!handoffs.TryGetValue(spawn.Ship.Id, out var handoff))
				continue;
			if (handoff.HullPoints <= 0)
				kills++;
		}

		return kills;
	}
}
