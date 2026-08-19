using GrimSpace.Battle.Ids;
using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Presentation.Replay;

public enum EReplayPlaybackPhase
{
	Player,
	Enemy,
	Upkeep,
}

public static class ReplayActorPhase
{
	public static EReplayPlaybackPhase Classify(string actorId, IReadOnlyDictionary<string, ETeam> participants)
	{
		if (string.Equals(actorId, BattleActorIds.Rules, StringComparison.Ordinal))
			return EReplayPlaybackPhase.Upkeep;

		if (participants.TryGetValue(actorId, out var team))
		{
			return team switch
			{
				ETeam.Player => EReplayPlaybackPhase.Player,
				ETeam.Enemy => EReplayPlaybackPhase.Enemy,
				_ => EReplayPlaybackPhase.Upkeep,
			};
		}

		return EReplayPlaybackPhase.Upkeep;
	}
}
