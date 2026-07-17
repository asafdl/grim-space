using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(
	IReadOnlyList<IAction> queuedActions,
	TurnState turnState)
{
	public IReadOnlyList<IAction> QueuedActions { get; } = queuedActions;

	public TurnState TurnState { get; } = turnState;
}
