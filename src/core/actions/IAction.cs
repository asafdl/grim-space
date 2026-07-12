namespace GrimSpace.Core.Actions;

public interface IAction<TBoard, TSlice, in TContext>
{
	bool IsLegal(TBoard board, TContext context);

	IReadOnlyList<IEffect<TSlice>> Resolve(TBoard board);
}
