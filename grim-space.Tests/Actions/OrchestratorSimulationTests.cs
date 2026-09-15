using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Actions;

public sealed class OrchestratorSimulationTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TryEnqueueRejectsBlockedMoveWithoutMutatingQueue()
	{
		var origin = new Coord(5, 5, 5);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		var battle = BattleTestFixture.BeginSimulation(
			BattleTestFixture.Player(origin),
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId)));
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(origin, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Position);
	}

	[Fact]
	public void BatchTryEnqueueRollsBackWhenLaterStepExhaustsAp()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, actionPoints: 1);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			BattleTestFixture.Enemy(new Coord(0, 5, 5)));
		IAction[] steps = [new MoveStepAction(PlayerId), new MoveStepAction(PlayerId)];

		Assert.False(battle.PlayerAgent.Sim.TryEnqueue(actions: steps));
		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.Equal(origin, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Position);
		Assert.Equal(1, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).ActionPoints);
	}

	[Fact]
	public void CombinedTurnMoveRollCostsOneAp()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(
			new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight),
			new RollAction(PlayerId, ERollDirection.Clockwise),
			new MoveStepAction(PlayerId)));

		var state = battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId);
		Assert.Equal(origin + new Coord(1, 0, 0), state.Position);
		Assert.Equal(new Coord(1, 0, 0), state.Fore);
		Assert.Equal(Coord.Forward, state.Dorsal);
		Assert.Equal(3, state.ActionPoints);
	}

}
