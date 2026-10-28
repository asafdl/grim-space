using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Agents;

public sealed class SalaryPayrollExecutionAgent : ExecutionAgent<StarMap, ActorRuntime>
{
	private readonly Func<StarMap> _world;
	private readonly Func<string, ActorRuntime> _runtimeFor;
	private Func<IReadOnlyList<SalaryPayrollCandidate>>? _roster;

	public SalaryPayrollExecutionAgent(
		Func<StarMap> world,
		Func<string, ActorRuntime> runtimeFor)
	{
		_world = world;
		_runtimeFor = runtimeFor;
	}

	public void BindRoster(Func<IReadOnlyList<SalaryPayrollCandidate>> roster)
	{
		ArgumentNullException.ThrowIfNull(roster);
		_roster = roster;
	}

	public void PlanAndPublish()
	{
		if (!_canWork || _actorId is null || _roster is null)
			return;

		ClearBatchInFlight();
		var map = _world();
		var tick = map.Timeline.Clock.Current;
		if (tick <= 0 || tick % StrategicEconomyConfig.SalaryCadenceTicks != 0)
			return;

		var roster = _roster();
		if (roster.Count == 0)
			return;

		var action = new PayPilotSalariesAction(
			StarSystemActorIds.SalaryPayroll,
			tick,
			roster,
			HashCode.Combine(map.Seed, tick, roster.Count));
		if (!PayPilotSalariesDef.Instance.IsLegal(action, map, _runtimeFor(_actorId)))
			return;

		Publish([action]);
	}
}
