using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
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
			Assert.Equal(EContractTask.Hunt, contract.Task);
			Assert.Equal(map.ControllingFaction, contract.IssuerFaction);
			Assert.Equal(plan.AdministrativePoiId, contract.IssuerPoiId);
			Assert.Equal(StarMap.DevContractRewardCredits, contract.Terms.RewardCredits);
			Assert.Contains(contract.Location.Description, contract.Narrative.Briefing);
			Assert.False(string.IsNullOrWhiteSpace(contract.Narrative.Title));
			Assert.False(string.IsNullOrWhiteSpace(contract.Location.Description));
		}
	}

	[Fact]
	public void CreateDevDefault_BriefingContainsLocationDescription()
	{
		var map = StarMap.CreateDevDefault(7);
		var contract = map.ContractRegistry.Offered.First();

		Assert.Contains(contract.Location.Description, contract.Narrative.Briefing);
		Assert.StartsWith("Survey the indicated sector.", contract.Narrative.Briefing);
	}
}
