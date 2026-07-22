using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string OwnerId,
	EFlakMount Mount,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public bool IsLegal(BattleBoard world, ActorSession runtime) =>
		FlakDef.For(Mount).IsLegal(this, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(BattleBoard world, ActorSession runtime) =>
		FlakDef.For(Mount).Resolve(this, world, runtime);
}
