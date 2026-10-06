using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AssignDeliveryInterceptorEffect(
	string contractId,
	string interceptorFleetId,
	string targetFleetId) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not DeliveryContractState delivery
			|| delivery.Progress.InterceptionState != EDeliveryInterceptionState.Pending
			|| !world.FleetRegistry.TryGet(interceptorFleetId, out var interceptor))
			return [];

		world.ContractRegistry.ReplaceState(delivery.WithInterceptorAssigned(interceptorFleetId));
		interceptor.State.PursuitDirective = new FleetPursuitDirective(contractId, targetFleetId);
		DeliveryDiagnostics.AssignInterceptor(contractId, interceptorFleetId, targetFleetId);
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
