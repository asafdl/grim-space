using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class BeginTransitEffect(
	string unitId,
	string destinationDockId,
	TransitPath path) : IEffect<StarMap, EmptyRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, EmptyRuntime runtime, string actorId)
	{
		var state = world.StateOf(unitId);
		if (!state.IsReadyToDepart)
		{
			throw new InvalidOperationException(
				$"Unit '{unitId}' is not ready to begin transit.");
		}

		state.BeginTransit(destinationDockId, path);
		return [];
	}

	public void Undo(StarMap world, EmptyRuntime runtime, string actorId) { }
}
