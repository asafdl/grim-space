using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Actions.Battle.Effects;
using GrimSpace.Core.Actions.Battle.Contexts;

namespace GrimSpace.Core.Actions.Battle;

public sealed class HeadingTurnAction(string ownerId, EHeadingTurn turn) : IAction
{
	public string OwnerId { get; } = ownerId;
	public EHeadingTurn Turn { get; } = turn;

	public bool IsLegal(BattleBoard board, BattlePlanContext context)
	{
		if (Orientation.IsYawTurn(Turn))
			return board.StateOf(OwnerId).ActionPoints >= QuoteYawApCost(context.TurnState, Turn);

		return board.StateOf(OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;
	}

	public IReadOnlyList<IEffect<BattleSlices>> Resolve(BattleBoard board, BattlePlanContext context)
	{
		if (!Orientation.IsYawTurn(Turn))
		{
			return
			[
				new HeadingTurnEffect(Turn),
				new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
			];
		}

		var turnState = context.TurnState;
		var yawDelta = YawDelta(Turn);
		var oldNet = turnState.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + yawDelta);
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		var momDelta = Orientation.MomentumLossForNetYaw(newNet) - Orientation.MomentumLossForNetYaw(oldNet);
		var consumedDiscount = false;

		if (apDelta > 0 && turnState.SpinBraked && turnState.HasSpinDiscount)
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
