using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ResolveEngagementAction(string ActorId, BattleOutcome Outcome)
	: IAction<StarMap, ActorRuntime>
{
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
		&& resolve.Outcome.IsOver
		&& TryResolveEngagedCounterparty(world, resolve.ActorId, out _);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var resolve = (ResolveEngagementAction)action;
		if (!TryResolveEngagedCounterparty(world, resolve.ActorId, out var counterpartyId))
			return [];

		return [new ResolveEngagementEffect(resolve.ActorId, counterpartyId, resolve.Outcome)];
	}

	internal static bool TryResolveEngagedCounterparty(StarMap world, string actorId, out string counterpartyId)
	{
		counterpartyId = "";
		if (!world.UnitRegistry.TryGet(actorId, out var actor))
			return false;

		if (actor.State.EngagementPhase != EEngagementPhase.Engaged
			|| actor.State.EngagedWithUnitIds.Count == 0)
			return false;

		counterpartyId = actor.State.EngagedWithUnitIds.First();
		return world.UnitRegistry.Contains(counterpartyId);
	}
}
