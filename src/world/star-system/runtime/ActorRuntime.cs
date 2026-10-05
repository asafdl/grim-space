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
	public int ActionCooldownUntilTick { get; set; }
	public Dictionary<string, int> IgnoreUntilTickByTargetId { get; } =
		new(StringComparer.Ordinal);

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
		IgnoreUntilTickByTargetId.Clear();
	}

	public ActorRuntime Fork() => ActorRuntimeCopy.Clone(this);
}

public readonly record struct ActorRuntimeSnapshot(
	TransitPath? CachedPath,
	IAction? PendingCompletion,
	int PendingCompletionTick,
	long JourneyIdSequence,
	int ActionCooldownUntilTick,
	IReadOnlyDictionary<string, int> IgnoreUntilTickByTargetId);

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
				StringComparer.Ordinal));

	public static void Restore(ActorRuntime session, ActorRuntimeSnapshot snapshot)
	{
		session.CachedPath = snapshot.CachedPath;
		session.PendingCompletion = snapshot.PendingCompletion;
		session.PendingCompletionTick = snapshot.PendingCompletionTick;
		session.JourneyIdSequence = snapshot.JourneyIdSequence;
		session.ActionCooldownUntilTick = snapshot.ActionCooldownUntilTick;
		session.IgnoreUntilTickByTargetId.Clear();
		foreach (var (targetId, ignoreUntilTick) in snapshot.IgnoreUntilTickByTargetId)
			session.IgnoreUntilTickByTargetId[targetId] = ignoreUntilTick;
	}

	public static ActorRuntime Clone(ActorRuntime session)
	{
		var clone = new ActorRuntime();
		Restore(clone, Snapshot(session));
		return clone;
	}
}
