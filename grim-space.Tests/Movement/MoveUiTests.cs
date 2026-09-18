using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;
using GrimSpace.Tests.Actions;

namespace GrimSpace.Tests.Movement;

public sealed class MoveUiTests
{
	[Fact]
	public void StagedPoseSelectsExactRoute()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.Frame(battle).MovePaths
			.First(path => path.EndPosition == origin + Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.NotNull(frame.SelectedMove);
		Assert.Equal(option.EndPosition, frame.SelectedMove.EndPosition);
		Assert.Equal(option.EndBasis, frame.SelectedMove.EndBasis);
		Assert.Equal(option.Steps, frame.SelectedMove.Steps);
		Assert.Contains(option.EndBasis.Forward, frame.ReachableMoveHeadings);
		Assert.NotNull(frame.MoveGhostState);
		Assert.Equal(option.ResultState.Position, frame.MoveGhostState.Position);
		Assert.Equal(option.ResultState.Fore, frame.MoveGhostState.Fore);
		Assert.Equal(option.ResultState.Dorsal, frame.MoveGhostState.Dorsal);
		Assert.Equal(option.ResultState.ActionPoints, frame.MoveGhostState.ActionPoints);
		Assert.False(frame.Instruction.Visible);
	}

	[Fact]
	public void UnreachableRequestedPoseDoesNotCreateGhost()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.Frame(battle).MovePaths
			.First(path => path.EndPosition == origin + Coord.Forward);
		var unreachable = MovePose.For(-Coord.Forward, 0);

