using GrimSpace.Battle.Movement;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Battle.Actions;

/// <summary>
/// Applies orientation button streamlining to the live planning queue.
/// </summary>
public static class OrientationStreamline
{
	public static bool TryApplyButton(BattleSimulation sim, IAction input)
	{
		var before = sim.Actions.ToList();
		var actor = sim.World.StateOf(input.ActorId);
		var after = input switch
		{
			HeadingTurnAction heading => WithCanonicalHeadingTail(
				before,
				heading,
				actor.Maneuverability),
			RollAction roll => WithCanonicalRollTail(
				before,
				roll,
				actor.Maneuverability),
			_ => null,
		};

		if (after is null)
			return false;

		if (!TryReplaceQueue(sim.Fork(), before, after))
			return false;

		return TryReplaceQueue(sim, before, after);
	}

	private static IReadOnlyList<IAction>? WithCanonicalHeadingTail(
		IReadOnlyList<IAction> queue,
		HeadingTurnAction input,
		GrimSpace.Units.Maneuvering.ManeuverabilitySpec maneuverability)
	{
		var yaw = Orientation.IsYawTurn(input.Turn);
		var prefixLength = queue.Count;
		var net = HeadingDelta(input.Turn);
		while (prefixLength > 0
			&& queue[prefixLength - 1] is HeadingTurnAction existing
			&& existing.ActorId == input.ActorId
			&& Orientation.IsYawTurn(existing.Turn) == yaw)
		{
			net += HeadingDelta(existing.Turn);
			prefixLength--;
		}

		var supported = maneuverability.SupportedHeadingTurns
			.Where(turn => Orientation.IsYawTurn(turn) == yaw)
			.Select(turn => new RotationOption<EHeadingTurn>(
				turn,
				HeadingDelta(turn),
				HeadingRank(turn),
				CostOfHeading(maneuverability, turn)))
			.ToArray();
		var tail = CheapestSequence<EHeadingTurn>(supported, net);
		return tail is null
			? null
			: ReplaceTail(
				queue,
				prefixLength,
				tail.Select(turn => (IAction)new HeadingTurnAction(input.ActorId, turn)));
	}

	private static IReadOnlyList<IAction>? WithCanonicalRollTail(
		IReadOnlyList<IAction> queue,
		RollAction input,
		GrimSpace.Units.Maneuvering.ManeuverabilitySpec maneuverability)
	{
		var prefixLength = queue.Count;
		var net = RollDelta(input.Direction);
		while (prefixLength > 0
			&& queue[prefixLength - 1] is RollAction existing
			&& existing.ActorId == input.ActorId)
		{
			net += RollDelta(existing.Direction);
			prefixLength--;
		}

		var supported = maneuverability.SupportedRolls
			.Select(direction => new RotationOption<ERollDirection>(
				direction,
				RollDelta(direction),
				(int)direction,
				CostOfRoll(maneuverability, direction)))
			.ToArray();
		var tail = CheapestSequence<ERollDirection>(supported, net);
		return tail is null
			? null
			: ReplaceTail(
				queue,
				prefixLength,
				tail.Select(direction => (IAction)new RollAction(input.ActorId, direction)));
	}

	private static IReadOnlyList<TAction>? CheapestSequence<TAction>(
		IReadOnlyList<RotationOption<TAction>> supported,
		int netQuarters)
		where TAction : struct, Enum
	{
		var target = Orientation.NormalizeQuarters(netQuarters);
		if (target == 0)
			return [];

		var candidates = new List<(TAction[] Actions, int Cost, int Rank)>();
		Collect([], delta: 0, cost: 0, rank: 0, depth: 0);
		return candidates
			.OrderBy(candidate => candidate.Cost)
			.ThenBy(candidate => candidate.Actions.Length)
			.ThenBy(candidate => candidate.Rank)
			.Select(candidate => (IReadOnlyList<TAction>)candidate.Actions)
			.FirstOrDefault();

		void Collect(List<TAction> actions, int delta, int cost, int rank, int depth)
		{
			if (depth > 0 && Orientation.NormalizeQuarters(delta) == target)
				candidates.Add((actions.ToArray(), cost, rank));
			if (depth == 3)
				return;

			foreach (var option in supported)
			{
				actions.Add(option.Action);
				Collect(
					actions,
					delta + option.Delta,
					cost + option.Cost,
					rank * 10 + option.Rank,
					depth + 1);
				actions.RemoveAt(actions.Count - 1);
			}
		}
	}

	private static IReadOnlyList<IAction> ReplaceTail(
		IReadOnlyList<IAction> queue,
		int prefixLength,
		IEnumerable<IAction> tail)
	{
		var result = new List<IAction>(queue.Count + 1);
		for (var i = 0; i < prefixLength; i++)
			result.Add(queue[i]);
		result.AddRange(tail);
		return result;
	}

	private static bool TryReplaceQueue(
		BattleSimulation sim,
		IReadOnlyList<IAction> before,
		IReadOnlyList<IAction> after)
	{
		var prefix = CommonPrefixLength(before, after);
		sim.Dequeue(prefix);
		if (sim.TryEnqueue(keepRecords: true, actions: [..after.Skip(prefix)])
			&& sim.InvariantStatus != InvariantStatus.Impossible)
			return true;

		sim.Dequeue(prefix);
		return false;
	}

	private static int CommonPrefixLength(IReadOnlyList<IAction> left, IReadOnlyList<IAction> right)
	{
		var n = System.Math.Min(left.Count, right.Count);
		var i = 0;
		while (i < n && Equals(left[i], right[i]))
			i++;
		return i;
	}

	private static int CostOfHeading(
		GrimSpace.Units.Maneuvering.ManeuverabilitySpec maneuverability,
		EHeadingTurn turn) =>
		maneuverability.TryGetHeadingMpCost(turn, out var cost)
			? cost
			: throw new InvalidOperationException($"Unsupported heading turn '{turn}'.");

	private static int CostOfRoll(
		GrimSpace.Units.Maneuvering.ManeuverabilitySpec maneuverability,
		ERollDirection direction) =>
		maneuverability.TryGetRollMpCost(direction, out var cost)
			? cost
			: throw new InvalidOperationException($"Unsupported roll '{direction}'.");

	private static int HeadingDelta(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight or EHeadingTurn.PitchUp => 1,
			EHeadingTurn.YawLeft or EHeadingTurn.PitchDown => -1,
			EHeadingTurn.Yaw180 => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static int HeadingRank(EHeadingTurn turn) =>
		turn switch
		{
			EHeadingTurn.YawRight => 0,
			EHeadingTurn.YawLeft => 1,
			EHeadingTurn.Yaw180 => 2,
			EHeadingTurn.PitchUp => 0,
			EHeadingTurn.PitchDown => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(turn), turn, null),
		};

	private static int RollDelta(ERollDirection direction) =>
		direction switch
		{
			ERollDirection.Clockwise => 1,
			ERollDirection.CounterClockwise => -1,
			_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
		};

	private readonly record struct RotationOption<TAction>(
		TAction Action,
		int Delta,
		int Rank,
		int Cost)
		where TAction : struct, Enum;
}
