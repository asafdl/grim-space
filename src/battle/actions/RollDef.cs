using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class RollDef : IActionDef
{
	public static RollDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = new RollAction(ownerId, direction);
			if (IsPossible(action, ctx))
				yield return action;
		}
	}

	public bool IsPossible(IAction action, BattleActionContext ctx) => true;

	public bool IsLegal(IAction action, BattleActionContext ctx) =>
		IsLegal(Cast(action), ctx);

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(IAction action, BattleActionContext ctx) =>
		Resolve(Cast(action), ctx);

	public bool IsLegal(RollAction action, BattleActionContext ctx) =>
		ctx.Board.StateOf(action.OwnerId).ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(RollAction action, BattleActionContext ctx) =>
	[
		new RollEffect(action.Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];

	private static RollAction Cast(IAction action) =>
		action as RollAction ?? throw new ArgumentException($"Expected {nameof(RollAction)}.", nameof(action));
}
