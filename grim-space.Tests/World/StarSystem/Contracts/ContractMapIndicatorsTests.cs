using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractMapIndicatorsTests(StarMapFixture maps)
{
	[Fact]
	public void CountPendingByIssuerPoi_IncludesStarterContractAtAdminPoi()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var plan = map.Blueprint.SupplyPlan;
		var counts = ContractMapIndicators.CountPendingByIssuerPoi(map);

		Assert.True(counts.TryGetValue(plan.AdministrativePoiId, out var count));
		Assert.Equal(1, count);
	}

	[Fact]
	public void CountPendingByIssuerPoi_OmitsPoiAfterContractRejected()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null));

		var counts = ContractMapIndicators.CountPendingByIssuerPoi(map);

		Assert.Empty(counts);
	}

	[Fact]
	public void TooltipForCount_UsesAvailablePhrase()
	{
		Assert.Equal("1 available", ContractMapIndicators.TooltipForCount(1));
		Assert.Equal("3 available", ContractMapIndicators.TooltipForCount(3));
	}
}
