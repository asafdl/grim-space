using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RoundUpkeepAction(
	string OwnerId,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public bool IsLegal(BattleBoard world, ActorSession runtime) =>
		RoundUpkeepDef.Instance.IsLegal(this, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(BattleBoard world, ActorSession runtime) =>
		RoundUpkeepDef.Instance.Resolve(this, world, runtime);
}
