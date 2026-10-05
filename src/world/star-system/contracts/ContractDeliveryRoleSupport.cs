using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;

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
		if (current.Status == EContractStatus.Active && !currentDelivery.IsObjectiveMet())
			GrantCurrentFacilityRole(world, delivery, currentDelivery);
	}

	public static void OnContractEnded(StarMap world, string contractId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery)
			return;

		foreach (var poi in world.PointsOfInterest)
			poi.OperatorTemporaryRoles.RevokeBySource(contractId);
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
}
