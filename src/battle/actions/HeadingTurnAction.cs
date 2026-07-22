using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string OwnerId,
	EHeadingTurn Turn,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> Definition =>
		HeadingDef.Instance;
}

public sealed class HeadingDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>
{
	public static HeadingDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string ownerId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = Bind(ownerId, turn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public HeadingTurnAction Bind(string ownerId, EHeadingTurn turn) => new(ownerId, turn);

	public bool IsPossible(IAction action, BattleBoard world, ActorSession runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleBoard world, ActorSession runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		IAction action,
		BattleBoard world,
		ActorSession runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(HeadingTurnAction action, BattleBoard world, ActorSession runtime) => true;

	public bool IsLegal(HeadingTurnAction action, BattleBoard world, ActorSession runtime)
	{
		if (Orientation.IsYawTurn(action.Turn))
			return world.StateOf(action.OwnerId).ActionPoints >= QuoteYawApCost(runtime, action.Turn);

		return world.StateOf(action.OwnerId).ActionPoints >= CombatConfig.HeadingTurn90ApCost;
	}

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		HeadingTurnAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		if (!Orientation.IsYawTurn(action.Turn))
		{
			return
			[
				new HeadingTurnEffect(action.Turn),
				new ApChangeEffect(-CombatConfig.HeadingTurn90ApCost),
			];
		}

		var yawDelta = YawDelta(action.Turn);
		var oldNet = runtime.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + yawDelta);
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);
		var momDelta = Orientation.MomentumLossForNetYaw(newNet) - Orientation.MomentumLossForNetYaw(oldNet);
		var consumedDiscount = false;

		if (apDelta > 0 && runtime.SpinBraked && runtime.SpinDiscount)
		{
			apDelta = System.Math.Max(0, apDelta - 1);
			momDelta = 0;
			consumedDiscount = true;
		}

		var effects = new List<IEffect<BattleBoard, ActorSession>>
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

	private static int QuoteYawApCost(ActorSession runtime, EHeadingTurn turn)
	{
		var oldNet = runtime.NetYaw;
		var newNet = Orientation.NormalizeQuarters(oldNet + YawDelta(turn));
		var apDelta = Orientation.ApCostForNetYaw(newNet) - Orientation.ApCostForNetYaw(oldNet);

		if (apDelta <= 0 || !runtime.SpinBraked || !runtime.SpinDiscount)
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
