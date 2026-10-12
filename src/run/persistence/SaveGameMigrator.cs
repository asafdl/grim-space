using System.Text.Json;
using System.Text.Json.Nodes;

namespace GrimSpace.Run.Persistence;

public static class SaveGameMigrator
{
	public static SaveGameDocument MigrateToCurrent(SaveGameDocument document)
	{
		ArgumentNullException.ThrowIfNull(document);
		return document.FormatVersion switch
		{
			SaveGameDocument.CurrentFormatVersion => document,
			3 => MigrateFromV3(document),
			_ => document,
		};
	}

	private static SaveGameDocument MigrateFromV3(SaveGameDocument document)
	{
		var payload = JsonNode.Parse(document.Payload.GetRawText())?.AsObject()
			?? throw new InvalidDataException("Save payload is not a JSON object.");
		StripLegacyLeadVisitedAtTick(payload);
		return document with
		{
			FormatVersion = SaveGameDocument.CurrentFormatVersion,
			Payload = JsonSerializer.SerializeToElement(payload),
		};
	}

	private static void StripLegacyLeadVisitedAtTick(JsonObject payload)
	{
		if (payload["starMap"] is not JsonObject starMap
			|| starMap["contracts"] is not JsonArray contracts)
			return;

		foreach (var contract in contracts)
		{
			if (contract?["state"] is not JsonObject state)
				continue;

			state.Remove("visitedAtTick");
		}
	}
}
