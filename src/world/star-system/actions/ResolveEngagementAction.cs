using GrimSpace.Battle.Objectives;
using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Run;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Resources;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ResolveEngagementAction(
	string InitiatorId,
	BattleOutcome Outcome,
	IReadOnlyList<LootRoll> LootRolls,
	ResourceBundle LootTotal) : IAction<StarMap, ActorRuntime>
{
	private static readonly string[] ActionTags =
		[StarMapActionTags.Reward, StarMapActionTags.Salvage];

	public string ActorId => InitiatorId;

	IReadOnlyList<string> IAction.Tags => ActionTags;

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
		&& resolve.Outcome.Result != EBattleResult.Ongoing
		&& world.FleetRegistry.TryGet(resolve.InitiatorId, out var initiator)
		&& initiator.State.CurrentEngagement is
		{
			Phase: EEngagementPhase.Engaged,
		} engagement
		&& engagement.Id == resolve.Outcome.BattleId;
	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var resolve = (ResolveEngagementAction)action;
		if (!IsLegal(resolve, world, runtime))
			return [];

		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new ResolveEngagementEffect(resolve.Outcome),
		};
		if (!resolve.LootTotal.IsEmpty && resolve.ActorId == Run.State.PlayerFleetUnitId)
			effects.Add(new AwardEngagementLootEffect(
				resolve.LootRolls,
				resolve.LootTotal));

		return runtime.ActionEffects.Apply(resolve, world, runtime, effects);
	}
}
