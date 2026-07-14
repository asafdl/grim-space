using GrimSpace.Battle.Movement;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class EnemyUnit : Unit
{
	public EnemyUnit(State state, IMovement movement)
		: base(EController.Enemy, state, movement)
	{
	}
}
