using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public enum EDeliveryInterceptionRetryReason
{
	PlayerNotInTransit,
	PlayerEngaged,
	NoEligibleInterceptor,
}

public sealed class RescheduleDeliveryInterceptionAttemptEffect(
	AttemptDeliveryInterceptionAction attempt,
	EDeliveryInterceptionRetryReason reason,
	int delayTicks) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.Timeline.Schedule(delayTicks, attempt);
		Contracts.DeliveryDiagnostics.RetryInterception(
			attempt.ContractId,
			attempt.PlayerFleetId,
			world.Timeline.Clock.Current,
			reason,
			delayTicks);
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
