using GrimSpace.Units.Enums;

namespace GrimSpace.Battle.Objectives;

public sealed record TacticalUnitOutcome(
	string TacticalUnitId,
	string OwnerParticipantId,
	EType Kind,
	EBattleParticipantState State);

public sealed class BattleOutcome
{
	public EBattleResult Result { get; }
	public IReadOnlyDictionary<string, EBattleParticipantState> ParticipantStates { get; }
	public IReadOnlyList<TacticalUnitOutcome> TacticalUnitOutcomes { get; }
	public bool IsOver => Result != EBattleResult.Ongoing;

	private BattleOutcome(
		EBattleResult result,
		IReadOnlyDictionary<string, EBattleParticipantState> participantStates,
		IReadOnlyList<TacticalUnitOutcome> tacticalUnitOutcomes)
	{
		Result = result;
		ParticipantStates = participantStates;
		TacticalUnitOutcomes = tacticalUnitOutcomes;
	}

	public static BattleOutcome Create(
		EBattleResult result,
		ReadOnlySpan<(string ParticipantId, EBattleParticipantState State)> participants,
		ReadOnlySpan<TacticalUnitOutcome> tacticalUnitOutcomes)
	{
		if (participants.Length == 0)
			throw new ArgumentException("A battle outcome requires at least one participant.", nameof(participants));
		foreach (var participant in participants)
		{
			if (string.IsNullOrWhiteSpace(participant.ParticipantId))
				throw new ArgumentException("Battle participant IDs cannot be empty.", nameof(participants));
		}

		var states = participants.ToArray().ToDictionary(
			participant => participant.ParticipantId,
			participant => participant.State,
			StringComparer.Ordinal);
		return new BattleOutcome(result, states, tacticalUnitOutcomes.ToArray());
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
