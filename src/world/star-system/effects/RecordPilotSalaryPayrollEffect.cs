using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class RecordPilotSalaryPayrollEffect(PilotSalaryPayroll payroll)
	: IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId) =>
		[new Record<PilotSalaryPayroll>(payroll)];

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) { }
}
