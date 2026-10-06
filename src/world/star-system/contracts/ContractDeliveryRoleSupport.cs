using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contracts;

internal static class ContractDeliveryRoleSupport
{
	public static void OnContractActivated(StarMap world, ContractState state)
	{
		if (state.Status != EContractStatus.Active)
			return;

		if (!world.ContractRegistry.TryGet(state.ContractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery
			|| state is not DeliveryContractState deliveryState)
			return;

		GrantCurrentFacilityRole(world, delivery, deliveryState);
		EnsureCurrentMeetingFleet(world, delivery, deliveryState);
	}

	public static void OnDeliveryLegAdvanced(
		StarMap world,
		ContractState previous,
		ContractState current)
	{
		if (!world.ContractRegistry.TryGet(previous.ContractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery
			|| previous is not DeliveryContractState previousDelivery
			|| current is not DeliveryContractState currentDelivery)
			return;

		if (delivery.Route.Legs[previousDelivery.Progress.CurrentLegIndex] is FacilityDeliveryLeg previousFacility)
			world.GetPointOfInterest(previousFacility.PoiId)
				.OperatorTemporaryRoles.RevokeBySource(previous.ContractId);
		RemoveMeetingFleet(world, delivery, previousDelivery.Progress.CurrentLegIndex);
		if (current.Status == EContractStatus.Active && !currentDelivery.IsObjectiveMet())
		{
			GrantCurrentFacilityRole(world, delivery, currentDelivery);
			EnsureCurrentMeetingFleet(world, delivery, currentDelivery);
		}
	}

	public static void OnContractEnded(StarMap world, string contractId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery)
			return;

		foreach (var poi in world.PointsOfInterest)
			poi.OperatorTemporaryRoles.RevokeBySource(contractId);

		foreach (var fleet in world.FleetRegistry.All
			         .Where(fleet => fleet.State.SourceContractId == contractId)
			         .ToArray())
			world.FleetRegistry.Remove(fleet.State.Id);
	}

	private static void GrantCurrentFacilityRole(
		StarMap world,
		DeliveryObjective delivery,
		DeliveryContractState state)
	{
		if (delivery.Route.Legs[state.Progress.CurrentLegIndex] is not FacilityDeliveryLeg facility)
			return;

		world.GetPointOfInterest(facility.PoiId).OperatorTemporaryRoles.Grant(
			facility.FacilityId,
			facility.OperatorName,
			EFacilityOperatorRole.DeliveryTurnIn,
			state.ContractId);
	}

	private static void EnsureCurrentMeetingFleet(
		StarMap world,
		DeliveryObjective delivery,
		DeliveryContractState state)
	{
		if (delivery.Route.Legs[state.Progress.CurrentLegIndex]
			is not SpaceMeetingDeliveryLeg meeting
			|| world.FleetRegistry.Contains(meeting.MeetingId))
			return;

		var spawn = new Spawn(
			meeting.MeetingId,
			EType.ServiceVessel,
			"",
			meeting.Position,
			UnitDefaults.SpeedPerTick(EType.ServiceVessel),
			UnitDefaults.EngageRadius(EType.ServiceVessel),
			UnitDefaults.VisionRadius(EType.ServiceVessel),
			[],
			world.ControllingFaction,
			SourceContractId: state.ContractId);
		world.FleetRegistry.Add(new Fleet(State.FromSpawn(spawn)));
	}

	private static void RemoveMeetingFleet(
		StarMap world,
		DeliveryObjective delivery,
		int legIndex)
	{
		if (delivery.Route.Legs[legIndex] is SpaceMeetingDeliveryLeg meeting
			&& world.FleetRegistry.TryGet(meeting.MeetingId, out var fleet)
			&& fleet.State.SourceContractId is not null)
			world.FleetRegistry.Remove(meeting.MeetingId);
	}
}
