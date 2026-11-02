using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Merchants;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DevBlackMarketLeadSpawnerTests(StarMapFixture maps)
{
	[Theory]
	[InlineData(EMerchantSubtype.Pilots)]
	[InlineData(EMerchantSubtype.Ships)]
	public void TrySpawn_AddsPendingLeadOfRequestedSubtype(EMerchantSubtype subtype)
	{
		var map = maps.Fresh(42);
		Assert.True(
			DevBlackMarketLeadSpawner.TrySpawn(map, subtype, out var contractId, out var failureReason),
			failureReason);

		var lead = Assert.Single(
			map.ContractRegistry.Pending,
			contract => contract.Category == EContractCategory.Lead);
		Assert.Equal(contractId, lead.Id);
		var objective = Assert.IsType<BlackMarketSaleObjective>(lead.Objective);
		Assert.Equal(subtype, objective.MerchantSubtype);
	}
}
