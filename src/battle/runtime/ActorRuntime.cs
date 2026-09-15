using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	public void Reset() { }

	public ActorRuntime Fork() => new();
}
