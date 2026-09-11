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
		Assert.False(frame.Instruction.CanConfirm);
	}

	[Fact]
	public void UnreachableRequestedPoseDoesNotCreateGhost()
	{
		var origin = new Coord(5, 5, 5);
		var battle = BattleTestFixture.BeginSimulation(origin);
		var builder = BattleTestFixture.FrameBuilder(battle);
		var option = builder.PreviewMoveOptions(battle, battle.PlayerAgent)
			.First(path => path.EndPosition == origin + Coord.Forward);
		var unreachable = MovePose.For(-Coord.Forward, 0);

		builder.Interaction.BeginMoveSelection(option.EndPosition, unreachable);
		var frame = builder.BuildFrame(battle, battle.PlayerAgent, acceptsCommands: true);

		Assert.Null(frame.SelectedMove);
		Assert.Null(frame.MoveGhostState);
		Assert.False(frame.Instruction.Visible);
		Assert.False(frame.Instruction.CanConfirm);
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
