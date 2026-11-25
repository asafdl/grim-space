using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ReplaceStrategicPilotCapabilitiesEffect(
	IReadOnlyList<PilotCapabilityGrant> strategicGrants)
	: IEffect<StarMap, ActorRuntime>
{
	private IReadOnlyList<PilotCapabilityGrant> _previous = [];

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_previous = runtime.StrategicCapabilities;
		runtime.ReplaceStrategicCapabilities(strategicGrants);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		runtime.ReplaceStrategicCapabilities(_previous);
}
