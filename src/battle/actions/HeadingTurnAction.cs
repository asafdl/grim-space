using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions.Battle;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Slices;

namespace GrimSpace.Battle.Actions;

public sealed class HeadingTurnAction(string ownerId, EHeadingTurn turn, int? undoGroup = null) : IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public int? UndoGroup { get; } = undoGroup;
	public EHeadingTurn Turn { get; } = turn;

	public bool IsLegal(BattleActionContext ctx)
	{
		if (Orientation.IsYawTurn(Turn))
			return ctx.Board.StateOf(OwnerId).ActionPoints >= QuoteYawApCost(ctx.TurnState, Turn);

		return ctx.Board.StateOf(OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleActionContext ctx)
	{
		var state = ctx.TurnState;

		if (!Orientation.IsYawTurn(Turn))
		{
			return
			[
				new HeadingTurnEffect(Turn),
				new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
			];
		}

		var yawDelta = YawDelta(Turn);
		var oldNet = state.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + yawDelta);
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		var momDelta = Orientation.MomentumLossForNetYaw(newNet) - Orientation.MomentumLossForNetYaw(oldNet);
		var consumedDiscount = false;

		if (apDelta > 0 && state.SpinBraked && state.HasSpinDiscount)
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
			new HeadingTurnEffect(Turn),
		};

		if (consumedDiscount)
			effects.Insert(2, new ConsumeSpinDiscountEffect());

		return effects;
	}

	private static int QuoteYawApCost(TurnState turnState, EHeadingTurn turn)
	{
		var oldNet = turnState.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + YawDelta(turn));
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);

		if (apDelta <= 0 || !turnState.SpinBraked || !turnState.HasSpinDiscount)
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
}
