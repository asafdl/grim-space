namespace GrimSpace.Core.Actions;

public static class PlanExecutor
{
	public static void Apply<TAction, TBoard, TSlice, TContext>(
		IReadOnlyList<TAction> actions,
		TBoard board,
		Func<TBoard, TSlice> toSlices)
		where TAction : IAction<TBoard, TSlice, TContext>
	{
		var slices = toSlices(board);

		foreach (var action in actions)
		{
			foreach (var effect in action.Resolve(board))
				effect.Apply(slices);
		}
	}
}
