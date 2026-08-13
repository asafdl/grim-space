using GrimSpace.Core;
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
		if (string.Equals(actorId, EntityIds.System, StringComparison.Ordinal))
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
