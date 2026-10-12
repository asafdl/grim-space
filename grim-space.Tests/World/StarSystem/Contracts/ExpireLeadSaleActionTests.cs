using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ExpireLeadSaleActionTests(StarMapFixture maps)
{
	[Fact]
	public void Accept_SchedulesExpireLeadSaleAtDeadlinePlusOne()
	{
		var (engine, unitId, contract) = CreatePendingLead("lead-expire-schedule", spaceMeetingChance: 0);

		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));

		var lead = GetLeadState(engine.World, contract.Id);
		var scheduledTick = engine.World.Timeline.ToSnapshot().Pending
			.First(pair => pair.Value.Any(action =>
				action is ExpireLeadSaleAction expire
				&& expire.ContractId == contract.Id
				&& expire.ActorId == StarSystemActorIds.Contracts))
			.Key;
		Assert.Equal(lead.DeadlineTick + 1, scheduledTick);
	}

	[Fact]
	public void Expire_ActiveLead_CompletesAndRevokesFacilityMerchant()
	{
		var (engine, unitId, contract) = CreateAcceptedFacilityLead("lead-expire-complete");
		var lead = GetLeadState(engine.World, contract.Id);
		var destination = GetFacilityDestination(contract);

		AdvancePastDeadline(engine, lead.DeadlineTick);
		engine.Commit(Expire(contract.Id));

		lead = GetLeadState(engine.World, contract.Id);
		Assert.Equal(EContractStatus.Completed, lead.Status);
		Assert.False(HasFacilityMerchantGrant(engine.World, contract.Id, lead, destination));
	}

	[Fact]
	public void Expire_SecondCommitAfterDeadline_IsNotLegal()
	{
		var (engine, unitId, contract) = CreateAcceptedFacilityLead("lead-expire-idempotent");
		var lead = GetLeadState(engine.World, contract.Id);
		AdvancePastDeadline(engine, lead.DeadlineTick);

		engine.Commit(Expire(contract.Id));
		Assert.Equal(EContractStatus.Completed, GetLeadState(engine.World, contract.Id).Status);

		Assert.False(ExpireLeadSaleDef.Instance.IsLegal(
			Expire(contract.Id),
			engine.World,
			engine.ActorRuntimes.For(StarSystemActorIds.Contracts)));
	}

	[Fact]
	public void Dismiss_ActiveFacilityLead_RevokesMerchantAndFailsContract()
	{
		var (engine, unitId, contract) = CreateAcceptedFacilityLead("lead-dismiss-facility");
		var lead = GetLeadState(engine.World, contract.Id);
		var destination = GetFacilityDestination(contract);
		Assert.True(HasFacilityMerchantGrant(engine.World, contract.Id, lead, destination));

		engine.Commit(ContractActionTestContext.Dismiss(unitId, contract.Id));

		lead = GetLeadState(engine.World, contract.Id);
		Assert.Equal(EContractStatus.Failed, lead.Status);
		Assert.False(HasFacilityMerchantGrant(engine.World, contract.Id, lead, destination));
	}

	[Fact]
	public void Dismiss_ActiveSpaceLead_RemovesMeetingFleet()
	{
		var (engine, unitId, contract) = CreateAcceptedSpaceLead("lead-dismiss-space");
		var meeting = (BlackMarketSpaceMeetingDestination)
			((BlackMarketSaleObjective)contract.Objective).Destination;
		Assert.True(engine.World.FleetRegistry.Contains(meeting.MeetingId));

		engine.Commit(ContractActionTestContext.Dismiss(unitId, contract.Id));

		Assert.Equal(EContractStatus.Failed, GetLeadState(engine.World, contract.Id).Status);
		Assert.False(engine.World.FleetRegistry.Contains(meeting.MeetingId));
	}

	[Fact]
	public void Expire_ActiveSpaceLead_RemovesMeetingFleet()
	{
		var (engine, _, contract) = CreateAcceptedSpaceLead("lead-expire-space");
		var lead = GetLeadState(engine.World, contract.Id);
		var meeting = (BlackMarketSpaceMeetingDestination)
			((BlackMarketSaleObjective)contract.Objective).Destination;
		Assert.True(engine.World.FleetRegistry.Contains(meeting.MeetingId));

		AdvancePastDeadline(engine, lead.DeadlineTick);
		engine.Commit(Expire(contract.Id));

		Assert.Equal(EContractStatus.Completed, GetLeadState(engine.World, contract.Id).Status);
		Assert.False(engine.World.FleetRegistry.Contains(meeting.MeetingId));
	}

	private static ExpireLeadSaleAction Expire(string contractId) =>
		new(StarSystemActorIds.Contracts, contractId);

	private static void AdvancePastDeadline(Engine<StarMap, ActorRuntime> engine, int deadlineTick) =>
		engine.World.Timeline.Clock.Set(deadlineTick + 1);

	private static BlackMarketFacilityDestination GetFacilityDestination(Contract contract) =>
		(BlackMarketFacilityDestination)((BlackMarketSaleObjective)contract.Objective).Destination;

	private static bool HasFacilityMerchantGrant(
		StarMap map,
		string contractId,
		LeadContractState lead,
		BlackMarketFacilityDestination destination)
	{
		if (lead.DestinationOperatorName is not { } operatorName)
			return false;

		var poi = map.GetPointOfInterest(destination.PoiId);
		return poi.OperatorTemporaryRoles.TryGetAssignment(
			destination.FacilityId,
			operatorName,
			out var assignment)
			&& assignment.Role == EFacilityOperatorRole.Merchant
			&& assignment.SourceIds.Contains(contractId);
	}

	private static LeadContractState GetLeadState(StarMap map, string contractId)
	{
		Assert.True(map.ContractRegistry.TryGetState(contractId, out var state));
		return Assert.IsType<LeadContractState>(state);
	}

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, Contract Contract)
		CreateAcceptedFacilityLead(string contractId) =>
		CreateAcceptedLead(contractId, spaceMeetingChance: 0);

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, Contract Contract)
		CreateAcceptedSpaceLead(string contractId) =>
		CreateAcceptedLead(contractId, spaceMeetingChance: 1);

	private (Engine<StarMap, ActorRuntime> Engine, string UnitId, Contract Contract)
		CreateAcceptedLead(string contractId, double spaceMeetingChance)
	{
		var (engine, unitId, contract) = CreatePendingLead(contractId, spaceMeetingChance);
		engine.Commit(ContractActionTestContext.Accept(engine.World, unitId, contract.Id));
		return (engine, unitId, contract);
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
		runtimes.For(StarSystemActorIds.Contracts);
		var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		return (engine, unitId, contract);
	}
}
