using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed record ReserveWorkOnArrivalEffect(string UnitId, string DockId)
	: IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		WorkScheduler.ReserveOnArrival(world, UnitId, DockId);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
