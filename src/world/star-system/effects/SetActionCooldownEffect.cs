using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public sealed class SetActionCooldownEffect(int durationTicks)
	: IEffect<StarMap, ActorRuntime>
{
	private int _previousUntilTick;

	public IReadOnlyList<IRecord> Apply(
		StarMap world,
		ActorRuntime runtime,
		string actorId)
	{
		_previousUntilTick = runtime.ActionCooldownUntilTick;
		runtime.ActionCooldownUntilTick = System.Math.Max(
			_previousUntilTick,
			world.Timeline.Clock.Current + durationTicks);
		return [];
	}

	public void Undo(StarMap world, ActorRuntime runtime, string actorId) =>
		runtime.ActionCooldownUntilTick = _previousUntilTick;
}
