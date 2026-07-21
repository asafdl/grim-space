namespace GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Actions;

public readonly struct DamageContext(BattleBoard board)
{
	public void ApplyTo(string targetUnitId, int damage)
	{
		if (!board.Units.TryGetValue(targetUnitId, out var unit))
			return;

		var target = unit.State;
		target.Hp = System.Math.Max(target.Hp - damage, 0);
	}
}
