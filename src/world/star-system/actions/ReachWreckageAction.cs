using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Ids;
using GrimSpace.World.StarSystem.Contracts;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ReachWreckageAction(
	string ActorId,
	string ContractId,
	string AmbushSpawnIdentity) : IAction<StarMap, ActorRuntime>
{
	public ReachWreckageAction(string actorId, string contractId)
		: this(actorId, contractId, TypedIdGenerator.NextInstanceSlug())
	{
	}

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ReachWreckageDef.Instance;
}

public sealed class ReachWreckageDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ReachWreckageDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ReachWreckageAction reach
		&& TryValidateReach(reach, world, runtime, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var reach = (ReachWreckageAction)action;
		if (!TryValidateReach(reach, world, runtime, out var unit))
			return [];

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new StopAtCurrentLocationEffect(reach.ActorId),
			new ClearPursueContactEffect(reach.ActorId),
		};
		var wreckage = (WreckageObjective)world.ContractRegistry.All
			.First(contract => contract.Id == reach.ContractId)
			.Objective;
		if (wreckage.Outcome is WreckageOutcome.Ambush ambush)
		{
			// Ambushes skip the wreck decision and use the engagement decision instead.
			var ambushUnitId = AmbushUnitIdFor(wreckage);
			effects.Add(new RecordWreckInvestigatedEffect(reach.ContractId, wreckage.WreckageId));
			if (!world.FleetRegistry.TryGet(ambushUnitId, out _))
				effects.Add(new SpawnMapUnitEffect(ContractEnemySpawner.CreateAmbushFleet(
					ambush.Fleet,
					wreckage.Position,
					ambushUnitId,
					reach.AmbushSpawnIdentity,
					reach.ContractId)));
			effects.Add(new SetEngagementIntentEffect(reach.ActorId, ambushUnitId));
			effects.Add(new ReachContactEffect(reach.ActorId, ambushUnitId));
		}
		else
			effects.Add(new SetPendingWreckContractEffect(reach.ActorId, reach.ContractId));

		if (unit.State.Type == EType.PlayerFleet)
			effects.Add(new PlayerInputEffect(true));

		return effects;
	}

	internal static bool TryValidateReach(
		ReachWreckageAction reach,
		StarMap world,
		ActorRuntime runtime,
		out Fleet unit)
	{
		unit = null!;
		if (!world.FleetRegistry.TryGet(reach.ActorId, out unit))
			return false;

		if (!string.IsNullOrEmpty(unit.State.PendingWreckContractId))
			return false;

		if (!WreckageQueries.IsActiveWreckContractForHolder(world, reach.ActorId, reach.ContractId)
			|| !world.ContractRegistry.TryGet(reach.ContractId, out var contract)
			|| contract.Objective is not WreckageObjective wreckage)
			return false;

		if (unit.State.TravelTarget.MatchesWreck(reach.ContractId))
			return true;

		return WreckageContactRange.IsWithinReachRange(world, runtime, reach.ActorId, wreckage.Position);
	}

	internal static string AmbushUnitIdFor(WreckageObjective wreckage) =>
		$"{wreckage.WreckageId}.ambush";
}
