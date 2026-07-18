using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext(
	IReadOnlyList<IAction> phaseActions,
	TurnState turnState)
{
	public IReadOnlyList<IAction> PhaseActions { get; } = phaseActions;

	public TurnState TurnState { get; } = turnState;
}
