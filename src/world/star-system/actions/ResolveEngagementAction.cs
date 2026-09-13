using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ResolveEngagementAction(
	string VictorFleetId,
	string DefeatedFleetId,
	BattleOutcome Outcome) : IAction<StarMap, ActorRuntime>
{
	public string ActorId => DefeatedFleetId;

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ResolveEngagementDef.Instance;
}

public sealed class ResolveEngagementDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ResolveEngagementDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ResolveEngagementAction resolve
		&& resolve.Outcome.Result == EBattleResult.Win
		&& resolve.Outcome.IsOver
		&& resolve.Outcome.TryGetState(resolve.VictorFleetId, out var victorState)
		&& victorState == EBattleParticipantState.Alive
		&& resolve.Outcome.TryGetState(resolve.DefeatedFleetId, out var defeatedState)
		&& defeatedState == EBattleParticipantState.Destroyed
		&& HasExactParticipantStates(
			resolve.Outcome,
			resolve.VictorFleetId,
			resolve.DefeatedFleetId)
		&& TryResolveEngagedCounterparty(world, resolve.VictorFleetId, out var counterpartyId)
		&& counterpartyId == resolve.DefeatedFleetId;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var resolve = (ResolveEngagementAction)action;
		if (!IsLegal(resolve, world, runtime))
			return [];

		return
		[
			new ResolveEngagementEffect(
				resolve.VictorFleetId,
				resolve.DefeatedFleetId),
		];
	}

	private static bool HasExactParticipantStates(
		BattleOutcome outcome,
		string actorId,
		string counterpartyId) =>
		outcome.ParticipantStates.Count == 2
		&& outcome.ParticipantStates.ContainsKey(actorId)
		&& outcome.ParticipantStates.ContainsKey(counterpartyId);

	internal static bool TryResolveEngagedCounterparty(StarMap world, string actorId, out string counterpartyId)
	{
		counterpartyId = "";
		if (!world.FleetRegistry.TryGet(actorId, out var actor))
			return false;

		if (actor.State.EngagementPhase != EEngagementPhase.Engaged
			|| actor.State.EngagedWithUnitIds.Count != 1)
			return false;

		counterpartyId = actor.State.EngagedWithUnitIds.First();
		return world.FleetRegistry.Contains(counterpartyId);
	}
}
