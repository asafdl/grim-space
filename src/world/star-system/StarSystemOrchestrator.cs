using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem;

public sealed class StarSystemOrchestrator
{
	private readonly Engine<StarMap, EmptyRuntime> _engine;

	private StarSystemOrchestrator(Engine<StarMap, EmptyRuntime> engine) =>
		_engine = engine;

	public StarMap Map => _engine.World;

	public int Tick => _engine.Tick;

	public static StarSystemOrchestrator FromMap(StarMap map)
	{
		var actorRuntimes = new ActorRuntimes<EmptyRuntime>();
		actorRuntimes.For(StarSystemActorIds.Traffic);
		return new StarSystemOrchestrator(new Engine<StarMap, EmptyRuntime>(map, actorRuntimes));
	}

	public IReadOnlyList<ITimelineEntry> AdvanceTick()
	{
		_engine.Commit(new AdvanceTrafficAction(StarSystemActorIds.Traffic));
		return _engine.AdvanceTick();
	}

	public void AdvanceTicks(int count)
	{
		ArgumentOutOfRangeException.ThrowIfNegative(count);
		for (var i = 0; i < count; i++)
			AdvanceTick();
	}
}
