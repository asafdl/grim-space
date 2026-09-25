using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetPendingWreckContractEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _actorId;
	private readonly string _contractId;

	public SetPendingWreckContractEffect(string actorId, string contractId)
	{
		_actorId = actorId;
		_contractId = contractId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.StateOf(_actorId).PendingWreckContractId = _contractId;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}

public sealed class ClearPendingWreckContractEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _actorId;

	public ClearPendingWreckContractEffect(string actorId) => _actorId = actorId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.StateOf(_actorId).PendingWreckContractId = "";
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
