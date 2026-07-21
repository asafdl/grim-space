using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Actions;

public sealed class RailgunAction(string ownerId, string targetUnitId, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public string TargetUnitId { get; } = targetUnitId;

	public bool IsLegal(BattleActionContext ctx)
	{
		var board = ctx.Board;
		if (!board.Units.TryGetValue(TargetUnitId, out var targetUnit) || !targetUnit.State.IsAlive)
			return false;

		var target = targetUnit.State;
		if (target.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		var actor = board.StateOf(OwnerId);
		return actor.Position.ManhattanDistanceTo(target.Position) <= CombatConfig.RailgunMaxRange;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx) =>
		[new DamageEffect(TargetUnitId, CombatConfig.RailgunDamage)];
}
