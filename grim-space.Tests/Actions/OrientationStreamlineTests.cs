using GrimSpace.Battle.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Tests.Actions;

[BattleTestSuite]
public sealed class OrientationStreamlineTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ThreeClockwiseRollsCanonicalizeToOneCounterClockwiseRoll()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		for (var i = 0; i < 3; i++)
			Assert.True(OrientationStreamline.TryApplyButton(
				sim,
				new RollAction(PlayerId, ERollDirection.Clockwise)));

		var roll = Assert.IsType<RollAction>(Assert.Single(sim.Actions));
		Assert.Equal(ERollDirection.CounterClockwise, roll.Direction);
		Assert.Equal(2, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void FourClockwiseRollsCancelAndRefundMp()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		for (var i = 0; i < 4; i++)
			Assert.True(OrientationStreamline.TryApplyButton(
				sim,
				new RollAction(PlayerId, ERollDirection.Clockwise)));

		Assert.Empty(sim.Actions);
		Assert.Equal(3, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void TwoRightYawsCanonicalizeToYaw180()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		for (var i = 0; i < 2; i++)
			Assert.True(OrientationStreamline.TryApplyButton(
				sim,
				new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var heading = Assert.IsType<HeadingTurnAction>(Assert.Single(sim.Actions));
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
		Assert.Equal(1, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void ThreeRightYawsCanonicalizeToOneLeftYaw()
	{
		var sim = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		for (var i = 0; i < 3; i++)
			Assert.True(OrientationStreamline.TryApplyButton(
				sim,
				new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var heading = Assert.IsType<HeadingTurnAction>(Assert.Single(sim.Actions));
		Assert.Equal(EHeadingTurn.YawLeft, heading.Turn);
		Assert.Equal(2, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}

	[Fact]
	public void UnaffordableExactOrientationPreservesExistingQueue()
	{
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Carrier(new Coord(5, 5, 5)),
			BattleTestFixture.Player(new Coord(0, 0, 0)));
		var sim = battle.Engine.CreateSimulation();

		Assert.True(OrientationStreamline.TryApplyButton(
			sim,
			new HeadingTurnAction("carrier", EHeadingTurn.YawRight)));
		Assert.False(OrientationStreamline.TryApplyButton(
			sim,
			new HeadingTurnAction("carrier", EHeadingTurn.YawRight)));

		var heading = Assert.IsType<HeadingTurnAction>(Assert.Single(sim.Actions));
		Assert.Equal(EHeadingTurn.YawRight, heading.Turn);
		Assert.Equal(0, sim.StateOf<ActorState>("carrier").ManeuverPoints);
	}

	[Fact]
	public void AsymmetricCostsChooseCheapestEquivalentSequence()
	{
		var player = BattleTestFixture.Player(new Coord(5, 5, 5));
		var baseline = player.State.Maneuverability;
		player.State.Maneuverability = new ManeuverabilitySpec(
			baseline.MaxActionPoints,
			3,
			baseline.SupportedTranslations.ToDictionary(
				direction => direction,
				direction => baseline.TryGetTranslationApCost(direction, out var cost) ? cost : 0),
			new Dictionary<EHeadingTurn, int>
			{
				[EHeadingTurn.YawRight] = 4,
				[EHeadingTurn.YawLeft] = 1,
			},
			new Dictionary<ERollDirection, int>());
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 0, 0)));
		var sim = battle.PlayerAgent.Sim;

		Assert.True(OrientationStreamline.TryApplyButton(
			sim,
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.Equal(3, sim.Actions.Count);
		Assert.All(
			sim.Actions,
			action => Assert.Equal(
				EHeadingTurn.YawLeft,
				Assert.IsType<HeadingTurnAction>(action).Turn));
		Assert.Equal(0, sim.StateOf<ActorState>(PlayerId).ManeuverPoints);
	}
}
