using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class HeadingTurnTests
{
	private const string PlayerId = "player";

	[Theory]
	[InlineData(EHeadingTurn.YawLeft, -1, 0, 0)]
	[InlineData(EHeadingTurn.YawRight, 1, 0, 0)]
	[InlineData(EHeadingTurn.PitchUp, 0, 1, 0)]
	[InlineData(EHeadingTurn.PitchDown, 0, -1, 0)]
	public void QuarterTurnAdvancesAlongNewHeading(EHeadingTurn heading, int x, int y, int z)
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId, heading)));

		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(origin + new Coord(x, y, z), actor.Position);
		Assert.Equal(new Coord(x, y, z), actor.Fore);
		Assert.Equal(3, actor.ActionPoints);
	}

	[Fact]
	public void TurnAndRollCanOccurInSameOneApStep()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new MoveStepAction(PlayerId, EHeadingTurn.YawRight, ERollDirection.Clockwise)));

		var actor = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(new Coord(1, 0, 0), actor.Fore);
		Assert.Equal(Coord.Forward, actor.Dorsal);
		Assert.Equal(3, actor.ActionPoints);
	}
}
