using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RollAction(
	string ActorId,
	ERollDirection Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RollDef.Instance;
}

public sealed class RollDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActionStreamline
{
	public static RollDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var direction in Enum.GetValues<ERollDirection>())
		{
			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public RollAction Bind(string actorId, ERollDirection direction) => new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsLegal(RollAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).ActionPoints >= CombatConfig.RollApCost;

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RollAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
	[
		new RollEffect(action.Direction),
		new ApChangeEffect(-CombatConfig.RollApCost),
	];

	public IReadOnlyList<IAction>? Streamline(
		IReadOnlyList<IAction> queue,
		IAction? input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal)
	{
		if (input is RollAction roll)
			return StreamlineRollButton(queue, roll, isCandidateLegal);

		if (input is not null)
			return null;

		return Compact(queue);
	}

	private static IReadOnlyList<IAction>? StreamlineRollButton(
		IReadOnlyList<IAction> queue,
		RollAction input,
		Func<IReadOnlyList<IAction>, bool> isCandidateLegal)
	{
		var actorId = input.ActorId;
		var (prefixLength, net) = TrailingRollNet(queue, actorId);
		net += RollDelta(input.Direction);

		for (var attempt = 0; attempt < 4; attempt++)
		{
			var candidate = WithRollTail(queue, prefixLength, actorId, net);
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
			if (actions[index] is RollAction first)
			{
				var actorId = first.ActorId;
				var net = RollDelta(first.Direction);
				index++;

				while (index < actions.Count
					&& actions[index] is RollAction next
					&& next.ActorId == actorId)
				{
					net += RollDelta(next.Direction);
					index++;
				}

				result.AddRange(ActionsForNetRoll(actorId, net));
				continue;
			}

			result.Add(actions[index]);
			index++;
		}

		return result;
	}

	private static List<IAction> WithRollTail(
		IReadOnlyList<IAction> queue,
		int prefixLength,
		string actorId,
		int net)
	{
		var candidate = new List<IAction>(prefixLength + 1);
		for (var i = 0; i < prefixLength; i++)
			candidate.Add(queue[i]);
		candidate.AddRange(ActionsForNetRoll(actorId, net));
		return candidate;
	}

	private static (int PrefixLength, int Net) TrailingRollNet(IReadOnlyList<IAction> queue, string actorId)
	{
		var net = 0;
		var prefixLength = queue.Count;
		while (prefixLength > 0
			&& queue[prefixLength - 1] is RollAction roll
			&& roll.ActorId == actorId)
		{
			net += RollDelta(roll.Direction);
			prefixLength--;
		}

		return (prefixLength, net);
	}

	private static IEnumerable<RollAction> ActionsForNetRoll(string actorId, int netQuarters) =>
		RollsForNet(netQuarters).Select(direction => new RollAction(actorId, direction));

	private static IEnumerable<ERollDirection> RollsForNet(int netQuarters) =>
		Orientation.NormalizeQuarters(netQuarters) switch
		{
			0 => [],
			1 => [ERollDirection.Clockwise],
			2 => [ERollDirection.Clockwise, ERollDirection.Clockwise],
			3 => [ERollDirection.CounterClockwise],
			_ => throw new InvalidOperationException($"Unexpected net roll quarters: {netQuarters}."),
		};

	private static int RollDelta(ERollDirection direction) =>
		direction switch
		{
			ERollDirection.Clockwise => 1,
			ERollDirection.CounterClockwise => -1,
			_ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
		};

	private static RollAction Cast(IAction action) =>
		action as RollAction ?? throw new ArgumentException($"Expected {nameof(RollAction)}.", nameof(action));
}
