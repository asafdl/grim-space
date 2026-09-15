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
			.First(option => option.ExtensionApCost == 1);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, second));

		Assert.IsType<MoveStepAction>(battle.PlayerAgent.Sim.Actions[0]);
		Assert.IsType<RailgunAction>(battle.PlayerAgent.Sim.Actions[1]);
		Assert.All(
			battle.PlayerAgent.Sim.Actions.Skip(2),
			action => Assert.True(action is HeadingTurnAction or RollAction or MoveStepAction));
		Assert.IsType<MoveStepAction>(battle.PlayerAgent.Sim.Actions[^1]);
	}

	[Fact]
	public void UndoRestoresPositionBasisAndApForCombinedSegment()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		IAction[] segment =
		[
			new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight),
			new MoveStepAction(battle.PlayerId),
			new RollAction(battle.PlayerId, ERollDirection.Clockwise),
			new MoveStepAction(battle.PlayerId),
		];
		Assert.True(battle.PlayerAgent.TryEnqueue(segment));

		Assert.True(battle.PlayerAgent.Undo());

		var state = battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId);
		Assert.Equal(origin, state.Position);
		Assert.Equal(Coord.Forward, state.Fore);
		Assert.Equal(Coord.Up, state.Dorsal);
		Assert.Equal(4, state.ActionPoints);
	}

	[Theory]
	[InlineData(ESpatialOrientation.Port, -1, 0, 0)]
	[InlineData(ESpatialOrientation.Starboard, 1, 0, 0)]
	[InlineData(ESpatialOrientation.Retro, 0, 0, -1)]
	[InlineData(ESpatialOrientation.Dorsal, 0, 1, 0)]
	[InlineData(ESpatialOrientation.Ventral, 0, -1, 0)]
	public void DirectionalStepTranslatesWithoutChangingOrientation(
		ESpatialOrientation direction,
		int x,
		int y,
		int z)
	{
		var origin = new Coord(5, 5, 5);
		var session = BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(new MoveStepAction("player", direction)));

		var state = session.StateOf<ActorState>("player");
		Assert.Equal(origin + new Coord(x, y, z), state.Position);
		Assert.Equal(Coord.Forward, state.Fore);
		Assert.Equal(Coord.Up, state.Dorsal);
	}

	[Fact]
	public void RetroStepCostsTwoAp()
	{
		var origin = new Coord(5, 5, 5);
		var session = BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Starboard)));
		Assert.Equal(3, session.StateOf<ActorState>("player").ActionPoints);

		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Retro)));
		Assert.Equal(1, session.StateOf<ActorState>("player").ActionPoints);
	}

	[Fact]
	public void RetroStepRequiresTwoAp()
	{
		var session = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(new Coord(5, 5, 5), actionPoints: 1),
			BattleTestFixture.Enemy(Coord.Zero)).PlayerAgent.Sim;

		Assert.False(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Retro)));
		Assert.True(session.TryEnqueue(
			new MoveStepAction("player", ESpatialOrientation.Port)));
	}
}
