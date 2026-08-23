namespace GrimSpace.Core.Engine;

public sealed class EmptyRuntime : IRuntimeContext<EmptyRuntime>
{
	public void Reset() { }

	public EmptyRuntime Fork() => new();
}
