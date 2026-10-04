using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class PauseContractIssuerGenerationEffect(string poiId, int untilTick)
	: IEffect<StarMap, ActorRuntime>
{
	private int? _previousUntilTick;
	private bool _applied;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_previousUntilTick = world.ContractRegistry.TryGetIssuerGenerationCooldown(
			poiId,
			out var previousUntilTick)
			? previousUntilTick
			: null;
		world.ContractRegistry.PauseIssuerGeneration(poiId, untilTick);
		_applied = true;
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (!_applied)
			throw new InvalidOperationException("Contract issuer generation pause was not applied.");

		world.ContractRegistry.RestoreIssuerGenerationCooldown(poiId, _previousUntilTick);
		_previousUntilTick = null;
		_applied = false;
	}
}
