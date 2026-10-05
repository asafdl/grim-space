using GrimSpace.World.Factions;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;
using GrimSpace.World.StarSystem.Vision;

namespace GrimSpace.World.StarSystem.Contact;

public static class HostileContactQueries
{
	public static IReadOnlyList<Fleet> FindEligibleTargets(
		StarMap world,
		string observerId,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction,
		int currentTick)
	{
		if (!world.FleetRegistry.TryGet(observerId, out var observer))
			return [];

		var observerPosition = FleetPositionSampler.Sample(
			world,
			observer.State,
			runtimeFor(observerId),
			tickFraction);

		return world.FleetRegistry.All
			.Where(target => IsEligible(
				world,
				observer,
				target,
				runtimeFor,
				tickFraction,
				currentTick))
			.OrderBy(target => DistanceSquared(
				observerPosition,
				FleetPositionSampler.Sample(
					world,
					target.State,
					runtimeFor(target.State.Id),
					tickFraction)))
			.ThenBy(target => target.State.Id, StringComparer.Ordinal)
			.ToArray();
	}

	public static bool IsStillEligible(
		StarMap world,
		string observerId,
		string targetId,
		ActorRuntime runtime,
		int currentTick)
	{
		if (!world.FleetRegistry.TryGet(observerId, out var observer)
			|| !world.FleetRegistry.TryGet(targetId, out var target))
			return false;

		return IsEligibleCore(observer, target, runtime, currentTick);
	}

	public static bool IsEligible(
		StarMap world,
		Fleet observer,
		Fleet target,
		Func<string, ActorRuntime> runtimeFor,
		float tickFraction,
		int currentTick)
	{
		if (!IsEligibleCore(
				observer,
				target,
				runtimeFor(observer.State.Id),
				currentTick)
			|| !FleetVisionQueries.CanSee(
				world,
				observer.State.Id,
				target.State.Id,
				runtimeFor,
				tickFraction))
			return false;

		return true;
	}

	private static bool IsEligibleCore(
		Fleet observer,
		Fleet target,
		ActorRuntime runtime,
		int currentTick)
	{
		return !(observer.State.AggressionRating <= 0
			|| observer.State.Id == target.State.Id
			|| observer.Members.Count == 0
			|| target.Members.Count == 0
			|| !FactionRelations.IsHostile(observer.State.Faction, target.State.Faction)
			|| !observer.State.CanMove
			|| observer.State.CurrentEngagement is not null
			|| target.State.CurrentEngagement?.HuntedBy is not null
			|| EngagementState.IsEngaged(target.State)
			|| IsIgnored(runtime, target.State.Id, currentTick));
	}

	private static bool IsIgnored(ActorRuntime runtime, string targetId, int currentTick) =>
		runtime.IgnoreUntilTickByTargetId
			.TryGetValue(targetId, out var ignoreUntilTick)
		&& currentTick < ignoreUntilTick;

	private static double DistanceSquared(
		FleetPositionSample left,
		FleetPositionSample right)
	{
		var dx = left.X - right.X;
		var dz = left.Z - right.Z;
		return dx * dx + dz * dz;
	}
}
