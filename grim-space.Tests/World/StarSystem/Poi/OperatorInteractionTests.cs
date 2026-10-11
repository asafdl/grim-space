using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi;
using SupplySystemPlan = GrimSpace.World.StarSystem.Generation.SupplySystemPlan;
using Coord = GrimSpace.Math.Grid.Coord;

namespace GrimSpace.Tests.World.StarSystem.Poi;

[StarSystemTestSuite]
public sealed class OperatorInteractionTests
{
	[Theory]
	[InlineData(EMerchantSubtype.Weapons)]
	[InlineData(EMerchantSubtype.ShipSupport)]
	[InlineData(EMerchantSubtype.Ships)]
	public void Merchant_RequiresAndPreservesSubtype(EMerchantSubtype subtype)
	{
		var interaction = new OperatorInteraction(EFacilityOperatorRole.Merchant, subtype);

		Assert.Equal(EFacilityOperatorRole.Merchant, interaction.Role);
		Assert.Equal(subtype, interaction.MerchantSubtype);
	}

	[Fact]
	public void Merchant_RejectsMissingSubtype()
	{
		Assert.Throws<ArgumentException>(() =>
			new OperatorInteraction(EFacilityOperatorRole.Merchant));
	}

	[Fact]
	public void NonMerchant_RejectsSubtype()
	{
		Assert.Throws<ArgumentException>(() =>
			new OperatorInteraction(
				EFacilityOperatorRole.HealingStation,
				EMerchantSubtype.ShipSupport));
	}

	[Theory]
	[InlineData(EMerchantCatalog.Weapons, EMerchantSubtype.Weapons)]
	[InlineData(EMerchantCatalog.ShipSupport, EMerchantSubtype.ShipSupport)]
	[InlineData(EMerchantCatalog.Ships, EMerchantSubtype.Ships)]
	public void LegacyMerchantTemplate_MapsCatalogToSubtype(
		EMerchantCatalog catalog,
		EMerchantSubtype expected)
	{
		var facilityOperator = new FacilityOperator(
			"Rook",
			EFacilityOperatorRole.Merchant,
			"Operator",
			catalog);

		Assert.Equal(expected, facilityOperator.Interaction.MerchantSubtype);
	}

	[Fact]
	public void TemporaryRole_ReplacesBaseMerchantInteraction()
	{
		var poi = new TestPointOfInterest();
		var baseInteraction = new OperatorInteraction(
			EFacilityOperatorRole.Merchant,
			EMerchantSubtype.Weapons);
		poi.OperatorTemporaryRoles.Grant(
			"facility",
			"Rook",
			EFacilityOperatorRole.StoryContact,
			"story");

		var resolved = poi.ResolveInteraction("facility", "Rook", baseInteraction);

		Assert.Equal(EFacilityOperatorRole.StoryContact, resolved.Role);
		Assert.Null(resolved.MerchantSubtype);
	}

	private sealed class TestPointOfInterest()
		: PointOfInterest("test", "Test", 1, EPoiLogicalRole.Trade, null)
	{
		public override string DockNeighbourPoiId(
			SupplySystemPlan plan) => throw new NotSupportedException();

		public override PointOfInterest Fork() => throw new NotSupportedException();

		protected override PointOfInterest WithCenter(Coord center) =>
			throw new NotSupportedException();
	}
}
