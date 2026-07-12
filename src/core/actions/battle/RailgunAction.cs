using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;

using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class RailgunAction(string targetUnitId) : IBattleAction
{
	public string TargetUnitId { get; } = targetUnitId;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (TargetUnitId != board.Enemy.Id || !board.Enemy.IsAlive)
			return false;

		if (board.Enemy.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		return board.Player.Position.ManhattanDistanceTo(board.Enemy.Position)
			<= CombatConfig.RailgunMaxRange;
	}

	public int GetApCost(State player) => 0;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board) =>
		[new DamageEffect(TargetUnitId, CombatConfig.RailgunDamage)];
}
