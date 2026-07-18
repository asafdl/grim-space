using GrimSpace.Battle.Units;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public abstract class Unit
{
	public EController Controller { get; }
	public State State { get; }

	protected Unit(EController controller, State state)
	{
		Controller = controller;
		State = state;
	}
}
