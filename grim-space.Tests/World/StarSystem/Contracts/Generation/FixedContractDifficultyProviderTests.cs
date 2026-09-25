using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts.Generation;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts.Generation;

[StarSystemTestSuite]
public sealed class FixedContractDifficultyProviderTests(StarMapFixture maps)
{
	[Fact]
	public void Get_ReturnsSameProfileRegardlessOfTick()
	{
		var map = maps.Fresh(42);
		var provider = FixedContractDifficultyProvider.Alpha;

		var atOne = provider.Get(map, 1);
		var atNinetyNine = provider.Get(map, 99);

		Assert.Equal(atOne.HuntRewardCredits, atNinetyNine.HuntRewardCredits);
		Assert.Equal(atOne.DeliveryRewardCredits, atNinetyNine.DeliveryRewardCredits);
		Assert.Equal(atOne.HuntEncounter.Danger, atNinetyNine.HuntEncounter.Danger);
	}

	[Fact]
	public void AlphaProfile_UsesVeryLowPirateHuntAndPositiveRewards()
	{
		var profile = FixedContractDifficultyProvider.Alpha.Get(maps.Fresh(42), 1);

		Assert.Equal(EDangerLevel.VeryLow, profile.HuntEncounter.Danger);
		Assert.Single(profile.HuntEncounter.MemberTypes);
		Assert.True(profile.HuntRewardCredits > 0);
		Assert.True(profile.DeliveryRewardCredits > 0);
	}
}
