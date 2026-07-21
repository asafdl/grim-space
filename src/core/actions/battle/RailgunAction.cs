using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class RailgunAction(string ownerId, string targetUnitId, int? undoGroup = null) : IAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public string TargetUnitId { get; } = targetUnitId;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (!board.Units.TryGetValue(TargetUnitId, out var targetUnit) || !targetUnit.State.IsAlive)
			return false;

		var target = targetUnit.State;
		if (target.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		var actor = board.StateOf(OwnerId);
		return actor.Position.ManhattanDistanceTo(target.Position) <= CombatConfig.RailgunMaxRange;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
		[new DamageEffect(TargetUnitId, CombatConfig.RailgunDamage)];
}
