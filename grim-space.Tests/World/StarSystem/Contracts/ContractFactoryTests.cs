using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersOfferedContract()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatAHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsOffered(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.Equal(args.IssuerPoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void Create_Hunt_WithMismatchedArgs_Throws()
	{
		var map = maps.Fresh(42);

		var deliveryArgs = TutorialBeatContracts.CreateBeatBDeliveryArgs(map);
		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Hunt, deliveryArgs));
	}
}
