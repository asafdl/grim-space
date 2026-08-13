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

		Assert.True(MoveUiTestActions.ClickMove(battle, end));
		Assert.Equal(3, battle.PlayerAgent.Sim.Actions.Count);
		Assert.All(battle.PlayerAgent.Sim.Actions, action => Assert.IsType<MoveStepAction>(action));

		Assert.Equal(end, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId).ActivePath != null);
		Assert.Equal(1, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var extensions = BattleTestCommands.MoveOptions(battle).ToList();
		Assert.Contains(extensions, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
	}

	[Fact]
	public void HeadingThenMoveClickQueuesFullCommittedActions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		var headingOptions = BattleTestCommands.MoveOptions(battle).ToList();
		Assert.NotEmpty(headingOptions);

		var end = headingOptions[0].EndPosition;
		Assert.True(MoveUiTestActions.ClickMove(battle, end));

		Assert.Equal(4, battle.PlayerAgent.Sim.Actions.Count);
		Assert.IsType<HeadingTurnAction>(battle.PlayerAgent.Sim.Actions[0]);
		Assert.All(battle.PlayerAgent.Sim.Actions.Skip(1), action => Assert.IsType<MoveStepAction>(action));
		Assert.Equal(end, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.True(battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId).ActivePath != null);
		Assert.Empty(BattleTestCommands.MoveOptions(battle));
	}

	[Fact]
	public void MoveThenHeadingClickQueuesFullCommittedActions()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.True(MoveUiTestActions.ClickMove(battle, end));
		var extensions = BattleTestCommands.MoveOptions(battle).ToList();
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

		Assert.True(MoveUiTestActions.ClickMove(battle, end));
		var frameAfterMove = BattleTestCommands.Frame(battle);
		Assert.NotEmpty(frameAfterMove.MovePaths);
		Assert.Contains(
			frameAfterMove.MovePaths,
			option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.True(BattleTestCommands.Undo(battle));

		Assert.Empty(battle.PlayerAgent.Sim.Actions);
		Assert.NotEmpty(BattleTestCommands.MoveOptions(battle));
		Assert.Empty(new PlanningPreview().CommittedMovePath(battle.PlayerAgent.Sim, battle.PlayerId));
		Assert.Equal(origin, BattleTestCommands.Frame(battle).FocusState.Position);
	}

	[Fact]
	public void UndoReturnsCachedRootPathsWithoutReSearching()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		var rootEndpoints = BattleTestCommands.DiscoverPaths(battle)
			.Select(path => path.EndPosition)
			.ToHashSet();
		Assert.True(BattleTestCommands.Move(battle, end));
		_ = BattleTestCommands.MoveOptions(battle);
		Assert.True(BattleTestCommands.Undo(battle));

		var restoredEndpoints = BattleTestCommands.DiscoverPaths(battle)
			.Select(path => path.EndPosition)
			.ToHashSet();
		Assert.Equal(rootEndpoints, restoredEndpoints);
	}

	[Fact]
	public void WeaponQueuedStillAllowsMoveOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.NotEmpty(BattleTestCommands.MoveOptions(battle));
		Assert.True(BattleTestCommands.FireRailgun(battle));

		Assert.NotEmpty(BattleTestCommands.DiscoverPaths(battle));
	}

	[Fact]
	public void MidPathShowsDilutedExtensionOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var rootEndpoints = BattleTestCommands.DiscoverPaths(battle)
			.Select(option => option.EndPosition)
			.ToHashSet();

		Assert.True(BattleTestCommands.Move(battle, origin + Coord.Forward * 2));

		var runtime = battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId);
		Assert.Equal(2, battle.PlayerAgent.Sim.Actions.Count);
		Assert.Equal(2, MovementExpectations.FighterApPerTurn - battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);
		Assert.True(runtime.ActivePath != null);
		Assert.True(runtime.ActivePath!.CanEnd(
			battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Stats.MinPathApCost));

		var diluted = BattleTestCommands.DiscoverPaths(battle).ToList();

		Assert.NotEmpty(diluted);
		Assert.True(diluted.Count < rootEndpoints.Count);

		var dilutedEnds = diluted.Select(option => option.EndPosition).ToHashSet();
		Assert.DoesNotContain(origin + Coord.Forward, dilutedEnds);
		Assert.DoesNotContain(origin + Coord.Forward * 2, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 3, dilutedEnds);
		Assert.Contains(origin + Coord.Forward * 4, dilutedEnds);
		Assert.Equal(origin + Coord.Forward * 2, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).Position);
		Assert.Contains(diluted, option => option.EndPosition == origin + Coord.Forward * 3 && option.CanEndPath);
	}

	[Fact]
	public void SpentApOnHeadingDilutesMoveEndpoints()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var longestRoot = BattleTestCommands.DiscoverPaths(battle)
			.MaxBy(option => option.Cells.Count)!;

		Assert.True(MoveUiTestActions.ClickHeading(battle, EHeadingTurn.YawRight));
		Assert.Equal(3, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var diluted = BattleTestCommands.DiscoverPaths(battle).ToList();

		Assert.NotEmpty(diluted);
		Assert.DoesNotContain(longestRoot.EndPosition, diluted.Select(option => option.EndPosition));
	}

	[Fact]
	public void ShortApMoveClickShowsDilutedFollowUpOptions()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		var shortMove = BattleTestCommands.DiscoverPaths(battle)
			.First(option => option.PathApSpent == 2);
		Assert.Equal(2, shortMove.Cells.Count);
		Assert.True(BattleTestCommands.Move(battle, shortMove.EndPosition));
		Assert.Equal(2, battle.PlayerAgent.Sim.RuntimeFor(battle.PlayerId).ActivePath!.PathApSpent);
		Assert.Equal(2, battle.PlayerAgent.Sim.StateOf<ActorState>(battle.PlayerId).ActionPoints);

		var followUp = BattleTestCommands.DiscoverPaths(battle).ToList();
		Assert.NotEmpty(followUp);
		Assert.True(followUp.Count < BattleTestCommands.DiscoverPaths(
			BattleTestFixture.BeginSimulation(origin)).Count);
		Assert.NotEmpty(BattleTestCommands.Frame(battle).MovePaths);
	}

	[Fact]
	public void FrameHighlightsCommittedPathAfterMoveClick()
	{
		var origin = new Coord(5, 5, 5);
		var end = origin + Coord.Forward * 3;
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		Assert.True(MoveUiTestActions.ClickMove(battle, end));

		var frame = BattleTestCommands.Frame(battle);

		Assert.NotEmpty(frame.MovePaths);
		Assert.Contains(frame.MovePaths, option => option.EndPosition == origin + Coord.Forward * 4);
		Assert.Equal(end, frame.MoveTarget);
		Assert.Equal(3, frame.MovePath.Count);
		Assert.Equal(end, frame.FocusState.Position);
	}

	[Fact]
	public void InspectionPreviewDoesNotPoisonPlayerCache()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));

		var playerEndpoints = BattleTestCommands.DiscoverPaths(battle)
			.Select(path => path.EndPosition)
			.ToHashSet();
		_ = BattleTestCommands.DiscoverPaths(battle, BattleTestFixture.FirstEnemyId(battle));

		var restoredEndpoints = BattleTestCommands.DiscoverPaths(battle)
			.Select(path => path.EndPosition)
			.ToHashSet();
		Assert.Equal(playerEndpoints, restoredEndpoints);
	}

	[Fact]
	public void RootMoveOptionsDoNotBorrowPathsAcrossFutureYaw()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(origin, TurnOrchestrationTests.EnemyInRailgunLine(origin));

		var rootSim = battle.Engine.CreateSimulation();
		var yawSim = battle.Engine.CreateSimulation();
		Assert.True(yawSim.TryEnqueue(new HeadingTurnAction(battle.PlayerId, EHeadingTurn.YawRight)));

		var yawedPaths = MovePathEndpoints.DiscoverExtensions(yawSim, battle.PlayerId).ToList();
		var rootPaths = MovePathEndpoints.DiscoverExtensions(rootSim, battle.PlayerId).ToList();

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
