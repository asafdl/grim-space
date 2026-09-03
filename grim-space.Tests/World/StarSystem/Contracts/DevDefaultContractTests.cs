using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class DevDefaultContractTests
{
	[Fact]
	public void CreateDevDefault_SeedsOfferedContractsWithValidShape()
	{
		var map = StarMap.CreateDevDefault(42);
		var plan = map.Blueprint.SupplyPlan;
		var offered = map.ContractRegistry.Offered.ToList();

		Assert.Single(offered);

		foreach (var contract in offered)
		{
			Assert.True(map.ContractRegistry.IsOffered(contract.Id));
			Assert.IsType<HuntObjective>(contract.Objective);
			Assert.Equal(map.ControllingFaction, contract.IssuerFaction);
			Assert.Equal(plan.AdministrativePoiId, contract.IssuerPoiId);
			Assert.Equal(StarMap.DevContractRewardCredits, contract.Terms.RewardCredits);

			var hunt = (HuntObjective)contract.Objective;
			Assert.Single(hunt.SpawnGroups);
			Assert.Contains(hunt.SpawnGroups[0].SearchArea.Description, contract.Narrative.Briefing);
			Assert.False(string.IsNullOrWhiteSpace(contract.Narrative.Title));
			Assert.False(string.IsNullOrWhiteSpace(hunt.SpawnGroups[0].SearchArea.Description));
		}
	}

	[Fact]
	public void CreateDevDefault_BriefingContainsSearchAreaDescription()
	{
		var map = StarMap.CreateDevDefault(7);
		var contract = map.ContractRegistry.Offered.First();
		var hunt = (HuntObjective)contract.Objective;

		Assert.Contains(hunt.SpawnGroups[0].SearchArea.Description, contract.Narrative.Briefing);
		Assert.StartsWith("Hunt targets in the indicated sector.", contract.Narrative.Briefing);
	}
}
