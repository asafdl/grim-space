namespace GrimSpace.Core.Actions.Battle;

using GrimSpace.Core.Actions;

public sealed class BattlePlanContext
{
	public BattlePlanContext(IEnumerable<IAction> phaseActions, TurnState turnState)
	{
		PhaseActions = phaseActions;
		TurnState = turnState;
	}

	public IEnumerable<IAction> PhaseActions { get; }

	public TurnState TurnState { get; }
}
