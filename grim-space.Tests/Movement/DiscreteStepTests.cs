using GrimSpace.Battle.Movement;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class DiscreteStepTests
{
	private readonly DiscreteStep _movement = new();

	[Fact]
	public void OneAndTwoApForwardMovesAreNotReachableEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 0);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord> { enemy.State.Position };

		var options = _movement.GetMoveOptions(player.State, BattleTestFixture.Grid(), blocked);
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

		var options = _movement.GetMoveOptions(player.State, BattleTestFixture.Grid(), blocked);
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
			Coord.Zero - player.State.ForwardDirection);

		Assert.False(_movement.CanMove(player.State, zigzag));
	}

	[Fact]
	public void PathsCannotPassThroughBlockedCellsOrLeaveGrid()
	{
		var origin = new Coord(0, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var blocked = new HashSet<Coord> { origin + Coord.Forward * 2 };

		var options = _movement.GetMoveOptions(player.State, BattleTestFixture.Grid(), blocked);

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
		var move = MovementExpectations.PureForwardMove(origin, stepCount, startMomentum);

		_movement.ApplyMomentum(player.State, move.Path);
		_movement.ApplyMove(player.State, move);

		Assert.Equal(origin + Coord.Forward * stepCount, player.State.Position);
		Assert.Equal(
			MovementExpectations.MomentumAfterPureForwardPath(startMomentum, stepCount),
			player.State.MomentumLevel);
	}

	[Fact]
	public void ResolvingRetroMoveLowersMomentum()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var retro = BattleTestFixture.Path(origin, 0, Coord.Zero - player.State.ForwardDirection);

		_movement.ApplyMomentum(player.State, retro.Path);
		_movement.ApplyMove(player.State, retro);

		Assert.Equal(1, player.State.MomentumLevel);
	}
}
