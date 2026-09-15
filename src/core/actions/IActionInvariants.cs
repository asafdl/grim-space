namespace GrimSpace.Core.Actions;

public interface IActionInvariants<TWorld, TRuntime>
{
	InvariantStatus EvaluateInvariants(
		TWorld world,
		TRuntime runtime,
		IReadOnlyList<IAction> actions,
		string actorId);
}
