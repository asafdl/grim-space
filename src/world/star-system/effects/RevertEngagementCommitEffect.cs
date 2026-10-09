using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Effects;

/// <summary>
/// Restores pre-battle contact state when a committed engagement fails to launch tactical combat.
/// </summary>
public sealed class RevertEngagementCommitEffect(EngagementCommitted fact) : IEffect<StarMap, Runtime.ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		if (fact.ParticipantFleetIds.Count < 2)
		{
			GameLog.Log(
				$"[star-map] engagement launch rollback skipped: engagement '{fact.EngagementId}' has fewer than two participants.");
			return [];
		}

		var initiatorId = fact.InitiatorFleetId;
		var counterpartyId = fact.ParticipantFleetIds.First(
			id => !string.Equals(id, initiatorId, StringComparison.Ordinal));
		if (!world.FleetRegistry.TryGet(initiatorId, out var initiator)
			|| !world.FleetRegistry.TryGet(counterpartyId, out var counterparty))
		{
			GameLog.Log(
				$"[star-map] engagement launch rollback skipped: participant missing for engagement '{fact.EngagementId}'.");
			return [];
		}

		var participants = new HashSet<string>(fact.ParticipantFleetIds, StringComparer.Ordinal);
		initiator.State.CurrentEngagement = new Engagement(
			fact.EngagementId,
			EEngagementPhase.AwaitingDecision,
			initiatorId,
			participants,
			HuntedBy: null,
			Hunting: counterpartyId);
		counterparty.State.CurrentEngagement = new Engagement(
			fact.EngagementId,
			EEngagementPhase.AwaitingDecision,
			initiatorId,
			new HashSet<string>(StringComparer.Ordinal),
			HuntedBy: initiatorId,
			Hunting: null);
		world.WaitingForPlayerInput = true;

		GameLog.Log(
			$"[star-map] engagement launch rolled back: engagement='{fact.EngagementId}' " +
			$"initiator={initiatorId} counterparty={counterpartyId}");
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
