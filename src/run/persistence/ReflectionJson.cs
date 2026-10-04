using System.Runtime.CompilerServices;
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
		var destinationProperties = destinationType.GetProperties(
			BindingFlags.Instance | BindingFlags.Public);
		var projected = new JsonObject();

		if (source is ITuple tuple)
		{
			var readable = destinationProperties.Where(property => property.CanRead).ToArray();
			if (tuple.Length != readable.Length)
				throw new InvalidDataException(
					$"Cannot map {tuple.Length}-element tuple to '{destinationType.Name}' with {readable.Length} properties.");

			for (var index = 0; index < readable.Length; index++)
			{
				var destinationProperty = readable[index];
				var propertyName = destinationProperty
					.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
					?? serializerOptions.PropertyNamingPolicy?.ConvertName(destinationProperty.Name)
					?? destinationProperty.Name;
				projected[propertyName] = JsonSerializer.SerializeToNode(
					tuple[index],
					destinationProperty.PropertyType,
					serializerOptions);
			}

			return projected.Deserialize<TDestination>(serializerOptions)
				?? throw new InvalidDataException(
					$"Unable to restore '{destinationType.FullName}'.");
		}

		foreach (var destinationProperty in destinationProperties)
		{
			if (!destinationProperty.CanRead)
				continue;

			if (!TryReadSourceMember(
				source,
				destinationProperty.Name,
				out var propertyValue,
				out var propertyType))
				continue;

			var propertyName = destinationProperty
				.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
				?? serializerOptions.PropertyNamingPolicy?.ConvertName(destinationProperty.Name)
				?? destinationProperty.Name;
			projected[propertyName] = JsonSerializer.SerializeToNode(
				propertyValue,
				propertyType,
				serializerOptions);
		}

		return projected.Deserialize<TDestination>(serializerOptions)
			?? throw new InvalidDataException(
				$"Unable to restore '{destinationType.FullName}'.");
	}

	private static bool TryReadSourceMember(
		object source,
		string memberName,
		out object? value,
		out Type propertyType)
	{
		var sourceType = source.GetType();
		const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
		var property = sourceType.GetProperty(memberName, flags);
		if (property is not null)
		{
			value = property.GetValue(source);
			propertyType = property.PropertyType;
			return true;
		}

		var field = sourceType.GetField(memberName, flags);
		if (field is not null)
		{
			value = field.GetValue(source);
			propertyType = field.FieldType;
			return true;
		}

		if (source is ITuple tuple
			&& TryReadTupleElement(sourceType, tuple, memberName, out value, out propertyType))
			return true;

		value = null;
		propertyType = typeof(object);
		return false;
	}

	private static bool TryReadTupleElement(
		Type tupleType,
		ITuple tuple,
		string memberName,
		out object? value,
		out Type propertyType)
	{
		var field = tupleType.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
		if (field is not null)
		{
			value = field.GetValue(tuple);
			propertyType = field.FieldType;
			return true;
		}

		var names = tupleType.GetCustomAttribute<TupleElementNamesAttribute>()?.TransformNames;
		if (names is not null)
		{
			for (var index = 0; index < names.Count; index++)
			{
				if (!string.Equals(names[index], memberName, StringComparison.OrdinalIgnoreCase))
					continue;
				value = tuple[index];
				propertyType = tupleType.GenericTypeArguments[index];
				return true;
			}
		}

		value = null;
		propertyType = typeof(object);
		return false;
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