		builder.Interaction.BeginMoveSelection(option.EndPosition, unreachable);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Null(frame.SelectedMove);
		Assert.Null(frame.MoveGhostState);
		Assert.False(frame.Instruction.Visible);
	}

	[Fact]
	public void PassiveHoverShowsHoveredResultAsGhostWithoutOrientationSelection()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.Frame(battle).MovePaths;
		var hovered = options
			.First(option => option.EndPosition == origin + Coord.Forward);

		builder.Interaction.SetMoveHover(hovered.EndPosition, options);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Null(frame.SelectedMove);
		Assert.Equal(hovered.ResultState.Position, frame.MoveGhostState?.Position);
		Assert.Equal(hovered.ResultState.Fore, frame.MoveGhostState?.Fore);
		Assert.Empty(frame.ReachableMoveHeadings);
	}

	[Fact]
	public void SelectedMoveGhostTakesPriorityOverPassiveHover()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.Frame(battle).MovePaths;
		var selected = options.First(option => option.EndPosition == origin + Coord.Forward);
		var hovered = options
			.First(option => option.EndPosition != selected.EndPosition);

		builder.Interaction.SetMoveHover(hovered.EndPosition, options);
		builder.Interaction.BeginMoveSelection(selected.EndPosition, selected.EndBasis);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(selected.EndPosition, frame.MoveGhostState?.Position);
		Assert.Equal(selected.EndBasis.Forward, frame.MoveGhostState?.Fore);
		Assert.Contains(selected.EndBasis.Forward, frame.ReachableMoveHeadings);
	}

	[Fact]
	public void PassiveHoverGhostIsHiddenWhenCommandsAreDisabled()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.Frame(battle).MovePaths;

		builder.Interaction.SetMoveHover(options[0].EndPosition, options);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: false);

		Assert.Null(frame.MoveGhostState);
	}

	[Fact]
	public void SelectedMoveGhostIsHiddenWhenCommandsAreDisabled()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.Frame(battle).MovePaths
			.First(path => path.EndPosition == origin + Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: false);

		Assert.Null(frame.MoveGhostState);
	}

	[Fact]
	public void FailedMoveQueueKeepsStagedPoseAndEndsDrag()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.Frame(battle).MovePaths
			.First(path => path.EndPosition == origin + Coord.Forward);
		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);

		builder.Interaction.ReportActionFailure();
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(option.EndPosition, frame.MoveDestination);
		Assert.Equal(option.EndBasis, frame.SelectedMove?.EndBasis);
		Assert.False(frame.IsMoveDragging);
		Assert.True(frame.Instruction.Visible);
		Assert.Equal(BattleHudCopy.ActionUnavailable, frame.Instruction.Label);
	}

	[Fact]
	public void HeadingSelectionRejectsUnreachableDirection()
	{
		var current = MovePose.For(Coord.Forward, 0);
		var right = new Coord(1, 0, 0);
		var reachable = new[]
		{
			current,
			MovePose.For(right, 1),
			MovePose.For(right, 3),
		};

		var selected = MovePose.SelectHeading(reachable, current, -Coord.Forward);

		Assert.Null(selected);
	}

	[Fact]
	public void WheelRollSkipsUnreachablePoses()
	{
		var roll0 = MovePose.For(Coord.Forward, 0);
		var roll1 = MovePose.For(Coord.Forward, 1);
		var roll3 = MovePose.For(Coord.Forward, 3);
		var reachable = new[] { roll0, roll1, roll3 };

		Assert.Equal(roll3, MovePose.CycleRoll(reachable, roll1, 1));
		Assert.Equal(roll0, MovePose.CycleRoll(reachable, roll1, -1));
	}

	[Fact]
	public void CancelMoveSelectionClearsStagedPose()
	{
		var state = new InteractionState();
		state.BeginMoveSelection(new Coord(1, 2, 3), MovePose.For(Coord.Forward, 0));

		state.ClearMoveSelection();

		Assert.Null(state.MoveDestination);
		Assert.Null(state.RequestedMoveBasis);
		Assert.False(state.IsMoveDragging);
	}

	[Fact]
	public void MultipleBasesAtEndpointUseFirstRoutePreference()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.Frame(battle).MovePaths;
		var destination = options
			.GroupBy(option => option.EndPosition)
			.First(group => group.Count() > 1)
			.Key;
		var preferred = options.First(option => option.EndPosition == destination);

		builder.Interaction.SetMoveHover(destination, options);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(preferred.EndBasis, frame.HoveredMove?.EndBasis);
		Assert.Equal(preferred.EndPosition, frame.MoveTarget);
		Assert.Equal(preferred.ResultState.Position, frame.MoveGhostState?.Position);
	}

	[Fact]
	public void HoveredRouteGhostPathAndTargetAgree()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var hovered = BattleTestCommands.Frame(battle).MovePaths
			.First(option => option.EndPosition == origin + Coord.Forward);

		builder.Interaction.SetMoveHover(hovered.EndPosition, BattleTestCommands.Frame(battle).MovePaths);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Equal(hovered.EndPosition, frame.MoveTarget);
		Assert.Equal(hovered.Checkpoints.Skip(1), frame.MoveCheckpoints);
		Assert.Equal(hovered.ResultState.Position, frame.MoveGhostState?.Position);
		Assert.Equal(hovered.EndBasis, frame.HoveredMove?.EndBasis);
	}

	[Fact]
	public void StagedMoveSelectionShowsPoseHitOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.MoveOptions(battle)
			.First(path =>
				path.EndPosition == origin
				&& path.EndBasis.Forward == Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.NotEmpty(frame.PoseHitOpportunities);
		Assert.Contains(
			frame.PoseHitOpportunities,
			opportunity => opportunity.IconPath == "res://assets/ui/abilities/railgun.svg");
	}

	[Fact]
	public void PassiveHoverDoesNotShowPoseHitOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var options = BattleTestCommands.MoveOptions(battle);
		var hovered = options.First(option => option.EndPosition == origin + Coord.Forward);

		builder.Interaction.SetMoveHover(hovered.EndPosition, options);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Empty(frame.PoseHitOpportunities);
	}

	[Fact]
	public void PoseHitOpportunitiesHiddenWhenCommandsDisabled()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.MoveOptions(battle)
			.First(path =>
				path.EndPosition == origin
				&& path.EndBasis.Forward == Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: false);

		Assert.Empty(frame.PoseHitOpportunities);
	}

	[Fact]
	public void ConfirmedMoveKeepsPoseHitOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.MoveOptions(battle)
			.First(path =>
				path.EndPosition == origin
				&& path.EndBasis.Forward == Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);
		Assert.NotEmpty(builder.Interaction.PoseHitOpportunities);

		builder.Interaction.CompleteMoveSelection();
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.NotEmpty(frame.PoseHitOpportunities);
		Assert.False(frame.IsMoveDragging);
	}

	[Fact]
	public void CancelMoveSelectionClearsPoseHitOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.MoveOptions(battle)
			.First(path =>
				path.EndPosition == origin
				&& path.EndBasis.Forward == Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);
		Assert.NotEmpty(builder.Interaction.PoseHitOpportunities);

		builder.Interaction.ClearMoveSelection();
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Empty(frame.PoseHitOpportunities);
	}

	[Fact]
	public void InspectingHidesPoseHitOpportunities()
	{
		var origin = new Coord(5, 5, 5);
		var battle = TurnOrchestrationTests.CreateOrchestrator(
			origin,
			TurnOrchestrationTests.EnemyInRailgunLine(origin));
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = BattleTestCommands.MoveOptions(battle)
			.First(path =>
				path.EndPosition == origin
				&& path.EndBasis.Forward == Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		builder.Interaction.FocusUnit(BattleTestFixture.FirstEnemyId(battle));
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Empty(frame.PoseHitOpportunities);
	}

	[Fact]
	public void SelectedRouteTakesPriorityOverHoverHighlight()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var paths = BattleTestCommands.Frame(battle).MovePaths;
		var selected = paths.First(path => path.EndPosition == origin + Coord.Forward * 2);

		var (checkpoints, target) = MoveUi.GetPathHighlights(paths, paths[0], [], selected);

		Assert.Equal(selected.Checkpoints.Skip(1), checkpoints);
		Assert.Equal(selected.EndPosition, target);
	}
}
