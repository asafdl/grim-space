using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ResolveEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly BattleOutcome _outcome;

	public ResolveEngagementEffect(BattleOutcome outcome) => _outcome = outcome;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		foreach (var handoff in _outcome.StateHandoffs)
		{
			if (handoff.HullPoints > 0)
				continue;
			if (!world.FleetRegistry.TryFleetContainingMember(handoff.Id, out var fleet))
				continue;

			var surviving = fleet.Members.Where(member => member.Id != handoff.Id).ToArray();
			if (surviving.Length == 0)
			{
				ResolveDeliveryInterception(world, fleet);
				if (fleet.State.SourceContractId is string contractId
					&& world.ContractRegistry.TryGetState(contractId, out var contractState))
				{
					switch (contractState)
					{
						case HuntContractState hunt:
							world.ContractRegistry.ReplaceState(hunt.MarkFleetDefeated(fleet.State.Id));
							break;
						case WreckageContractState wreckage when wreckage.RequiresAmbushResolution:
							world.ContractRegistry.ReplaceState(wreckage.MarkAmbushCleared());
							break;
					}
				}
				RemoveFleet(world, fleet.State.Id);
			}
			else
				world.FleetRegistry.Replace(new Fleet(fleet.State, surviving));
		}

		foreach (var handoff in _outcome.StateHandoffs)
		{
			if (!world.FleetRegistry.TryFleetContainingMember(handoff.Id, out var fleet))
				continue;

			fleet.State.CurrentEngagement = null;
		}

		return [];
	}

	private static void RemoveFleet(StarMap world, string fleetId)
		=> world.FleetRegistry.Remove(fleetId);

	private static void ResolveDeliveryInterception(StarMap world, Fleet fleet)
	{
		if (fleet.State.PursuitDirective is not { } directive
			|| !world.ContractRegistry.TryGetState(directive.ContractId, out var state)
			|| state.Status != EContractStatus.Active
			|| state is not DeliveryContractState delivery
			|| delivery.Progress.InterceptionState != EDeliveryInterceptionState.Assigned
			|| !string.Equals(
				delivery.Progress.InterceptorFleetId,
				fleet.State.Id,
				StringComparison.Ordinal))
			return;

		world.ContractRegistry.ReplaceState(delivery.WithInterceptorResolved(fleet.State.Id));
		DeliveryDiagnostics.ResolveInterception(directive.ContractId, fleet.State.Id);
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
