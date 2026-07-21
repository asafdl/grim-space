using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Effects;

namespace GrimSpace.Battle.Actions;

public sealed class ResolveHazardAction(string ownerId, string hazardId, int? undoGroup = null) : IAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public string HazardId { get; } = hazardId;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
		[new ResolveHazardEffect(HazardId), new RemoveHazardEffect(HazardId)];
}
