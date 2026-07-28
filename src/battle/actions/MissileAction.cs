using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Weapons;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record MissileAction(
	string ActorId,
	Coord Center,
	EMissileMount Mount,
	int Range) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		MissileDef.For(Mount, Range);
}

public sealed class MissileDef(EMissileMount mount, int range)
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>
{
	public EMissileMount Mount { get; } = mount;
	public int Range { get; } = range;

	public static MissileDef For(EMissileMount mount, int range) => new(mount, range);

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var frame = BodyFrame.From(world.StateOf(actorId));
		var config = MissileMountConfig.For(Mount).WithRange(Range);
		foreach (var cell in MissileTargeting.GetValidCells(frame, config, world.Grid.IsInBounds))
		{
			var action = Bind(actorId, cell);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public MissileAction Bind(string actorId, Coord center) => new(actorId, center, Mount, Range);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(MissileAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.StateOf(action.ActorId).MissilesRemaining <= 0)
			return false;

		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var config = MissileMountConfig.For(action.Mount).WithRange(action.Range);
		return MissileTargeting.IsValidTarget(frame, action.Center, config, world.Grid.IsInBounds);
	}

	public bool IsLegal(MissileAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(action, world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		MissileAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = new HashSet<Coord>(
			world.Grid.EnumerateCube(action.Center, CombatConfig.MissileRadius));
		return
		[
			new ScheduleActionEffect(
				CombatConfig.MissileResolveDelay,
				ResolveHazardDef.Instance.Bind(
					action.ActorId,
					EHazardKind.MissileZone,
					cells,
					CombatConfig.MissileDamage,
					CombatConfig.MissileMomentumLoss)),
			new MissileChangeEffect(-1),
		];
	}

	private static MissileAction Cast(IAction action) =>
		action as MissileAction ?? throw new ArgumentException($"Expected {nameof(MissileAction)}.", nameof(action));
}
