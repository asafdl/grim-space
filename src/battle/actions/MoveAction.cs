using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;

namespace GrimSpace.Battle.Actions;

public sealed class MoveAction(Option option) : IAction
{
	public Option Option { get; } = option;

	public int GetApCost(State player) => Option.ApCost;

	public IReadOnlyList<IStateEffect> Resolve(ActionBoard board) =>
	[
		new MoveEffect(Option),
		new ApChangeEffect(-Option.ApCost),
	];
}
