using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Player;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	public string? FocusId { get; private set; }

	public EPlayerMode Mode { get; set; } = EPlayerMode.Move;
	public EFlakMount? FlakHoverMount { get; set; }
	public bool RailgunHovered { get; set; }
	public ETorpedoMount? TorpedoHoverMount { get; set; }
	public int? MoveHoveredIndex { get; set; }

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

	public void SetMode(EPlayerMode mode)
	{
		Mode = mode;
		ClearHovers();
	}

	public void ResetAfterTurn()
	{
		FocusId = null;
		Mode = EPlayerMode.Move;
		ClearHovers();
	}

	public void ClearHovers()
	{
		MoveHoveredIndex = null;
		FlakHoverMount = null;
		RailgunHovered = false;
		TorpedoHoverMount = null;
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
