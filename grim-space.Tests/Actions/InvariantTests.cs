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
	public void StandaloneOrientationActionsAreIllegal()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
	}

	[Fact]
	public void CombinedStepRejectsHalfTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(
			new MoveStepAction(PlayerId, EHeadingTurn.Yaw180)));
	}

	[Fact]
	public void UndoLeavesSimulationCommittable()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new MoveStepAction(PlayerId, EHeadingTurn.YawRight, ERollDirection.Clockwise)));

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
