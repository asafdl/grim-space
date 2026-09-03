using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractRegistryTests
{
	[Fact]
	public void IsOffered_DistinguishesOfferedFromActive()
	{
		var map = StarMap.CreateDevDefault(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();

		Assert.True(map.ContractRegistry.IsOffered(contractId));
		Assert.False(map.ContractRegistry.TryGetActive(holderUnitId, out _));

		map.ContractRegistry.Accept(contractId, holderUnitId, map.Timeline.Clock.Current);

		Assert.False(map.ContractRegistry.IsOffered(contractId));
		Assert.True(map.ContractRegistry.TryGetActive(holderUnitId, out var active));
		Assert.Equal(contractId, active.Definition.Id);
		Assert.Equal(EContractStatus.Active, active.State.Status);
		Assert.Equal(holderUnitId, active.State.HolderUnitId);
	}

	[Fact]
	public void Fork_PreservesAcceptedContractState()
	{
		var map = StarMap.CreateDevDefault(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();
		map.ContractRegistry.Accept(contractId, holderUnitId, 1);

		var fork = map.Fork();

		Assert.False(fork.ContractRegistry.IsOffered(contractId));
		Assert.True(fork.ContractRegistry.TryGetActive(holderUnitId, out var forkActive));
		Assert.Equal(contractId, forkActive.Definition.Id);
		Assert.Equal(EContractStatus.Active, forkActive.State.Status);
		Assert.Equal(holderUnitId, forkActive.State.HolderUnitId);

		var divergentContract = map.ContractRegistry.All.First(contract => contract.Id == contractId)
			with { Id = "contract-2" };
		map.ContractRegistry.RegisterOffered(divergentContract);
		map.ContractRegistry.Accept("contract-2", holderUnitId, 1);
		Assert.True(map.ContractRegistry.TryGetState("contract-2", out _));
		Assert.False(fork.ContractRegistry.TryGetState("contract-2", out _));
	}
}
