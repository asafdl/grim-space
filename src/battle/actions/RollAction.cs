using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Movement.Enums;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Weapons;
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

	public IReadOnlyList<IAction> Streamline(IReadOnlyList<IAction> actions, IReadOnlyList<int?> undoGroups)
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

				foreach (var direction in RollsForNet(net))
					result.Add(new RollAction(actorId, direction));

				continue;
			}

			result.Add(actions[index]);
			index++;
		}

		return result;
	}

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
