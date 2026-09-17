using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class FleeEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _fleeingUnitId;
	private readonly string _counterpartyId;

	public FleeEngagementEffect(string fleeingUnitId, string counterpartyId)
	{
		_fleeingUnitId = fleeingUnitId;
		_counterpartyId = counterpartyId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var fleeing = world.StateOf(_fleeingUnitId);
		var counterparty = world.StateOf(_counterpartyId);
		fleeing.CurrentEngagement = null;
		EngagementState.ClearHuntedBy(counterparty, fleeing.Id);
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
