using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Encounter;
using GrimSpace.Battle.Objectives;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Core.Actions;
using GrimSpace.Run;
using GrimSpace.Tests;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[IntegrationTestSuite]
public sealed class BattleVictorySummaryTests
{
	[Fact]
	public void LastPlayerDamagerByTarget_UsesFinalPlayerImpactPerEnemy()
	{
		var timeline = new ITimelineEntry[]
		{
			new Record<ImpactFacts>(new ImpactFacts("ally-b", "enemy-1", default, default, 0, 5)),
			new Record<ImpactFacts>(new ImpactFacts("ally-a", "enemy-1", default, default, 0, 10)),
		};
		var players = new HashSet<string>(StringComparer.Ordinal) { "ally-a", "ally-b" };

		var killers = BattleKillAttribution.LastPlayerDamagerByTarget(timeline, players);

		Assert.Equal("ally-a", killers["enemy-1"]);
	}

	[Fact]
	public void LastPlayerDamagerByTarget_CreditsVoidBombKillToSpawner()
	{
		var timeline = new ITimelineEntry[]
		{
			new Record<SpawnFacts>(new SpawnFacts(
				"fighter-a",
				"void-bomb-1",
				EType.VoidBomb,
				default)),
			new Record<ImpactFacts>(new ImpactFacts("void-bomb-1", "enemy-1", default, default, 0, 20)),
		};
		var players = new HashSet<string>(StringComparer.Ordinal) { "fighter-a" };

		var killers = BattleKillAttribution.LastPlayerDamagerByTarget(timeline, players);

		Assert.Equal("fighter-a", killers["enemy-1"]);
	}

	[Fact]
	public void Build_ListsDestroyedEnemiesWithXpAndCompletionRow()
	{
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-1", EType.RepurposedMiner),
			EnemySpawn("enemy-2", EType.RepurposedMiner),
		};
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.RepurposedMiner, 0),
				OutcomeTestKit.Handoff("enemy-2", EType.RepurposedMiner, 0),
			]);

		var summary = BattleVictorySummaryBuilder.Build(outcome, spawns, []);

		Assert.Equal(2, summary.DamageRows.Count);
		Assert.All(
			summary.DamageRows,
			row => Assert.Equal(PilotProgression.KillXpPerKill, row.ExperienceGained));
		Assert.Equal(PilotProgression.CompletionXp, summary.CompletionRow?.ExperienceGained);
		Assert.Equal(
			"Repurposed Miner (T0)",
			summary.DamageRows[0].TargetLabel);
	}

	[Fact]
	public void Build_BundlesCompletionXpOntoSingleEnemyRow()
	{
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			EnemySpawn("enemy-1", EType.RepurposedMiner),
		};
		var outcome = new BattleOutcome(
			"battle-1",
			EBattleResult.Win,
			[
				OutcomeTestKit.Handoff("fighter-a", EType.Fighter, 1),
				OutcomeTestKit.Handoff("enemy-1", EType.RepurposedMiner, 0),
			]);

		var summary = BattleVictorySummaryBuilder.Build(outcome, spawns, []);

		var row = Assert.Single(summary.DamageRows);
		Assert.Null(summary.CompletionRow);
		Assert.Equal(
			PilotProgression.CompletionXp + PilotProgression.KillXpPerKill,
			row.ExperienceGained);
	}

	private static BattleSpawn PlayerSpawn(string shipId, string pilotId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Pilot = new PilotBattleSnapshot(pilotId, []),
			Team = ETeam.Player,
		};

	private static BattleSpawn EnemySpawn(string shipId, EType chassis) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, chassis),
			Team = ETeam.Enemy,
		};
}
