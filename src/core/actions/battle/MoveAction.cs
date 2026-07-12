using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Units;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;

using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class MoveAction(Option option) : IBattleAction
{
	public Option Option { get; } = option;

	public EnqueuePolicy EnqueuePolicy => EnqueuePolicy.ReplaceSameType;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		StepCosts.CanAffordMove(board.Player, Option)
		&& board.PlayerUnit.Movement.CanMove(board.Player, Option);

	public int GetApCost(State player) => Option.ApCost;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board) =>
	[
		new MoveEffect(Option),
		new ApChangeEffect(-Option.ApCost),
	];
}
