using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class MarkLeadPilotHireSlotPurchasedEffect(string contractId, int slot)
	: IEffect<StarMap, ActorRuntime>
{
	private LeadContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead)
			return [];

		if (lead.PurchasedPilotHireSlots.Contains(slot))
			return [];

		_previous = lead;
		var next = lead.MarkPilotHireSlotPurchased(slot);
		world.ContractRegistry.ReplaceState(next);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is not null)
			world.ContractRegistry.ReplaceState(_previous);
		_previous = null;
	}
}
