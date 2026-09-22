using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractOfferMapIndicatorsTests(StarMapFixture maps)
{
	[Fact]
	public void CountOfferedByIssuerPoi_IncludesStarterContractAtAdminPoi()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var plan = map.Blueprint.SupplyPlan;
		var counts = ContractOfferMapIndicators.CountOfferedByIssuerPoi(map);

		Assert.True(counts.TryGetValue(plan.AdministrativePoiId, out var count));
		Assert.Equal(1, count);
	}

	[Fact]
	public void CountOfferedByIssuerPoi_OmitsPoiAfterContractRejected()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Rejected,
			null,
			null,
			ContractState.EmptyBindings));

		var counts = ContractOfferMapIndicators.CountOfferedByIssuerPoi(map);

		Assert.Empty(counts);
	}

	[Fact]
	public void TooltipForCount_UsesAvailablePhrase()
	{
		Assert.Equal("1 available", ContractOfferMapIndicators.TooltipForCount(1));
		Assert.Equal("3 available", ContractOfferMapIndicators.TooltipForCount(3));
	}
}
