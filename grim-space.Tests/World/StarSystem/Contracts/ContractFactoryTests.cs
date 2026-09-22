using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersOfferedContract()
	{
		var map = maps.Fresh(42);
		var args = TutorialContractScheduler.CreateBeatAHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsOffered(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.Equal(args.IssuerPoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void Create_Hunt_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Hunt, new DeliveryCreateArgs("poi-storage")));
	}

	[Fact]
	public void Create_Delivery_ThrowsNotSupported()
	{
		var map = maps.Fresh(42);

		Assert.Throws<NotSupportedException>(() =>
			ContractFactory.Create(map, EContractKind.Delivery, new DeliveryCreateArgs("poi-storage")));
	}
}

public sealed class TutorialContractSchedulerTests(StarMapFixture maps)
{
	[Fact]
	public void Create_EmptyContractRegistry()
	{
		var map = StarMap.Create(42);

		Assert.Empty(map.ContractRegistry.Offered);
	}

	[Fact]
	public void OfferBeatA_AddsStoryHuntAtAdministrativePoi()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;

		TutorialContractScheduler.OfferBeatA(map);

		var contract = Assert.Single(map.ContractRegistry.Offered);
		Assert.Equal(plan.AdministrativePoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void Start_IsIdempotent()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, GrimSpace.Run.State.PlayerFleetUnitId);
		using var orchestrator = StarSystemOrchestrator.FromMap(
			map,
			GrimSpace.Run.State.PlayerFleetUnitId);
		var scheduler = new TutorialContractScheduler(orchestrator);

		scheduler.Start();
		scheduler.Start();

		Assert.Single(orchestrator.Map.ContractRegistry.Offered);
	}
}
