using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractRegistryTests
{
	[Fact]
	public void AvailableForPoi_ReturnsOnlyOfferedContractsForIssuer()
	{
		var map = StarMap.CreateDevDefault(42);
		var issuerPoiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		var contract = map.ContractRegistry.AvailableForPoi(issuerPoiId).Single();

		Assert.Equal(issuerPoiId, contract.IssuerPoiId);
		Assert.Empty(map.ContractRegistry.AvailableForPoi(map.Blueprint.SupplyPlan.RefineryPoiId));
	}

	[Fact]
	public void Reject_ExcludesContractFromOffered()
	{
		var map = StarMap.CreateDevDefault(42);
		var contractId = map.ContractRegistry.Offered.First().Id;

		map.ContractRegistry.Activate(CreateRejectedState(contractId));

		Assert.False(map.ContractRegistry.IsOffered(contractId));
		Assert.True(map.ContractRegistry.IsRejected(contractId));
		Assert.DoesNotContain(contractId, map.ContractRegistry.Offered.Select(contract => contract.Id));
	}

	[Fact]
	public void IsOffered_DistinguishesOfferedFromActive()
	{
		var map = StarMap.CreateDevDefault(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();

		Assert.True(map.ContractRegistry.IsOffered(contractId));
		Assert.False(map.ContractRegistry.TryGetActive(holderUnitId, out _));

		map.ContractRegistry.Activate(CreateActiveState(map, contractId, holderUnitId, map.Timeline.Clock.Current));

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
		map.ContractRegistry.Activate(CreateActiveState(map, contractId, holderUnitId, 1));

		var fork = map.Fork();

		Assert.False(fork.ContractRegistry.IsOffered(contractId));
		Assert.True(fork.ContractRegistry.TryGetActive(holderUnitId, out var forkActive));
		Assert.Equal(contractId, forkActive.Definition.Id);
		Assert.Equal(EContractStatus.Active, forkActive.State.Status);
		Assert.Equal(holderUnitId, forkActive.State.HolderUnitId);

		var divergentContract = map.ContractRegistry.All.First(contract => contract.Id == contractId)
			with { Id = "contract-2" };
		map.ContractRegistry.RegisterOffered(divergentContract);
		map.ContractRegistry.Activate(CreateActiveState(map, "contract-2", holderUnitId, 1));
		Assert.True(map.ContractRegistry.TryGetState("contract-2", out _));
		Assert.False(fork.ContractRegistry.TryGetState("contract-2", out _));
	}

	private static ContractState CreateRejectedState(string contractId) =>
		new(contractId, EContractStatus.Rejected, null, null, ContractState.EmptyBindings);

	private static ContractState CreateActiveState(
		StarMap map,
		string contractId,
		string holderUnitId,
		int acceptedAtTick)
	{
		var hunt = (HuntObjective)map.ContractRegistry.All.First(contract => contract.Id == contractId).Objective;
		var group = hunt.SpawnGroups[0];
		var bindings = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
		{
			[group.GroupId] = [$"{contractId}.{group.GroupId}.0"],
		};
		return new ContractState(
			contractId,
			EContractStatus.Active,
			acceptedAtTick,
			holderUnitId,
			bindings);
	}
}
