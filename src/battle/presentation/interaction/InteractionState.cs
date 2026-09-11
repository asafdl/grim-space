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
	public ESpatialOrientation? FlakHoverMountedOn { get; set; }
	public bool RailgunHovered { get; set; }
	public int? MoveHoveredIndex { get; set; }
	public Coord? MoveDestination { get; private set; }
	public GridBasis? RequestedMoveBasis { get; private set; }
	public int MoveRollQuarters { get; private set; }
	public bool IsMoveDragging { get; private set; }
	public ESpatialOrientation? StagedMountedOn { get; private set; }
	public string? ConfirmationError { get; private set; }

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
		ConfirmationError = null;
		ClearHovers();
		ClearAbilitySelection();
		ClearMoveSelection();
	}

	public void ResetAfterTurn()
	{
		FocusId = null;
		SetMode(EPlayerMode.Move);
	}

	public void ClearHovers()
	{
		MoveHoveredIndex = null;
		FlakHoverMountedOn = null;
		RailgunHovered = false;
	}

	public void StageMountedOn(ESpatialOrientation mountedOn)
	{
		if (StagedMountedOn == mountedOn)
			return;

		StagedMountedOn = mountedOn;
		ConfirmationError = null;
	}

	public void ClearAbilitySelection() => StagedMountedOn = null;

	public void BeginMoveSelection(Coord destination, GridBasis basis)
	{
		MoveDestination = destination;
		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		IsMoveDragging = true;
		ConfirmationError = null;
	}

	public void SetMovePose(GridBasis basis)
	{
		if (MoveDestination is null)
			return;

		RequestedMoveBasis = basis;
		MoveRollQuarters = MovePose.RollQuarters(basis);
		ConfirmationError = null;
	}

	public void ClearMoveSelection()
	{
		MoveDestination = null;
		RequestedMoveBasis = null;
		MoveRollQuarters = 0;
		IsMoveDragging = false;
		ConfirmationError = null;
	}

	public void ReportConfirmationFailure()
	{
		IsMoveDragging = false;
		ConfirmationError = BattleHudCopy.ActionUnavailable;
	}

	public void SetMoveHover(int? index, int optionCount) =>
		MoveHoveredIndex = ClampIndex(index, optionCount);

	public void ClampMoveHover(int optionCount) =>
		MoveHoveredIndex = ClampIndex(MoveHoveredIndex, optionCount);

	private static int? ClampIndex(int? index, int optionCount)
	{
		if (index is not int i || i < 0 || i >= optionCount)
			return null;

		return i;
	}
}
