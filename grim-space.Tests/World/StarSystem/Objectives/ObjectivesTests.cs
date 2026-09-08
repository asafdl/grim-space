using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

public sealed class ContractFulfillmentTests(DevStarMapFixture maps)
{
	[Fact]
	public void Evaluate_CompletesHuntContractWhenSpawnTargetsAreGone()
	{
		var map = maps.Fresh(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();
		var hunt = (HuntObjective)map.ContractRegistry.All.First(contract => contract.Id == contractId).Objective;
		var group = hunt.SpawnGroups[0];
		var targetUnitId = $"{contractId}.{group.GroupId}.0";

		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			1,
			holderUnitId,
			new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				[group.GroupId] = [targetUnitId],
			}));

		Assert.True(map.ContractRegistry.TryGetActive(holderUnitId, out _));

		ContractFulfillment.Evaluate(map, holderUnitId);

		Assert.False(map.ContractRegistry.TryGetActive(holderUnitId, out _));
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
	}

	[Fact]
	public void Evaluate_LeavesActiveContractWhileTargetsRemain()
	{
		var map = maps.Fresh(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();
		var hunt = (HuntObjective)map.ContractRegistry.All.First(contract => contract.Id == contractId).Objective;
		var group = hunt.SpawnGroups[0];

		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			1,
			holderUnitId,
			new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
			{
				[group.GroupId] = [holderUnitId],
			}));

		ContractFulfillment.Evaluate(map, holderUnitId);

		Assert.True(map.ContractRegistry.TryGetActive(holderUnitId, out var active));
		Assert.Equal(contractId, active.Definition.Id);
		Assert.False(map.ContractRegistry.IsCompleted(contractId));
	}
}

public sealed class ObjectivesCollectorTests(DevStarMapFixture maps)
{
	[Fact]
	public void Collect_IncludesActiveContractsAndStoryObjectives()
	{
		var map = maps.Fresh(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();

		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			1,
			holderUnitId,
			ContractState.EmptyBindings));
		map.StoryObjectives.Add(new StoryObjective("story-1", "Reach the refinery", "Survey the supply chain."));

		var objectives = ObjectivesCollector.Collect(map, holderUnitId);

		Assert.Equal(2, objectives.Count);
		Assert.Contains(objectives, objective => objective.Source == EObjectiveSource.Contract);
		Assert.Contains(objectives, objective =>
			objective.Source == EObjectiveSource.Story && objective.Id == "story-1");
	}

	[Fact]
	public void Collect_ExcludesCompletedContracts()
	{
		var map = maps.Fresh(42);
		var contractId = map.ContractRegistry.Offered.First().Id;
		var holderUnitId = map.UnitRegistry.Ids.First();

		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			1,
			holderUnitId,
			ContractState.EmptyBindings));
		map.ContractRegistry.Complete(contractId);

		var objectives = ObjectivesCollector.Collect(map, holderUnitId);

		Assert.Empty(objectives);
	}
}
