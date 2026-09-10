using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ReachContactAction(string InitiatorId, string TargetId)
	: IAction<StarMap, ActorRuntime>
{
	public string ActorId => InitiatorId;

	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ReachContactDef.Instance;
}

public sealed class ReachContactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static ReachContactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ReachContactAction reach
		&& world.UnitRegistry.TryGet(reach.InitiatorId, out var initiator)
		&& world.UnitRegistry.TryGet(reach.TargetId, out _)
		&& initiator.State.EngagementPhase == EEngagementPhase.Pursuing
		&& initiator.State.EngagementTargetUnitId == reach.TargetId
		&& initiator.State.EngagedWithUnitIds.Count == 0;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var reach = (ReachContactAction)action;
		var effects = new List<IEffect<StarMap, ActorRuntime>>
		{
			new ReachContactEffect(reach.InitiatorId, reach.TargetId),
		};

		if (world.UnitRegistry.TryGet(reach.InitiatorId, out var initiator)
			&& initiator.State.Type == EType.PlayerFleet)
			effects.Add(new PlayerInputEffect(true));

		return effects;
	}
}
