using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SpawnMapUnitEffect : IEffect<StarMap, ActorRuntime>
{
	private readonly Fleet _fleet;

	public SpawnMapUnitEffect(Fleet fleet) => _fleet = fleet;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (world.FleetRegistry.Contains(_fleet.State.Id))
		{
			GameLog.Log($"[star-map] spawn skipped: fleet '{_fleet.State.Id}' already exists.");
			return [];
		}

		world.FleetRegistry.Add(_fleet);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		world.FleetRegistry.Remove(_fleet.State.Id);
}
