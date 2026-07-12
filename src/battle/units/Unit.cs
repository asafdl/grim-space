using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Units;

public abstract class Unit
{
	public EController Controller { get; }
	public State State { get; }
	public IMovement Movement { get; }

	protected Unit(EController controller, State state, IMovement movement)
	{
		Controller = controller;
		State = state;
		Movement = movement;
	}

	public abstract Preview? ShowMovement(BoundedGrid grid);
}
