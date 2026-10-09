using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Units;

namespace GrimSpace.Run.Persistence;

internal sealed class ShipInstanceJsonConverter : JsonConverter<ShipInstance>
{
	public override ShipInstance Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var dto = document.RootElement.Deserialize<ShipPersistenceDto>(options)
			?? throw new JsonException("Ship persistence payload is missing.");
		return SaveDtoMapper.RestoreShip(dto);
	}

	public override void Write(
		Utf8JsonWriter writer,
		ShipInstance value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(writer, SaveDtoMapper.CaptureShip(value), options);
}
