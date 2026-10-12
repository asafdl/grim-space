using GrimSpace.Core.Engine;
using GrimSpace.Core.Actions;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class AcceptLeadContractActionTests(StarMapFixture maps)
{
	[Fact]
	public void Accept_FacilityLead_ActivatesWithDeadlineAndOperator()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-accept-facility", spaceMeetingChance: 0);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		var destination = (BlackMarketFacilityDestination)sale.Destination;
		var acceptedAtTick = engine.World.Timeline.Clock.Current;

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		Assert.True(engine.World.ContractRegistry.TryGetState(contract.Id, out var state));
		var lead = Assert.IsType<LeadContractState>(state);
		Assert.Equal(EContractStatus.Active, lead.Status);
		Assert.Equal(unitId, lead.HolderUnitId);
		Assert.Equal(acceptedAtTick + sale.DurationTicks, lead.DeadlineTick);
		Assert.False(string.IsNullOrEmpty(lead.DestinationOperatorName));
		var facility = engine.World.GetPointOfInterest(destination.PoiId)
			.GetFacility(destination.FacilityId);
		Assert.Contains(
			facility.Operators,
			facilityOperator =>
				facilityOperator.Role == EFacilityOperatorRole.Dialog
				&& facilityOperator.Name == lead.DestinationOperatorName);
	}

	[Fact]
	public void Accept_SpaceLead_ActivatesWithNullOperator()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-accept-space", spaceMeetingChance: 1);

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		var lead = GetLeadState(engine.World, contract.Id);
		Assert.Null(lead.DestinationOperatorName);
		Assert.IsType<BlackMarketSpaceMeetingDestination>(
			((BlackMarketSaleObjective)contract.Objective).Destination);
	}

	[Fact]
	public void Accept_FacilityLead_SelectsDeterministicOperator()
	{
		var first = CreatePendingLead("lead-deterministic-operator", spaceMeetingChance: 0);
		var second = CreatePendingLead("lead-deterministic-operator", spaceMeetingChance: 0);

		first.Engine.Commit(ContractActionTestContext.Accept(first.Engine.World, first.UnitId, first.Contract.Id));
		second.Engine.Commit(ContractActionTestContext.Accept(second.Engine.World, second.UnitId, second.Contract.Id));

		var firstLead = GetLeadState(first.Engine.World, first.Contract.Id);
		var secondLead = GetLeadState(second.Engine.World, second.Contract.Id);
		Assert.Equal(firstLead.DestinationOperatorName, secondLead.DestinationOperatorName);
	}

	[Fact]
	public void Accept_FacilityLead_ReclaimsContractOperatorForMerchant()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-occupied", spaceMeetingChance: 0);
		var destination = (BlackMarketFacilityDestination)
			((BlackMarketSaleObjective)contract.Objective).Destination;
		var poi = engine.World.GetPointOfInterest(destination.PoiId);
		var facility = poi.GetFacility(destination.FacilityId);
		foreach (var facilityOperator in facility.Operators
			.Where(candidate => candidate.Role == EFacilityOperatorRole.Dialog))
		{
			poi.OperatorTemporaryRoles.Grant(
				facility.Id,
				facilityOperator.Name,
			EFacilityOperatorRole.Contracts,
			$"pending-{facilityOperator.Name}");
		}

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		var lead = GetLeadState(engine.World, contract.Id);
		Assert.True(poi.OperatorTemporaryRoles.TryGetAssignment(
			facility.Id,
			lead.DestinationOperatorName!,
			out var assignment));
		Assert.Equal(EFacilityOperatorRole.Merchant, assignment.Role);
		Assert.DoesNotContain(
			poi.OperatorTemporaryRoles.AssignmentsIncludingSuspended(EFacilityOperatorRole.Contracts),
			candidate => candidate.OperatorName == lead.DestinationOperatorName);
	}

	[Fact]
	public void Accept_FacilityLead_GrantsTemporaryMerchant()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-merchant-grant", spaceMeetingChance: 0);
		var sale = (BlackMarketSaleObjective)contract.Objective;
		var destination = (BlackMarketFacilityDestination)sale.Destination;

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		var lead = GetLeadState(engine.World, contract.Id);

		var poi = engine.World.GetPointOfInterest(destination.PoiId);
		Assert.True(poi.OperatorTemporaryRoles.TryGetAssignment(
			destination.FacilityId,
			lead.DestinationOperatorName!,
			out var assignment));
		Assert.Equal(EFacilityOperatorRole.Merchant, assignment.Role);
		Assert.Equal(sale.MerchantSubtype, assignment.MerchantSubtype);
		Assert.Contains(contract.Id, assignment.SourceIds);
		Assert.Equal(lead.DeadlineTick, assignment.AcceptsSourcesUntilTick);
	}

	[Fact]
	public void Accept_SpaceLead_SpawnsMeetingFleet()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-space-fleet", spaceMeetingChance: 1);
		var meeting = (BlackMarketSpaceMeetingDestination)
			((BlackMarketSaleObjective)contract.Objective).Destination;

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		Assert.True(engine.World.FleetRegistry.TryGet(meeting.MeetingId, out var fleet));
		Assert.Equal(contract.Id, fleet.State.SourceContractId);
	}

	[Fact]
	public void Decline_PersistsRejectedLeadState()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-decline", spaceMeetingChance: 0);

		engine.Commit(ContractActionTestContext.Decline(engine.World, unitId, contract.Id));

		Assert.True(engine.World.ContractRegistry.IsRejected(contract.Id));
		var lead = GetLeadState(engine.World, contract.Id);
		Assert.Equal(EContractStatus.Rejected, lead.Status);
		Assert.Null(lead.HolderUnitId);
		Assert.Null(lead.DestinationOperatorName);
		Assert.Equal(0, lead.DeadlineTick);
	}

	private LeadContractState GetLeadState(StarMap map, string contractId)
	{
		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		return Assert.IsType<LeadContractState>(state);
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, Contract Contract)
		CreatePendingLead(string contractId, double spaceMeetingChance)
	{
		var map = maps.Fresh(42);
		var contract = ContractFactory.Build(
			map,
			contractId,
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				spaceMeetingChance));
		Assert.True(map.ContractRegistry.TryAdd(contract));
		var unitId = map.FleetRegistry.All.First().State.Id;
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(unitId);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, contract);
	}
}
