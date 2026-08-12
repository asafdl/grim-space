using GrimSpace.Battle.Actions;
using GrimSpace.Battle.Movement;
using GrimSpace.Battle.Runtime;
using GrimSpace.Battle.World;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;

namespace GrimSpace.Battle.Player;

public sealed class UserExecutionAgent
	: ExecutionAgent<BattleWorld, ActorRuntime>,
		IActionSink
{
	private bool _committed;

	public BattleSimulation Sim { get; private set; } = null!;

	public bool IsPlanning => _isActive && !_committed;

	public bool CanUndo => IsPlanning && Sim.Actions.Count > 0;

	public event Action? PlanningChanged;

	public bool TryEnqueue(IReadOnlyList<IAction> actions)
	{
		if (_committed || !_isActive || actions.Count == 0)
			return false;

		if (!Sim.TryEnqueue(keepRecords: true, actions: [..actions]))
			return false;

		if (actions.Any(action => action is HeadingTurnAction or RollAction))
			OrientationStreamline.CompactQueue(Sim);

		NotifyPlanningChanged();
		return true;
	}

	public bool Undo()
	{
		if (_committed || !_isActive || Sim.Actions.Count == 0)
			return false;

		if (!Sim.TryUndoLast())
			return false;

		NotifyPlanningChanged();
		return true;
	}

	public bool Commit()
	{
		if (_committed || !_isActive)
			return false;

		if (!Sim.TryCommit(out var actions, out _))
			return false;

		var streamlined = OrientationStreamline.Compact(actions, Sim.UndoGroups);
		_committed = true;
		_actions!.TrySetResult(streamlined);
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
