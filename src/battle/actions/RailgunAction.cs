using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Actions;

public sealed class RailgunAction(string targetUnitId) : IAction
{
	public string TargetUnitId { get; } = targetUnitId;

	public int GetApCost(State player) => 0;

	public IReadOnlyList<IStateEffect> Resolve(ActionBoard board) =>
		[new DamageEffect(TargetUnitId, CombatConfig.RailgunDamage)];
}
