using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Pilots;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run;

/// <summary>Pilot XP curve and battle-win rewards. Levels are derived from <see cref="Pilot.ExperiencePoints"/> only.</summary>
public static class PilotProgression
{
	public const int DefaultXpDifficulty = 3;

	/// <summary>Provisional grade XP bases before battle multiplier. C never yields zero for survivors.</summary>
	public const int XpGradeBaseC = 25;
	public const int XpGradeBaseB = 35;
	public const int XpGradeBaseA = 45;
	public const int XpGradeBaseS = 55;

	/// <summary>Performance-point bands for S/A/B/C — keep aligned with <c>BattlePerformanceScore.GradeFromPoints</c>.</summary>
	public const int PerformancePointsS = 80;
	public const int PerformancePointsA = 40;
	public const int PerformancePointsB = 10;

	private static readonly EPilotSkillId[] EmptyLearnedSkills = [];

	/// <summary>
	/// Cumulative XP required to reach each level (index 0 = level 1).
	/// Early band is fast (~1–3 A-rank fights); mid/late curve steepens hard.
	/// </summary>
	private static readonly int[] LevelThresholds = [0, 100, 300, 550, 1300, 3000];

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

	public static Pilot GrantMaxExperience(Pilot pilot)
	{
		ArgumentNullException.ThrowIfNull(pilot);
		var maxXp = ExperienceForLevel(MaxLevel);
		return pilot.ExperiencePoints >= maxXp
			? pilot
			: pilot.WithExperience(maxXp);
	}

	public static int CountEnemySpawns(IReadOnlyList<BattleSpawn> spawns) =>
		spawns.Count(spawn => spawn.Team == ETeam.Enemy);

	public static int EnemyXpWeight(EType chassis, EShipGearTier tier)
	{
		var power = ShipPowerCatalog.PowerFor(chassis, tier);
		return power > 0 ? power : UncataloguedEnemyXpWeight(tier);
	}

	public static int SumEnemyXpWeight(IReadOnlyList<BattleSpawn> spawns)
	{
		ArgumentNullException.ThrowIfNull(spawns);
		var weight = 0;
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Enemy)
				continue;

