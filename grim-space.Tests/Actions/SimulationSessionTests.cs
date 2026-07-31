using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Actions;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Tests.Simulation;

namespace GrimSpace.Tests.Actions;

public sealed class SimulationSessionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void PlanningMoveUpdatesPreviewButNotLiveState()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		EnqueueForwardMove(battle, steps: stepCount);

		var preview = Preview.Simulate(battle);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);
		var expectedMomentum = MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(startMomentum, player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);

		Assert.Equal(origin + Coord.Forward * stepCount, preview.Actor.Position);
		Assert.Equal(expectedMomentum, preview.Actor.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, preview.Actor.ActionPoints);
	}

	[Fact]
	public void PreviewReflectsQueuedMoveAndUpdatesAfterUndoAndReplace()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		var emptyPreview = Preview.Simulate(battle);
		Assert.Equal(origin, emptyPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.Actor.ActionPoints);

		EnqueueForwardMove(battle, steps: 3);
		var threeStepPreview = Preview.Simulate(battle);
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - threeStepCost, threeStepPreview.Actor.ActionPoints);

		Assert.True(battle.Sim.TryUndoLast());
		EnqueueForwardMove(battle, steps: 4);

		var fourStepPreview = Preview.Simulate(battle);
		var fourStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 4);
		Assert.Equal(origin + Coord.Forward * 4, fourStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - fourStepCost, fourStepPreview.Actor.ActionPoints);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void UndoLastRemovesHeadingUndoGroupAfterMovePath()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginSimulation(player, enemy, grid, blocked);

		EnqueueForwardMove(battle, steps: 3);
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(battle.Sim.TryUndoLast());
		Assert.Equal(3, battle.Sim.Actions.Count);
		Assert.All(battle.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));
	}

	[Fact]
	public void SecondMovePathRejectedWhenApInsufficientUntilUndo()
	{
		var origin = new Coord(5, 5, 5);
		var planning = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		var shortMove = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		var longMove = MovementExpectations.PureForwardMove(origin, stepCount: 4, startMomentum: 0);

		Assert.True(BattleTestActions.TryEnqueueMovePath(planning, shortMove));
		Assert.False(BattleTestActions.TryEnqueueMovePath(planning, longMove));
		Assert.Equal(3, planning.Sim.Actions.Count);

		Assert.True(planning.Sim.TryUndoLast());
		Assert.True(BattleTestActions.TryEnqueueMovePath(planning, longMove));
		Assert.Equal(
			origin + Coord.Forward * 4,
			planning.Sim.StateOf<ActorState>(planning.PlayerId).Position);
	}

	private static BattleOrchestrator BeginSimulation(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		GrimSpace.Math.Grid.Grid grid,
		IReadOnlySet<Coord> blocked) =>
		BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

	private static void EnqueueForwardMove(BattleOrchestrator battle, int steps)
	{
		var origin = battle.Sim.StateOf<ActorState>(battle.PlayerId).Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(battle.Sim.StateOf<ActorState>(battle.PlayerId));
		var option = GetMoveOptionsFromSimulation(battle.Sim)
			.First(o => o.Path.Count == steps);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
	}

	private static IReadOnlyList<GrimSpace.Battle.Movement.Option> GetMoveOptionsFromSimulation(
		GrimSpace.Core.Engine.Simulation<
			GrimSpace.Battle.World.BattleWorld,
			GrimSpace.Battle.Runtime.ActorRuntime> session)
	{
		var origin = session.StateOf<ActorState>("player").Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(session.StateOf<ActorState>("player"));
		var startCount = session.Actions.Count;
		var results = new Dictionary<Coord, GrimSpace.Battle.Movement.Option>();

		foreach (var searchFrame in session.Search("player", [MoveDef.Instance], BattleSearchVisit.ForCapabilities))
		{
			var moveSteps = searchFrame.Actions
				.Skip(startCount)
				.OfType<MoveStepAction>()
				.ToList();

			if (moveSteps.Count == 0)
				continue;

			var runtime = searchFrame.Runtimes.For("player");
			var option = GrimSpace.Battle.Movement.MovePathRules.ToEndpointOption(
				origin,
				frame,
				moveSteps,
				runtime);
			if (option is null)
				continue;

			if (!results.TryGetValue(option.EndPosition, out var existing) || option.ApCost < existing.ApCost)
				results[option.EndPosition] = option;
		}

		return results.Values.ToList();
	}
}
