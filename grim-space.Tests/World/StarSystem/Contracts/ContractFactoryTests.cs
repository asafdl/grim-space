using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersPendingContract()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsPending(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.False(contract.IsStoryObjective);
		Assert.Equal(
			ContractRewardCalculator.Roll(map.Seed, contract.Id, EContractKind.Hunt, args.Danger),
			contract.Terms);
	}

	[Fact]
	public void Build_Hunt_DoesNotRegister()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);
		const string contractId = "contract-scheduler-test";

		var contract = ContractFactory.Build(map, contractId, EContractKind.Hunt, args);

		Assert.Equal(contractId, contract.Id);
		Assert.False(map.ContractRegistry.Contains(contractId));
	}

	[Fact]
	public void Build_Hunt_SameContractId_IsDeterministic()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);
		const string contractId = "contract-deterministic";

		var first = (HuntObjective)ContractFactory.Build(map, contractId, EContractKind.Hunt, args).Objective;
		var second = (HuntObjective)ContractFactory.Build(map, contractId, EContractKind.Hunt, args).Objective;

		Assert.Equal(ContractFactory.SpawnGroupIdFor(contractId), first.SpawnGroups[0].GroupId);
		Assert.Equal(first.SpawnGroups[0].GroupId, second.SpawnGroups[0].GroupId);
		Assert.Equal(first.SpawnGroups[0].Spawn.Seed, second.SpawnGroups[0].Spawn.Seed);
	}

	[Fact]
	public void Create_Hunt_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);
		var (_, _, _, operatorName) = TutorialBeatContracts.BeatBDropoff(map);
		var deliveryArgs = new DeliveryCreateArgs(
			map.Blueprint.SupplyPlan.StoragePoiId,
			EDangerLevel.VeryLow,
			ContractNarrative.ForDelivery("x", "y", "z"),
			DropoffOperatorName: operatorName);

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Hunt, deliveryArgs));
	}

	[Fact]
	public void Build_BlackMarketLead_SelectsDeterministicEligibleDestination()
	{
		var map = maps.Fresh(42);
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative("Quiet Opportunity", "A seller is available."),
			300,
			SpaceMeetingChance: 0);

		var first = ContractFactory.Build(map, "lead-deterministic", EContractKind.Lead, args);
		var second = ContractFactory.Build(map, "lead-deterministic", EContractKind.Lead, args);

		Assert.Equal(EContractCategory.Lead, first.Category);
		Assert.True(first.Terms.Payment.IsEmpty);
		var objective = Assert.IsType<BlackMarketSaleObjective>(first.Objective);
		var repeated = Assert.IsType<BlackMarketSaleObjective>(second.Objective);
		Assert.Equal(objective, repeated);
		Assert.True(Enum.IsDefined(objective.MerchantSubtype));
		var destination = Assert.IsType<BlackMarketFacilityDestination>(objective.Destination);
		var facility = map.GetPointOfInterest(destination.PoiId)
			.GetFacility(destination.FacilityId);
		Assert.Contains(
			facility.Operators,
			facilityOperator => facilityOperator.Role == EFacilityOperatorRole.Dialog);
		Assert.Equal(EMerchantSubtype.ShipSupport, objective.MerchantSubtype);
	}

	[Fact]
	public void Build_BlackMarketLead_AtT0_RejectsExplicitMerchantWithoutStock()
	{
		var map = maps.Fresh(42);
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative("Quiet Opportunity", "A seller is available."),
			300,
			SpaceMeetingChance: 0,
			MerchantSubtype: EMerchantSubtype.Ships);

		Assert.False(ContractFactory.TryBuildLead(
			map,
			"lead-no-ships-at-t0",
			args,
			out _));
	}

	[Fact]
	public void Build_BlackMarketLead_RejectsDestinationWithoutDialogOperator()
	{
		var map = maps.Fresh(42);
		var poi = map.PointsOfInterest.First();
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative("Quiet Opportunity", "A seller is available."),
			300,
			SpaceMeetingChance: 0,
			Destination: new BlackMarketFacilityDestination(
				poi.Id,
				"missing-facility"),
			MerchantSubtype: EMerchantSubtype.Weapons);

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Build(map, "lead-invalid-destination", EContractKind.Lead, args));
	}

	[Fact]
	public void Build_BlackMarketLead_SpaceMeetingIsDeterministic()
	{
		var map = maps.Fresh(42);
		var args = new LeadCreateArgs(
			ELeadKind.BlackMarketSale,
			new ContractNarrative("Quiet Opportunity", "A seller is available."),
			300,
			SpaceMeetingChance: 1);

		var first = ContractFactory.Build(map, "lead-space", EContractKind.Lead, args);
		var second = ContractFactory.Build(map, "lead-space", EContractKind.Lead, args);

		var firstMeeting = Assert.IsType<BlackMarketSpaceMeetingDestination>(
			Assert.IsType<BlackMarketSaleObjective>(first.Objective).Destination);
		var secondMeeting = Assert.IsType<BlackMarketSpaceMeetingDestination>(
			Assert.IsType<BlackMarketSaleObjective>(second.Objective).Destination);
		Assert.Equal("lead-space.meeting", firstMeeting.MeetingId);
		Assert.Equal(firstMeeting.ContactName, secondMeeting.ContactName);
		Assert.Equal(firstMeeting.SearchArea.Intel, secondMeeting.SearchArea.Intel);
		Assert.Equal(firstMeeting.SearchArea.SpawnPoints, secondMeeting.SearchArea.SpawnPoints);
		Assert.False(map.FleetRegistry.Contains(firstMeeting.MeetingId));
	}

	private static HuntCreateArgs CreateGeneratedHuntArgs(StarMap map)
	{
		return new HuntCreateArgs(
			new AreaPickerArgs(MapLandmarkQueries.AllIds(map), DeterministicPickMix: 1),
			EDangerLevel.VeryLow,
			ContractNarrative.ForHunt("Generated Hunt"));
	}
}
