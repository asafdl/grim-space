using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;

using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class RollAction(ERollDirection direction) : IBattleAction
{
	public ERollDirection Direction { get; } = direction;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) =>
		board.Player.ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board) =>
	[
		new RollEffect(Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];
}
