using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;

using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class HeadingTurnAction(EHeadingTurn turn) : IBattleAction
{
	public EHeadingTurn Turn { get; } = turn;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		board.Player.ActionPoints >= GetApCost(board.Player);

	public int GetApCost(State player) =>
		CombatConfig.HeadingTurnBaseApCost + player.MomentumLevel;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board)
	{
		var cost = GetApCost(board.Player);
		return
		[
			new HeadingTurnEffect(Turn),
			new ApChangeEffect(-cost),
		];
	}
}
