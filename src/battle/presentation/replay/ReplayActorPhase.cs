using GrimSpace.Battle.Ids;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay;

public enum EReplayPlaybackPhase
{
	Player,
	Enemy,
	System,
}

public static class ReplayActorPhase
{
	public static IReadOnlyList<string> EnemyOverviewActorsForReplay(
		IReadOnlyList<string> activationOrder,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		for (var i = 0; i < activationOrder.Count; i++)
		{
			if (Classify(activationOrder[i], participants) == EReplayPlaybackPhase.Enemy)
				return EnemyOverviewActors(activationOrder, i, activationOrder[i], participants);
		}

		return [];
	}

	public static IReadOnlyList<string> EnemyOverviewActors(
		IReadOnlyList<string> activationOrder,
		int completedCount,
		string currentEnemy,
		IReadOnlyDictionary<string, ETeam> participants)
	{
		if (Classify(currentEnemy, participants) != EReplayPlaybackPhase.Enemy)
			throw new ArgumentException("Overview requires an enemy actor.", nameof(currentEnemy));

		return participants.Where(pair => pair.Value == ETeam.Player)
			.Select(pair => pair.Key)
			.Concat(activationOrder.Skip(completedCount)
				.TakeWhile(id => Classify(id, participants) == EReplayPlaybackPhase.Enemy)
				.Prepend(currentEnemy))
			.Distinct(StringComparer.Ordinal)
			.ToArray();
	}

	public static EReplayPlaybackPhase Classify(string actorId, IReadOnlyDictionary<string, ETeam> participants)
	{
		if (string.Equals(actorId, BattleActorIds.Rules, StringComparison.Ordinal)
			|| string.Equals(actorId, BattleActorIds.Terrain, StringComparison.Ordinal))
			return EReplayPlaybackPhase.System;

		if (!participants.TryGetValue(actorId, out var team))
			throw new KeyNotFoundException($"Replay actor '{actorId}' is not registered.");

		return team switch
		{
			ETeam.Player => EReplayPlaybackPhase.Player,
			ETeam.Enemy => EReplayPlaybackPhase.Enemy,
			_ => throw new InvalidOperationException(
				$"Replay actor '{actorId}' has unsupported team '{team}'."),
		};
	}
}
