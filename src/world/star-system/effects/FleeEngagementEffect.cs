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
		ResolveFlee(world.StateOf(_fleeingUnitId));
		ClearCounterpartyHuntLink(world.StateOf(_fleeingUnitId), world.StateOf(_counterpartyId));
		return [];
	}

	private static void ResolveFlee(State state)
	{
		state.EngagementTargetUnitId = null;
		state.HuntedByUnitId = null;
		state.EngagementInitiatorUnitId = null;
		state.ClearEngagedWith();
		state.EngagementPhase = EEngagementPhase.Resolved;
		state.ResolvedEngagementOutcome = null;
	}

	private static void ClearCounterpartyHuntLink(State fleeing, State counterparty)
	{
		if (counterparty.HuntedByUnitId == fleeing.Id)
			counterparty.HuntedByUnitId = null;
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
