using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersPendingContract()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatAHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsPending(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.Equal(args.IssuerPoiId, contract.IssuerPoiId);
		Assert.True(contract.IsStoryObjective);
	}

	[Fact]
	public void Build_Hunt_DoesNotRegister()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatAHuntArgs(map);
		const string contractId = "contract-scheduler-test";

		var contract = ContractFactory.Build(map, contractId, EContractKind.Hunt, args);

		Assert.Equal(contractId, contract.Id);
		Assert.False(map.ContractRegistry.Contains(contractId));
	}

	[Fact]
	public void Build_Hunt_SameContractId_IsDeterministic()
	{
		var map = maps.Fresh(42);
		var args = TutorialBeatContracts.CreateBeatAHuntArgs(map);
		const string contractId = "contract-deterministic";

		var first = (HuntObjective)ContractFactory.Build(map, contractId, EContractKind.Hunt, args).Objective;
		var second = (HuntObjective)ContractFactory.Build(map, contractId, EContractKind.Hunt, args).Objective;

		Assert.Equal(ContractFactory.SpawnGroupIdFor(contractId), first.SpawnGroups[0].GroupId);
		Assert.Equal(first.SpawnGroups[0].GroupId, second.SpawnGroups[0].GroupId);
		Assert.Equal(first.SpawnGroups[0].Spawn.Seed, second.SpawnGroups[0].Spawn.Seed);
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
