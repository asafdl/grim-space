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
		var members = payload.GetProperty("members")
			.Deserialize<IReadOnlyList<FleetMember>>(options)
			?? throw new JsonException("Fleet members are missing.");
		var registrations = payload.GetProperty("registrations")
			.Deserialize<IReadOnlyList<ShipSpawnDeclaration>>(options)
			?? throw new JsonException("Fleet registrations are missing.");
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
