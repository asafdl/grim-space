using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;
using BoundedGrid = GrimSpace.Math.Grid.Grid;

namespace GrimSpace.Battle.Units;

public sealed class EnemyUnit : Unit
{
	public EnemyUnit(State state, IMovement movement)
		: base(EController.Enemy, state, movement)
	{
	}

	public override Preview? ShowMovement(BoundedGrid grid) => null;
}
