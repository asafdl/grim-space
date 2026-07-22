using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class EndOfPhaseDef : IActionDef
{
	public static EndOfPhaseDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId) => [];

	public bool IsPossible(IAction action, BattleActionContext ctx) => true;

	public bool IsLegal(IAction action, BattleActionContext ctx) => true;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(EndOfPhaseAction action, BattleActionContext ctx)
	{
		if (ctx.PhaseContext.IsMovePathStarted)
			return [];

		return [new MomentumDecayEffect()];
	}

	private static EndOfPhaseAction Cast(IAction action) =>
		action as EndOfPhaseAction ?? throw new ArgumentException($"Expected {nameof(EndOfPhaseAction)}.", nameof(action));
}
