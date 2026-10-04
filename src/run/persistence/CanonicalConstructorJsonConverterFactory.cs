using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrimSpace.Run.Persistence;

/// <summary>
/// Deserializes types with multiple public constructors using the one with the most parameters,
/// binding JSON object properties to constructor parameters by name (camelCase).
/// </summary>
internal sealed class CanonicalConstructorJsonConverterFactory : JsonConverterFactory
{
	public override bool CanConvert(Type typeToConvert)
	{
		if (typeToConvert.IsAbstract
			|| typeToConvert.IsInterface
			|| typeToConvert.IsPrimitive
			|| typeToConvert == typeof(string)
			|| typeToConvert.Namespace?.StartsWith("GrimSpace.", StringComparison.Ordinal) != true)
			return false;

		var constructors = typeToConvert.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
		return constructors.Length > 1;
	}

	public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
	{
		var constructor = typeToConvert
			.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			.OrderByDescending(ctor => ctor.GetParameters().Length)
			.First();
		return (JsonConverter)Activator.CreateInstance(
			typeof(CanonicalConstructorConverter<>).MakeGenericType(typeToConvert),
			constructor)!;
	}

	private sealed class CanonicalConstructorConverter<T> : JsonConverter<T>
	{
		private readonly ConstructorInfo _constructor;

		public CanonicalConstructorConverter(ConstructorInfo constructor) =>
			_constructor = constructor;

		public override T Read(
			ref Utf8JsonReader reader,
			Type typeToConvert,
			JsonSerializerOptions options)
		{
			using var document = JsonDocument.ParseValue(ref reader);
			var root = document.RootElement;
			if (root.ValueKind != JsonValueKind.Object)
				throw new JsonException($"Expected JSON object for '{typeToConvert.Name}'.");

			var parameters = _constructor.GetParameters();
			var arguments = new object?[parameters.Length];
			for (var index = 0; index < parameters.Length; index++)
			{
				var parameter = parameters[index];
				var propertyName = options.PropertyNamingPolicy?.ConvertName(parameter.Name!)
					?? parameter.Name!;
				if (!root.TryGetProperty(propertyName, out var property))
					throw new JsonException(
						$"Missing property '{propertyName}' for '{typeToConvert.Name}'.");

				arguments[index] = JsonSerializer.Deserialize(
					property.GetRawText(),
					parameter.ParameterType,
					options);
			}

			return (T)_constructor.Invoke(arguments)!;
		}

		public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
		{
			JsonSerializer.Serialize(writer, value, PersistenceJson.OptionsForWrite(options));
		}
	}
}
