using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class RailgunDef : IActionDef
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		foreach (var (unitId, unit) in ctx.Board.Units)
		{
			if (unitId == ownerId || !unit.State.IsAlive)
				continue;

			var action = new RailgunAction(ownerId, unitId);
			if (IsPossible(action, ctx))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleActionContext ctx) =>
		IsPossible(Cast(action), ctx);

	public bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsLegal(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public bool IsPossible(RailgunAction action, BattleActionContext ctx) => IsLegal(action, ctx);

	public bool IsLegal(RailgunAction action, BattleActionContext ctx)
	{
		var board = ctx.Board;
		if (!board.Units.TryGetValue(action.TargetUnitId, out var targetUnit) || !targetUnit.State.IsAlive)
			return false;

		var target = targetUnit.State;
		if (target.MomentumLevel != CombatConfig.RailgunRequiredTargetMomentum)
			return false;

		var actor = board.StateOf(action.OwnerId);
		return actor.Position.ManhattanDistanceTo(target.Position) <= CombatConfig.RailgunMaxRange;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(RailgunAction action, BattleActionContext ctx) =>
		[new DamageEffect(action.TargetUnitId, CombatConfig.RailgunDamage)];

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
