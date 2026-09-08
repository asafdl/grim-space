using GrimSpace.Core.Actions;

namespace GrimSpace.Core.Engine;

public abstract class SimulationExecutionAgent<TWorld, TRuntime>
	: ExecutionAgent<TWorld, TRuntime>
	where TWorld : IWorld<TWorld>
	where TRuntime : IRuntimeContext<TRuntime>, new()
{
	private Func<Simulation<TWorld, TRuntime>>? _createSimulation;
	private readonly Queue<IAction> _pendingActions = new();
	private bool _refreshPending;

	public Simulation<TWorld, TRuntime> Sim { get; private set; } = null!;

	public void Init(
		string actorId,
		Func<Simulation<TWorld, TRuntime>> createSimulation,
		IActionBatchWriter writer)
	{
		ArgumentNullException.ThrowIfNull(createSimulation);

		_createSimulation = createSimulation;
		Init(actorId, writer);
	}

	public override void OnWorldUpdated()
	{
		if (!IsInitialized || _createSimulation is null)
			return;

		if (_refreshPending)
			return;

		_refreshPending = true;
		try
		{
			ReforkSimulation();
			if (_canWork)
				ProduceActionsJob(Sim);

			if (Sim.Actions.Count > 0)
				TryPublishIfReady();
		}
		finally
		{
			_refreshPending = false;
		}
	}

	protected bool PushPending(IAction action)
	{
		ArgumentNullException.ThrowIfNull(action);

		if (!_canWork)
			return false;

		_pendingActions.Enqueue(action);
		TryPublishIfReady();
		return true;
	}

	protected void FlushPendingToSim()
	{
		while (_pendingActions.Count > 0)
		{
			var action = _pendingActions.Dequeue();
			if (!Sim.TryEnqueue(action))
				throw new InvalidOperationException($"Failed to enqueue pending action {action}.");
		}
	}

	protected override void OnActivated()
	{
		_pendingActions.Clear();
		Sim = _createSimulation!();
		ProduceActionsJob(Sim);
	}

	protected override void OnPublishIfReady() => TryPublishIfReady();

	protected void TryPublishIfReady()
	{
		if (!CanPublish || Writer is null)
			return;

		MarkBatchInFlight();
		try
		{
			FlushPendingToSim();

			if (!_canWork || Writer is null || _actorId is null)
			{
				ClearBatchInFlight();
				return;
			}

			Writer.Publish(new ActionBatch(_actorId, Sim.Actions.ToList()));
		}
		catch
		{
			ClearBatchInFlight();
			throw;
		}
	}

	protected abstract void ProduceActionsJob(Simulation<TWorld, TRuntime> simulation);

	private void ReforkSimulation()
	{
		ClearBatchInFlight();
		Sim = _createSimulation!();

		var replay = _pendingActions.ToArray();
		_pendingActions.Clear();
		foreach (var action in replay)
			_pendingActions.Enqueue(action);

		FlushPendingToSim();
	}
}
