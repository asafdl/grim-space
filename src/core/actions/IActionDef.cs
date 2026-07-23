namespace GrimSpace.Core.Actions;

public interface IActionDef<TAction, TWorld, TRuntime, TEffect>
	where TAction : IAction
	where TEffect : IEffect<TWorld, TRuntime>
{
	IEnumerable<TAction> Discover(TWorld world, TRuntime runtime, string actorId);

	bool IsPossible(TAction action, TWorld world, TRuntime runtime);

	bool IsLegal(TAction action, TWorld world, TRuntime runtime);

	IReadOnlyList<TEffect> Resolve(TAction action, TWorld world, TRuntime runtime);
}
