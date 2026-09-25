using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ClearPursueContactEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _actorId;

	public ClearPursueContactEffect(string actorId) => _actorId = actorId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var state = world.StateOf(_actorId);
		state.TravelTarget = TravelTarget.None;

		if (state.CurrentEngagement?.Hunting is not { } targetId)
			return [];

		state.CurrentEngagement = null;

		if (world.FleetRegistry.TryGet(targetId, out var target))
			EngagementState.ClearHuntedBy(target.State, _actorId);

		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
