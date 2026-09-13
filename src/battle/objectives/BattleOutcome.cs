namespace GrimSpace.Battle.Objectives;

public sealed class BattleOutcome
{
	public EBattleResult Result { get; }
	public IReadOnlyDictionary<string, EBattleParticipantState> ParticipantStates { get; }
	public bool IsOver => Result != EBattleResult.Ongoing;

	private BattleOutcome(
		EBattleResult result,
		IReadOnlyDictionary<string, EBattleParticipantState> participantStates)
	{
		Result = result;
		ParticipantStates = participantStates;
	}

	public static BattleOutcome Create(
		EBattleResult result,
		params (string ParticipantId, EBattleParticipantState State)[] participants)
	{
		ArgumentNullException.ThrowIfNull(participants);
		if (participants.Length == 0)
			throw new ArgumentException("A battle outcome requires at least one participant.", nameof(participants));
		if (participants.Any(participant => string.IsNullOrWhiteSpace(participant.ParticipantId)))
			throw new ArgumentException("Battle participant IDs cannot be empty.", nameof(participants));

		var states = participants.ToDictionary(
			participant => participant.ParticipantId,
			participant => participant.State,
			StringComparer.Ordinal);
		return new BattleOutcome(result, states);
	}

	public bool TryGetState(string participantId, out EBattleParticipantState state) =>
		ParticipantStates.TryGetValue(participantId, out state);

	public EBattleParticipantState StateOf(string participantId) =>
		ParticipantStates.TryGetValue(participantId, out var state)
			? state
			: throw new KeyNotFoundException($"Battle participant '{participantId}' is not in the outcome.");
}

public enum EBattleParticipantState
{
	Alive,
	Destroyed,
}
