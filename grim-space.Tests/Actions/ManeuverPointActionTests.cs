using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class ManeuverPointActionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void StandaloneRotationsAreCommittableAndSpendOnlyMp()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Equal(InvariantStatus.Ok, sim.InvariantStatus);
		Assert.True(sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Equal(2, actions.Count);
		Assert.Equal(4, sim.StateOf<ActorState>(PlayerId).ActionPoints);
		Assert.Equal(1, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void Yaw180CostsTwoMp()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.Yaw180)));

		Assert.Equal(-Coord.Forward, sim.StateOf<ActorState>(PlayerId).Fore);
		Assert.Equal(1, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void RotationIsLegalWhenEveryAdjacentCellIsBlocked()
	{
		var origin = new Coord(5, 5, 5);
		var blocked = Enum.GetValues<ESpatialOrientation>()
			.Select(direction => origin + BodyFrame.WorldAligned(origin).Step(direction))
			.ToHashSet();
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(0, 0, 0)),
			BattleTestFixture.Grid(),
			blocked);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
	}

	[Fact]
	public void RotationCanBeFollowedByWeapon()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new LightningCannonAction(PlayerId)));
	}

	[Fact]
	public void ApAndMpExhaustIndependently()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;
		sim.StateOf<ActorState>(PlayerId).ActionPoints = 0;

		Assert.True(sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.False(sim.TryEnqueue(new MoveStepAction(PlayerId)));

		var moveOnly = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;
		moveOnly.StateOf<ActorState>(PlayerId).ManeuverPoints = 0;

		Assert.True(moveOnly.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.False(moveOnly.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.Clockwise)));
	}

	[Fact]
	public void MpExhaustionRejectsFurtherRotations()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new RollAction(PlayerId, ERollDirection.Clockwise),
			new HeadingTurnAction(PlayerId, EHeadingTurn.PitchUp)));
		Assert.Equal(0, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
		Assert.False(sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.CounterClockwise)));
	}
}
