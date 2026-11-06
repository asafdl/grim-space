using GrimSpace.Pilots;
using GrimSpace.Units.Enums;
using GrimSpace.World.StarSystem.Merchants;

namespace GrimSpace.Tests.Pilots;

[IntegrationTestSuite]
public sealed class PilotRecruitmentTests
{
	[Fact]
	public void RecruitableOperatorClasses_MatchConfiguredPlayableClasses()
	{
		Assert.Equal(4, PilotRecruitment.RecruitableOperatorClassIds.Count);
		Assert.Equal(
			PilotRecruitment.RecruitableOperatorClassIds.OrderBy(id => id),
			new[]
			{
				EOperatorClassId.SpaceRanger,
				EOperatorClassId.Support,
				EOperatorClassId.Tank,
				EOperatorClassId.Rogue,
			}.OrderBy(id => id));
	}

	[Fact]
	public void RecruitableBackgrounds_MatchCatalog()
	{
		Assert.Equal(10, PilotRecruitment.RecruitableBackgroundIds.Count);
		Assert.Equal(
			PilotRecruitment.RecruitableBackgroundIds.OrderBy(id => id),
			PilotBackgroundCatalog.All.Select(background => background.Id).OrderBy(id => id));
	}
}
