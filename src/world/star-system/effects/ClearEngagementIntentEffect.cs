using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ClearEngagementIntentEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _initiatorId;

	public ClearEngagementIntentEffect(string initiatorId) => _initiatorId = initiatorId;

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var initiator = world.StateOf(_initiatorId);
		if (initiator.CurrentEngagement?.Hunting is not { } targetId)
			return [];

		initiator.CurrentEngagement = null;

		if (world.FleetRegistry.TryGet(targetId, out var target))
			EngagementState.ClearHuntedBy(target.State, _initiatorId);

		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
