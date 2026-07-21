using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public static class SimulationRunner<TContext, TSlice, TAction>
	where TContext : ActionContext<TSlice>
	where TAction : class, IAction<TContext, TSlice>
{
	public static void Step(TContext context, TAction action) =>
		action.Apply(context);

	public static bool TryStep(TContext context, TAction action)
	{
		if (!action.IsLegal(context))
			return false;

		Step(context, action);
		return true;
	}
}
