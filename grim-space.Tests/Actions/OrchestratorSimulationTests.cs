using GrimSpace.Battle;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class OrchestratorSimulationTests
{
	private const string PlayerId = "player";

	[Fact]
	public void TryEnqueueRejectsBlockedMoveWithoutMutatingQueue()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(origin + Coord.Forward);
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord> { enemy.State.Position });
		var blockedMove = new MoveStepAction(PlayerId, ESpatialOrientation.Forward);

		Assert.False(battle.Sim.TryEnqueue(blockedMove));
		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(origin, battle.Sim.StateOf<ActorState>(PlayerId).Position);
	}

	[Fact]
	public void BatchTryEnqueueRollsBackWhenLaterStepFails()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 0, actionPoints: 1);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 5, 5));
		var battle = BattleTestFixture.BeginSimulation(
			player,
			enemy,
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		var steps = new IAction[]
		{
			new MoveStepAction(PlayerId, ESpatialOrientation.Forward),
			new MoveStepAction(PlayerId, ESpatialOrientation.Forward),
		};

		Assert.False(battle.Sim.TryEnqueue(actions: steps));
		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(origin, battle.Sim.StateOf<ActorState>(PlayerId).Position);
	}

	[Fact]
	public void NewSimulationStartsEmpty()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.Equal(1, battle.Sim.RuntimeFor(PlayerId).NetYaw);

		battle = BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

		Assert.Empty(battle.Sim.Actions);
		Assert.Equal(0, battle.Sim.RuntimeFor(PlayerId).NetYaw);
	}

	[Theory]
	[InlineData(2, false, 1)]
	[InlineData(2, true, 2)]
	public void EndOfPhaseActionAdjustsMomentumWhenStationary(int startMomentum, bool moved, int expectedMomentum)
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var runtime = new ActorRuntime();

		if (moved)
		{
			runtime.ActivePath = MovePathSession.Begin(
				PlayerId,
				origin,
				BodyFrame.From(player.State),
				startMomentum);
			runtime.ActivePath.UsedDirectionsMask = 1;
			runtime.ActivePath.PathForwardSteps = 1;
		}

		var board = BattleWorld.FromSnapshot(
			[player, BattleTestFixture.Enemy(new Coord(0, 0, 0))],
			new Dictionary<string, NonUnit>(),
			BattleTestFixture.Grid(),
			new HashSet<Coord>());
		BattleTestApply.TryApplyOne(new EndOfPhaseAction(PlayerId), board, runtime, PlayerId);

		Assert.Equal(expectedMomentum, board.StateOf(PlayerId).MomentumLevel);
		Assert.False(runtime.ActivePath != null);
	}

	[Fact]
	public void PeekEndOfPhaseAppliesMomentumDecayWhenStationary()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin, momentum: 2);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var battle = BattleTestFixture.BeginSimulation(player, enemy, grid, blocked);

		battle.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise));

		Assert.Equal(2, battle.Sim.StateOf<ActorState>(PlayerId).MomentumLevel);

		var peek = battle.Sim.Peek(EndOfPhaseDef.Instance.Bind(PlayerId));
		Assert.NotNull(peek);
		Assert.Equal(1, peek.Value.World.StateOf(PlayerId).MomentumLevel);
	}

	[Fact]
	public void ApplyCommittedActionsMutatesLiveStateIncrementally()
	{
		var origin = new Coord(5, 5, 5);
		var startMomentum = 0;
		var player = BattleTestFixture.Player(origin, momentum: startMomentum);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var nonUnits = new Dictionary<string, NonUnit>();
		var runtime = new ActorRuntime();
		var timeline = new Timeline();

		foreach (var step in BuildForwardSteps(origin, steps: 3, startMomentum))
		{
			BattleTestApply.ApplyCommittedAction(
				step,
				[player, enemy],
				grid,
				nonUnits,
				blocked,
				runtime,
				timeline,
				PlayerId);
		}

		Assert.Equal(origin + Coord.Forward * 3, player.State.Position);
		var expectedApCost = MovementExpectations.TotalApForPureForwardPath(startMomentum, 3);
		Assert.Equal(MovementExpectations.FighterApPerTurn - expectedApCost, player.State.ActionPoints);
	}

	[Fact]
	public void TryApplyAllStopsOnFirstIllegalAction()
	{
		var origin = new Coord(5, 5, 5);
		var player = BattleTestFixture.Player(origin);
		var postYawForward = origin + Coord.Cross(Coord.Up, Coord.Forward);
		var enemy = BattleTestFixture.Enemy(postYawForward);
		var grid = BattleTestFixture.Grid();
		var blocked = new HashSet<Coord> { enemy.State.Position };
		var board = BattleWorld.FromSnapshot(
			[player, enemy],
			new Dictionary<string, NonUnit>(),
			grid,
			blocked);
		var runtime = new ActorRuntime();
		var yaw = new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight);
		var blockedMove = new MoveStepAction(PlayerId, ESpatialOrientation.Forward);
		var actions = new List<IAction> { yaw, blockedMove };

		Assert.False(BattleTestApply.TryApplyAll(actions, board, runtime, PlayerId));
		Assert.Equal(
			MovementExpectations.FighterApPerTurn - CombatConfig.HeadingTurn90ApCost,
			board.StateOf(PlayerId).ActionPoints);
		Assert.Equal(origin, board.StateOf(PlayerId).Position);
	}

	private static IReadOnlyList<MoveStepAction> BuildForwardSteps(Coord origin, int steps, int startMomentum) =>
		MovementExpectations.PureForwardMove(PlayerId, origin, steps, startMomentum).Steps;
}
