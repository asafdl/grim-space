using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Tests.Run;

public sealed class StartingFuelGrantTests
{
	[Fact]
	public void StarMapCreate_SeedsStartingResources()
	{
		var map = StarMap.Create(42);

		AssertStartingResources(map.PlayerResources);
	}

	[Fact]
	public void CreateNewRun_HasStartingResources()
	{
		using var run = State.CreateNewRun(42);

		AssertStartingResources(run.StarSystem.Map.PlayerResources);
	}

	private static void AssertStartingResources(PlayerResources resources)
	{
		Assert.Equal(
			StrategicEconomyConfig.StartingCredits,
			resources.GetBalance(ResourceId.Credits));
		Assert.Equal(
			StrategicEconomyConfig.StartingScrapAlloy,
			resources.GetBalance(ResourceId.ScrapAlloy));
		Assert.Equal(
			StrategicEconomyConfig.StartingFuel,
			resources.GetBalance(ResourceId.Fuel));
	}
}
