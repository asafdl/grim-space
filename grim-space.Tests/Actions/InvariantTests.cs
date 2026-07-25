using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Planning;
using GrimSpace.Battle.Presentation.Ui;
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
		var battle = BattleTestFixture.BeginPlanning(origin);

		Assert.True(battle.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Session.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Incomplete, status);
	}

	[Fact]
	public void CompleteMovePathSetsOkAndAllowsCommit()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var option = MovementExpectations.PureForwardMove(origin, stepCount: 3, startMomentum: 0);

		Assert.True(battle.TryEnqueueMovePath(option));
		Assert.True(battle.Session.TryCommit(out var actions, out var status));
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
		var battle = BattleTestFixture.BeginPlanning(player, enemy, blocked: blocked);

		Assert.True(battle.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Session.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Impossible, status);
	}

	[Fact]
	public void SearchSkipsImpossibleBranchesButKeepsIncompleteFrames()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginPlanning(origin);
		var session = battle.Session;
		var sawIncomplete = false;
		var sawImpossibleEndpoint = false;

		foreach (var frame in session.SearchMoves(PlayerId))
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
	public void StreamlineRebuildsFreshSessionAndCollapsesYawPairs()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawLeft)));
		Assert.Equal(2, battle.Actions.Count);

		var before = battle.Session;
		Assert.True(battle.TryCommitPreview(out var actions));
		Assert.NotSame(before, battle.Session);
		Assert.Empty(actions);
	}

	[Fact]
	public void StreamlineCollapsesDoubleYawIntoSingleTurn()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		Assert.True(battle.TryCommitPreview(out var actions));

		var heading = Assert.Single(actions.OfType<HeadingTurnAction>());
		Assert.Equal(EHeadingTurn.Yaw180, heading.Turn);
	}

	[Fact]
	public void EndTurnFailsWhenInvariantStatusIsNotOk()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		var presenter = new BattlePresenter(battle);

		Assert.True(battle.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(presenter.EndTurn());
	}

	[Fact]
	public void UndoRefreshesInvariantStatusFromLastMove()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));

		Assert.True(battle.TryEnqueue(new MoveStepAction(PlayerId, EStepDirection.Forward)));
		Assert.False(battle.Session.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Incomplete, status);

		Assert.True(battle.TryUndoLast());
		Assert.True(battle.Session.TryCommit(out _, out status));
		Assert.Equal(InvariantStatus.Ok, status);
	}

	[Fact]
	public void ActionListStreamlineReplaysOntoFreshSimulation()
	{
		var battle = BattleTestFixture.BeginPlanning(new Coord(5, 5, 5));
		Assert.True(battle.TryEnqueue(new HeadingTurnAction(PlayerId, EHeadingTurn.YawRight)));

		var streamlined = ActionListStreamline.Apply(
			battle.Engine,
			battle.Session,
			BattleActionStreamliners.All);

		Assert.Single(streamlined.Actions);
		Assert.NotSame(battle.Session, streamlined);
		Assert.True(streamlined.TryCommit(out _, out var status));
		Assert.Equal(InvariantStatus.Ok, status);
	}
}
