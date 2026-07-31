using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Presentation.Ui;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Presentation.Interaction;

public sealed class InteractionState
{
	public EPlayerMode Mode { get; set; } = EPlayerMode.Move;
	public EMissileMount? MissileMount { get; set; }
	public int MissileRange { get; set; } = CombatConfig.ForeMissileMinRange;
	public Coord? MissileHover { get; set; }
	public Coord? FlakHover { get; set; }
	public Unit? RailgunHover { get; set; }
	public int? MoveHoveredIndex { get; set; }
	public IReadOnlyList<Coord> CommittedMovePath { get; set; } = [];
	public int ActiveRingIndex { get; private set; } = 0;

	public void SyncActiveRingForSnapshot(PresentationFrame frame)  {
		
	}

	public void ClearInteraction()
	{
		MoveHoveredIndex = null;
		MissileHover = null;
		FlakHover = null;
		RailgunHover = null;
	}

	public void ResetAfterTurn() => SetMoveMode();

	public void SetMoveMode()
	{
		Mode = EPlayerMode.Move;
		MissileMount = null;
		ClearInteraction();
		CommittedMovePath = [];
	}

	public void SetMode(EPlayerMode mode)
	{
		Mode = mode;
		MissileMount = null;
		ClearInteraction();
	}

	public void SelectMissileMount(EMissileMount mount)
	{
		Mode = EPlayerMode.Missile;
		MissileMount = mount;
		MissileRange = CombatConfig.ForeMissileMinRange;
		ClearInteraction();
	}

	public void SelectFlakMode()
	{
		Mode = EPlayerMode.Flak;
		MissileMount = null;
		ClearInteraction();
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
