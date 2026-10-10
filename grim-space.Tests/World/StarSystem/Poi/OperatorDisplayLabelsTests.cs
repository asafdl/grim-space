using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class OperatorDisplayLabelsTests
{
	[Theory]
	[InlineData(EMerchantCatalog.Weapons, "Dockyard Shop")]
	[InlineData(EMerchantCatalog.ShipSupport, "Ship Support")]
	[InlineData(EMerchantCatalog.Ships, "Ship Broker")]
	public void MerchantRole_LabelsEveryCatalog(EMerchantCatalog catalog, string expected)
	{
		Assert.Equal(expected, OperatorDisplayLabels.MerchantRole(catalog));
	}

	[Theory]
	[InlineData(EFacilityOperatorRole.Contracts, "Contracts")]
	[InlineData(EFacilityOperatorRole.StoryContact, "Story Contract")]
	[InlineData(EFacilityOperatorRole.DeliveryTurnIn, "Delivery")]
	public void Role_LabelsVisibleTemporaryRoles(EFacilityOperatorRole role, string expected)
	{
		Assert.Equal(expected, OperatorDisplayLabels.Role(role));
	}

	[Fact]
	public void Title_UsesResolvedTemporaryRole()
	{
		var facilityOperator = new FacilityOperator(
			"Rook",
			EFacilityOperatorRole.Dialog,
			"Operator");

		Assert.Equal(
			"Rook - Story Contract",
			OperatorDisplayLabels.Title(
				facilityOperator,
				EFacilityOperatorRole.StoryContact));
	}
}
