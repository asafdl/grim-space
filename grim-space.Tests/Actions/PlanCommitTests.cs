using GrimSpace.Battle;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Planning;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Tests.Planning;

namespace GrimSpace.Tests.Actions;

public sealed class PlanCommitTests
{
	private const string PlayerId = "player";

	[Fact]
	public void FightersBeginTurnWithFourActionPoints()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));

		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

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
		var battle = BeginPlanning(player, enemy, grid, blocked);

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
	public void PreviewReflectsQueuedMoveAndUpdatesWhenPlanChanges()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginPlanning(player, enemy, grid, blocked);

		var emptyPreview = Preview.Simulate(battle);
		Assert.Equal(origin, emptyPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, emptyPreview.Actor.ActionPoints);

		EnqueueForwardMove(battle, steps: 3);
		var threeStepPreview = Preview.Simulate(battle);
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(origin + Coord.Forward * 3, threeStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - threeStepCost, threeStepPreview.Actor.ActionPoints);

		Assert.True(battle.TryUndoLast());
		EnqueueForwardMove(battle, steps: 4);

		var fourStepPreview = Preview.Simulate(battle);
		var fourStepCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 4);
		Assert.Equal(origin + Coord.Forward * 4, fourStepPreview.Actor.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn - fourStepCost, fourStepPreview.Actor.ActionPoints);

		Assert.Equal(origin, player.State.Position);
		Assert.Equal(MovementExpectations.FighterApPerTurn, player.State.ActionPoints);
	}

	[Fact]
	public void EndingTurnAppliesMovePositionMomentumAndAp()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginPlanning(player, enemy, grid, blocked);

		EnqueueForwardMove(battle, stepCount);
		var actions = battle.Actions.ToList();
		var nonUnits = new Dictionary<string, NonUnit>();
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		BattleTestApply.ApplyToLive(actions, [player, enemy], grid, nonUnits, blocked, new Timeline(), PlayerId);

		Assert.Equal(origin + Coord.Forward * stepCount, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			player.State.MomentumLevel);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, player.State.ActionPoints);
	}

	[Fact]
	public void UndoRemovesMostRecentlyQueuedAction()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BeginPlanning(player, enemy, grid, blocked);

		EnqueueForwardMove(battle, steps: 3);
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(battle.TryUndoLast());
		Assert.Equal(3, battle.Actions.Count);
		Assert.All(battle.Actions, action => Assert.IsType<MoveStepAction>(action));
	}

	[Fact]
	public void SecondMoveEnqueueRejectedUntilUndo()
	{
		var origin = new Coord(5, 5, 5);
		var planning = PlanningTestFixture.Controller(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));

		var shortMove = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);
		var longMove = MovementExpectations.PureForwardMove(origin, stepCount: 4, startMomentum: 0);

		Assert.True(planning.TryEnqueueMovePath(shortMove));
		Assert.False(planning.TryEnqueueMovePath(longMove));
		Assert.Equal(3, planning.Actions.Count);

		Assert.True(planning.TryUndoLast());
		Assert.True(planning.TryEnqueueMovePath(longMove));
		Assert.Equal(
			origin + Coord.Forward * 4,
			planning.Board.StateOf(planning.PlayerId).Position);
	}

	private static BattleOrchestrator BeginPlanning(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		GrimSpace.Math.Grid.Grid grid,
		IReadOnlySet<Coord> blocked) =>
		PlanningTestFixture.Controller(player, enemy, grid, blocked);

	private static void EnqueueForwardMove(BattleOrchestrator battle, int steps)
	{
		var origin = battle.Board.StateOf(battle.PlayerId).Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(battle.Board.StateOf(battle.PlayerId));
		var option = GetMoveOptionsFromSession(battle.Session)
			.First(o => o.Path.Count == steps);
		Assert.True(battle.TryEnqueueMovePath(option));
	}

	private static IReadOnlyList<GrimSpace.Battle.Movement.Option> GetMoveOptionsFromSession(
		GrimSpace.Core.Engine.Simulation<
			GrimSpace.Battle.Board.BattleBoard,
			GrimSpace.Battle.Runtime.ActorSession> session)
	{
		var origin = session.PreviewWorld.StateOf("player").Position;
		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(session.PreviewWorld.StateOf("player"));
		var startCount = session.Actions.Count;
		var results = new Dictionary<Coord, GrimSpace.Battle.Movement.Option>();

		foreach (var searchFrame in session.SearchMoves("player"))
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
