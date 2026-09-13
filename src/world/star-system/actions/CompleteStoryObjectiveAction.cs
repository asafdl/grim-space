using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record CompleteStoryObjectiveAction(string ActorId, string ObjectiveId)
	: IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		CompleteStoryObjectiveDef.Instance;
}

public sealed class CompleteStoryObjectiveDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	public static CompleteStoryObjectiveDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is CompleteStoryObjectiveAction complete
		&& world.FleetRegistry.Contains(complete.ActorId)
		&& world.StoryObjectives.Active.Any(objective => objective.Id == complete.ObjectiveId);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var complete = (CompleteStoryObjectiveAction)action;
		var objective = world.StoryObjectives.Active.Single(
			objective => objective.Id == complete.ObjectiveId);
		return [new CompleteStoryObjectiveEffect(objective)];
	}
}
