using GrimSpace.Battle;
using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Runtime;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MoveUiTests
{
	[Fact]
	public void ClickedMoveQueuesAndShowsAffordableExtensions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));
		Assert.Equal(3, battle.Sim.Actions.Count);
		Assert.All(battle.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));

		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.Sim.RuntimeFor(battle.PlayerId).ActivePath != null);
		Assert.Equal(1, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var extensions = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void HeadingThenMoveClickQueuesFullCommittedActions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		var headingOptions = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		Assert.NotEmpty(headingOptions);

		var end = headingOptions[0].EndPosition;
		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));

		Assert.Equal(4, battle.Sim.Actions.Count);
		Assert.IsType<HeadingTurnAction>(battle.Sim.Actions[0]);
		Assert.All(battle.Sim.Actions.Skip(1), action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(end, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.Sim.RuntimeFor(battle.PlayerId).ActivePath != null);
		Assert.Empty(ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions));
	}

	[Fact]
	public void MoveThenHeadingClickQueuesFullCommittedActions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var state = new InteractionState();

		Assert.True(MoveUiTestActions.ClickMove(ui, state, end));
		var extensions = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		Assert.NotEmpty(extensions);
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
	}

	[Fact]
	public void UndoAfterMoveClickRestoresRootOptions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));
		var frameAfterMove = ui.BuildFrame();
		Assert.NotEmpty(frameAfterMove.MovePaths);
		Assert.Contains(
			frameAfterMove.MovePaths,
			option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.True(ui.Undo());

		Assert.Empty(battle.Sim.Actions);
		Assert.NotEmpty(ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions));
		Assert.Empty(ui.State.CommittedMovePath);
		Assert.Equal(origin, ui.BuildFrame().ActorState.Position);
	}

	[Fact]
	public void UndoReturnsCachedRootPathsWithoutReSearching()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);

		var rootPaths = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions);
		Assert.True(MoveUiTestActions.ClickMove(ui, end));
		_ = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions);
		Assert.True(ui.Undo());

		Assert.Same(rootPaths, ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions));
	}

	[Fact]
	public void WeaponQueuedBlocksMoveOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);

		Assert.NotEmpty(ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions));
		Assert.True(battle.Sim.TryEnqueue(new RailgunAction(battle.PlayerId)));

		Assert.Empty(ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions));
	}

	[Fact]
	public void MidPathShowsDilutedExtensionOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var rootEndpoints = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions)
			.Select(option => option.EndPosition)
			.ToHashSet();

		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));
		Assert.True(battle.Sim.TryEnqueue(new MoveStepAction(battle.PlayerId, ESpatialOrientation.Forward)));

		var runtime = battle.Sim.RuntimeFor(battle.PlayerId);
		Assert.Equal(2, battle.Sim.Actions.Count);
		Assert.Equal(2, MovementExpectations.FighterApPerTurn - battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
		Assert.True(runtime.ActivePath != null);
		Assert.True(runtime.ActivePath!.CanEnd(
			battle.Sim.StateOf<ActorState>(battle.PlayerId).Stats.MinPathApCost));

		var diluted = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();

		Assert.NotEmpty(diluted);
		Assert.True(diluted.Count < rootEndpoints.Count);

		var dilutedEnds = diluted.Select(option => option.EndPosition).ToHashSet();
		Assert.DoesNotContain(origin + Coord.Forward, dilutedEnds);
		Assert.DoesNotContain(origin + Coord.Forward * 2, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 3, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 4, dilutedEnds);
		Assert.Equal(origin + Coord.Forward * 2, battle.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Contains(diluted, option => option.EndPosition == origin + Coord.Forward * 3 && option.CanEndPath);
	}

	[Fact]
	public void SpentApOnHeadingDilutesMoveEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var longestRoot = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions)
			.MaxBy(option => option.Cells.Count)!;

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		Assert.Equal(3, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var diluted = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();

		Assert.NotEmpty(diluted);
		Assert.DoesNotContain(longestRoot.EndPosition, diluted.Select(option => option.EndPosition));
	}

	[Fact]
	public void ShortApMoveClickShowsDilutedFollowUpOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);
		var state = ui.State;

		var shortMove = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions)
			.First(option => option.PathApSpent == 2);
		Assert.Equal(2, shortMove.Cells.Count);
		Assert.True(battle.Sim.TryEnqueue(actions: [..shortMove.Steps]));
		state.CommittedMovePath = shortMove.Cells;
		state.ClearHovers();
		Assert.Equal(2, battle.Sim.RuntimeFor(battle.PlayerId).ActivePath!.PathApSpent);
		Assert.Equal(2, battle.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var followUp = ui.MoveUi.GetMovePaths(battle.Sim, battle.PlayerId, battle.Sim.Actions).ToList();
		Assert.NotEmpty(followUp);
		Assert.True(followUp.Count < ui.MoveUi.GetMovePaths(battle.Engine.CreateSimulation(), battle.PlayerId, []).Count);
		Assert.NotEmpty(ui.BuildFrame().MovePaths);
	}

	[Fact]
	public void FrameHighlightsCommittedPathAfterMoveClick()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);

		Assert.True(MoveUiTestActions.ClickMove(ui, end));

		var frame = ui.BuildFrame();

		Assert.NotEmpty(frame.MovePaths);
		Assert.Contains(frame.MovePaths, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
		Assert.Equal(end, frame.ActorState.Position);
	}

	[Fact]
	public void RootMoveOptionsDoNotBorrowPathsAcrossFutureYaw()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var ui = BattleTestFixture.Ui(battle);

		var rootSim = battle.Engine.CreateSimulation();
		var yawSim = battle.Engine.CreateSimulation();
		Assert.True(yawSim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));

		var yawedPaths = ui.MoveUi.GetMovePaths(yawSim, battle.PlayerId, yawSim.Actions).ToList();
		var rootPaths = ui.MoveUi.GetMovePaths(rootSim, battle.PlayerId, rootSim.Actions).ToList();

		Assert.NotEmpty(yawedPaths);
		Assert.NotEmpty(rootPaths);

		foreach (var path in rootPaths)
		{
			var trial = rootSim.Fork();
			Assert.True(BattleTestActions.TryEnqueueMovePath(trial, battle.PlayerId, path));
		}

		foreach (var path in yawedPaths)
		{
			var trial = yawSim.Fork();
			Assert.True(BattleTestActions.TryEnqueueMovePath(trial, battle.PlayerId, path));
		}

		Assert.All(rootPaths, path => Assert.All(path.Steps, step => Assert.IsType<MoveStepAction>(step)));
	}
}
