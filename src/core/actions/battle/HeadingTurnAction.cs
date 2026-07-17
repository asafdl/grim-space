using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class HeadingTurnAction(string ownerId, EHeadingTurn turn) : IAction, IBattleAction
{
	public string OwnerId { get; } = ownerId;
	public EHeadingTurn Turn { get; } = turn;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (Orientation.IsYawTurn(Turn))
			return board.StateOf(OwnerId).ActionPoints >= QuoteYawApDelta(context.Tags);

		return board.StateOf(OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		if (Orientation.IsYawTurn(Turn))
			ApplyYawBilling(board.StateOf(OwnerId), context.Tags);

		if (Orientation.IsYawTurn(Turn))
			return [new HeadingTurnEffect(Turn)];

		return
		[
			new HeadingTurnEffect(Turn),
			new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
		];
	}

	private int QuoteYawApDelta(BattleTurnTags tags)
	{
		var oldNet = tags.Yaw.NetQuarters;
		var newNet = Orientation.NormalizeQuarters(oldNet + YawDelta(Turn));
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		return ApplySpinDiscount(apDelta, tags.Spin, consume: false);
	}

	private void ApplyYawBilling(State actor, BattleTurnTags tags)
	{
		var oldNet = tags.Yaw.NetQuarters;
		tags.Yaw.AddQuarters(YawDelta(Turn));
		var newNet = tags.Yaw.NetQuarters;

		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		var momDelta = Orientation.MomentumLossForNetYaw(newNet) - Orientation.MomentumLossForNetYaw(oldNet);
		(apDelta, momDelta) = ApplySpinDiscountWithMomentum(apDelta, momDelta, tags.Spin);

		actor.ActionPoints -= apDelta;
		ApplyMomentumDelta(momDelta, tags.Yaw, actor);
	}

	private static void ApplyMomentumDelta(int momDelta, YawState yaw, State actor)
	{
		if (momDelta > 0)
		{
			var loss = System.Math.Min(momDelta, actor.MomentumLevel);
			actor.MomentumLevel -= loss;
			yaw.AddMomentumPaid(loss);
		}
		else if (momDelta < 0)
		{
			var refund = yaw.RefundMomentum(-momDelta);
			actor.MomentumLevel = System.Math.Min(
				actor.MomentumLevel + refund,
				MomentumConfig.MaxLevel);
		}
	}

	private static int ApplySpinDiscount(int apDelta, SpinState spin, bool consume)
	{
		if (apDelta <= 0 || !spin.IsBraked || !spin.HasDiscount)
			return apDelta;

		if (consume)
			spin.TryConsumeDiscount();

		return System.Math.Max(0, apDelta - 1);
	}

	private static (int ApDelta, int MomDelta) ApplySpinDiscountWithMomentum(
		int apDelta,
		int momDelta,
		SpinState spin)
	{
		if (apDelta <= 0 || !spin.IsBraked || !spin.HasDiscount)
			return (apDelta, momDelta);

		if (!spin.TryConsumeDiscount())
			return (apDelta, momDelta);

		return (System.Math.Max(0, apDelta - 1), 0);
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
