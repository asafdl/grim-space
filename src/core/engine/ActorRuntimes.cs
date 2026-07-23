using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public sealed class ActorRuntimes<TRuntime>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private readonly Dictionary<string, TRuntime> _contexts = new();

	public TRuntime For(string actorId)
	{
		if (!_contexts.TryGetValue(actorId, out var runtime))
		{
			runtime = new TRuntime();
			_contexts[actorId] = runtime;
		}

		return runtime;
	}

	public TRuntime For(IAction action) => For(action.ActorId);

	public ActorRuntimes<TRuntime> Fork()
	{
		var fork = new ActorRuntimes<TRuntime>();
		foreach (var (id, context) in _contexts)
			fork._contexts[id] = context.Fork();

		return fork;
	}

	public void Reset()
	{
		foreach (var context in _contexts.Values)
			context.Reset();
	}
}
