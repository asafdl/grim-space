using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ClearPursueContactEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _actorId;

	public ClearPursueContactEffect(string actorId) => _actorId = actorId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var change = ClearPursuit(world, _actorId);
		return change is null ? [] : [new Record<FleetPursuitChanged>(change)];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }

	internal static FleetPursuitChanged? ClearPursuit(StarMap world, string actorId)
	{
		var state = world.StateOf(actorId);
		state.TravelTarget = TravelTarget.None;

		if (state.CurrentEngagement?.Hunting is not { } targetId)
			return null;

		state.CurrentEngagement = null;

		if (world.FleetRegistry.TryGet(targetId, out var target))
			EngagementState.ClearHuntedBy(target.State, actorId);

		return new FleetPursuitChanged(actorId, targetId, false);
	}
}
