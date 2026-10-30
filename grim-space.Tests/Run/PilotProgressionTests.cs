using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Pilots;
using GrimSpace.Run;
using GrimSpace.Tests;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Run;

[IntegrationTestSuite]
public sealed class PilotProgressionTests
{
	[Theory]
	[InlineData(0, 1)]
	[InlineData(99, 1)]
	[InlineData(100, 2)]
	[InlineData(249, 2)]
	[InlineData(250, 3)]
	[InlineData(1000, 6)]
	public void LevelFor_MatchesThresholdTable(int experiencePoints, int expectedLevel)
	{
		Assert.Equal(expectedLevel, PilotProgression.LevelFor(experiencePoints));
	}

	[Fact]
	public void LevelFor_IsMonotonicAsExperienceIncreases()
	{
		var previous = PilotProgression.LevelFor(0);
		for (var xp = 1; xp <= 1200; xp++)
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

		pilot = PilotProgression.AddExperience(pilot, 150);
		Assert.Equal(250, pilot.ExperiencePoints);
		Assert.Equal(3, PilotProgression.LevelFor(pilot.ExperiencePoints));
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
		Assert.Equal((0, 150), PilotProgression.ProgressWithinLevel(100));
		Assert.Equal((0, 0), PilotProgression.ProgressWithinLevel(1000));
	}

	[Fact]
	public void ExperienceFromPerformance_ScalesByBattleBaseAndScore()
	{
		var battleBase = PilotProgression.BattleXpBase(enemyCount: 2, difficulty: 3);
		Assert.Equal(45, PilotProgression.ExperienceFromPerformance(battleBase, performancePoints: 40));
		Assert.Equal(90, PilotProgression.ExperienceFromPerformance(battleBase, performancePoints: 80));
	}

	[Fact]
	public void ExperienceFromPerformance_CapsAtSRankScore()
	{
		var battleBase = PilotProgression.BattleXpBase(enemyCount: 2, difficulty: 3);
		var atS = PilotProgression.ExperienceFromPerformance(battleBase, PilotProgression.PerformanceScoreDivisor);
		var aboveS = PilotProgression.ExperienceFromPerformance(
			battleBase,
			PilotProgression.PerformanceScoreDivisor * 3);
		Assert.Equal(atS, aboveS);
	}

	[Fact]
	public void ExperienceFromPerformance_TypicalT0Fight_DoesNotMultiLevelFromZero()
	{
		// 3 enemies, S-rank clear: should land around one early level, not 4+.
		var battleBase = PilotProgression.BattleXpBase(enemyCount: 3, difficulty: 3);
		var xp = PilotProgression.ExperienceFromPerformance(
			battleBase,
			PilotProgression.PerformanceScoreDivisor);
		Assert.Equal(135, xp);
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
		var battleBase = PilotProgression.BattleXpBase(3, xpDifficulty);
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
			PilotProgression.BattleXpBase(1, xpDifficulty),
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
	public void RewardsFor_UsesEnemySpawnCountNotKillCount()
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
			PilotProgression.BattleXpBase(2, xpDifficulty),
			40);

		Assert.Equal(expectedXp, Assert.Single(PilotProgression.RewardsFor(spawns, outcome, xpDifficulty)).Value);
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
		};

	private static BattleSpawn EnemySpawn(string shipId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Team = ETeam.Enemy,
		};
}
