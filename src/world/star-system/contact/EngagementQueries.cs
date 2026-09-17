using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

public readonly record struct PendingEngagement(
	string CounterpartyUnitId,
	EType CounterpartyType,
	EFaction CounterpartyFaction,
	EDangerLevel Danger);

public readonly record struct CommittedEngagement(
	string EngagementId,
	string InitiatorUnitId,
	IReadOnlyList<string> ParticipantUnitIds);

public static class EngagementQueries
{
	public static bool TryGetPendingPlayerEngagement(
		StarMap world,
		string playerId,
		out PendingEngagement info)
	{
		info = default;
		if (!world.FleetRegistry.TryGet(playerId, out var player))
			return false;

		if (player.State.CurrentEngagement?.Phase != EEngagementPhase.AwaitingDecision)
			return false;

		var counterpartyId = ResolveCounterpartyId(player.State);
		if (counterpartyId is null
			|| !world.FleetRegistry.TryGet(counterpartyId, out var counterparty))
			return false;

		var profile = counterparty.State.CombatProfile
			?? throw new InvalidOperationException(
				$"Engagement counterparty '{counterpartyId}' has no combat profile.");

		info = new PendingEngagement(
			counterpartyId,
			counterparty.State.Type,
			counterparty.State.Faction,
			profile.Danger);
		return true;
	}

	public static bool TryGetCommittedPlayerEngagement(
		StarMap world,
		string playerId,
		out CommittedEngagement info)
	{
		info = default;
		if (!world.FleetRegistry.TryGet(playerId, out var player))
			return false;

		if (player.State.CurrentEngagement is not { Phase: EEngagementPhase.Engaged } engagement)
			return false;

		if (engagement.EngagementParticipantIds.Count == 0)
			return false;

		if (!engagement.EngagementParticipantIds.Any(
			id => id != playerId && world.FleetRegistry.Contains(id)))
			return false;

		var participants = engagement.EngagementParticipantIds
			.OrderBy(id => id, StringComparer.Ordinal)
			.ToArray();

		info = new CommittedEngagement(
			engagement.Id,
			engagement.InitiatorFleetId,
			participants);
		return true;
	}

	internal static string? ResolveCounterpartyId(State state)
	{
		if (state.CurrentEngagement is not { } engagement)
			return null;

		if (engagement.Hunting is { } hunting)
			return hunting;

		if (engagement.HuntedBy is { } huntedBy)
			return huntedBy;

		return engagement.EngagementParticipantIds.FirstOrDefault(id => id != state.Id);
	}

	public static bool IsHunterInEngageRange(
		Coord hunterPosition,
		Coord targetPosition,
		double hunterEngageRadius)
	{
		var dx = hunterPosition.X - targetPosition.X;
		var dz = hunterPosition.Z - targetPosition.Z;
		var distanceSquared = (long)dx * dx + (long)dz * dz;
		return distanceSquared <= (long)hunterEngageRadius * hunterEngageRadius;
	}

	public static bool IsHunterInEngageRange(
		StarMap world,
		string hunterId,
		string targetId,
		Func<string, Coord> committedPositionOf)
	{
		if (!world.FleetRegistry.TryGet(hunterId, out var hunter)
			|| !world.FleetRegistry.TryGet(targetId, out _))
			return false;

		return IsHunterInEngageRange(
			committedPositionOf(hunterId),
			committedPositionOf(targetId),
			hunter.State.EngageRadius);
	}
}
