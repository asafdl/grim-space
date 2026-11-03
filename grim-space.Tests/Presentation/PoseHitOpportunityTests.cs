using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Units;
using GrimSpace.Components;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Presentation;

[BattleTestSuite]
public sealed class PoseHitOpportunityTests
{
	private const string PlayerId = "player";

	[Fact]
	public void LightningCannonHitFromPoseIncludesEnemyAndIcon()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var preview = new PlanningPreview();
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var selected = RouteAt(
			BattleTestCommands.MoveOptions(battle),
			origin,
			Coord.Forward);

		var opportunities = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			selected);

		var lightningCannon = Assert.Single(
			opportunities,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/lightning_cannon.svg");
		Assert.Equal(enemyId, lightningCannon.TargetId);
	}

	[Fact]
	public void PoseHitOpportunitiesDoNotMutatePlanningSim()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var preview = new PlanningPreview();
		var sim = battle.PlayerAgent.Sim;
		var selected = RouteAt(BattleTestCommands.MoveOptions(battle), origin, Coord.Forward);
		var worldVersion = sim.WorldVersion;
		var actionCount = sim.Actions.Count;
		var queuedActions = sim.Actions.ToArray();
		var canUndo = battle.PlayerAgent.CanUndo;

		preview.PoseHitOpportunities(sim, PlayerId, selected);

		Assert.Equal(worldVersion, sim.WorldVersion);
		Assert.Equal(actionCount, sim.Actions.Count);
		Assert.Equal(queuedActions, sim.Actions);
		Assert.Equal(canUndo, battle.PlayerAgent.CanUndo);
	}

	[Fact]
	public void OffAxisEnemyProducesNoOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var preview = new PlanningPreview();
		var selected = RouteAt(BattleTestCommands.MoveOptions(battle), origin, Coord.Forward);

		var opportunities = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			selected);

		Assert.Empty(opportunities);
	}

	[Fact]
	public void GroupedScrapDroneSwarmMountsEmitOneEntryPerTarget()
	{
		var origin = new Coord(5, 5, 5);
		var probe = BattleTestFixture.BeginScrapSimulation(origin);
		var swarmBurstCell = ScrapDroneSwarmDef.Instance.AffectedCells(
			new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard),
			probe.PlayerAgent.Sim.World).First();
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.ScrapPlayer(origin),
			BattleTestFixture.Enemy(swarmBurstCell),
			BattleTestFixture.Grid(20));
		var sim = battle.PlayerAgent.Sim;
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var route = new RouteHitPreview(BattleTestFixture.ForwardPath(PlayerId, origin, steps: 0));

		var opportunities = route.GetHitOpportunities(sim, PlayerId);

		Assert.Equal(
			1,
			opportunities.Count(opportunity =>
				opportunity.IconPath == "res://assets/ui/abilities/scrap_drone_swarm.svg"
				&& opportunity.TargetId == enemyId));
	}

	[Fact]
	public void SpentLightningCannonIsExcludedFromOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var preview = new PlanningPreview();
		Assert.True(BattleTestCommands.FireLightningCannon(battle));
		var selected = RouteAt(BattleTestCommands.MoveOptions(battle), origin, Coord.Forward);

		var opportunities = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			selected);

		Assert.DoesNotContain(
			opportunities,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/lightning_cannon.svg");
	}

	[Theory]
	[InlineData(false, true)]
	[InlineData(true, true)]
	[InlineData(false, false)]
	public void ScrapDroneSwarmUsesHealingHeartForRestoredAlliesAndAttackIconForEnemies(
		bool shieldDamage,
		bool injured)
	{
		var origin = new Coord(5, 5, 5);
		var probe = BattleTestFixture.BeginScrapSimulation(origin);
		var burstCells = ScrapDroneSwarmDef.Instance.AffectedCells(
			new ScrapDroneSwarmAction(PlayerId, ESpatialOrientation.Starboard),
			probe.PlayerAgent.Sim.World).Where(cell => cell != origin).Take(2).ToArray();
		var ally = Factory.Create(
			ShipInstance.FromCatalog("ally", EType.Fighter),
			ETeam.Player,
			burstCells[0],
			new UserExecutionAgent());
		if (injured)
		{
			if (shieldDamage)
				ally.State.CurrentShieldPoints[ESpatialOrientation.Forward] -= 100;
			else
				ally.State.CurrentHullPoints -= 100;
		}
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.ScrapPlayer(origin),
			[ally, BattleTestFixture.Enemy(burstCells[1])]);
		var sim = battle.PlayerAgent.Sim;
		var before = sim.World.StateOf(ally.State.Id).Clone();
		var worldVersion = sim.WorldVersion;
		var route = new RouteHitPreview(BattleTestFixture.ForwardPath(PlayerId, origin, steps: 0));

		var opportunities = route.GetHitOpportunities(sim, PlayerId);

		var enemy = Assert.Single(opportunities,
			opportunity => opportunity.TargetId == BattleTestFixture.FirstEnemyId(battle));
		Assert.Equal("res://assets/ui/abilities/scrap_drone_swarm.svg", enemy.IconPath);
		Assert.True(enemy.TintIcon);
		if (injured)
		{
			var heart = Assert.Single(opportunities, opportunity => opportunity.TargetId == ally.State.Id);
			var healIcon = HudTooltipIconCatalog.Resolve("Heal");
			Assert.Equal("res://assets/ui/battle/heal.png", heart.IconPath);
			Assert.Equal(healIcon.Color, heart.IconTint);
			Assert.False(heart.TintIcon);
		}
		else
		{
			Assert.DoesNotContain(opportunities, opportunity => opportunity.TargetId == ally.State.Id);
		}
		Assert.Equal(worldVersion, sim.WorldVersion);
		Assert.Empty(sim.Actions);
		Assert.Equal(before.CurrentHullPoints, sim.World.StateOf(ally.State.Id).CurrentHullPoints);
		Assert.Equal(before.CurrentShieldPoints[ESpatialOrientation.Forward],
			sim.World.StateOf(ally.State.Id).CurrentShieldPoints[ESpatialOrientation.Forward]);
	}

	[Fact]
	public void HeadingChangeAffectsLightningCannonLine()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInLightningCannonLine(origin));
		var preview = new PlanningPreview();
		var options = BattleTestCommands.MoveOptions(battle)
			.Where(option => option.EndPosition == origin)
			.ToList();
		var linedUp = options.First(option => option.EndBasis.Forward == Coord.Forward);
		var turnedAway = options.First(option => option.EndBasis.Forward != Coord.Forward);

		var linedUpHits = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			linedUp);
		var turnedAwayHits = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			turnedAway);

		Assert.Contains(
			linedUpHits,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/lightning_cannon.svg");
		Assert.DoesNotContain(
			turnedAwayHits,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/lightning_cannon.svg");
	}

	private static MovePathOption RouteAt(
		IReadOnlyList<MovePathOption> options,
		Coord endPosition,
		Coord forward) =>
		options.First(option =>
			option.EndPosition == endPosition
			&& option.EndBasis.Forward == forward);
}
