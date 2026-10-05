using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class FleeEngagementEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private const int CounterpartyCooldownTicks = 10;

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
		counterparty.CurrentEngagement = null;
		world.Timeline.Schedule(
			1,
			new BeginActionCooldownAction(_counterpartyId, CounterpartyCooldownTicks));
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
