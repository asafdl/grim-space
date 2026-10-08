using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record MoveStepAction(
	string ActorId,
	ESpatialOrientation Direction = ESpatialOrientation.Forward) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MoveDef.Instance;
}

public sealed class MoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static MoveDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] AllDirections = Enum.GetValues<ESpatialOrientation>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var actor = world.StateOf(actorId);
		foreach (var direction in AllDirections)
		{
			if (!actor.Maneuverability.TryGetTranslationApCost(direction, out _))
				continue;

			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public MoveStepAction Bind(
		string actorId,
		ESpatialOrientation direction = ESpatialOrientation.Forward) =>
		new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		if (actor.Projectile is not null
			|| !actor.Maneuverability.TryGetTranslationApCost(action.Direction, out _))
			return false;

		var destination = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		return world.Grid.IsInBounds(destination)
			&& !world.IsCellBlocked(destination);
	}

	public bool IsLegal(MoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var actor = world.StateOf(action.ActorId);
		return actor.Maneuverability.TryGetTranslationApCost(action.Direction, out var cost)
			&& actor.ActionPoints >= cost;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		MoveStepAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		if (!actor.Maneuverability.TryGetTranslationApCost(action.Direction, out var cost))
		{
			throw new InvalidOperationException(
				$"Actor '{actor.Id}' does not support translation '{action.Direction}'.");
		}

		var destination = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		return
		[
			new MoveEffect(destination),
			new ApChangeEffect(-cost),
		];
	}

	private static MoveStepAction Cast(IAction action) =>
		action as MoveStepAction ?? throw new ArgumentException($"Expected {nameof(MoveStepAction)}.", nameof(action));
}
