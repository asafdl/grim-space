using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record InvestigateWreckageAction(
	string ActorId,
	string ContractId,
	string AmbushSpawnIdentity) : IAction<StarMap, ActorRuntime>
{
	public InvestigateWreckageAction(string actorId, string contractId)
		: this(actorId, contractId, TypedIdGenerator.NextInstanceSlug())
	{
	}

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
		&& !ContractFactory.IsWreckageObjectiveMet(investigate.ContractId, world, investigate.ActorId)
		&& contract.Objective is WreckageObjective
		&& unit.State.PendingWreckContractId == investigate.ContractId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var investigate = (InvestigateWreckageAction)action;
		if (!IsLegal(investigate, world, runtime))
			return [];

		if (!world.ContractRegistry.TryGet(investigate.ContractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
			return [];

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new ClearPendingWreckContractEffect(investigate.ActorId),
			new RecordWreckInvestigatedEffect(investigate.ContractId, wreckage.WreckageId),
		};

		switch (wreckage.Outcome)
		{
			case WreckageOutcome.Salvage salvage:
				if (!salvage.Loot.IsEmpty)
					effects.Add(new ChangeResourceEffect(TransactionSource.WreckageSalvage, salvage.Loot));
				effects.Add(new PlayerInputEffect(false));
				break;
			case WreckageOutcome.Ambush ambush:
				var ambushUnitId = AmbushUnitIdFor(wreckage);
				var ambushFleet = ContractEnemySpawner.CreateAmbushFleet(
					ambush.Fleet,
					wreckage.Position,
					ambushUnitId,
					investigate.AmbushSpawnIdentity);
				effects.Add(new SpawnMapUnitEffect(ambushFleet));
				effects.Add(new SetEngagementIntentEffect(investigate.ActorId, ambushUnitId));
				effects.Add(new CommitEngagementEffect(investigate.ActorId, ambushUnitId));
				effects.Add(new PlayerInputEffect(false));
				break;
			default:
				throw new InvalidOperationException(
					$"Unsupported wreckage outcome '{wreckage.Outcome.GetType().Name}'.");
		}

		return effects;
	}

	internal static string AmbushUnitIdFor(WreckageObjective wreckage) =>
		$"{wreckage.WreckageId}.ambush";
}
