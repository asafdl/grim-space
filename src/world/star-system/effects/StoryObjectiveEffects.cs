using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Objectives;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class AddStoryObjectiveEffect(StoryObjective objective)
	: IEffect<StarMap, ActorRuntime>
{
	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		world.StoryObjectives.Add(objective);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		world.StoryObjectives.TryComplete(objective.Id);
}

public sealed class CompleteStoryObjectiveEffect(StoryObjective objective)
	: IEffect<StarMap, ActorRuntime>
{
	private bool _completed;

	public IReadOnlyList<IRecord> Apply(StarMap world, ActorRuntime runtime, string actorId)
	{
		_completed = world.StoryObjectives.TryComplete(objective.Id);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId)
	{
		if (_completed)
			world.StoryObjectives.Add(objective);
	}
}
