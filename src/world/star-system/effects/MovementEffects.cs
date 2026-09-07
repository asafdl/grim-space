using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Actions;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;

namespace GrimSpace.World.StarSystem.Effects;

public static class MovementEffects
{
	public static IReadOnlyList<IEffect<StarMap, ActorRuntime>> BeginJourney(
		string unitId,
		ActorRuntime runtime,
		StarMap world,
		Coord origin,
		Coord destination,
		TransitPath path)
	{
		var state = world.StateOf(unitId);
		var journeyId = runtime.NextJourneyId();
		var startTick = world.Timeline.Clock.Current;
		var durationTicks = path.DurationTicks(state.SpeedPerTick);
		var completion = new CompleteMoveAction(unitId, unitId, journeyId);

		return
		[
			UpdateLocationEffect.BeginJourney(
				unitId,
				journeyId,
				origin,
				destination,
				startTick,
				path),
			new ScheduleMoveCompletionEffect(durationTicks, completion),
		];
	}
}
