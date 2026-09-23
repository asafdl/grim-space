using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.Tutorials;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Areas;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class ContractFulfillmentTests(StarMapFixture maps)
{
	[Fact]
	public void ReactionsFor_ReturnsCompletionWhenDefeatedFleetWasLastBoundTarget()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();
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

		var completion = Assert.IsType<CompleteContractAction>(
			Assert.Single(ContractFulfillment.ReactionsFor(map, holderUnitId)));
		Assert.Equal(holderUnitId, completion.ActorId);
		Assert.Equal(contractId, completion.ContractId);
		Assert.True(completion.Payment.TryGet(ResourceId.Credits, out var payment));
		Assert.Equal(TutorialBeatContracts.BeatAHuntRewardCredits, payment);

		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(holderUnitId);
		using var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(completion);
		Assert.True(map.ContractRegistry.IsCompleted(contractId));
		Assert.Equal(TutorialBeatContracts.BeatAHuntRewardCredits, map.PlayerResources.GetBalance(ResourceId.Credits));
	}

	[Fact]
	public void Completion_GrantsContractPaymentOnce()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();
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

		var completion = Assert.IsType<CompleteContractAction>(
			Assert.Single(ContractFulfillment.ReactionsFor(map, holderUnitId)));
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(holderUnitId);
		using var engine = new Engine<StarMap, ActorRuntime>(map, runtimes);
		engine.Commit(completion);
		var creditsAfterFirst = map.PlayerResources.GetBalance(ResourceId.Credits);
		var historyCount = map.Timeline.History().Count;

		engine.Commit(completion);

		Assert.Equal(creditsAfterFirst, map.PlayerResources.GetBalance(ResourceId.Credits));
		Assert.Equal(historyCount, map.Timeline.History().Count);
	}

}

[StarSystemTestSuite]
public sealed class ObjectivesCollectorTests(StarMapFixture maps)
{
	[Fact]
	public void Collect_IncludesActiveContractsAndStoryObjectives()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();

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
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();

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

	[Fact]
	public void Collect_ActiveStarterContract_IncludesRouteIntelAndReward()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var contractId = map.ContractRegistry.Pending.First().Id;
		var holderUnitId = map.FleetRegistry.Ids.First();
		var contract = map.ContractRegistry.All.First(candidate => candidate.Id == contractId);
		var relation = Assert.IsType<AreaRelation.BetweenLandmarks>(
			((HuntObjective)contract.Objective).SpawnGroups[0].SearchArea.Relation);

		map.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			1,
			holderUnitId,
			ContractState.EmptyBindings));

		var objectives = ObjectivesCollector.Collect(map, holderUnitId);
		var objective = Assert.Single(objectives);

		Assert.Equal(EObjectiveSource.Contract, objective.Source);
		Assert.Equal("Pirate Hunt ★", objective.Title);
		var route = Assert.IsType<ObjectiveSummaryContent.RouteBetweenLandmarks>(objective.Summary);
		Assert.Equal(relation.LandmarkAId, route.LandmarkAPoiId);
		Assert.Equal(relation.LandmarkBId, route.LandmarkBPoiId);
		Assert.True(objective.Reward.TryGet(ResourceId.Credits, out var credits));
		Assert.Equal(TutorialBeatContracts.BeatAHuntRewardCredits, credits);
	}

	[Fact]
	public void Collect_StoryObjective_HasPlainSummaryAndNoReward()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var holderUnitId = map.FleetRegistry.Ids.First();
		map.StoryObjectives.Add(new StoryObjective("story-1", "Reach the refinery", "Survey the supply chain."));

		var objective = Assert.Single(ObjectivesCollector.Collect(map, holderUnitId));

		Assert.Equal(EObjectiveSource.Story, objective.Source);
		var plain = Assert.IsType<ObjectiveSummaryContent.Plain>(objective.Summary);
		Assert.Equal("Survey the supply chain.", plain.Text);
		Assert.True(objective.Reward.IsEmpty);
	}
}
