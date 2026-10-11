using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
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
	public void RewardsFor_Win_GrantsCompletionAndKillBonusToEachSurvivor()
	{
		const string survivorPilotA = "pilot-a";
		const string survivorPilotB = "pilot-b";
		const string deadPilot = "pilot-dead";
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", survivorPilotA),
			PlayerSpawn("fighter-b", survivorPilotB),
			PlayerSpawn("fighter-c", deadPilot),
			EnemySpawn("enemy-1"),
			EnemySpawn("enemy-2"),
			EnemySpawn("enemy-3"),
		};
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("fighter-b", EType.Fighter, 5),
				OutcomeTestKit.Handoff("fighter-c", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-2", EType.Fighter, 0),
				OutcomeTestKit.Handoff("enemy-3", EType.Fighter, 0),
			]);

		var expectedXp = PilotProgression.CompletionXp + 3 * PilotProgression.KillXpPerKill;
		var grants = PilotProgression.RewardsFor(spawns, outcome);

		Assert.Equal(2, grants.Count);
		Assert.Equal(expectedXp, grants[survivorPilotA]);
		Assert.Equal(expectedXp, grants[survivorPilotB]);
		Assert.False(grants.ContainsKey(deadPilot));
	}

	[Fact]
	public void RewardsFor_Lose_ReturnsEmpty()
	{
		var spawns = new[] { PlayerSpawn("fighter-a", "pilot-a") };
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Lose,
			[OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1)]);

		Assert.Empty(PilotProgression.RewardsFor(spawns, outcome));
	}

	[Fact]
	public void RewardsFor_OnlyCountsEnemySpawnsDestroyedInHandoffs()
	{
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-1"),
			EnemySpawn("enemy-2"),
		};
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.Fighter, 0),
			]);

		var expectedXp = PilotProgression.CompletionXp + PilotProgression.KillXpPerKill;
		Assert.Equal(expectedXp, Assert.Single(PilotProgression.RewardsFor(spawns, outcome)).Value);
	}

	[Fact]
	public void RewardsFor_SkipsPlayerSpawnsWithoutPilotId()
	{
		var spawns = new[] { PlayerSpawn("fighter-a", pilotId: null) };
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Win,
			[OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1)]);

		Assert.Empty(PilotProgression.RewardsFor(spawns, outcome));
	}

	private static BattleSpawn PlayerSpawn(string shipId, string? pilotId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			PilotId = pilotId,
			Team = ETeam.Player,
		};

	private static BattleSpawn EnemySpawn(string shipId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Team = ETeam.Enemy,
		};
}
