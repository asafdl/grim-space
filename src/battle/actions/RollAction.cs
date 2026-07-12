using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Actions;

public sealed class RollAction(ERollDirection direction) : IAction
{
	public ERollDirection Direction { get; } = direction;

	public int GetApCost(State player) => CombatConfig.RollApCost;

	public IReadOnlyList<IStateEffect> Resolve(ActionBoard board) =>
	[
		new RollEffect(Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];
}
