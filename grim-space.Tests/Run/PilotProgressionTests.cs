using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Tests;
using GrimSpace.Units;
using GrimSpace.Units.Enums;
using GrimSpace.World.Factions;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotProgressionTests
{
	[Theory]
	[InlineData(0, 1)]
	[InlineData(99, 1)]
	[InlineData(100, 2)]
	[InlineData(299, 2)]
	[InlineData(300, 3)]
	[InlineData(549, 3)]
	[InlineData(550, 4)]
	[InlineData(1299, 4)]
	[InlineData(1300, 5)]
	[InlineData(2999, 5)]
	[InlineData(3000, 6)]
	public void LevelFor_MatchesThresholdTable(int experiencePoints, int expectedLevel)
	{
		Assert.Equal(expectedLevel, PilotProgression.LevelFor(experiencePoints));
	}

	[Fact]
	public void LevelFor_IsMonotonicAsExperienceIncreases()
	{
		var previous = PilotProgression.LevelFor(0);
		var maxXp = PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel);
		for (var xp = 1; xp <= maxXp; xp++)
		{
			var level = PilotProgression.LevelFor(xp);
			Assert.True(level >= previous);
			previous = level;
		}
	}

	[Fact]
	public void AddExperience_CrossesLevelThresholds()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		Assert.Equal(1, PilotProgression.LevelFor(pilot.ExperiencePoints));

		pilot = PilotProgression.AddExperience(pilot, 100);
		Assert.Equal(100, pilot.ExperiencePoints);
		Assert.Equal(2, PilotProgression.LevelFor(pilot.ExperiencePoints));

		pilot = PilotProgression.AddExperience(pilot, 200);
		Assert.Equal(300, pilot.ExperiencePoints);
		Assert.Equal(3, PilotProgression.LevelFor(pilot.ExperiencePoints));
	}

	[Fact]
	public void GrantMaxExperience_SetsMaxLevelThreshold()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		var maxed = PilotProgression.GrantMaxExperience(pilot);

		Assert.Equal(PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel), maxed.ExperiencePoints);
		Assert.Equal(PilotProgression.MaxLevel, PilotProgression.LevelFor(maxed.ExperiencePoints));
		Assert.Same(maxed, PilotProgression.GrantMaxExperience(maxed));
	}

	[Fact]
	public void AddExperience_IgnoresNonPositiveAmount()
	{
		var pilot = Pilot.CreateForShip("fighter-a");
		Assert.Same(pilot, PilotProgression.AddExperience(pilot, 0));
	}

	[Fact]
	public void ProgressWithinLevel_ReportsBarFractions()
	{
		Assert.Equal((50, 100), PilotProgression.ProgressWithinLevel(50));
		Assert.Equal((0, 200), PilotProgression.ProgressWithinLevel(100));
		Assert.Equal((0, 0), PilotProgression.ProgressWithinLevel(PilotProgression.ExperienceForLevel(PilotProgression.MaxLevel)));
	}

	[Theory]
	[InlineData(0, 'C', PilotProgression.XpGradeBaseC)]
	[InlineData(9, 'C', PilotProgression.XpGradeBaseC)]
	[InlineData(10, 'B', PilotProgression.XpGradeBaseB)]
	[InlineData(40, 'A', PilotProgression.XpGradeBaseA)]
	[InlineData(80, 'S', PilotProgression.XpGradeBaseS)]
	[InlineData(240, 'S', PilotProgression.XpGradeBaseS)]
	public void ExperienceFromPerformance_UsesGradeBaseTimesBattleMultiplier(
		int performancePoints,
		char expectedGrade,
		int expectedGradeBase)
	{
		Assert.Equal(expectedGrade, PilotProgression.GradeForPerformancePoints(performancePoints));
		var battleBase = PilotProgression.BattleXpBase(enemyXpWeight: 1, difficulty: PilotProgression.DefaultXpDifficulty);
		Assert.Equal(expectedGradeBase, PilotProgression.ExperienceFromPerformance(battleBase, performancePoints));
	}

	[Fact]
	public void ExperienceFromGrade_ScalesWithEnemyCountAndDifficulty()
	{
		var oneEnemy = PilotProgression.BattleXpBase(enemyXpWeight: 1, difficulty: 3);
		var threeEnemies = PilotProgression.BattleXpBase(enemyXpWeight: 3, difficulty: 3);
		Assert.Equal(
			PilotProgression.XpGradeBaseC,
			PilotProgression.ExperienceFromGrade('C', oneEnemy));
		Assert.Equal(
			PilotProgression.XpGradeBaseC * 3,
			PilotProgression.ExperienceFromGrade('C', threeEnemies));
	}

	[Fact]
	public void ExperienceFromPerformance_CGradeNeverZeroWhenBattleBasePositive()
	{
		var battleBase = PilotProgression.BattleXpBase(enemyXpWeight: 2, difficulty: 3);
		var xp = PilotProgression.ExperienceFromPerformance(battleBase, performancePoints: 0);
		Assert.True(xp > 0);
		Assert.Equal(
			PilotProgression.ExperienceFromGrade('C', battleBase),
			xp);
	}

	[Fact]
	public void ExperienceFromPerformance_TypicalT0Fight_DoesNotMultiLevelFromZero()
	{
		// 3 enemies, S-rank clear: grade base × enemy count at default difficulty.
		var battleBase = PilotProgression.BattleXpBase(enemyXpWeight: 3, difficulty: 3);
		var xp = PilotProgression.ExperienceFromPerformance(
			battleBase,
			PilotProgression.PerformancePointsS);
		Assert.Equal(PilotProgression.XpGradeBaseS * 3, xp);
		Assert.Equal(2, PilotProgression.LevelFor(xp));
	}

	[Fact]
	public void RewardsFor_Win_ScalesXpByPerformanceForEachSurvivor()
	{
		const string survivorPilotA = "pilot-a";
		const string survivorPilotB = "pilot-b";
		const string deadPilot = "pilot-dead";
		const int xpDifficulty = 3;
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", survivorPilotA),
			PlayerSpawn("fighter-b", survivorPilotB),
			PlayerSpawn("fighter-c", deadPilot),
			EnemySpawn("enemy-1"),
			EnemySpawn("enemy-2"),
			EnemySpawn("enemy-3"),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("fighter-b", EType.Fighter, 5),
				OutcomeTestKit.Handoff("fighter-c", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-2", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-3", EType.Fighter, 0),
			],
			new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["fighter-a"] = 80,
				["fighter-b"] = 40,
			});
		var battleBase = PilotProgression.BattleXpBase(
			PilotProgression.SumEnemyXpWeight(spawns),
			xpDifficulty);
		var expectedA = PilotProgression.ExperienceFromPerformance(battleBase, 80);
		var expectedB = PilotProgression.ExperienceFromPerformance(battleBase, 40);

		var grants = PilotProgression.RewardsFor(spawns, outcome, xpDifficulty);

		Assert.Equal(2, grants.Count);
		Assert.Equal(expectedA, grants[survivorPilotA]);
		Assert.Equal(expectedB, grants[survivorPilotB]);
		Assert.False(grants.ContainsKey(deadPilot));
	}

	[Fact]
	public void RewardsFor_AppliesBetterRamBonusPerPilot()
	{
		const string boostedPilot = "pilot-boosted";
		const string baselinePilot = "pilot-baseline";
		const int xpDifficulty = 3;
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", boostedPilot),
			PlayerSpawn("fighter-b", baselinePilot),
			EnemySpawn("enemy-1"),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("fighter-b", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
			],
			new Dictionary<string, int>(StringComparer.Ordinal)
			{
				["fighter-a"] = 40,
				["fighter-b"] = 40,
			});
		var baseXp = PilotProgression.ExperienceFromPerformance(
			PilotProgression.BattleXpBase(PilotProgression.SumEnemyXpWeight(spawns), xpDifficulty),
			40);
		var grants = PilotProgression.RewardsFor(
			spawns,
			outcome,
			xpDifficulty,
			pilotId => pilotId switch
			{
				boostedPilot => new[] { EPilotSkillId.BetterRam },
				_ => Array.Empty<EPilotSkillId>(),
			});

		Assert.Equal(
			PilotProgression.ApplyExperienceGainBonus(baseXp, [EPilotSkillId.BetterRam]),
			grants[boostedPilot]);
		Assert.Equal(baseXp, grants[baselinePilot]);
	}

	[Fact]
	public void ApplyExperienceGainBonus_RoundsAwayFromZero()
	{
		Assert.Equal(12, PilotProgression.ApplyExperienceGainBonus(10, [EPilotSkillId.BetterRam]));
	}

	[Fact]
	public void ExperienceGainBonusPercent_StacksBetterRamAndCyberJunkieAdditively()
	{
		Assert.Equal(
			PilotSkillPercents.BetterRam.ExperienceGainBonusPercent
			+ PilotSkillPercents.CyberJunkie.ExperienceGainBonusPercent,
			PilotProgression.ExperienceGainBonusPercent(
				[EPilotSkillId.BetterRam],
				EPilotBackgroundId.CyberJunkie));
		Assert.Equal(
			13,
			PilotProgression.ApplyExperienceGainBonus(10, [EPilotSkillId.BetterRam], EPilotBackgroundId.CyberJunkie));
	}

	[Fact]
	public void RewardsFor_AppliesCyberJunkieXpBonus()
	{
		const string pilotId = "pilot-cyber";
		const int xpDifficulty = 3;
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", pilotId),
			EnemySpawn("enemy-1"),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
			],
			new Dictionary<string, int>(StringComparer.Ordinal) { ["fighter-a"] = 40 });
		var baseXp = PilotProgression.ExperienceFromPerformance(
			PilotProgression.BattleXpBase(PilotProgression.SumEnemyXpWeight(spawns), xpDifficulty),
			40);

		var grants = PilotProgression.RewardsFor(
			spawns,
			outcome,
			xpDifficulty,
			_ => Array.Empty<EPilotSkillId>(),
			_ => EPilotBackgroundId.CyberJunkie);

		Assert.Equal(
			PilotProgression.ApplyExperienceGainBonus(baseXp, [], EPilotBackgroundId.CyberJunkie),
			grants[pilotId]);
	}

	[Fact]
	public void RewardsFor_Lose_ReturnsEmpty()
	{
		var spawns = new[] { PlayerSpawn("fighter-a", "pilot-a") };
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Lose,
			[OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1)]);

		Assert.Empty(PilotProgression.RewardsFor(spawns, outcome, PilotProgression.DefaultXpDifficulty));
	}

	[Fact]
	public void RewardsFor_UsesEnemySpawnWeightNotKillCount()
	{
		const int xpDifficulty = 3;
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-1"),
			EnemySpawn("enemy-2"),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
			],
			new Dictionary<string, int>(StringComparer.Ordinal) { ["fighter-a"] = 40 });
		var expectedXp = PilotProgression.ExperienceFromPerformance(
			PilotProgression.BattleXpBase(PilotProgression.SumEnemyXpWeight(spawns), xpDifficulty),
			40);

		Assert.Equal(expectedXp, Assert.Single(PilotProgression.RewardsFor(spawns, outcome, xpDifficulty)).Value);
		Assert.Equal(2, PilotProgression.SumEnemyXpWeight(spawns));
	}

	[Fact]
	public void RewardsFor_UncataloguedFighter_FallsBackToTierPlusOne()
	{
		const int xpDifficulty = 3;
		var t0Spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-t0", EShipGearTier.T0),
		};
		var t2Spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-t2", EShipGearTier.T2),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-t0", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-t2", EType.Fighter, 0),
			],
			new Dictionary<string, int>(StringComparer.Ordinal) { ["fighter-a"] = 40 });

		var t0Xp = Assert.Single(PilotProgression.RewardsFor(t0Spawns, outcome, xpDifficulty)).Value;
		var t2Xp = Assert.Single(PilotProgression.RewardsFor(t2Spawns, outcome, xpDifficulty)).Value;

		Assert.Equal(0, ShipPowerCatalog.PowerFor(EType.Fighter, EShipGearTier.T0));
		Assert.Equal(PilotProgression.EnemyXpWeight(EType.Fighter, EShipGearTier.T0), PilotProgression.SumEnemyXpWeight(t0Spawns));
		Assert.Equal(PilotProgression.EnemyXpWeight(EType.Fighter, EShipGearTier.T2), PilotProgression.SumEnemyXpWeight(t2Spawns));
		Assert.Equal(t0Xp * 3, t2Xp);
	}

	[Fact]
	public void RewardsFor_GooperT0_PaysTripleMinerT0()
	{
		const int xpDifficulty = 3;
		var performance = new Dictionary<string, int>(StringComparer.Ordinal) { ["fighter-a"] = 40 };
		var minerXp = ExperienceAgainst(
			EnemySpawn("miner-t0", chassis: EType.RepurposedMiner),
			performance,
			xpDifficulty);
		var gooperXp = ExperienceAgainst(
			EnemySpawn("gooper-t0", chassis: EType.IndustrialGooper),
			performance,
			xpDifficulty);

		Assert.Equal(minerXp * 3, gooperXp);
	}

	[Fact]
	public void RewardsFor_MinerT1_MatchesTwoMinerT0()
	{
		const int xpDifficulty = 3;
		var performance = new Dictionary<string, int>(StringComparer.Ordinal) { ["fighter-a"] = 40 };
		var t1Xp = ExperienceAgainst(
			EnemySpawn("miner-t1", EShipGearTier.T1, EType.RepurposedMiner),
			performance,
			xpDifficulty);
		var twoT0 = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("miner-a", chassis: EType.RepurposedMiner),
			EnemySpawn("miner-b", chassis: EType.RepurposedMiner),
		};
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("miner-a", EType.RepurposedMiner, 0),
				OutcomeTestKit.Handoff("miner-b", EType.RepurposedMiner, 0),
			],
			performance);

		Assert.Equal(
			Assert.Single(PilotProgression.RewardsFor(twoT0, outcome, xpDifficulty)).Value,
			t1Xp);
	}

	[Fact]
	public void SumEnemyXpWeight_UncataloguedChassis_IsNonZero()
	{
		var spawns = new[] { EnemySpawn("fighter-enemy") };

		var weight = PilotProgression.SumEnemyXpWeight(spawns);

		Assert.True(weight > 0);
		Assert.Equal(PilotProgression.EnemyXpWeight(EType.Fighter, EShipGearTier.T0), weight);
	}

	[Fact]
	public void RewardsFor_SkipsPlayerSpawnsWithoutPilotId()
	{
		var spawns = new[] { PlayerSpawn("fighter-a", pilotId: null) };
		var outcome = WinOutcome([OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1)]);

		Assert.Empty(PilotProgression.RewardsFor(spawns, outcome, PilotProgression.DefaultXpDifficulty));
	}

	private static BattleOutcome WinOutcome(
		IReadOnlyList<UnitStateHandoff> handoffs,
		IReadOnlyDictionary<string, int>? performanceByUnitId = null) =>
		new(
			"battle-1",
			EBattleResult.Win,
			handoffs,
			performanceByUnitId ?? BattleOutcome.EmptyPerformancePoints);

	private static BattleSpawn PlayerSpawn(string shipId, string? pilotId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Pilot = pilotId is null ? null : new PilotBattleSnapshot(pilotId, []),
			Team = ETeam.Player,
			Faction = EFaction.Player,
		};

	private static int ExperienceAgainst(
		BattleSpawn enemy,
		IReadOnlyDictionary<string, int> performance,
		int xpDifficulty)
	{
		var spawns = new[] { PlayerSpawn("fighter-a", "pilot-a"), enemy };
		var outcome = WinOutcome(
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff(enemy.Ship.Id, enemy.Ship.Spec.Chassis, 0),
			],
			performance);
		return Assert.Single(PilotProgression.RewardsFor(spawns, outcome, xpDifficulty)).Value;
	}

	private static BattleSpawn EnemySpawn(
		string shipId,
		EShipGearTier gearTier = EShipGearTier.T0,
		EType chassis = EType.Fighter) =>
		new()
		{
			Ship = ShipInstance.FromSpec(
				shipId,
				ShipCatalog.SpecFor(chassis),
				ShipCatalog.LoadoutForTier(chassis, gearTier)),
			Team = ETeam.Enemy,
			Faction = EFaction.TheOptimality,
		};
}
