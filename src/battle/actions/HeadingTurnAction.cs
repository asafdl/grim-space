using GrimSpace.Battle.Actions.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;

namespace GrimSpace.Battle.Actions;

public sealed class HeadingTurnAction(EHeadingTurn turn) : IAction
{
	public EHeadingTurn Turn { get; } = turn;

	public int GetApCost(State player) =>
		CombatConfig.HeadingTurnBaseApCost + player.MomentumLevel;

	public IReadOnlyList<IStateEffect> Resolve(ActionBoard board)
	{
		var cost = GetApCost(board.Player);
		return
		[
			new HeadingTurnEffect(Turn),
			new ApChangeEffect(-cost),
		];
	}
}
