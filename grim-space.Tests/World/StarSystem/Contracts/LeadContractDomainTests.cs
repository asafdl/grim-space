using GrimSpace.Run.Persistence;
using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class LeadContractDomainTests
{
	[Fact]
	public void BlackMarketSaleObjective_ExposesLeadConfiguration()
	{
		ILeadObjective objective = new BlackMarketSaleObjective(
			new BlackMarketFacilityDestination("poi-trade", "market"),
			EMerchantSubtype.Weapons,
			300);

		Assert.Equal(ELeadKind.BlackMarketSale, objective.LeadKind);
		Assert.Equal(300, objective.DurationTicks);

		var sale = Assert.IsType<BlackMarketSaleObjective>(objective);
		var destination = Assert.IsType<BlackMarketFacilityDestination>(sale.Destination);
		Assert.Equal("poi-trade", destination.PoiId);
		Assert.Equal("market", destination.FacilityId);
		Assert.Equal(EMerchantSubtype.Weapons, sale.MerchantSubtype);
	}

	[Fact]
	public void BlackMarketSaleObjective_RejectsNonPositiveDuration()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() =>
			new BlackMarketSaleObjective(
				new BlackMarketFacilityDestination("poi-trade", "market"),
				EMerchantSubtype.Weapons,
				0));
	}

	[Fact]
	public void LeadContractState_DeadlineTracksSaleWindow()
	{
		var state = new LeadContractState(
			"lead-1",
			EContractStatus.Active,
			AcceptedAtTick: 20,
			"player",
			"Quartermaster",
			DeadlineTick: 320);

		Assert.False(state.IsObjectiveMet());
		Assert.Equal(320, ((IContractDeadlineState)state).DeadlineTick);
	}

	[Fact]
	public void Persistence_RoundTripsPendingLead()
	{
		var contract = CreateLead();
		var registry = PersistenceRegistry.CreateDefault();

		var dto = SaveDtoMapper.CaptureContract((contract, null, 120), registry);
		var restored = SaveDtoMapper.RestoreContract(dto, registry);

		Assert.Equal(EContractCategory.Lead, restored.Contract.Category);
		Assert.Equal(120, restored.ExpiresAtTick);
		Assert.Null(restored.State);
		var objective = Assert.IsType<BlackMarketSaleObjective>(restored.Contract.Objective);
		var destination = Assert.IsType<BlackMarketFacilityDestination>(objective.Destination);
		Assert.Equal("poi-trade", destination.PoiId);
		Assert.Equal("market", destination.FacilityId);
		Assert.Equal(EMerchantSubtype.Weapons, objective.MerchantSubtype);
		Assert.Equal(300, objective.DurationTicks);
	}

	[Fact]
	public void Persistence_RoundTripsSpaceMeetingDestination()
	{
		var map = new StarMapFixture().Fresh(42);
		var contract = ContractFactory.Build(
			map,
			"lead-space-persistence",
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative("Quiet Opportunity", "A seller is available."),
				300,
				SpaceMeetingChance: 1));
		var registry = PersistenceRegistry.CreateDefault();

		var dto = SaveDtoMapper.CaptureContract((contract, null, 120), registry);
		var restored = SaveDtoMapper.RestoreContract(dto, registry);

		var original = Assert.IsType<BlackMarketSpaceMeetingDestination>(
			Assert.IsType<BlackMarketSaleObjective>(contract.Objective).Destination);
		var destination = Assert.IsType<BlackMarketSpaceMeetingDestination>(
			Assert.IsType<BlackMarketSaleObjective>(restored.Contract.Objective).Destination);
		Assert.Equal(original.MeetingId, destination.MeetingId);
		Assert.Equal(original.ContactName, destination.ContactName);
		Assert.Equal(original.SearchArea.Intel, destination.SearchArea.Intel);
		Assert.Equal(original.SearchArea.SpawnPoints, destination.SearchArea.SpawnPoints);
	}

	[Theory]
	[InlineData(EContractStatus.Active)]
	[InlineData(EContractStatus.Completed)]
	public void Persistence_RoundTripsLeadRuntimeState(EContractStatus status)
	{
		var contract = CreateLead();
		var state = new LeadContractState(
			contract.Id,
			status,
			AcceptedAtTick: 20,
			"player",
			"Quartermaster",
			DeadlineTick: 320);
		var registry = PersistenceRegistry.CreateDefault();

		var dto = SaveDtoMapper.CaptureContract((contract, state, null), registry);
		var restored = SaveDtoMapper.RestoreContract(dto, registry);

		var leadState = Assert.IsType<LeadContractState>(restored.State);
		Assert.Equal(status, leadState.Status);
		Assert.Equal(20, leadState.AcceptedAtTick);
		Assert.Equal("player", leadState.HolderUnitId);
		Assert.Equal("Quartermaster", leadState.DestinationOperatorName);
		Assert.Equal(320, leadState.DeadlineTick);
	}

	[Fact]
	public void Persistence_RoundTripsPurchasedPilotHireSlots()
	{
		var contract = CreateLead();
		var state = new LeadContractState(
			contract.Id,
			EContractStatus.Active,
			AcceptedAtTick: 20,
			"player",
			"Quartermaster",
			DeadlineTick: 320)
		{
			PurchasedPilotHireSlots = [0, 2],
		};
		var registry = PersistenceRegistry.CreateDefault();

		var dto = SaveDtoMapper.CaptureContract((contract, state, null), registry);
		var restored = SaveDtoMapper.RestoreContract(dto, registry);

		var leadState = Assert.IsType<LeadContractState>(restored.State);
		Assert.Equal([0, 2], leadState.PurchasedPilotHireSlots);
	}

	[Fact]
	public void MarkPilotHireSlotPurchased_TracksSlotsWithoutDuplicates()
	{
		var state = new LeadContractState(
			"lead-1",
			EContractStatus.Active,
			AcceptedAtTick: 1,
			"player",
			"Quartermaster",
			DeadlineTick: 100);

		var once = state.MarkPilotHireSlotPurchased(1);
		var twice = once.MarkPilotHireSlotPurchased(1);

		Assert.Equal([1], once.PurchasedPilotHireSlots);
		Assert.Same(once, twice);
	}

	[Fact]
	public void Fork_LeadStateMutatesIndependently()
	{
		var map = new StarMapFixture().FreshWithBeatAHunt(42);
		var contract = CreateLead();
		var state = new LeadContractState(
			contract.Id,
			EContractStatus.Active,
			AcceptedAtTick: 20,
			map.FleetRegistry.Ids.First(),
			"Quartermaster",
			DeadlineTick: 320);
		Assert.True(map.ContractRegistry.TryAdd(contract));
		Assert.True(map.ContractRegistry.Activate(state));

		var fork = map.Fork();
		fork.ContractRegistry.Complete(contract.Id);

		Assert.True(map.ContractRegistry.TryGetState(contract.Id, out var originalState));
		Assert.Equal(EContractStatus.Active, originalState.Status);
		Assert.True(fork.ContractRegistry.TryGetState(contract.Id, out var forkState));
		Assert.Equal(EContractStatus.Completed, forkState.Status);
		Assert.IsType<LeadContractState>(forkState);
	}

	private static Contract CreateLead() =>
		new(
			"lead-1",
			new BlackMarketSaleObjective(
				new BlackMarketFacilityDestination("poi-trade", "market"),
				EMerchantSubtype.Weapons,
				300),
			EDangerLevel.VeryLow,
			EFaction.TheOptimality,
			new ContractTerms(ResourceBundle.Empty),
			new ContractNarrative("Quiet opportunity", "A discreet seller is available."),
			Category: EContractCategory.Lead);
}
