using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using GrimSpace.World.StarSystem.Contact;
using GrimSpace.World.StarSystem.Contracts.Objectives;
using GrimSpace.World.StarSystem.Pathfinding;
using GrimSpace.World.StarSystem.Resources;

namespace GrimSpace.Run.Persistence;

internal sealed class ResourceBundleJsonConverter : JsonConverter<ResourceBundle>
{
	public override ResourceBundle Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var values = JsonSerializer.Deserialize<Dictionary<ResourceId, int>>(
			ref reader,
			options) ?? [];
		return ResourceBundle.Create(values);
	}

	public override void Write(
		Utf8JsonWriter writer,
		ResourceBundle value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(
			writer,
			value.ToDictionary(pair => pair.Key, pair => pair.Value),
			options);
}

internal sealed class TransitPathJsonConverter : JsonConverter<TransitPath>
{
	public override TransitPath Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var dto = JsonSerializer.Deserialize<TransitPathDto>(ref reader, options)
			?? throw new InvalidDataException("Transit path payload is missing.");
		return new TransitPath(dto.Legs
			.Select(leg => new TransitLeg(
				leg.Points.ToImmutableArray(),
				leg.SpeedMultiplier,
				leg.Length))
			.ToImmutableArray());
	}

	public override void Write(
		Utf8JsonWriter writer,
		TransitPath value,
		JsonSerializerOptions options) =>
		JsonSerializer.Serialize(
			writer,
			new TransitPathDto(value.Legs.Select(leg =>
				new TransitLegDto(leg.Points, leg.SpeedMultiplier, leg.Length)).ToArray()),
			options);
}

internal sealed class ContactTargetJsonConverter : JsonConverter<ContactTarget>
{
	public override ContactTarget Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var dto = JsonSerializer.Deserialize<ContactTargetDto>(ref reader, options)
			?? throw new InvalidDataException("Contact target payload is missing.");
		return dto.Kind switch
		{
			"fleet" => new FleetContactTarget(dto.Id),
			"wreck" => new WreckContactTarget(dto.Id),
			_ => throw new InvalidDataException($"Unknown contact target '{dto.Kind}'."),
		};
	}

	public override void Write(
		Utf8JsonWriter writer,
		ContactTarget value,
		JsonSerializerOptions options)
	{
		var dto = value switch
		{
			FleetContactTarget fleet => new ContactTargetDto("fleet", fleet.UnitId),
			WreckContactTarget wreck => new ContactTargetDto("wreck", wreck.ContractId),
			_ => throw new InvalidDataException(
				$"Unsupported contact target '{value.GetType().Name}'."),
		};
		JsonSerializer.Serialize(writer, dto, options);
	}
}

internal sealed class WreckageOutcomeJsonConverter : JsonConverter<WreckageOutcome>
{
	public override WreckageOutcome Read(
		ref Utf8JsonReader reader,
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		using var document = JsonDocument.ParseValue(ref reader);
		var value = document.RootElement;
		if (value.TryGetProperty("loot", out var loot))
			return new WreckageOutcome.Salvage(
				JsonSerializer.Deserialize<ResourceBundle>(loot, options)
				?? throw new InvalidDataException("Wreckage salvage loot is missing."));

		if (value.TryGetProperty("fleet", out var fleet))
			return new WreckageOutcome.Ambush(
				JsonSerializer.Deserialize<FleetSpawnSpec>(fleet, options)
				?? throw new InvalidDataException("Wreckage ambush fleet is missing."));

		throw new InvalidDataException(
			"Wreckage outcome must contain either 'loot' or 'fleet'.");
	}

	public override void Write(
		Utf8JsonWriter writer,
		WreckageOutcome value,
		JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		switch (value)
		{
			case WreckageOutcome.Salvage salvage:
				writer.WritePropertyName("loot");
				JsonSerializer.Serialize(writer, salvage.Loot, options);
				break;
			case WreckageOutcome.Ambush ambush:
				writer.WritePropertyName("fleet");
				JsonSerializer.Serialize(writer, ambush.Fleet, options);
				break;
			default:
				throw new InvalidDataException(
					$"Unsupported wreckage outcome '{value.GetType().FullName}'.");
		}
		writer.WriteEndObject();
	}
}
