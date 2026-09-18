using GrimSpace.Battle.Player;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Battle.Presentation.Interaction;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Tests.Presentation;

public sealed class InteractionStateTests
{
	[Fact]
	public void ClearHoversClearsAbilityHover()
	{
		var state = new InteractionState();

		state.SetAbilityHover(0, optionCount: 1);
		state.ClearHovers();

		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void AbilityHoverIsClampedAndClearedWithOtherHovers()
	{
		var state = new InteractionState();

		state.SetAbilityHover(1, optionCount: 2);
		Assert.Equal(1, state.AbilityHoveredIndex);

		state.SetAbilityHover(2, optionCount: 2);
		Assert.Null(state.AbilityHoveredIndex);

		state.SetAbilityHover(0, optionCount: 2);
		state.ClearHovers();
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void ChangingAbilityHoverClearsActionError()
	{
		var state = new InteractionState();
		state.ReportActionFailure();

		state.SetAbilityHover(0, optionCount: 1);

		Assert.Null(state.ActionError);
	}

	[Fact]
	public void SetModeClearsAbilityHoverAndActiveSpec()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.SetAbilityHover(0, optionCount: 1);
		state.SetMode(EPlayerMode.Move);

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void FocusChangeClearsAbilityTargeting()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Railgun, spec);
		state.FocusUnit("enemy");

		Assert.Equal(EPlayerMode.Move, state.Mode);
		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
	}

	[Fact]
	public void ReorderingOptionsPreservesHoveredDestination()
	{
		var state = new InteractionState();
		var destination = new Coord(2, 3, 4);
		var first = CreateOptions(destination, basisA: true);
		var second = CreateOptions(destination, basisA: false);

		state.SetMoveHover(destination, first);
		state.ValidateMoveHover(second);

		Assert.Equal(destination, state.MoveHoveredCell);
	}

	[Fact]
	public void SameCountReplacementCannotSilentlyRetargetHover()
	{
		var state = new InteractionState();
		var first = new[]
		{
			CreateOption(new Coord(1, 0, 0)),
			CreateOption(new Coord(2, 0, 0)),
		};
		var second = new[]
		{
			CreateOption(new Coord(3, 0, 0)),
			CreateOption(new Coord(2, 0, 0)),
		};

		state.SetMoveHover(new Coord(1, 0, 0), first);
		state.ValidateMoveHover(second);

		Assert.Null(state.MoveHoveredCell);
	}

	[Fact]
	public void MissingCoordinateClearsHover()
	{
		var state = new InteractionState();
		state.SetMoveHover(new Coord(5, 5, 5), [CreateOption(new Coord(5, 5, 5))]);
		state.ValidateMoveHover([]);

		Assert.Null(state.MoveHoveredCell);
	}

	[Fact]
	public void ClearHoversClearsMoveHover()
	{
		var state = new InteractionState();
		state.SetMoveHover(new Coord(1, 2, 3), [CreateOption(new Coord(1, 2, 3))]);

		state.ClearHovers();

		Assert.Null(state.MoveHoveredCell);
	}

	[Fact]
	public void BeginMoveSelectionClearsPassiveHover()
	{
		var state = new InteractionState();
		var destination = new Coord(1, 2, 3);
		state.SetMoveHover(destination, [CreateOption(destination)]);

		state.BeginMoveSelection(destination, MovePose.For(Coord.Forward, 0));

		Assert.Null(state.MoveHoveredCell);
	}

	[Fact]
	public void CompleteMoveSelectionEndsDragWithoutClearingPoseHits()
	{
		var state = new InteractionState();
		state.RefreshPoseHitOpportunities(
			[new PoseHitOpportunity("enemy", "res://icon.svg", default)]);
		state.BeginMoveSelection(new Coord(1, 2, 3), MovePose.For(Coord.Forward, 0));

		state.CompleteMoveSelection();

		Assert.False(state.IsMoveDragging);
		Assert.Null(state.MoveDestination);
		Assert.Single(state.PoseHitOpportunities);
	}

	[Fact]
	public void ClearMoveSelectionClearsPoseHits()
	{
		var state = new InteractionState();
		state.RefreshPoseHitOpportunities(
			[new PoseHitOpportunity("enemy", "res://icon.svg", default)]);
		state.BeginMoveSelection(new Coord(1, 2, 3), MovePose.For(Coord.Forward, 0));

		state.ClearMoveSelection();

		Assert.Empty(state.PoseHitOpportunities);
	}

	[Fact]
	public void ResetAfterTurnClearsAbilityTargeting()
	{
		var state = new InteractionState();
		var spec = AbilityHudCatalog.ForUnit(GrimSpace.Units.Enums.EType.Fighter)[0];

		state.SetMode(EPlayerMode.Flak, spec);
		state.SetAbilityHover(0, optionCount: 1);
		state.ResetAfterTurn();

		Assert.Null(state.ActiveAbilitySpec);
		Assert.Null(state.AbilityHoveredIndex);
		Assert.Equal(EPlayerMode.Move, state.Mode);
	}

	private static IReadOnlyList<MovePathOption> CreateOptions(Coord destination, bool basisA)
	{
		var basis = basisA
			? MovePose.For(Coord.Forward, 0)
			: MovePose.For(new Coord(1, 0, 0), 1);
		return [CreateOption(destination, basis)];
	}

	private static MovePathOption CreateOption(Coord destination, GridBasis? basis = null)
	{
		basis ??= MovePose.For(Coord.Forward, 0);
		return new MovePathOption(
			[new GrimSpace.Battle.Actions.MoveStepAction("player")],
			[],
			destination,
			basis.Value,
			ExtensionApCost: 0,
			RemainingAp: 0,
			ResultState: default);
	}
}
