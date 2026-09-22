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
			|| contract.Objective is not DeliveryObjective delivery)
			return;

		world.GetPointOfInterest(delivery.TurnInPoiId).OperatorTemporaryRoles.Grant(
			delivery.TurnInFacilityId,
			delivery.TurnInOperatorName,
			EFacilityOperatorRole.DeliveryTurnIn,
			state.ContractId);
	}

	public static void OnContractEnded(StarMap world, string contractId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not DeliveryObjective delivery)
			return;

		world.GetPointOfInterest(delivery.TurnInPoiId).OperatorTemporaryRoles.RevokeBySource(contractId);
	}
}
