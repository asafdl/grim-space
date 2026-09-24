using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record DeliveryTurnedIn(string contractId);

public sealed class RecordDeliveryTurnedInEffect(string contractId) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<DeliveryTurnedIn>(new DeliveryTurnedIn(contractId))];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
