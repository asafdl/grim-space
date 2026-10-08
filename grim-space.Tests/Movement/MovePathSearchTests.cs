using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Units.Maneuvering;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

[BattleTestSuite]
public sealed class MovePathSearchTests
{
	private const string PlayerId = "player";

	[Fact]
	public void StraightRoutesConsumeOneApPerStep()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var paths = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);

		for (var steps = 1; steps <= 4; steps++)
		{
			var path = paths.First(option =>
				option.EndPosition == origin + Coord.Forward * steps
				&& option.EndBasis.Forward == Coord.Forward);
			Assert.Equal(steps, path.ExtensionApCost);
			Assert.Equal(4 - steps, path.RemainingAp);
		}
	}

	[Fact]
	public void RouteTurnsBeforeAdvancing()
	{
		var origin = new Coord(5, 5, 5);
		var paths = MovePathEndpoints.DiscoverExtensions(
			BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim,
			PlayerId);
		var end = origin + Coord.Forward * 3 + new Coord(1, 0, 0);

		var path = paths.First(option =>
			option.EndPosition == end
				&& option.EndBasis.Forward == new Coord(1, 0, 0)
				&& option.EndBasis.Up == Coord.Up);

		Assert.Equal(4, path.ExtensionApCost);
		Assert.Contains(path.Steps, action =>
			action is HeadingTurnAction { Turn: EHeadingTurn.YawRight });
	}

	[Fact]
	public void CombinedTurnAndRollUsesArrivalOrientation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new RollAction(PlayerId, ERollDirection.CounterClockwise),
			new MoveStepAction(PlayerId)));
		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(origin + new Coord(1, 0, 0), actor.Position);
		Assert.Equal(new Coord(1, 0, 0), actor.Fore);
		Assert.Equal(-Coord.Forward, actor.Dorsal);
		Assert.Equal(3, actor.ActionPoints);
	}

	[Theory]
	[InlineData(-1, 0, 0, 1, 0)]
	[InlineData(0, 0, -1, 1, 2)]
	public void SelectedDirectionalRouteUsesApFirstRanking(
		int x,
		int y,
		int z,
		int apCost,
		int mpCost)
	{
		var origin = new Coord(5, 5, 5);
		var paths = MovePathEndpoints.DiscoverExtensions(
			BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim,
			PlayerId);
		var expected = origin + new Coord(x, y, z);
		var startingBasis = GridBasis.From(Coord.Forward, Coord.Up, new Coord(1, 0, 0));

		var path = paths.First(option =>
			option.EndPosition == expected
			&& option.EndBasis == startingBasis);

		Assert.Equal(expected, path.EndPosition);
		Assert.Equal(path.ResultState.Position, path.EndPosition);
		Assert.Equal(path.ResultState.Fore, path.EndBasis.Forward);
		Assert.Equal(path.ResultState.Dorsal, path.EndBasis.Up);
		Assert.Equal(apCost, path.ExtensionApCost);
		Assert.Equal(mpCost, path.ExtensionMpCost);
	}

	[Fact]
	public void DominatedRouteReturningToOriginalPoseIsNotReturned()
	{
		var origin = new Coord(5, 5, 5);
		var paths = MovePathEndpoints.DiscoverExtensions(
			BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim,
			PlayerId);
		var startingBasis = GridBasis.From(Coord.Forward, Coord.Up, new Coord(1, 0, 0));

		Assert.DoesNotContain(paths, path =>
			path.EndPosition == origin
			&& path.EndBasis == startingBasis);
	}

	[Fact]
	public void RotationOnlyPathRetainsPositionAndChangesBasis()
	{
		var origin = new Coord(5, 5, 5);
		var paths = MovePathEndpoints.DiscoverExtensions(
			BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim,
			PlayerId);

		var path = paths.First(option =>
			option.EndPosition == origin
			&& option.EndBasis.Forward == new Coord(1, 0, 0)
			&& option.EndBasis.Up == Coord.Up);

		Assert.Equal(0, path.ExtensionApCost);
		Assert.Equal(1, path.ExtensionMpCost);
		Assert.Equal(4, path.RemainingAp);
		Assert.Equal(2, path.RemainingMp);
		Assert.Single(path.Checkpoints);
	}

	[Fact]
	public void PathsCannotPassThroughBlockedCellsOrLeaveGrid()
	{
		var origin = new Coord(0, 5, 5);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blockedCell = origin + Coord.Forward * 2;
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { blockedCell, enemy.State.Position });

		var options = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);

		Assert.DoesNotContain(options, path => path.EndPosition.X < 0);
		Assert.DoesNotContain(options, path => path.Checkpoints.Any(checkpoint => checkpoint.Position == blockedCell));
	}

	[Fact]
	public void SearchDoesNotMutateSimulation()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var session = battle.PlayerAgent.Sim;
		var before = session.StateOf<ActorState>(PlayerId);
		var position = before.Position;
		var ap = before.ActionPoints;

		foreach (var _ in ActionSearch.Run(
			session,
			PlayerId,
			Capabilities.Movement,
			BattleSearchVisit.ForMovePreview)) { }

		Assert.Empty(session.Actions);
		Assert.Equal(position, session.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(ap, session.StateOf<ActorState>(PlayerId).ActionPoints);
	}

	[Fact]
	public void EveryProjectedPathIsReplayable()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var paths = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, PlayerId);

		foreach (var path in paths)
		{
			var trial = battle.PlayerAgent.Sim.Fork();
			Assert.True(trial.TryEnqueue(path.Steps.Cast<GrimSpace.Core.Actions.IAction>().ToArray()));
		}
	}
}
