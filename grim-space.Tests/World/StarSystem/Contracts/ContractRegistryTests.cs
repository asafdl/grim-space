using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractRegistryTests(StarMapFixture maps)
{
	[Fact]
	public void AvailableForPoi_ReturnsOnlyOfferedContractsForIssuer()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var issuerPoiId = map.Blueprint.SupplyPlan.AdministrativePoiId;
		var contract = map.ContractRegistry.AvailableForPoi(issuerPoiId).Single();

		Assert.Equal(issuerPoiId, contract.IssuerPoiId);
		Assert.Empty(map.ContractRegistry.AvailableForPoi(map.Blueprint.SupplyPlan.RefineryPoiId));
	}

	[Fact]
	public void Reject_ExcludesContractFromOffered()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;

		map.ContractRegistry.Activate(CreateRejectedState(contractId));

		Assert.False(map.ContractRegistry.IsPending(contractId));
		Assert.True(map.ContractRegistry.IsRejected(contractId));
		Assert.DoesNotContain(contractId, map.ContractRegistry.Pending.Select(contract => contract.Id));
	}

	[Fact]
	public void IsPending_DistinguishesOfferedFromActive()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();

		Assert.True(map.ContractRegistry.IsPending(contractId));
		Assert.False(map.ContractRegistry.TryGetActive(holderUnitId, out _));

		map.ContractRegistry.Activate(CreateActiveState(map, contractId, holderUnitId, map.Timeline.Clock.Current));

		Assert.False(map.ContractRegistry.IsPending(contractId));
		Assert.True(map.ContractRegistry.TryGetActive(holderUnitId, out var active));
		Assert.Equal(contractId, active.Definition.Id);
		Assert.Equal(EContractStatus.Active, active.State.Status);
		Assert.Equal(holderUnitId, active.State.HolderUnitId);
	}

	[Fact]
	public void RemoveExpired_RemovesOnlyDueOfferedContracts()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var registry = map.ContractRegistry;
		var template = registry.Pending.First();
		var hunt = (HuntObjective)template.Objective;

		registry.TryAdd(CloneContract(template, "exp-soon", hunt), expiresAtTick: 5);
		registry.TryAdd(CloneContract(template, "exp-later", hunt), expiresAtTick: 10);
		registry.TryAdd(CloneContract(template, "immortal", hunt));

		Assert.Equal(4, registry.Pending.Count());

		var removedAt4 = registry.RemoveExpired(4);
		Assert.Empty(removedAt4);
		Assert.True(registry.IsPending("exp-soon"));

		var removedAt5 = registry.RemoveExpired(5);
		Assert.Single(removedAt5);
		Assert.Equal("exp-soon", removedAt5[0]);
		Assert.False(registry.IsPending("exp-soon"));
		Assert.True(registry.IsPending("exp-later"));
		Assert.True(registry.IsPending("immortal"));
	}

	[Fact]
	public void RemoveExpired_DoesNotRemoveActiveOrRejected()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var registry = map.ContractRegistry;
		var template = registry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var holderUnitId = map.FleetRegistry.Ids.First();

		registry.TryAdd(CloneContract(template, "active-exp", hunt), expiresAtTick: 1);
		registry.Activate(CreateActiveState(map, "active-exp", holderUnitId, 0));

		registry.TryAdd(CloneContract(template, "rejected-exp", hunt), expiresAtTick: 1);
		registry.Activate(CreateRejectedState("rejected-exp"));

		Assert.Empty(registry.RemoveExpired(1));
		Assert.True(registry.TryGetState("active-exp", out _));
		Assert.True(registry.IsRejected("rejected-exp"));
	}

	[Fact]
	public void CountPendingGenerated_ExcludesStoryObjectives()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var registry = map.ContractRegistry;
		Assert.Equal(1, registry.CountPending());
		Assert.Equal(0, registry.CountPendingGenerated());

		var template = registry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		registry.TryAdd(CloneContract(template, "generated", hunt) with { IsStoryObjective = false }, expiresAtTick: 50);

		Assert.Equal(2, registry.CountPending());
		Assert.Equal(1, registry.CountPendingGenerated());
	}

	[Fact]
	public void TryAdd_AtOfferedCap_FailsWithoutMutation()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var registry = map.ContractRegistry;
		registry.MaxPending = 3;
		var template = registry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var offeredBefore = registry.Pending.Select(contract => contract.Id).ToHashSet();

		Assert.True(registry.TryAdd(CloneContract(template, "slot-a", hunt), expiresAtTick: 100));
		Assert.True(registry.TryAdd(CloneContract(template, "slot-b", hunt), expiresAtTick: 101));
		Assert.False(registry.TryAdd(CloneContract(template, "slot-c", hunt), expiresAtTick: 102));
		Assert.False(registry.TryAdd(CloneContract(template, "immortal-extra", hunt)));
		Assert.False(registry.Contains("slot-c"));
		Assert.Equal(3, registry.Pending.Count());

		foreach (var id in offeredBefore)
			Assert.True(registry.IsPending(id));
	}

	[Fact]
	public void Fork_PreservesExpirationMetadata()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var registry = map.ContractRegistry;
		var template = registry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		registry.TryAdd(CloneContract(template, "fork-exp", hunt), expiresAtTick: 99);

		var fork = map.Fork();

		Assert.True(fork.ContractRegistry.TryGetExpiration("fork-exp", out var expiresAtTick));
		Assert.Equal(99, expiresAtTick);
		Assert.Equal(registry.MaxPending, fork.ContractRegistry.MaxPending);
	}

	[Fact]
	public void Fork_PreservesAcceptedContractState()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();
		map.ContractRegistry.Activate(CreateActiveState(map, contractId, holderUnitId, 1));

		var fork = map.Fork();

		Assert.False(fork.ContractRegistry.IsPending(contractId));
		Assert.True(fork.ContractRegistry.TryGetActive(holderUnitId, out var forkActive));
		Assert.Equal(contractId, forkActive.Definition.Id);
		Assert.Equal(EContractStatus.Active, forkActive.State.Status);
		Assert.Equal(holderUnitId, forkActive.State.HolderUnitId);

		var divergentContract = map.ContractRegistry.All.First(contract => contract.Id == contractId)
			with { Id = "contract-2" };
		Assert.True(map.ContractRegistry.TryAdd(divergentContract));
		map.ContractRegistry.Activate(CreateActiveState(map, "contract-2", holderUnitId, 1));
		Assert.True(map.ContractRegistry.TryGetState("contract-2", out _));
		Assert.False(fork.ContractRegistry.TryGetState("contract-2", out _));
	}

	private static Contract CloneContract(Contract template, string contractId, HuntObjective hunt) =>
		new(
			contractId,
			hunt,
			template.IssuerFaction,
			template.IssuerPoiId,
			template.Terms,
			template.Narrative);

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
