using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Pilots;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

/// <summary>Pilot XP curve and battle-win rewards. Levels are derived from <see cref="Pilot.ExperiencePoints"/> only.</summary>
public static class PilotProgression
{
	public const int DefaultXpDifficulty = 3;
	/// <summary>XP at S-rank (performance == <see cref="PerformanceScoreDivisor"/>) equals battleBase × this.</summary>
	public const int XpPerBattleBasePoint = 15;
	/// <summary>Performance score that yields full battle XP (matches S-grade threshold).</summary>
	public const int PerformanceScoreDivisor = 80;

	private static readonly EPilotSkillId[] EmptyLearnedSkills = [];

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

	public static int ExperienceForLevel(int level)
	{
		ArgumentOutOfRangeException.ThrowIfLessThan(level, 1);
		ArgumentOutOfRangeException.ThrowIfGreaterThan(level, MaxLevel);
		return LevelThresholds[level - 1];
	}

	public static bool TryGetNextLevelThreshold(int experiencePoints, out int thresholdExperience)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(experiencePoints);
		var level = LevelFor(experiencePoints);
		if (level >= MaxLevel)
		{
			thresholdExperience = 0;
			return false;
		}

		thresholdExperience = LevelThresholds[level];
		return true;
	}

	public static Pilot AddExperience(Pilot pilot, int amount)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		if (amount <= 0)
			return pilot;

		return pilot.WithExperience(pilot.ExperiencePoints + amount);
	}

	public static int CountEnemySpawns(IReadOnlyList<BattleSpawn> spawns) =>
		spawns.Count(spawn => spawn.Team == ETeam.Enemy);

	public static int BattleXpBase(int enemyCount, int difficulty) =>
		enemyCount * difficulty;

	public static int ExperienceFromPerformance(int battleBase, int performancePoints)
	{
		if (battleBase <= 0 || performancePoints <= 0)
			return 0;

		// Cap at S-rank so overkill damage cannot multiply XP past a full clear.
		var capped = System.Math.Min(performancePoints, PerformanceScoreDivisor);
		return (int)System.Math.Round(
			battleBase * XpPerBattleBasePoint * (decimal)capped / PerformanceScoreDivisor,
			System.MidpointRounding.AwayFromZero);
	}

	public static IReadOnlyDictionary<string, int> RewardsFor(
		IReadOnlyList<BattleSpawn> spawns,
		BattleOutcome outcome,
		int xpDifficulty,
		Func<string, IReadOnlyList<EPilotSkillId>>? resolveLearnedSkills = null)
	{
		ArgumentNullException.ThrowIfNull(spawns);
		ArgumentNullException.ThrowIfNull(outcome);
		if (outcome.Result != EBattleResult.Win)
			return new Dictionary<string, int>();

		var handoffs = BuildHandoffLookup(outcome.StateHandoffs);
		var battleBase = BattleXpBase(CountEnemySpawns(spawns), xpDifficulty);
		if (battleBase <= 0)
			return new Dictionary<string, int>();

		var grants = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Player || string.IsNullOrEmpty(spawn.PilotId))
				continue;
			if (!handoffs.TryGetValue(spawn.Ship.Id, out var handoff) || handoff.HullPoints <= 0)
				continue;

			outcome.PerformancePointsByUnitId.TryGetValue(spawn.Ship.Id, out var performancePoints);
			var xp = ExperienceFromPerformance(battleBase, performancePoints);
			if (xp <= 0)
				continue;

			var learnedSkills = resolveLearnedSkills?.Invoke(spawn.PilotId) ?? EmptyLearnedSkills;
			grants[spawn.PilotId] = ApplyExperienceGainBonus(xp, learnedSkills);
		}

		return grants;
	}

	public static int ApplyExperienceGainBonus(
		int baseXp,
		IReadOnlyList<EPilotSkillId> learnedSkills)
	{
		if (baseXp <= 0 || !HasExperienceGainBonus(learnedSkills))
			return baseXp;

		return ScalePercent(
			baseXp,
			100 + PilotSkillPercents.BetterRam.ExperienceGainBonusPercent);
	}

	public static bool HasExperienceGainBonus(IReadOnlyList<EPilotSkillId> learnedSkills)
	{
		ArgumentNullException.ThrowIfNull(learnedSkills);
		foreach (var skillId in learnedSkills)
		{
			foreach (var capabilityId in PilotSkillCatalog.Get(skillId).CapabilityIds)
			{
				if (capabilityId == EPilotCapabilityId.ExperienceGainBonus)
					return true;
			}
		}

		return false;
	}

	private static Dictionary<string, UnitStateHandoff> BuildHandoffLookup(
		IReadOnlyList<UnitStateHandoff> handoffs)
	{
		var lookup = new Dictionary<string, UnitStateHandoff>(StringComparer.Ordinal);
		foreach (var handoff in handoffs)
			lookup[handoff.Id] = handoff;
		return lookup;
	}

	private static int ScalePercent(int amount, int percent) =>
		(int)System.Math.Round(amount * percent / 100m, System.MidpointRounding.AwayFromZero);
}
