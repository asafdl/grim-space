using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrimSpace.Run.Persistence;

/// <summary>
/// System.Text.Json can write <see cref="IReadOnlySet{T}"/> as a JSON array but cannot
/// instantiate the interface on read. Restore as <see cref="HashSet{T}"/>.
/// </summary>
internal sealed class ReadOnlySetJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert) =>
		typeToConvert.IsGenericType
		&& typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlySet<>);

	public override JsonConverter CreateConverter(
		Type typeToConvert,
		JsonSerializerOptions options)
	{
		var elementType = typeToConvert.GetGenericArguments()[0];
		return (JsonConverter)Activator.CreateInstance(
			typeof(ReadOnlySetJsonConverter<>).MakeGenericType(elementType))!;
	}

	private sealed class ReadOnlySetJsonConverter<T> : JsonConverter<IReadOnlySet<T>>
	{
		public override IReadOnlySet<T> Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			var items = JsonSerializer.Deserialize<List<T>>(ref reader, options)
				?? throw new JsonException($"Unable to restore '{typeof(IReadOnlySet<T>)}'.");
			return items.ToHashSet();
		}

		public override void Write(
			Utf8JsonWriter writer,
			IReadOnlySet<T> value,
			JsonSerializerOptions options) =>
			JsonSerializer.Serialize(writer, value.ToList(), options);
	}
}
