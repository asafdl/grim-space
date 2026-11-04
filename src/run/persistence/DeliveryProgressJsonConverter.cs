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
		var completedLegs = payload.GetProperty("completedLegs")
			.Deserialize<IReadOnlyList<bool>>(options)
			?? throw new JsonException("Delivery completed legs are missing.");
		var currentLegIndex = payload.GetProperty("currentLegIndex").GetInt32();
		var activationTick = payload.GetProperty("activationTick").Deserialize<int?>(options);
		var deadlineTick = payload.GetProperty("deadlineTick").Deserialize<int?>(options);
		var redirectAcknowledged = payload.GetProperty("redirectAcknowledged").GetBoolean();
		var interceptorFleetId = payload.GetProperty("interceptorFleetId").GetString();
		var stateElement = payload.GetProperty("interceptionState");
		if (stateElement.ValueKind != JsonValueKind.String
			|| !Enum.TryParse<EDeliveryInterceptionState>(
				stateElement.GetString(),
				ignoreCase: true,
				out var interceptionState)
			|| !Enum.IsDefined(interceptionState))
			throw new JsonException("Delivery interception state is invalid.");
		var failureReason = ReadFailureReason(payload.GetProperty("failureReason"));

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

	private static EDeliveryFailureReason? ReadFailureReason(JsonElement element)
	{
		if (element.ValueKind == JsonValueKind.Null)
			return null;

		if (element.ValueKind == JsonValueKind.String
			&& Enum.TryParse<EDeliveryFailureReason>(
				element.GetString(),
				ignoreCase: true,
				out var reason)
			&& Enum.IsDefined(reason))
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
