using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MovePathFinderTests
{
	private const string PlayerId = "player";

	[Fact]
	public void OneAndTwoApForwardMovesAreNotReachableEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var options = GetMoveOptions(player, enemy, blocked, origin);
		var endpoints = options.ToDictionary(option => option.EndPosition);

		var oneForward = origin + Coord.Forward;
		var twoForward = origin + Coord.Forward * 2;
		var threeForward = origin + Coord.Forward * 3;
		var threeStepCost = MovementExpectations.TotalApForPureForwardPath(0, 3);

		Assert.False(endpoints.ContainsKey(oneForward));
		Assert.False(endpoints.ContainsKey(twoForward));
		Assert.True(MovementExpectations.IsValidMoveEndpoint(threeStepCost));
		Assert.True(endpoints.ContainsKey(threeForward));
		Assert.Equal(threeStepCost, endpoints[threeForward].ApCost);
	}

	[Fact]
	public void HighMomentumCanReachForwardEndpointWithoutSpendingAp()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = MovementExpectations.MaxMomentum;
		var stepCount = startMomentum;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };

		var options = GetMoveOptions(player, enemy, blocked, origin);
		var endpoint = origin + Coord.Forward * stepCount;
		var expectedCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, stepCount);

		Assert.Equal(0, expectedCost);
		Assert.True(MovementExpectations.IsValidMoveEndpoint(expectedCost));
		Assert.Contains(options, option => option.EndPosition == endpoint && option.ApCost == expectedCost);
	}

	[Fact]
	public void PathCannotContainOpposingDirections()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var zigzag = BattleTestFixture.Path(
			origin,
			0,
			Coord.Forward,
			Coord.Zero - player.State.Fore);

		var frame = GrimSpace.Battle.Spatial.BodyFrame.From(player.State);
		var steps = MoveDef.StepsFromPath(PlayerId, frame, origin, zigzag.Path);
		var board = BattleBoard.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var phaseContext = new TurnPhaseContext();
		var ctx = BattleActionContext.For(board, phaseContext, PlayerId);

		Assert.False(MoveDef.Instance.IsLegal(steps[^1], ctx));
	}

	[Fact]
	public void PathsCannotPassThroughBlockedCellsOrLeaveGrid()
	{
		var origin = new Coord(0, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { origin + Coord.Forward * 2, enemy.State.Position };

		var options = GetMoveOptions(player, enemy, blocked, origin);

		Assert.DoesNotContain(options, option => option.EndPosition.X < 0);
		Assert.DoesNotContain(options, option => option.Path.Contains(origin + Coord.Forward * 2));
	}

	[Fact]
	public void ResolvingForwardMoveRaisesMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 1;
		var stepCount = 3;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var plan = TestPlan.Begin(
			PlayerId,
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		var option = MovementExpectations.PureForwardMove(origin, stepCount, startMomentum);
		plan.EnqueueMovePath(option);

		Assert.Equal(origin + Coord.Forward * stepCount, plan.Board.StateOf(PlayerId).Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			plan.Board.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void ResolvingRetroMoveLowersMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var retro = BattleTestFixture.Path(origin, 0, Coord.Zero - player.State.Fore);
		var plan = TestPlan.Begin(
			PlayerId,
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		plan.EnqueueMovePath(retro);

		Assert.Equal(1, plan.Board.StateOf(PlayerId).MomentumLevel);
	}

	private static IReadOnlyList<Option> GetMoveOptions(
		GrimSpace.Battle.Units.Unit player,
		GrimSpace.Battle.Units.Unit enemy,
		IReadOnlySet<Coord> blocked,
		Coord origin)
	{
		var board = BattleBoard.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			blocked);
		var phaseContext = new TurnPhaseContext();
		return MovePathFinder.Find(board, phaseContext, PlayerId);
	}
}
