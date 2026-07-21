namespace GrimSpace.Core.Actions;

public interface IAction
{
	string OwnerId { get; }

	int? UndoGroup { get; }
}

public interface IAction<TContext, TSlice> : IAction
	where TContext : ActionContext<TSlice>
{
	bool IsLegal(TContext context);

	IReadOnlyList<IEffect<TSlice>> Resolve(TContext context);

	void Apply(TContext context)
	{
		foreach (var effect in Resolve(context))
			effect.Apply(context.Slices);
	}
}
