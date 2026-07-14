using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;

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
}
