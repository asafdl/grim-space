using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.Tests.World.StarSystem;
using GrimSpace.Tests.World.StarSystem.Traffic;
using RunState = GrimSpace.Run.State;

namespace GrimSpace.Tests.Engine;

[BattleTestSuite]
public sealed class EngineCommitTests(StarMapFixture maps)
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
		var actorId = RunState.PlayerFleetUnitId;
		var received = new List<GrantDebugResourcesAction>();
		using var subscription = engine.Subscribe<GrantDebugResourcesAction>(received.Add);
		var direct = Grant(actorId, 1);
		var committed = Grant(actorId, 2);

		engine.World.Timeline.Append(direct);
		engine.Commit(committed);

		Assert.Equal(committed, Assert.Single(received));
	}

	[Fact]
	public void DisposeSubscription_StopsCommitNotifications()
	{
		using var engine = CreateEngine();
		var actorId = RunState.PlayerFleetUnitId;
		var notifications = 0;
		var subscription = engine.Subscribe<GrantDebugResourcesAction>(_ => notifications++);

		subscription.Dispose();
		subscription.Dispose();
		engine.Commit(Grant(actorId, 1));

		Assert.Equal(0, notifications);
	}

	[Fact]
	public void Subscribe_UsesExactEntryType()
	{
		using var engine = CreateEngine();
		var actorId = RunState.PlayerFleetUnitId;
		var notifications = 0;
		using var subscription = engine.Subscribe<IAction>(_ => notifications++);

		engine.Commit(Grant(actorId, 1));

		Assert.Equal(0, notifications);
	}

	private static GrantDebugResourcesAction Grant(string actorId, int credits) =>
		new(actorId, ResourceBundle.Create((ResourceId.Credits, credits)));

	private Engine<StarMap, ActorRuntime> CreateEngine()
	{
		var map = maps.Fresh(42);
		StarSystemTestHarness.AddPlayerFleet(map, RunState.PlayerFleetUnitId);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in map.FleetRegistry.All)
			actorRuntimes.For(unit.State.Id);

		return new Engine<StarMap, ActorRuntime>(
			map,
			actorRuntimes,
			TimelineGcOptions.Disabled);
	}
}
