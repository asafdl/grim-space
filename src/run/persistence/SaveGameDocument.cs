using System.Text.Json;
using System.Text.Json.Serialization;

namespace GrimSpace.Run.Persistence;

public sealed record SaveGameDocument(
	[property: JsonPropertyName("saveSlotId")] string SaveSlotId,
	[property: JsonPropertyName("formatVersion")] int FormatVersion,
	[property: JsonPropertyName("gameVersion")] string GameVersion,
	[property: JsonPropertyName("savedAtUtc")] DateTimeOffset SavedAtUtc,
	[property: JsonPropertyName("activeScene")] string ActiveScene,
	[property: JsonPropertyName("payload")] JsonElement Payload)
{
	public const int CurrentFormatVersion = 1;
	public const string DefaultSaveSlotId = "slot-1";

	public static SaveGameDocument Create(
		string gameVersion,
		string activeScene,
		object payload,
		string saveSlotId = DefaultSaveSlotId,
		JsonSerializerOptions? options = null)
	{
		ArgumentException.ThrowIfNullOrEmpty(gameVersion);
		ArgumentException.ThrowIfNullOrEmpty(activeScene);
		ArgumentException.ThrowIfNullOrEmpty(saveSlotId);
		ArgumentNullException.ThrowIfNull(payload);

		return new SaveGameDocument(
			saveSlotId,
			CurrentFormatVersion,
			gameVersion,
			DateTimeOffset.UtcNow,
			activeScene,
			ReflectionJson.Write(payload, options));
	}

	public void Validate()
	{
		if (string.IsNullOrWhiteSpace(SaveSlotId)
			|| SaveSlotId.Contains(Path.DirectorySeparatorChar)
			|| SaveSlotId.Contains(Path.AltDirectorySeparatorChar)
			|| SaveSlotId is "." or "..")
			throw new InvalidDataException("Save slot ID is invalid.");
		if (FormatVersion <= 0)
			throw new InvalidDataException("Save format version must be positive.");
		if (string.IsNullOrWhiteSpace(GameVersion))
			throw new InvalidDataException("Save game version is missing.");
		if (string.IsNullOrWhiteSpace(ActiveScene))
			throw new InvalidDataException("Save active scene is missing.");
		if (Payload.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
			throw new InvalidDataException("Save payload is missing.");
	}
}
