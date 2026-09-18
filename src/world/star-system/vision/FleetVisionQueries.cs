using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Vision;

public static class FleetVisionQueries
{
	public static bool CanSee(
		StarMap world,
		string observerId,
		string targetId,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentException.ThrowIfNullOrEmpty(observerId);
		ArgumentException.ThrowIfNullOrEmpty(targetId);
		ArgumentNullException.ThrowIfNull(runtimeFor);

		if (!world.FleetRegistry.TryGet(observerId, out var observer)
			|| !world.FleetRegistry.TryGet(targetId, out _))
			return false;

		if (observerId == targetId)
			return true;

		var observerSample = FleetPositionSampler.Sample(
			world,
			observer.State,
			runtimeFor(observerId),
			tickFraction);
		var targetSample = FleetPositionSampler.Sample(
			world,
			world.FleetRegistry.FleetOf(targetId).State,
			runtimeFor(targetId),
			tickFraction);

		var dx = observerSample.X - targetSample.X;
		var dz = observerSample.Z - targetSample.Z;
		var radiusSquared = observer.State.VisionRadius * observer.State.VisionRadius;
		return dx * dx + dz * dz <= radiusSquared;
	}

	public static IReadOnlySet<string> VisibleTo(
		StarMap world,
		string observerId,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction)
	{
		ArgumentNullException.ThrowIfNull(world);
		ArgumentNullException.ThrowIfNull(runtimeFor);

		var visible = new HashSet<string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(observerId) || !world.FleetRegistry.TryGet(observerId, out _))
			return visible;

		foreach (var fleet in world.FleetRegistry.All)
		{
			if (CanSee(world, observerId, fleet.State.Id, runtimeFor, tickFraction))
				visible.Add(fleet.State.Id);
		}

		return visible;
	}
}
