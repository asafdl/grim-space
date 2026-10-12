using GrimSpace.Math;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.World.StarSystem.Contracts;

public static class LeadContractActivation
{
	public static bool TryBuildActiveStateOnAccept(
		StarMap world,
		Contract contract,
		string holderUnitId,
		int acceptedAtTick,
		out LeadContractState state)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentNullException.ThrowIfNull(contract);
		ArgumentException.ThrowIfNullOrEmpty(holderUnitId);
		ArgumentOutOfRangeException.ThrowIfNegative(acceptedAtTick);

		state = null!;
		if (contract.Category != EContractCategory.Lead
			|| contract.Objective is not BlackMarketSaleObjective sale)
			return false;

		var deadlineTick = acceptedAtTick + sale.DurationTicks;
		switch (sale.Destination)
		{
			case BlackMarketFacilityDestination facility:
				if (!TrySelectFacilityOperator(
						world,
						contract.Id,
						facility,
						out var operatorName))
					return false;

				state = new LeadContractState(
					contract.Id,
					EContractStatus.Active,
					acceptedAtTick,
					holderUnitId,
					operatorName,
					deadlineTick);
				return true;
			case BlackMarketSpaceMeetingDestination meeting:
				if (!ValidateSpaceMeeting(meeting))
					return false;

				state = new LeadContractState(
					contract.Id,
					EContractStatus.Active,
					acceptedAtTick,
					holderUnitId,
					DestinationOperatorName: null,
					deadlineTick);
				return true;
			default:
				return false;
		}
	}

	public static LeadContractState RequireActiveStateOnAccept(
		StarMap world,
		Contract contract,
		string holderUnitId,
		int acceptedAtTick)
	{
		if (!TryBuildActiveStateOnAccept(
				world,
				contract,
				holderUnitId,
				acceptedAtTick,
				out var state))
		{
			throw new InvalidOperationException(
				$"Lead contract '{contract.Id}' could not be activated on accept.");
		}

		return state;
	}

	public static LeadContractState BuildRejectedState(string contractId)
	{
		ArgumentException.ThrowIfNullOrEmpty(contractId);
		return new LeadContractState(
			contractId,
			EContractStatus.Rejected,
			AcceptedAtTick: null,
			HolderUnitId: null,
			DestinationOperatorName: null,
			DeadlineTick: 0);
	}

	private static bool ValidateSpaceMeeting(BlackMarketSpaceMeetingDestination meeting)
	{
		if (string.IsNullOrEmpty(meeting.MeetingId)
			|| string.IsNullOrEmpty(meeting.ContactName)
			|| meeting.SearchArea.SpawnPoints.Count == 0)
			return false;

		return true;
	}

	private static bool TrySelectFacilityOperator(
		StarMap world,
		string contractId,
		BlackMarketFacilityDestination destination,
		out string operatorName)
	{
		operatorName = "";
		if (!world.TryGetPointOfInterest(destination.PoiId, out var poi))
			return false;

		Facility facility;
		try
		{
			facility = poi.GetFacility(destination.FacilityId);
		}
		catch (InvalidOperationException)
		{
			return false;
		}

		var candidates = facility.Operators
			.Where(facilityOperator => facilityOperator.Role == EFacilityOperatorRole.Dialog)
			.Where(facilityOperator => IsOperatorAvailableForLeadSale(
				poi,
				facility.Id,
				facilityOperator.Name))
			.OrderBy(facilityOperator => facilityOperator.Name, StringComparer.Ordinal)
			.ToArray();
		if (candidates.Length == 0)
			return false;

		var random = new StableRandom(
			StableSeedMixer.From(world.Seed)
				.Add(contractId)
				.Add("lead-sale-operator")
				.Value);
		operatorName = candidates[(int)(random.NextDouble() * candidates.Length)].Name;
		return true;
	}

	private static bool IsOperatorAvailableForLeadSale(
		PointOfInterest poi,
		string facilityId,
		string operatorName) =>
		!poi.OperatorTemporaryRoles.HasAssignment(
			facilityId,
			operatorName,
			EFacilityOperatorRole.Merchant)
		&& !poi.OperatorTemporaryRoles.HasAssignment(
			facilityId,
			operatorName,
			EFacilityOperatorRole.StoryContact)
		&& !poi.OperatorTemporaryRoles.HasAssignment(
			facilityId,
			operatorName,
			EFacilityOperatorRole.DeliveryTurnIn);
}
