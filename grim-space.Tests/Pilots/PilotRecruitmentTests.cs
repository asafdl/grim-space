using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotRecruitmentTests
{
	[Fact]
	public void RecruitableBackgrounds_AreAllT0()
	{
		Assert.Equal(6, PilotRecruitment.RecruitableBackgroundIds.Count);
		Assert.Equal(
			PilotRecruitment.RecruitableBackgroundIds.OrderBy(id => id),
			PilotBackgroundCatalog.All.Select(background => background.Id).OrderBy(id => id));
	}

	[Fact]
	public void ListUnlockedBackgrounds_AtT0_IncludesEveryBackground()
	{
		var backgrounds = PilotRecruitment.ListUnlockedBackgrounds(EMerchantAvailabilityBand.T0);

		Assert.Equal(PilotRecruitment.RecruitableBackgroundIds.Count, backgrounds.Count);
	}

}
