using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Economy;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Ids;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record PayPilotSalariesAction(
	string ActorId,
	int Tick,
	IReadOnlyList<SalaryPayrollCandidate> Roster,
	int RngSeed) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		PayPilotSalariesDef.Instance;
}

public sealed class PayPilotSalariesDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static PayPilotSalariesDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is PayPilotSalariesAction pay
		&& string.Equals(pay.ActorId, StarSystemActorIds.SalaryPayroll, StringComparison.Ordinal)
		&& pay.Tick == world.Timeline.Clock.Current
		&& pay.Roster.Count > 0;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var pay = (PayPilotSalariesAction)action;
		var available = world.PlayerResources.GetBalance(ResourceId.Credits);
		var payroll = SalaryPayrollAccounting.Resolve(
			pay.Roster,
			available,
			new Random(pay.RngSeed));

		var effects = new List<IEffect<StarMap, ActorRuntime>>(2);
		var spend = SalaryPayrollAccounting.CreditSpend(payroll.CreditsSpent);
		if (!spend.IsEmpty)
			effects.Add(new ChangeResourceEffect(TransactionSource.PilotSalary, spend));
		effects.Add(new RecordPilotSalaryPayrollEffect(payroll));
		return effects;
	}
}
