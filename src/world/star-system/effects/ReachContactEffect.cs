using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ReachContactEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly string _initiatorId;
	private readonly string _targetId;

	public ReachContactEffect(string initiatorId, string targetId)
	{
		_initiatorId = initiatorId;
		_targetId = targetId;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var initiator = world.StateOf(_initiatorId);
		if (initiator.CurrentEngagement is { } engagement)
			initiator.CurrentEngagement = engagement with { Phase = EEngagementPhase.AwaitingDecision };

		if (initiator.Type != EType.PlayerFleet
			&& world.FleetRegistry.TryGet(_targetId, out var target)
			&& target.State.Type == EType.PlayerFleet
			&& initiator.CurrentEngagement is { } initiatorEngagement)
		{
			target.State.CurrentEngagement = target.State.CurrentEngagement is { } targetEngagement
				? targetEngagement with { Phase = EEngagementPhase.AwaitingDecision }
				: new Engagement(
					initiatorEngagement.Id,
					EEngagementPhase.AwaitingDecision,
					initiatorEngagement.InitiatorFleetId,
					new HashSet<string>(StringComparer.Ordinal),
					HuntedBy: _initiatorId,
					Hunting: null);
		}

		var targetPhase = world.FleetRegistry.TryGet(_targetId, out var reachedTarget)
			? EngagementState.Phase(reachedTarget.State)
			: EEngagementPhase.None;
		GameLog.Log(
			$"[star-map] contact reached initiator={_initiatorId} target={_targetId} " +
			$"initiatorPhase={EngagementState.Phase(initiator)} targetPhase={targetPhase}");

		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
