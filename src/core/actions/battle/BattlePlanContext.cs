using GrimSpace.Math.Grid;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(
	IReadOnlyList<IBattleAction> queuedActions,
	GridBasis startFacing,
	BattleTurnTags tags)
{
	public IReadOnlyList<IBattleAction> QueuedActions { get; } = queuedActions;

	public GridBasis StartFacing { get; } = startFacing;

	public BattleTurnTags Tags { get; } = tags;
}
