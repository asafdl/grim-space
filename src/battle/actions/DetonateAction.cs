using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Units;
using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record DetonateAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		DetonateDef.Instance;
}

public sealed class DetonateDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef
{
	public static DetonateDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public DetonateAction Bind(string actorId) => new(actorId);

	IAction IActorActionDef.Bind(string actorId) => Bind(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(DetonateAction action, BattleWorld world, ActorRuntime runtime) =>
		UnitRegistry.For(world).Contains(action.ActorId);

	public bool IsLegal(DetonateAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!IsPossible(action, world, runtime))
			return false;

		var actor = world.StateOf(action.ActorId);
		if (actor.FuelRemaining <= 0)
			return true;

		return HasOpponentInBlast(world, action.ActorId, actor.Position);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		DetonateAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var origin = world.StateOf(action.ActorId).Position;
		var cells = Manhattan.EnumerateBall(origin, TorpedoConfig.BlastRadius)
			.Where(world.Grid.IsInBounds)
			.ToHashSet();

		return
		[
			new ResolveHazardEffect(
				EHazardKind.TorpedoBlast,
				cells,
				TorpedoConfig.BlastDamage,
				momentumLoss: 0),
		];
	}

	public static bool HasOpponentInBlast(BattleWorld world, string actorId, Coord origin)
	{
		var units = UnitRegistry.For(world);
		var actor = units.UnitOf(actorId);
		foreach (var unit in units.Except(actorId))
		{
			if (!unit.State.IsAlive || actor.RelationTo(unit) != EUnitRelation.Opponent)
				continue;
			if (origin.ManhattanDistanceTo(unit.State.Position) <= TorpedoConfig.BlastRadius)
				return true;
		}

		return false;
	}

	private static DetonateAction Cast(IAction action) =>
		action as DetonateAction ?? throw new ArgumentException($"Expected {nameof(DetonateAction)}.", nameof(action));
}
