using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Grid;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units.Enums;
using BattleGrid = GrimSpace.Battle.Grid.Grid;

namespace GrimSpace.Battle.Units;

public abstract class Unit
{
	public EController Controller { get; }
	public State State { get; }
	public IMovement Movement { get; }
	public IActions Actions { get; }

	protected Unit(EController controller, State state, IMovement movement, IActions actions)
	{
		Controller = controller;
		State = state;
		Movement = movement;
		Actions = actions;
	}

	public abstract Preview? ShowMovement(BattleGrid grid);
}
