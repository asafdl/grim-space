using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Actions.Battle;

public sealed class BattlePlanContext
{
	public BattlePlanContext(IList<IAction> phaseActions, TurnState turnState)
	{
		PhaseActions = phaseActions;
		TurnState = turnState;
	}

	public IList<IAction> PhaseActions { get; }

	public TurnState TurnState { get; }
}
