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
		var ui = BattleTestFixture.Ui(battle);

		var cached = ui.MoveUi.GetMoveOptions([]);
		var expected = MoveUiExpectations.FromFreshSearch(battle.Sim, battle.PlayerId, []);

		MoveUiExpectations.AssertEquivalent(expected, cached);
	}

	[Fact]
	public void ClickedMoveLocatesCommittedQueueAndShowsAffordableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));
		Assert.Equal(3, battle.Sim.Actions.Count);
		Assert.All(battle.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));

		Assert.True(ui.MoveUi.TryLocate(battle.Sim.Actions));
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.Sim.RuntimeFor(battle.PlayerId).IsMovePathStarted);
		Assert.Equal(1, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var extensions = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void HeadingThenMoveClickLocatesFullCommittedQueue()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);
		var turnStartIndex = MoveUiExpectations.CaptureTurnStartIndex(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		var headingOptions = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromIndex(turnStartIndex, battle.Sim.Actions),
			headingOptions);
		Assert.NotEmpty(headingOptions);

		var end = headingOptions[0].EndPosition;
		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));

		Assert.True(ui.MoveUi.TryLocate(battle.Sim.Actions));
		Assert.Equal(4, battle.Sim.Actions.Count);
		Assert.IsType<HeadingTurnAction>(battle.Sim.Actions[0]);
		Assert.All(battle.Sim.Actions.Skip(1), action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.Sim.RuntimeFor(battle.PlayerId).IsMovePathStarted);
		Assert.Empty(ui.MoveUi.GetMoveOptions(battle.Sim.Actions));
	}

	[Fact]
	public void MoveThenHeadingClickLocatesFullCommittedQueue()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));
		var extensions = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
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
		var ui = BattleTestFixture.Ui(battle);
		var turnStartIndex = MoveUiExpectations.CaptureTurnStartIndex(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));
		var frameAfterMove = ui.BuildFrame();
		Assert.NotEmpty(frameAfterMove.MoveOptions);
		Assert.Contains(
			frameAfterMove.MoveOptions,
			option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.True(ui.Undo());

		Assert.Empty(battle.Sim.Actions);
		Assert.True(ui.MoveUi.TryLocate([]));
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromIndex(turnStartIndex, []),
			ui.MoveUi.GetMoveOptions([]));
		Assert.Empty(ui.State.CommittedMovePath);
		Assert.Equal(origin, ui.BuildFrame().ActorState.Position);
	}

	[Fact]
	public void UnknownCommittedPrefixCannotBeLocated()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);

		var yawRight = new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight);
		var impossiblePrefix = new IAction[] { yawRight, yawRight };

		Assert.False(ui.MoveUi.TryLocate(impossiblePrefix));
		Assert.Empty(ui.MoveUi.GetMoveOptions(impossiblePrefix));
	}

	[Fact]
	public void WeaponQueuedBlocksMoveOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);

		Assert.NotEmpty(ui.MoveUi.GetMoveOptions([]));
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(battle.PlayerId)));

		Assert.Empty(ui.MoveUi.GetMoveOptions(battle.Sim.Actions));
	}

	[Fact]
	public void IncompleteMidPathShowsDilutedExtensionOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);
		var turnStartIndex = MoveUiExpectations.CaptureTurnStartIndex(battle);
		var rootEndpoints = ui.MoveUi.GetMoveOptions([])
			.Select(option => option.EndPosition)
			.ToHashSet();

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));

		var runtime = battle.Sim.RuntimeFor(battle.PlayerId);
		Assert.Equal(2, battle.Sim.Actions.Count);
		Assert.Equal(2, MovementExpectations.FighterApPerTurn - battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
		Assert.True(runtime.IsMovePathStarted);
		Assert.False(MovePathRules.CanEndMovePath(runtime));
		Assert.True(ui.MoveUi.TryLocate(battle.Sim.Actions));

		var diluted = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromIndex(turnStartIndex, battle.Sim.Actions),
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
		var ui = BattleTestFixture.Ui(battle);
		var turnStartIndex = MoveUiExpectations.CaptureTurnStartIndex(battle);
		var longestRoot = ui.MoveUi.GetMoveOptions([])
			.MaxBy(option => option.Path.Count)!;

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		Assert.Equal(3, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var diluted = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		MoveUiExpectations.AssertEquivalent(
			MoveUiExpectations.FromIndex(turnStartIndex, battle.Sim.Actions),
			diluted);

		Assert.NotEmpty(diluted);
		Assert.DoesNotContain(longestRoot.EndPosition, diluted.Select(option => option.EndPosition));
	}

	[Fact]
	public void ShortApMoveClickShowsDilutedFollowUpOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);
		var state = ui.State;

		var shortMove = ui.MoveUi.GetMoveOptions([])
			.First(option => option.ApCost == 2);
		Assert.Equal(2, shortMove.Path.Count);
		var actorState = battle.Sim.StateOf<ActorState>(battle.PlayerId);
		var steps = MoveUi.ToMoveActions(battle.PlayerId, actorState, shortMove);
		Assert.NotNull(steps);
		Assert.True(battle.Sim.TryEnqueue(actions: [..steps]));
		state.CommittedMovePath = shortMove.Path;
		state.ClearHovers();
		Assert.Equal(2, battle.Sim.RuntimeFor(battle.PlayerId).PathApSpent);
		Assert.Equal(2, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var followUp = ui.MoveUi.GetMoveOptions(battle.Sim.Actions).ToList();
		Assert.NotEmpty(followUp);
		Assert.True(followUp.Count < ui.MoveUi.GetMoveOptions([]).Count);
		Assert.NotEmpty(ui.BuildFrame().MoveOptions);
	}

	[Fact]
	public void FrameHighlightsCommittedPathAfterMoveClick()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, new Coord(0, 0, 0));
		var ui = BattleTestFixture.Ui(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));

		var frame = ui.BuildFrame();

		Assert.NotEmpty(frame.MoveOptions);
		Assert.Contains(frame.MoveOptions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
		Assert.Equal(end, frame.ActorState.Position);
	}
}
