using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MoveUiTests
{
	[Fact]
	public void CachedOptionsMatchTurnStartFrames()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var cached = battle.MoveUi.GetMoveOptions([]);
		var expected = MoveUiExpectations.FromFreshSearch(battle.Sim, battle.PlayerId, []);

		MoveUiExpectations.AssertEquivalent(expected, cached);
	}

	[Fact]
	public void ClickedMoveLocatesCommittedQueueAndShowsAffordableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(battle, state, end));
		Assert.Equal(3, battle.Sim.Actions.Count);
		Assert.All(battle.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));

		Assert.True(battle.MoveUi.TryLocate(battle.Sim.Actions, out var node));
		Assert.Equal(end, node.World.StateOf(battle.PlayerId).Position);
		Assert.True(node.Runtimes.For(battle.PlayerId).IsMovePathStarted);
		Assert.Equal(1, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var extensions = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void HeadingThenMoveClickLocatesFullCommittedQueue()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var turnStartFrames = MoveUiExpectations.CaptureTurnStartFrames(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		var headingOptions = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromFrames(battle.PlayerId, turnStartFrames, battle.Sim.Actions),
			headingOptions);
		Assert.NotEmpty(headingOptions);

		var end = headingOptions[0].EndPosition;
		Assert.True(MoveUiTestActions.ClickMove(battle, state, end));

		Assert.True(battle.MoveUi.TryLocate(battle.Sim.Actions, out var node));
		Assert.Equal(4, battle.Sim.Actions.Count);
		Assert.IsType<HeadingTurnAction>(battle.Sim.Actions[0]);
		Assert.All(battle.Sim.Actions.Skip(1), action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(end, node.World.StateOf(battle.PlayerId).Position);
		Assert.True(node.Runtimes.For(battle.PlayerId).IsMovePathStarted);
		Assert.Empty(battle.MoveUi.GetMoveOptions(battle.Sim.Actions));
	}

	[Fact]
	public void MoveThenHeadingClickLocatesFullCommittedQueue()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(battle, state, end));
		var extensions = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.NotEmpty(extensions);
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
	}

	[Fact]
	public void UndoAfterMoveClickRestoresRootLocateAndOptions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = new BattleUi(battle);
		var turnStartFrames = MoveUiExpectations.CaptureTurnStartFrames(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));
		var frameAfterMove = ui.BuildFrame();
		Assert.NotEmpty(frameAfterMove.MoveOptions);
		Assert.Contains(
			frameAfterMove.MoveOptions,
			option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.True(ui.Undo());

		Assert.Empty(battle.Sim.Actions);
		Assert.True(battle.MoveUi.TryLocate([], out _));
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromFrames(battle.PlayerId, turnStartFrames, []),
			battle.MoveUi.GetMoveOptions([]));
		Assert.Empty(ui.State.CommittedMovePath);
		Assert.Equal(origin, ui.BuildFrame().ActorState.Position);
	}

	[Fact]
	public void UnknownCommittedPrefixCannotBeLocated()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		var yawRight = new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight);
		var impossiblePrefix = new IAction[] { yawRight, yawRight };

		Assert.False(battle.MoveUi.TryLocate(impossiblePrefix, out _));
		Assert.Empty(battle.MoveUi.GetMoveOptions(impossiblePrefix));
	}

	[Fact]
	public void WeaponQueuedBlocksMoveOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));

		Assert.NotEmpty(battle.MoveUi.GetMoveOptions([]));
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(battle.PlayerId)));

		Assert.Empty(battle.MoveUi.GetMoveOptions(battle.Sim.Actions));
	}

	[Fact]
	public void IncompleteMidPathShowsDilutedExtensionOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var turnStartFrames = MoveUiExpectations.CaptureTurnStartFrames(battle);
		var rootEndpoints = battle.MoveUi.GetMoveOptions([])
			.Select(option => option.EndPosition)
			.ToHashSet();

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));

		var runtime = battle.Sim.RuntimeFor(battle.PlayerId);
		Assert.Equal(2, battle.Sim.Actions.Count);
		Assert.Equal(2, MovementExpectations.FighterApPerTurn - battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
		Assert.True(runtime.IsMovePathStarted);
		Assert.False(MovePathRules.CanEndMovePath(runtime));
		Assert.True(battle.MoveUi.TryLocate(battle.Sim.Actions, out _));

		var diluted = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromFrames(battle.PlayerId, turnStartFrames, battle.Sim.Actions),
			diluted);

		Assert.NotEmpty(diluted);
		Assert.True(diluted.Count < rootEndpoints.Count);

		var dilutedEnds = diluted.Select(option => option.EndPosition).ToHashSet();
		Assert.DoesNotContain(origin + Coord.Forward, dilutedEnds);
		Assert.DoesNotContain(origin + Coord.Forward * 2, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 3, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 4, dilutedEnds);
		Assert.Equal(origin + Coord.Forward * 2, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.All(diluted, option => Assert.True(
			MovementExpectations.IsValidMoveEndpoint(
				battle.Sim.RuntimeFor(battle.PlayerId).PathApSpent + option.ApCost)));
	}

	[Fact]
	public void SpentApOnHeadingDilutesMoveEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var turnStartFrames = MoveUiExpectations.CaptureTurnStartFrames(battle);
		var longestRoot = battle.MoveUi.GetMoveOptions([])
			.MaxBy(option => option.Path.Count)!;

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		Assert.Equal(3, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var diluted = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromFrames(battle.PlayerId, turnStartFrames, battle.Sim.Actions),
			diluted);

		Assert.NotEmpty(diluted);
		Assert.DoesNotContain(longestRoot.EndPosition, diluted.Select(option => option.EndPosition));
	}

	[Fact]
	public void ShortApMoveClickShowsDilutedFollowUpOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = new BattleUi(battle);
		var state = ui.State;

		var shortMove = battle.MoveUi.GetMoveOptions([])
			.First(option => option.ApCost == 2);
		Assert.Equal(2, shortMove.Path.Count);
		Assert.True(MoveUi.TryApply(battle, state, shortMove));
		Assert.Equal(2, battle.Sim.RuntimeFor(battle.PlayerId).PathApSpent);
		Assert.Equal(2, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var followUp = battle.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.NotEmpty(followUp);
		Assert.True(followUp.Count < battle.MoveUi.GetMoveOptions([]).Count);
		Assert.NotEmpty(ui.BuildFrame().MoveOptions);
	}

	[Fact]
	public void FrameHighlightsCommittedPathAfterMoveClick()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = new BattleUi(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));

		var frame = ui.BuildFrame();

		Assert.NotEmpty(frame.MoveOptions);
		Assert.Contains(frame.MoveOptions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
		Assert.Equal(end, frame.ActorState.Position);
	}
}
