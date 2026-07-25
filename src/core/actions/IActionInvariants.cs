namespace GrimSpace.Core.Actions;

public interface IActionInvariants<TWorld, TRuntime>
{
	InvariantStatus EvaluateInvariants(TWorld world, TRuntime runtime, string actorId);
}
