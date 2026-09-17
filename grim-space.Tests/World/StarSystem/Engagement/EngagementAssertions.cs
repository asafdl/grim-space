using GrimSpace.World.StarSystem.Units;

namespace GrimSpace.Tests.World.StarSystem.Engagement;

internal static class EngagementAssertions
{
	public static EEngagementPhase Phase(State state) =>
		state.CurrentEngagement?.Phase ?? EEngagementPhase.None;

	public static string? Hunting(State state) => state.CurrentEngagement?.Hunting;

	public static string? HuntedBy(State state) => state.CurrentEngagement?.HuntedBy;

	public static string? EngagedCounterparty(State state)
	{
		if (state.CurrentEngagement?.Phase != EEngagementPhase.Engaged)
			return null;

		return state.CurrentEngagement.EngagementParticipantIds
			.FirstOrDefault(id => id != state.Id);
	}

	public static IReadOnlyCollection<string> Participants(State state) =>
		state.CurrentEngagement?.EngagementParticipantIds ?? [];
}
