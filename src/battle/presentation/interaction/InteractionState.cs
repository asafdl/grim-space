using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Presentation.Domains.Move;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	public string? FocusId { get; private set; }

	public EPlayerMode Mode { get; private set; } = EPlayerMode.Move;
	public AbilityHudCatalog.Spec? ActiveAbilitySpec { get; private set; }
	public int? AbilityHoveredIndex { get; private set; }
	public Coord? MoveHoveredCell { get; private set; }
	public Coord? MoveDestination { get; private set; }
	public GridBasis? RequestedMoveBasis { get; private set; }
	public int MoveRollQuarters { get; private set; }
	public bool IsMoveDragging { get; private set; }
	public string? ActionError { get; private set; }

	public void FocusUnit(string unitId)
	{
		FocusId = unitId;
		SetMode(EPlayerMode.Move);
	}

	public void ClearFocus()
	{
		FocusId = null;
		SetMode(EPlayerMode.Move);
	}

	public void SetMode(EPlayerMode mode, AbilityHudCatalog.Spec? abilitySpec = null)
	{
		Mode = mode;
		ActiveAbilitySpec = mode == EPlayerMode.Move ? null : abilitySpec;
		ActionError = null;
		ClearHovers();
		ClearMoveSelection();
	}

	public void ResetAfterTurn()
	{
		FocusId = null;
		SetMode(EPlayerMode.Move);
	}

	public void ClearHovers()
	{
		MoveHoveredCell = null;
		AbilityHoveredIndex = null;
	}

	public void BeginMoveSelection(Coord destination, GridBasis basis)
	{
		MoveHoveredCell = null;
		MoveDestination = destination;
		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		IsMoveDragging = true;
		ActionError = null;
	}

	public void SetMovePose(GridBasis basis)
	{
		if (MoveDestination is null)
			return;

		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		ActionError = null;
	}

	public void ClearMoveSelection()
	{
		MoveDestination = null;
		RequestedMoveBasis = null;
		MoveRollQuarters = 0;
		IsMoveDragging = false;
		ActionError = null;
	}

	public void ReportActionFailure()
	{
		IsMoveDragging = false;
		ActionError = BattleHudCopy.ActionUnavailable;
	}

	public void SetMoveHover(Coord? cell, IReadOnlyList<MovePathOption> options) =>
		MoveHoveredCell = ValidateMoveHover(cell, options);

	public void ValidateMoveHover(IReadOnlyList<MovePathOption> options) =>
		MoveHoveredCell = ValidateMoveHover(MoveHoveredCell, options);

	private static Coord? ValidateMoveHover(Coord? cell, IReadOnlyList<MovePathOption> options)
	{
		if (cell is not Coord coordinate)
			return null;

		foreach (var option in options)
		{
			if (option.EndPosition == coordinate && option.Steps.Count > 0)
				return coordinate;
		}

		return null;
	}

	public void SetAbilityHover(int? index, int optionCount)
	{
		var clamped = ClampIndex(index, optionCount);
		if (AbilityHoveredIndex == clamped)
			return;

		AbilityHoveredIndex = clamped;
		ActionError = null;
	}

	public void ClampAbilityHover(int optionCount) =>
		AbilityHoveredIndex = ClampIndex(AbilityHoveredIndex, optionCount);

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}
