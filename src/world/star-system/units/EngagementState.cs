namespace GrimSpace.World.StarSystem.Units;

internal static class EngagementState
{
	public static EEngagementPhase Phase(State state) =>
		state.CurrentEngagement?.Phase ?? EEngagementPhase.None;

	public static string? Hunting(State state) => state.CurrentEngagement?.Hunting;

	public static string? HuntedBy(State state) => state.CurrentEngagement?.HuntedBy;

	public static bool IsEngaged(State state) =>
		state.CurrentEngagement?.Phase == EEngagementPhase.Engaged;

	public static bool HasMutualHuntLink(State actor, State counterparty) =>
		counterparty.CurrentEngagement?.HuntedBy == actor.Id
		|| actor.CurrentEngagement?.HuntedBy == counterparty.Id;

	public static string? EngagedCounterparty(State state)
	{
		if (state.CurrentEngagement?.Phase != EEngagementPhase.Engaged)
			return null;

		return state.CurrentEngagement.EngagementParticipantIds
			.FirstOrDefault(id => id != state.Id);
	}

	public static void ClearHuntedBy(State target, string hunterId)
	{
		if (target.CurrentEngagement?.HuntedBy == hunterId)
			target.CurrentEngagement = null;
	}
}
