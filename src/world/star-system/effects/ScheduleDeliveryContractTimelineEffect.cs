using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ScheduleDeliveryContractTimelineEffect(
	string contractId,
	string playerFleetId,
	int acceptedAtTick,
	DeliveryProgress progress,
	DeliveryGenerationConfig config) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (progress.InterceptionState == EDeliveryInterceptionState.Pending)
		{
			world.Timeline.Schedule(
				config.InterceptionLeadTicks,
				new AttemptDeliveryInterceptionAction(
					StarSystemActorIds.Contracts,
					contractId,
					playerFleetId));
		}

		if (progress.DeadlineTick is int deadlineTick)
		{
			var failAtTick = deadlineTick + 1;
			world.Timeline.Schedule(
				failAtTick - acceptedAtTick,
				new FailDeliveryDeadlineAction(
					StarSystemActorIds.Contracts,
					contractId));
		}

		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
