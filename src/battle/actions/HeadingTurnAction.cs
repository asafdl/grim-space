using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record HeadingTurnAction(
	string ActorId,
	EHeadingTurn Turn) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		HeadingDef.Instance;
}

public sealed class HeadingDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActionStreamline
{
	public static HeadingDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var turn in Enum.GetValues<EHeadingTurn>())
		{
			var action = Bind(actorId, turn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public HeadingTurnAction Bind(string actorId, EHeadingTurn turn) => new(actorId, turn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).ActionPoints >= QuoteApCost(runtime, action.Turn);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		HeadingTurnAction action,
		BattleWorld world,
		ActorRuntime runtime)
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

		var effects = new List<IEffect<BattleWorld, ActorRuntime>>
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

	public IReadOnlyList<IAction> Streamline(IReadOnlyList<IAction> actions, IReadOnlyList<int?> undoGroups)
	{
		var result = new List<IAction>(actions.Count);
		var index = 0;

		while (index < actions.Count)
		{
			if (actions[index] is HeadingTurnAction first && Orientation.IsYawTurn(first.Turn))
			{
				var actorId = first.ActorId;
				var net = YawDelta(first.Turn);
				index++;

				while (index < actions.Count
					&& actions[index] is HeadingTurnAction next
					&& next.ActorId == actorId
					&& Orientation.IsYawTurn(next.Turn))
				{
					net += YawDelta(next.Turn);
					index++;
				}

				foreach (var turn in TurnsForNetYaw(net))
					result.Add(new HeadingTurnAction(actorId, turn));

				continue;
			}

			if (actions[index] is HeadingTurnAction pitchFirst && IsPitchTurn(pitchFirst.Turn))
			{
				var actorId = pitchFirst.ActorId;
				var net = PitchDelta(pitchFirst.Turn);
				index++;

				while (index < actions.Count
					&& actions[index] is HeadingTurnAction next
					&& next.ActorId == actorId
					&& IsPitchTurn(next.Turn))
				{
					net += PitchDelta(next.Turn);
					index++;
				}

				foreach (var turn in TurnsForNetPitch(net))
					result.Add(new HeadingTurnAction(actorId, turn));

				continue;
			}

			result.Add(actions[index]);
			index++;
		}

		return result;
	}

	private static int QuoteApCost(ActorRuntime runtime, EHeadingTurn turn)
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

	private static IEnumerable<EHeadingTurn> TurnsForNetPitch(int netQuarters) =>
		Orientation.NormalizeQuarters(netQuarters) switch
		{
			0 => [],
			1 => [EHeadingTurn.PitchUp],
			2 => [EHeadingTurn.PitchUp, EHeadingTurn.PitchUp],
			3 => [EHeadingTurn.PitchDown],
			_ => throw new InvalidOperationException($"Unexpected net pitch quarters: {netQuarters}."),
		};

	private static bool IsPitchTurn(EHeadingTurn turn) =>
		turn is EHeadingTurn.PitchUp or EHeadingTurn.PitchDown;

	private static int YawDelta(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight => 1,
			EHeadingTurn.YawLeft => -1,
			EHeadingTurn.Yaw180 => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static int PitchDelta(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.PitchUp => 1,
			EHeadingTurn.PitchDown => -1,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static HeadingTurnAction Cast(IAction action) =>
		action as HeadingTurnAction ?? throw new ArgumentException($"Expected {nameof(HeadingTurnAction)}.", nameof(action));
}
