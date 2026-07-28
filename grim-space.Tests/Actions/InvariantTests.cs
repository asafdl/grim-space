using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Ai;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Movement;

namespace GrimSpace.Tests.Actions;

public sealed class InvariantTests
{
	private const string PlayerId = "player";

	[Fact]
	public void IncompleteMovePathAllowsEnqueueButBlocksCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Incomplete, status);
	}

	[Fact]
	public void CompleteMovePathSetsOkAndAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);

		Assert.True(BattleTestActions.TryEnqueueMovePath(battle, option));
		Assert.True(battle.Sim.TryCommit(out var actions, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
		Assert.Equal(3, actions.Count);
	}

	[Fact]
	public void StuckMovePathIsImpossibleAndBlocksCommit()
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

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Impossible, status);
	}

	[Fact]
	public void SearchSkipsImpossibleBranchesButKeepsIncompleteFrames()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var session = battle.Sim;
		var sawIncomplete = false;
		var sawImpossibleEndpoint = false;

		foreach (var frame in session.Search(PlayerId, [MoveDef.Instance], BattleSearchVisit.MoveVisit))
		{
			var runtime = frame.Runtimes.For(PlayerId);
			if (!runtime.IsMovePathStarted)
				continue;

			if (MovePathRules.CanEndMovePath(runtime))
				continue;

			var hasContinuation = MoveDef.Instance
				.Discover(frame.World, runtime, PlayerId)
				.Any(candidate => MoveDef.Instance.IsPossible(candidate, frame.World, runtime));

			if (hasContinuation)
				sawIncomplete = true;
			else
				sawImpossibleEndpoint = true;
		}

		Assert.True(sawIncomplete);
		Assert.False(sawImpossibleEndpoint);
	}

	[Fact]
	public void StreamlineCollapsesYawPairsOnCommit()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(2, battle.Sim.Actions.Count);

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));
		Assert.Empty(actions);
	}

	[Fact]
	public void StreamlineCollapsesDoubleYawIntoSingleTurn()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(BattleTestActions.TryCommitPreview(battle, out var actions));

		var heading = Assert.Single(actions.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
	}

	[Fact]
	public void EndTurnFailsWhenInvariantStatusIsNotOk()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		var ui = new BattleUi(battle);

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.Null(ui.CommitAndResolve());
	}

	[Fact]
	public void UndoRefreshesInvariantStatusFromLastMove()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Sim.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Incomplete, status);

		Assert.True(battle.Sim.TryUndoLast());
		Assert.True(battle.Sim.TryCommit(out _, out status));
		Assert.Equal(InvariantStatus.Ok, status);
	}

	[Fact]
	public void CommitStreamlinesHeadingActions()
	{
		var battle = BattleTestFixture.BeginSimulation(new Coord(5, 5, 5));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.Sim.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var streamlined = HeadingDef.Instance.Streamline(battle.Sim.Actions, battle.Sim.UndoGroups).ToList();
		var heading = Assert.Single(streamlined.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
	}
}
