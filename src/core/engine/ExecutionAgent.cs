using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public abstract class ExecutionAgent<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private bool _isInitialized = false;
	protected string? _actorId;

	public string? ActorId => _actorId;
	private Func<Simulation<TWorld, TRuntime>>? _createSimulation;
	protected bool _isActive;

	protected TaskCompletionSource<IReadOnlyList<IAction>>? _actions;

	public Task<IReadOnlyList<IAction>> GetActions() =>
		_actions?.Task ?? Task.FromResult((IReadOnlyList<IAction>)Array.Empty<IAction>());

	public IReadOnlyList<IAction> TakeCompletedActions()
	{
		if (!_isActive || _actions is null)
			throw new InvalidOperationException("Agent is not active.");

		if (!_actions.Task.IsCompleted)
			throw new InvalidOperationException("Agent has not completed action production.");

		if (_actions.Task.IsFaulted)
			throw _actions.Task.Exception!.GetBaseException();

		return _actions.Task.GetAwaiter().GetResult();
	}

	protected void Complete(IReadOnlyList<IAction> actions)
	{
		if (!_isActive || _actions is null)
			throw new InvalidOperationException("Cannot complete actions while inactive.");

		if (_actions.Task.IsCompleted)
			return;

		if (!_actions.TrySetResult(actions))
			throw new InvalidOperationException("Failed to complete actions.");
	}

	protected void Fail(Exception exception)
	{
		if (!_isActive || _actions is null)
			throw new InvalidOperationException("Cannot fail while inactive.");

		if (_actions.Task.IsCompleted)
			return;

		if (!_actions.TrySetException(exception))
			throw new InvalidOperationException("Failed to fail actions.");
	}

	protected abstract void ProduceActionsJob(Simulation<TWorld, TRuntime> simulation);

	public void Init(string actorId, Func<Simulation<TWorld, TRuntime>> createSimulation, Action<Action<string?>> registerOnActivate) {
		if (_isInitialized)
			return;
		
		_isInitialized = true;
		_actorId = actorId;
		_createSimulation = createSimulation;
		registerOnActivate(OnActivate);
	}

	private void OnActivate(string? activeUnitId) {
		if (! _isInitialized)
			return;
			
		var isActive = activeUnitId == _actorId;
		if (isActive == _isActive)
			return;

		_isActive = isActive;
		if (isActive) {
			_actions = new TaskCompletionSource<IReadOnlyList<IAction>>();
			ProduceActionsJob(_createSimulation!());
		} else 
			_actions = null;
	}
}
