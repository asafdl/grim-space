namespace GrimSpace.Core.Actions;

public interface IEffect<TWorld, TRuntime>
{
	IReadOnlyList<IRecord> Apply(TWorld world, TRuntime runtime, string actorId);

	void Undo(TWorld world, TRuntime runtime, string actorId);
}
