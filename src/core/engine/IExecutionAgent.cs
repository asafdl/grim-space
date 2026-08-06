using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public interface IExecutionAgent<TWorld, TRuntime, TActor>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	Task<IReadOnlyList<IAction>> GetActionsAsync(
		TActor actor,
		Func<Simulation<TWorld, TRuntime>> createSim);
}
