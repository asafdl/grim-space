using GrimSpace.Battle.Weapons;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(IReadOnlyList<IBattleAction> queuedActions)
{
	public IReadOnlyList<IBattleAction> QueuedActions { get; } = queuedActions;

	public int MissilesQueued => QueuedActions.Count(action => action is MissileAction);

	public int MissilesRemaining =>
		System.Math.Max(CombatConfig.MissilesPerTurn - MissilesQueued, 0);
}
