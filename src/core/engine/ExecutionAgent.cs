using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public abstract class ExecutionAgent<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private bool _isInitialized;
	private IActionBatchWriter? _writer;
	private bool _batchInFlight;

	protected string? _actorId;
	protected bool _canWork;
	protected int _canWorkGeneration;

	public string? ActorId => _actorId;

	internal bool IsInitialized => _isInitialized;

	protected IActionBatchWriter? Writer => _writer;

	protected bool CanPublish => _canWork && !_batchInFlight;

	protected virtual bool PublishOnActivate => true;

	protected int CanWorkGeneration => _canWorkGeneration;

	public void Init(string actorId, IActionBatchWriter writer)
	{
		ArgumentNullException.ThrowIfNull(writer);

		if (_isInitialized)
			return;

		_isInitialized = true;
		_actorId = actorId;
		_writer = writer;
	}

	public virtual void SetCanWork(bool canWork)
	{
		if (!_isInitialized)
			return;

		if (_canWork == canWork)
			return;

		_canWork = canWork;
		if (!canWork)
			return;

		_canWorkGeneration++;
		_batchInFlight = false;
		OnActivated();

		if (PublishOnActivate)
			OnPublishIfReady();
	}

	public virtual void OnWorldUpdated()
	{
	}

	protected virtual void OnActivated()
	{
	}

	protected virtual void OnPublishIfReady()
	{
	}

	protected void MarkBatchInFlight() => _batchInFlight = true;

	protected void ClearBatchInFlight() => _batchInFlight = false;

	protected void Publish(IReadOnlyList<IAction> actions)
	{
		if (!_canWork || _writer is null || _actorId is null)
			return;

		_batchInFlight = true;
		_writer.Publish(new ActionBatch(_actorId, actions));
	}

	protected void Publish(IReadOnlyList<IAction> actions, int jobCanWorkGeneration)
	{
		if (jobCanWorkGeneration != _canWorkGeneration)
			return;

		Publish(actions);
	}

	protected void Fail(Exception exception)
	{
		if (!_canWork || _writer is null)
			return;

		_writer.Fail(exception);
	}

	protected void Fail(Exception exception, int jobCanWorkGeneration)
	{
		if (jobCanWorkGeneration != _canWorkGeneration)
			return;

		Fail(exception);
	}

	public static void Initialize(
		ExecutionAgent<TWorld, TRuntime> agent,
		string actorId,
		Func<Simulation<TWorld, TRuntime>> createSimulation,
		IActionBatchWriter writer)
	{
		ArgumentNullException.ThrowIfNull(createSimulation);

		if (agent is SimulationExecutionAgent<TWorld, TRuntime> simulationAgent)
			simulationAgent.Init(actorId, createSimulation, writer);
		else
			agent.Init(actorId, writer);
	}
}
