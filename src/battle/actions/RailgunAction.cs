using GrimSpace.Battle.Abilities;
using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record RailgunAction(string ActorId) : IAction<BattleWorld, ActorRuntime>
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		RailgunDef.Instance;
}

public sealed class RailgunDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IActorActionDef,
		IAreaActionDef
{
	public static RailgunDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		var action = Bind(actorId);
		if (IsPossible(action, world, runtime))
			yield return action;
	}

	public RailgunAction Bind(string actorId) => new(actorId);

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

	public bool IsPossible(RailgunAction action, BattleWorld world, ActorRuntime runtime)
		=> AffectedCells(action, world).Count > 0;

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
		var cells = AffectedCells(action, world);

		return
		[
			new ResolveHazardEffect(
				EHazardKind.RailgunBurst,
				cells,
				CombatConfig.RailgunDamage,
				CombatConfig.RailgunMomentumLoss),
			new RailgunChangeEffect(-1),
		];
	}

	public HashSet<Coord> AffectedCells(RailgunAction action, BattleWorld world)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var result = new HashSet<Coord>();

		for (var fore = 1; fore <= CombatConfig.RailgunLineLength; fore++)
		{
			var cell = frame.ToWorld(fore, 0, 0);
			if (world.Grid.IsInBounds(cell))
				result.Add(cell);
		}

		for (var depth = 0; depth <= CombatConfig.RailgunPyramidRange; depth++)
		{
			var fore = CombatConfig.RailgunLineLength + depth;
			for (var port = -depth; port <= depth; port++)
			{
				for (var dorsal = -depth; dorsal <= depth; dorsal++)
				{
					if (System.Math.Abs(port) + System.Math.Abs(dorsal) > depth)
						continue;

					var cell = frame.ToWorld(fore, port, dorsal);
					if (world.Grid.IsInBounds(cell))
						result.Add(cell);
				}
			}
		}

		return result;
	}

	IReadOnlySet<Coord> IAreaActionDef.AffectedCells(IAction action, BattleWorld world) =>
		AffectedCells(Cast(action), world);

	private static RailgunAction Cast(IAction action) =>
		action as RailgunAction ?? throw new ArgumentException($"Expected {nameof(RailgunAction)}.", nameof(action));
}
