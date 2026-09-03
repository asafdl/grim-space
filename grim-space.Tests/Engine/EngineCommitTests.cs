using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.Tests.Engine;

public sealed class EngineCommitTests
{
	[Fact]
	public void EmptyCommit_DoesNotBumpWorldVersion()
	{
		var map = StarMap.CreateDevDefault(42);
		var actorRuntimes = new ActorRuntimes<ActorRuntime>();
		foreach (var unit in map.UnitRegistry.All)
			actorRuntimes.Register(unit.State.Id, unit.Runtime);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Assert.Equal(0, engine.WorldVersion);

		engine.Commit();

		Assert.Equal(0, engine.WorldVersion);
	}
}
