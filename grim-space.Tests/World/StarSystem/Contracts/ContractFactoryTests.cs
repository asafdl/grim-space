using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Encounter;
using GrimSpace.World.StarSystem.Landmarks;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.Tutorials;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractFactoryTests(StarMapFixture maps)
{
	[Fact]
	public void Create_Hunt_RegistersPendingContract()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);

		var contract = ContractFactory.Create(map, EContractKind.Hunt, args);

		Assert.True(map.ContractRegistry.IsPending(contract.Id));
		Assert.IsType<HuntObjective>(contract.Objective);
		Assert.Equal(args.IssuerPoiId, contract.IssuerPoiId);
		Assert.False(contract.IsStoryObjective);
		Assert.Equal(
			ContractRewardCalculator.Roll(map.Seed, contract.Id, EContractKind.Hunt, args.Danger),
			contract.Terms);
	}

	[Fact]
	public void Build_Hunt_DoesNotRegister()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);
		const string contractId = "contract-scheduler-test";

		var contract = ContractFactory.Build(map, contractId, EContractKind.Hunt, args);

		Assert.Equal(contractId, contract.Id);
		Assert.False(map.ContractRegistry.Contains(contractId));
	}

	[Fact]
	public void Build_Hunt_SameContractId_IsDeterministic()
	{
		var map = maps.Fresh(42);
		var args = CreateGeneratedHuntArgs(map);
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
		var (_, _, _, operatorName) = TutorialBeatContracts.BeatBDropoff(map);
		var deliveryArgs = new DeliveryCreateArgs(
			map.Blueprint.SupplyPlan.StoragePoiId,
			EDangerLevel.VeryLow,
			ContractNarrative.ForDelivery("x", "y", "z"),
			DropoffOperatorName: operatorName);

		Assert.Throws<ArgumentException>(() =>
			ContractFactory.Create(map, EContractKind.Hunt, deliveryArgs));
	}

	private static HuntCreateArgs CreateGeneratedHuntArgs(StarMap map)
	{
		var plan = map.Blueprint.SupplyPlan;
		return new HuntCreateArgs(
			plan.AdministrativePoiId,
			new AreaPickerArgs(MapLandmarkQueries.AllIds(map), DeterministicPickMix: 1),
			EDangerLevel.VeryLow,
			ContractNarrative.ForHunt("Generated Hunt"));
	}
}