			var chassis = spawn.Ship.Spec.Chassis;
			var tier = ShipLoadoutGearTier.FromLoadout(spawn.Ship.Loadout, chassis);
			weight += EnemyXpWeight(chassis, tier);
		}

		return weight;
	}

	private static int UncataloguedEnemyXpWeight(EShipGearTier tier) => (int)tier + 1;

	public static int BattleXpBase(int enemyXpWeight, int difficulty) =>
		enemyXpWeight * difficulty;

	public static char GradeForPerformancePoints(int performancePoints) =>
		performancePoints switch
		{
			>= PerformancePointsS => 'S',
			>= PerformancePointsA => 'A',
			>= PerformancePointsB => 'B',
			_ => 'C',
		};

	public static int GradeBaseExperience(char grade) =>
		grade switch
		{
			'S' => XpGradeBaseS,
			'A' => XpGradeBaseA,
			'B' => XpGradeBaseB,
			_ => XpGradeBaseC,
		};

	/// <summary>
	/// Grade base × battle multiplier. Multiplier is <paramref name="battleBase"/> /
	/// <see cref="DefaultXpDifficulty"/> so 1 power of enemies at default difficulty pays the grade base.
	/// </summary>
	public static int ExperienceFromGrade(char grade, int battleBase)
	{
		if (battleBase <= 0)
			return 0;

		return (int)System.Math.Round(
			GradeBaseExperience(grade) * (decimal)battleBase / DefaultXpDifficulty,
			System.MidpointRounding.AwayFromZero);
	}

	public static int ExperienceFromPerformance(int battleBase, int performancePoints) =>
		ExperienceFromGrade(GradeForPerformancePoints(performancePoints), battleBase);

	public static IReadOnlyDictionary<string, int> RewardsFor(
		IReadOnlyList<BattleSpawn> spawns,
		BattleOutcome outcome,
		int xpDifficulty,
		Func<string, IReadOnlyList<EPilotSkillId>>? resolveLearnedSkills = null,
		Func<string, EPilotBackgroundId?>? resolveBackgroundId = null)
	{
		ArgumentNullException.ThrowIfNull(spawns);
		ArgumentNullException.ThrowIfNull(outcome);
		if (outcome.Result != EBattleResult.Win)
			return new Dictionary<string, int>();

		var handoffs = BuildHandoffLookup(outcome.StateHandoffs);
		var battleBase = BattleXpBase(SumEnemyXpWeight(spawns), xpDifficulty);
		if (battleBase <= 0)
			return new Dictionary<string, int>();

		var grants = new Dictionary<string, int>(StringComparer.Ordinal);
		foreach (var spawn in spawns)
		{
			if (spawn.Team != ETeam.Player || string.IsNullOrEmpty(spawn.PilotId))
				continue;
			if (!handoffs.TryGetValue(spawn.Ship.Id, out var handoff) || handoff.CurrentHullPoints <= 0)
				continue;

			outcome.PerformancePointsByUnitId.TryGetValue(spawn.Ship.Id, out var performancePoints);
			var xp = ExperienceFromPerformance(battleBase, performancePoints);
			if (xp <= 0)
				continue;

			var learnedSkills = resolveLearnedSkills?.Invoke(spawn.PilotId) ?? EmptyLearnedSkills;
			var backgroundId = resolveBackgroundId?.Invoke(spawn.PilotId);
			grants[spawn.PilotId] = ApplyExperienceGainBonus(xp, learnedSkills, backgroundId);
		}

		return grants;
	}

	public static int ApplyExperienceGainBonus(
		int baseXp,
		IReadOnlyList<EPilotSkillId> learnedSkills,
		EPilotBackgroundId? backgroundId = null) =>
		ApplyExperienceGainBonus(baseXp, ExperienceGainBonusPercent(learnedSkills, backgroundId));

	public static int ApplyExperienceGainBonus(int baseXp, int bonusPercent)
	{
		if (baseXp <= 0 || bonusPercent <= 0)
			return baseXp;

		return ScalePercent(baseXp, 100 + bonusPercent);
	}

	/// <summary>Additive XP growth percents from skill and background <see cref="EPilotCapabilityId.ExperienceGainBonus"/> grants.</summary>
	public static int ExperienceGainBonusPercent(
		IReadOnlyList<EPilotSkillId> learnedSkills,
		EPilotBackgroundId? backgroundId = null)
	{
		ArgumentNullException.ThrowIfNull(learnedSkills);
		var percent = 0;
		foreach (var skillId in learnedSkills)
		{
			foreach (var capabilityId in PilotSkillCatalog.Get(skillId).CapabilityIds)
			{
				if (capabilityId != EPilotCapabilityId.ExperienceGainBonus)
					continue;

				percent += skillId switch
				{
					EPilotSkillId.BetterRam => PilotSkillPercents.BetterRam.ExperienceGainBonusPercent,
					_ => throw new InvalidOperationException(
						$"No experience gain bonus percent configured for skill '{skillId}'."),
				};
			}
		}

		if (backgroundId is not { } id || !PilotBackgroundCatalog.TryGet(id, out var background))
			return percent;

		foreach (var capabilityId in background.CapabilityIds)
		{
			if (capabilityId != EPilotCapabilityId.ExperienceGainBonus)
				continue;

			percent += id switch
			{
				EPilotBackgroundId.CyberJunkie => PilotSkillPercents.CyberJunkie.ExperienceGainBonusPercent,
				_ => throw new InvalidOperationException(
					$"No experience gain bonus percent configured for background '{id}'."),
			};
		}

		return percent;
	}

	public static bool HasExperienceGainBonus(
		IReadOnlyList<EPilotSkillId> learnedSkills,
		EPilotBackgroundId? backgroundId = null) =>
		ExperienceGainBonusPercent(learnedSkills, backgroundId) > 0;

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
