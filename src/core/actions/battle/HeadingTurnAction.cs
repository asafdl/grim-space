using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class HeadingTurnAction(string ownerId, EHeadingTurn turn) : IAction, IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public EHeadingTurn Turn { get; } = turn;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		Orientation.IsYawTurn(Turn) || board.StateOf(OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board)
	{
		if (Orientation.IsYawTurn(Turn))
			return [new HeadingTurnEffect(Turn)];

		return
		[
			new HeadingTurnEffect(Turn),
			new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
		];
	}
}
