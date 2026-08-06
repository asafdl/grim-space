using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	public EPlayerMode Mode { get; set; } = EPlayerMode.Move;
	public Coord? FlakHover { get; set; }
	public Coord? RailgunHover { get; set; }
	public Coord? TorpedoHover { get; set; }
	public int? MoveHoveredIndex { get; set; }
	public IReadOnlyList<Coord> CommittedMovePath { get; set; } = [];

	public void SetMode(EPlayerMode mode)
	{
		Mode = mode;
		ClearHovers();
	}

	public void ResetAfterTurn()
	{
		Mode = EPlayerMode.Move;
		ClearHovers();
		CommittedMovePath = [];
	}

	public void ClearHovers()
	{
		MoveHoveredIndex = null;
		FlakHover = null;
		RailgunHover = null;
		TorpedoHover = null;
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
