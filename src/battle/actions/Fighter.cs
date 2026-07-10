using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public sealed class Fighter : IActions
{
	public int GetApCost(IAction action, State unit) =>
		action switch
		{
			MoveAction move => move.Option.Lateral is null ? 0 : 1,
			_ => int.MaxValue,
		};

	public bool CanPerform(IAction action, State unit) =>
		action switch
		{
			MoveAction => true,
			_ => false,
		};

	public void ApplyCost(IAction action, State unit) =>
		unit.ActionPoints -= GetApCost(action, unit);
}
