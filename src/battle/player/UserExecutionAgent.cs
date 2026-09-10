using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Player;

public sealed class UserExecutionAgent
	: SimulationExecutionAgent<BattleWorld, ActorRuntime>,
		IActionSink
{
	private bool _committed;

	public new BattleSimulation Sim { get; private set; } = null!;

	public bool IsPlanning => _canWork && !_committed;

	public bool CanUndo => IsPlanning && Sim.Actions.Count > 0;

	public event Action? PlanningChanged;

	protected override bool PublishOnActivate => false;

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (_committed || !_canWork || actions.Count == 0)
			return false;

		if (!Sim.TryEnqueue(keepRecords: true, actions: [..actions]))
			return false;

		NotifyPlanningChanged();
		return true;
	}

	public bool Undo()
	{
		if (_committed || !_canWork || Sim.Actions.Count == 0)
			return false;

		if (!Sim.TryUndoLast())
			return false;

		NotifyPlanningChanged();
		return true;
	}

	public bool Commit()
	{
		if (_committed || !_canWork)
			return false;

		if (!Sim.TryCommit(out var actions, out _))
			return false;

		_committed = true;
		Publish(actions);
		NotifyPlanningChanged();
		return true;
	}

	protected override void ProduceActionsJob(Simulation<BattleWorld, ActorRuntime> simulation)
	{
		_committed = false;
		Sim = (BattleSimulation)simulation;
		NotifyPlanningChanged();
	}

	private void NotifyPlanningChanged() => PlanningChanged?.Invoke();
}
