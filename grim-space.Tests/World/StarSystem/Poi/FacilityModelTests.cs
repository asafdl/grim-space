using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Poi;

public sealed class FacilityModelTests(StarMapFixture maps)
{
	[Fact]
	public void AdministrativeCore_HasManagementFacility()
	{
		var world = maps.Fresh(42);
		var admin = world.PointsOfInterest.OfType<AdministrativeCore>().Single();
		var facility = Assert.Single(admin.Facilities);

		Assert.Equal(
			Facility.ScopedId(SupplySystemPlan.Copper.AdministrativePoiId, AdministrativeCore.ManagementFacilitySlug),
			facility.Id);
		Assert.Equal("Command Authority", facility.DisplayName);
		Assert.Equal(EPresentationAnchor.Management, facility.PresentationAnchor);
		Assert.Equal(AdministrativeCore.ManagementScenePath, facility.ScenePath);

		var contractOperator = Assert.Single(facility.Operators);
		Assert.Equal(MapFacilityOperators.ContractOperatorName(world), contractOperator.Name);
		Assert.Equal(EFacilityOperatorRole.Contracts, contractOperator.Role);
		Assert.Equal(AdministrativeCore.ContractOperatorSceneSlotId, contractOperator.SceneSlotId);
		Assert.Contains(contractOperator.Name, OperatorNames.Pool);
	}

	[Fact]
	public void TradeHub_HasDockyardFacility()
	{
		var world = maps.Fresh(42);
		var hub = world.PointsOfInterest.OfType<TradeHub>().Single();
		var facility = Assert.Single(hub.Facilities);

		Assert.Equal(
			Facility.ScopedId(SupplySystemPlan.Copper.TradeHubPoiId, TradeHub.DockyardFacilitySlug),
			facility.Id);
		Assert.Equal("Dockyard", facility.DisplayName);
		Assert.Equal(EPresentationAnchor.Dockyard, facility.PresentationAnchor);
		Assert.Equal(TradeHub.DockyardScenePath, facility.ScenePath);
		Assert.Equal(2, facility.Operators.Count);

		var shop = facility.Operators.Single(op => op.Role == EFacilityOperatorRole.DockyardShop);
		Assert.Equal(MapFacilityOperators.ShopOperatorName(world), shop.Name);
		Assert.Equal(TradeHub.ShopOperatorSceneSlotId, shop.SceneSlotId);

		var shield = facility.Operators.Single(op => op.Role == EFacilityOperatorRole.ShieldRecharge);
		Assert.Equal(MapFacilityOperators.ShieldOperatorName(world), shield.Name);
		Assert.Equal(TradeHub.ShieldOperatorSceneSlotId, shield.SceneSlotId);

		foreach (var op in facility.Operators)
			Assert.Contains(op.Name, OperatorNames.Pool);
	}

	[Fact]
	public void OtherMapPois_HaveEmptyFacilities()
	{
		var world = maps.Fresh(42);

		foreach (var poi in world.PointsOfInterest.Where(poi =>
			         poi is not AdministrativeCore and not TradeHub))
			Assert.Empty(poi.Facilities);
	}

	[Fact]
	public void Fork_PreservesFacilityList()
	{
		var world = maps.Fresh(42);
		var admin = world.PointsOfInterest.OfType<AdministrativeCore>().Single();
		var fork = admin.Fork();

		Assert.Same(admin.Facilities, fork.Facilities);
		Assert.Equal(admin.Facilities.Count, fork.Facilities.Count);
		Assert.Equal(admin.Facilities[0].Id, fork.Facilities[0].Id);
	}

	[Fact]
	public void Constructor_RejectsDuplicateOperatorNames_CaseInsensitive()
	{
		var op = new FacilityOperator(OperatorNames.Effy, EFacilityOperatorRole.Contracts, "A");
		Assert.Throws<ArgumentException>(() => new Facility(
			"test-facility",
			"Test",
			EPresentationAnchor.Management,
			"res://scenes/command_authority.tscn",
			[op, new FacilityOperator("effy", EFacilityOperatorRole.Dialog, "B")]));
	}

	[Fact]
	public void Constructor_RejectsDuplicateSceneSlotIds()
	{
		Assert.Throws<ArgumentException>(() => new Facility(
			"test-facility",
			"Test",
			EPresentationAnchor.Dockyard,
			TradeHub.DockyardScenePath,
			[
				new FacilityOperator(OperatorNames.Cl4nk, EFacilityOperatorRole.DockyardShop, "Salesman"),
				new FacilityOperator(OperatorNames.Zorp, EFacilityOperatorRole.ShieldRecharge, "Salesman"),
			]));
	}

	[Fact]
	public void Constructor_RejectsEmptyScenePath()
	{
		Assert.Throws<ArgumentException>(() => new Facility(
			"test-facility",
			"Test",
			EPresentationAnchor.Dockyard,
			"",
			[new FacilityOperator(OperatorNames.Cl4nk, EFacilityOperatorRole.DockyardShop, "Salesman")]));
	}

	[Fact]
	public void Constructor_RejectsEmptyOperatorName()
	{
		Assert.Throws<ArgumentException>(() => new Facility(
			"test-facility",
			"Test",
			EPresentationAnchor.Dockyard,
			TradeHub.DockyardScenePath,
			[new FacilityOperator("  ", EFacilityOperatorRole.DockyardShop, "Salesman")]));
	}

	[Fact]
	public void Constructor_RejectsEmptySceneSlotId()
	{
		Assert.Throws<ArgumentException>(() => new Facility(
			"test-facility",
			"Test",
			EPresentationAnchor.Dockyard,
			TradeHub.DockyardScenePath,
			[new FacilityOperator(OperatorNames.Cl4nk, EFacilityOperatorRole.DockyardShop, "")]));
	}

	[Fact]
	public void PoiFacade_WithYawFromApproach_PreservesLayout()
	{
		var facade = PoiFacade.Planet.WithYawFromApproach(1.0, 0.0);

		Assert.Equal(EFacadeLayout.Planet, facade.Layout);
		Assert.NotEqual(PoiFacade.Planet.YawDegrees, facade.YawDegrees);
	}

	[Fact]
	public void PoiFacade_Presets_SetExpectedLayouts()
	{
		Assert.Equal(EFacadeLayout.Default, PoiFacade.Default.Layout);
		Assert.Equal(EFacadeLayout.Planet, PoiFacade.Planet.Layout);
		Assert.Equal(EFacadeLayout.Station, PoiFacade.LargeStation.Layout);
	}

	[Fact]
	public void GeneratedMap_AssignsUniqueOperatorNamesPerRun()
	{
		var world = maps.Fresh(42);
		var names = world.PointsOfInterest
			.SelectMany(poi => poi.Facilities)
			.SelectMany(facility => facility.Operators)
			.Select(op => op.Name)
			.ToList();

		Assert.Equal(names.Count, names.Distinct(StringComparer.OrdinalIgnoreCase).Count());
		foreach (var name in names)
			Assert.Contains(name, OperatorNames.Pool);
	}

	[Fact]
	public void AdministrativeCore_FacadeLayout_MatchesPhysicalForm()
	{
		var plan = SupplySystemPlan.Copper;
		foreach (var seed in new[] { 1, 7, 42, 99 })
		{
			var admin = AdministrativeCore.Template(plan, seed, new OperatorNameAllocator(seed));
			var expectedLayout = admin.PhysicalForm == EPoiPhysicalForm.Planet
				? EFacadeLayout.Planet
				: EFacadeLayout.Station;
			Assert.Equal(expectedLayout, admin.Facade.Layout);
		}
	}
}
