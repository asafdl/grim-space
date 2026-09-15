using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class OrientationStreamlineTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ButtonsStageACommittableManeuverWithoutSpendingAp()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(OrientationStreamline.TryApplyButton(
			sim,
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(OrientationStreamline.TryApplyButton(
			sim,
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Equal(InvariantStatus.Incomplete, sim.InvariantStatus);
		Assert.Equal(4, sim.StateOf<ActorState>(PlayerId).ActionPoints);

		Assert.True(sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Equal(InvariantStatus.Ok, sim.InvariantStatus);
		Assert.Equal(3, sim.StateOf<ActorState>(PlayerId).ActionPoints);
	}

	[Fact]
	public void ImpossibleButtonInputFallsBackToNoOpWithoutMutatingQueue()
	{
		var origin = new Coord(0, 0, 0);
		var sim = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			BattleTestFixture.Enemy(new Coord(5, 5, 5)),
			BattleTestFixture.Grid(),
			new HashSet<Coord>
			{
				origin + Coord.Forward,
				origin - Coord.Forward,
				origin + new Coord(1, 0, 0),
				origin - new Coord(1, 0, 0),
				origin + Coord.Up,
				origin - Coord.Up,
			})
			.PlayerAgent.Sim;

		Assert.True(OrientationStreamline.TryApplyButton(
			sim,
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Empty(sim.Actions);
		Assert.Equal(InvariantStatus.Ok, sim.InvariantStatus);
	}
}
