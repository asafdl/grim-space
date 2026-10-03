using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Reflection;

namespace GrimSpace.Run.Persistence;

public static class ReflectionJson
{
	public static JsonSerializerOptions CreateOptions(
		Action<JsonSerializerOptions>? configure = null)
	{
		var options = new JsonSerializerOptions
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
			PropertyNameCaseInsensitive = true,
		};
		configure?.Invoke(options);
		return options;
	}

	public static JsonElement Write(
		object value,
		JsonSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(value);
		return JsonSerializer.SerializeToElement(
			value,
			value.GetType(),
			options ?? CreateOptions());
	}

	public static T Read<T>(
		JsonElement value,
		JsonSerializerOptions? options = null) =>
		JsonSerializer.Deserialize<T>(value, options ?? CreateOptions())
		?? throw new InvalidDataException(
			$"Unable to restore '{typeof(T).FullName}'.");

	public static TDestination Map<TDestination>(
		object source,
		JsonSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(source);
		var serializerOptions = options ?? CreateOptions();
		var destinationType = typeof(TDestination);
		var sourceProperties = source.GetType()
			.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.ToDictionary(property => property.Name, StringComparer.OrdinalIgnoreCase);
		var destinationProperties = destinationType.GetProperties(
			BindingFlags.Instance | BindingFlags.Public);
		var projected = new JsonObject();

		foreach (var destinationProperty in destinationProperties)
		{
			if (!destinationProperty.CanRead
				|| !sourceProperties.TryGetValue(
					destinationProperty.Name,
					out var sourceProperty))
				continue;

			var propertyName = destinationProperty
				.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
				?? serializerOptions.PropertyNamingPolicy?.ConvertName(destinationProperty.Name)
				?? destinationProperty.Name;
			var propertyValue = sourceProperty.GetValue(source);
			projected[propertyName] = JsonSerializer.SerializeToNode(
				propertyValue,
				sourceProperty.PropertyType,
				serializerOptions);
		}

		return projected.Deserialize<TDestination>(serializerOptions)
			?? throw new InvalidDataException(
				$"Unable to restore '{destinationType.FullName}'.");
	}

	public static object Read(
		JsonElement value,
		Type type,
		JsonSerializerOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(type);
		return JsonSerializer.Deserialize(
			value,
			type,
			options ?? CreateOptions())
			?? throw new InvalidDataException(
				$"Unable to restore '{type.FullName}'.");
	}
}
