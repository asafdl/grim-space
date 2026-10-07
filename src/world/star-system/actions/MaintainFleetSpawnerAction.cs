using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record FleetSpawnerAddition(Fleet Fleet);

public sealed record MaintainFleetSpawnerAction(
	string ActorId,
	int Tick,
	IReadOnlyList<FleetSpawnerAddition> Additions) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		MaintainFleetSpawnerDef.Instance;
}

public sealed class MaintainFleetSpawnerDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static MaintainFleetSpawnerDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is MaintainFleetSpawnerAction maintain
		&& string.Equals(maintain.ActorId, StarSystemActorIds.FleetSpawner, StringComparison.Ordinal)
		&& maintain.Tick == world.Timeline.Clock.Current
		&& AdditionsAreLegal(maintain, world);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var maintain = (MaintainFleetSpawnerAction)action;
		return
		[
			new ExpireRandomAreaSpawnerFleetsEffect(maintain.Tick),
			..maintain.Additions.Select(addition => (IEffect<StarMap, ActorRuntime>)new SpawnMapUnitEffect(addition.Fleet)),
		];
	}

	private static bool AdditionsAreLegal(MaintainFleetSpawnerAction maintain, StarMap world)
	{
		var ids = new HashSet<string>(StringComparer.Ordinal);
		var reserved = new HashSet<Coord>();
		foreach (var addition in maintain.Additions)
		{
			var state = addition.Fleet.State;
			if (state.SpawnerSource != EFleetSpawnerSource.RandomArea
				|| state.FleetSpawnerExpiresAtTick <= maintain.Tick
				|| state.Travel is not FleetTravel.AtRest { Position: var position }
				|| !ids.Add(state.Id)
				|| world.FleetRegistry.Contains(state.Id)
				|| !reserved.Add(position)
				|| OccupancyRules.IsOccupied(world, position, reservedCoords: null, ignoredFleetId: state.Id))
				return false;
		}

		return true;
	}
}
