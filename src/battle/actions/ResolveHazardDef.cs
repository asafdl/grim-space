using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class ResolveHazardDef : IActionDef
{
	public static ResolveHazardDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId) => [];

	public bool IsPossible(IAction action, BattleActionContext ctx) => true;

	public bool IsLegal(IAction action, BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(ResolveHazardAction action, BattleActionContext ctx) =>
		[new ResolveHazardEffect(action.HazardId), new RemoveHazardEffect(action.HazardId)];

	private static ResolveHazardAction Cast(IAction action) =>
		action as ResolveHazardAction ?? throw new ArgumentException($"Expected {nameof(ResolveHazardAction)}.", nameof(action));
}
