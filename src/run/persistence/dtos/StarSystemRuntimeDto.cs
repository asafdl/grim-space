using System.Text.Json;

namespace GrimSpace.Run.Persistence;

public sealed record StarSystemRuntimeDto(
	string ActorId,
	JsonElement? CachedPath,
	JsonElement? PendingCompletion,
	int PendingCompletionTick,
	long JourneyIdSequence);
