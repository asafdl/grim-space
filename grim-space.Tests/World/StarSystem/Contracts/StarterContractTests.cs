using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class StarterContractTests(StarMapFixture maps)
{
	[Fact]
	public void Create_SeedsOfferedContractsWithValidShape()
	{
		var map = maps.Fresh(42);
		var plan = map.Blueprint.SupplyPlan;
		var offered = map.ContractRegistry.Offered.ToList();

		Assert.Single(offered);

		foreach (var contract in offered)
		{
			Assert.True(map.ContractRegistry.IsOffered(contract.Id));
			Assert.IsType<HuntObjective>(contract.Objective);
			Assert.Equal(map.ControllingFaction, contract.IssuerFaction);
			Assert.Equal(plan.AdministrativePoiId, contract.IssuerPoiId);
			Assert.True(contract.Terms.Payment.TryGet(ResourceId.Credits, out var credits));
			Assert.Equal(StarMap.StarterContractRewardCredits, credits);

			var hunt = (HuntObjective)contract.Objective;
			Assert.Single(hunt.SpawnGroups);
			Assert.False(string.IsNullOrWhiteSpace(contract.Narrative.Title));
			Assert.False(string.IsNullOrWhiteSpace(contract.Narrative.Briefing));
			var searchArea = hunt.SpawnGroups[0].SearchArea;
			Assert.False(string.IsNullOrWhiteSpace(ContractDisplay.DetailsBody(contract, map)));
			Assert.Contains(contract.Narrative.Briefing, ContractDisplay.DetailsBody(contract, map));
			Assert.IsType<AreaRelation.BetweenLandmarks>(searchArea.Relation);
			Assert.Equal(
				((AreaRelation.BetweenLandmarks)searchArea.Relation).LandmarkAId,
				searchArea.Intel.LandmarkAId);
			Assert.Equal(
				((AreaRelation.BetweenLandmarks)searchArea.Relation).LandmarkBId,
				searchArea.Intel.LandmarkBId);
		}
	}

}
