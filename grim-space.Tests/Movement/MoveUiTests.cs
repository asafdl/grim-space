using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Movement;

public sealed class MoveUiTests
{
	[Fact]
	public void StagedPoseSelectsExactRoute()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = builder.PreviewMoveOptions(battle, battle.PlayerAgent)
			.First(path => path.EndPosition == origin + Coord.Forward);

		builder.Interaction.BeginMoveSelection(option.EndPosition, option.EndBasis);
		builder.Interaction.EndMoveDrag();
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.NotNull(frame.SelectedMove);
		Assert.Equal(option.EndPosition, frame.SelectedMove.EndPosition);
		Assert.Equal(option.EndBasis, frame.SelectedMove.EndBasis);
		Assert.Equal(option.Steps, frame.SelectedMove.Steps);
		Assert.True(frame.MovePoseAvailable);
		Assert.NotNull(frame.MoveGhostState);
		Assert.Equal(option.ResultState.Position, frame.MoveGhostState.Position);
		Assert.Equal(option.ResultState.Fore, frame.MoveGhostState.Fore);
		Assert.Equal(option.ResultState.Dorsal, frame.MoveGhostState.Dorsal);
		Assert.Equal(option.ResultState.ActionPoints, frame.MoveGhostState.ActionPoints);
		Assert.True(frame.Instruction.CanConfirm);
	}

	[Fact]
	public void UnreachableRequestedPoseKeepsGhostAndDisablesConfirmation()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = builder.PreviewMoveOptions(battle, battle.PlayerAgent)
			.First(path => path.EndPosition == origin + Coord.Forward);
		var unreachable = MovePose.For(-Coord.Forward, 0);

		builder.Interaction.BeginMoveSelection(option.EndPosition, unreachable);
		builder.Interaction.EndMoveDrag();
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Null(frame.SelectedMove);
		Assert.False(frame.MovePoseAvailable);
		Assert.NotNull(frame.MoveGhostState);
		Assert.False(frame.Instruction.CanConfirm);
	}

	[Fact]
	public void WheelRollCyclesBackAfterFourQuarters()
	{
		var state = new InteractionState();
		var start = MovePose.For(Coord.Forward, 0);
		state.BeginMoveSelection(new Coord(1, 2, 3), start);

		for (var i = 0; i < 4; i++)
			state.RollMove(1);

		Assert.Equal(start, state.RequestedMoveBasis);
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
	public void SelectedRouteTakesPriorityOverHoverHighlight()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var paths = BattleTestFixture.FrameBuilder(battle)
			.PreviewMoveOptions(battle, battle.PlayerAgent);
		var selected = paths.First(path => path.EndPosition == origin + Coord.Forward * 2);

		var (cells, target) = MoveUi.GetPathHighlights(paths, hoveredIndex: 0, [], selected);

		Assert.Equal(selected.Cells, cells);
		Assert.Equal(selected.EndPosition, target);
	}
}
