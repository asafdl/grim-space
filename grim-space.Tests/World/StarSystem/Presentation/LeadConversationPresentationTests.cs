using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.World.StarSystem.Poi.Dialog;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Merchants;

namespace GrimSpace.Tests.World.StarSystem.Presentation;

[StarSystemTestSuite]
public sealed class LeadConversationPresentationTests(StarMapFixture maps)
{
	[Theory]
	[InlineData(EMerchantSubtype.Pilots, "pilots")]
	[InlineData(EMerchantSubtype.Ships, "ships")]
	public void FacilityLeadConversation_ResolvesDestinationAndMerchandise(
		EMerchantSubtype subtype,
		string merchandise)
	{
		var map = maps.Fresh(42);
		var contract = BuildLead(
			map,
			$"lead-{subtype}",
			subtype,
			spaceMeetingChance: 0);
		var destination = Assert.IsType<BlackMarketFacilityDestination>(
			Assert.IsType<BlackMarketSaleObjective>(contract.Objective).Destination);

		var line = LeadContactMessageCatalog.Pick(contract, map);

		Assert.Equal(
			$"Hey, if you're interested, I heard there's a sale going at " +
			$"{map.GetPointOfInterest(destination.PoiId).DisplayName} for {merchandise}.",
			line);
		Assert.DoesNotContain("{destination}", line);
		Assert.DoesNotContain("{merchant}", line);
	}

	[Fact]
	public void SpaceLeadConversation_DescribesRendezvous()
	{
		var map = maps.Fresh(42);
		var contract = BuildLead(
			map,
			"lead-space-conversation",
			EMerchantSubtype.Ships,
			spaceMeetingChance: 1);

		var line = LeadContactMessageCatalog.Pick(contract, map);

		Assert.Contains("a rendezvous point:", line);
		Assert.Contains("for ships", line);
	}

	[Fact]
	public void BlackMarketLeadDialog_OffersAcceptAndRejectChoices()
	{
		var dialog = FacilityNpcDialogs.BlackMarketLead("Rook", "Rumor.");

		Assert.Equal("Rook", dialog.SpeakerName);
		Assert.Equal("Rumor.", dialog.Line);
		Assert.Collection(
			dialog.Choices,
			choice =>
			{
				Assert.Equal(FacilityNpcDialogs.AcceptLeadChoiceId, choice.Id);
				Assert.Equal("Interested", choice.Label);
			},
			choice =>
			{
				Assert.Equal(FacilityNpcDialogs.RejectLeadChoiceId, choice.Id);
				Assert.Equal("Decline", choice.Label);
			});
	}

	private static Contract BuildLead(
		StarMap map,
		string contractId,
		EMerchantSubtype subtype,
		double spaceMeetingChance) =>
		ContractFactory.Build(
			map,
			contractId,
			EContractKind.Lead,
			new LeadCreateArgs(
				ELeadKind.BlackMarketSale,
				new ContractNarrative(
					"Quiet Opportunity",
					"Hey, if you're interested, I heard there's a sale going at {destination} for {merchant}."),
				300,
				spaceMeetingChance,
				MerchantSubtype: subtype));
}
