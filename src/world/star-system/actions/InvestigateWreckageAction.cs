using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record InvestigateWreckageAction(string ActorId, string ContractId)
	: IAction<StarMap, ActorRuntime>
{
	private static readonly string[] ActionTags =
		[StarMapActionTags.Reward, StarMapActionTags.Salvage];

	IReadOnlyList<string> IAction.Tags => ActionTags;

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		InvestigateWreckageDef.Instance;
}

public sealed class InvestigateWreckageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static InvestigateWreckageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is InvestigateWreckageAction investigate
		&& world.FleetRegistry.TryGet(investigate.ActorId, out var unit)
		&& world.ContractRegistry.TryGet(investigate.ContractId, out var contract)
		&& world.ContractRegistry.TryGetState(investigate.ContractId, out var state)
		&& state.Status == EContractStatus.Active
		&& state.HolderUnitId == investigate.ActorId
		&& state is WreckageContractState wreckageState
		&& !wreckageState.IsObjectiveMet()
		&& contract.Objective is WreckageObjective { Outcome: WreckageOutcome.Salvage }
		&& unit.State.PendingWreckContractId == investigate.ContractId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var investigate = (InvestigateWreckageAction)action;
		if (!IsLegal(investigate, world, runtime))
			return [];

		var contract = world.ContractRegistry.All.First(candidate => candidate.Id == investigate.ContractId);
		var wreckage = (WreckageObjective)contract.Objective;
		var salvage = (WreckageOutcome.Salvage)wreckage.Outcome;
		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new ClearPendingWreckContractEffect(investigate.ActorId),
			new RecordWreckInvestigatedEffect(investigate.ContractId, wreckage.WreckageId),
		};
		if (!salvage.Loot.IsEmpty)
			effects.Add(new ChangeResourceEffect(TransactionSource.WreckageSalvage, salvage.Loot));
		effects.Add(new PlayerInputEffect(false));

		return runtime.ActionEffects.Apply(investigate, world, runtime, effects);
	}
}
