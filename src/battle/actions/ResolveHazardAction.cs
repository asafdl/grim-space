using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ResolveHazardAction(
	string OwnerId,
	string HazardId,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public bool IsLegal(BattleBoard world, ActorSession runtime) =>
		ResolveHazardDef.Instance.IsLegal(this, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(BattleBoard world, ActorSession runtime) =>
		ResolveHazardDef.Instance.Resolve(this, world, runtime);
}
