using GrimSpace.Battle.Board;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record ClearTurnHazardsAction : IAction<BattleBoard, ActorSession>
{
	public string OwnerId { get; } = EntityIds.System;
	public int? UndoGroup { get; } = null;

	public bool IsLegal(BattleBoard world, ActorSession runtime) =>
		ClearTurnHazardsDef.Instance.IsLegal(this, world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(BattleBoard world, ActorSession runtime) =>
		ClearTurnHazardsDef.Instance.Resolve(this, world, runtime);
}
