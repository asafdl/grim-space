using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.Math.Grid;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	public ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> ActionEffects { get; private set; } =
		ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>>.Empty;

	public TransitPath? CachedPath { get; set; }
	public IAction? PendingCompletion { get; set; }
	public int PendingCompletionTick { get; set; }
	public long JourneyIdSequence { get; set; }
	public int ActionCooldownUntilTick { get; set; }
	public int PursuitLastReplanTick { get; set; }
	public long PursuitTrackedTargetJourneyId { get; set; } = -1;
	public Coord PursuitTrackedTargetJourneyDestination { get; set; }
	public Coord PursuitPlannedInterceptDestination { get; set; }
	public bool PursuitForceReplan { get; set; }
	public string? SelectedMemberShipId { get; set; }
	public long FuelLedgerJourneyId { get; set; }
	public double FuelChargedDistanceAlongCurrentJourney { get; set; }
	public double FuelUnchargedDistance { get; set; }
	public Dictionary<string, int> IgnoreUntilTickByTargetId { get; } =
		new(StringComparer.Ordinal);

	internal void ReplaceActionEffects(
		ActionEffectPipeline<StarMap, ActorRuntime, IEffect<StarMap, ActorRuntime>> pipeline)
	{
		ArgumentNullException.ThrowIfNull(pipeline);
		ActionEffects = pipeline;
	}

	public long NextJourneyId() => ++JourneyIdSequence;

	public void TrackPendingCompletion(IAction action, int tick)
	{
		PendingCompletion = action;
		PendingCompletionTick = tick;
	}

	public void ClearPendingCompletion()
	{
		PendingCompletion = null;
		PendingCompletionTick = 0;
	}

	public void Reset()
	{
		CachedPath = null;
		ClearPendingCompletion();
		JourneyIdSequence = 0;
		ActionCooldownUntilTick = 0;
		PursuitLastReplanTick = 0;
		PursuitTrackedTargetJourneyId = -1;
		PursuitTrackedTargetJourneyDestination = default;
		PursuitPlannedInterceptDestination = default;
		PursuitForceReplan = false;
		SelectedMemberShipId = null;
		ClearTravelFuelLedger();
		IgnoreUntilTickByTargetId.Clear();
	}

	public void ClearTravelFuelLedger()
	{
		FuelLedgerJourneyId = 0;
		FuelChargedDistanceAlongCurrentJourney = 0;
		FuelUnchargedDistance = 0;
	}

	public void ApplyTravelFuelLedger(long journeyId, double chargedAlongJourney, double unchargedDistance)
	{
		FuelLedgerJourneyId = journeyId;
		FuelChargedDistanceAlongCurrentJourney = chargedAlongJourney;
		FuelUnchargedDistance = unchargedDistance;
	}

	public ActorRuntime Fork() => ActorRuntimeCopy.Clone(this);
}

public readonly record struct ActorRuntimeSnapshot(
	TransitPath? CachedPath,
	IAction? PendingCompletion,
	int PendingCompletionTick,
	long JourneyIdSequence,
	int ActionCooldownUntilTick,
	IReadOnlyDictionary<string, int> IgnoreUntilTickByTargetId,
	long FuelLedgerJourneyId = 0,
	double FuelChargedDistanceAlongCurrentJourney = 0,
	double FuelUnchargedDistance = 0);

public static class ActorRuntimeCopy
{
	public static ActorRuntimeSnapshot Snapshot(ActorRuntime session) =>
		new(
			session.CachedPath,
			session.PendingCompletion,
			session.PendingCompletionTick,
			session.JourneyIdSequence,
			session.ActionCooldownUntilTick,
			new Dictionary<string, int>(
				session.IgnoreUntilTickByTargetId,
				StringComparer.Ordinal),
			session.FuelLedgerJourneyId,
			session.FuelChargedDistanceAlongCurrentJourney,
			session.FuelUnchargedDistance);

	public static void Restore(ActorRuntime session, ActorRuntimeSnapshot snapshot)
	{
		session.CachedPath = snapshot.CachedPath;
		session.PendingCompletion = snapshot.PendingCompletion;
		session.PendingCompletionTick = snapshot.PendingCompletionTick;
		session.JourneyIdSequence = snapshot.JourneyIdSequence;
		session.ActionCooldownUntilTick = snapshot.ActionCooldownUntilTick;
		session.ApplyTravelFuelLedger(
			snapshot.FuelLedgerJourneyId,
			snapshot.FuelChargedDistanceAlongCurrentJourney,
			snapshot.FuelUnchargedDistance);
		session.IgnoreUntilTickByTargetId.Clear();
		foreach (var (targetId, ignoreUntilTick) in snapshot.IgnoreUntilTickByTargetId)
			session.IgnoreUntilTickByTargetId[targetId] = ignoreUntilTick;
	}

	public static ActorRuntime Clone(ActorRuntime session)
	{
		var clone = new ActorRuntime();
		Restore(clone, Snapshot(session));
		clone.SelectedMemberShipId = session.SelectedMemberShipId;
		clone.ReplaceActionEffects(session.ActionEffects);
		return clone;
	}
}
