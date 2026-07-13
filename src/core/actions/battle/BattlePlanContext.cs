using GrimSpace.Battle.Weapons;
using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(IReadOnlyList<IBattleAction> queuedActions, GridBasis startFacing)
{
	public IReadOnlyList<IBattleAction> QueuedActions { get; } = queuedActions;

	public GridBasis StartFacing { get; } = startFacing;

	public int MissilesQueued => QueuedActions.Count(action => action is MissileAction);

	public int MissilesRemaining =>
		System.Math.Max(CombatConfig.MissilesPerTurn - MissilesQueued, 0);
}
