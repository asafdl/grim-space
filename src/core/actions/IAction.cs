namespace GrimSpace.Core.Actions;

public interface IAction<TBoard, TSlice, in TContext> : IEnqueueable
{
	bool IsLegal(TBoard board, TContext context);

	IReadOnlyList<IEffect<TSlice>> Resolve(TBoard board);
}
