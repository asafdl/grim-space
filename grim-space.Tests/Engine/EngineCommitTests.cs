using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem;
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
		foreach (var unit in map.UnitRegistry.All)
			actorRuntimes.For(unit.State.Id);

		var engine = new Engine<StarMap, ActorRuntime>(map, actorRuntimes);
		Assert.Equal(0, engine.WorldVersion);

		engine.Commit();

		Assert.Equal(0, engine.WorldVersion);
	}
}
