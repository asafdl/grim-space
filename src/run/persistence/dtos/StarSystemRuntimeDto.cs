using System.Text.Json;

namespace GrimSpace.Run.Persistence;

public sealed record StarSystemRuntimeDto(
	string ActorId,
	JsonElement? CachedPath,
	JsonElement? PendingCompletion,
	int PendingCompletionTick,
	long JourneyIdSequence,
	IReadOnlyDictionary<string, int> IgnoreUntilTickByTargetId,
	int ActionCooldownUntilTick,
	string? SelectedMemberShipId,
	long FuelLedgerJourneyId,
	double FuelChargedDistanceAlongCurrentJourney,
	double FuelUnchargedDistance);
