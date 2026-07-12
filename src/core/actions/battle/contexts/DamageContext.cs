using GrimSpace.Battle.Units;

namespace GrimSpace.Core.Actions.Battle.Contexts;

public readonly struct DamageContext(State enemy)
{
	public void ApplyTo(string targetUnitId, int damage)
	{
		if (targetUnitId != enemy.Id)
			return;

		enemy.Hp = System.Math.Max(enemy.Hp - damage, 0);
	}
}
