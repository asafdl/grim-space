using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Events;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetPendingWorldEventEffect(PendingWorldEvent? pending)
	: IEffect<StarMap, ActorRuntime>
{
	private PendingWorldEvent? _previous;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_previous = world.PendingWorldEvent;
		world.PendingWorldEvent = pending;
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		world.PendingWorldEvent = _previous;
}
