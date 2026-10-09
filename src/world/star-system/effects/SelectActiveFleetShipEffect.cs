using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SelectActiveFleetShipEffect(string shipId) : IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		runtime.SelectedMemberShipId = shipId;
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
	}
}
