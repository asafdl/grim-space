using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Core.Log;
using GrimSpace.Math;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Effects;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Runtime;
using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.World.StarSystem.Actions;

public sealed record ReactToHostileContactAction(
	string ActorId,
	FleetContactTarget Target,
	Coord Destination,
	TransitPath Path) : IAction<StarMap, ActorRuntime>
{
	public IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> Definition =>
		ReactToHostileContactDef.Instance;
}

public sealed class ReactToHostileContactDef
	: IActionDef<IAction, StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>
{
	private const int IgnoreCooldownTicks = 30;

	public static ReactToHostileContactDef Instance { get; } = new();

	public IEnumerable<IAction> Discover(StarMap world, ActorRuntime runtime, string actorId) => [];

	public bool IsPossible(IAction action, StarMap world, ActorRuntime runtime) => true;

	public bool IsLegal(IAction action, StarMap world, ActorRuntime runtime) =>
		action is ReactToHostileContactAction react
		&& react.Target is { UnitId: var targetId }
		&& world.FleetRegistry.TryGet(react.ActorId, out var actor)
		&& actor.State.Type == EType.PirateFleet
		&& actor.State.AggressionRating > 0
		&& HostileContactQueries.IsStillEligible(
			world,
			react.ActorId,
			targetId,
			runtime,
			world.Timeline.Clock.Current);

	public IReadOnlyList<IEffect<StarMap, ActorRuntime>> Resolve(
		IAction action,
		StarMap world,
		ActorRuntime runtime)
	{
		var react = (ReactToHostileContactAction)action;
		var target = react.Target.UnitId;
		var decision = RollDecision(world, react.ActorId, target);
		if (world.FleetRegistry.FleetOf(react.ActorId).State.Type == EType.PirateFleet)
			GameLog.Log(
				$"[star-map] hostile reaction actor={react.ActorId} target={target} " +
				$"tick={world.Timeline.Clock.Current} aggression={decision.AggressionRating} " +
				$"roll={decision.Roll:0.000000} threshold={decision.Threshold:0.000000} " +
				$"pursue={decision.ShouldPursue}");
		if (decision.ShouldPursue)
		{
			return PursueContactDef.ResolveEffects(
				new PursueContactAction(
					react.ActorId,
					react.Target,
					react.Destination,
					react.Path),
				world,
				runtime);
		}

		return
		[
			new RecordHostileContactIgnoreEffect(
				target,
				world.Timeline.Clock.Current + IgnoreCooldownTicks),
		];
	}

	private static (bool ShouldPursue, int AggressionRating, double Roll, double Threshold) RollDecision(
		StarMap world,
		string actorId,
		string targetId)
	{
		var actor = world.FleetRegistry.FleetOf(actorId);
		var roll = new StableRandom(
			StableSeedMixer.From(world.Seed)
				.Add(world.Timeline.Clock.Current)
				.Add(actorId)
				.Add(targetId)
				.Add("aggression-pursuit")
				.Value).NextDouble();
		var threshold = actor.State.AggressionRating / 10.0;
		return (roll < threshold, actor.State.AggressionRating, roll, threshold);
	}
}
