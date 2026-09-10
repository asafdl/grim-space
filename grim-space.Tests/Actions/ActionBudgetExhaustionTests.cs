using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class ActionBudgetExhaustionTests
{
	private const string PlayerId = "player";

	[Fact]
	public void AfterFourCombinedStepsNoMovementRemainsLegal()
	{
		var session = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;
		for (var i = 0; i < 4; i++)
			Assert.True(session.TryEnqueue(new MoveStepAction(PlayerId)));

		Assert.Equal(0, session.StateOf<ActorState>(PlayerId).ActionPoints);
		LegalActionProbe.AssertExhausted(session, PlayerId, MoveDef.Instance);
	}

	[Fact]
	public void OrientationDoesNotAddToStepCost()
	{
		var session = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5)).PlayerAgent.Sim;

		Assert.True(session.TryEnqueue(
			new MoveStepAction(PlayerId, EHeadingTurn.YawRight, ERollDirection.CounterClockwise)));

		Assert.Equal(3, session.StateOf<ActorState>(PlayerId).ActionPoints);
	}

	[Fact]
	public void WeaponBudgetsRemainIndependentFromMovementAp()
	{
		var session = BattleTestFixture.BeginSimulation(new Coord(5, 5, 1)).PlayerAgent.Sim;
		for (var i = 0; i < 4; i++)
			Assert.True(session.TryEnqueue(new MoveStepAction(PlayerId)));

		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, FlakDef.Instance));
		Assert.True(LegalActionProbe.HasAnyLegal(session, PlayerId, RailgunDef.Instance));
	}
}
