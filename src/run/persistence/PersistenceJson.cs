using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrimSpace.Run.Persistence;

/// <summary>
/// Shared System.Text.Json configuration for run persistence.
/// Timeline actions/records use <see cref="CanonicalConstructorJsonConverterFactory"/>
/// so record types with convenience constructors deserialize without per-type DTOs.
/// Types that embed delegates or non-JSON shapes stay on explicit registry handlers.
/// </summary>
public static class PersistenceJson
{
	public static JsonSerializerOptions CreateOptions(
		Action<JsonSerializerOptions>? configure = null)
	{
		var options = ReflectionJson.CreateOptions(o =>
		{
			o.IncludeFields = true;
			o.Converters.Add(new FleetJsonConverter());
			o.Converters.Add(new ShipInstanceJsonConverter());
			o.Converters.Add(new DeliveryProgressJsonConverter());
			o.Converters.Add(new CanonicalConstructorJsonConverterFactory());
			o.Converters.Add(new ContractSpawnMemberJsonConverter());
			o.Converters.Add(new ResourceBundleJsonConverter());
			o.Converters.Add(new TransitPathJsonConverter());
			o.Converters.Add(new ContactTargetJsonConverter());
			o.Converters.Add(new WreckageOutcomeJsonConverter());
		});
		configure?.Invoke(options);
		return options;
	}

	internal static JsonSerializerOptions OptionsForWrite(JsonSerializerOptions readOptions)
	{
		var writeOptions = new JsonSerializerOptions(readOptions);
		for (var index = writeOptions.Converters.Count - 1; index >= 0; index--)
		{
			if (writeOptions.Converters[index] is CanonicalConstructorJsonConverterFactory)
				writeOptions.Converters.RemoveAt(index);
		}

		return writeOptions;
	}
}
