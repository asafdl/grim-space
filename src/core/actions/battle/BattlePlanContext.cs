using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(IReadOnlyList<IBattleAction> queuedActions, GridBasis startFacing)
{
	public IReadOnlyList<IBattleAction> QueuedActions { get; } = queuedActions;

	public GridBasis StartFacing { get; } = startFacing;
}
