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
}
