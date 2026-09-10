using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Dfs;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

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

		Assert.Equal(4, path.Steps.Count);
		Assert.Equal(EHeadingTurn.YawRight, path.Steps[^1].Heading);
	}

	[Fact]
	public void CombinedTurnAndRollUsesArrivalOrientation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var action = new MoveStepAction(
			PlayerId,
			EHeadingTurn.YawRight,
			ERollDirection.CounterClockwise);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(action));
		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(origin + new Coord(1, 0, 0), actor.Position);
		Assert.Equal(new Coord(1, 0, 0), actor.Fore);
		Assert.Equal(-Coord.Forward, actor.Dorsal);
		Assert.Equal(3, actor.ActionPoints);
	}

	[Fact]
	public void FourStepRouteCanReturnToOrigin()
	{
		var origin = new Coord(5, 5, 5);
		var paths = MovePathEndpoints.DiscoverExtensions(
			BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim,
			PlayerId);

		Assert.Contains(paths, path => path.EndPosition == origin && path.Steps.Count == 4);
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
			[MoveDef.Instance],
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
