using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Core.Actions;
using GrimSpace.Units.Maneuvering;

namespace GrimSpace.Battle.Actions;

public sealed record RollAction(
	string ActorId,
	ERollDirection Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RollDef.Instance;
}

public sealed class RollDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static RollDef Instance { get; } = new();

	private static readonly ERollDirection[] AllDirections = Enum.GetValues<ERollDirection>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		foreach (var direction in AllDirections)
		{
			if (actor.Maneuverability.TryGetRollMpCost(direction, out _))
				yield return Bind(actorId, direction);
		}
	}

	public RollAction Bind(string actorId, ERollDirection direction) => new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(RollAction action, BattleWorld world, ActorRuntime runtime) =>
		world.StateOf(action.ActorId).Maneuverability.TryGetRollMpCost(action.Direction, out _);

	public bool IsLegal(RollAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		return actor.Maneuverability.TryGetRollMpCost(action.Direction, out var cost)
			&& actor.ManeuverPoints >= cost;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RollAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		if (!actor.Maneuverability.TryGetRollMpCost(action.Direction, out var cost))
		{
			throw new InvalidOperationException(
				$"Actor '{actor.Id}' does not support roll '{action.Direction}'.");
		}

		return
		[
			new RollEffect(action.Direction),
			new MpChangeEffect(-cost),
		];
	}

	private static RollAction Cast(IAction action) =>
		action as RollAction ?? throw new ArgumentException($"Expected {nameof(RollAction)}.", nameof(action));
}
