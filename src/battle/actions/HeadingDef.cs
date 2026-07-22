using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Slices;
using GrimSpace.Battle.Turn;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed class HeadingDef : IActionDef
{
	public static HeadingDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleActionContext ctx, string ownerId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = new HeadingTurnAction(ownerId, turn);
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

	public bool IsPossible(HeadingTurnAction action, BattleActionContext ctx) => true;

	public bool IsLegal(HeadingTurnAction action, BattleActionContext ctx)
	{
		if (Orientation.IsYawTurn(action.Turn))
			return ctx.Board.StateOf(action.OwnerId).ActionPoints >= QuoteYawApCost(ctx.PhaseContext, action.Turn);

		return ctx.Board.StateOf(action.OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(HeadingTurnAction action, BattleActionContext ctx)
	{
		var state = ctx.PhaseContext;

		if (!Orientation.IsYawTurn(action.Turn))
		{
			return
			[
				new HeadingTurnEffect(action.Turn),
				new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
			];
		}

		var yawDelta = YawDelta(action.Turn);
		var oldNet = state.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + yawDelta);
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		var momDelta = Orientation.MomentumLossForNetYaw(newNet) - Orientation.MomentumLossForNetYaw(oldNet);
		var consumedDiscount = false;

		if (apDelta > 0 && state.SpinBraked && state.SpinDiscount)
		{
			apDelta = System.Math.Max(0, apDelta - 1);
			momDelta = 0;
			consumedDiscount = true;
		}

		var effects = new List<IEffect<BattleSlices>>
		{
			new AddYawQuartersEffect(yawDelta),
			new ApChangeEffect(-apDelta),
			new YawMomentumEffect(momDelta),
			new HeadingTurnEffect(action.Turn),
		};

		if (consumedDiscount)
			effects.Insert(2, new ConsumeSpinDiscountEffect());

		return effects;
	}

	private static int QuoteYawApCost(TurnPhaseContext phaseContext, EHeadingTurn turn)
	{
		var oldNet = phaseContext.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + YawDelta(turn));
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);

		if (apDelta <= 0 || !phaseContext.SpinBraked || !phaseContext.SpinDiscount)
			return apDelta;

		return System.Math.Max(0, apDelta - 1);
	}

	private static int YawDelta(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight => 1,
			EHeadingTurn.YawLeft => -1,
			EHeadingTurn.Yaw180 => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static HeadingTurnAction Cast(IAction action) =>
		action as HeadingTurnAction ?? throw new ArgumentException($"Expected {nameof(HeadingTurnAction)}.", nameof(action));
}
