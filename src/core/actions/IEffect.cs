namespace GrimSpace.Core.Actions;

public interface IEffect<TWorld, TRuntime>
{
	void Apply(TWorld world, TRuntime runtime, string actorId);
}
