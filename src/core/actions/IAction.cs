namespace GrimSpace.Core.Actions;

public interface IAction
{
	string OwnerId { get; }

	int? UndoGroup { get; }
}

public interface IAction<TWorld, TState, TSlice> : IAction
{
	bool IsLegal(TWorld world, TState state, IEnumerable<IAction> applied);

	IReadOnlyList<IEffect<TSlice>> Resolve(TWorld world, TState state, IEnumerable<IAction> applied);
}
