using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Units.Enums;

namespace GrimSpace.Run.Persistence;

internal sealed class ContractSpawnMemberJsonConverter :
	JsonConverter<(EType Chassis, EShipGearTier GearTier)>
{
	public override (EType Chassis, EShipGearTier GearTier) Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var payload = document.RootElement;
		if (payload.ValueKind != JsonValueKind.Object
			|| payload.EnumerateObject().Any(property =>
				property.Name is not ("chassis" or "gearTier")))
			throw new JsonException("Contract spawn member does not match the current shape.");
		var chassis = payload.GetProperty("chassis");
		var gearTier = payload.GetProperty("gearTier");

		return (
			(EType)chassis.GetInt32(),
			(EShipGearTier)gearTier.GetInt32());
	}

	public override void Write(
		Utf8JsonWriter writer,
		(EType Chassis, EShipGearTier GearTier) value,
		JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("chassis", (int)value.Chassis);
		writer.WriteNumber("gearTier", (int)value.GearTier);
		writer.WriteEndObject();
	}
}
