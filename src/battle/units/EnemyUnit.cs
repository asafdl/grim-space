using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Units;

public sealed class EnemyUnit : Unit
{
	public EnemyUnit(State state)
		: base(EController.Enemy, state)
	{
	}
}
