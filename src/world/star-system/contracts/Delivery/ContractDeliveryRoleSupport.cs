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
		RestoreInterceptorDirective(world, deliveryState);
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
		RemoveMeetingFleet(
			world,
			delivery,
			previousDelivery.Progress.CurrentLegIndex,
			previous.ContractId);
		if (current.Status == EContractStatus.Active && !currentDelivery.IsObjectiveMet())
		{
			GrantCurrentFacilityRole(world, delivery, currentDelivery);
			EnsureCurrentMeetingFleet(world, delivery, currentDelivery);
		}
	}

	public static void OnContractEnded(StarMap world, string contractId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not DeliveryContractState deliveryState)
			return;

		foreach (var poi in world.PointsOfInterest)
			poi.OperatorTemporaryRoles.RevokeBySource(contractId);

		for (var index = 0; index < delivery.Route.Legs.Count; index++)
			RemoveMeetingFleet(world, delivery, index, contractId);

		if (deliveryState.Progress.InterceptorFleetId is { } interceptorFleetId
			&& world.FleetRegistry.TryGet(interceptorFleetId, out var interceptor)
			&& interceptor.State.PursuitDirective?.ContractId == contractId)
			interceptor.State.PursuitDirective = null;

		if (deliveryState.HolderUnitId is { } holderUnitId
			&& world.FleetRegistry.TryGet(holderUnitId, out var holder)
			&& ContractFleetIds(delivery, deliveryState).Contains(
				holder.State.TravelTarget.TargetId,
				StringComparer.Ordinal))
			holder.State.TravelTarget = TravelTarget.None;
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
		int legIndex,
		string contractId)
	{
		if (delivery.Route.Legs[legIndex] is SpaceMeetingDeliveryLeg meeting
			&& world.FleetRegistry.TryGet(meeting.MeetingId, out var fleet)
			&& string.Equals(
				fleet.State.SourceContractId,
				contractId,
				StringComparison.Ordinal))
			world.FleetRegistry.Remove(meeting.MeetingId);
	}

	private static void RestoreInterceptorDirective(
		StarMap world,
		DeliveryContractState state)
	{
		if (state.Progress.InterceptionState != EDeliveryInterceptionState.Assigned
			|| state.Progress.InterceptorFleetId is not { } interceptorFleetId
			|| state.HolderUnitId is not { } holderUnitId
			|| !world.FleetRegistry.TryGet(interceptorFleetId, out var interceptor)
			|| interceptor.State.PursuitDirective is not null)
			return;

		interceptor.State.PursuitDirective =
			new FleetPursuitDirective(state.ContractId, holderUnitId);
	}

	private static IEnumerable<string> ContractFleetIds(
		DeliveryObjective delivery,
		DeliveryContractState state)
	{
		if (state.Progress.InterceptorFleetId is { } interceptorFleetId)
			yield return interceptorFleetId;

		foreach (var meeting in delivery.Route.Legs.OfType<SpaceMeetingDeliveryLeg>())
			yield return meeting.MeetingId;
	}
}
