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
		if (!world.UnitRegistry.TryGet(playerId, out var player))
			return false;

		if (player.State.EngagementPhase != EEngagementPhase.AwaitingDecision)
			return false;

		var counterpartyId = ResolveCounterpartyId(player.State);
		if (counterpartyId is null
			|| !world.UnitRegistry.TryGet(counterpartyId, out var counterparty))
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
		if (!world.UnitRegistry.TryGet(playerId, out var player))
			return false;

		if (player.State.EngagementPhase != EEngagementPhase.Engaged
			|| player.State.EngagedWithUnitIds.Count == 0)
			return false;

		var counterpartyId = player.State.EngagedWithUnitIds.First();
		if (!world.UnitRegistry.TryGet(counterpartyId, out var counterparty))
			return false;

		var initiatorId = player.State.EngagementInitiatorUnitId
			?? counterparty.State.EngagementInitiatorUnitId
			?? playerId;
		var participants = new List<string> { initiatorId };
		participants.AddRange(
			new[] { playerId, counterpartyId }
				.Where(id => id != initiatorId)
				.OrderBy(id => id, StringComparer.Ordinal));

		info = new CommittedEngagement(initiatorId, participants);
		return true;
	}

	internal static string? ResolveCounterpartyId(State state) =>
		state.EngagementTargetUnitId ?? state.HuntedByUnitId;

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
		if (!world.UnitRegistry.TryGet(hunterId, out var hunter)
			|| !world.UnitRegistry.TryGet(targetId, out _))
			return false;

		return IsHunterInEngageRange(
			committedPositionOf(hunterId),
			committedPositionOf(targetId),
			hunter.State.EngageRadius);
	}
}
