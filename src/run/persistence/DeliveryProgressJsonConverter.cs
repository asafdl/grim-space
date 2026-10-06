using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.World.StarSystem.Contracts;

namespace GrimSpace.Run.Persistence;

internal sealed class DeliveryProgressJsonConverter : JsonConverter<DeliveryProgress>
{
	public override DeliveryProgress Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var payload = document.RootElement;
		var completedLegs = payload.GetProperty("completedLegs").Deserialize<IReadOnlyList<bool>>(options)
			?? throw new JsonException("Delivery completed legs are missing.");
		var currentLegIndex = payload.TryGetProperty("currentLegIndex", out var legIndex)
			? legIndex.GetInt32()
			: 0;
		var activationTick = payload.TryGetProperty("activationTick", out var activation)
			? activation.Deserialize<int?>(options)
			: null;
		var deadlineTick = payload.TryGetProperty("deadlineTick", out var deadline)
			? deadline.Deserialize<int?>(options)
			: null;
		var redirectAcknowledged = payload.TryGetProperty("redirectAcknowledged", out var redirect)
			&& redirect.GetBoolean();
		var interceptorFleetId = payload.TryGetProperty("interceptorFleetId", out var interceptor)
			? interceptor.GetString()
			: null;
		var interceptionState = ReadInterceptionState(payload, interceptorFleetId);
		var failureReason = ReadFailureReason(payload);

		return new DeliveryProgress(
			completedLegs,
			currentLegIndex,
			activationTick,
			deadlineTick,
			redirectAcknowledged,
			interceptorFleetId,
			interceptionState,
			failureReason);
	}

	public override void Write(
		Utf8JsonWriter writer,
		DeliveryProgress value,
		JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName("completedLegs");
		JsonSerializer.Serialize(writer, value.CompletedLegs, options);
		writer.WriteNumber("currentLegIndex", value.CurrentLegIndex);
		WriteNullableNumber(writer, "activationTick", value.ActivationTick);
		WriteNullableNumber(writer, "deadlineTick", value.DeadlineTick);
		writer.WriteBoolean("redirectAcknowledged", value.RedirectAcknowledged);
		writer.WriteString("interceptorFleetId", value.InterceptorFleetId);
		writer.WriteString("interceptionState", value.InterceptionState.ToString());
		writer.WriteString("failureReason", value.FailureReason?.ToString());
		writer.WriteEndObject();
	}

	private static EDeliveryInterceptionState ReadInterceptionState(
		JsonElement payload,
		string? interceptorFleetId)
	{
		if (payload.TryGetProperty("interceptionState", out var stateElement)
			&& stateElement.ValueKind == JsonValueKind.String
			&& Enum.TryParse<EDeliveryInterceptionState>(
				stateElement.GetString(),
				ignoreCase: true,
				out var parsed))
			return parsed;

		if (payload.TryGetProperty("interceptorResolved", out var resolved)
			&& resolved.GetBoolean())
			return EDeliveryInterceptionState.Resolved;

		if (payload.TryGetProperty("interceptionTriggered", out var triggered)
			&& triggered.GetBoolean())
			return EDeliveryInterceptionState.Pending;

		if (!string.IsNullOrEmpty(interceptorFleetId))
			return EDeliveryInterceptionState.Assigned;

		return EDeliveryInterceptionState.None;
	}

	private static EDeliveryFailureReason? ReadFailureReason(JsonElement payload)
	{
		if (!payload.TryGetProperty("failureReason", out var reasonElement)
			|| reasonElement.ValueKind == JsonValueKind.Null)
			return null;

		if (reasonElement.ValueKind == JsonValueKind.String
			&& Enum.TryParse<EDeliveryFailureReason>(
				reasonElement.GetString(),
				ignoreCase: true,
				out var reason))
			return reason;

		throw new JsonException("Delivery failure reason is invalid.");
	}

	private static void WriteNullableNumber(Utf8JsonWriter writer, string name, int? value)
	{
		if (value is { } number)
			writer.WriteNumber(name, number);
		else
			writer.WriteNull(name);
	}
}
