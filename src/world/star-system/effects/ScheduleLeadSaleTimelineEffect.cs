using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ScheduleLeadSaleTimelineEffect(
	string contractId,
	int acceptedAtTick,
	int deadlineTick) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		var failAtTick = deadlineTick + 1;
		world.Timeline.Schedule(
			failAtTick - acceptedAtTick,
			new ExpireLeadSaleAction(StarSystemActorIds.Contracts, contractId));
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
