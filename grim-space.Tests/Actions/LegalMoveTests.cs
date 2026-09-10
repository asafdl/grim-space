using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class LegalMoveTests
{
	[Fact]
	public void EnqueueMovePathAddsExactCombinedSteps()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var move = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.EndPosition == origin + Coord.Forward * 3);

		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, move));
		Assert.Equal(move.Steps, battle.PlayerAgent.Sim.Actions);
		Assert.Equal(move.EndPosition, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Equal(1, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
	}

	[Fact]
	public void ConfirmedSegmentCanBeFollowedByWeaponAndAnotherSegment()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var first = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.EndPosition == origin + Coord.Forward);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, first));
		Assert.True(battle.PlayerAgent.TryEnqueue([new RailgunAction(battle.PlayerId)]));

		var second = MovePathEndpoints.DiscoverExtensions(battle.PlayerAgent.Sim, battle.PlayerId)
			.First(option => option.Steps.Count == 1);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, second));

		Assert.Collection(
			battle.PlayerAgent.Sim.Actions,
			action => Assert.IsType<MoveStepAction>(action),
			action => Assert.IsType<RailgunAction>(action),
			action => Assert.IsType<MoveStepAction>(action));
	}

	[Fact]
	public void UndoRestoresPositionBasisAndApForCombinedSegment()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		IAction[] segment =
		[
			new MoveStepAction(battle.PlayerId, EHeadingTurn.YawRight),
			new MoveStepAction(battle.PlayerId, Roll: ERollDirection.Clockwise),
		];
		Assert.True(battle.PlayerAgent.TryEnqueue(segment));

		Assert.True(battle.PlayerAgent.Undo());

		var state = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId);
		Assert.Equal(origin, state.Position);
		Assert.Equal(Coord.Forward, state.Fore);
		Assert.Equal(Coord.Up, state.Dorsal);
		Assert.Equal(4, state.ActionPoints);
	}
}
