using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.World.StarSystem.Poi;
using GrimSpace.World.StarSystem.Poi.Concrete;

namespace GrimSpace.Tests.World.StarSystem.Poi;

public sealed class FacilityModelTests
{
	[Fact]
	public void AdministrativeCore_HasManagementFacility()
	{
		var world = StarMap.CreateDevDefault(42);
		var admin = world.PointsOfInterest.OfType<AdministrativeCore>().Single();
		var facility = Assert.Single(admin.Facilities);

		Assert.Equal("poi-admin-management", facility.Id);
		Assert.Equal("Command Authority", facility.DisplayName);
		Assert.Equal(EPresentationAnchor.Management, facility.PresentationAnchor);
		Assert.Equal([EServiceKind.Contracts], facility.ServiceKinds);
	}

	[Fact]
	public void OtherDevMapPois_HaveEmptyFacilities()
	{
		var world = StarMap.CreateDevDefault(42);

		foreach (var poi in world.PointsOfInterest.Where(poi => poi is not AdministrativeCore))
			Assert.Empty(poi.Facilities);
	}

	[Fact]
	public void Fork_PreservesFacilityList()
	{
		var world = StarMap.CreateDevDefault(42);
		var admin = world.PointsOfInterest.OfType<AdministrativeCore>().Single();
		var fork = admin.Fork();

		Assert.Same(admin.Facilities, fork.Facilities);
		Assert.Equal(admin.Facilities.Count, fork.Facilities.Count);
		Assert.Equal("poi-admin-management", fork.Facilities[0].Id);
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
	public void AdministrativeCore_FacadeLayout_MatchesPhysicalForm()
	{
		var plan = SupplySystemPlan.Copper;
		foreach (var seed in new[] { 1, 7, 42, 99 })
		{
			var admin = AdministrativeCore.Template(plan, seed);
			var expectedLayout = admin.PhysicalForm == EPoiPhysicalForm.Planet
				? EFacadeLayout.Planet
				: EFacadeLayout.Station;
			Assert.Equal(expectedLayout, admin.Facade.Layout);
		}
	}
}
