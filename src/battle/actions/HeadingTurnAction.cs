using GrimSpace.Battle.Board;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string OwnerId,
	EHeadingTurn Turn,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public bool IsLegal(BattleBoard world, ActorSession runtime) =>
		HeadingDef.Instance.IsLegal(this, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(BattleBoard world, ActorSession runtime) =>
		HeadingDef.Instance.Resolve(this, world, runtime);
}
