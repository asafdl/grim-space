using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Generation;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class DismissContractActionTests(StarMapFixture maps)
{
	[Fact]
	public void TryEnqueue_RequiresActiveContractHeldByActor()
	{
		var (engine, actorId, contractId) = CreateEngine(maps);

		Assert.False(engine.CreateSimulation().TryEnqueue(
			ContractActionTestContext.Dismiss(actorId, contractId)));

		Assert.True(engine.World.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			engine.Tick,
			actorId)));
		Assert.True(engine.World.ContractRegistry.TryGetState(contractId, out var active));
		Assert.Equal(EContractStatus.Active, active.Status);
		Assert.True(DismissContractDef.Instance.IsLegal(
			ContractActionTestContext.Dismiss(actorId, contractId),
			engine.World,
			engine.ActorRuntimes.For(actorId)));
	}

	[Fact]
	public void Commit_FailsContractAndCleansUpSpawnedFleetAndTravelState()
	{
		var (engine, actorId, contractId) = CreateEngine(maps);
		Assert.True(engine.World.ContractRegistry.Activate(new ContractState(
			contractId,
			EContractStatus.Active,
			engine.Tick,
			actorId)));

		var spawnedIds = engine.World.FleetRegistry.Ids
			.Where(id => id.StartsWith($"{contractId}.", StringComparison.Ordinal))
			.ToArray();
		foreach (var fleet in ContractEnemySpawner.SpawnForContract(
			engine.World.ContractRegistry.All.First(contract => contract.Id == contractId),
			engine.World,
			"test").Fleets)
		{
			if (fleet.State.Id != actorId)
				engine.World.FleetRegistry.Add(fleet);
		}
		spawnedIds = engine.World.FleetRegistry.Ids
			.Where(id => id.StartsWith($"{contractId}.", StringComparison.Ordinal))
			.ToArray();
		Assert.NotEmpty(spawnedIds);
		engine.World.StateOf(actorId).TravelTarget =
			GrimSpace.World.StarSystem.Units.TravelTarget.Fleet(
				spawnedIds[0],
				EContactIntent.Engagement);
		engine.World.WaitingForPlayerInput = true;

		engine.Commit(ContractActionTestContext.Dismiss(actorId, contractId));

		Assert.True(engine.World.ContractRegistry.TryGetState(contractId, out var state));
		Assert.Equal(EContractStatus.Failed, state.Status);
		Assert.DoesNotContain(
			engine.World.FleetRegistry.Ids,
			id => id.StartsWith($"{contractId}.", StringComparison.Ordinal));
		Assert.Equal(
			GrimSpace.World.StarSystem.Units.TravelTarget.None,
			engine.World.StateOf(actorId).TravelTarget);
		Assert.False(engine.World.WaitingForPlayerInput);
	}

	[Fact]
	public void TryEnqueue_RejectsStoryContract()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var actorId = map.FleetRegistry.All.First().State.Id;
		var contractId = map.ContractRegistry.Pending.First().Id;
		var runtimes = new ActorRuntimes<GrimSpace.World.StarSystem.Runtime.ActorRuntime>();
		runtimes.For(actorId);
		var engine = new Engine<StarMap, GrimSpace.World.StarSystem.Runtime.ActorRuntime>(map, runtimes);
		engine.Commit(ContractActionTestContext.Accept(engine.World, actorId, contractId));

		Assert.False(engine.CreateSimulation().TryEnqueue(
			ContractActionTestContext.Dismiss(actorId, contractId)));
	}

	private static (Engine<StarMap, GrimSpace.World.StarSystem.Runtime.ActorRuntime> Engine,
		string ActorId, string ContractId) CreateEngine(StarMapFixture maps)
	{
		var map = maps.FreshWithBeatAHunt(42);
		var actorId = map.FleetRegistry.All.First().State.Id;
		var starter = (HuntObjective)map.ContractRegistry.Pending.First().Objective;
		var contractId = "dismiss-test-contract";
		var hunt = new HuntObjective(starter.SpawnGroups
			.Select(group => group with { GroupId = "dismiss-group" })
			.ToArray());
		Assert.True(map.ContractRegistry.TryAdd(new Contract(
			contractId,
			hunt,
			map.ContractRegistry.Pending.First().Danger,
			map.ContractRegistry.Pending.First().IssuerFaction,
			map.ContractRegistry.Pending.First().IssuerPoiId,
			map.ContractRegistry.Pending.First().Terms,
			ContractNarrative.ForHunt("Dismiss test"))));

		var runtimes = new ActorRuntimes<GrimSpace.World.StarSystem.Runtime.ActorRuntime>();
		runtimes.For(actorId);
		return (new Engine<StarMap, GrimSpace.World.StarSystem.Runtime.ActorRuntime>(map, runtimes),
			actorId,
			contractId);
	}
}
