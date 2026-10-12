using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Contracts;

internal static class ContractLeadRoleSupport
{
	public static void OnContractActivated(StarMap world, ContractState state)
	{
		if (state.Status != EContractStatus.Active)
			return;

		if (state is not LeadContractState lead)
			return;

		if (!world.ContractRegistry.TryGet(lead.ContractId, out var contract)
			|| contract.Objective is not BlackMarketSaleObjective sale)
		{
			throw new InvalidOperationException(
				$"Lead contract '{lead.ContractId}' is missing a black-market sale objective.");
		}

		RevokeLeadPresentation(world, lead.ContractId);
		switch (sale.Destination)
		{
			case BlackMarketFacilityDestination facility:
				MaterializeFacilitySale(world, sale, lead, facility);
				break;
			case BlackMarketSpaceMeetingDestination meeting:
				EnsureMeetingFleet(world, lead, meeting);
				break;
			default:
				throw new InvalidOperationException(
					$"Lead contract '{lead.ContractId}' has unsupported destination " +
					$"'{sale.Destination.GetType().Name}'.");
		}
	}

	public static void OnContractEnded(StarMap world, string contractId)
	{
		if (!world.ContractRegistry.TryGet(contractId, out var contract)
			|| contract.Objective is not BlackMarketSaleObjective sale
			|| !world.ContractRegistry.TryGetState(contractId, out var state)
			|| state is not LeadContractState lead)
			return;

		foreach (var poi in world.PointsOfInterest)
			poi.OperatorTemporaryRoles.RevokeBySource(contractId);

		if (sale.Destination is BlackMarketSpaceMeetingDestination meeting)
			RemoveMeetingFleet(world, meeting, contractId);

		if (lead.HolderUnitId is { } holderUnitId
			&& world.FleetRegistry.TryGet(holderUnitId, out var holder)
			&& sale.Destination is BlackMarketSpaceMeetingDestination spaceMeeting
			&& holder.State.TravelTarget.TargetId == spaceMeeting.MeetingId)
			holder.State.TravelTarget = TravelTarget.None;
	}

	private static void RevokeLeadPresentation(StarMap world, string contractId)
	{
		foreach (var poi in world.PointsOfInterest)
			poi.OperatorTemporaryRoles.RevokeBySource(contractId);
	}

	private static void MaterializeFacilitySale(
		StarMap world,
		BlackMarketSaleObjective sale,
		LeadContractState lead,
		BlackMarketFacilityDestination destination)
	{
		if (lead.DestinationOperatorName is not { } operatorName)
		{
			throw new InvalidOperationException(
				$"Lead contract '{lead.ContractId}' is missing a facility sale operator.");
		}

		var poi = world.GetPointOfInterest(destination.PoiId);
		var roles = poi.OperatorTemporaryRoles;
		foreach (var role in new[]
		{
			EFacilityOperatorRole.Contracts,
			EFacilityOperatorRole.LeadContact,
		})
		{
			foreach (var sourceId in roles.AssignmentsIncludingSuspended(role)
				.Where(assignment =>
					assignment.FacilityId == destination.FacilityId
					&& assignment.OperatorName == operatorName)
				.SelectMany(assignment => assignment.SourceIds)
				.ToArray())
			{
				roles.RevokeSource(destination.FacilityId, operatorName, sourceId);
			}
		}

		roles.Grant(
			destination.FacilityId,
			operatorName,
			new OperatorInteraction(EFacilityOperatorRole.Merchant, sale.MerchantSubtype),
			EOperatorAssignmentPriority.Merchant,
			lead.ContractId,
			lead.DeadlineTick);
	}

	private static void EnsureMeetingFleet(
		StarMap world,
		LeadContractState lead,
		BlackMarketSpaceMeetingDestination meeting)
	{
		if (world.FleetRegistry.Contains(meeting.MeetingId))
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
			SourceContractId: lead.ContractId);
		world.FleetRegistry.Add(new Fleet(State.FromSpawn(spawn)));
	}

	private static void RemoveMeetingFleet(
		StarMap world,
		BlackMarketSpaceMeetingDestination meeting,
		string contractId)
	{
		if (!world.FleetRegistry.TryGet(meeting.MeetingId, out var fleet)
			|| !string.Equals(fleet.State.SourceContractId, contractId, StringComparison.Ordinal))
			return;

		world.FleetRegistry.Remove(meeting.MeetingId);
	}
}
