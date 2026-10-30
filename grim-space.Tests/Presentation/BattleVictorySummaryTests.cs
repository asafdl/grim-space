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
	public void ForPlayerUnits_CreditsVoidBombDamageToSpawner()
	{
		var timeline = new ITimelineEntry[]
		{
			new Record<SpawnFacts>(new SpawnFacts(
				"fighter-a",
				"void-bomb-1",
				EType.VoidBomb,
				default)),
			new Record<ImpactFacts>(new ImpactFacts("void-bomb-1", "enemy-1", default, default, 5, 20)),
			new Record<ImpactFacts>(new ImpactFacts("enemy-1", "fighter-a", default, default, 0, 12)),
		};
		var players = new HashSet<string>(StringComparer.Ordinal) { "fighter-a" };

		var totals = BattleUnitPerformance.ForPlayerUnits(timeline, players)["fighter-a"];

		Assert.Equal(25, totals.Dealt);
		Assert.Equal(12, totals.Taken);
		Assert.Equal(0, totals.HealingDone);
	}

	[Fact]
	public void ForPlayerUnits_CreditsAllyHealingToHealer()
	{
		var timeline = new ITimelineEntry[]
		{
			new Record<HealFacts>(new HealFacts(
				"fighter-a",
				"fighter-b",
				default,
				default,
				10,
				5)),
		};
		var players = new HashSet<string>(StringComparer.Ordinal) { "fighter-a", "fighter-b" };

		var totals = BattleUnitPerformance.ForPlayerUnits(timeline, players);

		Assert.Equal(15, totals["fighter-a"].HealingDone);
		Assert.Equal(0, totals["fighter-b"].HealingDone);
	}

	[Fact]
	public void Build_ListsPilotsWithDamageXpAndScore()
	{
		var spawns = new[]
		{
			PlayerSpawn("fighter-a", "pilot-a"),
			PlayerSpawn("fighter-b", "pilot-b"),
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
				["fighter-a"] = 100,
				["fighter-b"] = 0,
			});
		var timeline = new ITimelineEntry[]
		{
			new Record<ImpactFacts>(new ImpactFacts("fighter-a", "enemy-1", default, default, 0, 50)),
			new Record<ImpactFacts>(new ImpactFacts("enemy-1", "fighter-b", default, default, 0, 30)),
		};

		var summary = BattleVictorySummaryBuilder.Build(
			outcome,
			spawns,
			PilotProgression.DefaultXpDifficulty,
			timeline,
			_ => null);

		Assert.Equal(2, summary.Pilots.Count);
		var top = summary.Pilots[0];
		Assert.Equal("pilot-a", top.DisplayName);
		Assert.Equal(50, top.DamageDealt);
		Assert.Equal(0, top.DamageTaken);
		Assert.Equal(
			PilotProgression.ExperienceFromPerformance(
				PilotProgression.BattleXpBase(1, PilotProgression.DefaultXpDifficulty),
				100),
			top.ExperienceEarned);
		Assert.Equal(0, top.ExperiencePointsBefore);
		Assert.Equal(top.ExperienceEarned, top.ExperiencePointsAfter);
		Assert.Equal('A', top.ScoreGrade);

		var second = summary.Pilots[1];
		Assert.Equal(0, second.DamageDealt);
		Assert.Equal(30, second.DamageTaken);
		Assert.Equal(0, second.ExperienceEarned);
	}

	private static BattleOutcome WinOutcome(
		IReadOnlyList<UnitStateHandoff> handoffs,
		IReadOnlyDictionary<string, int> performanceByUnitId) =>
		new("battle-1", EBattleResult.Win, handoffs, performanceByUnitId);

	private static BattleSpawn PlayerSpawn(string shipId, string pilotId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Pilot = new PilotBattleSnapshot(pilotId, []),
			Team = ETeam.Player,
		};

	private static BattleSpawn EnemySpawn(string shipId) =>
		new()
		{
			Ship = ShipInstance.FromCatalog(shipId, EType.Fighter),
			Team = ETeam.Enemy,
		};
}
