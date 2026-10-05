using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public record WreckInvestigated(string contractId, string wreckageId);

public sealed class RecordWreckInvestigatedEffect(string contractId, string wreckageId)
	: IEffect<StarMap, ActorRuntime>
{
	private ContractState? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not WreckageContractState wreckage)
			return [];

		_previous = state;
		world.ContractRegistry.ReplaceState(wreckage with { Investigated = true });
		return [new Record<WreckInvestigated>(new WreckInvestigated(contractId, wreckageId))];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_previous is not null)
			world.ContractRegistry.ReplaceState(_previous);
		_previous = null;
	}
}
