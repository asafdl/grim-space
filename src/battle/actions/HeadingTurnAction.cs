using GrimSpace.Battle.Board;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string ActorId,
	EHeadingTurn Turn,
	int? UndoGroup = null) : IAction<BattleBoard, ActorSession>
{
	public IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>> Definition =>
		HeadingDef.Instance;
}

public sealed class HeadingDef
	: IActionDef<IAction, BattleBoard, ActorSession, IEffect<BattleBoard, ActorSession>>,
		IActionStreamline
{
	public static HeadingDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleBoard world, ActorSession runtime, string actorId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = Bind(actorId, turn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public HeadingTurnAction Bind(string actorId, EHeadingTurn turn) => new(actorId, turn);

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

	public bool IsLegal(HeadingTurnAction action, BattleBoard world, ActorSession runtime) =>
		world.StateOf(action.ActorId).ActionPoints >= QuoteApCost(runtime, action.Turn);

	public IReadOnlyList<IEffect<BattleBoard, ActorSession>> Resolve(
		HeadingTurnAction action,
		BattleBoard world,
		ActorSession runtime)
	{
		var apCost = ApCostForTurn(action.Turn);
		var momDelta = MomentumDeltaForTurn(action.Turn);
		var consumedDiscount = false;

		if (Orientation.IsYawTurn(action.Turn)
			&& apCost > 0
			&& runtime.SpinBraked
			&& runtime.SpinDiscount)
		{
			apCost = System.Math.Max(0, apCost - 1);
			momDelta = 0;
			consumedDiscount = true;
		}

		var effects = new List<IEffect<BattleBoard, ActorSession>>
		{
			new ApChangeEffect(-apCost),
			new YawMomentumEffect(momDelta),
			new HeadingTurnEffect(action.Turn),
		};

		if (Orientation.IsYawTurn(action.Turn))
			effects.Insert(0, new AddYawQuartersEffect(YawDelta(action.Turn)));

		if (consumedDiscount)
			effects.Insert(2, new ConsumeSpinDiscountEffect());

		return effects;
	}

	public IReadOnlyList<IAction> Streamline(IReadOnlyList<IAction> actions)
	{
		var result = new List<IAction>(actions.Count);
		var index = 0;

		while (index < actions.Count)
		{
			if (actions[index] is HeadingTurnAction first && Orientation.IsYawTurn(first.Turn))
			{
				var actorId = first.ActorId;
				var net = YawDelta(first.Turn);
				int? undoGroup = first.UndoGroup;
				index++;

				while (index < actions.Count
					&& actions[index] is HeadingTurnAction next
					&& next.ActorId == actorId
					&& Orientation.IsYawTurn(next.Turn))
				{
					net += YawDelta(next.Turn);
					undoGroup = next.UndoGroup ?? undoGroup;
					index++;
				}

				foreach (var turn in TurnsForNetYaw(net))
					result.Add(new HeadingTurnAction(actorId, turn, undoGroup));

				continue;
			}

			result.Add(actions[index]);
			index++;
		}

		return result;
	}

	private static int QuoteApCost(ActorSession runtime, EHeadingTurn turn)
	{
		var apCost = ApCostForTurn(turn);

		if (!Orientation.IsYawTurn(turn) || apCost <= 0 || !runtime.SpinBraked || !runtime.SpinDiscount)
			return apCost;

		return System.Math.Max(0, apCost - 1);
	}

	private static int ApCostForTurn(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight or EHeadingTurn.YawLeft or EHeadingTurn.PitchUp or EHeadingTurn.PitchDown =>
				CombatConfig.HeadingTurn90ApCost,
			EHeadingTurn.Yaw180 => CombatConfig.HeadingTurn180ApCost,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static int MomentumDeltaForTurn(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight or EHeadingTurn.YawLeft or EHeadingTurn.PitchUp or EHeadingTurn.PitchDown => 1,
			EHeadingTurn.Yaw180 => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static IEnumerable<EHeadingTurn> TurnsForNetYaw(int netQuarters)
	{
		return Orientation.NormalizeQuarters(netQuarters) switch
		{
			0 => [],
			1 => [EHeadingTurn.YawRight],
			2 => [EHeadingTurn.Yaw180],
			3 => [EHeadingTurn.YawLeft],
			_ => throw new InvalidOperationException($"Unexpected net yaw quarters: {netQuarters}."),
		};
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
