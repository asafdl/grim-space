using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DevPilotRecruitmentLeadSpawnerTests(StarMapFixture maps)
{
	[Fact]
	public void TrySpawn_AddsPendingPilotsLead()
	{
		var map = maps.Fresh(42);
		Assert.True(DevPilotRecruitmentLeadSpawner.TrySpawn(map, out var failureReason), failureReason);

		var lead = Assert.Single(
			map.ContractRegistry.Pending,
			contract => contract.Category == EContractCategory.Lead);
		var objective = Assert.IsType<BlackMarketSaleObjective>(lead.Objective);
		Assert.Equal(EMerchantSubtype.Pilots, objective.MerchantSubtype);
	}

	[Fact]
	public void TrySpawn_RequiresRecruitableBackgrounds()
	{
		var map = maps.Fresh(42);

		Assert.True(MerchantSubtypeStock.HasOffers(map, EMerchantSubtype.Pilots));
		Assert.True(DevPilotRecruitmentLeadSpawner.TrySpawn(map, out _));
	}
}
