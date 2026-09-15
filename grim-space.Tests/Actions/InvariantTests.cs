using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class InvariantTests
{
	private const string PlayerId = "player";

	[Fact]
	public void OneStepAllowsCommit()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Single(actions);
	}

	[Fact]
	public void StandaloneOrientationActionsAreIncomplete()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Equal(InvariantStatus.Incomplete, battle.PlayerAgent.Sim.InvariantStatus);
		Assert.False(battle.PlayerAgent.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Incomplete, status);
	}

	[Fact]
	public void HeadingRejectsHalfTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.Yaw180)));
	}

	[Fact]
	public void OrientationWithoutAnyMovementCompletionIsRejected()
	{
		var origin = new Coord(0, 0, 0);
		var blocked = new HashSet<Coord>
		{
			origin + Coord.Forward,
			origin - Coord.Forward,
			origin + new Coord(1, 0, 0),
			origin - new Coord(1, 0, 0),
			origin + Coord.Up,
			origin - Coord.Up,
		};
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(5, 5, 5)),
			BattleTestFixture.Grid(),
			blocked);

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(InvariantStatus.Ok, battle.PlayerAgent.Sim.InvariantStatus);
	}

	[Fact]
	public void OrientationCanCompleteWithLateralMoveWhenForwardIsBlocked()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(),
			new HashSet<Coord> { origin + Coord.Forward });

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Equal(InvariantStatus.Incomplete, battle.PlayerAgent.Sim.InvariantStatus);
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new MoveStepAction(PlayerId, ESpatialOrientation.Starboard)));
		Assert.Equal(InvariantStatus.Ok, battle.PlayerAgent.Sim.InvariantStatus);
	}

	[Fact]
	public void HeadingRequiresForwardMoveInNewHeading()
	{
		var origin = new Coord(5, 5, 5);
		var sim = BattleTestFixture.BeginSimulation(origin).PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.False(sim.TryEnqueue(
			new MoveStepAction(PlayerId, ESpatialOrientation.Starboard)));
		Assert.Single(sim.Actions);
		Assert.Equal(origin, sim.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(InvariantStatus.Incomplete, sim.InvariantStatus);

		var heading = sim.StateOf<ActorState>(PlayerId).Fore;
		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(origin + heading, sim.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(InvariantStatus.Ok, sim.InvariantStatus);
	}

	[Fact]
	public void HeadingIsRejectedWhenItsForwardCompletionIsBlocked()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(),
			new HashSet<Coord> { origin + new Coord(1, 0, 0) });

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(InvariantStatus.Ok, battle.PlayerAgent.Sim.InvariantStatus);
	}

	[Fact]
	public void OrientationCannotBeInterruptedByAnotherAction()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(new RailgunAction(PlayerId)));
		Assert.Single(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(InvariantStatus.Incomplete, battle.PlayerAgent.Sim.InvariantStatus);
	}

	[Fact]
	public void ManeuverAllowsAtMostOneHeadingAndRoll()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.PitchUp)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.CounterClockwise)));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(InvariantStatus.Ok, battle.PlayerAgent.Sim.InvariantStatus);
	}

	[Fact]
	public void UndoLeavesSimulationCommittable()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new RollAction(PlayerId, ERollDirection.Clockwise),
			new MoveStepAction(PlayerId)));

		Assert.True(battle.PlayerAgent.Sim.TryUndoLast());
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Empty(actions);
	}

	[Fact]
	public void StagedStepAllowsEndTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId)));

		battle.EndTurn();

		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}
}
