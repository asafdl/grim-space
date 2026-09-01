using GrimSpace.Core.Actions;
using GrimSpace.Core.Engine;
using GrimSpace.World.StarSystem.Pathfinding;

namespace GrimSpace.World.StarSystem.Runtime;

public sealed class ActorRuntime : IRuntimeContext<ActorRuntime>
{
	public TransitPath? CachedPath { get; set; }
	public IAction? PendingCompletion { get; set; }
	public int PendingCompletionTick { get; set; }
	public long JourneyIdSequence { get; set; }

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
	}

	public ActorRuntime Fork() => ActorRuntimeCopy.Clone(this);
}

public readonly record struct ActorRuntimeSnapshot(
	TransitPath? CachedPath,
	IAction? PendingCompletion,
	int PendingCompletionTick,
	long JourneyIdSequence);

public static class ActorRuntimeCopy
{
	public static ActorRuntimeSnapshot Snapshot(ActorRuntime session) =>
		new(
			session.CachedPath,
			session.PendingCompletion,
			session.PendingCompletionTick,
			session.JourneyIdSequence);

	public static void Restore(ActorRuntime session, ActorRuntimeSnapshot snapshot)
	{
		session.CachedPath = snapshot.CachedPath;
		session.PendingCompletion = snapshot.PendingCompletion;
		session.PendingCompletionTick = snapshot.PendingCompletionTick;
		session.JourneyIdSequence = snapshot.JourneyIdSequence;
	}

	public static ActorRuntime Clone(ActorRuntime session)
	{
		var clone = new ActorRuntime();
		Restore(clone, Snapshot(session));
		return clone;
	}
}
