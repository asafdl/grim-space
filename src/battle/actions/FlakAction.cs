using GrimSpace.Battle.World;
using GrimSpace.Battle.Effects;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.Spatial;
using GrimSpace.Battle.Abilities;
using GrimSpace.Core.Actions;
using GrimSpace.Math.Grid;

namespace GrimSpace.Battle.Actions;

public sealed record FlakAction(
	string ActorId,
	ESpatialOrientation MountedOn) : IAction<BattleWorld, ActorRuntime>, IMountedAction
{
	public IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>> Definition =>
		FlakDef.Instance;
}

public sealed class FlakDef
	: IActionDef<IAction, BattleWorld, ActorRuntime, IEffect<BattleWorld, ActorRuntime>>,
		IMountedActionDef,
		IAreaActionDef
{
	public static FlakDef Instance { get; } = new();

	private static readonly ESpatialOrientation[] MountedOn =
	[
		ESpatialOrientation.Port,
		ESpatialOrientation.Starboard,
	];

	public IEnumerable<IAction> Discover(BattleWorld world, ActorRuntime runtime, string actorId)
	{
		foreach (var mountedOn in MountedOn)
		{
			var action = Bind(actorId, mountedOn);
			if (IsPossible(action, world, runtime))
				yield return action;
		}
	}

	public FlakAction Bind(string actorId, ESpatialOrientation mountedOn) =>
		new(actorId, mountedOn);

	public bool SupportsMount(ESpatialOrientation mountedOn) =>
		MountedOn.Contains(mountedOn);

	IAction IMountedActionDef.Bind(string actorId, ESpatialOrientation mountedOn) =>
		Bind(actorId, mountedOn);

	public bool IsPossible(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsPossible(Cast(action), world, runtime);

	public bool IsLegal(IAction action, BattleWorld world, ActorRuntime runtime) =>
		IsLegal(Cast(action), world, runtime);

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		IAction action,
		BattleWorld world,
		ActorRuntime runtime) =>
		Resolve(Cast(action), world, runtime);

	public bool IsPossible(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (!SupportsMount(action.MountedOn))
			return false;

		return AffectedCells(action, world).Count > 0;
	}

	public bool IsLegal(FlakAction action, BattleWorld world, ActorRuntime runtime)
	{
		if (world.StateOf(action.ActorId).FlakRemaining <= 0)
			return false;

		return IsPossible(action, world, runtime);
	}

	public IReadOnlyList<IEffect<BattleWorld, ActorRuntime>> Resolve(
		FlakAction action,
		BattleWorld world,
		ActorRuntime runtime)
	{
		var cells = AffectedCells(action, world);

		return
		[
			new ResolveHazardEffect(
				EHazardKind.FlakBurst,
				cells,
				CombatConfig.FlakDamage,
				CombatConfig.FlakMomentumLoss),
			new FlakChangeEffect(-1),
		];
	}

	public HashSet<Coord> AffectedCells(FlakAction action, BattleWorld world)
	{
		var frame = BodyFrame.From(world.StateOf(action.ActorId));
		var result = new HashSet<Coord>();
		var (apexPort, outwardStep) = BurstAxes(action.MountedOn);

		for (var outward = 0; outward <= CombatConfig.FlakRange; outward++)
		{
			for (var fore = -outward; fore <= outward; fore++)
			{
				for (var dorsal = -outward; dorsal <= outward; dorsal++)
				{
					if (System.Math.Abs(fore) + System.Math.Abs(dorsal) > outward)
						continue;

					var port = apexPort + outwardStep * outward;
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

	private static (int ApexPort, int OutwardStep) BurstAxes(ESpatialOrientation mountedOn) =>
		mountedOn switch
		{
			ESpatialOrientation.Port => (1, 1),
			ESpatialOrientation.Starboard => (-1, -1),
			_ => throw new ArgumentOutOfRangeException(nameof(mountedOn), mountedOn, null),
		};

	private static FlakAction Cast(IAction action) =>
		action as FlakAction ?? throw new ArgumentException($"Expected {nameof(FlakAction)}.", nameof(action));
}
