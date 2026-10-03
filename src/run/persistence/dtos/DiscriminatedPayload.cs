using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrimSpace.Run.Persistence;

internal sealed record DiscriminatedPayload(
	[property: JsonPropertyName("type")] string Type,
	[property: JsonPropertyName("payload")] JsonElement Payload);
