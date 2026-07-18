using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Contexts;
using GrimSpace.Core.Actions.Battle.Effects;

namespace GrimSpace.Core.Actions.Battle;

public sealed class ResolveHazardAction(string ownerId, string hazardId) : IAction
{
	public string OwnerId { get; } = ownerId;
	public string HazardId { get; } = hazardId;

	public bool IsLegal(BattleBoard board, BattlePlanContext context) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context) =>
		[new ResolveHazardEffect(HazardId)];
}
