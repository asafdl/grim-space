using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
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
		IActionInvariants<BattleWorld, ActorRuntime>,
		IActionStreamline
{
	public static HeadingDef Instance { get; } = new();

	private static readonly EHeadingTurn[] SupportedTurns =
	[
		EHeadingTurn.YawLeft,
		EHeadingTurn.YawRight,
		EHeadingTurn.PitchUp,
		EHeadingTurn.PitchDown,
	];

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId) =>
		SupportedTurns
			.Select(turn => Bind(actorId, turn))
			.Where(action => IsPossible(action, world, runtime));

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

	public bool IsPossible(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime) =>
		SupportedTurns.Contains(action.Turn);

	public bool IsLegal(HeadingTurnAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(action, world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		HeadingTurnAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		[new HeadingTurnEffect(action.Turn)];

	public InvariantStatus EvaluateInvariants(
		BattleWorld world,
		ActorRuntime runtime,
		IReadOnlyList<IAction> actions,
		string actorId) =>
		ManeuverInvariant.Evaluate(world, runtime, actions, actorId);

	public IReadOnlyList<IAction>? Streamline(
		IReadOnlyList<IAction> queue,
		IAction? input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal)
	{
		if (input is HeadingTurnAction heading)
		{
			if (Orientation.IsYawTurn(heading.Turn))
				return StreamlineYawButton(queue, heading, isCandidateLegal);
			if (IsPitchTurn(heading.Turn))
				return StreamlinePitchButton(queue, heading, isCandidateLegal);
			return null;
		}

		if (input is not null)
			return null;

		return Compact(queue);
	}

	private static IReadOnlyList<IAction>? StreamlineYawButton(
		IReadOnlyList<IAction> queue,
		HeadingTurnAction input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal)
	{
		var actorId = input.ActorId;
		var (prefixLength, net) = TrailingYawNet(queue, actorId);
		net += YawDelta(input.Turn);

		for (var attempt = 0; attempt < 4; attempt++)
		{
			var candidate = WithYawTail(queue, prefixLength, actorId, net);
			if (isCandidateLegal(candidate))
				return candidate;

			net++;
		}

		return null;
	}

	private static IReadOnlyList<IAction>? StreamlinePitchButton(
		IReadOnlyList<IAction> queue,
		HeadingTurnAction input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal)
	{
		var actorId = input.ActorId;
		var (prefixLength, net) = TrailingPitchNet(queue, actorId);
		net += PitchDelta(input.Turn);

		for (var attempt = 0; attempt < 4; attempt++)
		{
			var candidate = WithPitchTail(queue, prefixLength, actorId, net);
			if (isCandidateLegal(candidate))
				return candidate;

			net++;
		}

		return null;
	}

	private static List<IAction> Compact(IReadOnlyList<IAction> actions)
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

				result.AddRange(ActionsForNetYaw(actorId, net));
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

				result.AddRange(ActionsForNetPitch(actorId, net));
				continue;
			}

			result.Add(actions[index]);
			index++;
		}

		return result;
	}

	private static List<IAction> WithYawTail(
		IReadOnlyList<IAction> queue,
		int prefixLength,
		string actorId,
		int net)
	{
		var candidate = new List<IAction>(prefixLength + 1);
		for (var i = 0; i < prefixLength; i++)
			candidate.Add(queue[i]);
		candidate.AddRange(ActionsForNetYaw(actorId, net));
		return candidate;
	}

	private static List<IAction> WithPitchTail(
		IReadOnlyList<IAction> queue,
		int prefixLength,
		string actorId,
		int net)
	{
		var candidate = new List<IAction>(prefixLength + 1);
		for (var i = 0; i < prefixLength; i++)
			candidate.Add(queue[i]);
		candidate.AddRange(ActionsForNetPitch(actorId, net));
		return candidate;
	}

	private static (int PrefixLength, int Net) TrailingYawNet(IReadOnlyList<IAction> queue, string actorId)
	{
		var net = 0;
		var prefixLength = queue.Count;
		while (prefixLength > 0
			&& queue[prefixLength - 1] is HeadingTurnAction heading
			&& heading.ActorId == actorId
			&& Orientation.IsYawTurn(heading.Turn))
		{
			net += YawDelta(heading.Turn);
			prefixLength--;
		}

		return (prefixLength, net);
	}

	private static (int PrefixLength, int Net) TrailingPitchNet(IReadOnlyList<IAction> queue, string actorId)
	{
		var net = 0;
		var prefixLength = queue.Count;
		while (prefixLength > 0
			&& queue[prefixLength - 1] is HeadingTurnAction heading
			&& heading.ActorId == actorId
			&& IsPitchTurn(heading.Turn))
		{
			net += PitchDelta(heading.Turn);
			prefixLength--;
		}

		return (prefixLength, net);
	}

	private static IEnumerable<HeadingTurnAction> ActionsForNetYaw(string actorId, int netQuarters) =>
		TurnsForNetYaw(netQuarters).Select(turn => new HeadingTurnAction(actorId, turn));

	private static IEnumerable<HeadingTurnAction> ActionsForNetPitch(string actorId, int netQuarters) =>
		TurnsForNetPitch(netQuarters).Select(turn => new HeadingTurnAction(actorId, turn));

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
