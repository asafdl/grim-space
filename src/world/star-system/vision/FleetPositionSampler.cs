using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Vision;

public static class FleetPositionSampler
{
	public static FleetPositionSample Sample(
		StarMap world,
		State state,
		ActorRuntime runtime,
		float tickFraction)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentNullException.ThrowIfNull(state);
		ArgumentNullException.ThrowIfNull(runtime);

		if (state.CommittedPositionContinuous(world, runtime.CachedPath, tickFraction) is { } continuous)
		{
			var (_, routeTangent) = continuous.Route.ToRoundedCoord();
			return new FleetPositionSample(continuous.Route.X, continuous.Route.Z, routeTangent);
		}

		var (position, tangent) = state.CommittedPosition(
			world,
			runtime.CachedPath,
			tickFraction);
		return new FleetPositionSample(position.X, position.Z, tangent);
	}
}
