using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Actions;

public sealed class RollAction(string ownerId, ERollDirection direction, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public ERollDirection Direction { get; } = direction;

	public bool IsLegal(BattleBoard board, TurnState state, IEnumerable<IAction> applied) =>
		board.StateOf(OwnerId).ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, TurnState state, IEnumerable<IAction> applied) =>
	[
		new RollEffect(Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];
}
