using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record VoidBombMoveStepAction(
	string ActorId,
	ESpatialOrientation Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		VoidBombMoveDef.Instance;
}

public sealed class VoidBombMoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static VoidBombMoveDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] AllDirections = Enum.GetValues<ESpatialOrientation>();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var direction in AllDirections)
		{
			var action = Bind(actorId, direction);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public VoidBombMoveStepAction Bind(string actorId, ESpatialOrientation direction) =>
		new(actorId, direction);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(VoidBombMoveStepAction action, BattleWorld world)
	{
		var actor = world.StateOf(action.ActorId);
		if (actor.Type != EType.VoidBomb)
			return false;

		if (actor.Projectile is null
			|| !actor.Maneuverability.TryGetTranslationApCost(action.Direction, out _))
			return false;

		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		return world.Grid.IsInBounds(to) && !world.IsCellBlocked(to);
	}

	public bool IsLegal(VoidBombMoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world))
			return false;

		var actor = world.StateOf(action.ActorId);
		var stepCost = actor.Maneuverability.TryGetTranslationApCost(action.Direction, out var cost)
			? cost
			: throw new InvalidOperationException($"Unsupported torpedo direction {action.Direction}.");
		return stepCost <= actor.ActionPoints;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		VoidBombMoveStepAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		var stepCost = actor.Maneuverability.TryGetTranslationApCost(action.Direction, out var cost)
			? cost
			: throw new InvalidOperationException($"Unsupported torpedo direction {action.Direction}.");

		return
		[
			new MoveEffect(to),
			new ApChangeEffect(-stepCost),
		];
	}

	private static VoidBombMoveStepAction Cast(IAction action) =>
		action as VoidBombMoveStepAction
			?? throw new ArgumentException($"Expected {nameof(VoidBombMoveStepAction)}.", nameof(action));
}
