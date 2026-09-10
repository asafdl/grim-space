using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class PlayerInputEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly bool _waiting;

	public PlayerInputEffect(bool waiting) => _waiting = waiting;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		world.WaitingForPlayerInput = _waiting;
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
