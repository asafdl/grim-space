using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.World;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Presentation;

public sealed class PoseHitOpportunityTests
{
	private const string PlayerId = "player";

	[Fact]
	public void RailgunHitFromPoseIncludesEnemyAndRailgunIcon()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
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

		var railgun = Assert.Single(
			opportunities,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/railgun.svg");
		Assert.Equal(enemyId, railgun.TargetId);
	}

	[Fact]
	public void PoseHitOpportunitiesDoNotMutatePlanningSim()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
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
	public void GroupedFlakMountsEmitOneEntryPerTarget()
	{
		var origin = new Coord(5, 5, 5);
		var probe = BattleTestFixture.BeginSimulation(origin);
		var flakCell = FlakDef.Instance.AffectedCells(
			new FlakAction(PlayerId, ESpatialOrientation.Starboard),
			probe.PlayerAgent.Sim.World).First();
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(flakCell),
			BattleTestFixture.Grid(20));
		var sim = battle.PlayerAgent.Sim;
		var enemyId = BattleTestFixture.FirstEnemyId(battle);
		var route = new RouteHitPreview(BattleTestFixture.ForwardPath(PlayerId, origin, steps: 0));

		var opportunities = route.GetHitOpportunities(sim, PlayerId);

		Assert.Equal(
			1,
			opportunities.Count(opportunity =>
				opportunity.IconPath == "res://assets/ui/abilities/flak.svg"
				&& opportunity.TargetId == enemyId));
	}

	[Fact]
	public void SpentRailgunIsExcludedFromOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var preview = new PlanningPreview();
		Assert.True(BattleTestCommands.FireRailgun(battle));
		var selected = RouteAt(BattleTestCommands.MoveOptions(battle), origin, Coord.Forward);

		var opportunities = preview.PoseHitOpportunities(
			battle.PlayerAgent.Sim,
			PlayerId,
			selected);

		Assert.DoesNotContain(
			opportunities,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/railgun.svg");
	}

	[Fact]
	public void HeadingChangeAffectsRailgunLine()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
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
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/railgun.svg");
		Assert.DoesNotContain(
			turnedAwayHits,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/railgun.svg");
	}

	private static MovePathOption RouteAt(
		IReadOnlyList<MovePathOption> options,
		Coord endPosition,
		Coord forward) =>
		options.First(option =>
			option.EndPosition == endPosition
			&& option.EndBasis.Forward == forward);
}
