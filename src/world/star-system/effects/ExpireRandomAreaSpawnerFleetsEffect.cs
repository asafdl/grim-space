using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.FleetSpawner;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ExpireRandomAreaSpawnerFleetsEffect(int currentTick) : IEffect<StarMap, ActorRuntime>
{
	private readonly List<Fleet> _removed = [];

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_removed.Clear();
		foreach (var fleetId in FleetSpawnerQueries.DueForExpiry(
			world.FleetRegistry,
			EFleetSpawnerSource.RandomArea,
			currentTick))
		{
			if (!world.FleetRegistry.TryGet(fleetId, out var fleet))
				continue;

			_removed.Add(fleet);
			world.FleetRegistry.Remove(fleetId);
		}

		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		foreach (var fleet in _removed)
			world.FleetRegistry.Add(fleet);
	}
}
