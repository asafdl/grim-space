using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Actions;

public sealed record TorpedoMoveStepAction(
	string ActorId,
	ESpatialOrientation Direction) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		TorpedoMoveDef.Instance;
}

public sealed class TorpedoMoveDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static TorpedoMoveDef Instance { get; } = new();

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

	public TorpedoMoveStepAction Bind(string actorId, ESpatialOrientation direction) =>
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

	public bool IsPossible(TorpedoMoveStepAction action, BattleWorld world)
	{
		var actor = world.StateOf(action.ActorId);
		if (actor.Type != EType.Torpedo || TorpedoConfig.MoveApCost(action.Direction) is null)
			return false;

		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		return world.Grid.IsInBounds(to) && !world.BlockedFor(action.ActorId).Contains(to);
	}

	public bool IsLegal(TorpedoMoveStepAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world))
			return false;

		var stepCost = TorpedoConfig.MoveApCost(action.Direction)
			?? throw new InvalidOperationException($"Unsupported torpedo direction {action.Direction}.");
		return stepCost <= world.StateOf(action.ActorId).ActionPoints;
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		TorpedoMoveStepAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var actor = world.StateOf(action.ActorId);
		var to = actor.Position + BodyFrame.From(actor).Step(action.Direction);
		var stepCost = TorpedoConfig.MoveApCost(action.Direction)
			?? throw new InvalidOperationException($"Unsupported torpedo direction {action.Direction}.");

		return
		[
			new MoveEffect(to),
			new ApChangeEffect(-stepCost),
			new HazardCellEntryEffect(to),
		];
	}

	private static TorpedoMoveStepAction Cast(IAction action) =>
		action as TorpedoMoveStepAction
			?? throw new ArgumentException($"Expected {nameof(TorpedoMoveStepAction)}.", nameof(action));
}
