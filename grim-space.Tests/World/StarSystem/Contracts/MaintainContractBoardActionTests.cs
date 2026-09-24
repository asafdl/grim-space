using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;

namespace GrimSpace.Tests.World.StarSystem.Contracts;

[StarSystemTestSuite]
public sealed class MaintainContractBoardActionTests(StarMapFixture maps)
{
	[Fact]
	public void Commit_ExpiresDuePendingContracts()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var template = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var engine = CreateEngine(map);
		var tick = engine.Tick;
		engine.World.ContractRegistry.TryAdd(Clone(template, "due", hunt), expiresAtTick: tick);

		engine.Commit(Collect(engine.World, tick, []));

		Assert.False(engine.World.ContractRegistry.IsPending("due"));
	}

	[Fact]
	public void Commit_ExpiresBeforeAddingNewContracts()
	{
		var map = maps.FreshWithBeatAHunt(42);
		map.ContractRegistry.MaxPending = 2;
		var template = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var engine = CreateEngine(map);
		var tick = engine.Tick;
		engine.World.ContractRegistry.TryAdd(Clone(template, "due", hunt), expiresAtTick: tick);
		var replacement = Clone(template, "replacement", hunt);

		engine.Commit(Collect(engine.World, tick, [new ContractAddition(replacement, tick + 15)]));

		Assert.False(engine.World.ContractRegistry.IsPending("due"));
		Assert.True(engine.World.ContractRegistry.IsPending("replacement"));
	}

	[Fact]
	public void Commit_SkipsRegisterWhenAtCap()
	{
		var map = maps.FreshWithBeatAHunt(42);
		map.ContractRegistry.MaxPending = 2;
		var template = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var first = Clone(template, "slot-a", hunt);
		var second = Clone(template, "slot-b", hunt);

		var engine = CreateEngine(map);
		var tick = engine.Tick;
		engine.Commit(Collect(engine.World, tick, [
			new ContractAddition(first, tick + 20),
			new ContractAddition(second, tick + 21),
		]));

		Assert.True(engine.World.ContractRegistry.IsPending("slot-a"));
		Assert.False(engine.World.ContractRegistry.IsPending("slot-b"));
	}

	[Fact]
	public void TryEnqueue_RejectsStoryObjectiveContract()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var template = map.ContractRegistry.Pending.First();
		var hunt = (HuntObjective)template.Objective;
		var story = template with { Id = "story-copy", IsStoryObjective = true };

		var engine = CreateEngine(map);
		var tick = engine.Tick;
		var sim = engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(Collect(engine.World, tick, [new ContractAddition(story, tick + 20)])));
	}

	[Fact]
	public void TryEnqueue_RejectsTickMismatch()
	{
		var map = maps.FreshWithBeatAHunt(42);
		var engine = CreateEngine(map);
		var sim = engine.CreateSimulation();
		Assert.False(sim.TryEnqueue(Collect(engine.World, engine.Tick + 1, [])));
	}

	private static MaintainContractBoardAction Collect(
		StarMap map,
		int tick,
		IReadOnlyList<ContractAddition> additions) =>
		new(StarSystemActorIds.Contracts, tick, additions);

	private static Engine<StarMap, ActorRuntime> CreateEngine(StarMap map)
	{
		StarSystemTestHarness.AddPlayerFleet(map, GrimSpace.Run.State.PlayerFleetUnitId);
		var runtimes = new ActorRuntimes<ActorRuntime>();
		runtimes.For(StarSystemActorIds.Contracts);
		foreach (var id in map.FleetRegistry.Ids)
			runtimes.For(id);
		return new Engine<StarMap, ActorRuntime>(map, runtimes);
	}

	private static Contract Clone(Contract template, string contractId, HuntObjective hunt) =>
		new(
			contractId,
			hunt,
			template.IssuerFaction,
			template.IssuerPoiId,
			template.Terms,
			template.Narrative,
			ContractFactory.IsHuntObjectiveMet);
}
