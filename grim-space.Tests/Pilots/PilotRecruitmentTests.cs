using GrimSpace.Pilots;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotRecruitmentTests
{
	[Fact]
	public void RecruitableBackgrounds_MatchCatalog()
	{
		Assert.Equal(6, PilotRecruitment.RecruitableBackgroundIds.Count);
		Assert.Equal(
			PilotRecruitment.RecruitableBackgroundIds.OrderBy(id => id),
			PilotBackgroundCatalog.All.Select(background => background.Id).OrderBy(id => id));
	}
}
