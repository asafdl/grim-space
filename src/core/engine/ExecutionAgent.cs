using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public abstract class ExecutionAgent<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private bool _isInitialized = false;
	protected string? _actorId;
	private Func<Simulation<TWorld, TRuntime>>? _createSimulation;
	protected bool _isActive;

	protected TaskCompletionSource<IReadOnlyList<IAction>>? _actions;

	public Task<IReadOnlyList<IAction>> GetActions() =>
    _actions?.Task ?? Task.FromResult((IReadOnlyList<IAction>)Array.Empty<IAction>());

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
