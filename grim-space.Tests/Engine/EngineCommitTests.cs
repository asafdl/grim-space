using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;

namespace GrimSpace.Tests.Engine;

public sealed class EngineCommitTests(DevStarMapFixture maps)
{
	[Fact]
	public void EmptyCommit_DoesNotBumpWorldVersion()
	{
		var map = maps.Fresh(42);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in map.FleetRegistry.All)
			actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Assert.Equal(0, engine.WorldVersion);

		engine.Commit();

		Assert.Equal(0, engine.WorldVersion);
	}

	[Fact]
	public void Subscribe_NotifiesOnlyMatchingEntriesCommittedByEngine()
	{
		using var engine = CreateEngine();
		var actorId = engine.World.FleetRegistry.Ids.First();
		var received = new List<BeginNarrativeAction>();
		using var subscription = engine.Subscribe<BeginNarrativeAction>(received.Add);
		var direct = new BeginNarrativeAction(actorId, "direct-append");
		var committed = new BeginNarrativeAction(actorId, "committed");

		engine.World.Timeline.Append(direct);
		engine.Commit(committed);

		Assert.Equal(committed, Assert.Single(received));
	}

	[Fact]
	public void DisposeSubscription_StopsCommitNotifications()
	{
		using var engine = CreateEngine();
		var actorId = engine.World.FleetRegistry.Ids.First();
		var notifications = 0;
		var subscription = engine.Subscribe<BeginNarrativeAction>(_ => notifications++);

		subscription.Dispose();
		subscription.Dispose();
		engine.Commit(new BeginNarrativeAction(actorId, "committed"));

		Assert.Equal(0, notifications);
	}

	[Fact]
	public void Subscribe_UsesExactEntryType()
	{
		using var engine = CreateEngine();
		var actorId = engine.World.FleetRegistry.Ids.First();
		var notifications = 0;
		using var subscription = engine.Subscribe<IAction>(_ => notifications++);

		engine.Commit(new BeginNarrativeAction(actorId, "committed"));

		Assert.Equal(0, notifications);
	}

	private Engine<StarMap, ActorRuntime> CreateEngine()
	{
		var map = maps.Fresh(42);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in map.FleetRegistry.All)
			actorRuntimes.For(unit.State.Id);

		return new Engine<StarMap, ActorRuntime>(
			map,
			actorRuntimes,
			TimelineGcOptions.Disabled);
	}
}
