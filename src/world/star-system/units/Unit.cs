using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Units;

public sealed class Unit
{
	public State State { get; }
	public ActorRuntime Runtime { get; }

	public Unit(State state) : this(state, new ActorRuntime()) { }

	public Unit(State state, ActorRuntime runtime)
	{
		State = state;
		Runtime = runtime;
	}
}
