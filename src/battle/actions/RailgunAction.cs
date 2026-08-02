using GrimSpace.Battle.Weapons;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RailgunDef.Instance;
}

public sealed class RailgunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public RailgunAction Bind(string actorId) => new(actorId);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(RailgunAction action, BattleWorld world, ActorRuntime runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		return WeaponBursts.IsValidRailgunBurst(frame, world.Grid.IsInBounds);
	}

	public bool IsLegal(RailgunAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.StateOf(action.ActorId).RailgunRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		RailgunAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var cells = WeaponBursts.RailgunBurstCells(frame, world.Grid.IsInBounds);

		return
		[
			new ScheduleActionEffect(
				CombatConfig.RailgunResolveDelay,
				ResolveHazardDef.Instance.Bind(
					action.ActorId,
					EHazardKind.RailgunBurst,
					cells,
					damage: CombatConfig.RailgunDamage,
					CombatConfig.RailgunMomentumLoss)),
			new RailgunChangeEffect(-1),
		];
	}

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
