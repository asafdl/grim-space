using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Dfs;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;
using GrimSpace.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Tests.Actions;

public sealed class InvariantTests
{
	private const string PlayerId = "player";

	[Fact]
	public void ShortMovePathAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Single(actions);
	}

	[Fact]
	public void CompleteMovePathSetsOkAndAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 0);

		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Equal(3, actions.Count);
	}

	[Fact]
	public void MovePathWithOnlyTwoApSpentAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin, momentum: 1);
		var option = MovementExpectations.PureForwardMove(PlayerId, origin, stepCount: 3, startMomentum: 1);

		Assert.Equal(2, option.PathApSpent);
		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		Assert.Equal(2, battle.PlayerAgent.Sim.RuntimeFor(PlayerId).ActivePath!.PathApSpent);
		Assert.Equal(2, battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).ActionPoints);
		Assert.True(battle.PlayerAgent.Sim.RuntimeFor(PlayerId).ActivePath!.CanEnd(
			battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).Stats.MinPathApCost));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
	}

	[Fact]
	public void DeadEndMovePathStillAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var trapped = origin + Coord.Forward;
		var player = BattleTestFixture.Player(origin);
		var enemy = BattleTestFixture.Enemy(new Coord(0, 0, 0));
		var blocked = new HashSet<Coord>
		{
			enemy.State.Position,
			origin,
			trapped + Coord.Forward,
			trapped + Coord.Cross(Coord.Up, Coord.Forward),
			trapped - Coord.Cross(Coord.Up, Coord.Forward),
			trapped + Coord.Up,
			trapped - Coord.Up,
		};
		var battle = BattleTestFixture.BeginSimulation(player, enemy, blocked: blocked);

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
	}

	[Fact]
	public void SearchNeverYieldsNonTerminalMovePathsForShips()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var session = battle.PlayerAgent.Sim;

		foreach (var frame in ActionSearch.Run(session, PlayerId, [MoveDef.Instance], BattleSearchVisit.ForCapabilities))
		{
			var runtime = frame.Runtimes.For(PlayerId);
			if (runtime.ActivePath is null)
				continue;

			Assert.True(runtime.ActivePath.CanEnd(frame.World.StateOf(PlayerId).Stats.MinPathApCost));
		}
	}

	[Fact]
	public void StreamlineCollapsesYawPairsOnCommit()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(2, battle.PlayerAgent.Sim.Actions.Count);

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));
		Assert.Empty(actions);
	}

	[Fact]
	public void StreamlineCollapsesDoubleYawIntoSingleTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));

		var heading = Assert.Single(actions.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
	}

	[Fact]
	public void ShortMovePathAllowsEndTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Forward)));
		battle.EndTurn();
		Assert.Equal(EBattlePhase.Resolving, battle.Phase);
	}

	[Fact]
	public void UndoRefreshesInvariantStatusFromLastMove()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new MoveStepAction(PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Ok, status);

		Assert.True(battle.PlayerAgent.Sim.TryUndoLast());
		Assert.True(battle.PlayerAgent.Sim.TryCommit(out _, out status));
		Assert.Equal(InvariantStatus.Ok, status);
	}

	[Fact]
	public void CommitStreamlinesHeadingActions()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var streamlined = HeadingDef.Instance.Streamline(battle.PlayerAgent.Sim.Actions, battle.PlayerAgent.Sim.UndoGroups).ToList();
		var heading = Assert.Single(streamlined.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
	}

	[Fact]
	public void StreamlineCollapsesTripleRollIntoOpposite()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));

		var roll = Assert.Single(actions.OfType<RollAction>());
		Assert.Equal(ERollDirection.CounterClockwise, roll.Direction);
	}

	[Fact]
	public void StreamlineCollapsesTripleYawIntoOpposite()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));

		var heading = Assert.Single(actions.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.YawLeft, heading.Turn);
	}

	[Fact]
	public void CompactQueueCollapsesYawWhilePlanning()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		OrientationStreamline.CompactQueue(battle.PlayerAgent.Sim);

		var heading = Assert.Single(battle.PlayerAgent.Sim.Actions.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.YawLeft, heading.Turn);
		Assert.Equal(
			Stats.ForType(EType.Fighter).MaxAp - CombatConfig.HeadingTurn90ApCost,
			battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).ActionPoints);
	}

	[Fact]
	public void CompactQueueCollapsesRollWhilePlanning()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));
		Assert.True(battle.PlayerAgent.Sim.TryEnqueue(new RollAction(PlayerId, ERollDirection.Clockwise)));

		OrientationStreamline.CompactQueue(battle.PlayerAgent.Sim);

		var roll = Assert.Single(battle.PlayerAgent.Sim.Actions.OfType<RollAction>());
		Assert.Equal(ERollDirection.CounterClockwise, roll.Direction);
		Assert.Equal(
			Stats.ForType(EType.Fighter).MaxAp - CombatConfig.RollApCost,
			battle.PlayerAgent.Sim.StateOf<ActorState>(PlayerId).ActionPoints);
	}
}
