using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.Units;
using GrimSpace.World.StarSystem.Units;
using FleetState = GrimSpace.World.StarSystem.Units.State;

namespace GrimSpace.Run.Persistence;

internal sealed class FleetJsonConverter : JsonConverter<Fleet>
{
	public override Fleet Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var payload = document.RootElement;
		var state = payload.GetProperty("state").Deserialize<FleetState>(options)
			?? throw new JsonException("Fleet state is missing.");
		var members = payload.TryGetProperty("members", out var membersPayload)
			? membersPayload.Deserialize<IReadOnlyList<FleetMember>>(options)
			: [];
		var registrations = payload.TryGetProperty("registrations", out var registrationsPayload)
			? registrationsPayload.Deserialize<IReadOnlyList<ShipSpawnDeclaration>>(options)
			: [];
		return new Fleet(state, members, registrations);
	}

	public override void Write(
		Utf8JsonWriter writer,
		Fleet value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(
			writer,
			new
			{
				value.State,
				value.Members,
				value.Registrations,
			},
			options);
}
