using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record StopPursuingDockedTargetAction(string ActorId, string TargetId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		StopPursuingDockedTargetDef.Instance;
}

public sealed class StopPursuingDockedTargetDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static StopPursuingDockedTargetDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is StopPursuingDockedTargetAction stop
		&& world.FleetRegistry.TryGet(stop.ActorId, out var actor)
		&& actor.State.CurrentEngagement is
		{
			Phase: EEngagementPhase.Pursuing,
			Hunting: var targetId,
		}
		&& targetId == stop.TargetId
		&& world.FleetRegistry.TryGet(stop.TargetId, out var target)
		&& world.DockAt(target.State) is not null;

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var stop = (StopPursuingDockedTargetAction)action;
		return
		[
			new StopAtCurrentLocationEffect(stop.ActorId),
			new ClearPursueContactEffect(stop.ActorId),
		];
	}
}
