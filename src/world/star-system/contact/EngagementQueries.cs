using GrimSpace.Math.Grid;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contact;

public readonly record struct PendingEngagement(
	string CounterpartyUnitId,
	EType CounterpartyType,
	EFaction CounterpartyFaction,
	string EncounterIntel,
	Contract? AssignedContract,
	bool FleeFailsDelivery);

public readonly record struct CommittedEngagement(
	string EngagementId,
	string InitiatorUnitId,
	IReadOnlyList<string> ParticipantUnitIds);

public static class EngagementQueries
{
	internal const int MaxContactCheckBackoffTicks = 64;
	internal const double PiratePursuitSpeedMultiplier = 1.5;

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

		var assignedContract = FindAssignedContract(world, counterpartyId);
		info = new PendingEngagement(
			counterpartyId,
			counterparty.State.Type,
			counterparty.State.Faction,
			EncounterIntelFormatter.FormatFleet(counterparty),
			assignedContract,
			IsAssignedDeliveryInterceptor(world, counterpartyId));
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

	private static Contract? FindAssignedContract(StarMap world, string unitId)
	{
		if (!world.FleetRegistry.TryGet(unitId, out var fleet))
			return null;

		var contractId = fleet.State.PursuitDirective?.ContractId
			?? fleet.State.SourceContractId;
		if (string.IsNullOrEmpty(contractId)
			|| !world.ContractRegistry.TryGet(contractId, out var contract)
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state.Status != EContractStatus.Active)
			return null;

		return contract;
	}

	private static bool IsAssignedDeliveryInterceptor(StarMap world, string unitId)
	{
		if (!world.FleetRegistry.TryGet(unitId, out var fleet)
			|| fleet.State.PursuitDirective is not { } directive
			|| !world.ContractRegistry.TryGetState(directive.ContractId, out var state)
			|| state.Status != EContractStatus.Active
			|| state is not DeliveryContractState delivery)
			return false;

		return delivery.Progress.InterceptionState == EDeliveryInterceptionState.Assigned
			&& string.Equals(
				delivery.Progress.InterceptorFleetId,
				unitId,
				StringComparison.Ordinal);
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

	internal static int ContactCheckDelay(
		Coord hunterPosition,
		Coord targetPosition,
		double hunterEngageRadius,
		double maxClosingSpeed)
	{
		var dx = hunterPosition.X - targetPosition.X;
		var dz = hunterPosition.Z - targetPosition.Z;
		var distance = System.Math.Sqrt(dx * dx + dz * dz);
		var gap = System.Math.Max(0, distance - hunterEngageRadius);
		return maxClosingSpeed <= 0
			? MaxContactCheckBackoffTicks
			: (int)System.Math.Clamp(
				System.Math.Floor(gap / maxClosingSpeed),
				1,
				MaxContactCheckBackoffTicks);
	}

	internal static double PursuitSpeedMultiplier(State state, EContactIntent intent) =>
		state.Type == EType.PirateFleet && intent == EContactIntent.Engagement
			? PiratePursuitSpeedMultiplier
			: 1.0;

	internal static double MaximumTravelSpeed(State state)
	{
		var pursuitMultiplier = state.TravelTarget.ContactIntent is { } intent
			? PursuitSpeedMultiplier(state, intent)
			: 1.0;
		return state.SpeedPerTick * PathfindingCell.RouteSpeedCeiling * pursuitMultiplier;
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
