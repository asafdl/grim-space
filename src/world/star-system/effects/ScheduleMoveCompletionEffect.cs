using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Actions;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class ScheduleMoveCompletionEffect : IEffect<StarMap, Runtime.ActorRuntime>
{
	private readonly int _delayTicks;
	private readonly CompleteMoveAction _completion;

	public ScheduleMoveCompletionEffect(int delayTicks, CompleteMoveAction completion)
	{
		_delayTicks = delayTicks;
		_completion = completion;
	}

	public IReadOnlyList<IRecord> Apply(StarMap world, Runtime.ActorRuntime runtime, string actorId)
	{
		var scheduledTick = world.Timeline.Clock.Current + _delayTicks;
		world.Timeline.Schedule(_delayTicks, _completion);
		runtime.TrackPendingCompletion(_completion, scheduledTick);
		return [];
	}

	public void Undo(StarMap world, Runtime.ActorRuntime runtime, string actorId) { }
}
